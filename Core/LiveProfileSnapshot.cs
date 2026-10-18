using Microsoft.Data.Sqlite;

namespace MultiBox.Core;

/// <summary>Copies on-disk profile data without closing, terminating or suspending the app.</summary>
internal static class LiveProfileSnapshot
{
    private static readonly byte[] SqliteHeader = "SQLite format 3\0"u8.ToArray();
    private const FileShare SharedRead = FileShare.ReadWrite | FileShare.Delete;
    private sealed record FileStamp(long Length, DateTime ModifiedUtc);
    internal sealed record CopyResult(long Bytes, string[] RegeneratedBrowserMetadata);
    internal sealed record BlockedFile(string RelativePath, string Reason, int ErrorCode);
    internal sealed class CopyFailedException(string message, BlockedFile[] files) : IOException(message)
    {
        public BlockedFile[] Files { get; } = files;
    }

    public static CopyResult Copy(string source, string destination, string appName, bool sourceClosed = false)
    {
        string name = Path.GetFileNameWithoutExtension(appName);
        string profileName = name.Contains("codex", StringComparison.OrdinalIgnoreCase) || name.Contains("chatgpt", StringComparison.OrdinalIgnoreCase) ? "Codex" : name;
        var regenerated = new List<string>();
        var blocked = new List<BlockedFile>();
        long bytes = CopyDirectory(source, destination, source, "User/AppData/Roaming/" + profileName + "/", regenerated, blocked, sourceClosed);
        if (blocked.Count > 0)
        {
            var files = blocked.DistinctBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray();
            string display = string.Join(", ", files.Take(3).Select(file => {
                string name = Path.GetFileName(file.RelativePath.TrimEnd('/'));
                return "‘" + (name.Length <= 70 ? name : name[..67] + "…") + "’";
            }));
            string more = files.Length > 3 ? $" and {files.Length - 3} more" : "";
            throw new CopyFailedException($"Could not capture {display}{more} yet. Your app is still open. Existing saved copies were kept.", files);
        }
        return new(bytes, regenerated.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static long CopyDirectory(string source, string destination, string root, string profileRoot, List<string> regenerated, List<BlockedFile> blocked, bool sourceClosed)
    {
        var directory = new DirectoryInfo(source);
        if (!directory.Exists)
        {
            if (string.Equals(source, root, StringComparison.OrdinalIgnoreCase)) throw new DirectoryNotFoundException(source);
            return 0;
        }
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) return 0;
        bool levelDb = File.Exists(Path.Combine(source, "CURRENT")) && directory.EnumerateFiles("MANIFEST-*").Any();
        for (int attempt = 0; ; attempt++)
        {
            int blockedBeforeAttempt = blocked.Count;
            try
            {
                Directory.CreateDirectory(destination);
                var before = levelDb ? LevelDbMetadata(source) : null;
                bool chromiumProfile = IsChromiumStorageDirectory(source, root);
                var files = directory.GetFiles().Where(file => {
                    // Privacy history and performance-report queues are separate from app state.
                    // Chromium can hold them exclusively. Exclude only exact DB/sidecar names
                    // in a recognized browser profile, and record the exclusion in the saved manifest.
                    if (!chromiumProfile || !IsRegenerableBrowserFile(file.Name)) return true;
                    regenerated.Add(Path.GetRelativePath(root, file.FullName).Replace('\\', '/'));
                    return false;
                }).ToArray();
                // A finished SQLite online backup contains committed WAL data. Its old sidecars must not be restored over it.
                var databases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var unreadable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in files)
                {
                    if (!File.Exists(file.FullName) || IsMarker(file, root, levelDb) || IsSidecarName(file.Name)) continue;
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    try { if (WithRetry(() => HasSqliteHeader(file.FullName))) databases.Add(file.Name); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        if (File.Exists(file.FullName))
                        {
                            unreadable.Add(file.Name);
                            blocked.Add(FileFailure(root, file.FullName, ex));
                        }
                    }
                }
                long bytes = 0;
                foreach (var file in files.OrderBy(file => file.Name.Equals("CURRENT", StringComparison.OrdinalIgnoreCase) ? 1 : 0))
                {
                    if (!File.Exists(file.FullName) || unreadable.Contains(file.Name) || IsMarker(file, root, levelDb) || IsCapturedSidecar(file.Name, databases)) continue;
                    string target = Path.Combine(destination, file.Name);
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    try
                    {
                        if (databases.Contains(file.Name))
                        {
                            bytes += WithRetry(() => BackupSqlite(file.FullName, target, sourceClosed));
                        }
                        else
                        {
                            bytes += WithRetry(() => CopyFile(file.FullName, target, levelDb && file.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase)));
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
                    {
                        if (File.Exists(file.FullName))
                        {
                            blocked.Add(FileFailure(root, file.FullName, ex));
                        }
                    }
                }
                if (before != null && before != LevelDbMetadata(source))
                    throw new IOException("The app reorganized its profile database while saving. Please try Save again; your app is still open.");
                foreach (var child in directory.GetDirectories())
                {
                    if (!child.Exists || (child.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    string relative = Path.GetRelativePath(root, child.FullName);
                    if (SaveBoxManager.ExcludeGeneratedFolder(relative) || IsBrowserCache(relative, profileRoot)) continue;
                    try { bytes += CopyDirectory(child.FullName, Path.Combine(destination, child.Name), root, profileRoot, regenerated, blocked, sourceClosed); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        if (Directory.Exists(child.FullName))
                        {
                            blocked.Add(FileFailure(root, child.FullName, ex));
                        }
                    }
                }
                return bytes;
            }
            catch (IOException) when (levelDb && attempt < 2)
            {
                // Retry only our private staging directory, never the live source.
                blocked.RemoveRange(blockedBeforeAttempt, blocked.Count - blockedBeforeAttempt);
                EphemeralVault.DeleteTree(destination);
                Thread.Sleep(100);
            }
        }
    }
    private static bool IsMarker(FileInfo file, string root, bool levelDb)
    {
        if (file.Name is VaultRecovery.OwnerFile or VaultRecovery.LeaseFile) return true;
        string relative = Path.GetRelativePath(root, file.FullName).Replace('\\', '/');
        // Codex elects a background SQLite maintenance worker with this OS lock.
        // It is not database content. A restored app must create its own marker.
        // CODEX_HOME is outside AppData, so classify it before the generic AppData guard.
        if (IsCodexMaintenanceLock(file, relative)) return true;
        if (!relative.StartsWith("User/AppData/", StringComparison.OrdinalIgnoreCase)) return false;
        string name = file.Name.ToLowerInvariant();
        return (levelDb && name is "lock" or "log" or "log.old") || 
            name is "singletonlock" or "singletoncookie" or "singletonsocket" or
            "devtoolsactiveport" or "parent.lock" or ".parentlock" or "lockfile" ||
            // Chromium's diagnostic logs are not profile state.
            name is "chrome_debug.log" or "debug.log";
    }
    private static bool IsCodexMaintenanceLock(FileInfo file, string relative)
    {
        if (!file.Name.Equals(".sqlite-maintenance.lock", StringComparison.OrdinalIgnoreCase)) return false;
        int separator = relative.LastIndexOf('/');
        if (separator < 0) return false;
        string parent = relative[..separator];
        string[] homes = ["User/.codex", "User/AppData/Roaming/Codex", "User/AppData/Roaming/ChatGPT", "User/AppData/Local/OpenAI/Codex"];
        if (homes.Any(home => parent.Equals(home, StringComparison.OrdinalIgnoreCase))) return true;
        // Support a nested SQLite home only when it has Codex's database layout.
        // Ordinary project files, including identical names in Documents, remain required data.
        return homes.Any(home => parent.StartsWith(home + "/", StringComparison.OrdinalIgnoreCase)) &&
            file.Directory!.EnumerateFiles("*.sqlite").Any(db => db.Name.StartsWith("state_", StringComparison.OrdinalIgnoreCase) || db.Name.StartsWith("logs_", StringComparison.OrdinalIgnoreCase));
    }
    private static bool IsRegenerableBrowserFile(string name)
    {
        string lower = name.ToLowerInvariant();
        foreach (string suffix in new[] { "-wal", "-shm", "-journal" })
            if (lower.EndsWith(suffix, StringComparison.Ordinal)) { lower = lower[..^suffix.Length]; break; }
        return lower is "dips" or "declarative_performance_observer.db";
    }
    private static bool IsChromiumProfileDirectory(string directory, string root)
    {
        string relative = Path.GetRelativePath(root, directory).Replace('\\', '/');
        if (!relative.StartsWith("User/AppData/Roaming/", StringComparison.OrdinalIgnoreCase) &&
            !relative.StartsWith("User/AppData/Local/", StringComparison.OrdinalIgnoreCase)) return false;
        bool RegularFile(string path) => File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
        // Electron uses its root; Chrome also uses Default/Profile N below a user-data root.
        if (RegularFile(Path.Combine(directory, "Local State"))) return true;
        if (!RegularFile(Path.Combine(directory, "Preferences"))) return false;
        string? parent = Path.GetDirectoryName(directory);
        string name = Path.GetFileName(directory);
        return Directory.Exists(Path.Combine(directory, "Local Storage")) || Directory.Exists(Path.Combine(directory, "Network")) ||
            (parent != null && (name.Equals("Default", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase)) &&
             RegularFile(Path.Combine(parent, "Local State")));
    }
    private static bool IsChromiumStorageDirectory(string directory, string root)
    {
        if (IsChromiumProfileDirectory(directory, root)) return true;
        // Chromium storage partitions inherit the enclosing profile's reporting stores.
        // Accept only the actual partition layouts, not arbitrary nested project folders.
        for (var parent = Directory.GetParent(directory); parent != null && parent.FullName.Length > root.Length; parent = parent.Parent)
        {
            string[] suffix = Path.GetRelativePath(parent.FullName, directory).Replace('\\', '/').Split('/');
            bool partition = suffix.Length == 2 && suffix[0].Equals("Partitions", StringComparison.OrdinalIgnoreCase) ||
                suffix.Length == 4 && suffix[0].Equals("Storage", StringComparison.OrdinalIgnoreCase) && suffix[1].Equals("ext", StringComparison.OrdinalIgnoreCase);
            if (partition && IsChromiumProfileDirectory(parent.FullName, root)) return true;
        }
        return false;
    }
    private static bool IsBrowserCache(string relative, string profileRoot)
    {
        string path = relative.Replace('\\', '/');
        if (!path.StartsWith(profileRoot, StringComparison.OrdinalIgnoreCase)) return false;
        string[] segments = path[profileRoot.Length..].Split('/');
        int index = segments.Length == 1 ? 0 : segments.Length == 2 && (segments[0].Equals("Default", StringComparison.OrdinalIgnoreCase) || segments[0].StartsWith("Profile ", StringComparison.OrdinalIgnoreCase)) ? 1 : -1;
        return index >= 0 && segments[index].ToLowerInvariant() is "cache" or "code cache" or "gpucache" or "dawncache" or "shadercache" or "grshadercache" or "crashpad";
    }
    private static bool IsSidecarName(string name) => name.EndsWith("-wal", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith("-shm", StringComparison.OrdinalIgnoreCase) || name.EndsWith("-journal", StringComparison.OrdinalIgnoreCase);
    private static bool IsCapturedSidecar(string name, HashSet<string> databases)
    {
        foreach (string suffix in new[] { "-wal", "-shm", "-journal" })
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && databases.Contains(name[..^suffix.Length])) return true;
        return false;
    }
    private static FileStream Read(string path) => new(path, FileMode.Open, FileAccess.Read, SharedRead, 81920, FileOptions.SequentialScan);
    private static bool HasSqliteHeader(string path)
    {
        using var input = Read(path);
        Span<byte> header = stackalloc byte[16];
        return input.ReadAtLeast(header, 16, false) == 16 && header.SequenceEqual(SqliteHeader);
    }
    private static long BackupSqlite(string source, string destination, bool sourceClosed)
    {
        // Pooling is off so saving does not keep handles on the running app's profile afterwards.
        SqliteConnection? input = null;
        bool readWrite = sourceClosed && (File.GetAttributes(source) & FileAttributes.ReadOnly) == 0;
        try
        {
            input = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = source, Mode = readWrite ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private, Pooling = false, DefaultTimeout = 3
            }.ToString());
            input.Open();
        }
        catch (SqliteException ex) when (readWrite && ex.SqliteErrorCode is 5 or 6)
        {
            input?.Dispose();
            input = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = source, Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private, Pooling = false, DefaultTimeout = 3
            }.ToString());
            input.Open();
        }
        using (input)
        using (var output = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = destination, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false, DefaultTimeout = 3
        }.ToString()))
        {
            output.Open();
            input.BackupDatabase(output);
            // The saved copy is standalone, rather than a DB + independently copied live WAL.
            using var mode = output.CreateCommand(); mode.CommandText = "PRAGMA journal_mode=DELETE;"; mode.ExecuteScalar();
            output.Close();
            return new FileInfo(destination).Length;
        }
    }
    private static FileStamp Stamp(string path)
    {
        var info = new FileInfo(path); info.Refresh();
        return new(info.Length, info.LastWriteTimeUtc);
    }
    private static long CopyFile(string source, string destination, bool appendOnlyLog)
    {
        var before = Stamp(source);
        using var input = Read(source);
        using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        // Bound append-only logs at the captured EOF instead of chasing an active writer indefinitely.
        long remaining = input.Length;
        byte[] buffer = new byte[81920];
        while (remaining > 0)
        {
            int count = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (count == 0) throw new IOException("A profile file changed while saving.");
            output.Write(buffer, 0, count); remaining -= count;
        }
        if (!appendOnlyLog && before != Stamp(source)) throw new IOException("A profile file changed while saving.");
        return output.Length;
    }
    private static T WithRetry<T>(Func<T> operation)
    {
        for (int attempt = 0; ; attempt++)
        {
            try { return operation(); }
            catch (Exception ex) when (attempt < 2 && (ex is IOException || ex is SqliteException se && se.SqliteErrorCode is 5 or 6))
            { Thread.Sleep(100 * (attempt + 1)); }
        }
    }
    private static string LevelDbMetadata(string directory)
    {
        string current = Path.Combine(directory, "CURRENT");
        if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("Profiles with linked files cannot be copied.");
        using var stream = Read(current); using var reader = new StreamReader(stream);
        string name = reader.ReadToEnd().Trim();
        if (name.Length == 0 || !name.Equals(Path.GetFileName(name), StringComparison.Ordinal) || !name.StartsWith("MANIFEST-", StringComparison.Ordinal))
            throw new IOException("The app's profile database is changing. Try Save again; your app is still open.");
        string manifest = Path.Combine(directory, name);
        if ((File.GetAttributes(manifest) & FileAttributes.ReparsePoint) != 0) throw new IOException("Profiles with linked files cannot be copied.");
        var stamp = Stamp(manifest);
        return name + "|" + stamp.Length + "|" + stamp.ModifiedUtc.Ticks;
    }
    private static BlockedFile FileFailure(string root, string path, Exception error)
    {
        string reason = error switch {
            UnauthorizedAccessException => "Access denied",
            SqliteException sqlite when sqlite.SqliteErrorCode is 5 or 6 => "Database in use",
            SqliteException => "Database backup failed",
            IOException io when (io.HResult & 0xffff) is 32 or 33 => "File in use",
            _ => "File changed or could not be copied"
        };
        int code = error is SqliteException databaseError ? databaseError.SqliteExtendedErrorCode : error.HResult;
        return new(Path.GetRelativePath(root, path).Replace('\\', '/'), reason, code);
    }
}
