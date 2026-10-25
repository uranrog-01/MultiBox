using System.IO;
using Microsoft.Win32;

namespace MultiBox.Setup;

/// <summary>Well-known install locations and registry entries shared by setup and uninstall.</summary>
public static class InstallTargets
{
    public const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiBox";
    public const string WebsiteUrl = "https://multibox.zer0ne.site";

    public static string DefaultPerUserDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MultiBox");

    public static string AllUsersDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MultiBox");

    public static string DesktopDir =>
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    public static string StartMenuProgramsDir =>
        Environment.GetFolderPath(Environment.SpecialFolder.Programs);

    public static string StartMenuDir => Path.Combine(StartMenuProgramsDir, "MultiBox");

    public static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiBox");

    public sealed record InstalledInfo(string InstallDir, string Version);

    /// <summary>Detects an existing MultiBox installation (registry first, then default directory).</summary>
    public static InstalledInfo? Detect()
    {
        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            try
            {
                using var key = root.OpenSubKey(UninstallKeyPath);
                if (key?.GetValue("InstallLocation") is string loc && Directory.Exists(loc))
                {
                    string version = key.GetValue("DisplayVersion") as string ?? "unknown";
                    return new InstalledInfo(loc, version);
                }
            }
            catch
            {
                // Fall through to directory-based detection.
            }
        }

        foreach (string dir in new[] { DefaultPerUserDir, AllUsersDir })
        {
            string manifest = Path.Combine(dir, "manifest.json");
            if (File.Exists(manifest))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifest));
                    string version = doc.RootElement.TryGetProperty("version", out var v) ? v.GetString() ?? "unknown" : "unknown";
                    return new InstalledInfo(dir, version);
                }
                catch
                {
                    return new InstalledInfo(dir, "unknown");
                }
            }
        }
        return null;
    }

    /// <summary>Validates a prospective install directory. Returns null when acceptable, otherwise an error message.</summary>
    public static string? Validate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Choose an installation folder.";
        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch
        {
            return "That folder path is not valid.";
        }
        if (string.Equals(full, Path.GetPathRoot(full), StringComparison.OrdinalIgnoreCase))
            return "Installing to a drive root is not supported. Pick a subfolder.";
        if (full.TrimEnd('\\').EndsWith("\\Windows", StringComparison.OrdinalIgnoreCase) ||
            full.StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows)), StringComparison.OrdinalIgnoreCase))
            return "The Windows folder is not a valid installation target.";
        try
        {
            Directory.CreateDirectory(full);
            string probe = Path.Combine(full, $".multibox-write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "x");
            File.Delete(probe);
        }
        catch (UnauthorizedAccessException)
        {
            return "No permission to write there. Try a folder in your user profile, or install for all users.";
        }
        catch (Exception ex)
        {
            return $"Cannot write there: {ex.Message}";
        }
        return null;
    }

    public static long FreeBytes(string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            if (root is null) return -1;
            var drive = new DriveInfo(root);
            return drive.IsReady ? drive.AvailableFreeSpace : -1;
        }
        catch
        {
            return -1;
        }
    }
}
