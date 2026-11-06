using System.Globalization;
using PPES.Events;
using PPES.Models;
using SPTarkov.DI.Annotations;

namespace PPES.Services;

/// <param name="RaidsLeft">Raids until the next draw in the "raids" mode, otherwise null.</param>
public sealed record RotationState(bool Active, IReadOnlyList<string> Picks, DateTime From, DateTime Until, int? RaidsLeft = null);

/// <param name="Pick">The seasonal event of this period, or null for a break (or rotation off).</param>
public sealed record SeasonalRotationState(bool Active, string? Pick, DateTime From, DateTime Until);

/// <summary>
/// Which past events are on at a given moment: the ones picked by hand plus the rotation.
/// The rotation only draws events that need no restart, so it can change while the server runs.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class EventSchedule(ConfigService config)
{
    private sealed class RaidState
    {
        public int Raids { get; set; }
    }

    private readonly Lock _lock = new();
    private StateFile<RaidState> State => new(Path.Combine(config.ModPath, "state.json"));
    private int? _raids;

    public static IEnumerable<PastEventDef> RotationPool => EventCatalog.PastEvents.Where(e => !e.NeedsRestart && e.MissingRequirement is null);

    public int Raids
    {
        get
        {
            lock (_lock) return _raids ??= Math.Max(0, State.Read().Raids);
        }
    }

    /// <summary>Counts a raid start and keeps the count on disk, so the "every N raids" draw survives restarts.</summary>
    public void CountRaid()
    {
        lock (_lock)
        {
            _raids = Raids + 1;
            try { State.Write(new RaidState { Raids = _raids.Value }); }
            catch { /* the count still works in memory */ }
        }
    }

    public RotationState Rotation(PpesConfig settings, DateTime now)
    {
        var mode = settings.Rotation.Mode;
        if (mode == RotationMode.Off) return new(false, [], now, now);

        DateTime from, until;
        var active = true;
        int seed;
        int? raidsLeft = null;
        switch (mode)
        {
            case RotationMode.Weekly:
                from = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
                until = from.AddDays(7);
                seed = ISOWeek.GetYear(from) * 100 + ISOWeek.GetWeekOfYear(from);
                break;
            case RotationMode.Weekend:
                var daysToSaturday = ((int)DayOfWeek.Saturday - (int)now.DayOfWeek + 7) % 7;
                from = now.DayOfWeek == DayOfWeek.Sunday ? now.Date.AddDays(-1) : now.Date.AddDays(daysToSaturday);
                until = from.AddDays(2);
                active = now >= from && now < until;
                seed = ISOWeek.GetYear(from) * 100 + ISOWeek.GetWeekOfYear(from) + 5000;
                break;
            case RotationMode.Daily:
                from = now.Date;
                until = from.AddDays(1);
                seed = from.Year * 1000 + from.DayOfYear + 7_000_000;
                break;
            case RotationMode.Hourly:
                from = now.Date.AddHours(now.Hour);
                until = from.AddHours(1);
                seed = (int)(from.Ticks / TimeSpan.TicksPerHour % 100_000_000) + 300_000_000;
                break;
            default:
                var raids = Raids;
                var every = settings.Rotation.Raids;
                from = until = now;
                raidsLeft = every - raids % every;
                seed = raids / every + 900_000_000;
                break;
        }

        var manual = settings.Events.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var manualGroups = settings.Events.Select(id => EventCatalog.Past(id)?.Group).Where(g => g is not null).ToHashSet();
        var pool = RotationPool.Where(e => !manual.Contains(e.Id) && (e.Group is null || !manualGroups.Contains(e.Group))).ToList();

        var random = new Random(seed);
        var picks = new List<string>();
        var pickedGroups = new HashSet<string>();
        foreach (var e in pool.OrderBy(_ => random.Next()))
        {
            if (picks.Count >= settings.Rotation.Count) break;
            if (e.Group is { } g && !pickedGroups.Add(g)) continue;
            picks.Add(e.Id);
        }
        return new(active, picks, from, until, raidsLeft);
    }

    /// <summary>Events whose raid edits apply right now: the picked ones plus the rotation's.</summary>
    public IReadOnlyList<string> RaidEvents(PpesConfig settings, DateTime now)
    {
        var rotation = Rotation(settings, now);
        return rotation.Active ? ConfigService.NormalizeEvents(settings.Events.Concat(rotation.Picks)) : settings.Events;
    }

    /// <summary>The past events the server runs now: restart-bound ones as started, raid ones as saved.</summary>
    public IReadOnlyList<string> ActiveNow(DateTime now) =>
        ConfigService.NormalizeEvents(config.Applied.Events.Where(id => EventCatalog.Past(id)?.NeedsRestart == true)
            .Concat(RaidEvents(config.Saved, now).Where(id => EventCatalog.Past(id)?.NeedsRestart == false)));
}

/// <summary>Which seasonal events are on for a date: the mode picked for each, or the seasonal rotation.</summary>
public static class SeasonalSchedule
{
    /// <summary>
    /// One random seasonal event per period, or none with the break chance. Events set to Off stay out of the draw.
    /// SPT only picks seasonal events at startup, so a new period needs a restart.
    /// </summary>
    public static SeasonalRotationState Rotation(PpesConfig config, DateTime date)
    {
        var mode = config.SeasonalRotation.Mode;
        if (mode == SeasonalRotationMode.Off) return new(false, null, date, date);
        DateTime from, until;
        int seed;
        switch (mode)
        {
            case SeasonalRotationMode.Daily:
                from = date.Date;
                until = from.AddDays(1);
                seed = from.Year * 1000 + from.DayOfYear + 11_000_000;
                break;
            case SeasonalRotationMode.Monthly:
                from = new DateTime(date.Year, date.Month, 1);
                until = from.AddMonths(1);
                seed = from.Year * 100 + from.Month + 13_000_000;
                break;
            default:
                from = date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
                until = from.AddDays(7);
                seed = ISOWeek.GetYear(from) * 100 + ISOWeek.GetWeekOfYear(from) + 12_000_000;
                break;
        }
        var pool = EventCatalog.Seasonal.Where(s => config.ModeOf(s.Id) != SeasonalMode.Off).Select(s => s.Id).ToList();
        var random = new Random(seed);
        var rest = random.Next(100) < config.SeasonalRotation.BreakChance;
        return new(true, rest || pool.Count == 0 ? null : pool[random.Next(pool.Count)], from, until);
    }

    /// <summary>The mode a seasonal event effectively has on a date, with the seasonal rotation taken into account.</summary>
    public static string ModeOn(PpesConfig config, string id, DateTime date)
    {
        var rotation = Rotation(config, date);
        if (!rotation.Active) return config.ModeOf(id);
        return string.Equals(rotation.Pick, id, StringComparison.OrdinalIgnoreCase) ? SeasonalMode.On : SeasonalMode.Off;
    }

    public static bool Active(PpesConfig config, SeasonalCalendar calendar, string id, DateTime date) => ModeOn(config, id, date) switch
    {
        SeasonalMode.On => true,
        SeasonalMode.Off => false,
        _ => calendar.InSeason(id, date),
    };
}
