using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MultiBox.Core
{
    public class IsolatedAppBorderOverlay : Form
    {
        private readonly IsolationEngine _engine;
        private readonly System.Windows.Forms.Timer _syncTimer;
        private readonly string _sessionLabel;
        private readonly string _sessionColor;
        private string _mode;
        private IntPtr _targetHwnd, _hook;
        private NativeMethods.WinEventDelegate? _winEventProc;
        private Bitmap? _surface;
        private long _enteredAt;
        private bool _wasForeground;
        private byte _lastAlpha;
        private int _currentX = int.MinValue, _currentY = int.MinValue;

        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Tool window: never appears on the taskbar, takes focus or intercepts a click.
                cp.ExStyle |= 0x00000080 | 0x08000000 | 0x00000020 | 0x00080000 | 0x00000008;
                return cp;
            }
        }
        public IsolatedAppBorderOverlay(IsolationEngine engine, string mode = "label", string? sessionLabel = null, string sessionColor = "lime")
        {
            _engine = engine;
            _mode = IndicatorPreferences.IsValid(mode) ? mode : "label";
            _sessionLabel = sessionLabel ?? Path.GetFileNameWithoutExtension(engine.CurrentAppName);
            _sessionColor = SessionIdentity.Colors.ContainsKey(sessionColor) ? sessionColor : "lime";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Text = "MultiBox session indicator";
            _syncTimer = new System.Windows.Forms.Timer { Interval = 60 };
            _syncTimer.Tick += (_, _) => SyncWithTargetWindow();
            _syncTimer.Start();
            _winEventProc = WinEventCallback;
            _hook = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, _winEventProc, 0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT);
        }
        public void SetMode(string mode)
        {
            if (!IndicatorPreferences.IsValid(mode) || mode == _mode) return;
            _mode = mode;
            _wasForeground = false;
            _surface?.Dispose();
            _surface = null;
            SyncWithTargetWindow();
        }
        protected override void OnPaint(PaintEventArgs e) { }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        private void WinEventCallback(IntPtr hook, uint eventType, IntPtr hwnd, int objectId, int childId, uint thread, uint time)
        {
            if (hwnd == IntPtr.Zero || !IsHandleCreated || IsDisposed) return;
            if (eventType != NativeMethods.EVENT_SYSTEM_FOREGROUND &&
                (eventType != NativeMethods.EVENT_OBJECT_LOCATIONCHANGE || hwnd != _targetHwnd)) return;
            try { BeginInvoke((Action)SyncWithTargetWindow); } catch (InvalidOperationException) { }
        }
        private void HideIndicator()
        {
            if (Visible) Hide();
        }
        private void SyncWithTargetWindow()
        {
            if (IsDisposed) return;
            if (!_engine.IsActive && !_engine.IsPreserved) { Close(); return; }
            try
            {
                if (_mode == "off") { HideIndicator(); _wasForeground = false; _syncTimer.Interval = 300; return; }
                IntPtr foreground = NativeMethods.GetForegroundWindow();
                NativeMethods.GetWindowThreadProcessId(foreground, out uint pid);
                if (foreground == IntPtr.Zero || !_engine.GetProcessIds().Contains((int)pid) ||
                    NativeMethods.IsIconic(foreground) || !NativeMethods.IsWindowVisible(foreground))
                {
                    HideIndicator(); _wasForeground = false; _syncTimer.Interval = 250; return;
                }
                if (!_wasForeground || _targetHwnd != foreground)
                {
                    _enteredAt = Stopwatch.GetTimestamp();
                    _targetHwnd = foreground;
                    _surface?.Dispose(); _surface = null;
                }
                _wasForeground = true;
                byte alpha = IndicatorPreferences.Opacity(_mode, Stopwatch.GetElapsedTime(_enteredAt).TotalMilliseconds);
                if (alpha == 0) { HideIndicator(); _syncTimer.Interval = 250; return; }
                _syncTimer.Interval = 60;
                NativeMethods.RECT rect;
                if (NativeMethods.IsZoomed(_targetHwnd))
                {
                    var area = Screen.FromHandle(_targetHwnd).WorkingArea;
                    rect = new NativeMethods.RECT { Left = area.Left, Top = area.Top, Right = area.Right, Bottom = area.Bottom };
                }
                else if (NativeMethods.DwmGetWindowAttribute(_targetHwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                    out rect, Marshal.SizeOf<NativeMethods.RECT>()) != 0 &&
                    !NativeMethods.GetWindowRect(_targetHwnd, out rect)) return;
                if (rect.Width < 120 || rect.Height < 80) { HideIndicator(); return; }
                using var labelFont = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
                int desiredWidth = Math.Clamp(TextRenderer.MeasureText(_sessionLabel, labelFont).Width + 42, 116, 208);
                var target = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
                var bounds = PillBounds(target, Screen.FromHandle(_targetHwnd).WorkingArea, desiredWidth);
                int width = bounds.Width, height = bounds.Height, x = bounds.X, y = bounds.Y;
                // A four-pixel entrance, with no bouncing or persistent animation.
                y -= (int)Math.Round(4 * (1 - Math.Clamp(Stopwatch.GetElapsedTime(_enteredAt).TotalMilliseconds / 160, 0, 1)));
                bool render = _surface == null || _surface.Width != width || _surface.Height != height;
                if (render)
                {
                    _surface?.Dispose();
                    _surface = CreateSurface(_mode, width, height, _sessionLabel, _sessionColor);
                }
                if (render || alpha != _lastAlpha || !Visible)
                {
                    if (!Visible) Show();
                    ApplyLayeredBitmap(_surface!, x, y, alpha);
                    _lastAlpha = alpha; _currentX = x; _currentY = y;
                }
                else if (x != _currentX || y != _currentY)
                {
                    NativeMethods.SetWindowPos(Handle, new IntPtr(-1), x, y, 0, 0,
                        NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
                    _currentX = x; _currentY = y;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or ArgumentException)
            { HideIndicator(); _wasForeground = false; }
        }
        internal static Rectangle PillBounds(Rectangle target, Rectangle workingArea, int desiredWidth)
        {
            int width = Math.Clamp(desiredWidth, 80, Math.Min(208, Math.Max(80, target.Width - 24)));
            const int height = 26;
            int x = Math.Clamp(target.Left + (target.Width - width) / 2, workingArea.Left + 4, Math.Max(workingArea.Left + 4, workingArea.Right - width - 4));
            int above = target.Top - height - 6;
            // Prefer space outside the app. Maximized windows use a brief titlebar hint.
            int y = above >= workingArea.Top + 6 ? above : Math.Max(workingArea.Top + 6, target.Top + 5);
            y = Math.Min(y, workingArea.Bottom - height - 4);
            return new Rectangle(x, y, width, height);
        }
        internal static Bitmap CreateSurface(string mode, int width, int height, string label, string color = "lime")
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                if (mode == "label")
                {
                    using var path = RoundedPath(1, 1, width - 3, height - 3, 12);
                    using var background = new SolidBrush(Color.FromArgb(242, 18, 23, 21));
                    using var edge = new Pen(Color.FromArgb(42, 180, 202, 190), 1);
                    g.FillPath(background, path); g.DrawPath(edge, path);
                    var accent = ColorTranslator.FromHtml(SessionIdentity.Colors.TryGetValue(color, out string? hex) ? hex : SessionIdentity.Colors["lime"]);
                    using var brand = new Pen(Color.FromArgb(225, accent), 1.2f) { LineJoin = LineJoin.Round };
                    g.DrawRectangle(brand, 10, 8, 8, 8); g.DrawRectangle(brand, 13, 11, 8, 8);
                    using var selected = RoundedPath(28, 4, width - 33, height - 9, 8);
                    using var segment = new SolidBrush(Color.FromArgb(22, accent));
                    g.FillPath(segment, selected);
                    using var text = new SolidBrush(Color.FromArgb(230, 215, 228, 222));
                    using var font = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
                    using var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                    g.DrawString(label, font, text, new RectangleF(35, 1, width - 44, height - 3), format);
                }
            }
            PremultiplyAlpha(bitmap);
            return bitmap;
        }
        private static GraphicsPath RoundedPath(int x, int y, int width, int height, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void PremultiplyAlpha(Bitmap bitmap)
        {
            var data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );

            try
            {
                int stride = data.Stride;
                IntPtr scan0 = data.Scan0;
                int width = bitmap.Width;
                int height = bitmap.Height;

                unsafe
                {
                    byte* row = (byte*)scan0;
                    for (int y = 0; y < height; y++)
                    {
                        byte* p = row;
                        for (int x = 0; x < width; x++)
                        {
                            byte a = p[3];
                            if (a == 0)
                            {
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = 0;
                            }
                            else if (a < 255)
                            {
                                p[0] = (byte)((p[0] * a + 127) / 255);
                                p[1] = (byte)((p[1] * a + 127) / 255);
                                p[2] = (byte)((p[2] * a + 127) / 255);
                            }
                            p += 4;
                        }
                        row += stride;
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private void ApplyLayeredBitmap(Bitmap bmp, int posX, int posY, byte opacity)
        {
            IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

                var size = new NativeMethods.SIZE(bmp.Width, bmp.Height);
                var pointSource = new NativeMethods.POINT(0, 0);
                var topPos = new NativeMethods.POINT(posX, posY);
                var blend = new NativeMethods.BLENDFUNCTION(
                    NativeMethods.AC_SRC_OVER,
                    0,
                    opacity,
                    NativeMethods.AC_SRC_ALPHA
                );

                NativeMethods.UpdateLayeredWindow(
                    this.Handle,
                    screenDc,
                    ref topPos,
                    ref size,
                    memDc,
                    ref pointSource,
                    0,
                    ref blend,
                    NativeMethods.ULW_ALPHA
                );
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero) NativeMethods.SelectObject(memDc, oldBitmap);
                if (hBitmap != IntPtr.Zero) NativeMethods.DeleteObject(hBitmap);
                NativeMethods.DeleteDC(memDc);
                NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _surface?.Dispose();
                _surface = null;
                _syncTimer.Stop();
                _syncTimer.Dispose();

                if (_hook != IntPtr.Zero)
                {
                    NativeMethods.UnhookWinEvent(_hook);
                    _hook = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }
    }
}
