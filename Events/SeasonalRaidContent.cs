using PPES.Services;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Events;

/// <summary>
/// Seasonal content SPT writes into the maps once at startup and map mods like ABPS later drop: event bosses
/// and Santa. Applied to each raid copy (and to the page's preview) the same way SPT would.
/// </summary>
public static class SeasonalRaidContent
{
    public static void Apply(RaidContext ctx, SeasonalBossRegistry registry, SeasonalEventConfig seasonalConfig, ICloner cloner)
    {
        if (ctx.Location.BossLocationSpawn is null) return;
        // same as SPT's zombie disableBosses: the map's own bosses and raiders go, the infected are added next
        if (registry.InfectedOnlyMaps.Contains(ctx.MapId))
            ctx.Location.BossLocationSpawn.RemoveAll(b => b.BossName?.StartsWith("infected", StringComparison.OrdinalIgnoreCase) != true);
        AddEventBosses(ctx, registry, seasonalConfig, cloner);
        if (registry.Santa) AddSanta(ctx, seasonalConfig);
        else if (registry.RemoveSanta) ctx.Location.BossLocationSpawn.RemoveAll(b => b.BossName == "gifter");
        if (registry.StopDancingTree) StopDancingTree(ctx.Location);
    }

    /// <summary>Same rule as SPT's AddEventBossesToMaps: a boss is added only when the map does not have it yet.</summary>
    private static void AddEventBosses(RaidContext ctx, SeasonalBossRegistry registry, SeasonalEventConfig seasonalConfig, ICloner cloner)
    {
        var infectedOnly = registry.InfectedOnlyMaps.Contains(ctx.MapId);
        foreach (var group in registry.Groups)
        {
            if (group.Maps is { } maps && !maps.Contains(ctx.MapId)) continue;
            // SPT clears these maps after adding the ritual group, so only the infected group lands there
            if (infectedOnly && group.Maps is null) continue;
            if (!seasonalConfig.EventBossSpawns.TryGetValue(group.Key, out var byMap) || !byMap.TryGetValue(ctx.MapId, out var bosses)) continue;
            foreach (var boss in bosses)
            {
                if (ctx.Location.BossLocationSpawn!.All(b => b.TriggerName == "botEvent" || b.BossName != boss.BossName))
                    ctx.Location.BossLocationSpawn!.Add(cloner.Clone(boss)!);
            }
        }
    }

    /// <summary>Same as SPT's AddGifterBotToMaps; ABPS ships Santa at 0% on every map.</summary>
    private static void AddSanta(RaidContext ctx, SeasonalEventConfig seasonalConfig)
    {
        var setting = seasonalConfig.GifterSettings?.FirstOrDefault(g => string.Equals(g.Map, ctx.MapId, StringComparison.OrdinalIgnoreCase));
        if (setting is null) return;

        var existing = ctx.Location.BossLocationSpawn!.FirstOrDefault(b => b.BossName == "gifter");
        if (existing is not null)
        {
            existing.BossChance = setting.SpawnChance;
            return;
        }
        ctx.Location.BossLocationSpawn!.Add(new BossLocationSpawn
        {
            BossName = "gifter",
            BossChance = setting.SpawnChance,
            BossZone = setting.Zones,
            IsBossPlayer = false,
            BossDifficulty = "normal",
            BossEscortType = "gifter",
            BossEscortDifficulty = "normal",
            BossEscortAmount = "0",
            ForceSpawn = true,
            SpawnMode = ["regular", "pve"],
            Time = -1,
            TriggerId = "",
            TriggerName = "",
            Delay = 0,
            IsRandomTimeSpawn = false,
            IgnoreMaxBots = true,
        });
    }

    /// <summary>Undoes SPT's EnableDancingTree on the raid copy when the Khorovod part is off.</summary>
    private static void StopDancingTree(LocationBase location)
    {
        if (location.Events?.Khorovod is { } khorovod) khorovod.Chance = 0;
        if (location.BotLocationModifier is { } modifier) modifier.KhorovodChance = 0;
    }
}
