using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiBox.Setup;

/// <summary>Console-style headless modes: --help, --version, --silent, --extract-to, --uninstall.</summary>
public static class Headless
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    public static int Run(CliOptions cli)
    {
        try { AttachConsole(ATTACH_PARENT_PROCESS); } catch { }

        try
        {
            if (cli.HelpRequested)
            {
                PrintUsage();
                return 0;
            }
            if (cli.VersionRequested)
            {
                Say($"MultiBox Setup {VersionString()}");
                return 0;
            }
            if (cli.UninstallRequested)
            {
                return LaunchUninstaller();
            }
            if (cli.ExtractTo is not null)
            {
                // Run off the dispatcher thread: awaiting on it would capture a blocked context.
                return Task.Run(() => ExtractAsync(cli.ExtractTo)).GetAwaiter().GetResult();
            }
            if (cli.Silent)
            {
                return Task.Run(() => SilentInstallAsync(cli)).GetAwaiter().GetResult();
            }
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
        catch (Exception ex)
        {
            SetupLog.Error("Headless mode failed", ex);
            Say($"Setup failed: {ex.Message}");
            return 1;
        }
        finally
        {
            try
            {
                Say(string.Empty);
                FreeConsole();
            }
            catch { }
        }
    }

    private static string VersionString()
    {
        try
        {
            if (Payload.IsEmbedded)
            {
                var m = Payload.LoadManifest();
                if (!string.IsNullOrEmpty(m.Version)) return m.Version;
            }
        }
        catch { }
        var asm = System.Reflection.Assembly.GetExecutingAssembly().GetName();
        return asm.Version?.ToString() ?? "0.0.0";
    }

    private static void Say(string message)
    {
        try { Console.Out.WriteLine(message); } catch { }
    }

    private static void PrintUsage()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"MultiBox Setup {VersionString()} — installs MultiBox on this PC.");
        sb.AppendLine();
        sb.AppendLine("Usage:");
        sb.AppendLine("  MultiBox.Setup.exe                         Run the graphical setup wizard");
        sb.AppendLine("  MultiBox.Setup.exe --silent                Install with defaults, no UI");
        sb.AppendLine("  MultiBox.Setup.exe --install-to <path>     Install into <path> (with --silent or as the wizard default)");
        sb.AppendLine("  MultiBox.Setup.exe --all-users             Per-machine install into Program Files (needs elevation)");
        sb.AppendLine("  MultiBox.Setup.exe --no-desktop-shortcut   Skip the desktop shortcut");
        sb.AppendLine("  MultiBox.Setup.exe --no-start-menu         Skip the Start menu folder");
        sb.AppendLine("  MultiBox.Setup.exe --extract-to <path>     Extract the portable app payload only (no shortcuts, no registry)");
        sb.AppendLine("  MultiBox.Setup.exe --uninstall             Launch the installed app's uninstaller");
        sb.AppendLine("  MultiBox.Setup.exe --version               Print the payload version");
        sb.AppendLine("  MultiBox.Setup.exe --help                  Show this help");
        sb.AppendLine();
        sb.AppendLine("Exit codes: 0 success · 1 failure · 2 no payload · 3 extraction error · 4 invalid target · 5 elevation required");
        Say(sb.ToString());
    }

    private static int LaunchUninstaller()
    {
        var installed = InstallTargets.Detect();
        string exePath = installed is not null ? Path.Combine(installed.InstallDir, "MultiBox.exe") : InstallTargets.DefaultPerUserDir + "\\MultiBox.exe";
        if (!File.Exists(exePath))
        {
            Say("MultiBox is not installed.");
            SetupLog.Warn($"--uninstall: no MultiBox.exe at {exePath}");
            return 4;
        }
        Process.Start(new ProcessStartInfo(exePath, "--uninstall") { UseShellExecute = true });
        return 0;
    }

    private sealed class ConsoleProgress : IProgress<InstallProgress>
    {
        private int _lastPercent = -1;
        public void Report(InstallProgress p)
        {
            int percent = (int)Math.Round(p.Fraction * 100);
            if (percent == _lastPercent && p.CurrentFile is not null) return;
            _lastPercent = percent;
            Say($"[{percent,3}%] {p.Stage}{(p.CurrentFile is null ? "" : $"  {p.CurrentFile}")}");
        }
    }

    private static async Task<int> ExtractAsync(string target)
    {
        if (!Payload.IsEmbedded)
        {
            Say("This setup package has no embedded payload. Rebuild with build-setup.ps1.");
            return 2;
        }
        try
        {
            await InstallEngine.ExtractPortableAsync(target, new ConsoleProgress(), CancellationToken.None);
            Say($"Extracted MultiBox {VersionString()} to {target}");
            return 0;
        }
        catch (Exception ex)
        {
            SetupLog.Error("Portable extraction failed", ex);
            Say($"Extraction failed: {ex.Message}");
            return 3;
        }
    }

    private static async Task<int> SilentInstallAsync(CliOptions cli)
    {
        if (!Payload.IsEmbedded)
        {
            Say("This setup package has no embedded payload. Rebuild with build-setup.ps1.");
            return 2;
        }

        string dir;
        bool forAllUsers = cli.ForAllUsers;
        if (cli.InstallTo is not null)
        {
            dir = cli.InstallTo;
        }
        else if (forAllUsers)
        {
            dir = InstallTargets.AllUsersDir;
        }
        else
        {
            dir = InstallTargets.DefaultPerUserDir;
        }

        var validation = InstallTargets.Validate(dir);
        if (validation is not null)
        {
            Say($"Invalid install target: {validation}");
            return 4;
        }

        if (forAllUsers && !IsElevated())
        {
            Say("An all-users install requires an elevated prompt. Run the setup from an administrator terminal, or omit --all-users.");
            return 5;
        }

        if (InstallEngine.IsMultiBoxRunning())
        {
            Say("MultiBox is running — closing it before installing.");
            InstallEngine.CloseMultiBox();
        }

        var options = new InstallOptions(dir, cli.DesktopShortcut, cli.StartMenuShortcut, forAllUsers);

        Say($"Installing MultiBox {VersionString()} to {dir} …");
        var result = await InstallEngine.InstallAsync(options, new ConsoleProgress(), CancellationToken.None);
        if (result.Success)
        {
            Say($"MultiBox {result.Version} installed successfully.");
            return 0;
        }
        Say(result.Error == "cancelled" ? "Installation cancelled." : $"Installation failed: {result.Error}");
        return result.Error == "cancelled" ? 130 : 1;
    }

    private static bool IsElevated()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
