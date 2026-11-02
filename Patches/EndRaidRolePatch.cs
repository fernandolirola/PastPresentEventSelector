using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarmonyLib;
using PPES.Factions;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace PPES.Patches;

/// <summary>
/// The client names custom bot roles in the end-of-raid profile (DeathCause.Role is the killer's role), but the
/// server's WildSpawnType enum has no such members and its deserializer rejects the whole request. PPES roles
/// are swapped for their ids; roles added by other client-only mods (BlackDiv, MoreBotsAPI) have no id known
/// here, so the field is dropped, which the nullable property accepts. Other role fields are plain strings.
/// </summary>
public sealed class EndRaidRolePatch : AbstractPatch
{
    private const string EndRaidUrl = "/client/match/local/end";

    protected override MethodBase? GetTargetMethod() =>
        AccessTools.Method(typeof(StaticRouter), nameof(StaticRouter.HandleStaticAsync));

    [PatchPrefix]
    public static void Prefix(string url, ref string? body)
    {
        if (string.IsNullOrEmpty(body) || url != EndRaidUrl) return;
        try
        {
            body = Fix(body);
        }
        catch (JsonException)
        {
        }
    }

    internal static string Fix(string body)
    {
        var root = JsonNode.Parse(body);
        var cause = root?["results"]?["profile"]?["Stats"]?["Eft"]?["DeathCause"] as JsonObject;
        if (cause is null || !cause.TryGetPropertyValue("Role", out var node) || node is not JsonValue value
            || !value.TryGetValue<string>(out var role)) return body;
        if (int.TryParse(role, out _) || Enum.TryParse<WildSpawnType>(role, true, out _)) return body;

        if (FactionCatalog.ByRole(role) is { } faction) cause["Role"] = faction.Id;
        else cause.Remove("Role");
        return root!.ToJsonString();
    }
}
