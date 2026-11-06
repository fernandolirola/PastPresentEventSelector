using PPES.Factions;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace PPES.Events;

internal sealed record ModProbe(BotTable Bots, IServiceProvider Services, ISptLogger<ModDetection> Logger);

public sealed record ModRequirement(string Id, string Name, string ForgeUrl)
{
    internal Func<ModProbe, bool> Detect { get; init; } = _ => false;
    public bool Installed => ModRequirements.IsInstalled(Id);
}

/// <param name="LostEn">What the event loses without the mod; null when the whole event needs it.</param>
public sealed record ModNeed(string Mod, string? LostEn = null, string? LostPt = null)
{
    public bool Partial => LostEn is not null;
    public ModRequirement Requirement => ModRequirements.Get(Mod);
}

public static class ModRequirements
{
    public const string BlackDivision = "blackdiv";
    public const string Scourge = "scourge";
    public const string Blackout = "vultifyblackout";
    public const string ScourgeBoss = "bossScourgeEventLab";

    public static readonly IReadOnlyList<ModRequirement> All =
    [
        new(BlackDivision, "Black Division", "https://sp-mod.com/mod/2511/wtt-black-division-redacted-home") { Detect = p => p.Bots.Types.ContainsKey(BotRoles.Wedge.ToLowerInvariant()) },
        new(Scourge, "SCOURGE", "https://sp-mod.com/mod/3196/scourge") { Detect = p => p.Bots.Types.ContainsKey(ScourgeBoss.ToLowerInvariant()) },
        new(Blackout, "Blackout (Vultify)", "https://sp-mod.com/mod/2843/blackout") { Detect = p => VultifyBlackout.Link(p.Services, p.Logger) },
    ];

    private static IReadOnlySet<string> _installed = new HashSet<string>();

    public static ModRequirement Get(string id) => All.First(r => r.Id == id);

    public static bool IsInstalled(string id) => _installed.Contains(id);

    /// <summary>A quest's or a role's need: one of these mods, or a PPES faction whose server side loaded.</summary>
    public static bool Has(string need) =>
        All.Any(r => r.Id == need) ? IsInstalled(need) : FactionCatalog.CanSpawn(need);

    internal static void Detect(ModProbe probe) =>
        _installed = All.Where(r => r.Detect(probe)).Select(r => r.Id).ToHashSet();
}

/// <summary>Runs as late as it can, after other mods have loaded their bots, and right before the event quests read which mods are installed.</summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 4998999)]
public sealed class ModDetection(BotTable bots, IServiceProvider services, ISptLogger<ModDetection> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        ModRequirements.Detect(new ModProbe(bots, services, logger));
        var found = ModRequirements.All.Where(r => r.Installed).Select(r => r.Name).ToList();
        if (found.Count > 0) logger.Info($"{ConfigService.LogPrefix} Mods used by events: {string.Join(", ", found)}");
        return Task.CompletedTask;
    }
}
