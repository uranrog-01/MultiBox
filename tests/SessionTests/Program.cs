using System.Diagnostics;
using MultiBox.Core;
using Microsoft.Data.Sqlite;

static void Check(bool value, string message) { if (!value) throw new Exception(message); }
static async Task Until(Func<bool> condition, string message)
{
    var timeout = Stopwatch.StartNew();
    while (!condition()) { if (timeout.ElapsedMilliseconds > 10000) throw new Exception(message); await Task.Delay(80); }
}
string fixtureDirectory = Path.GetFullPath(args[0]);
string uniqueName = "MultiBoxTest-" + Guid.NewGuid().ToString("N") + ".exe";
string exe = Path.Combine(fixtureDirectory, uniqueName);
File.Copy(Path.Combine(fixtureDirectory, "SessionFixture.exe"), exe);
using var manager = new SessionManager();
var checks = new List<string>();
try
{
    Check(manager.LaunchNew(exe, false, out string error), error);
    var first = manager.Snapshot().Sessions.Single();
    Check(manager.LaunchNew(exe, false, out error), error);
    var second = manager.Snapshot().Sessions.Last();
    Check(first.Id != second.Id && first.VaultPath != second.VaultPath, "Instances shared a vault or identity.");
    Check(first.Instance == 1 && second.Instance == 2, "Instance labels were not independent.");
    string Data(SessionView session, string name) => Path.Combine(session.VaultPath, "User", "AppData", "Roaming", name);
    await Until(() => File.Exists(Data(first, "child-pid.txt")) && File.Exists(Data(second, "child-pid.txt")), "Fixture children did not start.");
    string firstOwner = File.ReadAllText(Data(first, "owner.txt")), secondOwner = File.ReadAllText(Data(second, "owner.txt"));
    Check(firstOwner != secondOwner, "Profile marker crossed session boundaries.");
    checks.Add("Two copies run concurrently with independent IDs, jobs, environment and profile files.");
    await Task.Delay(1900);
    var firstEngine = manager.GetEngine(first.Id)!;
    var secondEngine = manager.GetEngine(second.Id)!;
    Check(firstEngine.IsRunning && secondEngine.IsRunning, "Launcher exit incorrectly stopped a live child session.");
    var firstPids = firstEngine.GetProcessIds();
    var secondPids = secondEngine.GetProcessIds();
    Check(firstPids.Length > 0 && secondPids.Length > 0 && !firstPids.Intersect(secondPids).Any(), "Session jobs share processes.");
    checks.Add("Child handoff keeps the session running; process memberships do not overlap.");
    manager.Select(first.Id);
    manager.Select(second.Id);
    Check(firstPids.SequenceEqual(firstEngine.GetProcessIds()), "Switching sessions restarted the app.");
    Check(manager.Customize(first.Id, "  Work  ", "violet", out error), error);
    Check(manager.Customize(second.Id, "Personal", "blue", out error), error);
    var named = manager.Snapshot();
    Check(named.Sessions.Single(s => s.Id == first.Id).DisplayName == "Work" && named.Sessions.Single(s => s.Id == first.Id).Color == "violet", "Session identity was not updated.");
    Check(named.SelectedId == second.Id && named.Sessions.Single(s => s.Id == second.Id).DisplayName == "Personal", "Editing another session changed selection or its identity.");
    Check(firstPids.SequenceEqual(firstEngine.GetProcessIds()) && secondPids.SequenceEqual(secondEngine.GetProcessIds()), "Renaming or recoloring restarted an app.");
    Check(!manager.Customize("missing-session", "Wrong", "blue", out error) && !manager.Customize(first.Id, "Bad\nname", "blue", out error) && !manager.Customize(first.Id, "Wrong", "unknown", out error), "An invalid identity request was accepted.");
    Check(manager.Snapshot().Sessions.Single(s => s.Id == first.Id).DisplayName == "Work", "A rejected request changed a session name.");
    Check(manager.Customize(first.Id, "", "lime", out error) && manager.Snapshot().Sessions.Single(s => s.Id == first.Id).DisplayName == null, "Reset did not restore the automatic name.");
    checks.Add("Names and colors target only their captured session, preserve processes/selection, reject invalid input and support reset.");
    File.WriteAllText(Data(first, "saved-only.txt"), "saved from session one");
    // Low-level live-copy coverage; the UI uses the close/save/reopen route tested separately.
    Check(manager.SaveProfile(first.Id, "Default", null, out error), error);
    var liveSaved = SaveBoxManager.ListSnapshots(true).Single(p => p.AppName == uniqueName && p.ProfileName == "Default");
    string liveSavedRoaming = Path.Combine(liveSaved.FolderPath, "Data", "User", "AppData", "Roaming");
    string savedBrowserProfile = Path.Combine(liveSavedRoaming, Path.GetFileNameWithoutExtension(exe));
    Check(File.ReadAllText(Path.Combine(savedBrowserProfile, "DIPS-project-notes.txt")) == "important app notes", "DIPS name filtering discarded ordinary app data.");
    Check(File.ReadAllText(Path.Combine(savedBrowserProfile, "Preferences")) == "{\"theme\":\"dark\"}", "App preferences were lost.");
    Check(File.ReadAllText(Path.Combine(liveSavedRoaming, "ordinary-app", "DIPS")) == "ordinary app state", "An ordinary app's DIPS file was mistaken for Chromium metadata.");
    Check(File.ReadAllText(Path.Combine(liveSavedRoaming, "ordinary-app", "declarative_performance_observer.db")) == "ordinary app data", "An ordinary app database was mistaken for browser telemetry.");
    Check(File.ReadAllText(Path.Combine(savedBrowserProfile, "declarative_performance_observer.db.backup")) == "user backup", "A similarly named performance database backup was lost.");
    string[] expectedRegenerated = Directory.GetFiles(Data(first, Path.GetFileNameWithoutExtension(exe)), "*", SearchOption.AllDirectories)
        .Where(p => Path.GetFileName(p) is "DIPS" or "DIPS-wal" or "DIPS-shm" or "DIPS-journal" or "declarative_performance_observer.db" or "declarative_performance_observer.db-wal" or "declarative_performance_observer.db-journal")
        .Select(p => Path.GetRelativePath(first.VaultPath, p).Replace('\\', '/')).ToArray();
    Check(expectedRegenerated.Length >= 3 && new HashSet<string>(expectedRegenerated).SetEquals(liveSaved.RegeneratedBrowserMetadata), "Browser metadata exclusions were not recorded precisely.");
    Check(!Directory.EnumerateFiles(savedBrowserProfile, "DIPS*").Any(f => Path.GetFileName(f) is "DIPS" or "DIPS-wal" or "DIPS-shm" or "DIPS-journal"), "An exclusively locked browser DIPS file or sidecar entered the snapshot.");
    Check(!File.Exists(Path.Combine(savedBrowserProfile, "Default", "DIPS")) && File.Exists(Path.Combine(savedBrowserProfile, "Default", "Preferences")), "Chrome's Default profile lost preferences or retained exclusively locked DIPS data.");
    Check(!File.Exists(Path.Combine(savedBrowserProfile, "declarative_performance_observer.db")) && !File.Exists(Path.Combine(savedBrowserProfile, "declarative_performance_observer.db-wal")), "A held performance-report store or sidecar blocked or entered the saved profile.");
    checks.Add("Locked browser performance reports do not block in-app saves; telemetry exclusions are recorded and similarly named app data survives.");
    string savedDocuments = Path.Combine(liveSaved.FolderPath, "Data", "User", "Documents");
    Check(File.ReadAllText(Path.Combine(savedDocuments, "test-project", "index.html")) == "<h1>Saved project</h1>", "Project source was lost.");
    Check(File.ReadAllText(Path.Combine(savedDocuments, "DIPS")) == "user document, not browser metadata", "A user document named DIPS was lost.");
    Check(File.ReadAllText(Path.Combine(savedDocuments, ".sqlite-maintenance.lock")) == "ordinary project file", "An ordinary project file was mistaken for a Codex runtime lock.");
    string savedCodex = Path.Combine(liveSaved.FolderPath, "Data", "User", ".codex");
    Check(!File.Exists(Path.Combine(savedCodex, ".sqlite-maintenance.lock")), "The active Codex maintenance lock entered the saved profile.");
    string liveMaintenanceLock = Path.Combine(first.VaultPath, "User", ".codex", ".sqlite-maintenance.lock");
    Check(File.Exists(liveMaintenanceLock), "Saving deleted the live app's maintenance marker.");
    bool maintenanceStillHeld = false;
    try { using var probe = new FileStream(liveMaintenanceLock, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); }
    catch (IOException) { maintenanceStillHeld = true; }
    Check(maintenanceStillHeld, "Saving released or replaced the live app's maintenance lock.");
    Check(File.ReadAllText(Path.Combine(savedCodex, ".sqlite-maintenance.lock.backup")) == "durable backup", "An exact-name marker exclusion discarded a similarly named backup.");
    Check(File.ReadAllText(Path.Combine(savedCodex, "sessions", "project.jsonl")) == "fixture conversation history\n", "Conversation history was lost.");
    using (var savedState = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(savedCodex, "state_5.sqlite"), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
    {
        savedState.Open(); using var inspect = savedState.CreateCommand();
        inspect.CommandText = "PRAGMA quick_check;"; Check((string?)inspect.ExecuteScalar() == "ok", "Saved Codex state database is corrupt.");
        inspect.CommandText = "SELECT title FROM threads WHERE id=1;"; Check((string?)inspect.ExecuteScalar() == "Saved project conversation", "Committed Codex project state was lost.");
        inspect.CommandText = "SELECT COUNT(*) FROM threads WHERE id=2;"; Check((long)inspect.ExecuteScalar()! == 0, "Uncommitted Codex state entered the saved profile.");
    }
    checks.Add("In-app Save omits the held Codex maintenance marker, retains committed SQLite state and conversation history, and keeps similarly named user files.");
    using (var savedDb = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(liveSavedRoaming, "live-profile.sqlite"), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
    {
        savedDb.Open();
        using var inspect = savedDb.CreateCommand();
        inspect.CommandText = "PRAGMA quick_check;"; Check((string?)inspect.ExecuteScalar() == "ok", "Live SQLite snapshot is corrupt.");
        inspect.CommandText = "SELECT value FROM profile_state WHERE id=1;";
        Check((string?)inspect.ExecuteScalar() == "committed-live-state", "Committed WAL data is missing from the saved profile.");
        inspect.CommandText = "SELECT COUNT(*) FROM profile_state WHERE id=2;";
        Check((long)inspect.ExecuteScalar()! == 0, "An unfinished app transaction entered the saved profile.");
    }
    Check(!File.Exists(Path.Combine(liveSavedRoaming, "live-profile.sqlite-wal")) && !File.Exists(Path.Combine(liveSavedRoaming, "live-profile.sqlite-shm")), "A live SQLite sidecar was copied over the consistent backup.");
    Check(!File.Exists(Path.Combine(liveSavedRoaming, "Local Storage", "leveldb", "LOCK")) && File.Exists(Path.Combine(liveSavedRoaming, "Local Storage", "leveldb", "000002.ldb")), "Lock exclusion removed persistent local storage or retained an active lock.");
    Check(firstEngine.IsRunning && firstPids.SequenceEqual(firstEngine.GetProcessIds()) && Directory.Exists(first.VaultPath), "Saving closed/restarted the app or wiped its session.");
    checks.Add("Exclusive Chromium DIPS locks do not block saves; project source, preferences and similarly named documents are preserved.");
    checks.Add("Live save keeps the same app processes running, skips held lock markers and captures committed SQLite WAL state without unfinished changes.");
    string lockedFile = Data(first, "locked-file.txt");
    File.WriteAllText(lockedFile, "locked");
    string secondBlocked = Path.Combine(first.VaultPath, "User", "Documents", "project-state.json");
    File.WriteAllText(secondBlocked, "important project data");
    using (var held = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    using (var otherHeld = new FileStream(secondBlocked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        Check(!manager.Save(first.Id, out error), "Saving a locked profile should report failure rather than save a partial copy.");
        Check(error.Contains("locked-file.txt") && error.Contains("project-state.json"), "The save stopped at the first blocker rather than reporting files across folders.");
        string diagnosticJson = File.ReadAllText(SaveBoxManager.SaveDiagnosticPath(exe));
        using var diagnostic = System.Text.Json.JsonDocument.Parse(diagnosticJson);
        var failed = diagnostic.RootElement.GetProperty("BlockedFiles");
        Check(failed.GetArrayLength() == 2 && !diagnosticJson.Contains(first.VaultPath), "Diagnostics missed blockers, duplicated a failed attempt, or included an absolute vault path.");
        Check(failed.EnumerateArray().Any(f => f.GetProperty("RelativePath").GetString() == "User/Documents/project-state.json"), "Diagnostics omitted the relative path needed to distinguish project files from runtime files.");
    }
    File.Delete(lockedFile); File.Delete(secondBlocked);
    checks.Add("A single failed save reports blockers across folders and records relative-path diagnostics without publishing a partial profile.");
    string lockedBackup = Path.Combine(first.VaultPath, "User", "Documents", "project.bak");
    File.WriteAllText(lockedBackup, "only project backup");
    using (var held = new FileStream(lockedBackup, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        Check(!manager.Save(first.Id, out error), "An unreadable project backup was silently discarded.");
    File.Delete(lockedBackup);
    string lockedDocumentMarker = Path.Combine(first.VaultPath, "User", "Documents", ".sqlite-maintenance.lock");
    using (var held = new FileStream(lockedDocumentMarker, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        Check(!manager.Save(first.Id, out error), "An unreadable ordinary project file was silently treated as a Codex maintenance marker.");
    string criticalDb = Data(first, "project-state.sqlite");
    using (var exclusive = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = criticalDb, Pooling = false }.ToString()))
    {
        exclusive.Open();
        using var setup = exclusive.CreateCommand();
        setup.CommandText = "PRAGMA locking_mode=EXCLUSIVE; CREATE TABLE project_state(value TEXT); INSERT INTO project_state VALUES('important');";
        setup.ExecuteNonQuery();
        Check(!manager.Save(first.Id, out error), "An exclusively locked important database used an unverified raw-copy fallback.");
    }
    File.Delete(criticalDb);
    Check(File.ReadAllText(Path.Combine(savedDocuments, "test-project", "index.html")) == "<h1>Saved project</h1>", "A failed save damaged the last valid project copy.");
    checks.Add("Locked project backups and important databases fail safely while keeping the last valid saved project.");
    await SaveBoxConcurrencyChecks.Run(fixtureDirectory, first.VaultPath, uniqueName, exe);
    Check(SaveBoxManager.HasSnapshot(uniqueName), "Failed save discarded the previous saved profile.");
    Check(manager.Duplicate(first.Id, out error), error);
    var third = manager.Snapshot().Sessions.Last();
    Check(third.DisplayName == null && third.Color == "lime", "A fresh duplicate inherited another session's identity.");
    Check(!File.Exists(Data(third, "saved-only.txt")), "Fresh duplicate restored saved data without permission.");
    Check(manager.LaunchNew(exe, true, out error), error);
    var restored = manager.Snapshot().Sessions.Last();
    Check(File.ReadAllText(Data(restored, "saved-only.txt")) == "saved from session one", "Explicit restore did not copy saved data.");
    Check(File.ReadAllText(Path.Combine(restored.VaultPath, "User", "Documents", "test-project", "index.html")) == "<h1>Saved project</h1>", "Restoring the saved profile lost project source.");
    Check(File.ReadAllText(Path.Combine(restored.VaultPath, "User", ".codex", "sessions", "project.jsonl")) == "fixture conversation history\n", "Restoring the saved profile lost conversation history.");
    Check(restored.VaultPath != first.VaultPath && restored.VaultPath != third.VaultPath, "Restore reused an existing vault.");
    File.WriteAllText(Data(restored, "saved-only.txt"), "changed restored instance");
    Check(File.ReadAllText(Data(first, "saved-only.txt")) == "saved from session one", "Restored profile was shared live.");
    checks.Add("Save targets its captured session; duplicates start fresh; explicit restore uses a new independent copy.");
    manager.Select(second.Id);
    Check(manager.Wipe(first.Id, out error), error);
    Check(!Directory.Exists(first.VaultPath), "Selected target vault was not wiped.");
    Check(Directory.Exists(second.VaultPath) && secondEngine.GetProcessIds().Length > 0, "Wiping session one killed selected session two.");
    Check(manager.SelectedId == second.Id, "Finishing a different session's wipe changed selection.");
    Check(SaveBoxManager.HasSnapshot(uniqueName), "Session wipe deleted the saved profile.");
    checks.Add("Captured-ID wipe removes only its target and keeps other processes, selection and saved profiles.");
    secondEngine.CloseProcessOnly();
    await Task.Delay(600);
    Check(manager.Resume(second.Id, out error), error);
    Check(secondEngine.CurrentVaultRootPath == second.VaultPath, "Reopen discarded the preserved profile.");
    Check(manager.Snapshot().Sessions.Single(s => s.Id == second.Id).DisplayName == "Personal" && manager.Snapshot().Sessions.Single(s => s.Id == second.Id).Color == "blue", "Reopen lost the session's name or color.");
    checks.Add("Reopening a closed session preserves its own vault.");
    var capturedIds = manager.Snapshot().Sessions.Select(s => s.Id).ToArray();
    Check(manager.LaunchNew(exe, false, out error), error);
    var newer = manager.Snapshot().Sessions.Last();
    Check(manager.WipeMany(capturedIds, out error), error);
    Check(manager.Snapshot().Sessions.Single().Id == newer.Id, "Bulk wipe included a session launched after the command was captured.");
    Check(SaveBoxManager.HasSnapshot(uniqueName), "Bulk wipe deleted saved profiles.");
    Check(!manager.LaunchNew(Path.Combine(fixtureDirectory, "missing-" + uniqueName), false, out error), "Missing executable launched.");
    Check(manager.Snapshot().Sessions.Single().Id == newer.Id, "Failed launch discarded existing sessions.");
    checks.Add("Bulk wipe keeps newer sessions and saved profiles; failed launches leave existing sessions intact.");
    // A linked host folder must never be traversed by session cleanup.
    string sibling = Path.Combine(fixtureDirectory, "link-target-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(sibling);
    File.WriteAllText(Path.Combine(sibling, "keep.txt"), "host data");
    try
    {
        try { Directory.CreateSymbolicLink(Path.Combine(newer.VaultPath, "host-link"), sibling); }
        catch (IOException ex) when ((ex.HResult & 0xffff) == 1314)
        {
            // Directory junctions exercise the same reparse-point cleanup without elevation.
            string link = Path.Combine(newer.VaultPath, "host-link");
            var info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe")) { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("-NoProfile"); info.ArgumentList.Add("-NonInteractive"); info.ArgumentList.Add("-Command");
            info.ArgumentList.Add($"New-Item -ItemType Junction -Path '{link.Replace("'", "''")}' -Target '{sibling.Replace("'", "''")}' | Out-Null");
            using var junction = Process.Start(info)!;
            junction.WaitForExit();
            Check(junction.ExitCode == 0, "Could not create junction for cleanup verification.");
        }
        Check(manager.Wipe(newer.Id, out error), error);
        Check(File.ReadAllText(Path.Combine(sibling, "keep.txt")) == "host data", "Wipe followed a link outside its vault.");
    }
    finally { Directory.Delete(sibling, true); }
    checks.Add("Session cleanup is scoped to its vault; host files remain untouched.");
    var parallel = await Task.WhenAll(Enumerable.Range(0, 5).Select(index => Task.Run(() => manager.LaunchNew(exe, false, out _))));
    Check(parallel.All(success => success), "A concurrent app launch failed.");
    var five = manager.Snapshot().Sessions;
    Check(five.Count == 5 && five.Select(s => s.VaultPath).Distinct().Count() == 5, "Concurrent launches shared a vault.");
    await Until(() => five.All(s => File.Exists(Data(s, "child-pid.txt"))), "Concurrent child processes did not start.");
    Check(manager.WipeMany(five.Select(s => s.Id), out error), error);
    Check(manager.Snapshot().Sessions.Count == 0 && SaveBoxManager.HasSnapshot(uniqueName), "End all discarded saved profiles or left a session behind.");
    checks.Add("Five concurrent launches remain independent; End all keeps saved profiles. Failed saves retain the last valid saved copy.");
    await FeatureChecks.Run(fixtureDirectory);
    await RestartSaveChecks.Run(fixtureDirectory);
    foreach (string check in checks) Console.WriteLine("PASS " + check);
}
finally
{
    manager.Dispose();
    SaveBoxManager.DeleteSnapshot(uniqueName);
    File.Delete(SaveBoxManager.SaveDiagnosticPath(exe));
    File.Delete(exe);
}
