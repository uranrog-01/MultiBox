using System.Runtime.InteropServices;

namespace MultiBox;

static class Program
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteFile(string lpFileName);

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        UnblockSelf();

        ApplicationConfiguration.Initialize();

        if (args.Length == 1 && args[0] == "--save-open-profile")
        {
            // This helper deliberately creates no SessionManager and performs no startup cleanup.
            Application.Run(new SaveOpenProfileForm());
            return;
        }

        IntPtr existingWnd = FindWindow(null, "MultiBox");
        if (existingWnd != IntPtr.Zero)
        {
            string payload = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : "__ACTIVATE__";
            IntPtr ptr = Marshal.StringToHGlobalUni(payload);
            try
            {
                var cds = new COPYDATASTRUCT
                {
                    dwData = IntPtr.Zero,
                    cbData = payload.Length * 2,
                    lpData = ptr
                };
                SendMessage(existingWnd, 0x004A /* WM_COPYDATA */, IntPtr.Zero, ref cds);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return;
        }

        Application.Run(new MainForm(args.Length > 0 ? args[0] : null));
    }

    public static void UnblockSelf()
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                DeleteFile(exePath + ":Zone.Identifier");

                // Also unblock surrounding directory files if downloaded together
                string? dir = Path.GetDirectoryName(exePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly))
                    {
                        DeleteFile(file + ":Zone.Identifier");
                    }
                }
            }
        }
        catch { }
    }
}
