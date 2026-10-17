using MultiBox.Core;
using Microsoft.Data.Sqlite;

// Opt-in smoke test of one installed app. It owns only its newly created engine,
// vault and uniquely named saved copy. Never terminates apps by executable name.
if (args.Length != 1 || !File.Exists(args[0])) throw new ArgumentException("Pass an installed app executable.");
string app = Path.GetFullPath(args[0]);
string name = "Diagnostic " + Guid.NewGuid().ToString("N")[..12];
string? savedId = null;
using var engine = new IsolationEngine();
try
{
    if (!engine.LaunchIsolated(app, out string error)) throw new Exception(error);
    Console.WriteLine("Launched a fresh diagnostic session. Waiting for background stores to initialize.");
    await Task.Delay(TimeSpan.FromSeconds(70));
    if (!engine.IsRunning || engine.GetProcessIds().Length == 0) throw new Exception("The installed app handed off or exited; live save was not verified.");
    string vault = engine.CurrentVaultRootPath!;
    string canary = Path.Combine(vault, "User", "Documents", "save-probe.html");
    File.WriteAllText(canary, "<h1>Diagnostic project</h1>");
    var outcome = engine.SaveProfileAndReopen(name, null, phase => Console.WriteLine("Save phase: " + phase));
    if (!outcome.Saved || !outcome.Reopened)
    {
        Console.WriteLine(outcome.Error + " " + outcome.Warning);
        string diagnostic = SaveBoxManager.SaveDiagnosticPath(app);
        if (File.Exists(diagnostic)) Console.WriteLine(File.ReadAllText(diagnostic));
        throw new Exception("Installed-app live save failed.");
    }
    var saved = SaveBoxManager.ListSnapshots(true).Single(p => p.AppPath == app && p.ProfileName == name);
    savedId = saved.ProfileId;
    if (!engine.IsRunning || engine.CurrentVaultRootPath != vault || !File.Exists(canary)) throw new Exception("Save did not reopen the same diagnostic session.");
    using var restored = new EphemeralVault();
    if (!SaveBoxManager.RestoreProfile(savedId, restored.RootPath) || File.ReadAllText(Path.Combine(restored.UserProfile, "Documents", "save-probe.html")) != "<h1>Diagnostic project</h1>")
        throw new Exception("Installed-app project restore failed.");
    int checkedDatabases = 0;
    foreach (string file in Directory.EnumerateFiles(Path.Combine(saved.FolderPath, "Data"), "*", SearchOption.AllDirectories))
    {
        using var stream = File.OpenRead(file);
        byte[] header = new byte[16];
        if (stream.Read(header) != 16 || !header.SequenceEqual("SQLite format 3\0"u8.ToArray())) continue;
        using var database = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        database.Open(); using var inspect = database.CreateCommand(); inspect.CommandText = "PRAGMA quick_check;";
        if ((string?)inspect.ExecuteScalar() != "ok") throw new Exception("Saved database integrity check failed: " + Path.GetFileName(file));
        checkedDatabases++;
    }
    Console.WriteLine($"PASS Installed app close/save/reopen and project restore; {checkedDatabases} saved SQLite databases passed quick_check. Same session vault was kept.");
}
finally
{
    engine.TerminateAndWipe();
    if (savedId != null) SaveBoxManager.DeleteProfile(savedId);
}
