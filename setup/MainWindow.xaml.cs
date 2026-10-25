using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.Win32;

namespace MultiBox.Setup;

public partial class MainWindow : Window
{
    private enum Step { Welcome = 0, Destination = 1, Installing = 2, Finish = 3 }

    private const int StepCount = 4;

    private readonly CliOptions _cli;
    private readonly bool _elevated;
    private readonly InstallTargets.InstalledInfo? _existing;

    private Step _current = Step.Welcome;
    private bool _installing;
    private CancellationTokenSource? _cts;
    private InstallResult? _installResult;

    private WelcomePage? _welcomePage;
    private DestinationPage? _destinationPage;
    private InstallingPage? _installingPage;
    private FinishPage? _finishPage;

    public MainWindow(CliOptions? cli)
    {
        _cli = cli ?? new CliOptions();
        _elevated = _cli.ElevatedUi;
        _existing = InstallTargets.Detect();
        InitializeComponent();

        try
        {
            Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/Assets/icon.ico") as ImageSource;
        }
        catch { /* icon is cosmetic */ }

        VersionChipText.Text = $"v{SetupVersion.Current}";

        if (_elevated)
        {
            _current = Step.Destination;
        }
        GoTo(_current, animate: false);
    }

    private static string PayloadVersion
    {
        get
        {
            try
            {
                if (Payload.IsEmbedded)
                {
                    string? v = Payload.LoadManifest().Version;
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            catch { }
            return SetupVersion.Current;
        }
    }

    // ======== Navigation ========

    private void GoTo(Step step, bool animate = true)
    {
        _current = step;
        UserControl page = step switch
        {
            Step.Welcome => _welcomePage = new WelcomePage(_existing),
            Step.Destination => _destinationPage = CreateDestinationPage(),
            Step.Installing => _installingPage = new InstallingPage(),
            Step.Finish => _finishPage = new FinishPage(_installResult),
            _ => throw new ArgumentOutOfRangeException(nameof(step)),
        };

        ContentHost.Children.Clear();
        ContentHost.Children.Add(page);
        if (animate)
        {
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            page.BeginAnimation(OpacityProperty, fade);
            var slide = new ThicknessAnimation(new Thickness(0, 10, 0, 0), new Thickness(0), TimeSpan.FromMilliseconds(240))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            page.BeginAnimation(MarginProperty, slide);
        }

        UpdateStepDots();
        UpdateFooter();
    }

    private DestinationPage CreateDestinationPage()
    {
        string initialDir = _cli.InstallTo
            ?? (_elevated || _cli.ForAllUsers ? InstallTargets.AllUsersDir : InstallTargets.DefaultPerUserDir);
        bool allUsers = _elevated || _cli.ForAllUsers;
        var page = new DestinationPage(initialDir, allUsers, _elevated, _existing);
        page.RequestElevation = request =>
        {
            var args = new List<string>
            {
                "--elevated-ui",
                $"--install-to \"{request.InstallDir}\"",
                "--all-users"
            };
            if (!request.DesktopShortcut) args.Add("--no-desktop-shortcut");
            if (!request.StartMenuShortcut) args.Add("--no-start-menu");
            if (InstallEngine.RelaunchElevated(string.Join(' ', args)))
            {
                Close();
            }
            else
            {
                MessageBox.Show(this,
                    "Administrator permission is required to install for all users.\nSetup stays open — try again or install for your user only.",
                    "MultiBox Setup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        };
        return page;
    }

    private void UpdateFooter()
    {
        bool isInstallStep = _current == Step.Installing;
        BackButton.Visibility = _current == Step.Destination && !_installing ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Visibility = _current is Step.Installing ? Visibility.Collapsed : Visibility.Visible;
        CancelButton.Visibility = isInstallStep ? Visibility.Visible : Visibility.Collapsed;

        NextButton.Content = _current switch
        {
            Step.Welcome => "Get started",
            Step.Destination => _existing is not null ? "Upgrade" : "Install",
            Step.Finish => "Finish",
            _ => "Continue",
        };
        NextButton.IsEnabled = true;
    }

    private void UpdateStepDots()
    {
        StepHost.Children.Clear();
        string[] labels = { "Welcome", "Location", "Install", "Finish" };
        for (int i = 0; i < StepCount; i++)
        {
            bool active = (int)_current == i;
            bool done = (int)_current > i;
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 22, 0) };
            var dot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                VerticalAlignment = VerticalAlignment.Center,
                Background = active ? new SolidColorBrush(Color.FromRgb(0xBA, 0xFF, 0x2A))
                    : done ? new SolidColorBrush(Color.FromArgb(0x66, 0xBA, 0xFF, 0x2A))
                    : new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF))
            };
            if (active)
            {
                dot.Effect = (System.Windows.Media.Effects.Effect)FindResource("EffectLimeGlow");
            }
            row.Children.Add(dot);
            row.Children.Add(new TextBlock
            {
                Text = labels[i],
                FontSize = 10.5,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = active ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xEA))
                    : done ? new SolidColorBrush(Color.FromRgb(0x9A, 0xA6, 0x9E))
                    : new SolidColorBrush(Color.FromRgb(0x5E, 0x6B, 0x62))
            });
            StepHost.Children.Add(row);
        }
    }

    // ======== Footer handlers ========

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_installing) return;
        if (_current == Step.Destination) GoTo(Step.Welcome);
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        switch (_current)
        {
            case Step.Welcome:
                GoTo(Step.Destination);
                break;

            case Step.Destination:
            {
                var page = _destinationPage!;
                string? error = page.Validate();
                if (error is not null)
                {
                    page.ShowValidationError(error);
                    return;
                }
                if (page.ForAllUsers && !_elevated)
                {
                    page.RequestElevation?.Invoke(new DestinationPage.ElevationRequest(
                        page.InstallDir, page.DesktopShortcut, page.StartMenuShortcut));
                    return;
                }                StartInstall();
                break;
            }

            case Step.Finish:
                if (_finishPage?.LaunchAfter == true && _installResult?.ExePath is { } exe && System.IO.File.Exists(exe))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        SetupLog.Warn($"Launch after install failed: {ex.Message}");
                    }
                }
                Close();
                break;
        }
    }

    // ======== Install flow ========

    private void StartInstall()
    {
        var page = _destinationPage!;
        var options = new InstallOptions(page.InstallDir, page.DesktopShortcut, page.StartMenuShortcut, page.ForAllUsers);
        _cts = new CancellationTokenSource();
        _installing = true;
        UpdateFooter();
        UpdateStepDots();

        GoTo(Step.Installing);
        SetupLog.LineAdded += OnLogLine;
        _installingPage?.AttachLogTail();

        var progress = new Progress<InstallProgress>(p => Dispatcher.BeginInvoke(() => _installingPage?.SetProgress(p)));

        Task.Run(async () => await InstallEngine.InstallAsync(options, progress, _cts.Token))
            .ContinueWith(t =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _installing = false;
                    SetupLog.LineAdded -= OnLogLine;
                    var result = t.Status == TaskStatus.RanToCompletion
                        ? t.Result
                        : new InstallResult { Success = false, Error = t.Exception?.GetBaseException().Message ?? "unknown error" };
                    _installResult = result;
                    UpdateFooter();
                    if (result.Success)
                    {
                        GoTo(Step.Finish);
                    }
                    else
                    {
                        UpdateStepDots(); // keep Install dot active-ish
                        _installingPage?.ShowError(result.Error ?? "Unknown error.");
                        BackButton.Visibility = Visibility.Visible;
                        NextButton.Visibility = Visibility.Collapsed;
                        CancelButton.Visibility = Visibility.Collapsed;
                    }
                });
            }, TaskScheduler.Default);
    }

    private void OnLogLine(string line) => Dispatcher.BeginInvoke(() => _installingPage?.AppendLog(line));

    private void CancelInstall_Click(object sender, RoutedEventArgs e)
    {
        var choice = MessageBox.Show(this,
            "Cancel the installation? Partially written files will be removed.",
            "MultiBox Setup", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (choice == MessageBoxResult.Yes)
        {
            _cts?.Cancel();
            CancelButton.IsEnabled = false;
        }
    }

    // ======== Titlebar handlers ========

    private void Titlebar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); } catch { }
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_installing)
        {
            CancelInstall_Click(sender, e);
            if (_cts?.IsCancellationRequested != true) return; // user chose to keep going
        }
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_installing)
        {
            e.Cancel = true;
            CancelInstall_Click(this, new RoutedEventArgs());
            return;
        }
        base.OnClosing(e);
    }
}
