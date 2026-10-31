using System.Globalization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace PPES.Services;

/// <summary>Settings only the client can apply; the plugin reads them when it loads.</summary>
[Injectable]
public sealed class MechanicsRouter(JsonUtil jsonUtil, ConfigService config) : StaticRouter(jsonUtil,
[
    new RouteAction<EmptyRequestData>("/ppes/mechanics", (_, _, _, _, _) =>
        new ValueTask<string>(string.Create(CultureInfo.InvariantCulture,
            $"{{\"kollontayJam\":{config.Saved.Mechanics.KollontayJam},\"vengefulJam\":{config.Saved.Mechanics.VengefulJam}}}"))),
]);
