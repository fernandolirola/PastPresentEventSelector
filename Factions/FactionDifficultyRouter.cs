using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace PPES.Factions;

[Injectable(InjectionType.Transient, TypePriority = 400031)]
public sealed class FactionDifficultyRouter : DynamicRouter
{
    public FactionDifficultyRouter(JsonUtil json, HttpResponseUtil response, BotTable bots, ISptLogger<FactionDifficultyRouter> logger)
        : base(json, Routes(json, response, bots, logger))
    {
    }

    private static List<RouteAction> Routes(JsonUtil json, HttpResponseUtil response, BotTable bots, ISptLogger<FactionDifficultyRouter> logger) =>
    [
        new RouteAction("/singleplayer/settings/bot/difficulties",
            (url, request, sessionId, output, cancellationToken) =>
            {
                var difficulties = string.IsNullOrWhiteSpace(output)
                    ? []
                    : json.Deserialize<Dictionary<string, Dictionary<string, DifficultyCategories>>>(output) ?? [];
                var added = 0;
                foreach (var def in FactionCatalog.All.Where(d => FactionCatalog.IsReady(d.Role)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!bots.Types.TryGetValue(def.Role.ToLowerInvariant(), out var type) || type?.BotDifficulty is null) continue;
                    difficulties[def.Role.ToLowerInvariant()] = type.BotDifficulty;
                    added++;
                }
                if (added != FactionCatalog.All.Count)
                    logger.Warning($"{ConfigService.LogPrefix} added difficulty data for {added}/{FactionCatalog.All.Count} built-in factions");
                return new ValueTask<object>(response.NoBody(difficulties));
            })
    ];
}
