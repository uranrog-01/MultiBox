namespace MultiBox.Core;

public sealed record ProfileCheck(string Status, string Label, string Detail, string Method, string ProfilePath);

/// <summary>Observes post-launch writes, not the existence of provisioned or restored files.</summary>
internal sealed class ProfileActivityMonitor : IDisposable
{
    private readonly string _path;
    private readonly string _method;
    private readonly bool _explicitProfile;
    private readonly DateTime _started = DateTime.UtcNow;
    private FileSystemWatcher? _watcher;
    private int _writes, _failed;
    public ProfileActivityMonitor(string path, string method, bool explicitProfile)
    {
        _path = path;
        _method = method;
        _explicitProfile = explicitProfile;
        try
        {
            _watcher = new FileSystemWatcher(path) {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                InternalBufferSize = 16384
            };
            _watcher.Created += Activity;
            _watcher.Changed += Activity;
            _watcher.Renamed += (_, e) => Activity(null, e);
            _watcher.Error += (_, _) => Interlocked.Exchange(ref _failed, 1);
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { _failed = 1; }
    }
    private void Activity(object? sender, FileSystemEventArgs e)
    {
        // Creating an empty directory alone is not evidence that the app used the profile.
        if (File.Exists(e.FullPath)) Interlocked.Increment(ref _writes);
    }
    public ProfileCheck Read()
    {
        if (Volatile.Read(ref _failed) != 0)
            return new("unverified", "Profile unverified", "Profile activity could not be monitored. A successful launch does not confirm profile separation.", _method, _path);
        bool writes = Volatile.Read(ref _writes) > 0;
        if (_explicitProfile && writes)
            return new("observed", "Profile activity confirmed", "An app-specific profile path was requested and new file activity was observed there. This checks profile storage, not all filesystem or network access.", _method, _path);
        if (!_explicitProfile)
            return new("unverified", "Profile unverified", writes
                ? "File activity was observed in the session, but this app has no recognized profile option. Complete profile separation has not been verified."
                : "Only profile environment variables were redirected. This app may store data elsewhere; profile separation has not been verified.", _method, _path);
        return DateTime.UtcNow - _started < TimeSpan.FromSeconds(15)
            ? new("checking", "Checking profile…", "A separate profile path was requested. Waiting for new file activity; pre-existing saved files do not count.", _method, _path)
            : new("unverified", "Profile unverified", "No new file activity has been observed in the requested profile folder. The app may be idle or may store data elsewhere.", _method, _path);
    }
    public void Dispose() => _watcher?.Dispose();
}
