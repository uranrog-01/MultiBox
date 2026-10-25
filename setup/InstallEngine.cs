using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace MultiBox.Setup;

public sealed record InstallOptions(
    string InstallDir,
    bool DesktopShortcut,
    bool StartMenuShortcut,
    bool ForAllUsers);

public sealed record InstallProgress(double Fraction, string Stage, string? CurrentFile = null, bool Indeterminate = false);

public sealed class InstallResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ExePath { get; init; }
    public string? Version { get; init; }
}

/// <summary>
/// Extracts the embedded MultiBox payload, writes an on-disk manifest, creates shortcuts,
/// and registers the Add/Remove Programs entry. Fully cancellable; cleans up on failure.
/// </summary>
public static class InstallEngine
{
    public static bool IsMultiBoxRunning()
    {
        try { return Process.GetProcessesByName("MultiBox").Length > 0; }
        catch { return false; }
    }

    /// <summary>Politely closes running MultiBox instances; force-kills after a short grace period.</summary>
    public static void CloseMultiBox()
    {
        try
        {
            var processes = Process.GetProcessesByName("MultiBox");
            foreach (var p in processes)
            {
                try
                {
                    p.CloseMainWindow();
                    if (!p.WaitForExit(3000)) p.Kill(entireProcessTree: true);
                }
                catch { /* already gone */ }
                finally { p.Dispose(); }
            }
        }
        catch (Exception ex)
        {
            SetupLog.Warn($"CloseMultiBox: {ex.Message}");
        }
    }

