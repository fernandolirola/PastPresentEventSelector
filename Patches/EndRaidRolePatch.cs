using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using PPES.Factions;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;

namespace PPES.Patches;

/// <summary>
/// The client names PPES's faction roles in the end-of-raid profile (DeathCause.Role is the killer's role),
/// but the server's WildSpawnType enum has no such members and its deserializer rejects the whole request.
/// Numbers are accepted for unknown values, so the names are swapped for their ids before parsing.
/// </summary>
public sealed class EndRaidRolePatch : AbstractPatch
{
    private const string EndRaidUrl = "/client/match/local/end";

    private static readonly Regex RoleName = new(
        "\"(" + string.Join("|", FactionCatalog.All.Select(f => Regex.Escape(f.Role))) + ")\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected override MethodBase? GetTargetMethod() =>
        AccessTools.Method(typeof(StaticRouter), nameof(StaticRouter.HandleStaticAsync));

    [PatchPrefix]
    public static void Prefix(string url, ref string? body)
    {
        if (string.IsNullOrEmpty(body) || url != EndRaidUrl) return;
        body = RoleName.Replace(body, match =>
            FactionCatalog.ByRole(match.Groups[1].Value)?.Id.ToString() ?? match.Value);
    }
}
