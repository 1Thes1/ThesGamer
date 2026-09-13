using System.IO;
using System.Text.Json;

namespace ThesGamer.Services;

public sealed class AppSettings
{
    public bool AutoRamEnabled { get; set; }
    public int AutoRamThresholdPercent { get; set; } = 85;
    public int AutoRamCooldownSeconds { get; set; } = 120;
    public bool CreateRestorePointBeforeBoost { get; set; } = true;
    public string LastGameProcess { get; set; } = "BlackDesert64";
    public string LastGameProfile { get; set; } = "FPS";
    public string LastPresetName { get; set; } = "Black Desert";
    public string Language { get; set; } = "ru";
    public bool AutoUpdateEnabled { get; set; } = true;
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string DirPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ThesGamer");

    private static string FilePath => Path.Combine(DirPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DirPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
