using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;

namespace PPES.Client;

internal static class RitualGroupSpawn
{
    private static ManualLogSource _logger;
    private static readonly RitualSpawnGate<BotCreationData, BotCreationData> Gate = new RitualSpawnGate<BotCreationData, BotCreationData>();

    public static void Enable(Harmony harmony, ManualLogSource logger)
    {
        _logger = logger;
        harmony.Patch(AccessTools.Method(typeof(BotBossSpawn), nameof(BotBossSpawn.SpawnBossAndFollowers)),
            prefix: new HarmonyMethod(typeof(RitualGroupSpawn), nameof(BeforeGroup)));
        harmony.Patch(AccessTools.Method(typeof(BotBossSpawn), nameof(BotBossSpawn.SpawnBossSupports)),
            prefix: new HarmonyMethod(typeof(RitualGroupSpawn), nameof(BeforeSupports)));
        logger.LogInfo("[RitualGroupFix] enabled; coordinated ritual spawn 1.7.2");
    }

    private static bool BeforeGroup(BotBossSpawn __instance, BotCreationData creationData, BossLocationSpawn wave,
        BotSpawnParams spawnParams, int followersCount, BotZone botZone, List<ISpawnPoint> openedPositions, ref Task __result)
    {
        if (wave.BossType != WildSpawnType.sectactPriestEvent || wave.EscortType != WildSpawnType.sectactPriestEvent ||
            followersCount <= 0 || wave.GetEscors() != null || creationData is null || Gate.IsReleased(creationData)) return true;
        __result = Gate.Run(creationData, async () =>
        {
            // The native group method assigns this before creating escort profiles; preserve that order.
            spawnParams.ShallBeGroup = new ShallBeGroupParams(group: true, bossGroup: true, followersCount + 1);
            var data = await BotCreationData.Create(new GetProfileDataParams(EPlayerSide.Savage, wave.EscortType,
                wave.EscortDif, wave.Time, spawnParams), __instance._botCreator, followersCount, __instance._spawner);
            if (data is null || data.SpawnStopped || data.Count != followersCount || data.Profiles.Any(p => p is null))
            {
                data?.StopSpawn();
                throw new InvalidOperationException("Escort preparation did not return the complete ritual group");
            }
            _logger.LogInfo($"[RitualGroupFix] escorts ready={data.Count}; releasing leader");
            return data;
        }, () => creationData.SpawnStopped,
        () => __instance.SpawnBossAndFollowers(creationData, wave, spawnParams, followersCount, botZone, openedPositions),
        ex => _logger.LogWarning($"[RitualGroupFix] escort preparation failed; using native spawning: {ex.Message}"),
        data => data.StopSpawn());
        return false;
    }

    private static bool BeforeSupports(BotBossSpawn __instance, BotCreationData bossCreationData, BotZone zone,
        int followersCount, BotSpawnParams spawnParams, BossLocationSpawn wave, List<ISpawnPoint> pointsToSpawn,
        bool forceSpawn, ref Task __result)
    {
        if (!Gate.TryTake(bossCreationData, out var prepared) || prepared is null) return true;
        if (bossCreationData.SpawnStopped || prepared.SpawnStopped)
        {
            prepared.StopSpawn();
            __result = Task.CompletedTask;
            return false;
        }
        // Keep the native activation callback, group assignment, spawn positions and cap/delay checks.
        __instance._spawner.TryToSpawnInZoneAndDelay(zone, prepared, withCheckMinMax: false, newWave: true,
            pointsToSpawn, forceSpawn);
        _logger.LogInfo($"[RitualGroupFix] released prepared escorts={prepared.Count}");
        __result = Task.CompletedTask;
        return false;
    }
}
