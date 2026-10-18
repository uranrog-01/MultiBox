using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MultiBox.Core;

namespace MultiBox
{
    public class MainForm : Form
    {
        private readonly WebView2 _webView;
        private readonly SessionManager _sessions;
        private string? _overlaySessionId;
        private string? _overlayIdentity;
        private long _workspaceRevision = -1;
        private RecoveryReport? _recoveryReport;
        private int _cleanupRunning;
        private bool _startupCleanupStarted;
        private IsolatedAppBorderOverlay? _borderOverlay;
        private IndicatorPreferences _indicatorPreferences = IndicatorPreferences.Load();
        private NotifyIcon? _trayIcon;
        private ToolStripMenuItem? _trayStatusItem;
        private ToolStripMenuItem? _trayWipeItem;
        private OleFileDropTarget? _oleDropTarget;
        private bool _isInitialized;
        private bool _isExplicitExit;
        private bool _hasShownTrayBalloon;
        private string? _trustedUiUri;

        private System.Windows.Forms.Timer? _animTimer;
        private Stopwatch? _animStopwatch;
        private Point _animStartPos;
        private Point _animEndPos;
        private int _animDurationMs;
        private bool _isSlidingOut;

        private enum SlideDirection
        {
            FromBottom,
            FromTop,
            FromRight,
            FromLeft
        }

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);

