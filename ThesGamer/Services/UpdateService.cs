using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

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
        catch
        {
            // many PCs block this WMI class without admin / OEM support
        }

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
        catch
        {
            // ignore
        }

        return null;
    }

    private static string? ReadGpuHeuristic()
    {
        // Best-effort: nvidia-smi if present
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
        catch
        {
            // AMD / no nvidia-smi
        }

        return null;
    }
}

public static class UpdateService
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ThesGamer", "0.3.0"));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return c;
    }

    public sealed record UpdateResult(bool Ok, bool UpdateAvailable, string LatestTag, string Message, string? HtmlUrl);

    public static async Task<UpdateResult> CheckAsync(string currentVersion)
    {
        try
        {
            using var resp = await Http.GetAsync("https://api.github.com/repos/1Thes1/ThesGamer/releases/latest");
            if (!resp.IsSuccessStatusCode)
                return new UpdateResult(false, false, "", Loc.T("update_fail"), null);

            await using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V') ?? "";
            var url = root.TryGetProperty("html_url", out var u) ? u.GetString() : null;

            var current = Normalize(currentVersion);
            var latest = Normalize(tag);
            var newer = Compare(latest, current) > 0;

            return newer
                ? new UpdateResult(true, true, tag, Loc.Tf("update_avail", tag), url)
                : new UpdateResult(true, false, tag, Loc.T("update_ok"), url);
        }
        catch
        {
            return new UpdateResult(false, false, "", Loc.T("update_fail"), null);
        }
    }

    private static Version Normalize(string v)
    {
        var clean = new string(v.Where(ch => char.IsDigit(ch) || ch == '.').ToArray());
        return Version.TryParse(clean, out var ver) ? ver : new Version(0, 0, 0);
    }

    private static int Compare(Version a, Version b) => a.CompareTo(b);
}
