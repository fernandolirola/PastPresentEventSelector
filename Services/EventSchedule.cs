using System.Globalization;
using PPES.Events;
using PPES.Models;

namespace PPES.Services;

public sealed record RotationState(bool Active, IReadOnlyList<string> Picks, DateTime From, DateTime Until);

/// <summary>
/// Which past events are on at a given moment: the ones picked by hand plus the rotation.
/// The rotation only draws events that need no restart, so it can change while the server runs.
/// </summary>
public static class EventSchedule
{
    public static IEnumerable<PastEventDef> RotationPool => EventCatalog.PastEvents.Where(e => !e.NeedsRestart && EventCatalog.MissingRequirement(e) is null);

    public static RotationState Rotation(PpesConfig config, DateTime now)
    {
        var mode = config.Rotation.Mode;
        if (mode == RotationMode.Off) return new(false, [], now, now);

        DateTime from, until;
        bool active;
        if (mode == RotationMode.Weekly)
        {
            from = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            until = from.AddDays(7);
            active = true;
        }
        else
        {
            var daysToSaturday = ((int)DayOfWeek.Saturday - (int)now.DayOfWeek + 7) % 7;
            from = now.DayOfWeek == DayOfWeek.Sunday ? now.Date.AddDays(-1) : now.Date.AddDays(daysToSaturday);
            until = from.AddDays(2);
            active = now >= from && now < until;
        }

        var seed = ISOWeek.GetYear(from) * 100 + ISOWeek.GetWeekOfYear(from) + (mode == RotationMode.Weekend ? 5000 : 0);
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
        return new(active, picks, from, until);
    }

    /// <summary>Events whose raid edits apply right now.</summary>
    public static IReadOnlyList<string> RaidEvents(PpesConfig config, DateTime now)
    {
        var rotation = Rotation(config, now);
        return rotation.Active ? ConfigService.NormalizeEvents(config.Events.Concat(rotation.Picks)) : config.Events;
    }
}
