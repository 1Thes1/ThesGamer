using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ThesGamer.Services;

public static class MemoryService
{
    /// <summary>
    /// Soft RAM relief: trim working sets of non-critical processes and empty standby list when possible.
    /// Requires admin for EmptyStandbyList; otherwise only trims working sets.
    /// </summary>
    public static string FreeMemory()
    {
        var before = GetAvailableMb();
        var trimmed = 0;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == Environment.ProcessId)
                    continue;

                var name = process.ProcessName;
                if (IsProtected(name))
                    continue;

                if (EmptyWorkingSet(process.Handle))
                    trimmed++;
            }
            catch
            {
                // ignore access-denied processes
            }
            finally
            {
                try { process.Dispose(); } catch { /* ignore */ }
            }
        }

        var standbyCleared = TryClearStandbyList();
        var after = GetAvailableMb();
        var gained = Math.Max(0, after - before);

        return standbyCleared
            ? $"Освобождено ~{gained:0} МБ (процессов подрезано: {trimmed}, standby очищен)."
            : $"Освобождено ~{gained:0} МБ (процессов подрезано: {trimmed}). Для standby запусти от администратора.";
    }

    private static bool IsProtected(string name)
    {
        string[] protectedNames =
        [
            "System", "Idle", "csrss", "smss", "wininit", "services", "lsass",
            "winlogon", "dwm", "explorer", "MsMpEng", "SecurityHealthService",
            "ThesGamer"
        ];
        return protectedNames.Any(p => name.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    private static double GetAvailableMb()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        GlobalMemoryStatusEx(ref status);
        return status.ullAvailPhys / (1024d * 1024d);
    }

    private static bool TryClearStandbyList()
    {
        try
        {
            // Privilege needed; fails quietly without admin.
            return NtSetSystemInformation(80, IntPtr.Zero, 0) == 0;
        }
        catch
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("ntdll.dll")]
    private static extern int NtSetSystemInformation(int infoClass, IntPtr info, int length);
}
