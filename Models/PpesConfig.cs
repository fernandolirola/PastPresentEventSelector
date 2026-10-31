using System.Text.Json.Serialization;

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
    public const string Daily = "daily";
    public const string Hourly = "hourly";
    public const string Raids = "raids";

    public static string Normalize(string? mode) => mode?.ToLowerInvariant() switch
    {
        Weekly => Weekly,
        Weekend => Weekend,
        Daily => Daily,
        Hourly => Hourly,
        Raids => Raids,
        _ => Off,
    };
}

public sealed class RotationSettings
{
    public string Mode { get; set; } = RotationMode.Off;
    public int Count { get; set; } = 1;
    /// <summary>Raids per draw in the "raids" mode.</summary>
    public int Raids { get; set; } = 3;

    public RotationSettings Clone() => new() { Mode = Mode, Count = Count, Raids = Raids };
}

public static class SeasonalRotationMode
{
    public const string Off = "off";
    public const string Daily = "daily";
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";

    public static string Normalize(string? mode) => mode?.ToLowerInvariant() switch
    {
        Daily => Daily,
        Weekly => Weekly,
        Monthly => Monthly,
        _ => Off,
    };
}

/// <summary>One random seasonal event per period instead of the calendar. Events set to Off stay out of the draw.</summary>
public sealed class SeasonalRotationSettings
{
    public string Mode { get; set; } = SeasonalRotationMode.Off;
    /// <summary>Chance (0 to 90) that a period has no seasonal event at all.</summary>
    public int BreakChance { get; set; } = 25;

    public SeasonalRotationSettings Clone() => new() { Mode = Mode, BreakChance = BreakChance };
    public bool SameAs(SeasonalRotationSettings other) => Mode == other.Mode && BreakChance == other.BreakChance;
}

/// <summary>
/// A user change to one role on one map, applied after the events. Null fields keep what the map has; a role the map
/// does not have is added when a chance is given.
/// </summary>
public sealed class SpawnEdit
{
    public string Role { get; set; } = "";
    public bool Remove { get; set; }
    public double? Chance { get; set; }
    public string? Zones { get; set; }
    /// <summary>Escort size as "2" or "1-3".</summary>
    public string? Escort { get; set; }

    public SpawnEdit Clone() => new() { Role = Role, Remove = Remove, Chance = Chance, Zones = Zones, Escort = Escort };
    public bool SameAs(SpawnEdit o) => Role == o.Role && Remove == o.Remove && Chance == o.Chance && Zones == o.Zones && Escort == o.Escort;
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
    /// <summary>Halloween 2026: infected on every map, Infected Tagilla, no Scav karma loss and the 2026 quests. Brings the zombies with it.</summary>
    public bool Outbreak2026 { get; set; }
    /// <summary>Pumpkins with sweets in containers, signal pistols and acid green flares on Scavs.</summary>
    public bool Loot { get; set; } = true;
    /// <summary>How long Kollontay's and Vengeful Zryachiy's weapon jams last, in percent of stock (0 turns them off). Applied by the client plugin.</summary>
    public int MisfirePercent { get; set; } = 100;

    public HalloweenParts Clone() => new() { Ritual = Ritual, Zombies = Zombies, Cultists = Cultists, LabInfectedOnly = LabInfectedOnly, FreeLab = FreeLab, Outbreak2026 = Outbreak2026, Loot = Loot, MisfirePercent = MisfirePercent };

    public bool SameAs(HalloweenParts other) =>
        Ritual == other.Ritual && Zombies == other.Zombies && Cultists == other.Cultists && LabInfectedOnly == other.LabInfectedOnly && FreeLab == other.FreeLab
        && Outbreak2026 == other.Outbreak2026 && Loot == other.Loot && MisfirePercent == other.MisfirePercent;

    /// <summary>The 2026 outbreak needs the zombies, so it switches them on.</summary>
    [JsonIgnore]
    public bool ZombiesOn => Zombies || Outbreak2026;
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
    public SeasonalRotationSettings SeasonalRotation { get; set; } = new();
    /// <summary>Raid preview edits per map id.</summary>
    public Dictionary<string, List<SpawnEdit>> Spawns { get; set; } = new();
    public Dictionary<string, PresetData> Presets { get; set; } = new();

    public string ModeOf(string seasonalId) => SeasonalMode.Normalize(Seasonal.GetValueOrDefault(seasonalId));
    public bool HasEvent(string id) => Events.Contains(id, StringComparer.OrdinalIgnoreCase);

    public List<SpawnEdit> SpawnsOf(string mapId) => Spawns.GetValueOrDefault(mapId) ?? [];

    public bool SameSpawns(PpesConfig other) =>
        Spawns.Keys.Concat(other.Spawns.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
            .All(map => SpawnsOf(map) is var a && other.SpawnsOf(map) is var b && a.Count == b.Count && a.Zip(b).All(x => x.First.SameAs(x.Second)));

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
        SeasonalRotation = SeasonalRotation.Clone(),
        Spawns = Spawns.ToDictionary(p => p.Key, p => p.Value.Select(e => e.Clone()).ToList(), StringComparer.OrdinalIgnoreCase),
        Presets = Presets.ToDictionary(p => p.Key, p => new PresetData
        {
            Seasonal = new Dictionary<string, string>(p.Value.Seasonal, StringComparer.OrdinalIgnoreCase),
            Events = [.. p.Value.Events],
        }),
    };
}
