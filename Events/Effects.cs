using PPES.Factions;
using PPES.Services;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace PPES.Events;

/// <summary>The building blocks events are made of.</summary>
internal static class Effects
{
    /// <summary>Adds an item to small loot containers on every map, at about <paramref name="percent"/>% of each container's picks.</summary>
    public static void AddStaticLoot(LoadContext ctx, string tpl, double percent) =>
        LootShares.AddToSmallContainers(ctx.Locations, tpl, percent / 100);

    /// <summary>Multiplies SPT's loot multipliers for a map, on top of what the user or other mods set.</summary>
    public static void ScaleLoot(LoadContext ctx, string map, double loose, double containers)
    {
        if (ctx.LocationConfig.LooseLootMultiplier.TryGetValue(map, out var l)) ctx.LocationConfig.LooseLootMultiplier[map] = l * loose;
        if (ctx.LocationConfig.StaticLootMultiplier.TryGetValue(map, out var s)) ctx.LocationConfig.StaticLootMultiplier[map] = s * containers;
    }

    public static void EnsureWeaponCrates(LoadContext ctx, int min, int max)
    {
        foreach (var (type, loot) in ctx.AirdropConfig.Loot)
        {
            if (type is "radar" or "toiletPaper" || loot.WeaponCrateCount is null) continue;
            loot.WeaponCrateCount.Min = Math.Max(loot.WeaponCrateCount.Min, min);
            loot.WeaponCrateCount.Max = Math.Max(loot.WeaponCrateCount.Max, max);
        }
    }

    public static void GatherScavBosses(RaidContext ctx, string map, string zones)
    {
        foreach (var (boss, home) in EventCatalog.ScavBosses)
            ctx.AddFrom(home, boss, home == map ? null : zones, 100);
    }

    /// <summary>Adds groups of a role until the map has <paramref name="groups"/> of them in these zones. False when the role cannot spawn.</summary>
    public static bool AddGroup(RaidContext ctx, string role, string escortAmount, string zones, double chance, int groups = 1,
        string? escortType = null, bool forceSpawn = false)
    {
        if (!FactionCatalog.CanSpawn(role)) return false;
        if (ctx.Location.BossLocationSpawn is not { } spawns) return false;
        var existing = spawns.Count(b => b.BossName == role && b.BossZone == zones);
        if (existing > 0) ctx.SetChance(role, chance);
        for (var i = existing; i < groups; i++)
        {
            // any spawn of the map works as a template, every field that matters is set below
            if (spawns.FirstOrDefault() is not { } template) return false;
            var spawn = ctx.Cloner.Clone(template)!;
            spawn.BossName = role;
            spawn.BossEscortType = escortType ?? role;
            spawn.BossEscortAmount = escortAmount;
            spawn.BossDifficulty = "normal";
            spawn.BossEscortDifficulty = "normal";
            spawn.BossZone = zones;
            spawn.BossChance = chance;
            spawn.Supports = [];
            spawn.TriggerId = "";
            spawn.TriggerName = "";
            spawn.Time = -1;
            spawn.IgnoreMaxBots = true;
            if (forceSpawn) spawn.ForceSpawn = true;
            spawns.Add(spawn);
        }
        return true;
    }

    public static void AddFactionGroup(RaidContext ctx, string role, string escortAmount) =>
        AddGroup(ctx, role, escortAmount, EventCatalog.FactionZones.GetValueOrDefault(ctx.MapId, ""), 100);

    /// <summary>Makes a boss spawn every raid, keeping a mod's own entry (its trigger included) when there is one.</summary>
    public static void ForceBoss(RaidContext ctx, string boss, string escort, string escortAmount, string zones)
    {
        var spawns = ctx.Location.BossLocationSpawn?.Where(b => string.Equals(b.BossName, boss, StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
        if (spawns.Count > 0)
        {
            foreach (var spawn in spawns) spawn.BossChance = 100;
            return;
        }
        AddGroup(ctx, boss, escortAmount, zones, 100, escortType: escort, forceSpawn: true);
    }

    public static void RemoveAccessKeys(LoadContext ctx, IEnumerable<string> maps)
    {
        foreach (var map in maps)
        {
            if (ctx.Locations.GetLocation(map)?.Base is not { } location) continue;
            location.AccessKeys = [];
            location.AccessKeysPvE = [];
        }
    }

    public static void ScaleOverweight(LoadContext ctx, double factor)
    {
        var stamina = ctx.Globals.Configuration!.Stamina!;
        stamina.BaseOverweightLimits = Scale(stamina.BaseOverweightLimits);
        stamina.WalkOverweightLimits = Scale(stamina.WalkOverweightLimits);
        stamina.SprintOverweightLimits = Scale(stamina.SprintOverweightLimits);
        stamina.WalkSpeedOverweightLimits = Scale(stamina.WalkSpeedOverweightLimits);

        Vector3 Scale(Vector3 v) => v with { X = (float)(v.X * factor), Y = (float)(v.Y * factor) };
    }

    public static void UnlockTraders(LoadContext ctx, IEnumerable<string> traderIds)
    {
        foreach (var id in traderIds)
        {
            if (ctx.Traders.GetTrader(new MongoId(id))?.Base?.LoyaltyLevels is not { } levels) continue;
            foreach (var level in levels)
            {
                level.MinLevel = 0;
                level.MinSalesSum = 0;
                level.MinStanding = 0;
            }
        }
    }

    public static void InstantHideout(LoadContext ctx)
    {
        foreach (var area in ctx.Hideout.Areas ?? [])
        {
            if (area.Stages is null) continue;
            foreach (var stage in area.Stages.Values)
                stage.ConstructionTime = 0;
        }
    }

    public static void EnableMortarStrike(LoadContext ctx, IEnumerable<string> maps)
    {
        var configs = ctx.Globals.Configuration?.ArtilleryShelling?.ArtilleryMapsConfigs;
        if (configs is null)
        {
            ctx.Logger.Warning($"{ConfigService.LogPrefix} globals has no ArtilleryShelling section, Mortar Strike skipped");
            return;
        }
        foreach (var map in maps)
        {
            // the artillery settings are keyed by the location's own spelling ("Woods", "RezervBase")
            var settings = configs.FirstOrDefault(c => string.Equals(c.Key, map, StringComparison.OrdinalIgnoreCase)).Value;
            if (settings is null) continue;
            settings.PlanedShellingOn = true;
            if (settings.ArtilleryShellingAirDropSettings is { } airdrop) airdrop.UseAirDrop = true;
        }
    }
}
