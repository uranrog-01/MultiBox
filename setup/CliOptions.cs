using System.IO;

namespace MultiBox.Setup;

/// <summary>Parsed setup command line. Headless modes run without the wizard UI.</summary>
public sealed class CliOptions
{
    public bool HelpRequested { get; private set; }
    public bool VersionRequested { get; private set; }
    public bool Silent { get; private set; }
    public bool UninstallRequested { get; private set; }
    public bool ElevatedUi { get; private set; }
    public string? ExtractTo { get; private set; }
    public string? InstallTo { get; private set; }
    public bool ForAllUsers { get; private set; }
    public bool DesktopShortcut { get; private set; } = true;
    public bool StartMenuShortcut { get; private set; } = true;

    public bool Headless =>
        HelpRequested || VersionRequested || Silent || UninstallRequested || ExtractTo is not null;

    public static bool TryParse(string[] args, out CliOptions options)
    {
        options = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--help":
                case "-h":
                case "/?":
                    options.HelpRequested = true;
                    break;
                case "--version":
                case "-v":
                    options.VersionRequested = true;
                    break;
                case "--silent":
                case "-s":
                    options.Silent = true;
                    break;
                case "--uninstall":
                    options.UninstallRequested = true;
                    break;
                case "--elevated-ui":
                    options.ElevatedUi = true;
                    options.Silent = false;
                    break;
                case "--extract-to":
                    if (i + 1 >= args.Length) return false;
                    options.ExtractTo = args[++i];
                    break;
                case "--install-to":
                    if (i + 1 >= args.Length) return false;
                    options.InstallTo = args[++i];
                    break;
                case "--all-users":
                    options.ForAllUsers = true;
                    break;
                case "--no-desktop-shortcut":
                    options.DesktopShortcut = false;
                    break;
                case "--no-start-menu":
                    options.StartMenuShortcut = false;
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        SetupLog.Warn($"Unknown option ignored: {arg}");
                    }
                    else if (options.InstallTo is null && arg.Contains(Path.DirectorySeparatorChar))
                    {
                        // Tolerate a bare path (drag & drop onto the setup exe).
                        options.InstallTo = arg;
                    }
                    break;
            }
        }
        return true;
    }
}
