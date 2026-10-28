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

/// <summary>Pieces of SPT's Halloween that can be switched on their own. Defaults follow SPT's stock config.</summary>
public sealed class HalloweenParts
{
    /// <summary>Halloween 2023: roaming Zryachiy, the priests' ritual and Vengeful Zryachiy.</summary>
    public bool Ritual { get; set; } = true;
    /// <summary>Halloween 2024: infected and map infection. SPT ships it off.</summary>
    public bool Zombies { get; set; }
    /// <summary>Halloween 2025: Oni, Harbinger and Ghost cultists.</summary>
    public bool Cultists { get; set; } = true;
    /// <summary>With zombies on, SPT clears the Lab's own bosses and raiders so only infected are left.</summary>
    public bool LabInfectedOnly { get; set; } = true;
    public bool FreeLab { get; set; } = true;

    public HalloweenParts Clone() => new() { Ritual = Ritual, Zombies = Zombies, Cultists = Cultists, LabInfectedOnly = LabInfectedOnly, FreeLab = FreeLab };

    public bool SameAs(HalloweenParts other) =>
        Ritual == other.Ritual && Zombies == other.Zombies && Cultists == other.Cultists && LabInfectedOnly == other.LabInfectedOnly && FreeLab == other.FreeLab;
}

/// <summary>Pieces of SPT's April Fools that can be switched on their own. All on in SPT's stock config.</summary>
public sealed class AprilFoolsParts
{
    public bool Winter { get; set; } = true;
    public bool Santa { get; set; } = true;
    public bool RitualAndCult { get; set; } = true;

    public AprilFoolsParts Clone() => new() { Winter = Winter, Santa = Santa, RitualAndCult = RitualAndCult };

    public bool SameAs(AprilFoolsParts other) =>
        Winter == other.Winter && Santa == other.Santa && RitualAndCult == other.RitualAndCult;
}

/// <summary>Pieces of SPT's Christmas and New Year that can be switched on their own. All on in SPT's stock config.</summary>
public sealed class ChristmasParts
{
    public bool Santa { get; set; } = true;
    public bool Hideout { get; set; } = true;
    /// <summary>Christmas 2024: the tree event and the dancing bots around it.</summary>
    public bool Khorovod { get; set; } = true;
    public bool WinterClothes { get; set; } = true;

    public ChristmasParts Clone() => new() { Santa = Santa, Hideout = Hideout, Khorovod = Khorovod, WinterClothes = WinterClothes };

    public bool SameAs(ChristmasParts other) =>
        Santa == other.Santa && Hideout == other.Hideout && Khorovod == other.Khorovod && WinterClothes == other.WinterClothes;
}

public sealed class PpesConfig
{
    public string Language { get; set; } = "en";
    public string View { get; set; } = "grid";
    public bool GroupByYear { get; set; } = true;
    public bool TraderMessage { get; set; } = true;
    public Dictionary<string, string> Seasonal { get; set; } = new();
    public HalloweenParts Halloween { get; set; } = new();
    public ChristmasParts Christmas { get; set; } = new();
    public AprilFoolsParts AprilFools { get; set; } = new();
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
        Halloween = Halloween.Clone(),
        Christmas = Christmas.Clone(),
        AprilFools = AprilFools.Clone(),
        Events = [.. Events],
        Rotation = Rotation.Clone(),
        Presets = Presets.ToDictionary(p => p.Key, p => new PresetData
        {
            Seasonal = new Dictionary<string, string>(p.Value.Seasonal, StringComparer.OrdinalIgnoreCase),
            Events = [.. p.Value.Events],
        }),
    };
}
