using PPES.Events;
using PPES.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace PPES.Services;

/// <summary>
/// SPT picks the active seasonal events once, in GameCallbacks, from the dates in seasonalevents.json.
/// Running just before it, PPES switches events off or moves a forced one onto today, and sets their parts.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.GameCallbacks - 1)]
public sealed class SeasonalApplier(
    ConfigService config,
    SeasonalCalendar calendar,
    SeasonalEventConfig seasonalConfig,
    BotTable botTable,
    LocationTable locations,
    ISptLogger<SeasonalApplier> logger) : IOnLoad
{
    private const string Pumpkin = "634959225289190e5e773b3b";
    private const string SignalPistol = "620109578d82e67e7911abf2";
    private const string AcidFlare = "635267f063651329f75a4ee8";
    private const double OutbreakTagillaChance = 12;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var settings = config.Applied;
        var today = config.StartedAt.Date;
        var events = seasonalConfig.Events ?? [];
        calendar.Capture(events, seasonalConfig.EventBossSpawns);
        var state = SeasonalState.Started(config, calendar);
        var summary = new List<string>();

        foreach (var def in EventCatalog.Seasonal)
        {
            var entries = SeasonalCalendar.EntriesOf(events, def).ToList();
            if (entries.Count == 0)
            {
                logger.Warning($"{ConfigService.LogPrefix} seasonalevents.json has no entry for {def.NameEn}, skipped");
                continue;
            }

            var mode = SeasonalSchedule.ModeOn(settings, def.Id, today);
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
            summary.Add($"{def.NameEn} {(state.Active.Contains(def.Id) ? "on" : "off")} ({mode})");
        }

        ApplyHalloweenParts(events, state);
        if (state.HalloweenOn && state.Halloween.Outbreak2026) ApplyOutbreak2026(events, state);
        if (state.HalloweenOn && state.Halloween.Loot) AddHalloweenLoot();
        ApplyChristmasParts(events, state.Christmas);
        ApplyAprilFoolsParts(events, state.AprilFools);

        // April Fools always adds these groups; an empty group keeps SPT from warning about a missing key
        if (!state.Ritual) EmptyGroup(SeasonalState.SummonGroup);
        if (!state.Cult) EmptyGroup(SeasonalState.CultGroup);

        var halloween = state.Halloween;
        var christmas = state.Christmas;
        var aprilFools = state.AprilFools;
        if (state.HalloweenOn)
            summary.Add($"Halloween parts: ritual {OnOff(halloween.Ritual)}, zombies {OnOff(halloween.ZombiesOn)}, zombie waves {WaveSummary(halloween)}, cultists {OnOff(halloween.Cultists)}, Lab infected only {OnOff(halloween.LabInfectedOnly)}, free Lab {OnOff(halloween.FreeLab)}, 2026 outbreak {OnOff(halloween.Outbreak2026)}, loot {OnOff(halloween.Loot)}");
        if (state.WinterOn)
            summary.Add($"Christmas parts: Santa {OnOff(christmas.Santa)}, hideout {OnOff(christmas.Hideout)}, Khorovod {OnOff(christmas.Khorovod)}, winter clothes {OnOff(christmas.WinterClothes)}");
        if (state.AprilFoolsOn)
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
        if (seasonalConfig.EventBossSpawns.ContainsKey(key)) seasonalConfig.EventBossSpawns[key] = new();
    }

    private static IEnumerable<SeasonalEventSettings> SettingsOf(List<SeasonalEvent> events, params SeasonalEventType[] types) =>
        events.Where(e => types.Contains(e.Type) && e.Settings is not null).Select(e => e.Settings!);

    private static void ApplyHalloweenParts(List<SeasonalEvent> events, SeasonalState state)
    {
        var parts = state.Halloween;
        foreach (var s in SettingsOf(events, SeasonalEventType.Halloween))
        {
            s.EnableSummoning = parts.Ritual;
            if (!parts.FreeLab) s.RemoveEntryRequirement = [];
            if (s.ZombieSettings is not { } zombies) continue;
            zombies.Enabled = parts.ZombiesOn;
            // without ABPS this stops SPT from clearing the Lab; with ABPS the raid patch does the clearing when asked
            if (!parts.LabInfectedOnly) zombies.DisableBosses = [];
        }
    }

    /// <summary>Halloween 2026: infected on every map, Infected Tagilla wherever there are infected, and no karma loss for player Scavs killing Scavs.</summary>
    private void ApplyOutbreak2026(List<SeasonalEvent> events, SeasonalState state)
    {
        foreach (var s in SettingsOf(events, SeasonalEventType.Halloween))
        {
            if (s.ZombieSettings?.MapInfectionAmount is not { } infection) continue;
            foreach (var map in infection.Keys.ToList())
                infection[map] = state.InfectionByKey.GetValueOrDefault(map.ToLowerInvariant(), infection[map]);
        }
        if (seasonalConfig.EventBossSpawns.TryGetValue(SeasonalState.ZombieGroup, out var byMap))
            foreach (var spawns in byMap.Values)
                foreach (var tagilla in spawns.Where(b => b.BossName == "infectedTagilla" && (b.BossChance ?? 0) <= 0))
                    tagilla.BossChance = OutbreakTagillaChance;
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
        LootShares.AddToSmallContainers(locations, Pumpkin, 0.03);
        if (botTable.Types.TryGetValue("assault", out var scav) && scav?.BotInventory.Items is { } loot)
        {
            foreach (var pool in new[] { loot.Pockets, loot.TacticalVest, loot.Backpack })
            {
                if (pool is null) continue;
                LootShares.AddShare(pool, SignalPistol, 0.02);
                LootShares.AddShare(pool, AcidFlare, 0.04);
            }
        }
    }

    private static void ApplyChristmasParts(List<SeasonalEvent> events, ChristmasParts parts)
    {
        foreach (var s in SettingsOf(events, SeasonalEventType.Christmas, SeasonalEventType.NewYears))
        {
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
        foreach (var s in SettingsOf(events, SeasonalEventType.AprilFools))
        {
            if (!parts.Winter) s.ForceSeason = null;
            if (!parts.RitualAndCult) s.ReplaceBotHostility = false;
        }
    }
}
