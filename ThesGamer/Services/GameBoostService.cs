using System.Diagnostics;
using Microsoft.Win32;

namespace ThesGamer.Services;

public static class GameBoostService
{
    public static string ApplyBoost(string profile, bool createRestorePoint)
    {
        var notes = new List<string>();

        if (createRestorePoint)
        {
            var (ok, message) = RestorePointService.Create("Thes Gamer — before boost");
            notes.Add(ok ? "точка восстановления OK" : message);
            if (!ok && message.Contains("отменено", StringComparison.OrdinalIgnoreCase))
                return "Буст отменён: " + message;
        }

        try
        {
            if (profile.Equals("Battery", StringComparison.OrdinalIgnoreCase))
                SetPowerPlanBalanced();
            else
                SetPowerPlanHighPerformance();

            notes.Add(profile.Equals("Battery", StringComparison.OrdinalIgnoreCase)
                ? "план питания → сбалансированный"
                : "план питания → высокая производительность");
        }
        catch (Exception ex)
        {
            notes.Add("план питания: " + ex.Message);
        }

        try
        {
            EnableGameMode(true);
            notes.Add("Game Mode включён");
        }
        catch (Exception ex)
        {
            notes.Add("Game Mode: " + ex.Message);
        }

        notes.Add(profile.Equals("FPS", StringComparison.OrdinalIgnoreCase)
            ? "профиль FPS"
            : profile.Equals("Battery", StringComparison.OrdinalIgnoreCase)
                ? "профиль Battery"
                : "профиль Quality");

        return "Буст применён: " + string.Join("; ", notes);
    }

    public static string FocusGameProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return "Укажи имя процесса игры (без .exe).";

        var clean = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        var procs = Process.GetProcessesByName(clean);
        if (procs.Length == 0)
            return $"Процесс «{clean}» не найден. Запусти игру и попробуй снова.";

        foreach (var p in procs)
        {
            try
            {
                p.PriorityClass = ProcessPriorityClass.High;
            }
            catch (Exception ex)
            {
                return "Не удалось поднять приоритет: " + ex.Message;
            }
        }

        return $"Приоритет High выставлен для {procs.Length} процесс(ов) «{clean}».";
    }

    private static void EnableGameMode(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar");
        key?.SetValue("AutoGameModeEnabled", enabled ? 1 : 0, RegistryValueKind.DWord);

        using var key2 = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore");
        key2?.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
    }

    private static void SetPowerPlanHighPerformance() =>
        RunPowerCfg("/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

    private static void SetPowerPlanBalanced() =>
        RunPowerCfg("/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");

    private static void RunPowerCfg(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powercfg.exe",
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        using var p = Process.Start(psi);
        p?.WaitForExit(5000);
    }
}
