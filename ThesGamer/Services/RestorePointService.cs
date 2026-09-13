using System.Diagnostics;
using System.Text;

namespace ThesGamer.Services;

public static class RestorePointService
{
    public static (bool Ok, string Message) Create(string description)
    {
        var desc = string.IsNullOrWhiteSpace(description)
            ? "Thes Gamer"
            : description.Replace("'", "''");

        try
        {
            var command =
                "$ErrorActionPreference='Stop'; " +
                "Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; " +
                $"Checkpoint-Computer -Description '{desc}' -RestorePointType MODIFY_SETTINGS";

            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " + encoded,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(psi);
            if (process is null)
                return (false, "Не удалось запустить создание точки восстановления.");

            process.WaitForExit(90000);
            return process.ExitCode == 0
                ? (true, $"Точка восстановления создана: {description}")
                : (false, "Точка восстановления не создана (нужны права админа и защита системы на диске C:).");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("canceled", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("отмен", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Создание точки восстановления отменено в UAC.");
            }

            return (false, "Точка восстановления: " + ex.Message);
        }
    }

    public static string OpenSystemRestore()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "SystemPropertiesProtection.exe",
                UseShellExecute = true
            });
            return "Открыты свойства защиты системы.";
        }
        catch (Exception ex)
        {
            return "Не удалось открыть защиту системы: " + ex.Message;
        }
    }
}
