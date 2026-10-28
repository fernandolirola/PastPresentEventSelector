using PPES.Events;
using PPES.Patches;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Services;

/// <summary>Applies the load-time part of the picked past events. Runs late so SVM and other tweakers go first.</summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 5000000)]
public sealed class EventApplier(
    ConfigService config,
    GlobalTable globals,
    TradersTable traders,
    HideoutTable hideout,
    LocationTable locations,
    QuestConfig questConfig,
    AirdropConfig airdropConfig,
    TraderMessageService traderMessages,
    SeasonalBossRegistry seasonalBosses,
    SeasonalEventConfig seasonalConfig,
    ICloner cloner,
    ISptLogger<EventApplier> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        config.MarkApplied();
        var picked = config.Applied.Events;
        var context = new LoadContext(globals, traders, hideout, locations, questConfig, airdropConfig, logger);
        var applied = new List<string>();

        foreach (var id in picked)
        {
            if (EventCatalog.Past(id) is not { } def) continue;
            try
            {
                def.OnLoad?.Invoke(context);
                foreach (var quest in def.Quests)
                    questConfig.EventQuests?.Remove(new MongoId(quest));
                applied.Add(def.Name);
            }
            catch (Exception ex)
            {
                logger.Error($"{ConfigService.LogPrefix} {def.Name} could not be applied: {ex.Message}");
            }
        }

        if (seasonalBosses.DisableSummonEvent && globals.Configuration?.EventSettings is { } eventSettings)
            eventSettings.EventActive = false;

        RaidLocationPatch.Init(config, locations, cloner, logger, seasonalBosses, seasonalConfig);
        new RaidLocationPatch().Enable();
        GameStartPatch.Init(traderMessages);
        new GameStartPatch().Enable();

        logger.Success(applied.Count == 0
            ? $"{ConfigService.LogPrefix} No past events picked"
            : $"{ConfigService.LogPrefix} Past events: {string.Join(", ", applied)}");
        return Task.CompletedTask;
    }
}
