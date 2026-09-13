using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ThesGamer.Models;

namespace ThesGamer.Services;

public static class SystemMonitorService
{
    private static readonly PerformanceCounter? CpuCounter = CreateCpuCounter();

    private static PerformanceCounter? CreateCpuCounter()
    {
        try
        {
            var counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = counter.NextValue();
            return counter;
        }
        catch
        {
            return null;
        }
    }

    public static SystemSnapshot GetSnapshot()
    {
        var memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        GlobalMemoryStatusEx(ref memStatus);

        var ramTotal = memStatus.ullTotalPhys / (1024d * 1024d * 1024d);
        var ramAvail = memStatus.ullAvailPhys / (1024d * 1024d * 1024d);
        var ramUsed = ramTotal - ramAvail;
        var ramPercent = memStatus.dwMemoryLoad;

        DriveInfo? systemDrive = DriveInfo.GetDrives()
            .FirstOrDefault(d => d.IsReady && d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase));

        double diskTotal = 0, diskFree = 0, diskUsed = 0, diskPercent = 0;
        if (systemDrive is not null)
        {
            diskTotal = systemDrive.TotalSize / (1024d * 1024d * 1024d);
            diskFree = systemDrive.AvailableFreeSpace / (1024d * 1024d * 1024d);
            diskUsed = diskTotal - diskFree;
            diskPercent = diskTotal <= 0 ? 0 : (diskUsed / diskTotal) * 100;
        }

        var cpu = CpuCounter?.NextValue() ?? 0;
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

        return new SystemSnapshot
        {
            CpuPercent = Math.Clamp(cpu, 0, 100),
            RamPercent = ramPercent,
            RamUsedGb = ramUsed,
            RamTotalGb = ramTotal,
            RamAvailableGb = ramAvail,
            DiskPercent = diskPercent,
            DiskUsedGb = diskUsed,
            DiskTotalGb = diskTotal,
            DiskFreeGb = diskFree,
            ProcessCount = Process.GetProcesses().Length,
            Hostname = Environment.MachineName,
            OsName = $"{Environment.OSVersion.VersionString}",
            Uptime = uptime
        };
    }

    public static IReadOnlyList<ProcessRow> GetTopProcesses(int limit = 10)
    {
        return Process.GetProcesses()
            .Select(p =>
            {
                try
                {
                    return new ProcessRow
                    {
                        Pid = p.Id,
                        Name = string.IsNullOrWhiteSpace(p.ProcessName) ? "?" : p.ProcessName,
                        CpuPercent = 0,
                        MemoryMb = p.WorkingSet64 / (1024d * 1024d)
                    };
                }
                catch
                {
                    return null;
                }
            })
            .Where(p => p is not null)
            .Cast<ProcessRow>()
            .OrderByDescending(p => p.MemoryMb)
            .Take(limit)
            .ToList();
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
}
