using System.Globalization;
using PPES.Events;
using PPES.Models;

namespace PPES.Services;

/// <param name="RaidsLeft">Raids until the next draw in the "raids" mode, otherwise null.</param>
public sealed record RotationState(bool Active, IReadOnlyList<string> Picks, DateTime From, DateTime Until, int? RaidsLeft = null);

/// <param name="Pick">The seasonal event of this period, or null for a break week (or rotation off).</param>
public sealed record SeasonalRotationState(bool Active, string? Pick, DateTime From, DateTime Until);

/// <summary>
/// Which past events are on at a given moment: the ones picked by hand plus the rotation.
/// The rotation only draws events that need no restart, so it can change while the server runs.
/// </summary>
public static class EventSchedule
{
    private static readonly Lock RaidLock = new();
    private static int? _raids;

    public static IEnumerable<PastEventDef> RotationPool => EventCatalog.PastEvents.Where(e => !e.NeedsRestart && EventCatalog.MissingRequirement(e) is null);

    public static RotationState Rotation(PpesConfig config, DateTime now)
    {
        var mode = config.Rotation.Mode;
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
                var raids = Raids();
                var every = config.Rotation.Raids;
                from = until = now;
                raidsLeft = every - raids % every;
                seed = raids / every + 900_000_000;
                break;
        }

        var manual = config.Events.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var manualGroups = config.Events.Select(id => EventCatalog.Past(id)?.Group).Where(g => g is not null).ToHashSet();
        var pool = RotationPool.Where(e => !manual.Contains(e.Id) && (e.Group is null || !manualGroups.Contains(e.Group))).ToList();

        var random = new Random(seed);
        var picks = new List<string>();
        var pickedGroups = new HashSet<string>();
        foreach (var e in pool.OrderBy(_ => random.Next()))
        {
            if (picks.Count >= config.Rotation.Count) break;
            if (e.Group is { } g && !pickedGroups.Add(g)) continue;
            picks.Add(e.Id);
        }
        return new(active, picks, from, until, raidsLeft);
    }

    /// <summary>Events whose raid edits apply right now.</summary>
    public static IReadOnlyList<string> RaidEvents(PpesConfig config, DateTime now)
    {
        var rotation = Rotation(config, now);
        return rotation.Active ? ConfigService.NormalizeEvents(config.Events.Concat(rotation.Picks)) : config.Events;
    }

    /// <summary>
    /// The seasonal rotation: one random seasonal event per period, or none with the break chance. Events set to Off
    /// stay out of the draw. SPT only picks seasonal events at startup, so a new period needs a restart.
    /// </summary>
    public static SeasonalRotationState SeasonalRotation(PpesConfig config, DateTime date)
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
    public static string SeasonalModeOn(PpesConfig config, string id, DateTime date)
    {
        var rotation = SeasonalRotation(config, date);
        if (!rotation.Active) return config.ModeOf(id);
        return string.Equals(rotation.Pick, id, StringComparison.OrdinalIgnoreCase) ? SeasonalMode.On : SeasonalMode.Off;
    }

    public static bool SeasonalActive(PpesConfig config, SeasonalCalendar calendar, string id, DateTime date) => SeasonalModeOn(config, id, date) switch
    {
        SeasonalMode.On => true,
        SeasonalMode.Off => false,
        _ => calendar.InSeason(id, date),
    };

    public static int Raids() => _raids ?? 0;

    /// <summary>Counts a raid start and keeps the count on disk, so the "every N raids" draw survives restarts.</summary>
    internal static void CountRaid(ConfigService service)
    {
        lock (RaidLock)
        {
            var path = Path.Combine(service.ModPath, "state.json");
            _raids ??= ReadRaids(path);
            _raids++;
            try { File.WriteAllText(path, $"{{ \"raids\": {_raids} }}"); }
            catch { /* the count still works in memory */ }
        }
    }

    internal static void LoadRaids(ConfigService service)
    {
        lock (RaidLock) _raids ??= ReadRaids(Path.Combine(service.ModPath, "state.json"));
    }

    private static int ReadRaids(string path)
    {
        try
        {
            if (!File.Exists(path)) return 0;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("raids", out var r) && r.TryGetInt32(out var n) ? Math.Max(0, n) : 0;
        }
        catch
        {
            return 0;
        }
    }
}
