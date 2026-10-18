using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace MultiBox.Core;

/// <summary>
/// Launch fixes for apps whose second copy collides with the first through something a vault
/// cannot redirect: a fixed localhost port or a session-wide named single-instance object.
/// Every step is best-effort; a failure here must never block the launch itself.
/// </summary>
internal static class AppCompatibility
{
    private static readonly object PortLock = new();
    private static readonly Dictionary<string, int> IssuedPorts = new(StringComparer.Ordinal);

    public static void Prepare(ProcessStartInfo startInfo, string targetExe, EphemeralVault vault)
    {
        // XDG tools (OpenCode, Bun/Node CLIs) fall back to ~/.local/state and ~/.cache.
        // Pin them to the vault even when the host environment sets them globally.
        startInfo.Environment["XDG_STATE_HOME"] = Path.Combine(vault.UserProfile, ".local", "state");
        startInfo.Environment["XDG_CACHE_HOME"] = Path.Combine(vault.UserProfile, ".cache");

        string dir = Path.GetDirectoryName(targetExe) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(targetExe);

        // OpenCode Desktop starts `opencode-cli serve --service` on a fixed port (45278 on Windows).
        // A vault cannot see the host's service registration, so it would start a second service on the same port.
        if (name.Contains("opencode", StringComparison.OrdinalIgnoreCase) || File.Exists(Path.Combine(dir, "resources", "opencode-cli.exe")))
            Try(() => PrepareOpenCode(vault));

        // Cline's backend probes a shared hub on port 25463 and would attach to (or fail against) the host's hub.
        if (name.StartsWith("cline", StringComparison.OrdinalIgnoreCase) || File.Exists(Path.Combine(dir, "cline-cli.exe")))
            Try(() => startInfo.Environment["CLINE_HUB_PORT"] = ReservePort(vault.VaultId + ":cline-hub", 26000, 1000).ToString());

        // WebView2 apps that never choose a data folder use "<exe>.WebView2" beside the exe, shared by every copy.
        // Only then redirect it; apps that pick their own folder (Tauri: %LOCALAPPDATA%\<id>\EBWebView) already follow the vault,
        // and the variable would override their choice and move data saved profiles expect.
        string sharedWebView = Path.Combine(dir, Path.GetFileName(targetExe) + ".WebView2");
        if (Directory.Exists(sharedWebView))
            startInfo.Environment["WEBVIEW2_USER_DATA_FOLDER"] = Path.Combine(vault.LocalAppData, Path.GetFileName(targetExe) + ".WebView2");

        // Tauri and similar apps forward to an already running copy when their named mutex exists.
        Try(() => SingleInstanceLocks.Release(targetExe));
    }

