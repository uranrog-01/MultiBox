using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MultiBox.Core;

internal sealed record VaultOwner(int Version, string VaultId, int ProcessId, long ProcessStartUtcTicks);
public sealed record RecoveryReport(int Removed, int ActiveKept, int UnknownKept, int Failed, string CheckedAtUtc);

public static class VaultRecovery
{
    internal const string OwnerFile = ".multibox-session.json";
    internal const string LeaseFile = ".multibox-owner.lock";
    internal const string ClaimedPrefix = ".multibox-abandoned-";
    public static string DefaultRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiBox", "Vaults");

    public static RecoveryReport Cleanup(string? vaultsRoot = null)
    {
        string root = Path.GetFullPath(vaultsRoot ?? DefaultRoot);
        int removed = 0, active = 0, unknown = 0, failed = 0;
        if (!Directory.Exists(root)) return new(0, 0, 0, 0, DateTime.UtcNow.ToString("O"));
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) return new(0, 0, 1, 0, DateTime.UtcNow.ToString("O"));
        string[] folders;
        try { folders = Directory.GetDirectories(root); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return new(0, 0, 0, 1, DateTime.UtcNow.ToString("O")); }
        foreach (string folder in folders)
        {
            string name = Path.GetFileName(folder);
            if (name.Equals("SaveBox", StringComparison.OrdinalIgnoreCase)) continue;
            string id = name.StartsWith(ClaimedPrefix, StringComparison.Ordinal) ? name[ClaimedPrefix.Length..] : name;
            if (!Regex.IsMatch(id, "^[a-f0-9]{12}$")) { unknown++; continue; }
            try
            {
                if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0) { unknown++; continue; }
                string marker = Path.Combine(folder, OwnerFile), lease = Path.Combine(folder, LeaseFile);
                if (!File.Exists(marker) || (File.GetAttributes(marker) & FileAttributes.ReparsePoint) != 0) { unknown++; continue; }
                var owner = JsonSerializer.Deserialize<VaultOwner>(File.ReadAllText(marker));
                if (owner == null || owner.Version != 1 || owner.VaultId != id || owner.ProcessId <= 0 || owner.ProcessStartUtcTicks <= 0) { unknown++; continue; }
                if (OwnerAlive(owner)) { active++; continue; }
                if (!File.Exists(lease) || (File.GetAttributes(lease) & FileAttributes.ReparsePoint) != 0) { unknown++; continue; }
                // The live owner denies this open. Holding the lease also excludes other cleaners.
                using (var held = new FileStream(lease, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete))
                {
                    // Keep ownership metadata until the actual profile data has gone.
                    // Windows can refuse renaming a directory containing our open lease.
                    // Delete in place while that exclusive lease excludes other cleaners.
                    foreach (string item in Directory.EnumerateFileSystemEntries(folder))
                    {
                        if (Path.GetFileName(item) is OwnerFile or LeaseFile) continue;
                        DeleteEntry(item);
                    }
                }
                File.Delete(Path.Combine(folder, LeaseFile));
                File.Delete(Path.Combine(folder, OwnerFile));
                Directory.Delete(folder);
                removed++;
            }
            catch (JsonException) { unknown++; }
            catch (FileNotFoundException) { /* Another cleaner completed the same claim. */ }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { failed++; }
            catch (UnauthorizedAccessException) { failed++; }
        }
        return new(removed, active, unknown, failed, DateTime.UtcNow.ToString("O"));
    }
    private static bool OwnerAlive(VaultOwner owner)
    {
        try
        {
            using var process = Process.GetProcessById(owner.ProcessId);
            return !process.HasExited && process.StartTime.ToUniversalTime().Ticks == owner.ProcessStartUtcTicks;
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return true; } // Cannot prove it is abandoned.
    }
    private static void DeleteEntry(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.Directory) != 0) EphemeralVault.DeleteTree(path);
        else { if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0) File.SetAttributes(path, FileAttributes.Normal); File.Delete(path); }
    }
}
