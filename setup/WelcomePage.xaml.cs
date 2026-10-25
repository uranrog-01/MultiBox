using System.Windows;
using System.Windows.Controls;

namespace MultiBox.Setup;

public partial class WelcomePage : UserControl
{
    public WelcomePage(InstallTargets.InstalledInfo? existing)
    {
        InitializeComponent();
        VersionChip.Text = $"MULTIBOX v{SetupVersion.Current} · WINDOWS X64";
        HeroVersion.Text = $"BUNDLE v{SetupVersion.Current}";
        if (existing is not null)
        {
            UpgradeBanner.Visibility = Visibility.Visible;
            UpgradeTitle.Text = existing.Version == SetupVersion.Current
                ? $"MultiBox v{existing.Version} is already installed"
                : $"Upgrade: v{existing.Version} → v{SetupVersion.Current}";
            UpgradeDetail.Text = existing.Version == SetupVersion.Current
                ? "Setup will reinstall the same version in place. Your sessions stay."
                : "Setup will update your installation in place — your sessions stay.";
        }
    }
}
