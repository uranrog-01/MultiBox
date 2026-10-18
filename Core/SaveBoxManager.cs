using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MultiBox.Core;

public class SaveBoxSnapshot
{
    public string ProfileId { get; set; } = string.Empty;
    public string ProfileName { get; set; } = "Default";
    public string AppName { get; set; } = string.Empty;
    public string AppPath { get; set; } = string.Empty;
    public string SavedAt { get; set; } = string.Empty;
    public double SizeMb { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public string[] RegeneratedBrowserMetadata { get; set; } = [];
    public string? OriginalVaultId { get; set; }
    public string? OriginalVaultPath { get; set; }
}

public static class SaveBoxManager
{
    private static readonly object SnapshotLock = new();
    private static SaveBoxSnapshot[]? _cache;
    private static DateTime _cachedAt;
    public static string SaveBoxRoot => Path.Combine(VaultRecovery.DefaultRoot, "SaveBox");
    public static string SaveDiagnosticPath(string appPath) => Path.Combine(Path.GetDirectoryName(VaultRecovery.DefaultRoot)!, "SaveDiagnostics",
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(appPath.ToUpperInvariant()))) + ".json");
    public static List<SaveBoxSnapshot> ListSnapshots(bool refresh = false)
    {
        lock (SnapshotLock)
        {
            if (!refresh && _cache != null && DateTime.UtcNow - _cachedAt < TimeSpan.FromSeconds(2)) return _cache.ToList();
            RecoverInterruptedSaves();
            var list = new List<SaveBoxSnapshot>();
            if (Directory.Exists(SaveBoxRoot))
                foreach (string folder in Directory.GetDirectories(SaveBoxRoot))
                {
                    try
                    {
                        string name = Path.GetFileName(folder);
                        if (name.StartsWith(".multibox-", StringComparison.Ordinal) || (File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0) continue;
                        if (!Directory.Exists(Path.Combine(folder, "Data"))) continue;
                        string manifest = Path.Combine(folder, "manifest.json");
                        var profile = File.Exists(manifest) ? JsonSerializer.Deserialize<SaveBoxSnapshot>(File.ReadAllText(manifest)) : null;
                        profile ??= new SaveBoxSnapshot { AppName = name, SavedAt = Directory.GetLastWriteTime(folder).ToString("MMM dd, yyyy • h:mm tt") };
                        if (string.IsNullOrWhiteSpace(profile.ProfileId))
                            profile.ProfileId = "legacy-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name.ToUpperInvariant())))[..32].ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(profile.ProfileName)) profile.ProfileName = "Default";
                        if (string.IsNullOrWhiteSpace(profile.AppName)) profile.AppName = name;
                        profile.FolderPath = folder; // Never trust a manifest's path for deletion or restore.
                        list.Add(profile);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
                }
            _cache = list.OrderBy(p => p.AppName, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.ProfileName, StringComparer.OrdinalIgnoreCase).ToArray();
            _cachedAt = DateTime.UtcNow;
            return _cache.ToList();
        }
    }

    private static void RecoverInterruptedSaves()
    {
        // The upgrade helper saves in another process while the main app keeps listing profiles.
        // An active staging directory is not an interrupted save.
        using var procMutex = new Mutex(false, @"Local\MultiBox_SaveBox_Mutex");
        bool acquired = false;
        try
        {
            try { acquired = procMutex.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) return;
            if (!Directory.Exists(SaveBoxRoot)) return;
            foreach (string folder in Directory.GetDirectories(SaveBoxRoot))
            {
                string name = Path.GetFileName(folder);
                if (name.StartsWith(".multibox-backup-", StringComparison.Ordinal))
                {
                    string manifest = Path.Combine(folder, "manifest.json");
                    if (File.Exists(manifest))
                    {
                        try
                        {
                            var profile = JsonSerializer.Deserialize<SaveBoxSnapshot>(File.ReadAllText(manifest));
                            if (profile != null && !string.IsNullOrWhiteSpace(profile.ProfileId))
                            {
                                string target = Path.Combine(SaveBoxRoot, profile.ProfileId);
                                if (!Directory.Exists(target))
                                {
                                    Directory.Move(folder, target);
                                    continue;
                                }
                            }
                        }
                        catch { }
                    }
                    try { EphemeralVault.DeleteTree(folder); } catch { }
                }
                else if (name.StartsWith(".multibox-stage-", StringComparison.Ordinal))
                {
                    try { EphemeralVault.DeleteTree(folder); } catch { }
                }
            }
        }
        catch { }
        finally { if (acquired) procMutex.ReleaseMutex(); }
    }
    public static SaveBoxSnapshot? GetProfile(string? profileId) => profileId == null ? null : ListSnapshots().FirstOrDefault(p => p.ProfileId == profileId);
    public static bool HasSnapshot(string appName) => ListSnapshots().Any(p => p.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase));
    internal static SaveBoxSnapshot? DefaultProfile(string appName) => ListSnapshots().FirstOrDefault(p => p.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase) && p.ProfileName.Equals("Default", StringComparison.OrdinalIgnoreCase));
    public static bool SaveSnapshot(string appName, string appPath, string vault, out string error)
        => SaveProfile(appName, appPath, vault, "Default", DefaultProfile(appName)?.ProfileId, out error);
    public static bool RestoreSnapshot(string appName, string vault) => RestoreProfile(DefaultProfile(appName)?.ProfileId, vault);
    public static bool DeleteSnapshot(string appName) => DeleteProfile(DefaultProfile(appName)?.ProfileId);

    internal static bool CanSaveProfile(string appPath, string vault, string profileName, string? replaceProfileId, out string error)
    {
        lock (SnapshotLock)
        {
            try { ValidateSaveTarget(appPath, vault, profileName.Trim(), replaceProfileId); error = ""; return true; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { error = ex.Message; return false; }
        }
    }
    private static SaveBoxSnapshot? ValidateSaveTarget(string appPath, string vault, string name, string? replaceId)
    {
        if (name.Length is < 1 or > 40 || name.Any(char.IsControl)) throw new IOException("Use a profile name of 1–40 characters.");
        if (!Directory.Exists(vault)) throw new IOException("No session files are available to save.");
        var profiles = ListSnapshots(true);
        var replacing = replaceId == null ? null : profiles.FirstOrDefault(p => p.ProfileId == replaceId);
        if (replaceId != null && (replacing == null || !replacing.AppPath.Equals(appPath, StringComparison.OrdinalIgnoreCase)))
            throw new IOException("That saved profile is missing or belongs to a different app. Create a new profile instead.");
        if (profiles.Any(p => p.AppPath.Equals(appPath, StringComparison.OrdinalIgnoreCase) && p.ProfileName.Equals(name, StringComparison.OrdinalIgnoreCase) && p.ProfileId != replaceId))
            throw new IOException("This app already has a profile with that name. Choose Replace or use another name.");
        return replacing;
    }
    public static bool SaveProfile(string appName, string appPath, string vault, string profileName, string? replaceProfileId, out string error, bool sourceClosed = false)
    {
        lock (SnapshotLock)
        {
            using var procMutex = new Mutex(false, @"Local\MultiBox_SaveBox_Mutex");
            bool acquired = false;
            try { acquired = procMutex.WaitOne(TimeSpan.FromSeconds(5)); } catch (AbandonedMutexException) { acquired = true; }
            error = "";
            string? staging = null, backup = null;
            try
            {
                if (!acquired) throw new IOException("Another profile save is still running. Try Save again when it finishes; your app is still open.");
                profileName = profileName.Trim();
                var replacing = ValidateSaveTarget(appPath, vault, profileName, replaceProfileId);
                Directory.CreateDirectory(SaveBoxRoot);
                string id = replacing?.ProfileId ?? Guid.NewGuid().ToString("N");
                string target = replacing?.FolderPath ?? Path.Combine(SaveBoxRoot, id);
                staging = Path.Combine(SaveBoxRoot, ".multibox-stage-" + Guid.NewGuid().ToString("N"));
                string data = Path.Combine(staging, "Data");
                LiveProfileSnapshot.CopyResult copied;
                try { copied = LiveProfileSnapshot.Copy(vault, data, appName, sourceClosed); }
                catch (LiveProfileSnapshot.CopyFailedException ex)
                {
                    try
                    {
                        string diagnostic = SaveDiagnosticPath(appPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(diagnostic)!);
                        // Only relative paths and error categories, never file contents or sign-in values.
                        File.WriteAllText(diagnostic, JsonSerializer.Serialize(new { RecordedAtUtc = DateTime.UtcNow, AppName = appName, BlockedFiles = ex.Files }));
                    }
                    catch (Exception diagnosticError) when (diagnosticError is IOException or UnauthorizedAccessException) { }
                    throw new IOException(CopyError(ex), ex);
                }
                string finalData = Path.Combine(target, "Data");
                try { PathMigrationHelper.MigrateVaultPaths(data, vault, finalData); } catch { }
                var profile = new SaveBoxSnapshot {
                    ProfileId = id, ProfileName = profileName, AppName = appName, AppPath = appPath, FolderPath = target,
                    SavedAt = DateTime.Now.ToString("MMM dd, yyyy • h:mm tt"), SizeMb = Math.Round(copied.Bytes / 1048576.0, 2),
                    RegeneratedBrowserMetadata = copied.RegeneratedBrowserMetadata,
                    OriginalVaultId = Path.GetFileName(vault.TrimEnd('\\', '/')),
                    OriginalVaultPath = vault
                };
                File.WriteAllText(Path.Combine(staging, "manifest.json"), JsonSerializer.Serialize(profile));
                if (Directory.Exists(target))
                {
                    backup = Path.Combine(SaveBoxRoot, ".multibox-backup-" + Guid.NewGuid().ToString("N"));
                    Directory.Move(target, backup);
                }
                try { Directory.Move(staging, target); }
                catch { if (backup != null && !Directory.Exists(target)) Directory.Move(backup, target); throw; }
                _cache = null;
                if (backup != null) { try { EphemeralVault.DeleteTree(backup); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { error = ex.Message; return false; }
            finally
            {
                if (staging != null) { try { EphemeralVault.DeleteTree(staging); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
                if (acquired) { try { procMutex.ReleaseMutex(); } catch { } }
            }
        }
    }
    public static void SanitizeRestoredProfile(string targetVault)
    {
        try
        {
            if (!Directory.Exists(targetVault)) return;
            var dirInfo = new DirectoryInfo(targetVault);

            // 1. Remove all Secure Preferences files in the restored vault so Chromium
            // regenerates valid HMACs for the new vault path without error dialogs.
            foreach (var file in dirInfo.EnumerateFiles("Secure Preferences", SearchOption.AllDirectories))
            {
                try { file.Delete(); } catch { }
            }

            // 2. Remove regenerable Chromium browser cache databases (Web Data, Account Web Data)
            // and stale runtime locks that frequently cause ProfileErrorType::DB_CORRUPT or locking on path relocation.
            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                string name = file.Name.ToLowerInvariant();
                if (name is "web data" or "web data-journal" or "web data-wal" or "web data-shm"
                         or "account web data" or "account web data-journal" or "account web data-wal" or "account web data-shm"
                         or "singletonlock" or "singletoncookie" or "singletonsocket" or "devtoolsactiveport")
                {
                    try { file.Delete(); } catch { }
                }
            }
        }
        catch { }
    }

    public static void PrepareProfileForLaunch(SaveBoxSnapshot profile)
    {
        try
        {
            string dataPath = Path.Combine(profile.FolderPath, "Data");
            Directory.CreateDirectory(dataPath);
            if (!string.IsNullOrWhiteSpace(profile.OriginalVaultPath))
            {
                PathMigrationHelper.MigrateVaultPaths(dataPath, profile.OriginalVaultPath, dataPath);
            }
            PathMigrationHelper.MigrateAnyOldVaultPaths(dataPath, dataPath);
            SanitizeRestoredProfile(dataPath);
        }
        catch { }
    }
    public static bool RestoreProfile(string? profileId, string targetVault)
    {
        lock (SnapshotLock)
        {
            var profile = GetProfile(profileId);
            if (profile == null) return false;
            try 
            { 
                CopyDirectory(Path.Combine(profile.FolderPath, "Data"), targetVault); 
                SanitizeRestoredProfile(targetVault);
                return true; 
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
        }
    }
    public static event Action<string>? ProfileDeleting;
    public static bool DeleteProfile(string? profileId)
    {
        lock (SnapshotLock)
        {
            using var procMutex = new Mutex(false, @"Local\MultiBox_SaveBox_Mutex");
            bool acquired = false;
            try { acquired = procMutex.WaitOne(TimeSpan.FromSeconds(5)); } catch (AbandonedMutexException) { acquired = true; }
            try
            {
                if (!acquired) return false;
                var profile = GetProfile(profileId);
                if (profile == null) return false;
                try { ProfileDeleting?.Invoke(profile.ProfileId); } catch { }
                try { EphemeralVault.DeleteTree(profile.FolderPath); return !Directory.Exists(profile.FolderPath); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
                finally { _cache = null; }
            }
            finally
            {
                if (acquired) { try { procMutex.ReleaseMutex(); } catch { } }
            }
        }
    }
    private static string CopyError(Exception ex)
    {
        if (ex.Message.Contains("your app is still open", StringComparison.OrdinalIgnoreCase)) return ex.Message;
        string reason = ex switch {
            FileNotFoundException or DirectoryNotFoundException => "The app changed or removed a file while saving.",
            UnauthorizedAccessException => "Windows denied access to a profile file.",
            IOException io when (io.HResult & 0xffff) is 32 or 33 => "Some profile files are still in use.",
            PathTooLongException => "A profile file path is too long to copy.",
            _ => !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message.TrimEnd('.') : "The app profile could not be copied"
        };
        return reason + ". Your app is still open. Try Save again in a moment. Existing saved profiles were kept.";
    }
    internal static bool ExcludeGeneratedFolder(string relative)
    {
        string path = relative.Replace('\\', '/');
        if (path.Equals("User/AppData/Local/Temp", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("User/.cache/codex-runtimes", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("User/.codex/.tmp", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("User/.codex/tmp", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("User/.codex/cache", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("User/.codex/.sandbox", StringComparison.OrdinalIgnoreCase)) return true;
        // Only generated installer folders under known app-owned branches, never arbitrary user folders.
        string[] roots = ["User/.codex/", "User/AppData/Roaming/Codex/", "User/AppData/Roaming/ChatGPT/", "User/AppData/Local/OpenAI/"];
        return roots.Any(root => path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) &&
            path.Contains("/runtimes/codex-runtime-install-", StringComparison.OrdinalIgnoreCase);
    }
    private static long CopyDirectory(string source, string destination, string? sourceRoot = null)
    {
        sourceRoot ??= source;
        var dir = new DirectoryInfo(source);
        if (!dir.Exists) throw new DirectoryNotFoundException(source);
        if ((dir.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Profiles with linked folders cannot be copied.");
        Directory.CreateDirectory(destination);
        long copied = 0;
        foreach (var file in dir.GetFiles())
        {
            // Ownership is local to a live vault and must never enter a saved profile.
            if (file.Name is VaultRecovery.OwnerFile or VaultRecovery.LeaseFile or "Secure Preferences") continue;
            // Never copy stale OS shared memory lock files
            if (file.Name.EndsWith("-shm", StringComparison.OrdinalIgnoreCase)) continue;
            if ((file.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Profiles with linked files cannot be copied.");
            using var input = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var output = new FileStream(Path.Combine(destination, file.Name), FileMode.Create, FileAccess.Write, FileShare.Read);
            input.CopyTo(output);
            copied += output.Length;
        }
        foreach (var child in dir.GetDirectories())
        {
            if (ExcludeGeneratedFolder(Path.GetRelativePath(sourceRoot, child.FullName))) continue;
            copied += CopyDirectory(child.FullName, Path.Combine(destination, child.Name), sourceRoot);
        }
        return copied;
    }

    public static void SyncVaultToProfile(string profileId, string vaultPath)
    {
        lock (SnapshotLock)
        {
            try
            {
                var profile = GetProfile(profileId);
                if (profile == null) return;
                string destData = Path.Combine(profile.FolderPath, "Data");
                if (!Directory.Exists(destData)) Directory.CreateDirectory(destData);

                SyncDirectories(vaultPath, destData, vaultPath);

                PathMigrationHelper.MigrateVaultPaths(destData, vaultPath, destData);

                profile.SavedAt = DateTime.Now.ToString("MMM dd, yyyy • h:mm tt");
                profile.SizeMb = CalculateDirectorySizeMb(destData);
                File.WriteAllText(Path.Combine(profile.FolderPath, "manifest.json"), JsonSerializer.Serialize(profile));
                _cache = null;
            }
            catch { }
        }
    }

    private static double CalculateDirectorySizeMb(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return 0;
            long bytes = 0;
            var dir = new DirectoryInfo(path);
            foreach (var f in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if ((f.Attributes & FileAttributes.ReparsePoint) == 0) bytes += f.Length;
            }
            return Math.Round(bytes / 1048576.0, 2);
        }
        catch { return 0; }
    }

    private static void SyncDirectories(string source, string destination, string? sourceRoot = null)
    {
        sourceRoot ??= source;
        var dir = new DirectoryInfo(source);
        if (!dir.Exists) return;
        if ((dir.Attributes & FileAttributes.ReparsePoint) != 0) return;
        Directory.CreateDirectory(destination);

        foreach (var file in dir.GetFiles())
        {
            if (file.Name is VaultRecovery.OwnerFile or VaultRecovery.LeaseFile or "Secure Preferences") continue;
            if ((file.Attributes & FileAttributes.ReparsePoint) != 0) continue;
            // Never copy stale OS shared memory lock files
            if (file.Name.EndsWith("-shm", StringComparison.OrdinalIgnoreCase)) continue;
            string targetPath = Path.Combine(destination, file.Name);
            try
            {
                if (!File.Exists(targetPath) || file.LastWriteTimeUtc > File.GetLastWriteTimeUtc(targetPath) || file.Length != new FileInfo(targetPath).Length)
                {
                    using var input = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    input.CopyTo(output);
                    output.Flush();
                }
                File.SetLastWriteTimeUtc(targetPath, file.LastWriteTimeUtc);
            }
            catch { }
        }

        // Prune files that were removed from the live vault
        try
        {
            foreach (var destFile in new DirectoryInfo(destination).GetFiles())
            {
                if (destFile.Name is "manifest.json" or VaultRecovery.OwnerFile or VaultRecovery.LeaseFile) continue;
                string srcFile = Path.Combine(source, destFile.Name);
                if (!File.Exists(srcFile))
                {
                    try { destFile.Delete(); } catch { }
                }
            }
        }
        catch { }

        foreach (var child in dir.GetDirectories())
        {
            if (ExcludeGeneratedFolder(Path.GetRelativePath(sourceRoot, child.FullName))) continue;
            SyncDirectories(child.FullName, Path.Combine(destination, child.Name), sourceRoot);
        }

        // Prune directories that were removed from the live vault
        try
        {
            foreach (var destSubDir in new DirectoryInfo(destination).GetDirectories())
            {
                string srcSubDir = Path.Combine(source, destSubDir.Name);
                if (!Directory.Exists(srcSubDir))
                {
                    try { EphemeralVault.DeleteTree(destSubDir.FullName); } catch { }
                }
            }
        }
        catch { }
    }
}
