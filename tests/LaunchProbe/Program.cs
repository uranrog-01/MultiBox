using MultiBox.Core;

// E2E probe: drives the real MultiBox engine (SessionManager) against an arbitrary exe,
// reporting session liveness, job PIDs and window presence. Wipes every session it creates.
// Usage: LaunchProbe <exePath> [waitSeconds] [--two]
string exe = ShortcutResolver.Resolve(args[0]);
int waitSeconds = args.Length > 1 && int.TryParse(args[1], out int w) ? w : 20;
bool two = args.Contains("--two");

using var manager = new SessionManager();
var created = new List<string>();
try
{
    if (!manager.LaunchNew(exe, false, out string error))
    {
        Console.WriteLine($"SESSION1 LAUNCH FAILED: {error}");
        return 1;
    }
    created.Add(manager.Snapshot().Sessions.Last().Id);
    Console.WriteLine($"SESSION1 launched id={created[0]}");

    if (two)
    {
        await Task.Delay(3000);
        bool secondOk = manager.LaunchNew(exe, false, out string error2);
        if (secondOk) created.Add(manager.Snapshot().Sessions.Last().Id);
        Console.WriteLine(secondOk ? $"SESSION2 launched id={created[^1]}" : $"SESSION2 LAUNCH FAILED: {error2}");
    }

    Console.WriteLine($"Waiting {waitSeconds}s…");
    await Task.Delay(TimeSpan.FromSeconds(waitSeconds));

    foreach (var session in manager.Snapshot().Sessions.Where(s => created.Contains(s.Id)))
    {
        var engine = manager.GetEngine(session.Id)!;
        var pids = engine.GetProcessIds();
        Console.WriteLine($"SESSION {session.Id}: state={session.State} running={engine.IsRunning} pids=[{string.Join(",", pids)}] window={SessionWindows.HasVisibleWindow(pids)}");
    }
    return 0;
}
finally
{
    foreach (string id in created)
        Console.WriteLine(manager.Wipe(id, out string wipeError) ? $"WIPED {id}" : $"WIPE FAILED {id}: {wipeError}");
}
