namespace PPES.Models;

public static class SeasonalMode
{
    public const string Auto = "auto";
    public const string On = "on";
    public const string Off = "off";

    public static string Normalize(string? mode) => mode?.ToLowerInvariant() switch
    {
        On => On,
        Off => Off,
        _ => Auto,
    };
}

public static class RotationMode
{
    public const string Off = "off";
    public const string Weekly = "weekly";
    public const string Weekend = "weekend";

    public static string Normalize(string? mode) => mode?.ToLowerInvariant() switch
    {
        Weekly => Weekly,
        Weekend => Weekend,
        _ => Off,
    };
}

public sealed class RotationSettings
{
    public string Mode { get; set; } = RotationMode.Off;
    public int Count { get; set; } = 1;

    public RotationSettings Clone() => new() { Mode = Mode, Count = Count };
}

public sealed class PresetData
{
    public Dictionary<string, string> Seasonal { get; set; } = new();
    public List<string> Events { get; set; } = [];
}

public sealed class PpesConfig
{
    public string Language { get; set; } = "en";
    public string View { get; set; } = "grid";
    public bool GroupByYear { get; set; } = true;
    public bool TraderMessage { get; set; } = true;
    public Dictionary<string, string> Seasonal { get; set; } = new();
    public List<string> Events { get; set; } = [];
    public RotationSettings Rotation { get; set; } = new();
    public Dictionary<string, PresetData> Presets { get; set; } = new();

    public string ModeOf(string seasonalId) => SeasonalMode.Normalize(Seasonal.GetValueOrDefault(seasonalId));
    public bool HasEvent(string id) => Events.Contains(id, StringComparer.OrdinalIgnoreCase);

    public PpesConfig Clone() => new()
    {
        Language = Language,
        View = View,
        GroupByYear = GroupByYear,
        TraderMessage = TraderMessage,
        Seasonal = new Dictionary<string, string>(Seasonal, StringComparer.OrdinalIgnoreCase),
        Events = [.. Events],
        Rotation = Rotation.Clone(),
        Presets = Presets.ToDictionary(p => p.Key, p => new PresetData
        {
            Seasonal = new Dictionary<string, string>(p.Value.Seasonal, StringComparer.OrdinalIgnoreCase),
            Events = [.. p.Value.Events],
        }),
    };
}
