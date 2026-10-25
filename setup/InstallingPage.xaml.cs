using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MultiBox.Setup;

public partial class InstallingPage : UserControl
{
    private const int MaxLogLines = 200;
    private readonly List<string> _logLines = new();

    public InstallingPage()
    {
        InitializeComponent();
        var pulse = new DoubleAnimation(1, 0.25, TimeSpan.FromMilliseconds(650))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        PulseDot.BeginAnimation(OpacityProperty, pulse);
    }

    public void SetProgress(InstallProgress p)
    {
        StageText.Text = p.Stage + (p.Indeterminate ? "…" : "");
        FileText.Text = p.CurrentFile ?? string.Empty;

        if (p.Indeterminate)
        {
            Progress.IsIndeterminate = true;
            PercentText.Text = "";
        }
        else
        {
            if (Progress.IsIndeterminate) Progress.IsIndeterminate = false;
            Progress.Value = Math.Clamp(p.Fraction, 0, 1);
            PercentText.Text = $"{Math.Round(p.Fraction * 100)}%";
        }
    }

    public void AppendLog(string line)
    {
        _logLines.Add(line);
        if (_logLines.Count > MaxLogLines)
            _logLines.RemoveRange(0, _logLines.Count - MaxLogLines);
        LogText.Text = string.Join(Environment.NewLine, _logLines);
        LogScroller.ScrollToEnd();
    }

    /// <summary>Seeds the log tail with anything already logged before this page appeared.</summary>
    public void AttachLogTail()
    {
        // No historical buffer is kept by SetupLog; live lines flow in from MainWindow via LineAdded.
    }

    public void ShowError(string message)
    {
        StageText.Text = "Stopped";
        FileText.Text = string.Empty;
        RemainHint.Visibility = Visibility.Collapsed;
        PulseDot.BeginAnimation(OpacityProperty, null);
        PulseDot.Opacity = 1;
        PulseDot.Background = (Brush)FindResource("BrushDanger");
        LogCard.Visibility = Visibility.Collapsed;
        ErrorCard.Visibility = Visibility.Visible;
        ErrorText.Text = message;
    }
}
