using System.Diagnostics;
using MultiBox.Core;

static class SaveBoxConcurrencyChecks
{
    public static async Task Run(string fixtureDirectory, string vault, string appName, string appPath)
    {
        string token = Guid.NewGuid().ToString("N");
        string staging = Path.Combine(SaveBoxManager.SaveBoxRoot, ".multibox-stage-test-" + token);
        string ready = Path.Combine(fixtureDirectory, "savebox-lock-" + token);
        var start = new ProcessStartInfo(Path.Combine(fixtureDirectory, "SessionFixture.exe")) { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("--hold-savebox"); start.ArgumentList.Add(staging); start.ArgumentList.Add(ready);
        using var owner = Process.Start(start)!;
        try
        {
            var elapsed = Stopwatch.StartNew();
            while (!File.Exists(ready))
            {
                if (owner.HasExited || elapsed.Elapsed > TimeSpan.FromSeconds(8)) throw new Exception("SaveBox mutex fixture did not start.");
                await Task.Delay(50);
            }
            SaveBoxManager.ListSnapshots(true);
            if (!File.Exists(Path.Combine(staging, "keep.txt"))) throw new Exception("Listing profiles deleted another process's active save.");
            if (SaveBoxManager.SaveProfile(appName, appPath, vault, "Busy test", null, out _)) throw new Exception("A save proceeded after its cross-process lock timed out.");
            if (!File.Exists(Path.Combine(staging, "keep.txt"))) throw new Exception("A timed-out save removed another process's staging directory.");
            Console.WriteLine("PASS Profile listing preserves another process's active save; lock timeouts never start an overlapping save.");
        }
        finally
        {
            File.WriteAllText(ready + ".release", "release");
            await owner.WaitForExitAsync();
            // Only these exact per-test paths are removed.
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            File.Delete(ready); File.Delete(ready + ".release");
        }
    }
}
