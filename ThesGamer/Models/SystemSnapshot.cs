namespace ThesGamer.Models;

public sealed class SystemSnapshot
{
    public double CpuPercent { get; init; }
    public double RamPercent { get; init; }
    public double RamUsedGb { get; init; }
    public double RamTotalGb { get; init; }
    public double RamAvailableGb { get; init; }
    public double DiskPercent { get; init; }
    public double DiskUsedGb { get; init; }
    public double DiskTotalGb { get; init; }
    public double DiskFreeGb { get; init; }
    public int ProcessCount { get; init; }
    public string Hostname { get; init; } = "";
    public string OsName { get; init; } = "";
    public TimeSpan Uptime { get; init; }
}

public sealed class ProcessRow
{
    public int Pid { get; init; }
    public string Name { get; init; } = "";
    public double CpuPercent { get; init; }
    public double MemoryMb { get; init; }
}

public sealed class DriverRow
{
    public string DeviceName { get; init; } = "";
    public string DriverVersion { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string DriverDate { get; init; } = "";
}

public sealed class CleanupResult
{
    public long BytesFreed { get; init; }
    public int FilesRemoved { get; init; }
    public string Message { get; init; } = "";
}
