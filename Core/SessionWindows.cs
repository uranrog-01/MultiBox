using System.Runtime.InteropServices;

namespace MultiBox.Core;

internal static class SessionWindows
{
    private delegate bool EnumWindowProc(IntPtr window, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool EnumWindows(EnumWindowProc callback, IntPtr data);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);

    public static bool HasVisibleWindow(IEnumerable<int> processIds)
    {
        var ids = processIds.ToHashSet();
        bool found = false;
        bool completed = EnumWindows((window, _) => {
            GetWindowThreadProcessId(window, out uint pid);
            if (ids.Contains((int)pid) && IsWindowVisible(window)) { found = true; return false; }
            return true;
        }, IntPtr.Zero);
        if (!completed && !found) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Could not inspect this app's windows safely.");
        return found;
    }
}
