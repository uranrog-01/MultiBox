using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace MultiBox.Core
{
    public static class ContextMenuManager
    {
        private const string MenuName = "MultiBoxIsolated";
        private const string MenuText = "Run in Isolated Mode (MultiBox)";

        private static readonly string[] Targets = new[]
        {
            @"Software\Classes\exefile\shell",
            @"Software\Classes\lnkfile\shell"
        };

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\exefile\shell\" + MenuName);
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetEnabled(bool enable)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName 
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MultiBox.exe");

                if (!File.Exists(exePath))
                {
                    string rootExe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MultiBox.exe"));
                    if (File.Exists(rootExe)) exePath = rootExe;
                }

                foreach (var target in Targets)
                {
                    if (enable)
                    {
                        using var shellKey = Registry.CurrentUser.CreateSubKey(target, true);
                        if (shellKey != null)
                        {
                            using var menuKey = shellKey.CreateSubKey(MenuName, true);
                            if (menuKey != null)
                            {
                                menuKey.SetValue("", MenuText);
                                menuKey.SetValue("Icon", $"\"{exePath}\",0");

                                using var cmdKey = menuKey.CreateSubKey("command", true);
                                cmdKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                            }
                        }
                    }
                    else
                    {
                        using var shellKey = Registry.CurrentUser.OpenSubKey(target, true);
                        shellKey?.DeleteSubKeyTree(MenuName, false);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContextMenuManager Error] {ex.Message}");
                return false;
            }
        }
    }
}

