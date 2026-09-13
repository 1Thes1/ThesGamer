using System.Diagnostics;
using System.Management;

namespace ThesGamer.Services;

public static class DefenderService
{
    public sealed record Status(string Realtime, string Antivirus, string LastUpdate);

    public static Status GetStatus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\Microsoft\Windows\Defender",
                "SELECT * FROM MSFT_MpComputerStatus");
            foreach (ManagementObject obj in searcher.Get())
            {
                var realtime = obj["RealTimeProtectionEnabled"]?.ToString() ?? "unknown";
                var antivirus = obj["AntivirusEnabled"]?.ToString() ?? "unknown";
                var updated = obj["AntivirusSignatureLastUpdated"]?.ToString() ?? "n/a";
                return new Status(realtime, antivirus, updated);
            }
        }
        catch
        {
            // fallback below
        }

        return ProbeViaPowerShell();
    }

    public static string StartQuickScan()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"Start-MpScan -ScanType QuickScan\"",
                UseShellExecute = true,
                Verb = "runas"
            });
            return "Запрошен быстрый скан Windows Defender (может запросить права админа).";
        }
        catch (Exception ex)
        {
            return $"Не удалось запустить скан: {ex.Message}";
        }
    }

    private static Status ProbeViaPowerShell()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"try { $p=Get-MpComputerStatus; Write-Output $p.RealTimeProtectionEnabled; Write-Output $p.AntivirusEnabled; Write-Output $p.AntivirusSignatureLastUpdated } catch { Write-Output unknown; Write-Output unknown; Write-Output n/a }\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            var output = p?.StandardOutput.ReadToEnd() ?? "";
            p?.WaitForExit(8000);
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return new Status(
                lines.ElementAtOrDefault(0) ?? "unknown",
                lines.ElementAtOrDefault(1) ?? "unknown",
                lines.ElementAtOrDefault(2) ?? "n/a");
        }
        catch
        {
            return new Status("unknown", "unknown", "n/a");
        }
    }
}
