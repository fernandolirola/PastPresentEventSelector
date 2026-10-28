namespace PPES.Factions;

/// <param name="Sources">Bot types to copy gear, health and AI from, first one found wins.</param>
public sealed record FactionDef(
    int Id,
    string Role,
    string ScavRole,
    string Faction,
    string[] Sources,
    string NameEn,
    string NamePt,
    string DescEn);

/// <summary>
/// The factions PPES adds through MoreBotsAPI. Ids, roles and scav roles must match client/Prepatch/FactionTypesPatch.cs,
/// since the game only knows a bot type the prepatcher registered.
/// </summary>
public static class FactionCatalog
{
    public const string CleanupCrew = "ppesCleanupCrew";
    public const string Smuggler = "ppesSmuggler";
    public const string Pillager = "ppesPillager";

    public static readonly IReadOnlyList<FactionDef> All =
    [
        new(731001, CleanupCrew, "PpesCleanupCrew", "ppes_cleanupcrew", ["arenafighterevent", "pmcbot"],
            "Cleanup Crew", "Cleanup Crew", "Ref's arena fighters sent to clean up a location."),
        new(731002, Smuggler, "PpesSmuggler", "ppes_smugglers", ["exusec", "pmcbot"],
            "Smugglers", "Contrabandistas", "Armed smugglers moving goods across the region."),
        new(731003, Pillager, "PpesPillager", "ppes_pillagers", ["pmcbot"],
            "Pillagers", "Saqueadores", "Well-armed raiders looting the locations."),
    ];

    private static readonly HashSet<string> Ready = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True once the server side of the faction loaded, so events never spawn a type the server cannot build.</summary>
    public static bool IsReady(string role) => Ready.Contains(role);

    internal static void MarkReady(string role) => Ready.Add(role);

    /// <summary>Black Division (BlackDivServer) is installed; the Blackout event needs Wedge and his men.</summary>
    public static bool BlackDivision { get; internal set; }

    public static FactionDef? ByRole(string role) =>
        All.FirstOrDefault(f => string.Equals(f.Role, role, StringComparison.OrdinalIgnoreCase));
}
