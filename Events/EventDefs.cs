using System.Text.RegularExpressions;
using PPES.Quests;

namespace PPES.Events;

public enum EventTag
{
    /// <summary>SPT already has everything; PPES only switches it on.</summary>
    Ready,
    /// <summary>Recreated with server data (spawns, globals, traders).</summary>
    Simulated,
    /// <summary>Dormant game code switched back on; may not work on every map.</summary>
    Experimental,
}

[Flags]
public enum EventKind
{
    None = 0,
    Bosses = 1,
    Quests = 2,
    Traders = 4,
    Flea = 8,
    Airdrops = 16,
    Hideout = 32,
    Rules = 64,
    Access = 128,
    Hazard = 256,
}

/// <param name="SptNames">Entry names in SPT's seasonalevents.json.</param>
/// <param name="GatedQuests">Quests SPT shows only while the event is on.</param>
/// <param name="RecreatedQuests">Event key of the quests PPES recreated for this event (tools/questgen).</param>
public sealed record SeasonalEventDef(
    string Id,
    string NameEn,
    string NamePt,
    string[] SptNames,
    string DescEn,
    string DescPt,
    string RumorEn,
    string RumorPt,
    string? Wiki = null,
    string[]? GatedQuests = null,
    string? RecreatedQuests = null)
{
    public string[] GatedQuests { get; init; } = GatedQuests ?? [];
}

/// <param name="Gated">SPT hides this quest outside the event, so it is the one PPES unlocks; the rest of the chain follows from it.</param>
public sealed record QuestLink(string Name, string Url, bool Gated);

/// <param name="Maps">Maps the event changes, for the page's filters and chips.</param>
/// <param name="Touches">What the event changes, so the page can point out two events changing the same thing.</param>
/// <param name="RumorEn">Line a trader sends at login while the event is on.</param>
/// <param name="FreeAccess">Maps whose keycard requirement the event removes at load.</param>
/// <param name="Mortar">Maps the event turns the mortar barrages on for, at load.</param>
public sealed record PastEventDef(
    string Id,
    string Name,
    string When,
    EventTag Tag,
    string DescEn,
    string DescPt,
    string[] QuestIds,
    Action<LoadContext>? OnLoad = null,
    Action<RaidContext>? OnRaid = null,
    string? Group = null,
    ModNeed? Needs = null,
    ModNeed? AlsoNeeds = null,
    string[]? Maps = null,
    EventKind Kinds = EventKind.None,
    string[]? Touches = null,
    string RumorEn = "",
    string RumorPt = "",
    string? Wiki = null,
    string[]? FreeAccess = null,
    string[]? Mortar = null)
{
    public string[] Maps { get; init; } = Maps ?? [];
    public string[] Touches { get; init; } = Touches ?? [];
    public string[] FreeAccess { get; init; } = FreeAccess ?? [];
    public string[] Mortar { get; init; } = Mortar ?? [];

    public IEnumerable<ModNeed> AllNeeds => new[] { Needs, AlsoNeeds }.OfType<ModNeed>();

    /// <summary>The mod the event needs as a whole that is not installed, or null when it can run.</summary>
    public ModRequirement? MissingRequirement =>
        AllNeeds.Where(n => !n.Partial && !n.Requirement.Installed).Select(n => n.Requirement).FirstOrDefault();

    /// <summary>The event's quests: the game's ones it unlocks plus the ones PPES recreated for it and could load.</summary>
    public string[] Quests => [.. QuestIds, .. QuestCatalog.LoadedFor(Id)];

    /// <summary>Load-time edits and quest unlocks only take effect on server start; raid edits apply from the next raid.</summary>
    public bool NeedsRestart => OnLoad is not null || Quests.Length > 0 || FreeAccess.Length > 0 || Mortar.Length > 0;

    public EventKind AllKinds =>
        Kinds | (Quests.Length > 0 || WikiEventQuests.ByEvent.ContainsKey(Id) ? EventKind.Quests : EventKind.None);

    /// <summary>Latest year in the "When" label, used for the timeline.</summary>
    public int Year => YearPattern.Matches(When).Select(m => int.Parse(m.Value)).DefaultIfEmpty(0).Max();

    private static readonly Regex YearPattern = new(@"\b20\d{2}\b", RegexOptions.Compiled);
}

public sealed record BuiltInPreset(string Id, string NameEn, string NamePt, string DescEn, string DescPt, string[] Events, bool ResetSeasonal = false);
