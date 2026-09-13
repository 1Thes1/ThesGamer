namespace ThesGamer.Services;

public sealed record GamePreset(
    string Name,
    string ProcessName,
    string Profile,
    string Hint);

public static class GamePresets
{
    public static IReadOnlyList<GamePreset> All { get; } =
    [
        new("Black Desert", "BlackDesert64", "FPS", "BDO — приоритет FPS + Game Mode"),
        new("CS2", "cs2", "FPS", "Counter-Strike 2"),
        new("Valorant", "VALORANT-Win64-Shipping", "FPS", "Valorant"),
        new("Dota 2", "dota2", "FPS", "Dota 2"),
        new("Fortnite", "FortniteClient-Win64-Shipping", "FPS", "Fortnite"),
        new("GTA V", "GTA5", "Quality", "GTA V / Rockstar"),
        new("Minecraft", "javaw", "Quality", "Java Minecraft"),
        new("Custom / Manual", "", "FPS", "Укажи процесс вручную")
    ];
}
