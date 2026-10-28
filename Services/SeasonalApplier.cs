using PPES.Events;
using PPES.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace PPES.Services;

public sealed record SeasonalWindow(int StartMonth, int StartDay, int EndMonth, int EndDay);

/// <summary>The stock calendar, captured before PPES changes it, so the page can tell what "auto" means today.</summary>
[Injectable(InjectionType.Singleton)]
public sealed class SeasonalCalendar
{
    private readonly Dictionary<string, List<SeasonalWindow>> _windows = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SeasonalWindow> WindowsOf(string id) => _windows.GetValueOrDefault(id) ?? [];

    public bool InSeason(string id, DateTime date) =>
        WindowsOf(id).Any(w => date.DateIsBetweenTwoDates(w.StartMonth, w.StartDay, w.EndMonth, w.EndDay));

    internal void Set(string id, List<SeasonalWindow> windows) => _windows[id] = windows;
}

/// <summary>
/// SPT picks the active seasonal events once, in GameCallbacks, from the dates in seasonalevents.json.
/// Running just before it, PPES switches events off or moves a forced one onto today.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.GameCallbacks - 1)]
public sealed class SeasonalApplier(
    ConfigService config,
    SeasonalCalendar calendar,
    SeasonalEventConfig seasonalConfig,
    ISptLogger<SeasonalApplier> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var settings = config.Saved;
        var today = DateTimeOffset.Now.DateTime;
        var events = seasonalConfig.Events ?? [];
        var summary = new List<string>();

        foreach (var def in EventCatalog.Seasonal)
        {
            var entries = events.Where(e => def.SptNames.Contains(e.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            calendar.Set(def.Id, entries.Where(e => e.Enabled).Select(e => new SeasonalWindow(e.StartMonth, e.StartDay, e.EndMonth, e.EndDay)).ToList());
            if (entries.Count == 0)
            {
                logger.Warning($"{ConfigService.LogPrefix} seasonalevents.json has no entry for {def.NameEn}, skipped");
                continue;
            }

            var mode = settings.ModeOf(def.Id);
            switch (mode)
            {
                case SeasonalMode.Off:
                    foreach (var e in entries) e.Enabled = false;
                    break;
                case SeasonalMode.On when !calendar.InSeason(def.Id, today):
                    var forced = entries[0];
                    forced.Enabled = true;
                    forced.StartMonth = forced.EndMonth = today.Month;
                    forced.StartDay = forced.EndDay = today.Day;
                    break;
            }

            var active = mode == SeasonalMode.On || (mode == SeasonalMode.Auto && calendar.InSeason(def.Id, today));
            summary.Add($"{def.NameEn} {(active ? "on" : "off")} ({mode})");
        }

        if (!seasonalConfig.EnableSeasonalEventDetection)
            logger.Info($"{ConfigService.LogPrefix} Seasonal event detection was off in SPT config, turned on so the selector can work");
        seasonalConfig.EnableSeasonalEventDetection = true;

        logger.Info($"{ConfigService.LogPrefix} Seasonal: {string.Join(", ", summary)}");
        return Task.CompletedTask;
    }
}
