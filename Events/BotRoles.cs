using PPES.Factions;

namespace PPES.Events;

public sealed record RoleOption(string Role, string NameEn, string NamePt);

/// <summary>Display names of the spawn roles the preview lists, and the ones its editor can add.</summary>
public static class BotRoles
{
    public const string Wedge = "bossWedge";
    public const string BlackDivAssault = "blackDivAssault";

    private static readonly Dictionary<string, (string En, string Pt)> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bossBully"] = ("Reshala", "Reshala"),
        ["bossKojaniy"] = ("Shturman", "Shturman"),
        ["bossSanitar"] = ("Sanitar", "Sanitar"),
        ["bossGluhar"] = ("Glukhar", "Glukhar"),
        ["bossKilla"] = ("Killa", "Killa"),
        ["bossTagilla"] = ("Tagilla", "Tagilla"),
        ["bossKolontay"] = ("Kollontay", "Kollontay"),
        ["bossBoar"] = ("Kaban", "Kaban"),
        ["bossBoarSniper"] = ("Kaban's snipers", "Snipers do Kaban"),
        ["bossKnight"] = ("The Goons", "Os Goons"),
        ["bossPartisan"] = ("Partisan", "Partisan"),
        ["bossZryachiy"] = ("Zryachiy", "Zryachiy"),
        ["sectantPriest"] = ("Cultists", "Cultistas"),
        ["arenaFighterEvent"] = ("Bloodhounds", "Bloodhounds"),
        ["exUsec"] = ("Rogues", "Rogues"),
        ["pmcBot"] = ("Raiders", "Raiders"),
        ["skier"] = ("Skier's troops", "Tropas do Skier"),
        ["peacemaker"] = ("Peacekeeper's troops", "Tropas do Peacekeeper"),
        ["gifter"] = ("Santa", "Papai Noel"),
        ["peacefullZryachiyEvent"] = ("Zryachiy (ritual)", "Zryachiy (ritual)"),
        ["sectactPriestEvent"] = ("Ritual priests", "Sacerdotes do ritual"),
        ["ravangeZryachiyEvent"] = ("Vengeful Zryachiy", "Zryachiy Vingativo"),
        ["sectantOni"] = ("Oni", "Oni"),
        ["sectantPredvestnik"] = ("Harbinger", "Harbinger"),
        ["sectantPrizrak"] = ("Ghost", "Ghost"),
        ["infectedTagilla"] = ("Infected Tagilla", "Tagilla infectado"),
        ["infectedAssault"] = ("Infected", "Infectados"),
        ["infectedPmc"] = ("Infected PMCs", "PMCs infectados"),
        ["infectedCivil"] = ("Infected civilians", "Civis infectados"),
        ["infectedLaborant"] = ("Infected lab staff", "Laboratoristas infectados"),
        [Wedge] = ("Wedge", "Wedge"),
        [BlackDivAssault] = ("Black Division", "Black Division"),
    };

    // trigger-only and event-scripted roles (ritual, Santa's helpers) are left out
    private static readonly string[] Addable =
    [
        "bossBully", "bossKojaniy", "bossSanitar", "bossGluhar", "bossKilla", "bossTagilla", "bossKolontay", "bossBoar", "bossKnight", "bossPartisan",
        "bossZryachiy", "sectantPriest", "sectantOni", "sectantPredvestnik", "sectantPrizrak", "arenaFighterEvent", "exUsec", "pmcBot",
        "skier", "peacemaker", "gifter", "infectedTagilla", "infectedAssault", "infectedPmc", "infectedCivil", "infectedLaborant",
        .. PpesIds.Factions.Select(f => f.Role), Wedge, BlackDivAssault,
    ];

    private static readonly HashSet<string> BlackDivisionRoles = new(StringComparer.OrdinalIgnoreCase) { Wedge, BlackDivAssault };

    /// <summary>Plain Scavs and PMCs; the preview leaves them out.</summary>
    public static readonly IReadOnlySet<string> Ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "assault", "marksman", "pmcUSEC", "pmcBEAR", "cursedAssault", "assaultGroup", "crazyAssaultEvent",
    };

    public static (string En, string Pt) NameOf(string role) =>
        FactionCatalog.ByRole(role) is { } faction ? (faction.NameEn, faction.NamePt) : Names.GetValueOrDefault(role, (role, role));

    /// <summary>Roles the editor can add: the bots this server can build.</summary>
    public static IReadOnlyList<RoleOption> AddableRoles() =>
        Addable
            .Where(r => BlackDivisionRoles.Contains(r) ? ModRequirements.IsInstalled(ModRequirements.BlackDivision) : FactionCatalog.CanSpawn(r))
            .Select(r => NameOf(r) is var (en, pt) ? new RoleOption(r, en, pt) : null!)
            .ToList();
}
