using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MultiBox.Setup;

public partial class FinishPage : UserControl
{
    public bool LaunchAfter => LaunchCheck.IsChecked == true;

    public FinishPage(InstallResult? result)
    {
        InitializeComponent();
        if (result is not null)
        {
            HeadlineText.Text = "MultiBox is ready.";
            SubText.Text = string.IsNullOrEmpty(result.Version)
                ? "Everything is installed and registered."
                : $"Version {result.Version} is installed and registered.";
            InstallPathText.Text = result.ExePath is not null ? Path.GetDirectoryName(result.ExePath) : null;
        }
    }
}
