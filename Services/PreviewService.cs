using PPES.Events;
using PPES.Factions;
using PPES.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Services;

/// <param name="RawZones">The zone list as SPT has it, for the editor.</param>
/// <param name="Custom">The user edited this role on this map.</param>
public sealed record PreviewSpawn(string Role, string NameEn, string NamePt, double Chance, int Groups, string Zones, int EscortMin, int EscortMax, bool Changed,
    string RawZones = "", bool Custom = false);

public sealed record RaidPreview(string MapId, IReadOnlyList<PreviewSpawn> Spawns, double AirdropChance, int AirdropMax, bool Mortar, bool? NeedsKeycard,
    IReadOnlyList<PreviewSpawn> Removed);

public sealed record RoleOption(string Role, string NameEn, string NamePt);

/// <summary>
/// Runs the raid-time part of a selection on a copy of the map, the same way a raid does, so the page can show
/// what the next raid would look like. Works on unsaved picks too.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class PreviewService(LocationTable locations, ICloner cloner, SeasonalBossRegistry seasonalBosses, SeasonalEventConfig seasonalConfig)
{
    // roles the editor offers to add; trigger-only and event-scripted roles (ritual, Santa's helpers) are left out
    private static readonly string[] Addable =
    [
        "bossBully", "bossKojaniy", "bossSanitar", "bossGluhar", "bossKilla", "bossTagilla", "bossKolontay", "bossBoar", "bossKnight", "bossPartisan",
        "bossZryachiy", "sectantPriest", "sectantOni", "sectantPredvestnik", "sectantPrizrak", "arenaFighterEvent", "exUsec", "pmcBot",
        "skier", "peacemaker", "gifter", "infectedTagilla", "infectedAssault", "infectedPmc", "infectedCivil", "infectedLaborant",
        "ppesCleanupCrew", "ppesSmuggler", "ppesPillager", "ppesGus", "ppesBasmach", "bossWedge", "blackDivAssault",
    ];

    private static readonly string[] MortarMaps = ["bigmap", "woods", "shoreline", "rezervbase"];
    private static readonly HashSet<string> Ignored = new(StringComparer.OrdinalIgnoreCase)
    {
        "assault", "marksman", "pmcUSEC", "pmcBEAR", "cursedAssault", "assaultGroup", "crazyAssaultEvent",
    };

    private static readonly Dictionary<string, (string En, string Pt)> RoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bossBully"] = ("Reshala", "Reshala"),
        ["bossKojaniy"] = ("Shturman", "Shturman"),
        ["bossSanitar"] = ("Sanitar", "Sanitar"),
        ["bossGluhar"] = ("Glukhar", "Glukhar"),
        ["bossKilla"] = ("Killa", "Killa"),
        ["bossTagilla"] = ("Tagilla", "Tagilla"),
        ["bossKolontay"] = ("Kollontay", "Kollontay"),
        ["bossBoar"] = ("Kaban", "Kaban"),
        ["bossBoarSniper"] = ("Kaban's snipers", "Snipers do Kaban"),
        ["bossKnight"] = ("The Goons", "Os Goons"),
        ["bossPartisan"] = ("Partisan", "Partisan"),
        ["bossZryachiy"] = ("Zryachiy", "Zryachiy"),
        ["sectantPriest"] = ("Cultists", "Cultistas"),
        ["arenaFighterEvent"] = ("Bloodhounds", "Bloodhounds"),
        ["exUsec"] = ("Rogues", "Rogues"),
        ["pmcBot"] = ("Raiders", "Raiders"),
        ["skier"] = ("Skier's troops", "Tropas do Skier"),
        ["peacemaker"] = ("Peacekeeper's troops", "Tropas do Peacekeeper"),
        ["gifter"] = ("Santa", "Papai Noel"),
        ["peacefullZryachiyEvent"] = ("Zryachiy (ritual)", "Zryachiy (ritual)"),
        ["sectactPriestEvent"] = ("Ritual priests", "Sacerdotes do ritual"),
        ["ravangeZryachiyEvent"] = ("Vengeful Zryachiy", "Zryachiy Vingativo"),
        ["sectantOni"] = ("Oni", "Oni"),
        ["sectantPredvestnik"] = ("Harbinger", "Harbinger"),
        ["sectantPrizrak"] = ("Ghost", "Ghost"),
        ["infectedTagilla"] = ("Infected Tagilla", "Tagilla infectado"),
        ["infectedAssault"] = ("Infected", "Infectados"),
        ["infectedPmc"] = ("Infected PMCs", "PMCs infectados"),
        ["infectedCivil"] = ("Infected civilians", "Civis infectados"),
        ["infectedLaborant"] = ("Infected lab staff", "Laboratoristas infectados"),
        ["ppesCleanupCrew"] = ("Cleanup Crew", "Cleanup Crew"),
        ["ppesSmuggler"] = ("Smugglers", "Contrabandistas"),
        ["ppesPillager"] = ("Pillagers", "Saqueadores"),
        ["ppesGus"] = ("Gus", "Gus"),
        ["ppesBasmach"] = ("Basmach", "Basmach"),
        ["bossWedge"] = ("Wedge", "Wedge"),
        ["blackDivAssault"] = ("Black Division", "Black Division"),
    };

    /// <summary>Roles the editor can add: bosses and groups, and the factions whose server side loaded.</summary>
    public IReadOnlyList<RoleOption> AddableRoles() =>
        Addable
            .Where(r => FactionCatalog.ByRole(r) is null || FactionCatalog.IsReady(r))
            .Where(r => r is not ("bossWedge" or "blackDivAssault") || FactionCatalog.BlackDivision)
            .Select(r => RoleNames.TryGetValue(r, out var n) ? new RoleOption(r, n.En, n.Pt) : new RoleOption(r, r, r))
            .ToList();

    /// <summary>Zone names of a map: its open zones plus every zone its spawns use.</summary>
    public IReadOnlyList<string> ZonesOf(string mapId)
    {
        var location = locations.GetLocation(mapId)?.Base;
        if (location is null) return [];
        return (location.OpenZones ?? "").Split(',')
            .Concat((location.BossLocationSpawn ?? []).SelectMany(b => (b.BossZone ?? "").Split(',')))
            .Select(z => z.Trim()).Where(z => z.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public RaidPreview Preview(string mapId, IReadOnlyCollection<string> events, IReadOnlyList<SpawnEdit>? edits = null)
    {
        var source = locations.GetLocation(mapId)?.Base ?? throw new InvalidOperationException($"Unknown map {mapId}");
        var before = Summarize(source.BossLocationSpawn ?? []);

        var copy = cloner.Clone(source)!;
        var ctx = new RaidContext(copy, mapId, locations, cloner);
        // seasonal bosses as the server runs them now; seasonal changes only apply after a restart
        SeasonalRaidContent.Apply(ctx, seasonalBosses, seasonalConfig, cloner);
        foreach (var id in events)
            EventCatalog.Past(id)?.OnRaid?.Invoke(ctx);
        var withEvents = Summarize(copy.BossLocationSpawn ?? []);
        edits ??= [];
        SpawnEdits.Apply(ctx, edits);

        var after = Summarize(copy.BossLocationSpawn ?? []);
        var custom = edits.Where(e => !e.Remove).Select(e => e.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removed = edits.Where(e => e.Remove && withEvents.ContainsKey(e.Role)).Select(e => withEvents[e.Role] with { Custom = true }).ToList();
        var spawns = after.Values
            .Select(s => s with
            {
                Changed = !before.TryGetValue(s.Role, out var old) || old.Chance != s.Chance || old.Zones != s.Zones || old.Groups != s.Groups,
                Custom = custom.Contains(s.Role),
            })
            .OrderByDescending(s => s.Changed).ThenByDescending(s => s.Chance).ThenBy(s => s.NameEn)
            .ToList();

        var airdrops = copy.AirdropParameters ?? [];
        bool? keycard = mapId is "laboratory" or "labyrinth"
            ? !events.Contains("softcore", StringComparer.OrdinalIgnoreCase) && !(mapId == "laboratory" && events.Contains("freeLabs", StringComparer.OrdinalIgnoreCase))
            : null;

        return new RaidPreview(
            mapId,
            spawns,
            airdrops.Count == 0 ? 0 : airdrops.Max(a => a.PlaneAirdropChance ?? 0),
            airdrops.Count == 0 ? 0 : airdrops.Max(a => a.PlaneAirdropMax ?? 0),
            events.Contains("mortarStrike", StringComparer.OrdinalIgnoreCase) && MortarMaps.Contains(mapId),
            keycard,
            removed);
    }

    private static Dictionary<string, PreviewSpawn> Summarize(IEnumerable<SPTarkov.Server.Core.Models.Eft.Common.BossLocationSpawn> spawns)
    {
        var result = new Dictionary<string, PreviewSpawn>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in spawns.Where(s => s.BossName is { } n && !Ignored.Contains(n)).GroupBy(s => s.BossName!, StringComparer.OrdinalIgnoreCase))
        {
            var (en, pt) = RoleNames.GetValueOrDefault(group.Key, (group.Key, group.Key));
            var zones = string.Join(", ", group.SelectMany(s => (s.BossZone ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(PrettyZone).Distinct().Take(6));
            // SPT stores escort size as a weighted list ("1,1,2,2,3"); the page only needs its range
            var sizes = group.SelectMany(s => (s.BossEscortAmount ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(v => int.TryParse(v.Trim(), out var n) ? n : 0).DefaultIfEmpty(0).ToList();
            var raw = string.Join(",", group.SelectMany(s => (s.BossZone ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Distinct());
            result[group.Key] = new PreviewSpawn(group.Key, en, pt, group.Max(s => s.BossChance ?? 0), group.Count(), zones, sizes.Min(), sizes.Max(), false, raw);
        }
        return result;
    }

    private static string PrettyZone(string zone)
    {
        var name = zone.StartsWith("Zone_") ? zone[5..] : zone.StartsWith("Zone") ? zone[4..] : zone.StartsWith("BotZone") ? zone[7..] : zone;
        return name.Length == 0 ? zone : name;
    }
}
