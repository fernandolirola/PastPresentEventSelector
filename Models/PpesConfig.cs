using System.Text.Json;
using System.Text.Json.Serialization;
using PPES.Events;

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
    [Applies(Applies.NextRaid)]
    public string Mode { get; set; } = RotationMode.Off;
    /// <summary>Events per draw; a draw takes fewer when the pool is smaller.</summary>
    [Applies(Applies.NextRaid), Limits(1, 100, Decimals = 0)]
    public int Count { get; set; } = 1;
    /// <summary>Raids per draw in the "raids" mode.</summary>
    [Applies(Applies.NextRaid), Limits(1, 50, Decimals = 0)]
    public int Raids { get; set; } = 3;
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
    /// <summary>Chance that a period has no seasonal event at all.</summary>
    [Limits(0, 90, Decimals = 0)]
    public int BreakChance { get; set; } = 25;
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

    [JsonIgnore]
    public bool ChangesNothing => !Remove && Chance is null && Zones is null && Escort is null;
}

public sealed class PresetData
{
    public Dictionary<string, string> Seasonal { get; set; } = new();
    public List<string> Events { get; set; } = [];
}

/// <summary>Independent Halloween settings. Normal AI replaces forced hiding by default.</summary>
public sealed class HalloweenParts
{
    /// <summary>Halloween 2023: roaming Zryachiy, the priests' ritual and Vengeful Zryachiy.</summary>
    public bool Ritual { get; set; } = true;
    /// <summary>Halloween 2024: infected and map infection. SPT ships it off.</summary>
    public bool Zombies { get; set; }
    /// <summary>Keep the game's Halloween hiding layer instead of normal patrol and combat AI.</summary>
    [Applies(Applies.Live)]
    public bool BotsHideFromZombies { get; set; }
    /// <summary>Adds infected reinforcement waves after the stock finite schedule.</summary>
    [Applies(Applies.NextRaid)]
    public bool ZombieWaves { get; set; }
    [Applies(Applies.NextRaid), Limits(1, 20, Decimals = 0)]
    public int ZombieWaveSizeMin { get; set; } = 3;
    [Applies(Applies.NextRaid), Limits(1, 20, Decimals = 0, AtLeast = nameof(ZombieWaveSizeMin))]
    public int ZombieWaveSizeMax { get; set; } = 3;
    [Applies(Applies.NextRaid), Limits(1, 30, Decimals = 0)]
    public int ZombieWaveIntervalMin { get; set; } = 5;
    [Applies(Applies.NextRaid), Limits(1, 30, Decimals = 0, AtLeast = nameof(ZombieWaveIntervalMin))]
    public int ZombieWaveIntervalMax { get; set; } = 5;
    /// <summary>Halloween 2025: Oni, Harbinger and Ghost cultists.</summary>
    public bool Cultists { get; set; } = true;
    /// <summary>With zombies on, SPT clears the Lab's own bosses and raiders so only infected are left.</summary>
    public bool LabInfectedOnly { get; set; } = true;
    public bool FreeLab { get; set; } = true;
    /// <summary>Halloween 2026: infected on every map, Infected Tagilla, no Scav karma loss and the 2026 quests. Brings the zombies with it.</summary>
    public bool Outbreak2026 { get; set; }
    /// <summary>Pumpkins with sweets in containers, signal pistols and acid green flares on Scavs.</summary>
    public bool Loot { get; set; } = true;

    [JsonIgnore]
    public bool ZombiesOn => Zombies || Outbreak2026;
}

/// <summary>Pieces of SPT's April Fools that can be switched on their own. All on in SPT's stock config.</summary>
public sealed class AprilFoolsParts
{
    public bool Winter { get; set; } = true;
    public bool Santa { get; set; } = true;
    public bool RitualAndCult { get; set; } = true;
}

/// <summary>Pieces of SPT's Christmas and New Year that can be switched on their own. All on in SPT's stock config.</summary>
public sealed class ChristmasParts
{
    public bool Santa { get; set; } = true;
    public bool Hideout { get; set; } = true;
    /// <summary>Christmas 2024: the tree event and the dancing bots around it.</summary>
    public bool Khorovod { get; set; } = true;
    public bool WinterClothes { get; set; } = true;
}

