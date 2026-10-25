using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MultiBox.Core;

public sealed record HealCandidate(string ExePath, string Source, string AppLabel, int Score)
{
    public override string ToString() => $"{AppLabel} · {ExePath} ({Source})";
}

/// <summary>
/// Hunts a missing application across Windows' own app inventory so a broken saved profile can be
/// repaired: running processes, Start Menu shortcuts, the App Paths and installed-apps registry keys
/// (whose DisplayName and DisplayIcon still find executables that were renamed), Store packages, and
/// a budgeted search of common install folders.
/// </summary>
public static class ProfileHealer
{
    public enum Depth { Quick, Deep }

    public static async Task<HealCandidate[]> HuntAsync(string appName, string? oldPath,
        Depth depth = Depth.Quick, IProgress<string>? progress = null, CancellationToken token = default)
    {
        string key = Path.GetFileNameWithoutExtension(appName);
        if (key.Length == 0 && oldPath != null) key = Path.GetFileNameWithoutExtension(oldPath);
        key = Regex.Replace(key, @"^\d+$", ""); // A folder-number-only name is not an app key.
        if (key.Length == 0) return [];
        return await Task.Run(() =>
        {
            var found = new Dictionary<string, (int Score, string Source, string Label)>(StringComparer.OrdinalIgnoreCase);
            void Add(string? path, int score, string source, string label)
            {
                if (string.IsNullOrWhiteSpace(path) || score <= 0) return;
                path = Path.TrimEndingDirectorySeparator(path.Trim());
                if (!File.Exists(path) || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return;
                if (IsJunk(Path.GetFileName(path))) return;
                if (!found.TryGetValue(path, out var existing) || score > existing.Score)
                    found[path] = (score, source, string.IsNullOrWhiteSpace(label) ? Path.GetFileNameWithoutExtension(path) : label);
            }

            progress?.Report("Checking running apps and shortcuts…");
            if (!token.IsCancellationRequested) AddRunningProcesses(key, Add);
            if (!token.IsCancellationRequested) AddShortcuts(key, Add);

            progress?.Report("Checking the installed-apps registry…");
            if (!token.IsCancellationRequested) AddAppPathsRegistry(key, Add);
            if (!token.IsCancellationRequested) AddUninstallEntries(key, Add);

            progress?.Report("Checking Microsoft Store packages…");
            if (!token.IsCancellationRequested) AddStorePackage(key, Add);

            if (depth == Depth.Deep && !token.IsCancellationRequested)
            {
                foreach (string drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).Select(d => d.Name))
                {
                    if (token.IsCancellationRequested) break;
                    progress?.Report($"Searching {drive}…");
                    BoundedScan(drive, key, Add, token, fileBudget: 150_000, secondsBudget: 90, progress);
                }
            }
            else if (!token.IsCancellationRequested)
            {
                progress?.Report("Searching common install folders…");
                foreach (string root in new[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
                {
                    if (token.IsCancellationRequested) break;
                    BoundedScan(root, key, Add, token, fileBudget: 40_000, secondsBudget: 20, progress);
                }
            }
            return Rank(found);
        }, token);
    }

    internal static HealCandidate[] Rank(Dictionary<string, (int Score, string Source, string Label)> found) =>
        found.Select(kv => new HealCandidate(kv.Key, kv.Value.Source, kv.Value.Label, kv.Value.Score))
            .OrderByDescending(c => c.Score).ThenBy(c => c.ExePath, StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>True when the best match stands clearly alone and is safe to preselect without the user comparing options.</summary>
    internal static bool AutoFixIsSafe(HealCandidate[] ranked) =>
        ranked.Length > 0 && ranked[0].Score >= 50 && (ranked.Length == 1 || ranked[0].Score - ranked[1].Score >= 15);

    /// <summary>3 = equal, 2 = one contains the other, 1 = token overlap, 0 = unrelated.</summary>
    internal static int NameMatch(string key, string? label)
    {
        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(key)) return 0;
        string normalized = Regex.Replace(label.ToLowerInvariant(), "[^a-z0-9]+", "");
        string target = Regex.Replace(key.ToLowerInvariant(), "[^a-z0-9]+", "");
        if (target.Length == 0 || normalized.Length == 0) return 0;
        if (normalized == target) return 3;
        if ((target.Length >= 4 && normalized.Contains(target)) || (normalized.Length >= 4 && target.Contains(normalized))) return 2;
        return Regex.Split(label.ToLowerInvariant(), "[^a-z0-9]+").Any(part => part.Length > 0 && (part == target || (target.Length >= 4 && part.Contains(target)))) ? 1 : 0;
    }

    internal static int BasenameMatch(string basename, string key)
    {
        if (IsJunk(basename)) return 0;
        return NameMatch(key, Path.GetFileNameWithoutExtension(basename)) switch { 3 => 35, 2 => 28, 1 => 20, _ => 0 };
    }

    internal static bool IsJunk(string basename) =>
        Regex.IsMatch(basename, @"(?i)unins|setup|update|eula|readme|licen[cs]e|crashpad|crashreport|reporter|install(?:er)?\.exe$|helper|bootstrap");

    private static void AddRunningProcesses(string key, Action<string?, int, string, string> add)
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id == Environment.ProcessId || process.MainModule?.FileName is not string path) continue;
                    int score = BasenameMatch(Path.GetFileName(path), key);
                    if (score > 0) add(path, score + 15, "Running now", Path.GetFileNameWithoutExtension(path));
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException) { }
            }
        }
    }

    private static void AddShortcuts(string key, Action<string?, int, string, string> add)
    {
        string[] dirs = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Start Menu"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Internet Explorer", "Quick Launch")
        };
        foreach (string dir in dirs.Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d)))
        {
            string[] links;
            try { links = Directory.GetFiles(dir, "*.lnk", SearchOption.AllDirectories); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
            foreach (string link in links.Where(l => NameMatch(key, Path.GetFileNameWithoutExtension(l)) > 0))
            {
                try
                {
                    string target = ShortcutResolver.Resolve(link);
                    if (File.Exists(target) && !target.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                        add(target, 50, "Start Menu or Desktop shortcut", Path.GetFileNameWithoutExtension(link));
                }
                catch { }
            }
        }
    }

    private static void AddAppPathsRegistry(string key, Action<string?, int, string, string> add)
    {
        foreach (RegistryKey? root in new[] {
            Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths"),
            Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths"),
            Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths") })
        {
            if (root == null) continue;
            using (root)
                foreach (string entry in root.GetSubKeyNames())
                {
                    if (NameMatch(key, entry) == 0) continue;
                    using var sub = root.OpenSubKey(entry);
                    add(sub?.GetValue(null) as string, 45, "App Paths registry", Path.GetFileNameWithoutExtension(entry));
                }
        }
    }

    private static void AddUninstallEntries(string key, Action<string?, int, string, string> add)
    {
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
            foreach (string relative in new[] { @"Software\Microsoft\Windows\CurrentVersion\Uninstall", @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" })
                using (var root = hive.OpenSubKey(relative))
                {
                    if (root == null) continue;
                    foreach (string entry in root.GetSubKeyNames())
                    {
                        using var sub = root.OpenSubKey(entry);
                        if (sub == null) continue;
                        string? displayName = sub.GetValue("DisplayName") as string;
                        int similarity = NameMatch(key, displayName);
                        if (similarity == 0) continue;
                        int baseScore = similarity switch { 3 => 40, 2 => 30, _ => 15 };
                        string? icon = (sub.GetValue("DisplayIcon") as string)?.Split(',')[0].Trim().Trim('"');
                        if (!string.IsNullOrWhiteSpace(icon) && NameMatch(key, Path.GetFileName(icon)) > 0 && File.Exists(icon))
                            add(icon, baseScore + 25, "Installed app registry", displayName ?? entry);
                        else if (sub.GetValue("InstallLocation") as string is string location && Directory.Exists(location))
                        {
                            try
                            {
                                string found = Directory.GetFiles(location, key + ".exe", SearchOption.AllDirectories).FirstOrDefault() ?? "";
                                add(found, baseScore + 10, "Installed app registry", displayName ?? entry);
                            }
                            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                        }
                    }
                }
    }

    private static void AddStorePackage(string key, Action<string?, int, string, string> add)
    {
        string? install = ShortcutResolver.QueryPackageInstallLocation(key, null);
        if (install == null || !Directory.Exists(install)) return;
        try
        {
            string found = Directory.GetFiles(install, key + ".exe", SearchOption.AllDirectories).FirstOrDefault() ?? "";
            add(found, 35, "Microsoft Store package", key);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private static void BoundedScan(string root, string key, Action<string?, int, string, string> add,
        CancellationToken token, int fileBudget, int secondsBudget, IProgress<string>? progress)
    {
        if (!Directory.Exists(root)) return;
        var deadline = Stopwatch.StartNew();
        var queue = new Queue<string>();
        queue.Enqueue(root);
        int visited = 0;
        while (queue.Count > 0 && visited < fileBudget && deadline.Elapsed < TimeSpan.FromSeconds(secondsBudget) && !token.IsCancellationRequested)
        {
            string directory = queue.Dequeue();
            string[] subDirectories, files;
            try
            {
                subDirectories = Directory.GetDirectories(directory);
                files = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
            foreach (string file in files)
            {
                visited++;
                string basename = Path.GetFileName(file);
                int score = BasenameMatch(basename, key);
                if (score > 0) add(file, score, "Searched folders", Path.GetFileNameWithoutExtension(basename));
            }
            if (visited > 0 && visited % 5000 == 0) progress?.Report($"Searched {visited:N0} files…");
            foreach (string sub in subDirectories)
            {
                try { if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue; }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
                if (SkipDirectory(Path.GetFileName(sub))) continue;
                queue.Enqueue(sub);
            }
        }
    }

    private static bool SkipDirectory(string name) =>
        Regex.IsMatch(name, @"(?i)^\$recycle\.bin$|^system volume information$|^windows$|^programdata$|^temp$|node_modules|^\.git$|^cache[sc]?$|crashreports");
}
