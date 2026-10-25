using MultiBox.Core;

// Read-only diagnostic: for every saved profile, report whether its stored path is broken,
// what the automatic resolver would heal it to, and what the Fix-Profile hunt finds. Writes nothing.
var profiles = SaveBoxManager.ListSnapshots(true);
if (profiles.Count == 0) { Console.WriteLine("No saved profiles."); return; }
foreach (var profile in profiles)
{
    Console.WriteLine($"Profile '{profile.ProfileName}' for {profile.AppName}");
    Console.WriteLine($"  stored: {profile.AppPath}");
    try
    {
        string resolved = ShortcutResolver.Resolve(profile.AppPath);
        bool usable = File.Exists(resolved) || Directory.Exists(resolved);
        Console.WriteLine($"  resolver: {resolved} (usable: {usable})");
    }
    catch (Exception ex) { Console.WriteLine($"  resolver failed: {ex.Message}"); }
    HealCandidate[] found = await ProfileHealer.HuntAsync(profile.AppName, profile.AppPath, ProfileHealer.Depth.Quick,
        new Progress<string>(text => Console.WriteLine($"  … {text}")));
    if (found.Length == 0) { Console.WriteLine("  hunt: nothing found"); continue; }
    foreach (var candidate in found.Take(5)) Console.WriteLine($"  hunt [{candidate.Score}] {candidate.Source}: {candidate.ExePath}");
}
