using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MultiBox.Setup;

public partial class DestinationPage : UserControl
{
    public Action<ElevationRequest>? RequestElevation;

    public sealed record ElevationRequest(string InstallDir, bool DesktopShortcut, bool StartMenuShortcut);

    private static string _perUserDefault = InstallTargets.DefaultPerUserDir;
    private static string _allUsersDefault = InstallTargets.AllUsersDir;
    private bool _suspendPathEvents;
    private bool _userEditedPath;

    public string InstallDir
    {
        get
        {
            try { return Path.GetFullPath(PathBox.Text.Trim()); }
            catch { return PathBox.Text.Trim(); }
        }
    }

    public bool DesktopShortcut => DesktopShortcutCheck.IsChecked == true;
    public bool StartMenuShortcut => StartMenuCheck.IsChecked == true;
    public bool LaunchAfter => LaunchCheck.IsChecked == true;
    public bool ForAllUsers => AllUsersCheck.IsChecked == true;

    public DestinationPage(string initialDir, bool allUsers, bool elevated, InstallTargets.InstalledInfo? existing)
    {
        InitializeComponent();
        PathBox.Text = initialDir;
        AllUsersCheck.IsChecked = allUsers;
        AllUsersCard.Visibility = elevated ? Visibility.Collapsed : Visibility.Visible;
        RefreshDiskInfo();
    }

    public string? Validate() => InstallTargets.Validate(InstallDir);

    public void ShowValidationError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a MultiBox installation folder",
            InitialDirectory = Directory.Exists(InstallDir) ? InstallDir : null,
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            PathBox.Text = dialog.FolderName;
            _userEditedPath = true;
        }
    }

    private void ResetDefault_Click(object sender, RoutedEventArgs e)
    {
        _userEditedPath = false;
        PathBox.Text = AllUsersCheck.IsChecked == true ? _allUsersDefault : _perUserDefault;
    }

    private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suspendPathEvents) return;
        _userEditedPath = true;
        ErrorText.Visibility = Visibility.Collapsed;
        bool hasDefault = string.Equals(PathBox.Text.Trim(), _perUserDefault, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(PathBox.Text.Trim(), _allUsersDefault, StringComparison.OrdinalIgnoreCase);
        DefaultReset.Visibility = hasDefault ? Visibility.Collapsed : Visibility.Visible;
        RefreshDiskInfo();
    }

    private void AllUsers_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        // Follow the user's path choice when they flip the scope, unless they typed a custom folder.
        if (!_userEditedPath)
        {
            _suspendPathEvents = true;
            PathBox.Text = AllUsersCheck.IsChecked == true ? _allUsersDefault : _perUserDefault;
            DefaultReset.Visibility = Visibility.Collapsed;
            _suspendPathEvents = false;
        }
        RefreshDiskInfo();
    }

    private void RefreshDiskInfo()
    {
        string dir = InstallDir;
        long required = 60 * 1024 * 1024; // conservative payload upper bound
        try
        {
            if (Payload.IsEmbedded)
            {
                long total = Payload.LoadManifest().TotalBytes;
                if (total > 0) required = (long)(total * 1.15);
            }
        }
        catch { }

        long free = InstallTargets.FreeBytes(dir);
        if (free < 0)
        {
            DiskText.Text = "Disk space could not be determined.";
            DiskBar.Width = 0;
            return;
        }

        DiskText.Text = $"Requires about {Payload.HumanSize(required)}  ·  {Payload.HumanSize(free)} free on {Path.GetPathRoot(dir)}";
        bool enough = free > required;
        DiskText.Foreground = enough
            ? FindResource("BrushTextDim") as System.Windows.Media.Brush
            : FindResource("BrushDanger") as System.Windows.Media.Brush;

        // Bar shows required share of free space (capped for display).
        double ratio = free > 0 ? Math.Min(required / (double)free, 1.0) : 1.0;
        double trackWidth = DiskBarTrack.ActualWidth > 0 ? DiskBarTrack.ActualWidth : 300;
        DiskBar.Width = Math.Max(10, trackWidth * ratio);
        DiskBar.Background = enough
            ? DiskBar.Background
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x7A, 0x70));
    }
}
