namespace ThesGamer.Services;

public static class Loc
{
    public static string Language { get; private set; } = "ru";

    private static readonly Dictionary<string, Dictionary<string, string>> Map = new()
    {
        ["ru"] = new()
        {
            ["brand"] = "by Thes · github.com/1Thes1",
            ["overview"] = "Обзор",
            ["memory"] = "Память",
            ["cleanup"] = "Чистка",
            ["drivers"] = "Драйверы",
            ["games"] = "Игры",
            ["refresh"] = "Обновить",
            ["safe_boost"] = "Safe Boost",
            ["load"] = "Нагрузка системы",
            ["scan_defender"] = "Скан Defender",
            ["free_ram"] = "Освободить RAM",
            ["disk_c"] = "Диск C:",
            ["top_proc"] = "Топ процессов",
            ["ram_clean_title"] = "Очистка RAM",
            ["ram_clean_sub"] = "Аккуратно подрезает working set. Standby — только от администратора.",
            ["auto_ram"] = "Автоочистка при высокой загрузке RAM",
            ["temp_title"] = "Чистка Temp",
            ["temp_sub"] = "Удаляет временные файлы. Заблокированные объекты пропускаются.",
            ["clean_temp"] = "Очистить Temp",
            ["disk_cleanup"] = "Очистка диска Windows",
            ["scan"] = "Сканировать",
            ["device_mgr"] = "Диспетчер устройств",
            ["boost_ready"] = "Boost ready",
            ["boost_feats"] = "Game Mode · план питания · приоритет процесса · точка восстановления",
            ["profile"] = "Профиль",
            ["game_proc"] = "Процесс игры",
            ["game_proc_hint"] = "Без .exe — BlackDesert64, cs2, dota2…",
            ["restore_point"] = "Точка восстановления перед бустом",
            ["apply"] = "Применить",
            ["priority"] = "Приоритет",
            ["protect"] = "Защита",
            ["ready_boost"] = "Готово к бусту.",
            ["quick_pick"] = "Быстрый выбор",
            ["temps"] = "Температуры",
            ["cpu_temp"] = "CPU",
            ["gpu_temp"] = "GPU",
            ["before_after"] = "До / после",
            ["check_updates"] = "Проверить обновления",
            ["lang"] = "Язык",
            ["updated"] = "Обновлено",
            ["ready"] = "Готово",
            ["update_ok"] = "У вас актуальная версия.",
            ["update_avail"] = "Доступна новая версия: {0}",
            ["update_fail"] = "Не удалось проверить обновления.",
            ["na"] = "н/д",
            ["before"] = "До",
            ["after"] = "После",
            ["freed"] = "Освобождено",
        },
        ["en"] = new()
        {
            ["brand"] = "by Thes · github.com/1Thes1",
            ["overview"] = "Overview",
            ["memory"] = "Memory",
            ["cleanup"] = "Cleanup",
            ["drivers"] = "Drivers",
            ["games"] = "Games",
            ["refresh"] = "Refresh",
            ["safe_boost"] = "Safe Boost",
            ["load"] = "System load",
            ["scan_defender"] = "Scan Defender",
            ["free_ram"] = "Free RAM",
            ["disk_c"] = "Disk C:",
            ["top_proc"] = "Top processes",
            ["ram_clean_title"] = "RAM cleanup",
            ["ram_clean_sub"] = "Gently trims process working sets. Standby list needs administrator rights.",
            ["auto_ram"] = "Auto-clean when RAM usage is high",
            ["temp_title"] = "Temp cleanup",
            ["temp_sub"] = "Removes temporary files. Locked files are skipped.",
            ["clean_temp"] = "Clean Temp",
            ["disk_cleanup"] = "Windows Disk Cleanup",
            ["scan"] = "Scan",
            ["device_mgr"] = "Device Manager",
            ["boost_ready"] = "Boost ready",
            ["boost_feats"] = "Game Mode · power plan · process priority · restore point",
            ["profile"] = "Profile",
            ["game_proc"] = "Game process",
            ["game_proc_hint"] = "Without .exe — BlackDesert64, cs2, dota2…",
            ["restore_point"] = "Create restore point before boost",
            ["apply"] = "Apply",
            ["priority"] = "Priority",
            ["protect"] = "Protection",
            ["ready_boost"] = "Ready to boost.",
            ["quick_pick"] = "Quick pick",
            ["temps"] = "Temperatures",
            ["cpu_temp"] = "CPU",
            ["gpu_temp"] = "GPU",
            ["before_after"] = "Before / after",
            ["check_updates"] = "Check for updates",
            ["lang"] = "Language",
            ["updated"] = "Updated",
            ["ready"] = "Ready",
            ["update_ok"] = "You are on the latest version.",
            ["update_avail"] = "New version available: {0}",
            ["update_fail"] = "Could not check for updates.",
            ["na"] = "n/a",
            ["before"] = "Before",
            ["after"] = "After",
            ["freed"] = "Freed",
        }
    };

    public static void SetLanguage(string lang)
    {
        Language = Map.ContainsKey(lang) ? lang : "ru";
    }

    public static string T(string key)
    {
        if (Map.TryGetValue(Language, out var dict) && dict.TryGetValue(key, out var value))
            return value;
        if (Map["en"].TryGetValue(key, out var en))
            return en;
        return key;
    }

    public static string Tf(string key, params object[] args) => string.Format(T(key), args);
}
