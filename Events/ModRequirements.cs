namespace PPES.Events;

/// <summary>A mod some events need. Installed is read after the server loaded, so it reflects this run.</summary>
public sealed record ModRequirement(string Id, string Name, string ForgeUrl, Func<bool> Installed);

/// <param name="LostEn">What the event loses without the mod; null when the whole event needs it.</param>
public sealed record ModNeed(string Mod, string? LostEn = null, string? LostPt = null)
{
    public bool Partial => LostEn is not null;
}

public static class ModRequirements
{
    public const string BlackDivision = "blackdiv";
    public const string Scourge = "scourge";
    public const string Blackout = "vultifyblackout";

    /// <summary>Set at load: the SCOURGE mod's boss is in the bot table.</summary>
    public static bool ScourgeLoaded { get; internal set; }

    public const string ScourgeBoss = "bossScourgeEventLab";

    public static readonly IReadOnlyList<ModRequirement> All =
    [
        new(BlackDivision, "Black Division", "https://sp-mod.com/mod/2511/wtt-black-division-redacted-home", () => Factions.FactionCatalog.BlackDivision),
        new(Scourge, "SCOURGE", "https://sp-mod.com/mod/3196/scourge", () => ScourgeLoaded),
        new(Blackout, "Blackout (Vultify)", "https://sp-mod.com/mod/2843/blackout", () => VultifyBlackout.Loaded),
    ];

    public static ModRequirement Get(string id) => All.First(r => r.Id == id);

    public static bool IsInstalled(string id) => Get(id).Installed();
}
