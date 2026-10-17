using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using MultiBox.Core;

namespace MultiBox;

/// <summary>One-time upgrade helper. It owns no app processes and never runs session cleanup.</summary>
internal sealed class SaveOpenProfileForm : Form
{
    private sealed record VaultChoice(string Path, string Label) { public override string ToString() => Label; }
    private sealed record AppChoice(string Path) { public override string ToString() => System.IO.Path.GetFileNameWithoutExtension(Path); }
    private readonly ComboBox _session = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _app = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _name = new() { Text = "Live copy", Dock = DockStyle.Fill, MaxLength = 40 };
    private readonly Label _status = new() { AutoSize = false, Dock = DockStyle.Fill };
    private readonly Button _save = new() { Text = "Save to SaveBox", AutoSize = true };
    private readonly Button _cancel = new() { Text = "Close", AutoSize = true };
    private bool _saving;

    public SaveOpenProfileForm()
    {
        Text = "Save an open profile · MultiBox";
        ClientSize = new Size(500, 315); MinimumSize = new Size(460, 330);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.FromArgb(18, 24, 19); ForeColor = Color.FromArgb(222, 235, 213);
        Font = new Font("Segoe UI", 9);
        foreach (Control field in new Control[] { _session, _app, _name }) { field.BackColor = Color.FromArgb(31, 40, 29); field.ForeColor = ForeColor; }
        _session.FlatStyle = _app.FlatStyle = FlatStyle.Flat;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 3, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        for (int row = 1; row <= 3; row++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        var explanation = new Label { Text = "Save a session from the older MultiBox without restarting it.\nYour app stays open; this tool does not close or wipe anything.", Dock = DockStyle.Fill };
        layout.Controls.Add(explanation, 0, 0); layout.SetColumnSpan(explanation, 3);
        AddField(layout, "Session", _session, 1); layout.SetColumnSpan(_session, 2);
        AddField(layout, "App", _app, 2);
        var browse = new Button { Text = "…", Dock = DockStyle.Fill }; layout.Controls.Add(browse, 2, 2);
        AddField(layout, "Save as", _name, 3); layout.SetColumnSpan(_name, 2);
        _status.ForeColor = Color.FromArgb(178, 198, 163);
        layout.Controls.Add(_status, 0, 4); layout.SetColumnSpan(_status, 3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        _save.FlatStyle = FlatStyle.Flat; _save.BackColor = Color.FromArgb(46, 66, 25);
        _cancel.FlatStyle = FlatStyle.Flat;
        actions.Controls.Add(_save); actions.Controls.Add(_cancel);
        layout.Controls.Add(actions, 0, 5); layout.SetColumnSpan(actions, 3);
        Controls.Add(layout); AcceptButton = _save; CancelButton = _cancel;
        _cancel.Click += (_, _) => Close();
        FormClosing += (_, e) => { if (_saving) e.Cancel = true; };
        _save.Click += async (_, _) => await Save();
        browse.Click += (_, _) => {
            using var chooser = new OpenFileDialog { Filter = "Applications (*.exe)|*.exe", Title = "Choose the app for this profile" };
            if (chooser.ShowDialog(this) == DialogResult.OK) { int index = _app.Items.Add(new AppChoice(chooser.FileName)); _app.SelectedIndex = index; }
        };
        foreach (var path in AppPaths()) _app.Items.Add(new AppChoice(path));
        foreach (var vault in Vaults()) _session.Items.Add(vault);
        _session.SelectedIndexChanged += (_, _) => GuessApp();
        _session.SelectedIndex = _session.Items.Count > 0 ? 0 : -1;
        _save.Enabled = _session.Items.Count > 0;
        _status.Text = _session.Items.Count == 0 ? "No temporary sessions were found. Nothing was changed." : "Choose the app that belongs to this session, then Save.";
    }
    private static void AddField(TableLayoutPanel panel, string label, Control control, int row)
    {
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        panel.Controls.Add(control, 1, row);
    }
    private void GuessApp()
    {
        if (_session.SelectedItem is not VaultChoice vault) return;
        var matches = _app.Items.Cast<AppChoice>().Where(app => {
            string appName = Path.GetFileNameWithoutExtension(app.Path);
            string folder = appName.Contains("chatgpt", StringComparison.OrdinalIgnoreCase) || appName.Contains("codex", StringComparison.OrdinalIgnoreCase) ? "Codex" : appName;
            return File.Exists(Path.Combine(vault.Path, "User", "AppData", "Roaming", folder, "Local State"));
        }).ToArray();
        _app.SelectedItem = matches.Length == 1 ? matches[0] : null;
    }
    private static IEnumerable<string> AppPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
                try { if (process.Id != Environment.ProcessId && process.MainWindowHandle != IntPtr.Zero && process.MainModule?.FileName is string path && File.Exists(path)) paths.Add(path); }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException) { }
        }
        return paths.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
    }
    private static IEnumerable<VaultChoice> Vaults()
    {
        string root = VaultRecovery.DefaultRoot;
        if (!Directory.Exists(root) || (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) return [];
        var found = new List<VaultChoice>();
        foreach (string folder in Directory.GetDirectories(root).OrderByDescending(Directory.GetCreationTimeUtc))
        {
            try
            {
                string id = Path.GetFileName(folder), marker = Path.Combine(folder, VaultRecovery.OwnerFile);
                if (!Regex.IsMatch(id, "^[a-f0-9]{12}$") || (File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0 || !File.Exists(marker) || (File.GetAttributes(marker) & FileAttributes.ReparsePoint) != 0) continue;
                var owner = JsonSerializer.Deserialize<VaultOwner>(File.ReadAllText(marker));
                if (owner?.Version != 1 || owner.VaultId != id) continue;
                found.Add(new(folder, $"Session {found.Count + 1} · {Directory.GetCreationTime(folder):MMM d, h:mm tt}"));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        }
        return found;
    }
    private async Task Save()
    {
        if (_saving) return;
        if (_session.SelectedItem is not VaultChoice vault || _app.SelectedItem is not AppChoice app || string.IsNullOrWhiteSpace(_name.Text))
        { _status.Text = "Choose a session, its app and a name first."; return; }
        string name = _name.Text.Trim();
        _saving = true; _save.Enabled = _cancel.Enabled = _session.Enabled = _app.Enabled = _name.Enabled = false;
        _status.Text = "Saving a copy… Your app is still running.";
        try
        {
            var result = await Task.Run(() => { bool ok = SaveBoxManager.SaveProfile(Path.GetFileName(app.Path), app.Path, vault.Path, name, null, out string error); return (ok, error); });
            _status.Text = result.ok ? $"‘{name}’ is saved in SaveBox. Your original session is unchanged." : result.error;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _status.Text = "Could not save this copy yet. Your original session is unchanged."; }
        finally { _saving = false; _save.Enabled = _cancel.Enabled = _session.Enabled = _app.Enabled = _name.Enabled = true; }
    }
}
