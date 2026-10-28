using PPES.Events;
using PPES.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace PPES.Services;

public sealed record SeasonalWindow(int StartMonth, int StartDay, int EndMonth, int EndDay);

/// <summary>The stock calendar, captured before PPES changes it, so the page can tell what "auto" means today.</summary>
[Injectable(InjectionType.Singleton)]
public sealed class SeasonalCalendar
{
    private readonly Dictionary<string, List<SeasonalWindow>> _windows = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SeasonalWindow> WindowsOf(string id) => _windows.GetValueOrDefault(id) ?? [];

    public bool InSeason(string id, DateTime date) =>
        WindowsOf(id).Any(w => date.DateIsBetweenTwoDates(w.StartMonth, w.StartDay, w.EndMonth, w.EndDay));

    internal void Set(string id, List<SeasonalWindow> windows) => _windows[id] = windows;
}

/// <param name="Maps">Maps the group may spawn on; null means every map listed in the group.</param>
public sealed record SeasonalBossGroup(string Key, HashSet<string>? Maps);

/// <summary>
/// The event boss groups (seasonalevents.json eventBossSpawns) of the seasonal events active this run.
/// SPT adds them to the maps once at startup, but map mods like ABPS rebuild the spawn list and drop them,
/// so the raid patch adds them back to each raid copy.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class SeasonalBossRegistry
{
    public IReadOnlyList<SeasonalBossGroup> Groups { get; private set; } = [];

    /// <summary>Santa (gifter) from SPT's gifter settings; ABPS ships him at 0% on every map.</summary>
    public bool Santa { get; private set; }

    /// <summary>April Fools adds Santa without a setting; true when the user turned him off there.</summary>
    public bool RemoveSanta { get; private set; }

    /// <summary>SPT always makes bots dance around the map trees at Christmas; true when the Khorovod part is off.</summary>
    public bool StopDancingTree { get; private set; }

    /// <summary>Maps where only infected may spawn (SPT's zombie disableBosses), since ABPS puts the usual bosses back.</summary>
    public IReadOnlySet<string> InfectedOnlyMaps { get; private set; } = new HashSet<string>();

    /// <summary>April Fools turns the summon event on without a setting; EventApplier switches it back off after SPT.</summary>
    public bool DisableSummonEvent { get; private set; }

    internal void Set(List<SeasonalBossGroup> groups, bool santa, bool removeSanta, bool stopDancingTree, HashSet<string> infectedOnlyMaps, bool disableSummonEvent)
    {
        Groups = groups;
        Santa = santa;
        RemoveSanta = removeSanta;
        StopDancingTree = stopDancingTree;
        InfectedOnlyMaps = infectedOnlyMaps;
        DisableSummonEvent = disableSummonEvent;
    }
}

