using System.Diagnostics;

namespace MultiBox.Core;

public sealed record SessionView(string Id, string AppName, string AppPath, int Instance, string State,
    bool IsBusy, int Pid, double RamMb, double CpuPercent, string Elapsed, string VaultPath,
    bool HasSnapshot, bool FromSavedProfile, string? Error, ProfileCheck? ProfileCheck = null, string? SavedProfileId = null, string? ProfileName = null,
    string? DisplayName = null, string Color = "lime")
{
    public string DisplayLabel => string.IsNullOrEmpty(DisplayName)
        ? $"{System.IO.Path.GetFileNameWithoutExtension(AppName)} #{Instance}" + (ProfileName == null ? "" : $" · {ProfileName}")
        : $"{DisplayName} · {System.IO.Path.GetFileNameWithoutExtension(AppName)} #{Instance}";
}
public sealed record SessionWorkspace(long Revision, string? SelectedId, IReadOnlyList<SessionView> Sessions);

/// <summary>Owns independent per-instance engines. No operation follows a changing selection.</summary>
public sealed class SessionManager : IDisposable
{
    private sealed class Entry(string path, int instance, bool fromSaved, string? profileId, string? profileName)
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public readonly string Path = path;
        public readonly string Name = System.IO.Path.GetFileName(path);
        public readonly int Instance = instance;
        public readonly bool FromSaved = fromSaved;
        public readonly string? ProfileId = profileId;
        public readonly string? ProfileName = profileName;
        public readonly IsolationEngine Engine = new();
        public readonly object Gate = new();
        public string State = "loading";
        public bool Busy = true;
        public IsolationTelemetry? Telemetry;
        public string VaultPath = "";
        public string? Error;
        public string? DisplayName;
        public string Color = "lime";
    }
    private readonly object _sync = new();
    private readonly List<Entry> _entries = [];
    private readonly Dictionary<string, int> _instances = new(StringComparer.OrdinalIgnoreCase);
    private string? _selected;
    private long _revision;
    private bool _disposed;
    public event Action<SessionWorkspace>? Changed;
    public string? SelectedId { get { lock (_sync) return _selected; } }
    public SessionWorkspace Snapshot() { lock (_sync) return BuildSnapshot(); }
    public IsolationEngine? GetEngine(string? id) { lock (_sync) return _entries.FirstOrDefault(e => e.Id == id)?.Engine; }

    public SessionManager()
    {
        SaveBoxManager.ProfileDeleting += OnProfileDeleting;
    }

    private void OnProfileDeleting(string profileId)
    {
        Entry[] matching;
        lock (_sync)
        {
            matching = _entries.Where(e => e.ProfileId == profileId).ToArray();
        }
        foreach (var entry in matching)
        {
            lock (entry.Gate)
            {
                try { entry.Engine.TerminateAndWipe(); } catch { }
                lock (_sync) Remove(entry);
            }
        }
        Publish();
    }

    public bool LaunchNew(string rawPath, bool restoreSaved, out string error, string? profileId = null)
    {
        error = "";
        string path;
        try { path = ShortcutResolver.Resolve(rawPath); }
        catch (Exception ex) { error = ex.Message; return false; }
        if (!File.Exists(path)) { error = "The app executable could not be found. Existing sessions were kept."; return false; }
        if (restoreSaved && !SaveBoxManager.HasSnapshot(System.IO.Path.GetFileName(path)))
        { error = "The saved profile could not be found. Existing sessions were kept."; return false; }
        var savedProfile = profileId != null ? SaveBoxManager.GetProfile(profileId) : null;
        if (restoreSaved && profileId != null && (savedProfile == null || !savedProfile.AppPath.Equals(path, StringComparison.OrdinalIgnoreCase)))
        { error = "That saved profile is missing or belongs to a different app. Existing sessions were kept."; return false; }
        Entry entry;
        lock (_sync)
        {
            if (_disposed) { error = "MultiBox is closing."; return false; }
            string appKey = System.IO.Path.GetFileName(path);
            _instances.TryGetValue(appKey, out int number);
            _instances[appKey] = ++number;
            entry = new Entry(path, number, restoreSaved, profileId, restoreSaved ? savedProfile?.ProfileName ?? "Default" : null);
            entry.Engine.TelemetryUpdated += telemetry => UpdateTelemetry(entry, telemetry);
            entry.Engine.SessionPaused += _ => Paused(entry);
            _entries.Add(entry);
            _selected = entry.Id;
        }
        Publish();
        bool success;
        lock (entry.Gate)
        {
            lock (_sync) { if (_disposed) { error = "MultiBox is closing."; return false; } }
            success = entry.Engine.LaunchIsolated(path, out error, restoreSaved, profileId);
            string state = entry.Engine.State == SessionState.Running ? "running" : "preserved";
            string vault = entry.Engine.CurrentVaultRootPath ?? "";
            lock (_sync)
            {
                entry.Busy = false;
                entry.State = state;
                entry.VaultPath = vault;
                if (!success) Remove(entry);
            }
        }
        Publish();
        return success;
    }
    public bool Select(string id)
    {
        lock (_sync)
        {
            if (!_entries.Any(e => e.Id == id)) return false;
            _selected = id;
        }
        Publish();
        return true;
    }
    public bool Customize(string id, string? displayName, string? color, out string error)
    {
        string name = (displayName ?? "").Trim();
        error = "";
        if (name.Length > 32 || name.Any(char.IsControl)) { error = "Use a name of up to 32 characters."; return false; }
        if (color == null || !SessionIdentity.Colors.ContainsKey(color)) { error = "Choose one of the session colors."; return false; }
        lock (_sync)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (_disposed || entry == null) { error = "That session has already ended."; return false; }
            if (entry.Busy) { error = "Wait for this session's current operation to finish."; return false; }
            entry.DisplayName = name.Length == 0 ? null : name;
            entry.Color = color;
        }
        Publish();
        return true;
    }
    public bool Duplicate(string id, out string error)
    {
        string? path;
        lock (_sync) path = _entries.FirstOrDefault(e => e.Id == id)?.Path;
        if (path == null) { error = "That session has already ended."; return false; }
        return LaunchNew(path, false, out error);
    }
    public bool Resume(string id, out string error)
    {
        if (!Begin(id, "loading", out var entry, out error)) return false;
        bool result;
        lock (entry!.Gate)
        {
            if (entry.Engine.IsRunning) { error = "The app is already running."; result = false; }
            else result = entry.Engine.LaunchIsolated(entry.Path, out error);
            lock (_sync)
            {
                entry.Busy = false;
                entry.State = entry.Engine.IsRunning ? "running" : "preserved";
                entry.Error = result ? null : error;
            }
        }
        Publish();
        return result;
    }
    public bool Save(string id, out string error)
    {
        if (!Begin(id, null, out var entry, out error)) return false;
        bool result;
        lock (entry!.Gate)
        {
            result = entry.Engine.SaveCurrentSessionToSaveBox(out error);
            lock (_sync) { entry.Busy = false; entry.Error = result ? null : error; }
        }
        Publish();
        return result;
    }
    public bool SaveProfile(string id, string name, string? replaceProfileId, out string error)
    {
        if (!Begin(id, null, out var entry, out error)) return false;
        bool result;
        lock (entry!.Gate)
        {
            result = entry.Engine.SaveNamedProfile(name, replaceProfileId, out error);
            lock (_sync)
            {
                entry.Busy = false;
                entry.Error = result ? null : error;
                if (result && replaceProfileId != null)
                {
                    foreach (var other in _entries.Where(e => e.Id != id && e.ProfileId == replaceProfileId))
                    {
                        other.Engine.UnlinkProfile(replaceProfileId);
                    }
                }
            }
        }
        Publish();
        return result;
    }
    public ProfileSaveOutcome SaveProfileAndReopen(string id, string name, string? replaceProfileId, Action<string>? progress = null)
    {
        if (!Begin(id, null, out var entry, out string error)) return new(false, false, error);
        ProfileSaveOutcome result;
        lock (entry!.Gate)
        {
            try { result = entry.Engine.SaveProfileAndReopen(name, replaceProfileId, progress); }
            catch (Exception) { result = new(false, false, "Save could not complete. Session files were kept."); }
            finally
            {
                lock (_sync)
                {
                    entry.Busy = false;
                    entry.State = entry.Engine.IsRunning ? "running" : "preserved";
                    entry.Telemetry = null;
                }
            }
            lock (_sync) entry.Error = result.Saved ? (result.Warning.Length > 0 ? result.Warning : null) : result.Error;
        }
        Publish();
        return result;
    }
    public bool Wipe(string id, out string error)
    {
        if (!Begin(id, "wiping", out var entry, out error)) return false;
        bool result = false;
        lock (entry!.Gate)
        {
            try
            {
                entry.Engine.TerminateAndWipe();
                lock (_sync) Remove(entry);
                result = true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                lock (_sync) { entry.Busy = false; entry.State = "preserved"; entry.Error = error; }
            }
        }
        Publish();
        return result;
    }
    public bool WipeMany(IEnumerable<string> ids, out string error)
    {
        var failures = new List<string>();
        // Capture IDs before beginning; a session launched later is never included.
        foreach (string id in ids.Distinct().ToArray())
            if (!Wipe(id, out var message)) failures.Add(message);
        error = string.Join(" ", failures);
        return failures.Count == 0;
    }
    private bool Begin(string id, string? state, out Entry? entry, out string error)
    {
        error = "";
        lock (_sync)
        {
            entry = _entries.FirstOrDefault(e => e.Id == id);
            if (_disposed || entry == null) { error = "That session has already ended."; return false; }
            if (entry.Busy) { error = "This session is busy. Wait for its current operation to finish."; return false; }
            entry.Busy = true;
            entry.Error = null;
            if (state != null) entry.State = state;
        }
        Publish();
        return true;
    }
    private void UpdateTelemetry(Entry entry, IsolationTelemetry telemetry)
    {
        lock (_sync)
        {
            if (_disposed || !_entries.Contains(entry)) return;
            entry.Telemetry = telemetry;
            entry.VaultPath = telemetry.VaultPath;
            if (!entry.Busy) entry.State = "running";
        }
        Publish();
    }
    private void Paused(Entry entry)
    {
        if (entry.Engine.State != SessionState.Preserved) return;
        lock (_sync)
        {
            if (_disposed || !_entries.Contains(entry) || entry.Busy) return;
            entry.State = "preserved";
        }
        Publish();
    }
    private void Remove(Entry entry)
    {
        _entries.Remove(entry);
        if (_selected == entry.Id) _selected = _entries.LastOrDefault()?.Id;
    }
    private SessionWorkspace BuildSnapshot() => new(_revision, _selected, _entries.Select(entry =>
    {
        var t = entry.Telemetry;
        bool running = entry.State == "running";
        return new SessionView(entry.Id, entry.Name, entry.Path, entry.Instance, entry.State, entry.Busy,
            running ? t?.ProcessId ?? 0 : 0, running ? Math.Round((t?.MemoryBytes ?? 0) / 1048576.0, 1) : 0,
            running ? t?.CpuPercent ?? 0 : 0, t?.Elapsed.ToString(@"hh\:mm\:ss") ?? "00:00:00",
            entry.VaultPath, SaveBoxManager.HasSnapshot(entry.Name), entry.FromSaved, entry.Error, entry.Engine.ProfileCheck, entry.ProfileId, entry.ProfileName, entry.DisplayName, entry.Color);
    }).ToArray());
    private void Publish()
    {
        SessionWorkspace snapshot;
        lock (_sync) { if (_disposed) return; _revision++; snapshot = BuildSnapshot(); }
        Changed?.Invoke(snapshot);
    }
    public void Dispose()
    {
        SaveBoxManager.ProfileDeleting -= OnProfileDeleting;
        Entry[] entries;
        lock (_sync) { if (_disposed) return; _disposed = true; entries = _entries.ToArray(); }
        foreach (var entry in entries)
            lock (entry.Gate) { try { entry.Engine.Dispose(); } catch { /* Job is closed even if files need a later retry. */ } }
        lock (_sync) { _entries.Clear(); _selected = null; }
    }
}
