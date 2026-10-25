using MultiBox.Core;

namespace MultiBox;

/// <summary>Repair tool for a saved profile whose app executable no longer exists at its saved location.</summary>
internal sealed class FixProfileForm : Form
{
    private readonly SaveBoxSnapshot _profile;
    private readonly ListBox _results = new() { HorizontalScrollbar = true, IntegralHeight = false, BorderStyle = BorderStyle.None };
    private readonly Label _status = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _use = new() { Text = "Use this app", AutoSize = true };
    private readonly Button _deep = new() { Text = "Deep search…", AutoSize = true };
    private readonly Button _browse = new() { Text = "Browse…", AutoSize = true };
    private readonly Button _cancel = new() { Text = "Cancel", AutoSize = true };
    private CancellationTokenSource? _hunt;
    private bool _searching;
    /// <summary>The replacement executable chosen by the user, when the dialog closes as OK.</summary>
    public string? FixedPath { get; private set; }

    public FixProfileForm(SaveBoxSnapshot profile)
    {
        _profile = profile;
        Text = "Fix saved profile · MultiBox";
        ClientSize = new Size(620, 430);
        MinimumSize = new Size(520, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.FromArgb(18, 24, 19); ForeColor = Color.FromArgb(222, 235, 213);
        Font = new Font("Segoe UI", 9);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var explanation = new Label {
            Dock = DockStyle.Fill,
            Text = $"Saved profile ‘{profile.ProfileName}’ points at {profile.AppName}, which no longer exists:\n{profile.AppPath}\nSearch this PC for the app's new location, or browse for its .exe."
        };
        layout.Controls.Add(explanation, 0, 0);
        _status.ForeColor = Color.FromArgb(178, 198, 163);
        layout.Controls.Add(_status, 0, 1);
        _results.BackColor = Color.FromArgb(31, 40, 29); _results.ForeColor = ForeColor;
        layout.Controls.Add(_results, 0, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        _use.FlatStyle = _deep.FlatStyle = _browse.FlatStyle = _cancel.FlatStyle = FlatStyle.Flat;
        _use.BackColor = Color.FromArgb(46, 66, 25);
        actions.Controls.Add(_use); actions.Controls.Add(_deep); actions.Controls.Add(_browse); actions.Controls.Add(_cancel);
        layout.Controls.Add(actions, 0, 3);
        Controls.Add(layout); CancelButton = _cancel;
        _use.Click += (_, _) => UseSelected();
        _results.DoubleClick += (_, _) => UseSelected();
        _results.SelectedIndexChanged += (_, _) => _use.Enabled = _results.SelectedIndex >= 0 && !_searching;
        _deep.Click += async (_, _) => await Hunt(ProfileHealer.Depth.Deep);
        _browse.Click += (_, _) => Browse();
        _cancel.Click += (_, _) => Close();
        FormClosing += (_, _) => _hunt?.Cancel();
        Shown += async (_, _) => await Hunt(ProfileHealer.Depth.Quick);
        _use.Enabled = false;
    }

    private void UseSelected()
    {
        if (_results.SelectedItem is not ResultRow row || _searching) return;
        FixedPath = row.Candidate.ExePath;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void Browse()
    {
        using var chooser = new OpenFileDialog { Filter = "Applications (*.exe)|*.exe", Title = "Choose the new location of the app" };
        if (chooser.ShowDialog(this) != DialogResult.OK) return;
        FixedPath = chooser.FileName;
        DialogResult = DialogResult.OK;
        Close();
    }

    private async Task Hunt(ProfileHealer.Depth depth)
    {
        if (_searching) return;
        _searching = true;
        _use.Enabled = _deep.Enabled = _browse.Enabled = false;
        _results.Items.Clear();
        _hunt?.Cancel(); _hunt?.Dispose();
        _hunt = new CancellationTokenSource();
        var progress = new Progress<string>(text => _status.Text = text + " This can take a moment.");
        try
        {
            HealCandidate[] candidates = await ProfileHealer.HuntAsync(_profile.AppName, _profile.AppPath, depth, progress, _hunt.Token);
            if (candidates.Length == 0)
            {
                _status.Text = depth == ProfileHealer.Depth.Quick
                    ? "No matching app was found yet. Try a deep search, or browse for the app's new .exe."
                    : $"‘{Path.GetFileNameWithoutExtension(_profile.AppName)}’ was not found on this PC. Browse for its new .exe, or cancel and re-save the profile.";
                return;
            }
            foreach (var candidate in candidates) _results.Items.Add(new ResultRow(candidate));
            _results.SelectedIndex = 0;
            _status.Text = ProfileHealer.AutoFixIsSafe(candidates)
                ? $"Found {candidates[0].AppLabel} in its new location — recommended."
                : $"Found {candidates.Length} possible matches. Pick the right one, then choose Use this app.";
        }
        catch (OperationCanceledException) { }
        finally
        {
            _searching = false;
            _deep.Enabled = _browse.Enabled = true;
            _use.Enabled = _results.SelectedIndex >= 0;
        }
    }

    private sealed record ResultRow(HealCandidate Candidate)
    {
        public override string ToString() => $"{Candidate.AppLabel}  ·  {Candidate.ExePath}  ·  {Candidate.Source}";
    }
}
