using PPES.Events;
using PPES.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

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

    public bool ZombiesActive { get; private set; }

    internal void Set(List<SeasonalBossGroup> groups, bool santa, bool removeSanta, bool stopDancingTree, HashSet<string> infectedOnlyMaps, bool disableSummonEvent, bool zombiesActive)
    {
        Groups = groups;
        Santa = santa;
        RemoveSanta = removeSanta;
        StopDancingTree = stopDancingTree;
        InfectedOnlyMaps = infectedOnlyMaps;
        DisableSummonEvent = disableSummonEvent;
        ZombiesActive = zombiesActive;
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
    BotTable botTable,
    LocationTable locations,
    ISptLogger<SeasonalApplier> logger) : IOnLoad
{
    private const string NightCult = "halloweennightcult";
    private const string Summon = "halloweensummon";
    private const string Zombies = "halloweenzombies";
    private const string Pumpkin = "634959225289190e5e773b3b";
    private const string SignalPistol = "620109578d82e67e7911abf2";
    private const string AcidFlare = "635267f063651329f75a4ee8";
    private static readonly string[] HalloweenLootMaps = ["bigmap", "woods", "shoreline", "interchange", "lighthouse", "rezervbase", "tarkovstreets",
        "factory4_day", "factory4_night", "laboratory", "sandbox", "sandbox_high"];
    // drawers, jackets and PC blocks
    private static readonly HashSet<string> SmallContainers = ["5675838d4bdc2d95058b456e", "578f87b7245977356274f2cd", "578f8778245977358849a9b5",
        "5914944186f774189e5e76c2", "5937ef2b86f77408a47244b3", "59139c2186f77411564f8e42"];

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

            var mode = EventSchedule.SeasonalModeOn(settings, def.Id, today);
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
        if (halloweenOn && halloween.Outbreak2026) ApplyOutbreak2026(events);
        if (halloweenOn && halloween.Loot) AddHalloweenLoot();
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
            halloweenOn && halloween.ZombiesOn && halloween.LabInfectedOnly ? ZombieOnlyMaps(events) : [],
            aprilOn && !summonWanted,
            halloweenOn && halloween.ZombiesOn);

        if (halloweenOn)
            summary.Add($"Halloween parts: ritual {OnOff(halloween.Ritual)}, zombies {OnOff(halloween.ZombiesOn)}, zombie waves {WaveSummary(halloween)}, cultists {OnOff(halloween.Cultists)}, Lab infected only {OnOff(halloween.LabInfectedOnly)}, free Lab {OnOff(halloween.FreeLab)}, 2026 outbreak {OnOff(halloween.Outbreak2026)}, loot {OnOff(halloween.Loot)}");
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

    private static string WaveSummary(HalloweenParts halloween) => halloween.ZombieWaves
        ? $"on ({halloween.ZombieWaveSizeMin}-{halloween.ZombieWaveSizeMax} bots every {halloween.ZombieWaveIntervalMin}-{halloween.ZombieWaveIntervalMax} min)"
        : "off";

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
            zombies.Enabled = parts.ZombiesOn;
            // without ABPS this stops SPT from clearing the Lab; with ABPS the raid patch does the clearing when asked
            if (!parts.LabInfectedOnly) zombies.DisableBosses = [];
        }
    }

    /// <summary>
    /// Halloween 2026: infected on every map (SPT leaves Customs and Interchange at zero infection), Infected Tagilla
    /// wherever there are infected, and no karma loss for player Scavs killing Scavs.
    /// </summary>
    private void ApplyOutbreak2026(List<SeasonalEvent> events)
    {
        foreach (var e in OfType(events, SeasonalEventType.Halloween))
        {
            if (e.Settings!.ZombieSettings?.MapInfectionAmount is not { } infection) continue;
            foreach (var map in infection.Keys.ToList())
                if (infection[map] <= 0) infection[map] = 40;
        }
        if (seasonalConfig.EventBossSpawns.TryGetValue(Zombies, out var byMap))
            foreach (var spawns in byMap.Values)
                foreach (var tagilla in spawns.Where(b => b.BossName == "infectedTagilla" && (b.BossChance ?? 0) <= 0))
                    tagilla.BossChance = 12;
        foreach (var (name, type) in botTable.Types)
        {
            if (name is not ("assault" or "marksman" or "cursedassault" or "assaultgroup" or "crazyassaultevent") || type?.BotExperience is not { } experience) continue;
            // the karma loss is a negative standing for each kill, per difficulty
            foreach (var key in experience.StandingForKill?.Keys.ToList() ?? [])
                if (experience.StandingForKill![key] < 0) experience.StandingForKill[key] = 0;
        }
    }

    private void AddHalloweenLoot()
    {
        // pumpkins with sweets in small containers on every map
        foreach (var map in HalloweenLootMaps)
        {
            locations.GetLocation(map)?.StaticLoot?.AddTransformer(data =>
            {
                if (data is null) return data;
                foreach (var (container, details) in data)
                {
                    if (!SmallContainers.Contains(container.ToString()) || details.ItemDistribution is not { } dist || dist.Any(d => d.Tpl == Pumpkin)) continue;
                    var sum = dist.Sum(d => d.RelativeProbability ?? 0);
                    details.ItemDistribution = dist.Append(new ItemDistribution { Tpl = Pumpkin, RelativeProbability = (float)Math.Max(1, Math.Round(sum * 0.03 / 0.97)) }).ToList();
                }
                return data;
            });
        }
        // signal pistols and acid green flares on Scavs
        if (botTable.Types.TryGetValue("assault", out var scav) && scav?.BotInventory.Items is { } loot)
        {
            foreach (var pool in new[] { loot.Pockets, loot.TacticalVest, loot.Backpack })
            {
                if (pool is null) continue;
                AddShare(pool, SignalPistol, 0.02);
                AddShare(pool, AcidFlare, 0.04);
            }
        }
    }

    private static void AddShare(Dictionary<MongoId, double> pool, string tpl, double share)
    {
        var others = pool.Where(p => p.Key != tpl).Sum(p => p.Value);
        pool[tpl] = Math.Max(1, Math.Round(others * share / (1 - share)));
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
        if (halloweenOn && halloween.ZombiesOn && ZombieMaps(events, SeasonalEventType.Halloween) is { } maps) groups.Add(new(Zombies, maps));
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
