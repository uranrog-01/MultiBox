using System.Diagnostics;
using MultiBox.Core;
using Microsoft.Data.Sqlite;

if (args.Length == 3 && args[0] == "--hold-savebox")
{
    using var mutex = new Mutex(false, @"Local\MultiBox_SaveBox_Mutex");
    mutex.WaitOne();
    try
    {
        Directory.CreateDirectory(args[1]);
        File.WriteAllText(Path.Combine(args[1], "keep.txt"), "save in progress");
        File.WriteAllText(args[2], "ready");
        var elapsed = Stopwatch.StartNew();
        while (!File.Exists(args[2] + ".release") && elapsed.Elapsed < TimeSpan.FromSeconds(20)) Thread.Sleep(50);
    }
    finally { mutex.ReleaseMutex(); }
    return;
}

if (args.Length == 3 && args[0] == "--vault-owner")
{
    using var owned = new EphemeralVault(args[1]);
    File.WriteAllText(Path.Combine(owned.UserProfile, "crash-canary.txt"), "temporary data");
    File.WriteAllText(args[2], owned.RootPath);
    await Task.Delay(Timeout.Infinite);
    return;
}
if (Path.GetFileName(Environment.ProcessPath!).StartsWith("quiet-chrome-"))
{
    await Task.Delay(TimeSpan.FromMinutes(3));
    return;
}

