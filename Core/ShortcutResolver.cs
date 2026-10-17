using System;
using System.IO;

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

            // 2. If path is a directory, look for main executable inside
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

            // 3. If target is Codex.exe trampoline in OpenAI folder, switch to ChatGPT.exe
            if (File.Exists(path) && Path.GetFileName(path).Equals("Codex.exe", StringComparison.OrdinalIgnoreCase))
            {
                string? dir = Path.GetDirectoryName(path);
                if (dir != null)
                {
                    string siblingChatGpt = Path.Combine(dir, "ChatGPT.exe");
                    if (File.Exists(siblingChatGpt)) return siblingChatGpt;
                }
            }

            // 4. If it's a .lnk shortcut, resolve target executable
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