    /// <summary>
    /// Explains a failed start in plain language. Microsoft Store apps (C:\Program Files\WindowsApps) normally
    /// start fine by path, e.g. Codex/ChatGPT and Claude desktop; only when Windows denies one is this shown.
    /// </summary>
    public static string DescribeStartFailure(string targetExe, Exception error)
    {
        if (error is Win32Exception { NativeErrorCode: 5 } && targetExe.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase))
            return "Windows didn't allow this Microsoft Store app to start outside its package. Try the app's regular desktop (.exe) version instead.";
        return error.Message;
    }

    private static void PrepareOpenCode(EphemeralVault vault)
    {
        int port = ReservePort(vault.VaultId + ":opencode-service", 46000, 1000);
        string configDir = Path.Combine(vault.UserProfile, ".config", "opencode");
        Directory.CreateDirectory(configDir);
        // Same file `opencode service set port <port>` writes. Keep its password and other keys.
        string config = Path.Combine(configDir, "service.json");
        var files = Directory.EnumerateFiles(configDir, "service*.json").Append(config).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (string file in files)
        {
            JsonObject json;
            try { json = File.Exists(file) ? JsonNode.Parse(File.ReadAllText(file)) as JsonObject ?? new() : new(); }
            catch (System.Text.Json.JsonException) { json = new(); }
            json["port"] = port;
            File.WriteAllText(file, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
        // A restored profile carries the old service's pid/url registration. That pid may now belong to
        // an unrelated process, which the desktop app would try to stop. Let the new service register itself.
        string stateDir = Path.Combine(vault.UserProfile, ".local", "state", "opencode");
        if (Directory.Exists(stateDir))
            foreach (string file in Directory.EnumerateFiles(stateDir, "service*.json*"))
                Try(() => File.Delete(file));
    }

    /// <summary>Picks a free loopback port, stable per key while it stays free, never shared between keys.</summary>
    internal static int ReservePort(string key, int first, int count)
    {
        lock (PortLock)
        {
            if (IssuedPorts.TryGetValue(key, out int previous) && IsFree(previous)) return previous;
            var taken = IssuedPorts.Where(pair => pair.Key != key).Select(pair => pair.Value).ToHashSet();
            var listening = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Select(endpoint => endpoint.Port).ToHashSet();
            int start = (int)(BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0) % (uint)count);
            for (int i = 0; i < count; i++)
            {
                int port = first + (start + i) % count;
                if (taken.Contains(port) || listening.Contains(port) || !IsFree(port)) continue;
                return IssuedPorts[key] = port;
            }
            var any = new TcpListener(IPAddress.Loopback, 0);
            any.Start();
            try { return IssuedPorts[key] = ((IPEndPoint)any.LocalEndpoint).Port; }
            finally { any.Stop(); }
        }
    }

    private static bool IsFree(int port)
    {
        // Binding also rejects Windows' excluded port ranges (Hyper-V, WinNAT).
        foreach (var address in new[] { IPAddress.Loopback, IPAddress.Any })
        {
            var listener = new TcpListener(address, port) { ExclusiveAddressUse = true };
            try { listener.Start(); }
            catch (SocketException) { return false; }
            finally { listener.Stop(); }
        }
        return true;
    }

    private static void Try(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SocketException or NetworkInformationException or Win32Exception or InvalidOperationException) { }
    }
}

