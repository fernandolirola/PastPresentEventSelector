using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PPES.Events;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Tables;
using MoreBotsLoadOrder = MoreBotsServer.MoreBotsLoadOrder;

namespace PPES.Factions;

/// <summary>
/// Finds the optional mods PPES events use and builds the PPES factions when MoreBotsAPI is there.
/// Runs right after MoreBotsAPI and the mods using it (Black Division, SCOURGE) have loaded their bots.
/// Nothing here names a MoreBotsAPI type, so the server can build this class without MoreBotsAPI installed.
/// </summary>
[Injectable(TypePriority = MoreBotsLoadOrder.LoadBots + 1)]
public sealed class FactionSetup(IServiceProvider services, BotTable botTable, ISptLogger<FactionSetup> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        FactionCatalog.BlackDivision = botTable.Types.ContainsKey("bosswedge");
        ModRequirements.ScourgeLoaded = botTable.Types.ContainsKey(ModRequirements.ScourgeBoss.ToLowerInvariant());

        if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "MoreBotsServer"))
        {
            logger.Warning($"{ConfigService.LogPrefix} MoreBotsAPI is not installed: the Cleanup Crew, Smugglers, Pillagers, Gus and Basmach are off");
            return Task.CompletedTask;
        }

        try
        {
            BuildFactions();
            ModRequirements.MoreBotsLoaded = true;
        }
        catch (Exception ex)
        {
            logger.Error($"{ConfigService.LogPrefix} factions could not be created with MoreBotsAPI: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    // kept out of line so the MoreBotsAPI types are only resolved when this runs
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void BuildFactions() => ActivatorUtilities.CreateInstance<MoreBotsFactions>(services, logger).Build();
}
