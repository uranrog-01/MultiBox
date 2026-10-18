using System.Runtime.InteropServices;

namespace MultiBox.Core;

internal static class SessionWindows
{
    private delegate bool EnumWindowProc(IntPtr window, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool EnumWindows(EnumWindowProc callback, IntPtr data);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);

    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080;
    private const int DwmwaCloaked = 14;
    private const int MinimumSize = 32;

    /// <summary>True when a process in the session still shows a window the user could interact with.</summary>
    public static bool HasVisibleWindow(IEnumerable<int> processIds)
    {
        var ids = processIds.ToHashSet();
        bool found = false;
        bool completed = EnumWindows((window, _) => {
            GetWindowThreadProcessId(window, out uint pid);
            if (ids.Contains((int)pid) && IsUserWindow(window)) { found = true; return false; }
            return true;
        }, IntPtr.Zero);
        if (!completed && !found) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Could not inspect this app's windows safely.");
        return found;
    }

    // Frameworks such as Tauri/Tao keep tiny "visible" helper windows (event targets,
    // single-instance message windows) alive after the real window closes. They are tool
    // windows the user never sees, so they must not count as "the app is still open".
    // Minimized windows still count: they can hold unsaved work.
    private static bool IsUserWindow(IntPtr window)
    {
        if (!IsWindowVisible(window)) return false;
        if (((long)GetWindowLongPtr(window, GwlExStyle) & WsExToolWindow) != 0) return false;
        if (DwmGetWindowAttribute(window, DwmwaCloaked, out int cloaked, sizeof(int)) == 0 && cloaked != 0) return false;
        if (IsIconic(window)) return true;
        return GetWindowRect(window, out Rect rect) && rect.Right - rect.Left >= MinimumSize && rect.Bottom - rect.Top >= MinimumSize;
    }
}