/// <summary>
/// SPT picks the active seasonal events once, in GameCallbacks, from the dates in seasonalevents.json.
/// Running just before it, PPES switches events off or moves a forced one onto today.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.GameCallbacks - 1)]
public sealed class SeasonalApplier(
    ConfigService config,
    SeasonalCalendar calendar,
    SeasonalBossRegistry bossRegistry,
    SeasonalEventConfig seasonalConfig,
    ISptLogger<SeasonalApplier> logger) : IOnLoad
{
    private const string NightCult = "halloweennightcult";
    private const string Summon = "halloweensummon";
    private const string Zombies = "halloweenzombies";

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var settings = config.Saved;
        var today = DateTimeOffset.Now.DateTime;
        var events = seasonalConfig.Events ?? [];
        var summary = new List<string>();
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in EventCatalog.Seasonal)
        {
            var entries = events.Where(e => def.SptNames.Contains(e.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            calendar.Set(def.Id, entries.Where(e => e.Enabled).Select(e => new SeasonalWindow(e.StartMonth, e.StartDay, e.EndMonth, e.EndDay)).ToList());
            if (entries.Count == 0)
            {
                logger.Warning($"{ConfigService.LogPrefix} seasonalevents.json has no entry for {def.NameEn}, skipped");
                continue;
            }

            var mode = settings.ModeOf(def.Id);
            switch (mode)
            {
                case SeasonalMode.Off:
                    foreach (var e in entries) e.Enabled = false;
                    break;
                case SeasonalMode.On when !calendar.InSeason(def.Id, today):
                    var forced = entries[0];
                    forced.Enabled = true;
                    forced.StartMonth = forced.EndMonth = today.Month;
                    forced.StartDay = forced.EndDay = today.Day;
                    break;
            }

            var isActive = mode == SeasonalMode.On || (mode == SeasonalMode.Auto && calendar.InSeason(def.Id, today));
            if (isActive) active.Add(def.Id);
            summary.Add($"{def.NameEn} {(isActive ? "on" : "off")} ({mode})");
        }

        var halloween = settings.Halloween;
        var christmas = settings.Christmas;
        var aprilFools = settings.AprilFools;
        var halloweenOn = active.Contains("halloween");
        var aprilOn = active.Contains("aprilfools");
        var winterOn = active.Contains("christmas") || active.Contains("newyears");

        ApplyHalloweenParts(events, halloween);
        ApplyChristmasParts(events, christmas);
        ApplyAprilFoolsParts(events, aprilFools);

        // April Fools always adds these groups, so they are emptied when no active event wants them
        var summonWanted = (halloweenOn && halloween.Ritual) || (aprilOn && aprilFools.RitualAndCult);
        var cultWanted = (halloweenOn && halloween.Cultists) || (aprilOn && aprilFools.RitualAndCult);
        if (!summonWanted) EmptyGroup(Summon);
        if (!cultWanted) EmptyGroup(NightCult);

        var santa = SantaFromSettings(events, active) || (aprilOn && aprilFools.Santa);
        bossRegistry.Set(
            BossGroups(events, halloweenOn, halloween, summonWanted, cultWanted),
            santa,
            aprilOn && !santa,
            winterOn && !christmas.Khorovod,
            halloweenOn && halloween.Zombies && halloween.LabInfectedOnly ? ZombieOnlyMaps(events) : [],
            aprilOn && !summonWanted);

        if (halloweenOn)
            summary.Add($"Halloween parts: ritual {OnOff(halloween.Ritual)}, zombies {OnOff(halloween.Zombies)}, cultists {OnOff(halloween.Cultists)}, Lab infected only {OnOff(halloween.LabInfectedOnly)}, free Lab {OnOff(halloween.FreeLab)}");
        if (winterOn)
            summary.Add($"Christmas parts: Santa {OnOff(christmas.Santa)}, hideout {OnOff(christmas.Hideout)}, Khorovod {OnOff(christmas.Khorovod)}, winter clothes {OnOff(christmas.WinterClothes)}");
        if (aprilOn)
            summary.Add($"April Fools parts: winter {OnOff(aprilFools.Winter)}, Santa {OnOff(aprilFools.Santa)}, ritual and cult {OnOff(aprilFools.RitualAndCult)}");

        if (!seasonalConfig.EnableSeasonalEventDetection)
            logger.Info($"{ConfigService.LogPrefix} Seasonal event detection was off in SPT config, turned on so the selector can work");
        seasonalConfig.EnableSeasonalEventDetection = true;

        logger.Info($"{ConfigService.LogPrefix} Seasonal: {string.Join(", ", summary)}");
        return Task.CompletedTask;
    }

    private static string OnOff(bool on) => on ? "on" : "off";

    private void EmptyGroup(string key)
    {
        // an empty group keeps SPT from warning about a missing key
        if (seasonalConfig.EventBossSpawns.ContainsKey(key)) seasonalConfig.EventBossSpawns[key] = new();
    }

    private static IEnumerable<SeasonalEvent> OfType(List<SeasonalEvent> events, params SeasonalEventType[] types) =>
        events.Where(e => types.Contains(e.Type) && e.Settings is not null);

    private static void ApplyHalloweenParts(List<SeasonalEvent> events, HalloweenParts parts)
    {
        foreach (var e in OfType(events, SeasonalEventType.Halloween))
        {
            var s = e.Settings!;
            s.EnableSummoning = parts.Ritual;
            if (!parts.FreeLab) s.RemoveEntryRequirement = [];
            if (s.ZombieSettings is not { } zombies) continue;
            zombies.Enabled = parts.Zombies;
            // without ABPS this stops SPT from clearing the Lab; with ABPS the raid patch does the clearing when asked
            if (!parts.LabInfectedOnly) zombies.DisableBosses = [];
        }
    }

    private static void ApplyChristmasParts(List<SeasonalEvent> events, ChristmasParts parts)
    {
        foreach (var e in OfType(events, SeasonalEventType.Christmas, SeasonalEventType.NewYears))
        {
            var s = e.Settings!;
            s.EnableSanta = parts.Santa;
            s.EnableChristmasHideout = parts.Hideout;
            s.AdjustBotAppearances = parts.WinterClothes;
            // only events that had the tree event keep it; turning it on must not add it to the January entry
            if (s.EnableRundansEvent is not null) s.EnableRundansEvent = parts.Khorovod;
            if (s.EnableKhorvodEvent is not null) s.EnableKhorvodEvent = parts.Khorovod;
        }
    }

    private static void ApplyAprilFoolsParts(List<SeasonalEvent> events, AprilFoolsParts parts)
    {
        foreach (var e in OfType(events, SeasonalEventType.AprilFools))
        {
            var s = e.Settings!;
            if (!parts.Winter) s.ForceSeason = null;
            if (!parts.RitualAndCult) s.ReplaceBotHostility = false;
        }
    }

    /// <summary>Mirrors which boss groups SPT's SeasonalEventService adds for the active events.</summary>
    private static List<SeasonalBossGroup> BossGroups(List<SeasonalEvent> events, bool halloweenOn, HalloweenParts halloween, bool summonWanted, bool cultWanted)
    {
        var groups = new List<SeasonalBossGroup>();
        if (summonWanted) groups.Add(new(Summon, null));
        if (cultWanted) groups.Add(new(NightCult, null));
        if (halloweenOn && halloween.Zombies && ZombieMaps(events, SeasonalEventType.Halloween) is { } maps) groups.Add(new(Zombies, maps));
        return groups;
    }

    /// <summary>Christmas and New Year bring Santa when their settings say so.</summary>
    private static bool SantaFromSettings(List<SeasonalEvent> events, HashSet<string> active) =>
        EventCatalog.Seasonal
            .Where(def => def.Id is "christmas" or "newyears" && active.Contains(def.Id))
            .Any(def => events.Any(e => def.SptNames.Contains(e.Name, StringComparer.OrdinalIgnoreCase) && e.Settings?.EnableSanta == true));

    private static HashSet<string> ZombieOnlyMaps(List<SeasonalEvent> events) =>
        OfType(events, SeasonalEventType.Halloween)
            .SelectMany(e => e.Settings!.ZombieSettings?.DisableBosses ?? [])
            .Select(m => m.ToLowerInvariant())
            .ToHashSet();

    /// <summary>Same map mapping as SPT: infection above zero, factory4 and Sandbox cover both of their variants.</summary>
    private static HashSet<string>? ZombieMaps(List<SeasonalEvent> events, SeasonalEventType type)
    {
        var zombies = events.FirstOrDefault(e => e.Type == type && e.Settings?.ZombieSettings?.Enabled == true)?.Settings?.ZombieSettings;
        if (zombies?.MapInfectionAmount is null) return null;
        var maps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (map, amount) in zombies.MapInfectionAmount)
        {
            if (amount <= 0) continue;
            var id = map.ToLowerInvariant();
            switch (id)
            {
                case "factory4": maps.Add("factory4_day"); maps.Add("factory4_night"); break;
                case "sandbox": maps.Add("sandbox"); maps.Add("sandbox_high"); break;
                default: maps.Add(id); break;
            }
        }
        return maps;
    }
}
