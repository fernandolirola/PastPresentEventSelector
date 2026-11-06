using PPES.Models;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace PPES.Events;

/// <summary>
/// Seasonal content SPT writes into the maps once at startup and map mods like ABPS later drop: event bosses
/// and Santa. Applied to each raid copy (and to the page's preview) the same way SPT would.
/// </summary>
internal static class SeasonalRaidContent
{
    /// <param name="baseline">The state SPT set the maps up with at startup; groups it added that <paramref name="state"/> does not want are taken out.</param>
    public static void Apply(RaidContext ctx, SeasonalState state, SeasonalState baseline, SeasonalEventConfig seasonalConfig)
    {
        if (ctx.Location.BossLocationSpawn is not { } spawns) return;
        var wanted = state.BossGroups.Select(g => g.Key).ToHashSet();
        foreach (var group in baseline.BossGroups.Where(g => !wanted.Contains(g.Key)))
            foreach (var boss in baseline.GroupOn(group.Key, ctx.MapId))
                spawns.RemoveAll(b => b.BossName == boss.BossName && b.BossZone == boss.BossZone && b.TriggerName == boss.TriggerName);

        // same as SPT's zombie disableBosses: the map's own bosses and raiders go, the infected are added next
        var infectedOnly = state.InfectedOnlyMaps.Contains(ctx.MapId);
        if (infectedOnly) spawns.RemoveAll(b => !IsInfected(b.BossName));

        foreach (var group in state.BossGroups)
        {
            if (group.Maps is { } maps && !maps.Contains(ctx.MapId)) continue;
            // SPT clears these maps after adding the ritual group, so only the infected group lands there
            if (infectedOnly && group.Maps is null) continue;
            AddGroup(ctx, state.GroupOn(group.Key, ctx.MapId));
        }

        if (state.Santa) AddSanta(ctx, seasonalConfig);
        else if (state.RemoveSanta || baseline.Santa) spawns.RemoveAll(b => b.BossName == "gifter");
        if (state.StopDancingTree) StopDancingTree(ctx.Location);
    }

    public static bool IsInfected(string? role) => role?.StartsWith("infected", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Same rule as SPT's AddEventBossesToMaps: a boss is added only when the map does not have it yet.</summary>
    private static void AddGroup(RaidContext ctx, IEnumerable<BossLocationSpawn> group)
    {
        var spawns = ctx.Location.BossLocationSpawn!;
        foreach (var boss in group)
        {
            if (spawns.Any(b => b.TriggerName != "botEvent" && b.BossName == boss.BossName)) continue;
            spawns.Add(ctx.Cloner.Clone(boss)!);
        }
    }

    public static void AddZombieWaves(RaidContext ctx, HalloweenParts waves)
    {
        if (ctx.Location.BossLocationSpawn is not { } spawns) return;
        var templates = spawns.Where(b => IsInfected(b.BossName) && !string.Equals(b.BossName, "infectedTagilla", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (templates.Length == 0) return;

        var lastWave = (int)Math.Round((ctx.Location.EscapeTimeLimit ?? 0) * 60) - 2 * 60;
        var (intervalMin, intervalMax) = (waves.ZombieWaveIntervalMin, Math.Max(waves.ZombieWaveIntervalMin, waves.ZombieWaveIntervalMax));
        var (sizeMin, sizeMax) = (waves.ZombieWaveSizeMin, Math.Max(waves.ZombieWaveSizeMin, waves.ZombieWaveSizeMax));
        for (var time = Random.Shared.Next(intervalMin, intervalMax + 1) * 60; time <= lastWave; time += Random.Shared.Next(intervalMin, intervalMax + 1) * 60)
        {
            var waveSize = Random.Shared.Next(sizeMin, sizeMax + 1);
            for (var member = 0; member < waveSize; member++)
            {
                var spawn = ctx.Cloner.Clone(templates[Random.Shared.Next(templates.Length)])!;
                spawn.BossChance = 100;
                spawn.BossEscortAmount = "0";
                spawn.Supports = [];
                spawn.Time = time;
                spawn.TriggerId = "";
                spawn.TriggerName = "";
                spawn.Delay = 0;
                spawn.IsRandomTimeSpawn = false;
                spawn.ForceSpawn = true;
                spawn.IgnoreMaxBots = true;
                spawns.Add(spawn);
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