/// <summary>Event mechanics with absolute values; the defaults are what the game does.</summary>
public sealed class MechanicsSettings
{
    /// <summary>Seconds Kollontay's hits jam the weapon (Streets boss, any raid); 0 turns it off.</summary>
    [Applies(Applies.Live), Limits(0, 600, Decimals = 1)]
    public double KollontayJam { get; set; } = 5;
    /// <summary>Seconds Vengeful Zryachiy's jam lasts, 0 turns it off and -1 keeps the game's, which never ends.</summary>
    [Applies(Applies.Live), Limits(0, 3600, Decimals = 1, Off = -1)]
    public double VengefulJam { get; set; } = -1;
    /// <summary>Halloween 2023 ritual: how many times longer extracts take.</summary>
    [Limits(0.1, 20)]
    public double RitualExitTime { get; set; } = 3;
    /// <summary>Halloween 2023 ritual: how many times faster stamina drains.</summary>
    [Limits(0.1, 20)]
    public double RitualStamina { get; set; } = 3;
    /// <summary>Zombie infection: how fast hydration drains, written to the game as a negative value.</summary>
    [Limits(0, 100)]
    public double ToxinDehydration { get; set; } = 0.84;
    /// <summary>Zombie infection: hearing lost, in percent.</summary>
    [Limits(0, 100, Decimals = 0)]
    public double ToxinHearing { get; set; } = 20;
}

public sealed class PpesConfig
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true, PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string Language { get; set; } = "en";
    public string View { get; set; } = "grid";
    public bool GroupByYear { get; set; } = true;
    public bool TraderMessage { get; set; } = true;
    public Dictionary<string, string> Seasonal { get; set; } = new();
    public HalloweenParts Halloween { get; set; } = new();
    public ChristmasParts Christmas { get; set; } = new();
    public AprilFoolsParts AprilFools { get; set; } = new();
    public MechanicsSettings Mechanics { get; set; } = new();
    public List<string> Events { get; set; } = [];
    public RotationSettings Rotation { get; set; } = new();
    public SeasonalRotationSettings SeasonalRotation { get; set; } = new();
    /// <summary>Raid preview edits per map id.</summary>
    public Dictionary<string, List<SpawnEdit>> Spawns { get; set; } = new();
    public Dictionary<string, PresetData> Presets { get; set; } = new();

    public string ModeOf(string seasonalId) => SeasonalMode.Normalize(Seasonal.GetValueOrDefault(seasonalId));
    public bool HasEvent(string id) => Events.Contains(id, StringComparer.OrdinalIgnoreCase);

    public List<SpawnEdit> SpawnsOf(string mapId) => Spawns.GetValueOrDefault(mapId) ?? [];

    /// <summary>Page preferences are saved as soon as they change and are not part of the editable selection.</summary>
    public void CopyPreferencesFrom(PpesConfig other)
    {
        Language = other.Language;
        View = other.View;
        GroupByYear = other.GroupByYear;
        Presets = other.Clone().Presets;
    }

    /// <summary>The editable selection as text: preferences left out, seasonal modes and spawn edits in a fixed order.</summary>
    public string SelectionKey()
    {
        var selection = Clone();
        selection.CopyPreferencesFrom(new PpesConfig());
        selection.Seasonal = EventCatalog.Seasonal.ToDictionary(s => s.Id, s => ModeOf(s.Id));
        selection.Spawns = Spawns.Where(p => p.Value.Count > 0)
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(p => p.Key.ToLowerInvariant(), p => p.Value);
        return JsonSerializer.Serialize(selection, JsonOptions);
    }

    public bool SameSelectionAs(PpesConfig other) => SelectionKey() == other.SelectionKey();

    public PpesConfig Clone()
    {
        var copy = JsonSerializer.Deserialize<PpesConfig>(JsonSerializer.Serialize(this, JsonOptions), JsonOptions)!;
        copy.Seasonal = new Dictionary<string, string>(copy.Seasonal, StringComparer.OrdinalIgnoreCase);
        copy.Spawns = new Dictionary<string, List<SpawnEdit>>(copy.Spawns, StringComparer.OrdinalIgnoreCase);
        foreach (var preset in copy.Presets.Values)
            preset.Seasonal = new Dictionary<string, string>(preset.Seasonal, StringComparer.OrdinalIgnoreCase);
        return copy;
    }
}
