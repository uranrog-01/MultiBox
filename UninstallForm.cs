using System.Diagnostics;
using Microsoft.Win32;

namespace MultiBox;

/// <summary>
/// In-app uninstaller (launched via <c>MultiBox.exe --uninstall</c> or the setup's hard-linked Uninstall.exe).
/// Removes shortcuts, the Add/Remove Programs entry, and the install folder.
/// Session data under %LocalAppData%\MultiBox is only removed when explicitly requested.
/// The layout is content-driven (AutoSize rows, growing form) so it renders correctly at any DPI.
/// </summary>
internal sealed class UninstallForm : Form
{
    private static readonly Color Bg = Color.FromArgb(6, 8, 7);
    private static readonly Color Surface = Color.FromArgb(18, 23, 20);
    private static readonly Color TextMain = Color.FromArgb(238, 243, 234);
    private static readonly Color TextDim = Color.FromArgb(147, 160, 143);
    private static readonly Color Accent = Color.FromArgb(186, 255, 42);

    private readonly string _installDir;
    private readonly string _exePath;
    private readonly CheckBox _removeData = new()
    {
        Text = "Also delete my MultiBox sessions and profiles",
        AutoSize = true,
        FlatStyle = FlatStyle.Flat, // System style ignores ForeColor, which vanishes on the dark background
        ForeColor = TextMain,
        BackColor = Bg,
        Cursor = Cursors.Hand,
        Margin = new Padding(2, 8, 0, 4)
    };
    private readonly Label _status = new() { AutoSize = false, Dock = DockStyle.Fill, ForeColor = TextDim, Text = "Ready.", TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _uninstall = new() { Text = "Uninstall MultiBox", AutoSize = true };
    private readonly Button _cancel = new() { Text = "Cancel", AutoSize = true };
    private readonly ProgressBar _progress = new()
    {
        Style = ProgressBarStyle.Marquee,
        MarqueeAnimationSpeed = 18,
        Visible = false,
        Height = 6,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 3, 0, 3)
    };
    private bool _working;

    public UninstallForm()
    {
        _exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "MultiBox.exe");
        _installDir = Path.GetDirectoryName(_exePath) ?? AppContext.BaseDirectory;

