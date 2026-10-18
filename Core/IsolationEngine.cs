using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MultiBox.Core
{
    public enum SessionState
    {
        Idle,
        Running,
        Preserved
    }
    public sealed record ProfileSaveOutcome(bool Saved, bool Reopened, string Error, string Warning = "");

    public class IsolationTelemetry
    {
        public string SessionId { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string AppPath { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public long MemoryBytes { get; set; }
        public double CpuPercent { get; set; }
        public TimeSpan Elapsed { get; set; }
        public bool IsRunning { get; set; }
        public string VaultPath { get; set; } = string.Empty;
        public bool HasSnapshot { get; set; }
    }

    public class IsolationEngine : IDisposable
    {
        public event Action<IsolationTelemetry>? TelemetryUpdated;
        public event Action<string>? SessionPaused;
        public event Action? SessionTerminated;

        private JobObject? _jobObject;
        private EphemeralVault? _vault;
        private Process? _process;
        private System.Threading.Timer? _telemetryTimer;
        private DateTime _startTime;
        private TimeSpan _lastCpuTime;
        private DateTime _lastCpuMeasurementTime;
        private readonly object _lock = new();
        private bool _isTerminating;
        private ProfileActivityMonitor? _profileMonitor;
        private string? _linkedProfileId;
        public ProfileCheck ProfileCheck => _profileMonitor?.Read() ?? new("unverified", "Profile unverified", "No app profile activity has been checked yet.", "Not started", "");

        public SessionState State { get; private set; } = SessionState.Idle;
        public bool IsActive => State == SessionState.Running;
        public bool IsRunning => State == SessionState.Running;
        public bool IsPreserved => State == SessionState.Preserved;
        public Process? ActiveProcess => _process;
        public string CurrentAppPath { get; private set; } = string.Empty;
        public string CurrentAppName { get; private set; } = string.Empty;
        public string? CurrentVaultRootPath => _vault?.RootPath;

        public bool HasSavedSnapshot()
        {
            return !string.IsNullOrWhiteSpace(CurrentAppName) && SaveBoxManager.HasSnapshot(CurrentAppName);
        }

        public bool SaveCurrentSessionToSaveBox(out string error)
        {
            error = string.Empty;
            lock (_lock)
            {
                if (_vault == null || string.IsNullOrWhiteSpace(CurrentAppName))
                {
                    error = "No active or preserved session to save.";
                    return false;
                }

                return SaveBoxManager.SaveSnapshot(CurrentAppName, CurrentAppPath, _vault.RootPath, out error);
            }
        }

        public bool SaveNamedProfile(string name, string? replaceProfileId, out string error)
        {
            lock (_lock)
            {
                if (_vault == null) { error = "No session files are available to save."; return false; }
                bool ok = SaveBoxManager.SaveProfile(CurrentAppName, CurrentAppPath, _vault.RootPath, name, replaceProfileId, out error);
                if (ok)
                {
                    var prof = SaveBoxManager.ListSnapshots().FirstOrDefault(p => p.AppPath.Equals(CurrentAppPath, StringComparison.OrdinalIgnoreCase) && p.ProfileName.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (prof != null) _linkedProfileId = prof.ProfileId;
                }
                return ok;
            }
        }

        public ProfileSaveOutcome SaveProfileAndReopen(string name, string? replaceProfileId, Action<string>? progress = null)
        {
            lock (_lock)
            {
                if (_vault == null) return new(false, false, "No session files are available to save.");
                if (!SaveBoxManager.CanSaveProfile(CurrentAppPath, _vault.RootPath, name, replaceProfileId, out string error)) return new(false, false, error);
                bool reopen = (_jobObject?.GetProcessIds().Length ?? 0) > 0;
                void Report(string phase) { try { progress?.Invoke(phase); } catch { /* UI reporting must not interrupt the save. */ } }
                if (reopen)
                {
                    Report("closing");
                    if (!CloseForProfileSave(out error)) return new(false, false, error);
                }
                else CloseProcessOnly();
                bool saved = false, reopened = false;
                string warning = "";
                try
                {
                    Report("saving");
                    saved = SaveBoxManager.SaveProfile(CurrentAppName, CurrentAppPath, _vault.RootPath, name, replaceProfileId, out error, sourceClosed: true);
                    if (saved)
                    {
                        var prof = SaveBoxManager.ListSnapshots().FirstOrDefault(p => p.AppPath.Equals(CurrentAppPath, StringComparison.OrdinalIgnoreCase) && p.ProfileName.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (prof != null) _linkedProfileId = prof.ProfileId;
                    }
                }
                catch (Exception) { error = "The save could not complete. Session files and existing saved copies were kept."; }
                finally
                {
                    if (reopen)
                    {
                        Report("reopening");
                        reopened = LaunchIsolated(CurrentAppPath, out string reopenError);
                        if (!reopened) warning = "The app could not reopen. Its session files were kept; use Open to retry. " + reopenError;
                    }
                }
                if (!saved)
                {
                    error = error.Replace("your app is still open", "your session files were kept", StringComparison.OrdinalIgnoreCase);
                    if (reopened) error += " Your app reopened.";
                    if (warning.Length > 0) error += " " + warning;
                }
                return new(saved, reopened, saved ? "" : error, saved ? warning : "");
            }
        }

        private bool CloseForProfileSave(out string error)
        {
            error = "";
            var job = _jobObject;
            if (job == null) { State = SessionState.Preserved; return true; }
            int[] ids = job.GetProcessIds();
            bool visible = SessionWindows.HasVisibleWindow(ids);
            foreach (int id in ids)
            {
                try { using var app = Process.GetProcessById(id); app.CloseMainWindow(); }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
            }
            var wait = Stopwatch.StartNew();
            while (visible && job.GetProcessIds().Length > 0 && wait.Elapsed < TimeSpan.FromSeconds(12))
            {
                Thread.Sleep(100);
                if (!SessionWindows.HasVisibleWindow(job.GetProcessIds())) break;
            }
            if (SessionWindows.HasVisibleWindow(job.GetProcessIds()))
            {
                error = "The app is still open, possibly waiting for unsaved work. Finish its dialog or close it, then try Save again. Session files were kept.";
                return false;
            }
            // Only background processes left in this selected session's job may be stopped.
            // No window is force-closed, and the vault and its lease stay alive.
            job.Terminate(0);
            wait.Restart();
            while (job.GetProcessIds().Length > 0 && wait.Elapsed < TimeSpan.FromSeconds(5)) Thread.Sleep(50);
            if (job.GetProcessIds().Length > 0) { error = "The app is still closing. Its session files were kept; try Save again."; return false; }
            CloseProcessOnly();
            return true;
        }

        public bool LaunchIsolated(string rawPath, out string errorMessage, bool restoreSavedProfile = false, string? profileId = null)
        {
            errorMessage = string.Empty;
            lock (_lock)
            {
                bool preserveExistingVault = _vault != null && State == SessionState.Preserved;
                if (IsActive)
                {
                    errorMessage = "This session is already running.";
                    return false;
                }

                try
                {
                    string targetExe = ShortcutResolver.Resolve(rawPath);
                    if (!File.Exists(targetExe))
                    {
                        errorMessage = $"Executable not found at path: {targetExe}";
                        return false;
                    }

                    CurrentAppPath = targetExe;
                    CurrentAppName = Path.GetFileName(targetExe);

                    // 1. Provision Ephemeral Vault if not resuming preserved session
                    if (_vault == null || State == SessionState.Idle)
                    {
                        _linkedProfileId = restoreSavedProfile ? (profileId ?? SaveBoxManager.DefaultProfile(CurrentAppName)?.ProfileId) : null;
                        _vault = new EphemeralVault();
                        if (restoreSavedProfile)
                        {
                            bool restored = profileId != null
                                ? SaveBoxManager.RestoreProfile(profileId, _vault.RootPath)
                                : SaveBoxManager.RestoreSnapshot(CurrentAppName, _vault.RootPath);

                            if (!restored)
                                throw new IOException("The saved profile could not be restored. Existing sessions were kept.");

                            string? profIdToMigrate = profileId ?? _linkedProfileId;
                            if (profIdToMigrate != null)
                            {
                                var savedProf = SaveBoxManager.GetProfile(profIdToMigrate);
                                if (savedProf != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(savedProf.OriginalVaultPath))
                                    {
                                        PathMigrationHelper.MigrateVaultPaths(_vault.RootPath, savedProf.OriginalVaultPath, _vault.RootPath);
                                    }
                                    PathMigrationHelper.MigrateAnyOldVaultPaths(_vault.RootPath, _vault.RootPath);
                                }
                            }
                        }
                    }

                    // 2. Create Job Object for atomic process-tree management
                    _jobObject ??= new JobObject();

                    // 3. Prepare ProcessStartInfo with environment redirection
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = targetExe,
                        WorkingDirectory = Path.GetDirectoryName(targetExe) ?? string.Empty,
                        UseShellExecute = false
                    };

                    // Redirect primary profile paths to ephemeral vault
                    startInfo.Environment["APPDATA"] = _vault.RoamingAppData;
                    startInfo.Environment["LOCALAPPDATA"] = _vault.LocalAppData;
                    startInfo.Environment["USERPROFILE"] = _vault.UserProfile;
                    startInfo.Environment["HOME"] = _vault.UserProfile;

                    string rootDrive = Path.GetPathRoot(_vault.UserProfile)?.TrimEnd('\\') ?? "C:";
                    string homePath = _vault.UserProfile.Length > rootDrive.Length 
                        ? _vault.UserProfile.Substring(rootDrive.Length) 
                        : "\\";

                    startInfo.Environment["HOMEDRIVE"] = rootDrive;
                    startInfo.Environment["HOMEPATH"] = homePath;
                    startInfo.Environment["TEMP"] = _vault.TempDir;
                    startInfo.Environment["TMP"] = _vault.TempDir;
                    startInfo.Environment["MULTIBOX_ISOLATED"] = "1";
                    startInfo.Environment["MULTIBOX_VAULT"] = _vault.VaultId;

                    // Browser config home overrides
                    startInfo.Environment["CHROME_CONFIG_HOME"] = Path.Combine(_vault.UserProfile, ".config");
                    startInfo.Environment["XDG_CONFIG_HOME"] = Path.Combine(_vault.UserProfile, ".config");
                    startInfo.Environment["XDG_DATA_HOME"] = _vault.LocalAppData;

                    // Application specific overrides for seamless launch
                    string zcodeVaultDir = Path.Combine(_vault.RoamingAppData, "ZCode");
                    startInfo.Environment["ZCODE_DESKTOP_HOME_DIR"] = _vault.UserProfile;
                    startInfo.Environment["ZCODE_DESKTOP_USER_DATA_DIR"] = zcodeVaultDir;
                    startInfo.Environment["ZCODE_DESKTOP_SESSION_DATA_DIR"] = Path.Combine(zcodeVaultDir, "session");

                    // OpenAI Codex / ChatGPT Desktop specific overrides
                    string codexHome = Path.Combine(_vault.UserProfile, ".codex");
                    string codexVaultDir = Path.Combine(_vault.RoamingAppData, "Codex");
                    Directory.CreateDirectory(codexHome);
                    Directory.CreateDirectory(codexVaultDir);
                    startInfo.Environment["CODEX_HOME"] = codexHome;
                    startInfo.Environment["CODEX_ELECTRON_USER_DATA_PATH"] = codexVaultDir;
                    startInfo.Environment["CODEX_DESKTOP_USER_DATA_DIR"] = codexVaultDir;
                    startInfo.Environment["CODEX_DESKTOP_SESSION_DATA_DIR"] = Path.Combine(codexVaultDir, "session");
                    startInfo.Environment["CODEX_DESKTOP_HOME_DIR"] = _vault.UserProfile;

                    // 4. Detect and optimize Electron / Chromium / Mozilla engines
                    var profile = ApplyEngineArguments(startInfo, targetExe, _vault);
                    _profileMonitor?.Dispose();
                    _profileMonitor = new ProfileActivityMonitor(profile.Path, profile.Method, profile.Explicit);

                    // Attach before the app runs so its children stay in this session.
                    _process = _jobObject.Start(startInfo);

                    State = SessionState.Running;

                    // 7. Start Telemetry
                    _startTime = DateTime.UtcNow;
                    _lastCpuMeasurementTime = DateTime.UtcNow;
                    _lastCpuTime = TimeSpan.Zero;

                    _telemetryTimer = new System.Threading.Timer(PollTelemetry, null, 500, 1000);

                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    CleanupInternal(preserveExistingVault);
                    return false;
                }
            }
        }

        private static (string Path, string Method, bool Explicit) ApplyEngineArguments(ProcessStartInfo startInfo, string targetExe, EphemeralVault vault)
        {
            string dir = Path.GetDirectoryName(targetExe) ?? string.Empty;
            string fileName = Path.GetFileName(targetExe).ToLowerInvariant();

            bool isCodexOrChatGPT = fileName.Contains("chatgpt") || fileName.Contains("codex");

            // Chromium / Electron detection
            bool isElectronOrChromium =
                isCodexOrChatGPT ||
                File.Exists(Path.Combine(dir, "resources", "app.asar")) ||
                Directory.Exists(Path.Combine(dir, "resources", "app")) ||
                File.Exists(Path.Combine(dir, "chrome.dll")) ||
                File.Exists(Path.Combine(dir, "ffmpeg.dll")) ||
                fileName.Contains("electron") ||
                fileName.Contains("chrome") ||
                fileName.Contains("brave") ||
                fileName.Contains("edge");

            if (isElectronOrChromium)
            {
                string folderName = isCodexOrChatGPT ? "Codex" : Path.GetFileNameWithoutExtension(targetExe);
                string targetUserData = Path.Combine(vault.RoamingAppData, folderName);
                Directory.CreateDirectory(targetUserData);

                string chromiumArgs = $"--user-data-dir=\"{targetUserData}\" --no-first-run --no-default-browser-check";
                startInfo.Arguments = string.IsNullOrWhiteSpace(startInfo.Arguments)
                    ? chromiumArgs
                    : $"{startInfo.Arguments} {chromiumArgs}";
                return (targetUserData, "Chromium/Electron profile argument", true);
            }

            // Mozilla / Thunderbird detection
            if (fileName.Contains("thunderbird") || fileName.Contains("firefox"))
            {
                string mozillaArgs = $"-profile \"{vault.UserDataDir}\" -no-remote";
                startInfo.Arguments = string.IsNullOrWhiteSpace(startInfo.Arguments)
                    ? mozillaArgs
                    : $"{startInfo.Arguments} {mozillaArgs}";
                return (vault.UserDataDir, "Mozilla profile argument", true);
            }
            return (vault.UserProfile, "Profile environment variables", false);
        }

        public int[] GetProcessIds()
        {
            lock (_lock) return _jobObject?.GetProcessIds() ?? [];
        }

        private void PollTelemetry(object? state)
        {
            IsolationTelemetry? telemetry = null;
            string? paused = null;
            lock (_lock)
            {
                if (_jobObject == null || _isTerminating || State != SessionState.Running) return;
                try
                {
                    var ids = _jobObject.GetProcessIds();
                    if (ids.Length == 0)
                    {
                        _telemetryTimer?.Dispose();
                        _telemetryTimer = null;
                        _process?.Dispose();
                        _process = null;
                        if (_linkedProfileId != null && _vault != null)
                        {
                            try { SaveBoxManager.SyncVaultToProfile(_linkedProfileId, _vault.RootPath); } catch { }
                        }
                        State = SessionState.Preserved;
                        paused = CurrentAppName;
                    }
                    else
                    {
                        long memory = 0;
                        TimeSpan cpu = TimeSpan.Zero;
                        foreach (int id in ids)
                        {
                            try { using var p = Process.GetProcessById(id); memory += p.WorkingSet64; cpu += p.TotalProcessorTime; }
                            catch (ArgumentException) { }
                            catch (InvalidOperationException) { }
                            catch (System.ComponentModel.Win32Exception) { }
                        }
                        var now = DateTime.UtcNow;
                        double duration = (now - _lastCpuMeasurementTime).TotalMilliseconds;
                        double percent = duration > 0 ? Math.Clamp((cpu - _lastCpuTime).TotalMilliseconds / (Environment.ProcessorCount * duration) * 100, 0, 100) : 0;
                        _lastCpuTime = cpu;
                        _lastCpuMeasurementTime = now;
                        telemetry = new IsolationTelemetry {
                            SessionId = _vault?.VaultId ?? "", AppName = CurrentAppName, AppPath = CurrentAppPath,
                            ProcessId = ids[0], MemoryBytes = memory, CpuPercent = Math.Round(percent, 1),
                            Elapsed = now - _startTime, IsRunning = true, VaultPath = _vault?.RootPath ?? "",
                            HasSnapshot = SaveBoxManager.HasSnapshot(CurrentAppName)
                        };
                    }
                }
                catch { /* A transient process query must not discard a live session. */ }
            }
            if (telemetry != null) TelemetryUpdated?.Invoke(telemetry);
            if (paused != null) SessionPaused?.Invoke(paused);
        }

        public void CloseProcessOnly()
        {
            lock (_lock)
            {
                _telemetryTimer?.Dispose();
                _telemetryTimer = null;

                if (_jobObject != null)
                {
                    try { _jobObject.Terminate(0); } catch { }
                    _jobObject.Dispose();
                    _jobObject = null;
                }

                if (_process != null)
                {
                    try
                    {
                        if (!_process.HasExited)
                        {
                            _process.Kill(true);
                            _process.WaitForExit(500);
                        }
                    }
                    catch { }
                    try { _process.Dispose(); } catch { }
                    _process = null;
                }

                if (_linkedProfileId != null && _vault != null)
                {
                    try { SaveBoxManager.SyncVaultToProfile(_linkedProfileId, _vault.RootPath); } catch { }
                }

                State = SessionState.Preserved;
            }
        }

        public void TerminateAndWipe()
        {
            lock (_lock)
            {
                if (_isTerminating) return;
                _isTerminating = true;
                try
                {
                    _telemetryTimer?.Dispose();
                    _telemetryTimer = null;
                    _profileMonitor?.Dispose();
                    _profileMonitor = null;
                    if (_jobObject != null)
                    {
                        try { _jobObject.Terminate(0); } catch { }
                        var wait = Stopwatch.StartNew();
                        while (_jobObject.GetProcessIds().Length > 0 && wait.Elapsed < TimeSpan.FromSeconds(5)) Thread.Sleep(40);
                        _jobObject.Dispose();
                        _jobObject = null;
                    }
                    if (_process != null)
                    {
                        try
                        {
                            if (!_process.HasExited)
                            {
                                _process.Kill(true);
                                _process.WaitForExit(1000);
                            }
                        }
                        catch { }
                        _process.Dispose();
                        _process = null;
                    }
                    if (_linkedProfileId != null && _vault != null)
                    {
                        try { SaveBoxManager.SyncVaultToProfile(_linkedProfileId, _vault.RootPath); } catch { }
                    }
                    State = SessionState.Preserved;
                    if (_vault != null && !_vault.ScrubAndWipe())
                        throw new IOException("Some session files could not be deleted. Retry End & wipe.");
                    _vault?.Dispose();
                    _vault = null;
                    CurrentAppName = CurrentAppPath = string.Empty;
                    State = SessionState.Idle;
                }
                finally { _isTerminating = false; }
            }
            SessionTerminated?.Invoke();
        }

        private void CleanupInternal(bool preserveVault = false)
        {
            _profileMonitor?.Dispose();
            _profileMonitor = null;
            _telemetryTimer?.Dispose();
            _telemetryTimer = null;
            _jobObject?.Dispose();
            _jobObject = null;
            if (!preserveVault) { _vault?.Dispose(); _vault = null; }
            _process?.Dispose();
            _process = null;
            State = preserveVault ? SessionState.Preserved : SessionState.Idle;
        }

        public void UnlinkProfile(string profileId)
        {
            lock (_lock)
            {
                if (_linkedProfileId == profileId) _linkedProfileId = null;
            }
        }

        public void Dispose()
        {
            TerminateAndWipe();
            GC.SuppressFinalize(this);
        }
    }
}
