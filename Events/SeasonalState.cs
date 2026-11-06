using PPES.Models;
using PPES.Services;

namespace PPES.Events;

/// <param name="Maps">Maps the group spawns on; null means every map listed in the group.</param>
public sealed record SeasonalBossGroup(string Key, IReadOnlySet<string>? Maps);

/// <summary>
/// The seasonal events on for a selection and a date, with what their parts add up to. SPT is set up from the state
/// the server started with; the page's preview builds one for the unsaved selection.
/// </summary>
public sealed class SeasonalState
{
    public const string SummonGroup = "halloweensummon";
    public const string CultGroup = "halloweennightcult";
    public const string ZombieGroup = "halloweenzombies";
    /// <summary>Halloween 2026 infection on maps SPT leaves at zero (Customs and Interchange).</summary>
    public const double OutbreakInfection = 40;

    private readonly SeasonalCalendar _stock;

    private SeasonalState(IReadOnlySet<string> active, PpesConfig parts, SeasonalCalendar stock)
    {
        Active = active;
        Halloween = parts.Halloween;
        Christmas = parts.Christmas;
        AprilFools = parts.AprilFools;
        _stock = stock;
    }

    public static SeasonalState For(PpesConfig config, SeasonalCalendar calendar, DateTime date) =>
        new(EventCatalog.Seasonal.Where(s => SeasonalSchedule.Active(config, calendar, s.Id, date)).Select(s => s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase),
            config, calendar);

    /// <summary>What SPT was set up with at startup.</summary>
    public static SeasonalState Started(ConfigService config, SeasonalCalendar calendar) => For(config.Applied, calendar, config.StartedAt.Date);

    public IReadOnlySet<string> Active { get; }
    public HalloweenParts Halloween { get; }
    public ChristmasParts Christmas { get; }
    public AprilFoolsParts AprilFools { get; }

    public bool HalloweenOn => Active.Contains(SeasonalIds.Halloween);
    public bool AprilFoolsOn => Active.Contains(SeasonalIds.AprilFools);
    public bool WinterOn => Active.Contains(SeasonalIds.Christmas) || Active.Contains(SeasonalIds.NewYears);

    /// <summary>The Zryachiy ritual group; April Fools brings it too.</summary>
    public bool Ritual => (HalloweenOn && Halloween.Ritual) || (AprilFoolsOn && AprilFools.RitualAndCult);
    public bool Cult => (HalloweenOn && Halloween.Cultists) || (AprilFoolsOn && AprilFools.RitualAndCult);
    public bool ZombiesOn => HalloweenOn && Halloween.ZombiesOn;
    public bool Santa => (WinterOn && Christmas.Santa) || (AprilFoolsOn && AprilFools.Santa);
    /// <summary>April Fools adds Santa without a setting; true when the user turned him off there.</summary>
    public bool RemoveSanta => AprilFoolsOn && !Santa;
    /// <summary>SPT always makes bots dance around the map trees at Christmas.</summary>
    public bool StopDancingTree => WinterOn && !Christmas.Khorovod;
    /// <summary>April Fools turns the summon event on without a setting.</summary>
    public bool DisableSummonEvent => AprilFoolsOn && !Ritual;

    public IReadOnlySet<string> InfectedOnlyMaps => ZombiesOn && Halloween.LabInfectedOnly ? _stock.InfectedOnlyMaps : new HashSet<string>();

    public IReadOnlySet<string> FreeEntryMaps => HalloweenOn && Halloween.FreeLab ? _stock.HalloweenFreeEntry : new HashSet<string>();

    /// <summary>Infection per seasonal map key, as SPT gets it.</summary>
    public IReadOnlyDictionary<string, double> InfectionByKey =>
        _stock.Infection.ToDictionary(p => p.Key, p => Halloween.Outbreak2026 && p.Value <= 0 ? OutbreakInfection : p.Value);

    /// <summary>Infection on a map in percent, or null when zombies are off or the map has no setting.</summary>
    public double? Infection(string mapId) =>
        !ZombiesOn ? null
        : InfectionByKey.Where(p => GameMaps.FromSeasonalKey(p.Key).Contains(mapId, StringComparer.OrdinalIgnoreCase))
            .Select(p => (double?)p.Value).FirstOrDefault();

    /// <summary>The bosses of an event boss group on a map, as SPT ships them.</summary>
    public IReadOnlyList<SPTarkov.Server.Core.Models.Eft.Common.BossLocationSpawn> GroupOn(string key, string mapId) =>
        _stock.BossGroups.TryGetValue(key, out var byMap) && byMap.TryGetValue(mapId, out var bosses) ? bosses : [];

    /// <summary>The event boss groups SPT's SeasonalEventService adds for this state.</summary>
    public IReadOnlyList<SeasonalBossGroup> BossGroups
    {
        get
        {
            var groups = new List<SeasonalBossGroup>();
            if (Ritual) groups.Add(new(SummonGroup, null));
            if (Cult) groups.Add(new(CultGroup, null));
            if (ZombiesOn && InfectionByKey.Count > 0)
                groups.Add(new(ZombieGroup, InfectionByKey.Where(p => p.Value > 0).SelectMany(p => GameMaps.FromSeasonalKey(p.Key))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)));
            return groups;
        }
    }
}
