using PPES.Events;
using PPES.Models;

namespace PPES.Services;

/// <summary>What differs between the saved selection and the one the server started with that only a restart applies.</summary>
public static class RestartCheck
{
    /// <param name="startedOn">The day the server started; the applied selection's seasonal events were picked for it.</param>
    public static IReadOnlyList<(string En, string Pt)> Pending(PpesConfig saved, PpesConfig applied, SeasonalCalendar calendar, DateTime startedOn, DateTime today)
    {
        bool SavedOn(string id) => SeasonalSchedule.Active(saved, calendar, id, today);
        var names = EventCatalog.Seasonal
            .Where(s => SavedOn(s.Id) != SeasonalSchedule.Active(applied, calendar, s.Id, startedOn))
            .Select(s => (s.NameEn, s.NamePt))
            .ToList();
        if (SavedOn(SeasonalIds.Halloween) && !Settings.Same(saved.Halloween, applied.Halloween, Applies.Restart))
            names.Add(("Halloween parts", "Partes do Halloween"));
        if ((SavedOn(SeasonalIds.Christmas) || SavedOn(SeasonalIds.NewYears)) && !Settings.Same(saved.Christmas, applied.Christmas, Applies.Restart))
            names.Add(("Christmas and New Year parts", "Partes do Natal e Ano Novo"));
        if (SavedOn(SeasonalIds.AprilFools) && !Settings.Same(saved.AprilFools, applied.AprilFools, Applies.Restart))
            names.Add(("April Fools parts", "Partes do 1\u00ba de abril"));
        if (!Settings.Same(saved.Mechanics, applied.Mechanics, Applies.Restart))
            names.Add(("Ritual and infection effects", "Efeitos do ritual e da infec\u00e7\u00e3o"));
        if (!Settings.Same(saved.SeasonalRotation, applied.SeasonalRotation, Applies.Restart))
            names.Add(("Seasonal rotation", "Rota\u00e7\u00e3o dos sazonais"));
        names.AddRange(EventCatalog.PastEvents
            .Where(e => e.NeedsRestart && saved.HasEvent(e.Id) != applied.HasEvent(e.Id))
            .Select(e => (e.Name, e.Name)));
        return names;
    }
}