        Text = "Uninstall MultiBox";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Bg;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        const int contentWidth = 470;
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            MinimumSize = new Size(contentWidth + 70, 0),
            Padding = new Padding(28, 22, 28, 20),
            ColumnCount = 1,
            BackColor = Bg
        };
        for (int i = 0; i < 7; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Uninstall MultiBox",
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 16f),
            ForeColor = TextMain,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 0, 6)
        };
        title.Size = new Size(contentWidth, TextRenderer.MeasureText(title.Text, title.Font).Height + 10);
        root.Controls.Add(title);

        var blurb = new Label
        {
            Text = "This removes the MultiBox application, its shortcuts, and the Add/Remove Programs entry.",
            AutoSize = false,
            ForeColor = TextDim,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 2, 0, 8)
        };
        blurb.Size = new Size(contentWidth, WrappedHeight(blurb.Text, blurb.Font, contentWidth));
        root.Controls.Add(blurb);

        var pathLabel = new Label
        {
            Text = _installDir,
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = TextMain,
            Font = new Font("Consolas", 9f),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };
        var pathBox = new Panel
        {
            Size = new Size(contentWidth, TextRenderer.MeasureText(_installDir, pathLabel.Font).Height + 20),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BackColor = Surface,
            Padding = new Padding(14, 9, 14, 9),
            Margin = new Padding(0, 4, 0, 2)
        };
        pathBox.Controls.Add(pathLabel);
        root.Controls.Add(pathBox);

        root.Controls.Add(_removeData);

        var dataNote = new Label
        {
            Text = $"Sessions and profiles live in {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiBox")} and are kept unless you tick the box above.",
            AutoSize = false,
            ForeColor = Color.FromArgb(110, 124, 113),
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(2, 4, 0, 8)
        };
        dataNote.Size = new Size(contentWidth, WrappedHeight(dataNote.Text, dataNote.Font, contentWidth));
        root.Controls.Add(dataNote);

        _progress.BackColor = Surface;
        _progress.ForeColor = Accent;
        _progress.Size = new Size(contentWidth, 6);
        _progress.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        root.Controls.Add(_progress);

        var bottom = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Bg,
            Margin = new Padding(0, 2, 0, 0),
            Dock = DockStyle.Fill
        };
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _status.Font = new Font("Segoe UI", 8.75f);
        _status.Size = new Size(contentWidth, TextRenderer.MeasureText("Ready.", _status.Font).Height + 6);
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        bottom.Controls.Add(_status, 0, 0);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 6, 0, 0),
            Dock = DockStyle.Fill
        };
        StyleButton(_uninstall, accent: true);
        StyleButton(_cancel, accent: false);
        actions.Controls.Add(_uninstall);
        actions.Controls.Add(_cancel);
        bottom.Controls.Add(actions, 0, 1);
        root.Controls.Add(bottom);

        Controls.Add(root);
        AcceptButton = _uninstall;
        CancelButton = _cancel;

        _cancel.Click += (_, _) => Close();
        _uninstall.Click += async (_, _) => await RunUninstallAsync();
        FormClosing += (_, e) => { if (_working) e.Cancel = true; };
    }

    /// <summary>Pixel height of text wrapped at the given width, using the live font (DPI-correct).</summary>
    private static int WrappedHeight(string text, Font font, int width)
    {
        return TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height;
    }

    private static void StyleButton(Button button, bool accent)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Padding = new Padding(18, 8, 18, 8);
        button.Margin = new Padding(8, 0, 0, 0);
        button.Cursor = Cursors.Hand;
        if (accent)
        {
            button.BackColor = Accent;
            button.ForeColor = Color.FromArgb(10, 18, 7);
            button.FlatAppearance.BorderColor = Accent;
            button.Font = new Font("Segoe UI Semibold", 9.5f);
        }
        else
        {
            button.BackColor = Color.FromArgb(24, 30, 26);
            button.ForeColor = TextMain;
            button.FlatAppearance.BorderColor = Color.FromArgb(58, 68, 60);
        }
    }

    private async Task RunUninstallAsync()
    {
        var confirm = MessageBox.Show(this,
            _removeData.Checked
                ? "Uninstall MultiBox and delete all of your sessions and profiles?\n\nThis cannot be undone."
                : "Uninstall MultiBox?\n\nYour sessions and profiles will be kept.",
            "Uninstall MultiBox", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        _working = true;
        _uninstall.Enabled = false;
        _cancel.Enabled = false;
        _removeData.Enabled = false;
        _progress.Visible = true;
        _status.ForeColor = TextDim;
        _status.Text = "Closing MultiBox…";

        bool removeData = _removeData.Checked;
        _pendingCleanup = (_installDir, _exePath);

        await Task.Run(() =>
        {
            CloseRunningInstances();
            RemoveShortcuts();
            RemoveRegistryEntries();
            if (removeData) RemoveSessionData();
        });

        _progress.Visible = false;
        _status.ForeColor = Accent;
        _status.Text = removeData ? "MultiBox removed, including session data." : "MultiBox removed. Your session data was kept.";
        _uninstall.Text = "Close";
        _uninstall.Enabled = true;
        _uninstall.Click += (_, _) => Close();
        _working = false;
        _cancel.Visible = false;
    }

    /// <summary>Set once the removal work is done; the folder delete is deferred to form close.</summary>
    private (string Dir, string Exe)? _pendingCleanup;

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        if (_pendingCleanup is { } pending)
        {
            // Must run after this process exits: the running exe locks itself and its folder.
            ScheduleSelfDelete(pending.Dir, pending.Exe);
        }
    }

    /// <summary>
    /// Closes running MultiBox app instances (never this process) so their executables
    /// can be removed. Running as Uninstall.exe the process name differs, but guard anyway.
    /// </summary>
    private static void CloseRunningInstances()
    {
        try
        {
            int self = Environment.ProcessId;
            foreach (Process p in Process.GetProcessesByName("MultiBox"))
            {
                if (p.Id == self) continue;
                try
                {
                    p.CloseMainWindow();
                    if (!p.WaitForExit(3000)) p.Kill(entireProcessTree: true);
                }
                catch { /* already gone */ }
                finally { p.Dispose(); }
            }
        }
        catch { }
    }

    private static void RemoveShortcuts()
    {
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MultiBox.lnk"));
        string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "MultiBox");
        try
        {
            if (Directory.Exists(startMenu))
            {
                foreach (string file in Directory.GetFiles(startMenu)) TryDelete(file);
                Directory.Delete(startMenu, recursive: true);
            }
        }
        catch { }
        // A shortcut may also have been pinned directly into the Start menu root.
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "MultiBox.lnk"));
    }

    private static void RemoveRegistryEntries()
    {
        const string subKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiBox";
        foreach (RegistryKey root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            try { root.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false); }
            catch { }
        }
    }

    private static void RemoveSessionData()
    {
        string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiBox");
        try
        {
            if (Directory.Exists(dataDir))
            {
                // The setup log folder lives here too; drop the whole tree.
                Directory.Delete(dataDir, recursive: true);
            }
        }
        catch { }
    }

    /// <summary>
    /// The running executable cannot delete itself, so hand the folder removal to a detached
    /// script that waits for this process to exit and retries until the lock is released.
    /// </summary>
    private static void ScheduleSelfDelete(string installDir, string exePath)
    {
        try
        {
            string scriptPath = Path.Combine(Path.GetTempPath(), $"multibox-uninstall-{Guid.NewGuid():N}.cmd");
            string script = string.Join(Environment.NewLine, new[]
            {
                "@echo off",
                "setlocal",
                "for /l %%i in (1,1,90) do (",
                $"  if not exist \"{exePath}\" goto done",
                $"  del /f /q \"{exePath}\" >nul 2>&1",
                "  ping 127.0.0.1 -n 2 >nul",
                ")",
                ":done",
                $"rmdir /s /q \"{installDir}\" >nul 2>&1",
                "del \"%~f0\" >nul 2>&1",
                "endlocal"
            });
            File.WriteAllText(scriptPath, script);
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch { }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
