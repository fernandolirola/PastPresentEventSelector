using Microsoft.Extensions.DependencyInjection;
using PPES.Events;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace PPES.Factions;

/// <summary>
/// Finds the optional mods PPES events use and builds PPES's own factions.
/// </summary>
[Injectable(TypePriority = 180086)]
public sealed class FactionSetup(IServiceProvider services, BotTable botTable, ISptLogger<FactionSetup> logger) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        FactionCatalog.BlackDivision = botTable.Types.ContainsKey("bosswedge");
        Events.ModRequirements.ScourgeLoaded = botTable.Types.ContainsKey(Events.ModRequirements.ScourgeBoss.ToLowerInvariant());
        VultifyBlackout.Find(services, logger);

        try
        {
            await ActivatorUtilities.CreateInstance<PpesFactions>(services, logger).BuildAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error($"{ConfigService.LogPrefix} built-in factions could not be created: {ex.Message}");
        }
    }
}
