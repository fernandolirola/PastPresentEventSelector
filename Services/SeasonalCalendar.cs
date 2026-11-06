using PPES.Events;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace PPES.Services;

public sealed record SeasonalWindow(int StartMonth, int StartDay, int EndMonth, int EndDay);

/// <summary>
/// SPT's stock seasonal data, captured before PPES changes it: the dates, so the page can tell what "auto" means
/// today, and the Halloween zombie settings the PPES parts switch on and off.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class SeasonalCalendar
{
    private readonly Dictionary<string, List<SeasonalWindow>> _windows = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Infection per map key of seasonalevents.json ("factory4", "sandbox" cover both variants).</summary>
    public IReadOnlyDictionary<string, double> Infection { get; private set; } = new Dictionary<string, double>();

    /// <summary>Maps where SPT keeps only the infected while zombies are on.</summary>
    public IReadOnlySet<string> InfectedOnlyMaps { get; private set; } = new HashSet<string>();

    /// <summary>Maps Halloween lets you enter without a keycard.</summary>
    public IReadOnlySet<string> HalloweenFreeEntry { get; private set; } = new HashSet<string>();

    /// <summary>SPT's event boss groups by key and map, kept before PPES empties the unwanted ones, so the preview can show them switched back on.</summary>
    public IReadOnlyDictionary<string, Dictionary<string, List<BossLocationSpawn>>> BossGroups { get; private set; } =
        new Dictionary<string, Dictionary<string, List<BossLocationSpawn>>>();

    public IReadOnlyList<SeasonalWindow> WindowsOf(string id) => _windows.GetValueOrDefault(id) ?? [];

    public bool InSeason(string id, DateTime date) =>
        WindowsOf(id).Any(w => date.DateIsBetweenTwoDates(w.StartMonth, w.StartDay, w.EndMonth, w.EndDay));

    public void Capture(IReadOnlyList<SeasonalEvent> events, IReadOnlyDictionary<string, Dictionary<string, List<BossLocationSpawn>>>? bossGroups = null)
    {
        BossGroups = bossGroups?.ToDictionary(p => p.Key, p => p.Value) ?? new Dictionary<string, Dictionary<string, List<BossLocationSpawn>>>();
        foreach (var def in EventCatalog.Seasonal)
            _windows[def.Id] = EntriesOf(events, def)
                .Where(e => e.Enabled).Select(e => new SeasonalWindow(e.StartMonth, e.StartDay, e.EndMonth, e.EndDay)).ToList();

        var halloween = events.Where(e => e.Type == SeasonalEventType.Halloween && e.Settings is not null).Select(e => e.Settings!).ToList();
        Infection = halloween.Select(s => s.ZombieSettings?.MapInfectionAmount).FirstOrDefault(m => m is not null)?
            .ToDictionary(p => p.Key.ToLowerInvariant(), p => p.Value) ?? new Dictionary<string, double>();
        InfectedOnlyMaps = halloween.SelectMany(s => s.ZombieSettings?.DisableBosses ?? []).Select(m => m.ToLowerInvariant()).ToHashSet();
        HalloweenFreeEntry = halloween.SelectMany(s => s.RemoveEntryRequirement ?? []).Select(m => m.ToLowerInvariant()).ToHashSet();
    }

    public static IEnumerable<SeasonalEvent> EntriesOf(IEnumerable<SeasonalEvent> events, SeasonalEventDef def) =>
        events.Where(e => def.SptNames.Contains(e.Name, StringComparer.OrdinalIgnoreCase));
}
