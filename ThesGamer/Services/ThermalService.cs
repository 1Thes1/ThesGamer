using System.Management;

namespace ThesGamer.Services;

public static class ThermalService
{
    public sealed record Temps(string Cpu, string Gpu);

    public static Temps Read()
    {
        var cpu = ReadCpuWmi() ?? Loc.T("na");
        var gpu = ReadGpuHeuristic() ?? Loc.T("na");
        return new Temps(cpu, gpu);
    }

    private static string? ReadCpuWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentTemperature"] is null) continue;
                var kelvinTenths = Convert.ToDouble(obj["CurrentTemperature"]);
                var celsius = kelvinTenths / 10.0 - 273.15;
                if (celsius is > 0 and < 125)
                    return $"{celsius:0}°C";
            }
        }
        catch { /* blocked */ }

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Temperature FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["Temperature"] is null) continue;
                var celsius = Convert.ToDouble(obj["Temperature"]);
                if (celsius is > 0 and < 125)
                    return $"{celsius:0}°C";
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private static string? ReadGpuHeuristic()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=temperature.gpu --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return null;
            var output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(2500);
            if (p.ExitCode == 0 && double.TryParse(output.Split('\n')[0].Trim(), out var c) && c is > 0 and < 125)
                return $"{c:0}°C";
        }
        catch { /* no nvidia-smi */ }

        return null;
    }
}
