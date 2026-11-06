using Microsoft.AspNetCore.Components;
using PPES.Events;
using PPES.Models;
using PPES.Services;

namespace PPES.Web;

/// <summary>Language and date helpers every part of the page uses.</summary>
public abstract class PpesComponent : ComponentBase
{
    private static readonly string[] MonthsEn = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    private static readonly string[] MonthsPt = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    [Inject] protected ConfigService Config { get; set; } = null!;

    protected bool IsPt => Config.Saved.Language == "pt";
    protected string T(string pt, string en) => IsPt ? pt : en;
    protected static DateTime Now => DateTimeOffset.Now.DateTime;
    protected static DateTime Today => Now.Date;

    protected string FormatDate(DateTime date) => IsPt ? $"{date.Day} {MonthsPt[date.Month - 1]}" : $"{MonthsEn[date.Month - 1]} {date.Day}";

    /// <summary>An event's "When" label, with month names in the page's language.</summary>
    protected string WhenText(string when)
    {
        if (!IsPt) return when;
        var text = when.Replace(" to ", " a ");
        for (var i = 0; i < 12; i++) text = text.Replace(MonthsEn[i], MonthsPt[i]);
        return text;
    }

    protected string Status(bool active) => active ? T("Ativo", "Active") : T("Desligado", "Off");
}

/// <summary>A section of the page: works on the shared draft and can send the user to another section.</summary>
public abstract class PpesSection : PpesComponent
{
    [Inject] protected SeasonalCalendar Calendar { get; set; } = null!;

    [Parameter, EditorRequired] public Draft Draft { get; set; } = null!;

    /// <summary>Opens a section, optionally with a tab or item after a slash ("settings/presets").</summary>
    [Parameter] public EventCallback<string> Open { get; set; }

    protected PpesConfig Working => Draft.Working;

    protected bool IsSeasonalActive(string id) => SeasonalSchedule.Active(Working, Calendar, id, Today);

    protected string SeasonalName(string id) => EventCatalog.SeasonalById(id) is { } s ? T(s.NamePt, s.NameEn) : id;
}
