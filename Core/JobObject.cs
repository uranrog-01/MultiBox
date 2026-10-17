using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text;

namespace MultiBox.Core
{
    public class JobObject : IDisposable
    {
        private IntPtr _handle;
        private bool _disposed;

        public JobObject()
        {
            _handle = NativeMethods.CreateJobObject(IntPtr.Zero, null);
            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to create Job Object. Win32 Error: {Marshal.GetLastWin32Error()}");
            }

            // Configure the job object so all assigned processes and child processes
            // are terminated automatically when the handle is closed or explicitly terminated.
            var extendedInfo = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };

            int length = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);

            try
            {
                Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);
                if (!NativeMethods.SetInformationJobObject(
                    _handle,
                    NativeMethods.JobObjectExtendedLimitInformation,
                    extendedInfoPtr,
                    (uint)length))
                {
                    throw new InvalidOperationException($"Failed to configure Job Object limits. Win32 Error: {Marshal.GetLastWin32Error()}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(extendedInfoPtr);
            }
        }

        public bool AssignProcess(Process process)
        {
            if (_disposed || _handle == IntPtr.Zero) return false;
            try
            {
                return NativeMethods.AssignProcessToJobObject(_handle, process.Handle);
            }
            catch
            {
                return false;
            }
        }

        // Assign while suspended so even very fast child processes inherit this job.
        public Process Start(ProcessStartInfo info)
        {
            var startup = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };
            string environment = string.Join('\0', info.Environment.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}")) + "\0\0";
            IntPtr block = Marshal.StringToHGlobalUni(environment);
            PROCESS_INFORMATION created = default;
            try
            {
                var command = new StringBuilder($"\"{info.FileName}\" {info.Arguments}");
                if (!CreateProcessW(info.FileName, command, IntPtr.Zero, IntPtr.Zero, false,
                    0x00000004 | 0x00000400 | (info.CreateNoWindow ? 0x08000000u : 0), block,
                    info.WorkingDirectory, ref startup, out created))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not start the app.");
                if (!NativeMethods.AssignProcessToJobObject(_handle, created.hProcess))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not attach the app to its session job.");
                var process = Process.GetProcessById((int)created.dwProcessId);
                if (ResumeThread(created.hThread) == uint.MaxValue)
                {
                    process.Dispose();
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resume the app.");
                }
                return process;
            }
            catch
            {
                if (created.hProcess != IntPtr.Zero) TerminateProcess(created.hProcess, 1);
                throw;
            }
            finally
            {
                if (created.hThread != IntPtr.Zero) NativeMethods.CloseHandle(created.hThread);
                if (created.hProcess != IntPtr.Zero) NativeMethods.CloseHandle(created.hProcess);
                Marshal.FreeHGlobal(block);
            }
        }

        public int[] GetProcessIds()
        {
            if (_disposed || _handle == IntPtr.Zero) return [];
            int capacity = 64;
            while (capacity <= 65536)
            {
                int size = 8 + capacity * IntPtr.Size;
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    if (QueryInformationJobObject(_handle, 3, buffer, (uint)size, IntPtr.Zero))
                    {
                        int count = Marshal.ReadInt32(buffer, 4);
                        var ids = new int[count];
                        for (int i = 0; i < count; i++) ids[i] = checked((int)Marshal.ReadIntPtr(buffer, 8 + i * IntPtr.Size));
                        return ids;
                    }
                    int error = Marshal.GetLastWin32Error();
                    if (error != 234) throw new Win32Exception(error);
                }
                finally { Marshal.FreeHGlobal(buffer); }
                capacity *= 2;
            }
            throw new InvalidOperationException("Session process list is too large.");
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public string? lpReserved, lpDesktop, lpTitle;
            public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
            public ushort wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public uint dwProcessId, dwThreadId; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcessW(string application, StringBuilder command, IntPtr processAttributes,
            IntPtr threadAttributes, bool inheritHandles, uint flags, IntPtr environment, string directory,
            ref STARTUPINFO startup, out PROCESS_INFORMATION process);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(IntPtr process, uint code);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryInformationJobObject(
            IntPtr job, int infoClass, IntPtr information, uint size, IntPtr returnedLength);

        public void Terminate(uint exitCode = 0)
        {
            if (_handle != IntPtr.Zero && !_disposed)
            {
                NativeMethods.TerminateJobObject(_handle, exitCode);
                NativeMethods.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Terminate();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~JobObject()
        {
            Dispose();
        }
    }
}