string root = Environment.GetEnvironmentVariable("APPDATA")!;
string vault = Environment.GetEnvironmentVariable("MULTIBOX_VAULT")!;
if (Path.GetFileName(Environment.ProcessPath!).StartsWith("veto-close-"))
{
    var ui = new Thread(() => {
        using var window = new Form { Text = "MultiBox close guard fixture", Opacity = 0, ShowInTaskbar = false, Width = 100, Height = 100 };
        window.FormClosing += (_, e) => e.Cancel = true;
        window.Shown += (_, _) => File.WriteAllText(Path.Combine(root, "veto-ready.txt"), "ready");
        Application.Run(window);
    });
    ui.SetApartmentState(ApartmentState.STA); ui.Start(); ui.Join(); return;
}
if (args.Contains("--child"))
{
    using var recoveryDatabase = Path.GetFileName(Environment.ProcessPath!).StartsWith("RestartSave-")
        ? new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(root, "recovery-test.sqlite"), Pooling = false }.ToString()) : null;
    if (recoveryDatabase != null)
    {
        recoveryDatabase.Open(); using var setup = recoveryDatabase.CreateCommand();
        setup.CommandText = "PRAGMA journal_mode=DELETE; PRAGMA cache_size=1; CREATE TABLE IF NOT EXISTS entries(id INTEGER PRIMARY KEY, value TEXT); INSERT OR REPLACE INTO entries VALUES(1, 'committed');";
        setup.ExecuteNonQuery();
    }
    using var pendingRecovery = recoveryDatabase?.BeginTransaction();
    if (recoveryDatabase != null)
    {
        using var change = recoveryDatabase.CreateCommand(); change.Transaction = pendingRecovery;
        change.CommandText = "UPDATE entries SET value='unfinished' WHERE id=1; WITH RECURSIVE numbers(n) AS (VALUES(2) UNION ALL SELECT n+1 FROM numbers WHERE n<100) INSERT OR REPLACE INTO entries SELECT n, printf('%01024d', n) FROM numbers;";
        change.ExecuteNonQuery();
    }
    using var database = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(root, "live-profile.sqlite"), Pooling = false }.ToString());
    database.Open();
    using (var setup = database.CreateCommand())
    {
        setup.CommandText = "PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; CREATE TABLE IF NOT EXISTS profile_state(id INTEGER PRIMARY KEY, value TEXT); INSERT OR REPLACE INTO profile_state VALUES(1, 'committed-live-state');";
        setup.ExecuteNonQuery();
    }
    using var pending = database.BeginTransaction();
    using (var change = database.CreateCommand()) { change.Transaction = pending; change.CommandText = "INSERT OR REPLACE INTO profile_state VALUES(2, 'uncommitted-change');"; change.ExecuteNonQuery(); }
    string store = Path.Combine(root, "Local Storage", "leveldb"); Directory.CreateDirectory(store);
    File.WriteAllText(Path.Combine(store, "CURRENT"), "MANIFEST-000001\n");
    File.WriteAllText(Path.Combine(store, "MANIFEST-000001"), "fixture-manifest");
    File.WriteAllText(Path.Combine(store, "000002.ldb"), "durable-local-storage");
    using var runtimeLock = new FileStream(Path.Combine(store, "LOCK"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    // Chromium's DIPS database holds an exclusive lock for the lifetime of the app.
    string browserProfile = Path.Combine(root, Path.GetFileNameWithoutExtension(Environment.ProcessPath!));
    Directory.CreateDirectory(browserProfile);
    File.WriteAllText(Path.Combine(browserProfile, "Preferences"), "{\"theme\":\"dark\"}");
    File.WriteAllText(Path.Combine(browserProfile, "Local State"), "{}");
    File.WriteAllText(Path.Combine(browserProfile, "DIPS-project-notes.txt"), "important app notes");
    string ordinaryApp = Path.Combine(root, "ordinary-app"); Directory.CreateDirectory(ordinaryApp);
    File.WriteAllText(Path.Combine(ordinaryApp, "DIPS"), "ordinary app state");
    using var dips = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(browserProfile, "DIPS"), Pooling = false }.ToString());
    dips.Open();
    using (var setup = dips.CreateCommand())
    {
        setup.CommandText = "PRAGMA locking_mode=EXCLUSIVE; CREATE TABLE IF NOT EXISTS bounces(site TEXT); INSERT INTO bounces VALUES('tracker.example');";
        setup.ExecuteNonQuery();
    }
    using var dipsWal = new FileStream(Path.Combine(browserProfile, "DIPS-wal"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    using var dipsShm = new FileStream(Path.Combine(browserProfile, "DIPS-shm"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    string defaultProfile = Path.Combine(browserProfile, "Default"); Directory.CreateDirectory(defaultProfile);
    File.WriteAllText(Path.Combine(defaultProfile, "Preferences"), "{}");
    using var defaultDips = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(defaultProfile, "DIPS"), Pooling = false }.ToString());
    defaultDips.Open();
    using (var setup = defaultDips.CreateCommand())
    {
        setup.CommandText = "PRAGMA locking_mode=EXCLUSIVE; CREATE TABLE IF NOT EXISTS bounces(site TEXT); INSERT INTO bounces VALUES('tracker.example');";
        setup.ExecuteNonQuery();
    }
    using var performanceDb = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(browserProfile, "declarative_performance_observer.db"), Pooling = false }.ToString());
    performanceDb.Open();
    using (var setup = performanceDb.CreateCommand())
    {
        setup.CommandText = "PRAGMA locking_mode=EXCLUSIVE; CREATE TABLE IF NOT EXISTS reports(payload TEXT); INSERT INTO reports VALUES('performance report');";
        setup.ExecuteNonQuery();
    }
    using var performanceWal = new FileStream(Path.Combine(browserProfile, "declarative_performance_observer.db-wal"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    string partition = Path.Combine(defaultProfile, "Storage", "ext", "test-extension", "def"); Directory.CreateDirectory(partition);
    using var partitionPerformance = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(partition, "declarative_performance_observer.db"), Pooling = false }.ToString());
    partitionPerformance.Open();
    using (var setup = partitionPerformance.CreateCommand()) { setup.CommandText = "PRAGMA locking_mode=EXCLUSIVE; CREATE TABLE IF NOT EXISTS reports(value TEXT); INSERT INTO reports VALUES('partition report');"; setup.ExecuteNonQuery(); }
    File.WriteAllText(Path.Combine(browserProfile, "declarative_performance_observer.db.backup"), "user backup");
    File.WriteAllText(Path.Combine(ordinaryApp, "declarative_performance_observer.db"), "ordinary app data");
    string codexHome = Environment.GetEnvironmentVariable("CODEX_HOME")!; Directory.CreateDirectory(codexHome);
    using var stateDb = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(codexHome, "state_5.sqlite"), Pooling = false }.ToString());
    stateDb.Open();
    using (var setup = stateDb.CreateCommand())
    {
        setup.CommandText = "PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; CREATE TABLE IF NOT EXISTS threads(id INTEGER PRIMARY KEY, title TEXT); INSERT OR REPLACE INTO threads VALUES(1, 'Saved project conversation');";
        setup.ExecuteNonQuery();
    }
    using var pendingState = stateDb.BeginTransaction();
    using (var change = stateDb.CreateCommand()) { change.Transaction = pendingState; change.CommandText = "INSERT OR REPLACE INTO threads VALUES(2, 'unfinished');"; change.ExecuteNonQuery(); }
    using var maintenanceLock = new FileStream(Path.Combine(codexHome, ".sqlite-maintenance.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    File.WriteAllText(Path.Combine(codexHome, ".sqlite-maintenance.lock.backup"), "durable backup");
    Directory.CreateDirectory(Path.Combine(codexHome, "sessions"));
    File.WriteAllText(Path.Combine(codexHome, "sessions", "project.jsonl"), "fixture conversation history\n");
    string documents = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE")!, "Documents");
    Directory.CreateDirectory(Path.Combine(documents, "test-project"));
    File.WriteAllText(Path.Combine(documents, "test-project", "index.html"), "<h1>Saved project</h1>");
    File.WriteAllText(Path.Combine(documents, "DIPS"), "user document, not browser metadata");
    File.WriteAllText(Path.Combine(documents, ".sqlite-maintenance.lock"), "ordinary project file");
    File.WriteAllText(Path.Combine(root, "child-pid.txt"), Environment.ProcessId.ToString());
    await Task.Delay(TimeSpan.FromMinutes(3));
    return;
}
File.WriteAllText(Path.Combine(root, "owner.txt"), vault);
File.WriteAllText(Path.Combine(root, "parent-pid.txt"), Environment.ProcessId.ToString());
string? profile = args.FirstOrDefault(a => a.StartsWith("--user-data-dir="))?.Split('=', 2)[1];
if (profile != null) File.WriteAllText(Path.Combine(profile, "actual-profile-write.txt"), "new app activity");
using var child = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--child") { UseShellExecute = false, CreateNoWindow = true });
await Task.Delay(1200); // The session must survive this launcher handing off to its child.
