using MultiBox.Core;

static class UpdateHealChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static bool Throws<T>(Action action) where T : Exception { try { action(); return false; } catch (T) { return true; } }

    public static Task Run()
    {
        Check(ShortcutResolver.TryParseStorePackagePath(@"C:\Program Files\WindowsApps\OpenAI.Codex_26.1016.8622.0_x64__2p2nqsd0c76g0\app\ChatGPT.exe", out string? family, out string? relative), "A real Store package path was not recognized.");
        Check(family == "OpenAI.Codex_2p2nqsd0c76g0" && relative == "app\\ChatGPT.exe", $"Store parsing produced the wrong family or sub-path: {family} / {relative}");
        Check(ShortcutResolver.TryParseStorePackagePath(@"C:\Program Files\WindowsApps\Some.App_2.0.0.0_neutral__abcd13efghij", out _, out relative) && relative == "", "A Store package root lost its empty sub-path.");
        Check(!ShortcutResolver.TryParseStorePackagePath(@"C:\Tools\MyApp\MyApp.exe", out _, out _), "A non-Store path was parsed as a Store package.");
        Check(!ShortcutResolver.TryParseStorePackagePath(@"C:\Program Files\WindowsApps\", out _, out _), "The WindowsApps root itself parsed as a package.");
        Console.WriteLine("PASS Store package paths resolve to their package family and keep the executable's sub-path.");

        // Hunt engine: matching, junk filtering, ranking and auto-fix confidence.
        Check(ProfileHealer.NameMatch("ChatGPT", "OpenAI Codex — ChatGPT") >= 2, "A display name sharing the app key was not matched.");
        Check(ProfileHealer.NameMatch("MyApp", "Totally Different") == 0, "An unrelated display name matched.");
        Check(!ProfileHealer.IsJunk("MyApp3.exe") && ProfileHealer.IsJunk("unins000.exe") && ProfileHealer.IsJunk("Update.exe"), "Installer and updater executables were not filtered as junk.");
        Check(ProfileHealer.BasenameMatch("MyApp3.exe", "MyApp") >= 28, "A renamed executable was not matched by its shared name.");
        Check(ProfileHealer.BasenameMatch("Uninstall MyTool.exe", "MyTool") == 0, "An uninstaller matched as the app.");
        var ranked = ProfileHealer.Rank(new Dictionary<string, (int Score, string Source, string Label)>(StringComparer.OrdinalIgnoreCase) {
            [@"C:\A\App.exe"] = (50, "Shortcut", "App"),
            [@"C:\A\app.exe"] = (20, "Scan", "App"),
            [@"C:\B\App.exe"] = (60, "Registry", "App") });
        Check(ranked[0].ExePath == @"C:\B\App.exe" && ranked[0].Score == 60 && ranked.Length == 2, "Ranking or deduplication kept the wrong candidates.");
        var tie = ProfileHealer.Rank(new Dictionary<string, (int, string, string)>(StringComparer.OrdinalIgnoreCase) {
            [@"C:\A\App.exe"] = (50, "A", "App"), [@"C:\B\App.exe"] = (50, "B", "App") });
        Check(!ProfileHealer.AutoFixIsSafe(tie), "An ambiguous match was called safe to auto-select.");
        Check(ProfileHealer.AutoFixIsSafe(ProfileHealer.Rank(new Dictionary<string, (int, string, string)> { [@"C:\A\App.exe"] = (60, "A", "App") })), "A confident match was not called safe to auto-select.");
        Console.WriteLine("PASS The hunt engine ranks unique best matches, ignores installers and refuses to auto-select between equals.");

        string root = Path.Combine(Path.GetTempPath(), "MultiBoxUpdateHeal-" + Guid.NewGuid().ToString("N"));
        try
        {
            string oldDir = Path.Combine(root, "MyApp", "app-1.0.0");
            string newDir = Path.Combine(root, "MyApp", "app-2.0.0");
            Directory.CreateDirectory(oldDir);
            Directory.CreateDirectory(newDir);
            string stale = Path.Combine(oldDir, "MyApp.exe");
            string healed = Path.Combine(newDir, "MyApp.exe");
            File.WriteAllText(healed, "new");
            Check(ShortcutResolver.Resolve(stale) == healed, "A versioned install folder was not healed to the newest install.");
            Check(ShortcutResolver.TrySplitVersionSuffix("app-1.2.3", out string prefix, out Version version) && prefix == "app-" && version.Major == 1, "Version folder parsing failed.");
            Check(!ShortcutResolver.TrySplitVersionSuffix("MyApp", out _, out _), "A folder without a version was treated as versioned.");

            Directory.Delete(newDir, true);
            Directory.CreateDirectory(Path.Combine(root, "MyApp", "app-2.0.0", "bin"));
            string moved = Path.Combine(root, "MyApp", "app-2.0.0", "bin", "MyApp.exe");
            File.WriteAllText(moved, "moved");
            Check(ShortcutResolver.Resolve(stale) == moved, "Healing did not follow an executable that moved inside the updated install.");

            string nowhere = Path.Combine(root, "MyApp", "app-3.0.0", "MyApp.exe");
            Check(ShortcutResolver.Resolve(nowhere) == nowhere, "A missing newest version invented a path.");
            Console.WriteLine("PASS Launch paths heal across versioned install folders, including executables that moved, and never invent missing versions.");
        }
        finally { try { Directory.Delete(root, true); } catch { } }

        string token = Guid.NewGuid().ToString("N");
        string appName = "HealTest-" + token + ".exe";
        string vault = Path.Combine(Path.GetTempPath(), "MultiBoxHealVault-" + token);
        string oldPath = Path.Combine(Path.GetTempPath(), "MultiBoxHealOld-" + token, appName);
        string newPath = Path.Combine(Path.GetTempPath(), "MultiBoxHealNew-" + token, appName);
        string otherApp = Path.Combine(Path.GetTempPath(), "MultiBoxHealOther-" + token, "Other-" + token + ".exe");
        try
        {
            Directory.CreateDirectory(vault);
            File.WriteAllText(Path.Combine(vault, "note.txt"), "data");
            Directory.CreateDirectory(Path.GetDirectoryName(oldPath)!); File.WriteAllText(oldPath, "old install");
            Directory.CreateDirectory(Path.GetDirectoryName(newPath)!); File.WriteAllText(newPath, "updated install");
            Directory.CreateDirectory(Path.GetDirectoryName(otherApp)!); File.WriteAllText(otherApp, "other app");

            Check(SaveBoxManager.SaveProfile(appName, oldPath, vault, "Default", null, out string error), error);
            var profile = SaveBoxManager.ListSnapshots(true).Single(p => p.AppName == appName);
            File.Delete(oldPath);
            Check(!File.Exists(profile.AppPath), "Test setup expected the stored executable to be missing.");

            Check(SaveBoxManager.ValidateSaveTarget(newPath, vault, "Default", profile.ProfileId) != null, "An app update blocked replacing its own saved profile.");
            Check(Throws<IOException>(() => SaveBoxManager.ValidateSaveTarget(newPath, vault, "Unknown", "missing-id-" + token)), "Replacing an unknown profile was accepted.");

            Check(SaveBoxManager.TryUpdateProfilePath(profile.ProfileId, newPath, out error), error);
            var healedProfile = SaveBoxManager.ListSnapshots(true).Single(p => p.ProfileId == profile.ProfileId);
            Check(healedProfile.AppPath == newPath && healedProfile.AppName == appName, "Self-heal did not persist the new executable path.");
            Check(!SaveBoxManager.TryUpdateProfilePath("missing-profile-" + token, newPath, out _), "Healing an unknown profile reported success.");

            Check(Throws<IOException>(() => SaveBoxManager.ValidateSaveTarget(otherApp, vault, "Crossed", healedProfile.ProfileId)), "A different existing app replaced a healthy profile.");

            Check(SessionManager.IsProfileAppMismatch(null, newPath), "A missing profile was accepted as a match.");
            Check(!SessionManager.IsProfileAppMismatch(healedProfile, newPath), "Matching paths were called a mismatch.");
            Check(SessionManager.IsProfileAppMismatch(healedProfile, otherApp), "An existing different app passed the profile guard.");
            var staleSnapshot = new SaveBoxSnapshot { ProfileId = profile.ProfileId, AppName = appName, AppPath = oldPath };
            Check(!SessionManager.IsProfileAppMismatch(staleSnapshot, newPath), "A same-named executable at a moved location was rejected instead of healing.");
            Check(SessionManager.IsProfileAppMismatch(new SaveBoxSnapshot { ProfileId = profile.ProfileId, AppName = appName, AppPath = newPath }, otherApp + ".renamed"), "A renamed executable passed the profile guard.");
            Console.WriteLine("PASS Saved profiles self-heal their executable path after updates and stale profiles can be replaced, while healthy profiles still reject foreign apps.");
        }
        finally
        {
            foreach (var saved in SaveBoxManager.ListSnapshots(true).Where(p => p.AppName == appName))
                SaveBoxManager.DeleteProfile(saved.ProfileId);
            foreach (string dir in new[] { vault, Path.GetDirectoryName(oldPath)!, Path.GetDirectoryName(newPath)!, Path.GetDirectoryName(otherApp)! })
                try { Directory.Delete(dir, true); } catch { }
        }
        return Task.CompletedTask;
    }
}
