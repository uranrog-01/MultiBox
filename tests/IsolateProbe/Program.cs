using MultiBox.Core;

// Diagnostic for the exact Vault flow: launch a saved profile (restore its data into a fresh
// isolated vault) and verify the restored files are actually inside the session's vault.
// Wipes its own diagnostic session afterwards. Writes nothing outside the vault.
if (args.Length != 2) throw new ArgumentException("Pass the app executable path and the profileId.");
string target = ShortcutResolver.Resolve(args[0]);
Console.WriteLine($"Target: {target}");

using var manager = new SessionManager();
if (!manager.LaunchNew(target, true, out string error, args[1]))
{
    Console.WriteLine($"Launch FAILED: {error}");
    return;
}
var session = manager.Snapshot().Sessions.First();
Console.WriteLine($"Launched profile session '{session.ProfileName}'. Waiting 25 seconds for the app to initialize…");
await Task.Delay(25000);
var engine = manager.GetEngine(session.Id)!;
string vault = engine.CurrentVaultRootPath ?? "";
Console.WriteLine($"Vault: {vault}");
string codexData = Path.Combine(vault, "User", "AppData", "Roaming", "Codex");
if (Directory.Exists(codexData))
{
    var files = Directory.GetFiles(codexData, "*", SearchOption.AllDirectories).ToList();
    Console.WriteLine($"Restored Codex data inside the vault: {files.Count} files.");
    var preferences = files.FirstOrDefault(f => f.EndsWith("Preferences"));
    if (preferences != null)
        Console.WriteLine($"  e.g. {Path.GetRelativePath(vault, preferences)} ({new FileInfo(preferences).Length} bytes)");
}
else
{
    Console.WriteLine("NO Codex data inside the vault — restore did not copy the saved account!");
}
Console.WriteLine($"ownWindow: {SessionWindows.HasVisibleWindow(engine.GetProcessIds())}");
Console.WriteLine(manager.Wipe(session.Id, out string wipeError)
    ? "Diagnostic session wiped (vault + processes). The saved profile was not touched."
    : $"Wipe failed: {wipeError}");
