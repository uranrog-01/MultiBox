using System.Diagnostics;
using System.Text.Json;
using MultiBox.Core;

static class FeatureChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task Until(Func<bool> condition, string message)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition()) { if (timeout.ElapsedMilliseconds > 10000) throw new Exception(message); await Task.Delay(70); }
    }
    public static async Task Run(string fixtureDirectory)
    {
        string token = Guid.NewGuid().ToString("N");
        string exe = Path.Combine(fixtureDirectory, "chrome-" + token + ".exe");
        string quiet = Path.Combine(fixtureDirectory, "quiet-chrome-" + token + ".exe");
        string generic = Path.Combine(fixtureDirectory, "Generic-" + token + ".exe");
        string recoveryRoot = Path.Combine(fixtureDirectory, "recovery-" + token);
        string legacy = Path.Combine(SaveBoxManager.SaveBoxRoot, "Legacy-" + token);
        foreach (string copy in new[] { exe, quiet, generic }) File.Copy(Path.Combine(fixtureDirectory, "SessionFixture.exe"), copy);
        using var manager = new SessionManager();
        var profileIds = new List<string>();
        try
        {
            Check(new IndicatorPreferences().Mode == "label" && !IndicatorPreferences.IsValid("outline"), "Legacy full-window outline is still offered.");
            Check(IndicatorPreferences.Opacity("label", 0) == 1 && IndicatorPreferences.Opacity("label", 140) == 255 &&
                IndicatorPreferences.Opacity("label", 1500) == 255 && IndicatorPreferences.Opacity("label", 1650) is > 100 and < 150 &&
                IndicatorPreferences.Opacity("label", 1800) == 0 && IndicatorPreferences.Opacity("label", 4000) == 0 &&
                IndicatorPreferences.Opacity("off", 0) == 0, "Pill does not enter/fade/hide at the expected boundaries.");
            var render = typeof(IsolatedAppBorderOverlay).GetMethod("CreateSurface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            foreach (string mode in new[] { "label", "off" })
            {
                using var surface = (System.Drawing.Bitmap)render.Invoke(null, [mode, 208, 26, "ChatGPT · Work", "lime"])!;
                Check(surface.GetPixel(0, 0).A == 0, "Pill corners are not transparent.");
                int visible = 0, maxAlpha = 0;
                for (int y = 0; y < surface.Height; y++) for (int x = 0; x < surface.Width; x++)
                { int alpha = surface.GetPixel(x, y).A; if (alpha > 0) visible++; maxAlpha = Math.Max(alpha, maxAlpha); }
                Check(mode == "off" ? visible == 0 : visible > 3000 && visible <= 208 * 26, "Pill surface geometry is incorrect.");
                if (mode == "label") surface.Save("design/pill-banner.png", System.Drawing.Imaging.ImageFormat.Png);
            }
            using (var blue = (System.Drawing.Bitmap)render.Invoke(null, ["label", 208, 26, "Work · Chrome #1", "blue"])!)
            using (var lime = (System.Drawing.Bitmap)render.Invoke(null, ["label", 208, 26, "Work · Chrome #1", "lime"])!)
                Check(blue.GetPixel(10, 12).ToArgb() != lime.GetPixel(10, 12).ToArgb(), "The app indicator ignored the session color.");
            var boundsMethod = typeof(IsolatedAppBorderOverlay).GetMethod("PillBounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            var area = new System.Drawing.Rectangle(-1920, 0, 1920, 1080);
            var normal = new System.Drawing.Rectangle(-1500, 100, 900, 700);
            var pill = (System.Drawing.Rectangle)boundsMethod.Invoke(null, [normal, area, 208])!;
            Check(pill.Bottom < normal.Top && pill.Width <= 208 && pill.Height == 26, "Pill covers a normal app instead of sitting above it.");
            var maximizedPill = (System.Drawing.Rectangle)boundsMethod.Invoke(null, [area, area, 208])!;
            Check(area.Contains(maximizedPill), "Maximized-window pill is off screen.");
            Console.WriteLine("PASS Compact 208×26 pill sits above normal apps, stays on screen and auto-hides; Off stays transparent.");
            Check(manager.LaunchNew(exe, false, out string error), error);
            var session = manager.Snapshot().Sessions.Single();
            await Until(() => manager.GetEngine(session.Id)!.ProfileCheck.Status == "observed", "Explicit profile write was not observed.");
            Check(manager.GetEngine(session.Id)!.ProfileCheck.ProfilePath.StartsWith(session.VaultPath), "Profile evidence points outside its vault.");
            Check(manager.LaunchNew(generic, false, out error), error);
            var genericSession = manager.Snapshot().Sessions.Last();
            string genericOwner = Path.Combine(genericSession.VaultPath, "User", "AppData", "Roaming", "owner.txt");
            await Until(() => File.Exists(genericOwner), "Generic app did not write.");
            foreach (var readySession in new[] { session, genericSession })
            {
                string ready = Path.Combine(readySession.VaultPath, "User", "AppData", "Roaming", "child-pid.txt");
                await Until(() => File.Exists(ready) && int.TryParse(File.ReadAllText(ready), out int child) && manager.GetEngine(readySession.Id)!.GetProcessIds().Contains(child), "Fixture database initialization did not finish.");
            }
            Check(manager.GetEngine(genericSession.Id)!.ProfileCheck.Status == "unverified", "Environment-only app was incorrectly verified.");
            Console.WriteLine("PASS Profile checks distinguish explicit post-launch activity from generic environment writes.");
            string marker = Path.Combine(session.VaultPath, "User", "profile-choice.txt");
            File.WriteAllText(marker, "work");
            string runtime = Path.Combine(session.VaultPath, "User", ".codex", "runtimes", "codex-runtime-install-test", "payload", "python", "Lib", "site-packages");
            Directory.CreateDirectory(runtime);
            string runtimeFile = Path.Combine(runtime, "pyhumps-3.8.0.dist-info"); File.WriteAllText(runtimeFile, "generated");
            string temporary = Path.Combine(session.VaultPath, "User", "AppData", "Local", "Temp", "install.lock"); File.WriteAllText(temporary, "temporary");
            string documentRuntime = Path.Combine(session.VaultPath, "User", "Documents", "runtimes", "codex-runtime-install-user"); Directory.CreateDirectory(documentRuntime);
            File.WriteAllText(Path.Combine(documentRuntime, "keep.txt"), "user document");
            string durable = Path.Combine(session.VaultPath, "User", "AppData", "Roaming", "Codex", "Local Storage", string.Join(Path.DirectorySeparatorChar, Enumerable.Repeat(new string('d', 36), 7)));
            Directory.CreateDirectory(durable); File.WriteAllText(Path.Combine(durable, "profile.txt"), "durable long-path data");
            using (var runtimeLock = new FileStream(runtimeFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (var tempLock = new FileStream(temporary, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Check(manager.SaveProfile(session.Id, "Work", null, out error), error);
            var work = SaveBoxManager.ListSnapshots(true).Single(p => p.AppPath == exe && p.ProfileName == "Work"); profileIds.Add(work.ProfileId);
            string copiedRoot = Path.Combine(work.FolderPath, "Data");
            Check(!Directory.Exists(Path.Combine(copiedRoot, Path.GetRelativePath(session.VaultPath, runtime))), "Generated runtime was copied into a saved profile.");
            Check(File.Exists(Path.Combine(copiedRoot, Path.GetRelativePath(session.VaultPath, durable), "profile.txt")), "Long-path app profile data was lost.");
            Check(File.Exists(Path.Combine(copiedRoot, Path.GetRelativePath(session.VaultPath, documentRuntime), "keep.txt")), "Generated-folder exclusion removed an ordinary user document.");
            Console.WriteLine("PASS Saves skip locked installer/temp files, keep long-path profile data and preserve similarly named user folders.");
            File.WriteAllText(marker, "personal");
            Check(manager.SaveProfile(session.Id, "Personal", null, out error), error);
            var personal = SaveBoxManager.ListSnapshots(true).Single(p => p.AppPath == exe && p.ProfileName == "Personal"); profileIds.Add(personal.ProfileId);
            Check(!manager.SaveProfile(session.Id, "work", null, out error), "Name collision silently replaced a profile.");
            Check(!manager.SaveProfile(genericSession.Id, "Work", work.ProfileId, out error), "Replacement crossed app identity.");
            Check(manager.LaunchNew(exe, true, out error, work.ProfileId), error);
            var workSession = manager.Snapshot().Sessions.Last();
            Check(workSession.ProfileName == "Work" && File.ReadAllText(Path.Combine(workSession.VaultPath, "User", "profile-choice.txt")) == "work", "Work profile restored wrong data or label.");
            Check(manager.LaunchNew(exe, true, out error, personal.ProfileId), error);
            var personalSession = manager.Snapshot().Sessions.Last();
            Check(File.ReadAllText(Path.Combine(personalSession.VaultPath, "User", "profile-choice.txt")) == "personal", "Personal profile restored wrong data.");
            Check(workSession.VaultPath != personalSession.VaultPath, "Named restores shared a live folder.");
            string locked = Path.Combine(session.VaultPath, "User", "locked.txt"); File.WriteAllText(locked, "locked");
            using (var held = new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Check(!manager.SaveProfile(session.Id, "Work", work.ProfileId, out error), "Failed named replacement reported success.");
            Check(error.Contains("app is still open", StringComparison.OrdinalIgnoreCase) && !error.Contains(session.VaultPath), "Save failure requested app closure or leaked an unexplained raw file path.");
            Check(File.ReadAllText(Path.Combine(work.FolderPath, "Data", "User", "profile-choice.txt")) == "work", "Failed save damaged previous profile.");
            File.Delete(locked);
            Check(manager.SaveProfile(session.Id, "Work", work.ProfileId, out error), error);
            Check(File.ReadAllText(Path.Combine(work.FolderPath, "Data", "User", "profile-choice.txt")) == "personal", "Explicit replacement did not update chosen profile.");
            Check(!File.Exists(Path.Combine(work.FolderPath, "Data", ".multibox-session.json")), "Save copied vault ownership.");
            Check(SaveBoxManager.DeleteProfile(work.ProfileId) && SaveBoxManager.GetProfile(personal.ProfileId) != null, "Deleting Work removed Personal.");
            Console.WriteLine("PASS Work/Personal copies restore independently; collisions, cross-app replacements and failed writes preserve existing profiles.");
            using (var seed = new EphemeralVault())
            {
                string target = Path.Combine(seed.RoamingAppData, Path.GetFileNameWithoutExtension(quiet)); Directory.CreateDirectory(target);
                File.WriteAllText(Path.Combine(target, "old.txt"), "restored data only");
                Check(SaveBoxManager.SaveProfile(Path.GetFileName(quiet), quiet, seed.RootPath, "Seeded", null, out error), error);
            }
            var seeded = SaveBoxManager.ListSnapshots(true).Single(p => p.AppPath == quiet); profileIds.Add(seeded.ProfileId);
            Check(manager.LaunchNew(quiet, true, out error, seeded.ProfileId), error);
            var quietSession = manager.Snapshot().Sessions.Last();
            await Task.Delay(600);
            Check(manager.GetEngine(quietSession.Id)!.ProfileCheck.Status == "checking", "Restored files were mistaken for new app activity.");
            Console.WriteLine("PASS Restored files alone never confirm profile compatibility.");
            Directory.CreateDirectory(Path.Combine(legacy, "Data", "User"));
            File.WriteAllText(Path.Combine(legacy, "Data", "User", "legacy.txt"), "old copy");
            string manifest = JsonSerializer.Serialize(new { AppName = "Legacy-" + token, AppPath = exe, SavedAt = "Earlier" });
            File.WriteAllText(Path.Combine(legacy, "manifest.json"), manifest);
            var old = SaveBoxManager.ListSnapshots(true).Single(p => p.FolderPath == legacy); profileIds.Add(old.ProfileId);
            Check(old.ProfileName == "Default" && old.ProfileId.StartsWith("legacy-"), "Legacy profile did not load as Default.");
            Check(SaveBoxManager.ListSnapshots(true).Single(p => p.FolderPath == legacy).ProfileId == old.ProfileId, "Legacy identity changed across reads.");
            Check(File.ReadAllText(Path.Combine(legacy, "manifest.json")) == manifest, "Reading legacy profile rewrote it.");
            using (var destination = new EphemeralVault())
                Check(SaveBoxManager.RestoreProfile(old.ProfileId, destination.RootPath) && File.Exists(Path.Combine(destination.UserProfile, "legacy.txt")), "Legacy profile could not restore.");
            Console.WriteLine("PASS Existing saved profiles load as Default without rewriting the original manifest.");
            Directory.CreateDirectory(recoveryRoot);
            string report = Path.Combine(recoveryRoot, "owner-result.txt");
            var info = new ProcessStartInfo(Path.Combine(fixtureDirectory, "SessionFixture.exe")) { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("--vault-owner"); info.ArgumentList.Add(recoveryRoot); info.ArgumentList.Add(report);
            using var helper = Process.Start(info)!;
            try
            {
                await Until(() => File.Exists(report) && File.ReadAllText(report).Length > 0, "Crash fixture did not provision vault.");
                string abandoned = File.ReadAllText(report);
                string savedCanary = Path.Combine(recoveryRoot, "SaveBox", "keep.txt"); Directory.CreateDirectory(Path.GetDirectoryName(savedCanary)!); File.WriteAllText(savedCanary, "saved");
                string unknown = Path.Combine(recoveryRoot, "aaaaaaaaaaaa"); Directory.CreateDirectory(unknown); File.WriteAllText(Path.Combine(unknown, "keep.txt"), "unowned");
                using var active = new EphemeralVault(recoveryRoot);
                Check(VaultRecovery.Cleanup(recoveryRoot).ActiveKept == 2 && Directory.Exists(abandoned), "Cleanup removed a live session.");
                helper.Kill(true); helper.WaitForExit();
                // Locked file leaves an owned, recoverable folder; retry finishes after release.
                string canary = Path.Combine(abandoned, "User", "crash-canary.txt");
                using (var held = new FileStream(canary, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    var failed = VaultRecovery.Cleanup(recoveryRoot);
                    Check(failed.Failed == 1 && failed.Removed == 0, "Locked cleanup did not report failure.");
                }
                var clean = VaultRecovery.Cleanup(recoveryRoot);
                Check(clean.Removed == 1 && clean.ActiveKept == 1 && clean.UnknownKept == 1, "Recovery did not remove exactly the abandoned owner: " + JsonSerializer.Serialize(clean) + " folders=" + string.Join(",", Directory.GetDirectories(recoveryRoot).Select(Path.GetFileName)));
                Check(File.ReadAllText(savedCanary) == "saved" && File.Exists(Path.Combine(unknown, "keep.txt")) && Directory.Exists(active.RootPath), "Crash cleanup damaged kept data.");
                Check(VaultRecovery.Cleanup(recoveryRoot).Removed == 0, "Repeated cleanup removed additional data.");
                Console.WriteLine("PASS Real process crash recovery keeps live, saved and unknown data; locked files remain retryable.");
            }
            finally { if (!helper.HasExited) { helper.Kill(true); helper.WaitForExit(); } }
        }
        finally
        {
            manager.Dispose();
            foreach (string id in profileIds) SaveBoxManager.DeleteProfile(id);
            foreach (string file in new[] { exe, quiet, generic }) { File.Delete(file); File.Delete(SaveBoxManager.SaveDiagnosticPath(file)); }
            // These are exact per-run test directories, never application/user profile roots.
            if (Directory.Exists(recoveryRoot)) Directory.Delete(recoveryRoot, true);
            if (Directory.Exists(legacy)) Directory.Delete(legacy, true);
        }
    }
}