        [DllImport("shell32.dll")]
        public static extern void DragAcceptFiles(IntPtr hwnd, bool fAccept);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [Out] StringBuilder? lpszFile, uint cch);

        [DllImport("shell32.dll")]
        public static extern void DragFinish(IntPtr hDrop);

        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private const int WM_DROPFILES = 0x0233;
        private const uint MSGFLT_ADD = 1;

        private readonly string? _initialAppPath;

        public MainForm(string? initialAppPath = null)
        {
            _initialAppPath = initialAppPath;

            // Allow drag and drop across privilege boundaries (UIPI)
            try
            {
                ChangeWindowMessageFilter(WM_DROPFILES, MSGFLT_ADD);
                ChangeWindowMessageFilter(0x0049 /* WM_COPYGLOBALDATA */, MSGFLT_ADD);
                ChangeWindowMessageFilter(0x004A /* WM_COPYDATA */, MSGFLT_ADD);
            }
            catch { }

            // Friendly, screen-proportional modern dimensions
            int targetWidth = 620;
            int targetHeight = 960;
            var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            targetWidth = Math.Min(targetWidth, Math.Max(320, workingArea.Width - 48));
            targetHeight = Math.Min(targetHeight, Math.Max(480, workingArea.Height - 48));

            this.Text = "MultiBox";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(targetWidth, targetHeight);
            this.BackColor = Color.FromArgb(7, 8, 10);
            this.AllowDrop = true;

            var initialTargetPos = GetSystemTrayTargetLocation();
            var initialDir = GetSlideDirection();
            int initialOffset = 90;
            this.Location = initialDir switch
            {
                SlideDirection.FromTop => new Point(initialTargetPos.X, initialTargetPos.Y - initialOffset),
                SlideDirection.FromLeft => new Point(initialTargetPos.X - initialOffset, initialTargetPos.Y),
                SlideDirection.FromRight => new Point(initialTargetPos.X + initialOffset, initialTargetPos.Y),
                _ => new Point(initialTargetPos.X, initialTargetPos.Y + initialOffset)
            };

            // Load application icon
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    this.Icon = new Icon(iconPath);
                }
                catch { }
            }

            _sessions = new SessionManager();
            _sessions.Changed += Sessions_Changed;

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.Transparent,
                AllowExternalDrop = false
            };

            this.Controls.Add(_webView);

            // Create native COM OLE Drop Target
            _oleDropTarget = new OleFileDropTarget(
                isHovering =>
                {
                    try
                    {
                        this.BeginInvoke(() =>
                        {
                            _webView.CoreWebView2?.ExecuteScriptAsync($"window.onDragHover({(isHovering ? "true" : "false")});");
                        });
                    }
                    catch { }
                },
                droppedFile =>
                {
                    this.BeginInvoke(() =>
                    {
                        LaunchApp(droppedFile);
                    });
                }
            );

            // Hook native drag-and-drop events on form
            this.DragEnter += MainForm_DragEnter;
            this.DragOver += MainForm_DragOver;
            this.DragLeave += MainForm_DragLeave;
            this.DragDrop += MainForm_DragDrop;
            this.MouseDown += MainForm_MouseDown;

            InitializeSystemTray();

            this.Load += MainForm_Load;
            this.Shown += (s, e) =>
            {
                ApplyRoundedCorners();
                SlideInFromTray();
            };
            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                    SlideOutAndMinimizeToTray();
                }
                else
                {
                    ApplyRoundedCorners();
                }
            };
            this.FormClosing += MainForm_FormClosing;
        }

        private void ApplyRoundedCorners()
        {
            if (!IsHandleCreated || IsDisposed || Disposing) return;

            try
            {
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch { }

            int radiusDiameter = 48; // 24px corner radius * 2
            try
            {
                IntPtr hFormRgn = CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, radiusDiameter, radiusDiameter);
                if (hFormRgn != IntPtr.Zero)
                {
                    if (SetWindowRgn(this.Handle, hFormRgn, true) == 0)
                    {
                        DeleteObject(hFormRgn);
                    }
                }

                if (_webView != null && _webView.IsHandleCreated)
                {
                    IntPtr hWebRgn = CreateRoundRectRgn(0, 0, _webView.Width + 1, _webView.Height + 1, radiusDiameter, radiusDiameter);
                    if (hWebRgn != IntPtr.Zero)
                    {
                        if (SetWindowRgn(_webView.Handle, hWebRgn, true) == 0)
                        {
                            DeleteObject(hWebRgn);
                        }
                    }
                }
            }
            catch { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AttachOleDropTargets();
        }

        public void AttachOleDropTargets()
        {
            if (_oleDropTarget == null) return;

            try
            {
                NativeMethods.RevokeDragDrop(this.Handle);
                NativeMethods.RegisterDragDrop(this.Handle, _oleDropTarget);
                DragAcceptFiles(this.Handle, true);

                NativeMethods.EnumChildWindows(this.Handle, (hwnd, lparam) =>
                {
                    try
                    {
                        NativeMethods.RevokeDragDrop(hwnd);
                        NativeMethods.RegisterDragDrop(hwnd, _oleDropTarget);
                        DragAcceptFiles(hwnd, true);
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }

        private void InitializeSystemTray()
        {
            _trayIcon = new NotifyIcon
            {
                Text = "MultiBox - Ephemeral Isolation",
                Visible = true
            };

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath))
            {
                // Ask for the tray's exact size (16/20/24 px by display scale) so Windows never shrinks the 32 px frame.
                try { _trayIcon.Icon = new Icon(iconPath, SystemInformation.SmallIconSize); }
                catch { _trayIcon.Icon = this.Icon ?? SystemIcons.Application; }
            }
            else
            {
                _trayIcon.Icon = this.Icon ?? SystemIcons.Application;
            }

            var menu = new ContextMenuStrip();
            var showItem = new ToolStripMenuItem("Open MultiBox", null, (s, e) => RestoreFromTray())
            {
                Font = new Font(this.Font, FontStyle.Bold)
            };
            _trayStatusItem = new ToolStripMenuItem("Status: Idle") { Enabled = false };
            _trayWipeItem = new ToolStripMenuItem("End & wipe selected session", null, (s, e) => { string? id = _sessions.SelectedId; if (id != null) Task.Run(() => { if (!_sessions.Wipe(id, out string error)) Notify("error", error); }); }) { Enabled = false };
            var exitItem = new ToolStripMenuItem("Exit MultiBox", null, (s, e) => ExitApplication());

            menu.Items.Add(showItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_trayStatusItem);
            menu.Items.Add(_trayWipeItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (this.Visible && this.WindowState != FormWindowState.Minimized && !_isSlidingOut)
                    {
                        SlideOutAndMinimizeToTray();
                    }
                    else
                    {
                        RestoreFromTray();
                    }
                }
            };
            _trayIcon.DoubleClick += (s, e) =>
            {
                if (!this.Visible || this.WindowState == FormWindowState.Minimized)
                {
                    RestoreFromTray();
                }
            };
        }

        private Point GetSystemTrayTargetLocation()
        {
            var screen = Screen.PrimaryScreen ?? (Screen.AllScreens.Length > 0 ? Screen.AllScreens[0] : null);
            var work = screen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            var bounds = screen?.Bounds ?? work;

            int margin = 16;
            int targetX = work.Right - this.Width - margin;
            int targetY = work.Bottom - this.Height - margin;

            if (work.Top > bounds.Top)
            {
                targetY = work.Top + margin;
            }
            else if (work.Left > bounds.Left)
            {
                targetX = work.Left + margin;
            }

            targetX = Math.Max(work.Left + margin, Math.Min(targetX, work.Right - this.Width - margin));
            targetY = Math.Max(work.Top + margin, Math.Min(targetY, work.Bottom - this.Height - margin));

            return new Point(targetX, targetY);
        }

        private SlideDirection GetSlideDirection()
        {
            var screen = Screen.PrimaryScreen ?? (Screen.AllScreens.Length > 0 ? Screen.AllScreens[0] : null);
            var work = screen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            var bounds = screen?.Bounds ?? work;

            if (work.Top > bounds.Top) return SlideDirection.FromTop;
            if (work.Left > bounds.Left) return SlideDirection.FromLeft;
            if (work.Right < bounds.Right) return SlideDirection.FromRight;
            return SlideDirection.FromBottom;
        }

        public void RestoreFromTray()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(RestoreFromTray);
                return;
            }

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }

            this.Show();
            this.BringToFront();
            this.Activate();
            SlideInFromTray();
        }

        public void SlideInFromTray()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(SlideInFromTray);
                return;
            }

            _animTimer?.Stop();
            _animTimer?.Dispose();

            var targetPos = GetSystemTrayTargetLocation();
            var dir = GetSlideDirection();
            int offset = 90;
            Point startPos = dir switch
            {
                SlideDirection.FromTop => new Point(targetPos.X, targetPos.Y - offset),
                SlideDirection.FromLeft => new Point(targetPos.X - offset, targetPos.Y),
                SlideDirection.FromRight => new Point(targetPos.X + offset, targetPos.Y),
                _ => new Point(targetPos.X, targetPos.Y + offset)
            };

            this.Location = startPos;
            _animStartPos = startPos;
            _animEndPos = targetPos;
            _animDurationMs = 240;
            _isSlidingOut = false;

            try { TimeBeginPeriod(1); } catch { }

            _animStopwatch = Stopwatch.StartNew();
            _animTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _animTimer.Tick += AnimTimer_Tick;
            _animTimer.Start();

            try
            {
                _webView.CoreWebView2?.ExecuteScriptAsync(
                    "window.playSlideInMotion ? window.playSlideInMotion() : (document.body.classList.remove('motion-slide-in'), void document.body.offsetWidth, document.body.classList.add('motion-slide-in'));"
                );
            }
            catch { }
        }

        public void SlideOutAndMinimizeToTray()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(SlideOutAndMinimizeToTray);
                return;
            }

            if (_isSlidingOut || !this.Visible) return;

            _animTimer?.Stop();
            _animTimer?.Dispose();

            var currentPos = this.Location;
            var dir = GetSlideDirection();
            int offset = 90;
            Point endPos = dir switch
            {
                SlideDirection.FromTop => new Point(currentPos.X, currentPos.Y - offset),
                SlideDirection.FromLeft => new Point(currentPos.X - offset, currentPos.Y),
                SlideDirection.FromRight => new Point(currentPos.X + offset, currentPos.Y),
                _ => new Point(currentPos.X, currentPos.Y + offset)
            };

            _animStartPos = currentPos;
            _animEndPos = endPos;
            _animDurationMs = 180;
            _isSlidingOut = true;

            try { TimeBeginPeriod(1); } catch { }

            _animStopwatch = Stopwatch.StartNew();
            _animTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _animTimer.Tick += AnimTimer_Tick;
            _animTimer.Start();
        }

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            if (_animStopwatch == null)
            {
                _animTimer?.Stop();
                return;
            }

            long elapsed = _animStopwatch.ElapsedMilliseconds;
            float t = Math.Clamp((float)elapsed / _animDurationMs, 0f, 1f);

            if (_isSlidingOut)
            {
                // Ease-in quadratic curve: smooth acceleration into the tray
                float ease = t * t;
                int curX = (int)(_animStartPos.X + (_animEndPos.X - _animStartPos.X) * ease);
                int curY = (int)(_animStartPos.Y + (_animEndPos.Y - _animStartPos.Y) * ease);
                this.Location = new Point(curX, curY);

                if (t >= 1f)
                {
                    _animTimer?.Stop();
                    _animTimer?.Dispose();
                    _animTimer = null;
                    _animStopwatch = null;
                    try { TimeEndPeriod(1); } catch { }

                    this.Hide();
                    _isSlidingOut = false;
                    this.Location = _animStartPos; // reset for next restore

                    if (!_hasShownTrayBalloon)
                    {
                        _hasShownTrayBalloon = true;
                        _trayIcon?.ShowBalloonTip(2000, "MultiBox Running", "MultiBox minimized to system tray. Click icon to reopen.", ToolTipIcon.Info);
                    }
                }
            }
            else
            {
                // Ease-out cubic curve: smooth deceleration into position
                float ease = 1f - MathF.Pow(1f - t, 3f);
                int curX = (int)(_animStartPos.X + (_animEndPos.X - _animStartPos.X) * ease);
                int curY = (int)(_animStartPos.Y + (_animEndPos.Y - _animStartPos.Y) * ease);
                this.Location = new Point(curX, curY);

                if (t >= 1f)
                {
                    _animTimer?.Stop();
                    _animTimer?.Dispose();
                    _animTimer = null;
                    _animStopwatch = null;
                    try { TimeEndPeriod(1); } catch { }

                    this.Location = _animEndPos;
                    this.Activate();
                    this.BringToFront();
                }
            }
        }

        public void ExitApplication()
        {
            _isExplicitExit = true;
            _animTimer?.Stop();
            _animTimer?.Dispose();
            try { TimeEndPeriod(1); } catch { }
            this.Close();
            Application.Exit();
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            await InitializeWebViewAsync();
        }

        private static bool EnsureWebView2RuntimeAvailable()
        {
            try
            {
                string? version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (!string.IsNullOrWhiteSpace(version)) return true;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                // Expected when WebView2 runtime is missing
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView2 Probe Error] {ex.Message}");
            }

            PromptAndOfferWebView2Download();
            return false;
        }

        private static void PromptAndOfferWebView2Download()
        {
            var choice = MessageBox.Show(
                "Microsoft Edge WebView2 Runtime is required to run MultiBox, but was not found on this computer.\n\n" +
                "Would you like to open the official Microsoft download page to install it now?",
                "MultiBox - Missing Runtime Requirement",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (choice == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://go.microsoft.com/fwlink/p/?LinkId=2124703")
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not open browser automatically.\n\nPlease visit the official Microsoft page:\nhttps://go.microsoft.com/fwlink/p/?LinkId=2124703\n\nError: {ex.Message}",
                        "MultiBox - Open Link Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private bool IsMessageSourceTrusted(string? source)
        {
            if (string.IsNullOrEmpty(_trustedUiUri) || string.IsNullOrEmpty(source))
                return false;

            if (Uri.TryCreate(source, UriKind.Absolute, out var sourceUri) &&
                Uri.TryCreate(_trustedUiUri, UriKind.Absolute, out var trustedUri))
            {
                return sourceUri.Scheme.Equals(trustedUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(sourceUri.LocalPath, trustedUri.LocalPath, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private async Task InitializeWebViewAsync()
        {
            if (!EnsureWebView2RuntimeAvailable())
            {
                Application.Exit();
                return;
            }

            try
            {
                string userDataFolder = Path.Combine(Path.GetTempPath(), "MultiBox_WebView2_Env");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                _webView.AllowExternalDrop = false;

                _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
#if DEBUG
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#else
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

                _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                _webView.CoreWebView2.NavigationStarting += (s, args) =>
                {
                    if (string.IsNullOrEmpty(_trustedUiUri)) return;

                    if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var navUri) &&
                        Uri.TryCreate(_trustedUiUri, UriKind.Absolute, out var trustedUri))
                    {
                        if (navUri.Scheme.Equals(trustedUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(navUri.LocalPath, trustedUri.LocalPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return; // Allow navigation to trusted UI
                        }

                        args.Cancel = true;

                        // Open external web links in system browser
                        if (navUri.Scheme == Uri.UriSchemeHttp || navUri.Scheme == Uri.UriSchemeHttps)
                        {
                            try { Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true }); } catch { }
                        }
                    }
                    else
                    {
                        args.Cancel = true;
                    }
                };

                _webView.CoreWebView2.FrameNavigationStarting += (s, args) =>
                {
                    args.Cancel = true;
                };

                _webView.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    {
                        try { Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true }); } catch { }
                    }
                };

                _webView.NavigationCompleted += (s, e) =>
                {
                    ApplyRoundedCorners();
                    AttachOleDropTargets();
                    SyncContextMenuStatus();
                    Sessions_Changed(_sessions.Snapshot());
                    if (!_startupCleanupStarted) { _startupCleanupStarted = true; RunCrashCleanup(); }
                };

                // Load HTML
                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "index.html");
                if (!File.Exists(htmlPath))
                {
                    string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "UI", "index.html"));
                    if (File.Exists(devPath))
                    {
                        htmlPath = devPath;
                    }
                }

                if (File.Exists(htmlPath))
                {
                    _trustedUiUri = new Uri(htmlPath).AbsoluteUri;
                    _webView.CoreWebView2.Navigate(_trustedUiUri);
                }

                _isInitialized = true;
                AttachOleDropTargets();

                var timer = new System.Windows.Forms.Timer { Interval = 1500 };
                timer.Tick += (s, e) => AttachOleDropTargets();
                timer.Start();

                if (!string.IsNullOrWhiteSpace(_initialAppPath))
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(600);
                        LaunchApp(_initialAppPath);
                    });
                }
            }
            catch (WebView2RuntimeNotFoundException)
            {
                PromptAndOfferWebView2Download();
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing MultiBox WebView2: {ex.Message}", "MultiBox Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncContextMenuStatus()
        {
            bool enabled = ContextMenuManager.IsEnabled();
            _webView.CoreWebView2?.ExecuteScriptAsync($"window.onContextMenuStatus({(enabled ? "true" : "false")});");
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                if (!IsMessageSourceTrusted(e.Source))
                {
                    Debug.WriteLine($"[Security] Blocked unauthorized web message from: {e.Source}");
                    return;
                }

                string json = e.WebMessageAsJson;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("action", out var actionElem)) return;
                string action = actionElem.GetString() ?? string.Empty;
                string sessionId = root.TryGetProperty("sessionId", out var idElement) ? idElement.GetString() ?? "" : _sessions.SelectedId ?? "";
                string[] sessionIds = root.TryGetProperty("sessionIds", out var idsElement) ? idsElement.EnumerateArray().Select(id => id.GetString() ?? "").ToArray() : _sessions.Snapshot().Sessions.Select(s => s.Id).ToArray();

                switch (action)
                {
                    case "minimizeWindow":
                        this.Invoke(SlideOutAndMinimizeToTray);
                        break;

                    case "closeWindow":
                        this.Invoke(SlideOutAndMinimizeToTray);
                        break;

                    case "exitApplication":
                        this.Invoke(ExitApplication);
                        break;

                    case "dragWindow":
                        this.Invoke(() =>
                        {
                            ReleaseCapture();
                            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                        });
                        break;

                    case "browseApp":
                        this.Invoke(() => BrowseForApp());
                        break;

                    case "launchApp":
                        if (root.TryGetProperty("path", out var pathElem))
                        {
                            string appPath = pathElem.GetString() ?? string.Empty;
                            LaunchApp(appPath);
                        }
                        break;

                    case "reopenApp":
                    case "reopenOrFocusApp":
                        ReopenOrFocusApp(sessionId);
                        break;

                    case "getRunningProcesses":
                        Task.Run(() => SendRunningProcessesToUi());
                        break;

                    case "isolateRunningProcess":
                        if (root.TryGetProperty("path", out var procPathElem))
                        {
                            string targetPath = procPathElem.GetString() ?? string.Empty;
                            int pid = root.TryGetProperty("pid", out var pidElem) ? pidElem.GetInt32() : 0;
                            IsolateRunningProcess(pid, targetPath);
                        }
                        break;

                    case "terminateAndWipe":
                        Task.Run(() => { bool ok = _sessions.Wipe(sessionId, out string error); Notify(ok ? "success" : "error", ok ? "Session ended. Saved profiles were kept." : error); });
                        break;

                    case "openVaultDirectory":
                        string vaultBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiBox", "Vaults");
                        Directory.CreateDirectory(vaultBase);
                        Process.Start("explorer.exe", vaultBase);
                        break;

                    case "openSaveBoxDirectory":
                        Directory.CreateDirectory(SaveBoxManager.SaveBoxRoot);
                        Process.Start("explorer.exe", SaveBoxManager.SaveBoxRoot);
                        break;

                    case "saveToSaveBox":
                        string profileName = root.TryGetProperty("profileName", out var profileNameValue) ? profileNameValue.GetString() ?? "" : "";
                        string? replaceId = root.TryGetProperty("replaceProfileId", out var replaceValue) ? replaceValue.GetString() : null;
                        string requestId = root.TryGetProperty("requestId", out var requestValue) ? requestValue.GetString() ?? "" : "";
                        Task.Run(() =>
                        {
                            var outcome = _sessions.SaveProfileAndReopen(sessionId, profileName, replaceId, phase => {
                                var update = JsonSerializer.Serialize(new { requestId, phase });
                                try { BeginInvoke(() => _webView.CoreWebView2?.ExecuteScriptAsync($"window.onProfileSaveProgress?.({update});")); } catch (InvalidOperationException) { }
                            });
                            var result = JsonSerializer.Serialize(new { requestId, ok = outcome.Saved, error = outcome.Error, warning = outcome.Warning });
                            try { BeginInvoke(() => _webView.CoreWebView2?.ExecuteScriptAsync($"window.onNamedProfileSaved?.({result});")); } catch (InvalidOperationException) { }
                            Notify(outcome.Saved && outcome.Warning.Length == 0 ? "success" : "error", !outcome.Saved ? outcome.Error :
                                outcome.Warning.Length > 0 ? $"‘{profileName}’ saved to SaveBox. " + outcome.Warning :
                                outcome.Reopened ? $"‘{profileName}’ saved to SaveBox. Your app reopened." : $"‘{profileName}’ saved to SaveBox.");
                            SendSaveBoxSnapshotsToUi();
                        });
                        break;

                    case "getSaveBoxSnapshots":
                        Task.Run(() => SendSaveBoxSnapshotsToUi());
                        break;

                    case "deleteSaveBoxSnapshot":
                        string deletingId = root.TryGetProperty("profileId", out var deletingValue) ? deletingValue.GetString() ?? "" : "";
                        Task.Run(() =>
                        {
                            bool ok = SaveBoxManager.DeleteProfile(deletingId);
                            Notify(ok ? "success" : "error", ok ? "Saved profile deleted. Other profiles were kept." : "The saved profile could not be deleted.");
                            SendSaveBoxSnapshotsToUi();
                        });
                        break;
                    case "launchSaveBoxSnapshot":
                        string launchingId = root.TryGetProperty("profileId", out var launchingValue) ? launchingValue.GetString() ?? "" : "";
                        var savedProfile = SaveBoxManager.GetProfile(launchingId);
                        if (savedProfile == null) Notify("error", "That saved profile could not be found. Existing sessions were kept.");
                        else LaunchApp(savedProfile.AppPath, true, savedProfile.ProfileId);
                        break;
                    case "getRecoveryStatus":
                        SendRecoveryStatus();
                        break;
                    case "getIndicatorSettings":
                        SendIndicatorSettings();
                        break;
                    case "setIndicatorSettings":
                        string? indicatorMode = root.TryGetProperty("mode", out var indicatorValue) ? indicatorValue.GetString() : null;
                        if (IndicatorPreferences.IsValid(indicatorMode))
                        {
                            var preferences = new IndicatorPreferences(indicatorMode!);
                            if (preferences.Save()) { _indicatorPreferences = preferences; _borderOverlay?.SetMode(preferences.Mode); }
                            else Notify("error", "The indicator preference could not be saved.");
                        }
                        SendIndicatorSettings();
                        break;
                    case "runCrashCleanup":
                        RunCrashCleanup();
                        break;

                    case "selectSession":
                        _sessions.Select(sessionId);
                        break;
                    case "customizeSession":
                        string customizeRequest = root.TryGetProperty("requestId", out var customizeRequestValue) ? customizeRequestValue.GetString() ?? "" : "";
                        string customizeId = root.TryGetProperty("sessionId", out var customizeIdValue) ? customizeIdValue.GetString() ?? "" : "";
                        string? displayName = root.TryGetProperty("displayName", out var displayNameValue) ? displayNameValue.GetString() : null;
                        string? sessionColor = root.TryGetProperty("color", out var sessionColorValue) ? sessionColorValue.GetString() : null;
                        bool customized = _sessions.Customize(customizeId, displayName, sessionColor, out string customizeError);
                        string customizeResult = JsonSerializer.Serialize(new { requestId = customizeRequest, ok = customized, error = customizeError });
                        _webView.CoreWebView2?.ExecuteScriptAsync($"window.onSessionCustomized?.({customizeResult});");
                        break;
                    case "getSessions":
                        Sessions_Changed(_sessions.Snapshot());
                        break;
                    case "duplicateSession":
                        Task.Run(() => { if (!_sessions.Duplicate(sessionId, out string error)) Notify("error", error); });
                        break;
                    case "endAllSessions":
                    case "cleanAllVaults":
                        Task.Run(() => { bool ok = _sessions.WipeMany(sessionIds, out string error); Notify(ok ? "success" : "error", ok ? "Sessions ended. Saved profiles were kept." : error); });
                        break;
                    case "purgeAllVaultsConfirmed":
                        var savedNames = SaveBoxManager.ListSnapshots(true).Select(profile => profile.ProfileId).ToArray();
                        Task.Run(() =>
                        {
                            if (!_sessions.WipeMany(sessionIds, out string error)) { Notify("error", error); return; }
                            bool removed = savedNames.Select(SaveBoxManager.DeleteProfile).ToArray().All(result => result);
                            Notify(removed ? "success" : "error", removed ? "Sessions and saved profiles deleted." : "Some saved profiles could not be deleted.");
                            SendSaveBoxSnapshotsToUi();
                        });
                        break;

                    case "getContextMenuStatus":
                        SyncContextMenuStatus();
                        break;

                    case "setContextMenu":
                        if (root.TryGetProperty("enabled", out var enElem))
                        {
                            bool enable = enElem.GetBoolean();
                            bool ok = ContextMenuManager.SetEnabled(enable);
                            string msg = enable 
                                ? (ok ? "Right-click 'Run in Isolated Mode' added to Explorer!" : "Failed to register context menu.")
                                : (ok ? "Context menu entry removed from Explorer." : "Failed to remove context menu.");
                            
                            this.Invoke(() =>
                            {
                                _webView.CoreWebView2.ExecuteScriptAsync($"window.showNotification('{(ok ? "success" : "error")}', '{msg}');");
                                SyncContextMenuStatus();
                            });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebMessageReceived Error] {ex.Message}");
            }
        }

        private void Notify(string type, string message)
        {
            if (IsDisposed || Disposing) return;
            try { BeginInvoke(() => _webView.CoreWebView2?.ExecuteScriptAsync($"window.showNotification({JsonSerializer.Serialize(type)}, {JsonSerializer.Serialize(message)});")); }
            catch (InvalidOperationException) { }
        }

        private void LaunchApp(string rawPath, bool restoreSavedProfile = false, string? profileId = null)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return;
            Task.Run(() =>
            {
                if (!_sessions.LaunchNew(rawPath, restoreSavedProfile, out string error, profileId))
                    Notify("error", error);
            });
        }

        private void BrowseForApp()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select Application or Shortcut to Isolate",
                Filter = "Executables & Shortcuts (*.exe;*.lnk;*.bat;*.cmd)|*.exe;*.lnk;*.bat;*.cmd|All Files (*.*)|*.*",
                CheckFileExists = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                LaunchApp(ofd.FileName);
            }
        }

        private void ReopenOrFocusApp(string id)
        {
            var engine = _sessions.GetEngine(id);
            if (engine == null) return;
            if (engine.IsPreserved)
            {
                Task.Run(() => { if (!_sessions.Resume(id, out string error)) Notify("error", error); });
                return;
            }
            IntPtr window = FindSessionWindow(engine);
            if (window == IntPtr.Zero) { Notify("error", "This session has no visible window yet. Its processes are still running."); return; }
            if (NativeMethods.IsIconic(window)) NativeMethods.ShowWindow(window, NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(window);
        }

        private IntPtr FindSessionWindow(IsolationEngine engine)
        {
            var processIds = engine.GetProcessIds().ToHashSet();
            IntPtr found = IntPtr.Zero;
            long largest = 0;
            NativeMethods.EnumWindows((window, _) =>
            {
                if (window == Handle || !NativeMethods.IsWindowVisible(window)) return true;
                NativeMethods.GetWindowThreadProcessId(window, out uint pid);
                if (!processIds.Contains((int)pid)) return true;
                if (NativeMethods.GetWindowRect(window, out var rect))
                {
                    long area = (long)rect.Width * rect.Height;
                    if (area > largest) { largest = area; found = window; }
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private void SendRunningProcessesToUi()
        {
            try
            {
                var list = new List<object>();
                var processes = Process.GetProcesses();
                int currentPid = Environment.ProcessId;

                foreach (var p in processes)
                {
                    try
                    {
                        if (p.Id == 0 || p.Id == 4 || p.Id == currentPid) continue;

                        string procName = p.ProcessName;
                        if (procName.Equals("Idle", StringComparison.OrdinalIgnoreCase) ||
                            procName.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                            procName.Equals("Registry", StringComparison.OrdinalIgnoreCase) ||
                            procName.Equals("smss", StringComparison.OrdinalIgnoreCase) ||
                            procName.Equals("csrss", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string? exePath = GetProcessPath(p);
                        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                        {
                            continue;
                        }

                        string sysRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                        bool isSystemCore = exePath.StartsWith(Path.Combine(sysRoot, "System32"), StringComparison.OrdinalIgnoreCase) &&
                                           (procName.Equals("svchost", StringComparison.OrdinalIgnoreCase) ||
                                            procName.Equals("services", StringComparison.OrdinalIgnoreCase) ||
                                            procName.Equals("lsass", StringComparison.OrdinalIgnoreCase) ||
                                            procName.Equals("wininit", StringComparison.OrdinalIgnoreCase) ||
                                            procName.Equals("winlogon", StringComparison.OrdinalIgnoreCase) ||
                                            procName.Equals("dwm", StringComparison.OrdinalIgnoreCase) ||
                                            procName.Equals("fontdrvhost", StringComparison.OrdinalIgnoreCase));
                        if (isSystemCore) continue;

                        string title = string.Empty;
                        try { title = p.MainWindowTitle; } catch { }
                        bool hasWindow = !string.IsNullOrWhiteSpace(title);

                        long memBytes = 0;
                        try { memBytes = p.WorkingSet64; } catch { }
                        double memMb = Math.Round((double)memBytes / (1024 * 1024), 1);

                        list.Add(new
                        {
                            pid = p.Id,
                            name = procName + ".exe",
                            title = string.IsNullOrWhiteSpace(title) ? procName : title,
                            exePath = exePath,
                            memMb = memMb,
                            hasWindow = hasWindow
                        });
                    }
                    catch { }
                    finally
                    {
                        p.Dispose();
                    }
                }

                var ordered = list
                    .OrderByDescending(x => (bool)x.GetType().GetProperty("hasWindow")!.GetValue(x)!)
                    .ThenByDescending(x => (double)x.GetType().GetProperty("memMb")!.GetValue(x)!)
                    .ToList();

                string json = JsonSerializer.Serialize(ordered);
                this.Invoke(() =>
                {
                    _webView.CoreWebView2?.ExecuteScriptAsync($"window.onProcessesLoaded({json});");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SendRunningProcessesToUi Error] {ex.Message}");
            }
        }

        private static string? GetProcessPath(Process p)
        {
            try
            {
                return p.MainModule?.FileName;
            }
            catch
            {
                try
                {
                    IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, p.Id);
                    if (hProc != IntPtr.Zero)
                    {
                        try
                        {
                            var sb = new StringBuilder(1024);
                            int size = sb.Capacity;
                            if (NativeMethods.QueryFullProcessImageName(hProc, 0, sb, ref size))
                            {
                                return sb.ToString();
                            }
                        }
                        finally
                        {
                            NativeMethods.CloseHandle(hProc);
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private void IsolateRunningProcess(int pid, string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                this.Invoke(() =>
                {
                    _webView.CoreWebView2.ExecuteScriptAsync("window.showNotification('error', 'Executable path could not be resolved for this process.');");
                });
                return;
            }

            LaunchApp(exePath);
        }

        private void Sessions_Changed(SessionWorkspace workspace)
        {
            if (!_isInitialized || IsDisposed || Disposing) return;
            try
            {
                BeginInvoke(() =>
                {
                    if (workspace.Revision < _workspaceRevision || IsDisposed) return;
                    _workspaceRevision = workspace.Revision;
                    string json = JsonSerializer.Serialize(workspace, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    _webView.CoreWebView2?.ExecuteScriptAsync($"window.onSessionsChanged?.({json});");
                    var selected = workspace.Sessions.FirstOrDefault(session => session.Id == workspace.SelectedId);
                    if (_trayStatusItem != null) _trayStatusItem.Text = $"Sessions: {workspace.Sessions.Count} · Selected: {selected?.DisplayLabel ?? "none"}";
                    if (_trayWipeItem != null) _trayWipeItem.Enabled = selected != null && !selected.IsBusy;
                    string? overlayIdentity = selected == null ? null : selected.DisplayLabel + "|" + selected.Color;
                    if (_overlaySessionId != selected?.Id || _overlayIdentity != overlayIdentity || selected?.State != "running" || _borderOverlay == null || _borderOverlay.IsDisposed)
                    {
                        _borderOverlay?.Dispose();
                        _borderOverlay = null;
                        _overlaySessionId = selected?.Id;
                        _overlayIdentity = overlayIdentity;
                        var engine = _sessions.GetEngine(selected?.Id);
                        if (engine != null && selected?.State == "running")
                        {
                            try { _borderOverlay = new IsolatedAppBorderOverlay(engine, _indicatorPreferences.Mode, selected.DisplayLabel, selected.Color); } catch { }
                        }
                    }
                });
            }
            catch (InvalidOperationException) { }
        }

        private void SendIndicatorSettings()
        {
            string data = JsonSerializer.Serialize(new { mode = _indicatorPreferences.Mode });
            _webView.CoreWebView2?.ExecuteScriptAsync($"window.onIndicatorSettings?.({data});");
        }
        private void SendRecoveryStatus()
        {
            if (IsDisposed || Disposing || !_isInitialized) return;
            string data = JsonSerializer.Serialize(new { isChecking = Volatile.Read(ref _cleanupRunning) != 0, report = _recoveryReport }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            try { BeginInvoke(() => _webView.CoreWebView2?.ExecuteScriptAsync($"window.onRecoveryStatus?.({data});")); } catch (InvalidOperationException) { }
        }
        private void RunCrashCleanup()
        {
            if (Interlocked.CompareExchange(ref _cleanupRunning, 1, 0) != 0) return;
            SendRecoveryStatus();
            Task.Run(() =>
            {
                try { _recoveryReport = VaultRecovery.Cleanup(); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _recoveryReport = new(0, 0, 0, 1, DateTime.UtcNow.ToString("O")); }
                finally { Interlocked.Exchange(ref _cleanupRunning, 0); SendRecoveryStatus(); }
                if (_recoveryReport?.Removed > 0) Notify("success", $"Cleaned {_recoveryReport.Removed} abandoned session(s). Active sessions and saved profiles were kept.");
                else if (_recoveryReport?.Failed > 0) Notify("error", "Some abandoned session files could not be cleaned. You can retry in Settings.");
            });
        }

        private void SendSaveBoxSnapshotsToUi()
        {
            try
            {
                var list = SaveBoxManager.ListSnapshots(true);
                string json = JsonSerializer.Serialize(list);
                this.Invoke(() =>
                {
                    _webView.CoreWebView2?.ExecuteScriptAsync($"window.onSaveBoxLoaded({json});");
                });
            }
            catch { }
        }

        private void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
                _webView.CoreWebView2?.ExecuteScriptAsync("window.onDragHover(true);");
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainForm_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragLeave(object? sender, EventArgs e)
        {
            _webView.CoreWebView2?.ExecuteScriptAsync("window.onDragHover(false);");
        }

        private void MainForm_DragDrop(object? sender, DragEventArgs e)
        {
            _webView.CoreWebView2?.ExecuteScriptAsync("window.onDragHover(false);");

            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                LaunchApp(files[0]);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        private const int WM_COPYDATA = 0x004A;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_COPYDATA)
            {
                try
                {
                    var cds = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT))!;
                    string? payload = cds.cbData > 0 ? Marshal.PtrToStringUni(cds.lpData, cds.cbData / 2) : null;
                    this.BeginInvoke(() =>
                    {
                        RestoreFromTray();
                        if (!string.IsNullOrWhiteSpace(payload) && payload != "__ACTIVATE__")
                        {
                            LaunchApp(payload);
                        }
                    });
                    m.Result = (IntPtr)1;
                    return;
                }
                catch { }
            }

            if (m.Msg == WM_DROPFILES)
            {
                IntPtr hDrop = m.WParam;
                uint count = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                if (count > 0)
                {
                    var sb = new StringBuilder(1024);
                    if (DragQueryFile(hDrop, 0, sb, 1024) > 0)
                    {
                        string file = sb.ToString();
                        LaunchApp(file);
                    }
                }
                DragFinish(hDrop);
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }

        private void MainForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_isExplicitExit)
            {
                e.Cancel = true;
                SlideOutAndMinimizeToTray();
                return;
            }

            _animTimer?.Stop();
            _animTimer?.Dispose();
            try { TimeEndPeriod(1); } catch { }

            _borderOverlay?.Dispose();
            _trayIcon?.Dispose();
            _sessions.Dispose();
        }
    }
}
