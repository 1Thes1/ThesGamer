using System.Diagnostics;
using System.Management;
using ThesGamer.Models;

namespace ThesGamer.Services;

public static class DriverService
{
    public static IReadOnlyList<DriverRow> ScanDrivers()
    {
        var rows = new List<DriverRow>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceName, DriverVersion, Manufacturer, DriverDate FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["DeviceName"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // Keep displayable subset: GPU / network / audio / chipset-ish
                if (!IsInteresting(name))
                    continue;

                rows.Add(new DriverRow
                {
                    DeviceName = name,
                    DriverVersion = obj["DriverVersion"]?.ToString() ?? "—",
                    Manufacturer = obj["Manufacturer"]?.ToString() ?? "—",
                    DriverDate = FormatDriverDate(obj["DriverDate"]?.ToString())
                });
            }
        }
        catch (Exception ex)
        {
            rows.Add(new DriverRow
            {
                DeviceName = "Ошибка сканирования",
                DriverVersion = ex.Message,
                Manufacturer = "—",
                DriverDate = "—"
            });
        }

        return rows
            .GroupBy(r => r.DeviceName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(r => r.DeviceName)
            .Take(40)
            .ToList();
    }

    public static string OpenWindowsUpdate()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:windowsupdate",
                UseShellExecute = true
            });
            return "Открыты параметры Windows Update (официальный источник драйверов).";
        }
        catch (Exception ex)
        {
            return $"Не удалось открыть Windows Update: {ex.Message}";
        }
    }

    public static string OpenDeviceManager()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "devmgmt.msc",
                UseShellExecute = true
            });
            return "Открыт Диспетчер устройств.";
        }
        catch (Exception ex)
        {
            return $"Не удалось открыть Диспетчер устройств: {ex.Message}";
        }
    }

    private static bool IsInteresting(string name)
    {
        string[] keys =
        [
            "NVIDIA", "AMD", "Radeon", "GeForce", "Intel", "Display", "Graphics",
            "Audio", "Realtek", "Network", "Ethernet", "Wi-Fi", "Wireless",
            "Bluetooth", "USB", "Chipset", "Storage", "NVMe", "SSD"
        ];
        return keys.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatDriverDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 8)
            return "—";
        // WMI format: yyyyMMddHHmmss.xxxxxx±UUU
        return $"{raw[..4]}-{raw.Substring(4, 2)}-{raw.Substring(6, 2)}";
    }
}
