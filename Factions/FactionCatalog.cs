namespace PPES.Factions;

/// <param name="Sources">Bot types to copy gear, health and AI from, first one found wins.</param>
internal sealed record FactionDef(
    FactionRole Key,
    string Faction,
    string[] Sources,
    string NameEn,
    string NamePt,
    string DescEn)
{
    public int Id => Key.Id;
    public string Role => Key.Role;
    public string ScavRole => Key.ScavRole;
}

/// <summary>PPES's built-in factions; their ids and roles come from shared/PpesIds.cs, which the client compiles too.</summary>
internal static class FactionCatalog
{
    public const string CleanupCrew = PpesIds.CleanupCrew;
    public const string Smuggler = PpesIds.Smuggler;
    public const string Pillager = PpesIds.Pillager;
    public const string Gus = PpesIds.Gus;
    public const string Basmach = PpesIds.Basmach;

    private static readonly Dictionary<string, (string Faction, string[] Sources, string NameEn, string NamePt, string DescEn)> Details = new()
    {
        [CleanupCrew] = ("ppes_cleanupcrew", ["arenafighterevent", "pmcbot"], "Cleanup Crew", "Cleanup Crew", "Ref's arena fighters sent to clean up a location."),
        [Smuggler] = ("ppes_smugglers", ["exusec", "pmcbot"], "Smugglers", "Contrabandistas", "Armed smugglers moving goods across the region."),
        [Pillager] = ("ppes_pillagers", ["pmcbot"], "Pillagers", "Saqueadores", "Well-armed raiders looting the locations."),
        // the two arms dealers of the 2024 Arms Race event, each with Kolontay's guards
        [Gus] = ("ppes_gus", ["followerkolontaysecurity", "pmcbot"], "Gus", "Gus", "An arms dealer selling Western weapons on Streets."),
        [Basmach] = ("ppes_basmach", ["followerkolontaysecurity", "pmcbot"], "Basmach", "Basmach", "An arms dealer selling Russian weapons on Streets."),
    };

    public static readonly IReadOnlyList<FactionDef> All = PpesIds.Factions
        .Select(f => Details[f.Role] is var d ? new FactionDef(f, d.Faction, d.Sources, d.NameEn, d.NamePt, d.DescEn) : null!)
        .ToList();

    private static readonly HashSet<string> Ready = new(StringComparer.OrdinalIgnoreCase);

    internal static void MarkReady(string role) => Ready.Add(role);

    public static FactionDef? ByRole(string role) =>
        All.FirstOrDefault(f => string.Equals(f.Role, role, StringComparison.OrdinalIgnoreCase));

    /// <summary>False only for a PPES faction whose server side did not load, so nothing spawns a type the server cannot build.</summary>
    public static bool CanSpawn(string role) => ByRole(role) is null || Ready.Contains(role);
}
