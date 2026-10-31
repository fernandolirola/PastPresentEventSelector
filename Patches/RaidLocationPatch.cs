using System.Reflection;
using HarmonyLib;
using PPES.Events;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.InRaid;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Patches;

/// <summary>
/// Spawn and airdrop events are applied to the location copy SPT builds for each raid. ABPS rebuilds the
/// database spawns after every raid, so editing them once at startup would not last. The same rebuild drops
/// the seasonal event bosses SPT added at startup, so those are put back here too.
/// </summary>
public sealed class RaidLocationPatch : AbstractPatch
{
    private static ConfigService? _config;
    private static LocationTable? _locations;
    private static ICloner? _cloner;
    private static ISptLogger<EventApplier>? _logger;
    private static SeasonalBossRegistry? _seasonalBosses;
    private static SeasonalEventConfig? _seasonalConfig;

    public static void Init(ConfigService config, LocationTable locations, ICloner cloner, ISptLogger<EventApplier> logger,
        SeasonalBossRegistry seasonalBosses, SeasonalEventConfig seasonalConfig)
    {
        _seasonalBosses = seasonalBosses;
        _seasonalConfig = seasonalConfig;
        _config = config;
        _locations = locations;
        _cloner = cloner;
        _logger = logger;
    }

    protected override MethodBase? GetTargetMethod() =>
        AccessTools.Method(typeof(LocationLifecycleService), nameof(LocationLifecycleService.GenerateLocationAndLoot));

    [PatchPostfix]
    public static void Postfix(LocationBase __result, string name)
    {
        if (_config is null || _locations is null || _cloner is null || __result is null) return;
        if (string.Equals(name, "hideout", StringComparison.OrdinalIgnoreCase)) return;

        var ctx = new RaidContext(__result, name.ToLowerInvariant(), _locations, _cloner);
        if (_seasonalBosses is not null && _seasonalConfig is not null)
        {
            SeasonalRaidContent.Apply(ctx, _seasonalBosses, _seasonalConfig, _cloner);
            SeasonalRaidContent.AddZombieWaves(ctx, _seasonalBosses, _config.Saved.Halloween, _cloner);
        }
        // each generated location is a raid starting, which the "every N raids" rotation counts
        EventSchedule.CountRaid(_config);
        foreach (var id in EventSchedule.RaidEvents(_config.Saved, DateTimeOffset.Now.DateTime))
        {
            if (EventCatalog.Past(id) is not { OnRaid: { } onRaid } def) continue;
            try
            {
                onRaid(ctx);
            }
            catch (Exception ex)
            {
                _logger?.Error($"{ConfigService.LogPrefix} {def.Name} failed on {name}: {ex.Message}");
            }
        }
        try
        {
            SpawnEdits.Apply(ctx, _config.Saved.SpawnsOf(ctx.MapId));
        }
        catch (Exception ex)
        {
            _logger?.Error($"{ConfigService.LogPrefix} Spawn edits failed on {name}: {ex.Message}");
        }
    }
}