    public static bool RelaunchElevated(string arguments)
    {
        try
        {
            string? exe = Environment.ProcessPath;
            if (exe is null) return false;
            var psi = new ProcessStartInfo(exe, arguments)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false; // user dismissed the UAC prompt
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    /// <summary>
    /// Creates Uninstall.exe as a hard link to the app executable: a real, separately
    /// launchable uninstaller that costs no extra disk space or payload size.
    /// Falls back to <c>MultiBox.exe --uninstall</c> where hard links don't exist (exFAT, network drives).
    /// </summary>
    private static bool TryCreateUninstallLink(string uninstallExe, string exePath)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                if (File.Exists(uninstallExe)) File.Delete(uninstallExe);
                if (CreateHardLink(uninstallExe, exePath, IntPtr.Zero))
                {
                    SetupLog.Info($"Uninstaller linked: {uninstallExe}");
                    return true;
                }
            }
            catch { /* antivirus can hold the fresh exe briefly; retry */ }
            Thread.Sleep(400);
        }
        SetupLog.Warn("Could not create the Uninstall.exe hard link; shortcuts will call MultiBox.exe --uninstall instead.");
        return false;
    }

    public static async Task<InstallResult> InstallAsync(
        InstallOptions options, IProgress<InstallProgress> progress, CancellationToken ct)
    {
        string dir = Path.GetFullPath(options.InstallDir);
        string exePath = Path.Combine(dir, "MultiBox.exe");
        bool directoryWasNew = !Directory.Exists(dir);
        var written = new List<string>();
        string version = "0.0.0";

        try
        {
            progress.Report(new InstallProgress(0, "Preparing", Indeterminate: true));

            if (!Payload.IsEmbedded)
                return Fail("This setup package has no embedded payload. Rebuild it with build-setup.ps1.");

            var manifest = Payload.LoadManifest();
            version = manifest.Version ?? "0.0.0";

            if (IsMultiBoxRunning())
            {
                progress.Report(new InstallProgress(0, "Closing MultiBox", Indeterminate: true));
                CloseMultiBox();
            }

            Directory.CreateDirectory(dir);
            progress.Report(new InstallProgress(0, "Extracting MultiBox"));

            long totalBytes = 0, doneBytes = 0;
            await using (Stream zipStream = Payload.OpenZip())
            await using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                    if (!string.IsNullOrEmpty(entry.Name)) totalBytes += entry.Length;

                foreach (var entry in archive.Entries)
                {
                    ct.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(entry.Name)) continue; // directory marker

                    string destPath = Path.Combine(dir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                    string destDir = Path.GetDirectoryName(destPath)!;
                    Directory.CreateDirectory(destDir);

                    string display = entry.FullName;
                    progress.Report(new InstallProgress(
                        totalBytes > 0 ? (double)doneBytes / totalBytes : 0.02,
                        "Extracting MultiBox", display));

                    await using (var src = entry.Open())
                    await using (var dst = File.Create(destPath))
                    {
                        await src.CopyToAsync(dst, 1 << 16, ct);
                    }
                    written.Add(Path.GetRelativePath(dir, destPath));
                    doneBytes += entry.Length;
                }
            }

            if (!File.Exists(exePath))
                throw new IOException("Payload archive did not contain MultiBox.exe.");

            progress.Report(new InstallProgress(0.9, "Creating the uninstaller"));
            string uninstallExe = Path.Combine(dir, "Uninstall.exe");
            bool linked = TryCreateUninstallLink(uninstallExe, exePath);
            if (linked) written.Add("Uninstall.exe");

            progress.Report(new InstallProgress(0.93, "Writing manifest"));
            await WriteManifestAsync(dir, version, written, ct);

            progress.Report(new InstallProgress(0.96, "Creating shortcuts"));
            CreateShortcuts(options, exePath, uninstallExe, linked);

            progress.Report(new InstallProgress(0.98, "Registering"));
            RegisterUninstallEntry(options, exePath, version, manifest.TotalBytes > 0 ? manifest.TotalBytes : totalBytes, linked, uninstallExe);

            progress.Report(new InstallProgress(1, "Done"));
            SetupLog.Info($"Install complete: {dir} (v{version})");
            return new InstallResult { Success = true, ExePath = exePath, Version = version };
        }
        catch (OperationCanceledException)
        {
            SetupLog.Info("Install cancelled by user; rolling back.");
            Rollback(dir, written, directoryWasNew);
            return new InstallResult { Success = false, Error = "cancelled" };
        }
        catch (Exception ex)
        {
            SetupLog.Error("Install failed", ex);
            Rollback(dir, written, directoryWasNew);
            return new InstallResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Pure portable extraction (no shortcuts, registry, or manifest) for --extract-to.</summary>
    public static async Task ExtractPortableAsync(string targetDir, IProgress<InstallProgress> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(targetDir);
        await using Stream zipStream = Payload.OpenZip();
        await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        long totalBytes = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).Sum(e => e.Length);
        long done = 0;
        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name)) continue;
            string destPath = Path.Combine(targetDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            progress.Report(new InstallProgress(totalBytes > 0 ? (double)done / totalBytes : 0, "Extracting", entry.FullName));
            await using var src = entry.Open();
            await using var dst = File.Create(destPath);
            await src.CopyToAsync(dst, 1 << 16, ct);
            done += entry.Length;
        }
        progress.Report(new InstallProgress(1, "Done"));
        SetupLog.Info($"Portable extraction complete: {targetDir}");
    }

    private static async Task WriteManifestAsync(string dir, string version, List<string> files, CancellationToken ct)
    {
        var payload = new
        {
            version,
            installed = DateTime.UtcNow.ToString("o"),
            files
        };
        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(dir, "manifest.json"), json, ct);
    }

    private static void CreateShortcuts(InstallOptions options, string exePath, string uninstallExe, bool linked)
    {
        if (options.DesktopShortcut)
        {
            ShortcutCreator.Create(
                Path.Combine(InstallTargets.DesktopDir, "MultiBox.lnk"),
                exePath, "MultiBox — isolated app sessions");
        }
        if (options.StartMenuShortcut)
        {
            Directory.CreateDirectory(InstallTargets.StartMenuDir);
            ShortcutCreator.Create(
                Path.Combine(InstallTargets.StartMenuDir, "MultiBox.lnk"),
                exePath, "MultiBox — isolated app sessions");
            if (linked)
            {
                ShortcutCreator.Create(
                    Path.Combine(InstallTargets.StartMenuDir, "Uninstall MultiBox.lnk"),
                    uninstallExe, "Uninstall MultiBox");
            }
            else
            {
                ShortcutCreator.Create(
                    Path.Combine(InstallTargets.StartMenuDir, "Uninstall MultiBox.lnk"),
                    exePath, "Uninstall MultiBox", "--uninstall");
            }
        }
    }

    private static void RegisterUninstallEntry(InstallOptions options, string exePath, string version, long totalBytes, bool linked, string uninstallExe)
    {
        try
        {
            RegistryKey root = options.ForAllUsers ? Registry.LocalMachine : Registry.CurrentUser;
            using RegistryKey? key = root.CreateSubKey(InstallTargets.UninstallKeyPath, writable: true);
            if (key is null) throw new IOException("Could not open the uninstall registry key.");
            key.SetValue("DisplayName", "MultiBox");
            key.SetValue("DisplayVersion", version);
            key.SetValue("Publisher", "zer0ne");
            key.SetValue("InstallLocation", options.InstallDir);
            key.SetValue("DisplayIcon", exePath);
            key.SetValue("UninstallString", linked ? $"\"{uninstallExe}\"" : $"\"{exePath}\" --uninstall");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            key.SetValue("EstimatedSize", (int)(totalBytes / 1024), RegistryValueKind.DWord);
            key.SetValue("URLInfoAbout", InstallTargets.WebsiteUrl);
            key.SetValue("HelpLink", InstallTargets.WebsiteUrl);
            SetupLog.Info($"Uninstall entry registered ({(options.ForAllUsers ? "HKLM" : "HKCU")}).");
        }
        catch (Exception ex)
        {
            // Registration is cosmetic for usability, not correctness of the files on disk.
            SetupLog.Warn($"Uninstall registration failed: {ex.Message}");
        }
    }

    private static void Rollback(string dir, List<string> written, bool directoryWasNew)
    {
        foreach (string relative in written)
        {
            try { File.Delete(Path.Combine(dir, relative)); } catch { }
        }
        try
        {
            if (Directory.Exists(dir) && directoryWasNew && Directory.EnumerateFileSystemEntries(dir).Any() == false)
                Directory.Delete(dir);
        }
        catch { }
    }

    private static InstallResult Fail(string message)
    {
        SetupLog.Error(message);
        return new InstallResult { Success = false, Error = message };
    }
}
