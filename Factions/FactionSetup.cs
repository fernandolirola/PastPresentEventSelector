using Microsoft.Extensions.DependencyInjection;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace PPES.Factions;

[Injectable(TypePriority = 180086)]
public sealed class FactionSetup(IServiceProvider services, ISptLogger<FactionSetup> logger) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
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
