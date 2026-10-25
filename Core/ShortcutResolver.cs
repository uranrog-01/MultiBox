using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultiBox.Core
{
    public static class ShortcutResolver
    {
        public static string Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            // Trim quotes if wrapped
            path = path.Trim('\"', '\'');

            // 1. If path doesn't exist as given, search common desktop and system locations
            if (!File.Exists(path))
            {
                string? discovered = FindInCommonLocations(path);
                if (discovered != null)
                {
                    path = discovered;
                }
            }

            // 2. If still missing, an app update may have replaced the Store package or a versioned
            //    install folder; the same executable usually exists in the new location.
            if (!File.Exists(path))
            {
                string? healed = ResolveUpdatedInstall(path);
                if (healed != null)
                {
                    path = healed;
                }
            }

            // 3. If path is a directory, look for main executable inside
            if (Directory.Exists(path))
            {
                string chatGptExe = Path.Combine(path, "ChatGPT.exe");
                if (File.Exists(chatGptExe)) return chatGptExe;

                string[] exes = Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly);
                if (exes.Length > 0)
                {
                    // Prioritize non-helpers
                    foreach (var exe in exes)
                    {
                        string fn = Path.GetFileName(exe).ToLowerInvariant();
                        if (fn.Contains("chatgpt") || fn.Contains("app") || fn.Contains("main"))
                        {
                            return exe;
                        }
                    }
                    return exes[0];
                }
            }

            // 4. If target is Codex.exe trampoline in OpenAI folder, switch to ChatGPT.exe
            if (File.Exists(path) && Path.GetFileName(path).Equals("Codex.exe", StringComparison.OrdinalIgnoreCase))
            {
                string? dir = Path.GetDirectoryName(path);
                if (dir != null)
                {
                    string siblingChatGpt = Path.Combine(dir, "ChatGPT.exe");
                    if (File.Exists(siblingChatGpt)) return siblingChatGpt;
                }
            }

            // 5. If it's a .lnk shortcut, resolve target executable
            if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            {
                try
                {
                    Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic? shell = Activator.CreateInstance(shellType);
                        if (shell != null)
                        {
                            dynamic shortcut = shell.CreateShortcut(path);
                            string rawTarget = (string)shortcut.TargetPath;
                            if (!string.IsNullOrWhiteSpace(rawTarget))
                            {
                                string expanded = Environment.ExpandEnvironmentVariables(rawTarget);
                                if (File.Exists(expanded))
                                {
                                    // Check if shortcut pointed to Codex.exe helper
                                    if (Path.GetFileName(expanded).Equals("Codex.exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string? dir = Path.GetDirectoryName(expanded);
                                        if (dir != null)
                                        {
                                            string siblingChatGpt = Path.Combine(dir, "ChatGPT.exe");
                                            if (File.Exists(siblingChatGpt)) return siblingChatGpt;
                                        }
                                    }
                                    return expanded;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to original path
                }
            }

            return path;
        }

        /// <summary>
        /// Recovers a path whose target vanished because the app was updated: Microsoft Store packages
        /// live in versioned folders (and Store apps have no classic Start Menu shortcut to fall back on),
        /// and Squirrel-style installs live in "app-&lt;version&gt;" folders. Both are replaced wholesale on update.
        /// </summary>
        private static string? ResolveUpdatedInstall(string path)
        {
            if (TryParseStorePackagePath(path, out string? family, out string? relative))
            {
                string packageFamily = family!;
                string? install = QueryPackageInstallLocation(packageFamily[..packageFamily.LastIndexOf('_')], packageFamily);
                if (install != null)
                {
                    string candidate = relative!.Length == 0 ? install : Path.Combine(install, relative);
                    if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
                    try
                    {
                        string found = Directory.GetFiles(install, Path.GetFileName(path), SearchOption.AllDirectories).FirstOrDefault() ?? "";
                        if (File.Exists(found)) return found;
                    }
                    catch
                    {
                        // Package folders deny listing for some package types.
                    }
                }
            }
            return ResolveVersionedFolderUpdate(path);
        }

        /// <summary>Parses a path into a Store package: the family name ("Name_PublisherId") and the sub-path below the package root.</summary>
        public static bool TryParseStorePackagePath(string path, out string? packageFamily, out string? relativeSubPath)
        {
            packageFamily = null;
            relativeSubPath = null;
            if (string.IsNullOrWhiteSpace(path)) return false;
            string full;
            try { full = Path.GetFullPath(path); }
            catch { return false; }
            string[] segments = full.Split('\\', '/');
            int index = Array.FindLastIndex(segments, s => s.Equals("WindowsApps", StringComparison.OrdinalIgnoreCase));
            if (index < 0 || index + 1 >= segments.Length) return false;
            // Package folders are "<Name>_<Version>_<Architecture>[_<ResourceId>]__<PublisherId>".
            string folder = segments[index + 1];
            string[] parts = folder.Split('_');
            if (parts.Length < 4 || !folder.Contains("__", StringComparison.Ordinal)) return false;
            string name = parts[0];
            string publisherId = parts[^1];
            if (name.Length == 0 || publisherId.Length == 0 || publisherId == name) return false;
            packageFamily = name + "_" + publisherId;
            relativeSubPath = string.Join('\\', segments[(index + 2)..]);
            return true;
        }

        /// <summary>Queries the current install location of a Store package, optionally filtered to one exact package family.</summary>
        internal static string? QueryPackageInstallLocation(string packageName, string? packageFamily = null)
        {
            try
            {
                string filter = packageFamily == null ? $"{{ $_.Name -eq '{packageName}' }}" : $"{{ $_.PackageFamilyName -eq '{packageFamily}' }}";
                var info = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"Get-AppxPackage -Name '{packageName}' | Where-Object {filter} | Select-Object -First 1 | ForEach-Object {{ $_.InstallLocation }}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var process = Process.Start(info);
                if (process == null) return null;
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(15000);
                if (output.Length == 0 || output.Contains('\n') || output.Contains('\r')) return null;
                return Directory.Exists(output) ? output : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Finds the same executable in the newest "app-&lt;version&gt;" (or bare version) sibling folder.</summary>
        private static string? ResolveVersionedFolderUpdate(string path)
        {
            try
            {
                string full = Path.GetFullPath(path);
                string? versionedFolder = Path.GetDirectoryName(full);
                if (versionedFolder == null) return null;
                if (!TrySplitVersionSuffix(Path.GetFileName(versionedFolder), out string prefix, out Version staleVersion)) return null;
                string? root = Path.GetDirectoryName(versionedFolder);
                if (root == null || !Directory.Exists(root)) return null;
                string remainder = full.Length > versionedFolder.Length ? full[(versionedFolder.Length + 1)..] : "";
                string? newest = null;
                Version newestVersion = staleVersion;
                foreach (string directory in Directory.GetDirectories(root))
                {
                    string name = Path.GetFileName(directory);
                    if (!TrySplitVersionSuffix(name, out string candidatePrefix, out Version candidate)) continue;
                    if (!candidatePrefix.Equals(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    if (candidate <= staleVersion) continue;
                    if (newest == null || candidate > newestVersion) { newestVersion = candidate; newest = directory; }
                }
                if (newest == null) return null;
                string healed = remainder.Length == 0 ? newest : Path.Combine(newest, remainder);
                if (File.Exists(healed) || Directory.Exists(healed)) return healed;
                try
                {
                    string found = Directory.GetFiles(newest, Path.GetFileName(full), SearchOption.AllDirectories).FirstOrDefault() ?? "";
                    if (File.Exists(found)) return found;
                }
                catch
                {
                    // Structure changed in a way we cannot enumerate.
                }
            }
            catch
            {
                // Not a filesystem path we can reason about.
            }
            return null;
        }

        /// <summary>Splits a trailing version number from a folder name: "app-1.2.3" → "app-" + 1.2.3, "1.2.3" → "" + 1.2.3.</summary>
        public static bool TrySplitVersionSuffix(string name, out string prefix, out Version version)
        {
            prefix = "";
            version = new Version(0, 0);
            Match match = Regex.Match(name, @"^(?<prefix>.*?)(?<version>\d+(?:\.\d+){1,3})$");
            if (!match.Success) return false;
            prefix = match.Groups["prefix"].Value;
            return Version.TryParse(match.Groups["version"].Value, out version!);
        }

        private static string? FindInCommonLocations(string filename)
        {
            string nameOnly = Path.GetFileName(filename);

            string[] candidateDirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), // C:\Users\Public\Desktop
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                AppDomain.CurrentDomain.BaseDirectory,
                Directory.GetCurrentDirectory()
            };

            foreach (var dir in candidateDirs)
            {
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;

                // Check exact name
                string directPath = Path.Combine(dir, nameOnly);
                if (File.Exists(directPath)) return directPath;

                // Check .lnk variant if .exe was passed, or .exe variant if .lnk was passed
                if (!nameOnly.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    string lnkVariant = Path.Combine(dir, Path.GetFileNameWithoutExtension(nameOnly) + ".lnk");
                    if (File.Exists(lnkVariant)) return lnkVariant;
                }
            }

            return null;
        }
    }
}
