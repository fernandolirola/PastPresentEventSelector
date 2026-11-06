using PPES.Models;

namespace PPES.Events;

/// <summary>
/// The user's own boss and faction changes from the raid preview, applied to each raid copy after the events so
/// they have the last word.
/// </summary>
public static class SpawnEdits
{
    public static void Apply(RaidContext ctx, IEnumerable<SpawnEdit> edits)
    {
        if (ctx.Location.BossLocationSpawn is not { } spawns) return;
        foreach (var e in edits)
        {
            if (e.Remove)
            {
                ctx.Remove(e.Role);
                continue;
            }
            var escort = EscortList(e.Escort);
            var own = spawns.Where(s => string.Equals(s.BossName, e.Role, StringComparison.OrdinalIgnoreCase)).ToList();
            if (own.Count > 0)
            {
                if (e.Zones is not null) own = ApplyZones(spawns, own, e.Zones);
                foreach (var s in own)
                {
                    if (e.Chance is { } chance) s.BossChance = StoredChance(e.Role, chance, s.BossChance ?? 0);
                    if (escort is not null) s.BossEscortAmount = escort;
                }
                continue;
            }
            if (e.Chance is not { } addChance) continue;
            // a boss from another map keeps its followers; anything else is built from one of this map's spawns
            var zones = e.Zones ?? ctx.Location.OpenZones ?? "";
            if (ctx.HomeOf(e.Role) is { } home && home != ctx.MapId)
                ctx.AddFrom(home, e.Role, zones, addChance, escort);
            else
                Effects.AddGroup(ctx, e.Role, escort ?? "0", zones, addChance);
        }
    }

    /// <summary>"2" or "1-3" (any order, clamped to 0..10), or null when empty or unreadable.</summary>
    public static string? NormalizeEscort(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var parts = text.Split(['-', ',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => int.TryParse(p, out var n) ? Math.Clamp(n, 0, 10) : -1).ToList();
        if (parts.Count == 0 || parts.Any(n => n < 0)) return null;
        var (min, max) = (parts.Min(), parts.Max());
        return min == max ? $"{min}" : $"{min}-{max}";
    }

    /// <summary>SPT stores escort size as a weighted list, so "1-3" becomes "1,2,3".</summary>
    private static string? EscortList(string? escort)
    {
        if (NormalizeEscort(escort) is not { } normalized) return null;
        var parts = normalized.Split('-').Select(int.Parse).ToArray();
        return parts.Length == 1 ? normalized : string.Join(",", Enumerable.Range(parts[0], parts[1] - parts[0] + 1));
    }

    private static double StoredChance(string role, double chance, double current) =>
        role.StartsWith("infected", StringComparison.OrdinalIgnoreCase) && current > 100 ? chance * 100 : chance;

    private static List<SPTarkov.Server.Core.Models.Eft.Common.BossLocationSpawn> ApplyZones(
        List<SPTarkov.Server.Core.Models.Eft.Common.BossLocationSpawn> spawns,
        List<SPTarkov.Server.Core.Models.Eft.Common.BossLocationSpawn> own,
        string zones)
    {
        var requested = zones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var remaining = new List<SPTarkov.Server.Core.Models.Eft.Common.BossLocationSpawn>();
        // the spawn that takes the new zones when none of the old ones were kept
        var fallback = own.FirstOrDefault(s => string.IsNullOrEmpty(s.TriggerId));

        foreach (var spawn in own)
        {
            var current = (spawn.BossZone ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var kept = current.Where(requested.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (current.Length > 0 && kept.Length == 0 && spawn != fallback)
            {
                spawns.Remove(spawn);
                continue;
            }
            if (kept.Length > 0) spawn.BossZone = string.Join(",", kept);
            covered.UnionWith(kept);
            remaining.Add(spawn);
        }

        var added = requested.Except(covered, StringComparer.OrdinalIgnoreCase).ToArray();
        if (fallback is not null)
        {
            var zonesNow = (fallback.BossZone ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var kept = zonesNow.Where(requested.Contains).ToArray();
            if (zonesNow.Length > 0 && kept.Length == 0 && added.Length == 0)
            {
                spawns.Remove(fallback);
                remaining.Remove(fallback);
            }
            else if (added.Length > 0)
                fallback.BossZone = string.Join(",", kept.Concat(added));
        }
        return remaining;
    }
}
