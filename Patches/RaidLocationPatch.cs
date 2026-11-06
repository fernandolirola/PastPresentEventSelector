using System.Reflection;
using HarmonyLib;
using PPES.Events;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.InRaid;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Patches;

/// <summary>Runs the raid pipeline on the map copy SPT builds for a raid, with the saved selection.</summary>
[Injectable(InjectionType.Singleton)]
public sealed class RaidBuilder(
    ConfigService config,
    LocationTable locations,
    ICloner cloner,
    SeasonalCalendar calendar,
    SeasonalEventConfig seasonalConfig,
    EventSchedule schedule,
    ISptLogger<RaidBuilder> logger)
{
    public void Build(LocationBase location, string name)
    {
        var mapId = name.ToLowerInvariant();
        var saved = config.Saved;
        // each generated location is a raid starting, which the "every N raids" rotation counts
        schedule.CountRaid();
        // seasonal events only change on restart, so SPT's startup setup is both what the raid wants and what it already has
        var started = SeasonalState.Started(config, calendar);
        var plan = new RaidPlan(started, started, saved.Halloween, schedule.RaidEvents(saved, DateTimeOffset.Now.DateTime), saved.SpawnsOf(mapId));
        RaidPipeline.Run(new RaidContext(location, mapId, locations, cloner), plan, seasonalConfig,
            (step, ex) => logger.Error($"{ConfigService.LogPrefix} {step} failed on {name}: {ex.Message}"));
    }
}

/// <summary>
/// Spawn and airdrop events are applied to the location copy SPT builds for each raid. ABPS rebuilds the
/// database spawns after every raid, so editing them once at startup would not last. The same rebuild drops
/// the seasonal event bosses SPT added at startup, so those are put back here too.
/// </summary>
public sealed class RaidLocationPatch : AbstractPatch
{
    internal static RaidBuilder? Builder;

    protected override MethodBase? GetTargetMethod() =>
        AccessTools.Method(typeof(LocationLifecycleService), nameof(LocationLifecycleService.GenerateLocationAndLoot));

    [PatchPostfix]
    public static void Postfix(LocationBase __result, string name)
    {
        if (Builder is null || __result is null || string.Equals(name, "hideout", StringComparison.OrdinalIgnoreCase)) return;
        Builder.Build(__result, name);
    }
}
