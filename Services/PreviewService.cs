using System.Runtime.ExceptionServices;
using PPES.Events;
using PPES.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Services;

/// <param name="RawZones">The zone list as SPT has it, for the editor.</param>
/// <param name="Custom">The user edited this role on this map.</param>
public sealed record PreviewSpawn(string Role, string NameEn, string NamePt, double Chance, int Groups, string Zones, int EscortMin, int EscortMax, bool Changed,
    string RawZones = "", bool Custom = false);

public sealed record RaidPreview(string MapId, IReadOnlyList<PreviewSpawn> Spawns, double AirdropChance, int AirdropMax, bool Mortar, bool? NeedsKeycard, double? ZombieInfection,
    IReadOnlyList<PreviewSpawn> Removed);

/// <summary>
/// Runs the raid pipeline for a selection on a copy of the map, so the page can show what the next raid would
/// look like. Works on unsaved picks too.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class PreviewService(LocationTable locations, ICloner cloner, SeasonalCalendar calendar, SeasonalEventConfig seasonalConfig,
    ConfigService config, EventSchedule schedule)
{
    private static readonly Dictionary<string, string[]> ZoneFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sandbox"] = ["ZoneSandbox"],
        ["sandbox_high"] = ["ZoneSandbox"],
    };

    /// <summary>Zone names of a map: its open zones plus every zone its spawns use.</summary>
    public IReadOnlyList<string> ZonesOf(string mapId)
    {
        var location = locations.GetLocation(mapId)?.Base;
        if (location is null) return [];
        return ZoneList.Split(location.OpenZones)
            .Concat((location.BossLocationSpawn ?? []).SelectMany(b => ZoneList.Split(b.BossZone)))
            .Concat(ZoneFallbacks.GetValueOrDefault(mapId) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public RaidPreview Preview(string mapId, PpesConfig draft)
    {
        var source = locations.GetLocation(mapId)?.Base ?? throw new InvalidOperationException($"Unknown map {mapId}");
        var before = Summarize(source.BossLocationSpawn ?? []);
        var now = DateTimeOffset.Now.DateTime;
        var seasonal = SeasonalState.For(draft, calendar, now.Date);
        var events = schedule.RaidEvents(draft, now);
        var edits = draft.SpawnsOf(mapId);

        var copy = cloner.Clone(source)!;
        var ctx = new RaidContext(copy, mapId, locations, cloner, preview: true);
        var withEvents = before;
        RaidPipeline.Run(ctx, new RaidPlan(seasonal, SeasonalState.Started(config, calendar), draft.Halloween, events, edits), seasonalConfig,
            (_, ex) => ExceptionDispatchInfo.Throw(ex), () => withEvents = Summarize(copy.BossLocationSpawn ?? []));

        var after = Summarize(copy.BossLocationSpawn ?? []);
        var custom = edits.Where(e => !e.Remove).Select(e => e.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removed = edits.Where(e => e.Remove && withEvents.ContainsKey(e.Role)).Select(e => withEvents[e.Role] with { Custom = true }).ToList();
        var spawns = after.Values
            .Select(s => s with
            {
                Changed = !before.TryGetValue(s.Role, out var old) || old.Chance != s.Chance || old.Zones != s.Zones || old.Groups != s.Groups
                    || old.EscortMin != s.EscortMin || old.EscortMax != s.EscortMax,
                Custom = custom.Contains(s.Role),
            })
            .OrderByDescending(s => s.Changed).ThenByDescending(s => s.Chance).ThenBy(s => s.NameEn)
            .ToList();

        var defs = events.Select(EventCatalog.Past).OfType<PastEventDef>().ToList();
        bool? keycard = GameMaps.Get(mapId)?.Keycard == true
            ? (copy.AccessKeys?.Any() == true || copy.AccessKeysPvE?.Any() == true)
                && !defs.Any(e => e.FreeAccess.Contains(mapId)) && !seasonal.FreeEntryMaps.Contains(mapId)
            : null;
        var airdrops = copy.AirdropParameters ?? [];

        return new RaidPreview(
            mapId,
            spawns,
            airdrops.Count == 0 ? 0 : airdrops.Max(a => a.PlaneAirdropChance ?? 0),
            airdrops.Count == 0 ? 0 : airdrops.Max(a => a.PlaneAirdropMax ?? 0),
            defs.Any(e => e.Mortar.Contains(mapId)),
            keycard,
            seasonal.Infection(mapId),
            removed);
    }

    private static Dictionary<string, PreviewSpawn> Summarize(IEnumerable<BossLocationSpawn> spawns)
    {
        var result = new Dictionary<string, PreviewSpawn>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in spawns.Where(s => s.BossName is { } n && !BotRoles.Ignored.Contains(n)).GroupBy(s => s.BossName!, StringComparer.OrdinalIgnoreCase))
        {
            var (en, pt) = BotRoles.NameOf(group.Key);
            var allZones = group.SelectMany(s => ZoneList.Split(s.BossZone)).Distinct().ToList();
            // SPT stores escort size as a weighted list ("1,1,2,2,3"); the page only needs its range
            var sizes = group.SelectMany(s => (s.BossEscortAmount ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(v => int.TryParse(v.Trim(), out var n) ? n : 0).DefaultIfEmpty(0).ToList();
            var chance = group.Max(s => s.BossChance ?? 0);
            // SPT's zombie groups store chance times 100
            if (SeasonalRaidContent.IsInfected(group.Key) && chance > 100) chance /= 100;
            result[group.Key] = new PreviewSpawn(group.Key, en, pt, chance, group.Count(), string.Join(", ", allZones.Select(ZoneLabel).Distinct().Take(6)),
                sizes.Min(), sizes.Max(), false, string.Join(",", allZones));
        }
        return result;
    }

    public static string ZoneLabel(string zone)
    {
        if (string.Equals(zone, "ZoneRailStrorage", StringComparison.OrdinalIgnoreCase)) return "Rail Storage";
        var name = zone.StartsWith("Zone_") ? zone[5..] : zone.StartsWith("Zone") ? zone[4..] : zone.StartsWith("BotZone") ? zone[7..] : zone;
        return name.Length == 0 ? zone : name;
    }
}
