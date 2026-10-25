using System.Windows;

namespace MultiBox.Setup;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, ex) =>
        {
            SetupLog.Error("Unhandled UI exception", ex.Exception);
            MessageBox.Show(
                $"{ex.Exception.Message}\n\nDetails were written to the setup log.",
                "MultiBox Setup", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
            Shutdown(1);
        };

        // Headless modes (--silent / --extract-to / --help / --uninstall passthrough) run without a window.
        if (CliOptions.TryParse(e.Args, out CliOptions? cli) && cli.Headless)
        {
            int exitCode = Headless.Run(cli);
            Shutdown(exitCode);
            return;
        }

        base.OnStartup(e);
        var window = new MainWindow(cli);
        window.Show();
    }
}
