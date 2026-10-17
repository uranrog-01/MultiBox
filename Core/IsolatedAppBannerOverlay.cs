using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MultiBox.Core
{
    public class IsolatedAppBannerOverlay : Form
    {
        private readonly IsolationEngine _engine;
        private readonly System.Windows.Forms.Timer _trackerTimer;
        private IntPtr _targetHwnd = IntPtr.Zero;
        private bool _titleModified = false;
        private string _originalTitle = string.Empty;
        private Point _dragOffset;
        private bool _isDragging;

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
                return cp;
            }
        }

        public IsolatedAppBannerOverlay(IsolationEngine engine)
        {
            _engine = engine;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(460, 38);
            this.BackColor = Color.FromArgb(12, 14, 18);
            this.DoubleBuffered = true;

            UpdateRegion();

            _trackerTimer = new System.Windows.Forms.Timer
            {
                Interval = 80
            };
            _trackerTimer.Tick += TrackerTimer_Tick;
            _trackerTimer.Start();

            this.MouseDown += Overlay_MouseDown;
            this.MouseMove += Overlay_MouseMove;
            this.MouseUp += Overlay_MouseUp;
            this.Paint += Overlay_Paint;
            this.Cursor = Cursors.SizeAll;
        }

        private void UpdateRegion()
        {
            try
            {
                IntPtr rgn = CreateRoundRectRgn(0, 0, this.Width, this.Height, 16, 16);
                this.Region = Region.FromHrgn(rgn);
                DeleteObject(rgn);
            }
            catch { }
        }

        private void TrackerTimer_Tick(object? sender, EventArgs e)
        {
            if (!_engine.IsActive)
            {
                _trackerTimer.Stop();
                this.Close();
                return;
            }

            try
            {
                // Ensure target HWND is located
                if (_targetHwnd == IntPtr.Zero || !IsWindow(_targetHwnd) || !IsWindowVisible(_targetHwnd))
                {
                    _targetHwnd = FindIsolatedAppWindow();

                    if (_targetHwnd != IntPtr.Zero && !_titleModified)
                    {
                        TagTargetWindowTitle();
                    }
                }

                if (_targetHwnd != IntPtr.Zero && IsWindow(_targetHwnd))
                {
                    if (IsIconic(_targetHwnd) || !IsWindowVisible(_targetHwnd))
                    {
                        if (this.Visible) this.Hide();
                        return;
                    }

                    if (!this.Visible) this.Show();

                    if (!_isDragging && GetWindowRect(_targetHwnd, out RECT rect))
                    {
                        int targetWidth = rect.Right - rect.Left;
                        int targetHeight = rect.Bottom - rect.Top;

                        if (targetWidth > 150 && targetHeight > 100)
                        {
                            int bannerX = rect.Left + (targetWidth - this.Width) / 2;
                            int bannerY = rect.Top + 6;

                            if (bannerY < 0) bannerY = rect.Top + 32;

                            if (this.Location.X != bannerX || this.Location.Y != bannerY)
                            {
                                this.Location = new Point(bannerX, bannerY);
                            }
                        }
                    }
                }
                else
                {
                    // Fallback to top center of main display if window handle not yet acquired
                    if (!this.Visible)
                    {
                        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
                        this.Location = new Point((screen.Width - this.Width) / 2, screen.Top + 12);
                        this.Show();
                    }
                }

                this.Invalidate();
            }
            catch
            {
            }
        }

        private IntPtr FindIsolatedAppWindow()
        {
            int mainPid = _engine.ActiveProcess?.Id ?? 0;
            string targetName = Path.GetFileNameWithoutExtension(_engine.CurrentAppName).ToLowerInvariant();
            IntPtr candidateHwnd = IntPtr.Zero;
            long candidateArea = 0;

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                if (IsIconic(hWnd)) return true;

                NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid);
                if (windowPid == 0) return true;

                if (!GetWindowRect(hWnd, out RECT rect)) return true;
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width < 200 || height < 150) return true;

                // 1. Direct PID match with active process
                if (mainPid != 0 && windowPid == mainPid)
                {
                    long area = (long)width * height;
                    if (area > candidateArea)
                    {
                        candidateArea = area;
                        candidateHwnd = hWnd;
                    }
                    return true;
                }

                // 2. Child/sibling process match by target app name (e.g. ChatGPT, Codex, Chrome)
                try
                {
                    using var p = Process.GetProcessById((int)windowPid);
                    string procName = p.ProcessName.ToLowerInvariant();

                    bool matchesName = 
                        procName.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
                        (targetName.Contains("codex") && procName.Contains("chatgpt")) ||
                        (targetName.Contains("chatgpt") && procName.Contains("codex"));

                    if (matchesName)
                    {
                        var sb = new StringBuilder(256);
                        GetWindowText(hWnd, sb, 256);
                        string title = sb.ToString();

                        long area = (long)width * height;
                        if (area > candidateArea)
                        {
                            candidateArea = area;
                            candidateHwnd = hWnd;
                        }
                    }
                }
                catch { }

                return true;
            }, IntPtr.Zero);

            return candidateHwnd;
        }

        private void TagTargetWindowTitle()
        {
            try
            {
                var sb = new StringBuilder(512);
                GetWindowText(_targetHwnd, sb, 512);
                _originalTitle = sb.ToString();

                string tag = "[🔒 MultiBox Isolated] ";
                if (!_originalTitle.StartsWith(tag) && !string.IsNullOrWhiteSpace(_originalTitle))
                {
                    SetWindowText(_targetHwnd, tag + _originalTitle);
                    _titleModified = true;
                }
            }
            catch { }
        }

        private void Overlay_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            // Sleek dark glass gradient
            using (var brush = new LinearGradientBrush(bounds, Color.FromArgb(20, 24, 32), Color.FromArgb(9, 11, 15), 90f))
            {
                g.FillRectangle(brush, bounds);
            }

            // Glowing neon lime border
            using (var pen = new Pen(Color.FromArgb(180, 240, 0), 1.5f))
            {
                g.DrawRectangle(pen, bounds);
            }

            // Pulsing shield dot
            using (var dotBrush = new SolidBrush(Color.FromArgb(180, 240, 0)))
            {
                g.FillEllipse(dotBrush, 14, 14, 10, 10);
            }

            using (var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontSub = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (var brushBright = new SolidBrush(Color.White))
            using (var brushAccent = new SolidBrush(Color.FromArgb(180, 240, 0)))
            using (var brushDim = new SolidBrush(Color.FromArgb(150, 158, 175)))
            {
                g.DrawString("ISOLATED", fontBold, brushAccent, new PointF(30, 10));

                string appName = _engine.CurrentAppName;
                if (appName.Length > 22) appName = appName.Substring(0, 20) + "...";

                string infoText = $"• {appName}";
                g.DrawString(infoText, fontBold, brushBright, new PointF(96, 10));

                g.DrawString("EPHEMERAL VAULT", fontSub, brushDim, new PointF(this.Width - 215, 11));

                // Right-aligned "Wipe & Close" button
                var wipeBtnRect = new Rectangle(this.Width - 96, 6, 86, 26);
                using (var wipeBg = new SolidBrush(Color.FromArgb(48, 18, 24)))
                using (var wipePen = new Pen(Color.FromArgb(255, 71, 87), 1f))
                {
                    g.FillRectangle(wipeBg, wipeBtnRect);
                    g.DrawRectangle(wipePen, wipeBtnRect);
                }

                using (var wipeTextBrush = new SolidBrush(Color.FromArgb(255, 110, 130)))
                using (var wipeFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                {
                    g.DrawString("✕ WIPE", wipeFont, wipeTextBrush, new PointF(this.Width - 76, 11));
                }
            }
        }

        private void Overlay_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Click on "✕ WIPE" button area
                if (e.X >= this.Width - 96 && e.X <= this.Width - 10 && e.Y >= 6 && e.Y <= 32)
                {
                    _trackerTimer.Stop();
                    this.Hide();
                    System.Threading.Tasks.Task.Run(() => _engine.TerminateAndWipe());
                    return;
                }

                _isDragging = true;
                _dragOffset = e.Location;
            }
        }

        private void Overlay_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                this.Location = new Point(
                    this.Location.X + (e.X - _dragOffset.X),
                    this.Location.Y + (e.Y - _dragOffset.Y)
                );
            }
        }

        private void Overlay_MouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trackerTimer.Stop();
                _trackerTimer.Dispose();

                if (_titleModified && _targetHwnd != IntPtr.Zero && IsWindow(_targetHwnd))
                {
                    try { SetWindowText(_targetHwnd, _originalTitle); } catch { }
                }
            }
            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}

