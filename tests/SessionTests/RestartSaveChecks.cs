using System.Diagnostics;
using Microsoft.Data.Sqlite;
using MultiBox.Core;

static class RestartSaveChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task Until(Func<bool> ready)
    {
        var elapsed = Stopwatch.StartNew();
        while (!ready()) { if (elapsed.Elapsed > TimeSpan.FromSeconds(12)) throw new Exception("Restart save fixture did not become ready."); await Task.Delay(80); }
    }
    public static async Task Run(string directory)
    {
        string token = Guid.NewGuid().ToString("N");
        string exe = Path.Combine(directory, "RestartSave-" + token + ".exe");
        string veto = Path.Combine(directory, "veto-close-" + token + ".exe");
        string heldExe = exe + ".held";
        foreach (string path in new[] { exe, veto }) File.Copy(Path.Combine(directory, "SessionFixture.exe"), path);
        using var manager = new SessionManager();
        try
        {
            Check(manager.LaunchNew(exe, false, out string error), error);
            var first = manager.Snapshot().Sessions.Single();
            Check(manager.LaunchNew(exe, false, out error), error);
            var other = manager.Snapshot().Sessions.Last();
            string Ready(SessionView session) => Path.Combine(session.VaultPath, "User", "AppData", "Roaming", "child-pid.txt");
            await Until(() => File.Exists(Ready(first)) && File.Exists(Ready(other)));
            await Task.Delay(1500);
            int[] otherIds = manager.GetEngine(other.Id)!.GetProcessIds();
            string marker = Path.Combine(first.VaultPath, "User", "profile-choice.txt"); File.WriteAllText(marker, "first");
            var phases = new List<string>();
            Check(manager.Customize(first.Id, "Work", "teal", out error), error);
            var result = manager.SaveProfileAndReopen(first.Id, "First", null, phases.Add);
            Check(result.Saved && result.Reopened && result.Warning == "", result.Error + result.Warning);
            Check(phases.SequenceEqual(new[] { "closing", "saving", "reopening" }), "Save phases are out of order.");
            Check(manager.GetEngine(first.Id)!.CurrentVaultRootPath == first.VaultPath && File.ReadAllText(marker) == "first", "Reopen changed or wiped its source vault.");
            Check(manager.SelectedId == other.Id && otherIds.SequenceEqual(manager.GetEngine(other.Id)!.GetProcessIds()), "Saving another session restarted or selected the unrelated app.");
            Check(manager.Snapshot().Sessions.Single(s => s.Id == first.Id).DisplayName == "Work" && manager.Snapshot().Sessions.Single(s => s.Id == first.Id).Color == "teal", "Save and reopen lost its session identity.");
            var saved = SaveBoxManager.ListSnapshots(true).Single(p => p.AppPath == exe && p.ProfileName == "First");
            using (var database = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(saved.FolderPath, "Data", "User", "AppData", "Roaming", "recovery-test.sqlite"), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            {
                database.Open(); using var query = database.CreateCommand();
                query.CommandText = "PRAGMA quick_check;"; Check((string?)query.ExecuteScalar() == "ok", "Recovered snapshot failed integrity check.");
                query.CommandText = "SELECT value FROM entries WHERE id=1;"; Check((string?)query.ExecuteScalar() == "committed", "Rollback recovery saved an unfinished transaction.");
                query.CommandText = "SELECT COUNT(*) FROM entries WHERE id>1;"; Check((long)query.ExecuteScalar()! == 0, "Uncommitted spilled pages survived recovery.");
            }
            Console.WriteLine("PASS Save closes only its captured app, recovers SQLite journals, saves and reopens the same vault; other apps and selection are kept.");
            await Until(() => manager.GetEngine(first.Id)!.GetProcessIds().Contains(int.Parse(File.ReadAllText(Ready(first)))));
            int[] beforeInvalid = manager.GetEngine(first.Id)!.GetProcessIds();
            result = manager.SaveProfileAndReopen(first.Id, "First", null);
            Check(!result.Saved && beforeInvalid.SequenceEqual(manager.GetEngine(first.Id)!.GetProcessIds()), "A name collision closed the app before validation.");
            string locked = Path.Combine(first.VaultPath, "User", "Documents", "locked-project.txt"); File.WriteAllText(locked, "project");
            using (var lockFile = new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                result = manager.SaveProfileAndReopen(first.Id, "First", saved.ProfileId);
                Check(!result.Saved && result.Reopened && Directory.Exists(first.VaultPath), "A failed save did not keep and reopen its original session.");
                Check(File.ReadAllText(Path.Combine(saved.FolderPath, "Data", "User", "profile-choice.txt")) == "first", "A failed save damaged the saved copy.");
            }
            File.Delete(locked);
            result = manager.SaveProfileAndReopen(first.Id, "Reopen failure", null, phase => {
                if (phase == "reopening") { File.Move(exe, heldExe); File.WriteAllText(exe, "invalid executable"); }
            });
            Check(result.Saved && !result.Reopened && result.Warning.Contains("could not reopen"), "Reopen failure hid or misreported a successful save.");
            Check(Directory.Exists(first.VaultPath) && File.ReadAllText(marker) == "first" && manager.GetEngine(first.Id)!.IsPreserved, "Failed process creation wiped the preserved vault.");
            File.Delete(exe); File.Move(heldExe, exe);
            Check(manager.Resume(first.Id, out error), error);
            Console.WriteLine("PASS Invalid names never close an app; failed copies keep the last save and reopen; failed reopen keeps both the saved copy and source vault.");
            manager.GetEngine(first.Id)!.CloseProcessOnly();
            result = manager.SaveProfileAndReopen(first.Id, "Closed copy", null);
            Check(result.Saved && !result.Reopened && manager.GetEngine(first.Id)!.IsPreserved, "Saving an already closed session reopened it unexpectedly.");
            Check(manager.LaunchNew(veto, false, out error), error);
            var guarded = manager.Snapshot().Sessions.Last();
            await Until(() => File.Exists(Path.Combine(guarded.VaultPath, "User", "AppData", "Roaming", "veto-ready.txt")));
            result = manager.SaveProfileAndReopen(guarded.Id, "Guarded", null);
            Check(!result.Saved && manager.GetEngine(guarded.Id)!.GetProcessIds().Length > 0 && Directory.Exists(guarded.VaultPath), "Save forced a window closed after it refused normal shutdown.");
            Check(otherIds.SequenceEqual(manager.GetEngine(other.Id)!.GetProcessIds()), "The close guard affected another app.");
            Console.WriteLine("PASS Already closed sessions stay closed; a window that refuses shutdown aborts the save without forcing it closed.");
        }
        finally
        {
            manager.Dispose();
            if (File.Exists(heldExe)) { File.Delete(exe); File.Move(heldExe, exe); }
            foreach (var saved in SaveBoxManager.ListSnapshots(true).Where(p => p.AppPath == exe || p.AppPath == veto)) SaveBoxManager.DeleteProfile(saved.ProfileId);
            foreach (string path in new[] { exe, veto }) { File.Delete(path); File.Delete(SaveBoxManager.SaveDiagnosticPath(path)); }
        }
    }
}