/// <summary>
/// Closes single-instance mutexes held by other running copies of the same executable so a vault copy
/// starts as its own instance instead of handing its arguments to the existing window and exiting.
/// Only mutexes whose names follow known single-instance conventions are touched.
/// </summary>
internal static class SingleInstanceLocks
{
    public static int Release(string targetExe)
    {
        string fullPath = Path.GetFullPath(targetExe);
        var targets = new Dictionary<int, IntPtr>();
        int released = 0;
        try
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(fullPath)))
            {
                using (process)
                {
                    if (process.Id == Environment.ProcessId) continue;
                    IntPtr handle = OpenProcess(ProcessDupHandle | ProcessQueryLimitedInformation, false, process.Id);
                    if (handle == IntPtr.Zero) continue;
                    if (string.Equals(ImagePath(handle), fullPath, StringComparison.OrdinalIgnoreCase)) targets[process.Id] = handle;
                    else CloseHandle(handle);
                }
            }
            if (targets.Count == 0) return 0;

            using var probe = new Mutex(false);
            byte[] table = HandleTable();
            int entrySize = Marshal.SizeOf<HandleEntry>();
            long count = (long)BitConverter.ToUInt64(table, 0);
            int offset = 2 * IntPtr.Size;
            ushort mutantType = 0;
            IntPtr probeHandle = probe.SafeWaitHandle.DangerousGetHandle();
            for (long i = 0; i < count && mutantType == 0; i++)
            {
                var entry = MemoryMarshal.Read<HandleEntry>(table.AsSpan(offset + (int)i * entrySize, entrySize));
                if ((int)entry.UniqueProcessId == Environment.ProcessId && entry.HandleValue == probeHandle) mutantType = entry.ObjectTypeIndex;
            }
            if (mutantType == 0) return 0;

            IntPtr self = GetCurrentProcess();
            for (long i = 0; i < count; i++)
            {
                var entry = MemoryMarshal.Read<HandleEntry>(table.AsSpan(offset + (int)i * entrySize, entrySize));
                if (entry.ObjectTypeIndex != mutantType || !targets.TryGetValue((int)entry.UniqueProcessId, out IntPtr owner)) continue;
                if (!DuplicateHandle(owner, entry.HandleValue, self, out IntPtr copy, 0, false, DuplicateSameAccess)) continue;
                string name;
                try { name = ObjectName(copy); }
                finally { CloseHandle(copy); }
                if (!IsSingleInstanceName(name)) continue;
                if (DuplicateHandle(owner, entry.HandleValue, IntPtr.Zero, out _, 0, false, DuplicateCloseSource)) released++;
            }
            return released;
        }
        finally
        {
            foreach (IntPtr handle in targets.Values) CloseHandle(handle);
        }
    }

    internal static bool IsSingleInstanceName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        string name = objectName[(objectName.LastIndexOf('\\') + 1)..].ToLowerInvariant();
        // Tauri's single-instance plugin: "<identifier>-sim" (window class "-sic", window "-siw").
        return name.EndsWith("-sim", StringComparison.Ordinal) && name.Length > 4 ||
            name.Contains("singleinstance") || name.Contains("single-instance") || name.Contains("single_instance");
    }

    private static string? ImagePath(IntPtr process)
    {
        var buffer = new StringBuilder(1024);
        int size = buffer.Capacity;
        return QueryFullProcessImageName(process, 0, buffer, ref size) ? buffer.ToString(0, size) : null;
    }

    private static byte[] HandleTable()
    {
        int size = 1 << 20;
        while (true)
        {
            byte[] buffer = new byte[size];
            int status = NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, size, out int needed);
            if (status == 0) return buffer;
            if (status != StatusInfoLengthMismatch || size >= 1 << 28) throw new Win32Exception(RtlNtStatusToDosError(status));
            size = Math.Max(size * 2, needed + (1 << 16));
        }
    }

    private static string ObjectName(IntPtr handle)
    {
        // Unmanaged so the UNICODE_STRING's pointer into this buffer stays valid after the call.
        const int size = 2048;
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (NtQueryObject(handle, ObjectNameInformation, buffer, size, out _) != 0) return string.Empty;
            // UNICODE_STRING { USHORT Length; USHORT MaximumLength; PWSTR Buffer; }
            int length = (ushort)Marshal.ReadInt16(buffer);
            IntPtr text = Marshal.ReadIntPtr(buffer, IntPtr.Size);
            return length == 0 || text == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(text, length / 2);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HandleEntry
    {
        public IntPtr Object;
        public IntPtr UniqueProcessId;
        public IntPtr HandleValue;
        public uint GrantedAccess;
        public ushort CreatorBackTraceIndex;
        public ushort ObjectTypeIndex;
        public uint HandleAttributes;
        public uint Reserved;
    }

    private const int SystemExtendedHandleInformation = 64;
    private const int ObjectNameInformation = 1;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
    private const uint ProcessDupHandle = 0x0040;
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint DuplicateCloseSource = 0x1;
    private const uint DuplicateSameAccess = 0x2;

    [DllImport("ntdll.dll")] private static extern int NtQuerySystemInformation(int infoClass, byte[] info, int length, out int returnLength);
    [DllImport("ntdll.dll")] private static extern int NtQueryObject(IntPtr handle, int infoClass, IntPtr info, int length, out int returnLength);
    [DllImport("ntdll.dll")] private static extern int RtlNtStatusToDosError(int status);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DuplicateHandle(IntPtr sourceProcess, IntPtr sourceHandle, IntPtr targetProcess, out IntPtr targetHandle, uint access, bool inherit, uint options);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);
}
