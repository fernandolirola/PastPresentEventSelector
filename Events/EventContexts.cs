using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Events;

public sealed record LoadContext(
    GlobalTable Globals,
    TradersTable Traders,
    HideoutTable Hideout,
    LocationTable Locations,
    QuestConfig QuestConfig,
    AirdropConfig AirdropConfig,
    ISptLogger<Services.EventApplier> Logger);

/// <summary>
/// Wraps the per-raid copy of a map. SPT clones the location base for every raid, so edits here never
/// touch the database and run after map mods like ABPS have rebuilt their spawns.
/// </summary>
public sealed class RaidContext(LocationBase location, string mapId, LocationTable locations, ICloner cloner)
{
    public LocationBase Location { get; } = location;
    public string MapId { get; } = mapId;

    public bool Is(params string[] maps) => maps.Contains(MapId, StringComparer.OrdinalIgnoreCase);

    private IEnumerable<BossLocationSpawn> Spawns(string boss) =>
        Location.BossLocationSpawn?.Where(b => string.Equals(b.BossName, boss, StringComparison.OrdinalIgnoreCase)) ?? [];

    public void SetChance(string boss, double chance)
    {
        foreach (var spawn in Spawns(boss).Where(s => string.IsNullOrEmpty(s.TriggerId)))
            spawn.BossChance = chance;
    }

    public void Remove(string boss) =>
        Location.BossLocationSpawn?.RemoveAll(b => string.Equals(b.BossName, boss, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Brings a boss from its home map. The home entry keeps followers and supports; when another mod removed it,
    /// any spawn of this map is used as a template so the event still works.
    /// </summary>
    public bool AddFrom(string sourceMap, string boss, string? zones, double chance, string? escortAmount = null)
    {
        if (Location.BossLocationSpawn is null) return false;
        // two events bringing the same boss (or the boss already being home) must not spawn it twice
        if (Spawns(boss).Any(s => string.IsNullOrEmpty(s.TriggerId)))
        {
            SetChance(boss, chance);
            return true;
        }

        var source = locations.GetLocation(sourceMap)?.Base?.BossLocationSpawn?
            .FirstOrDefault(b => string.Equals(b.BossName, boss, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(b.TriggerId));
        var template = source ?? Location.BossLocationSpawn.FirstOrDefault();
        if (template is null) return false;

        var spawn = cloner.Clone(template)!;
        if (source is null)
        {
            spawn.BossName = boss;
            spawn.BossEscortType = boss;
            spawn.BossEscortAmount = "0";
            spawn.Supports = [];
            spawn.TriggerId = "";
            spawn.TriggerName = "";
        }
        spawn.BossChance = chance;
        spawn.Time = -1;
        if (zones is not null) spawn.BossZone = zones;
        if (escortAmount is not null) spawn.BossEscortAmount = escortAmount;
        Location.BossLocationSpawn.Add(spawn);
        return true;
    }

    public void AddSpawn(BossLocationSpawn spawn) => Location.BossLocationSpawn?.Add(spawn);

    public BossLocationSpawn? CloneAnySpawn() =>
        Location.BossLocationSpawn?.FirstOrDefault() is { } first ? cloner.Clone(first) : null;
}
