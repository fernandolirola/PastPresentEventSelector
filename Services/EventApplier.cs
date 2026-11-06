using PPES.Events;
using PPES.Models;
using Effects = PPES.Events.Effects;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace PPES.Services;

/// <summary>Applies the load-time part of the picked past events. Runs late so SVM and other tweakers go first.</summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 5000000)]
public sealed class EventApplier(
    ConfigService config,
    SeasonalCalendar calendar,
    GlobalTable globals,
    TradersTable traders,
    HideoutTable hideout,
    LocationTable locations,
    QuestConfig questConfig,
    AirdropConfig airdropConfig,
    LocationConfig locationConfig,
    TemplateTable templates,
    ISptLogger<EventApplier> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var context = new LoadContext(globals, traders, hideout, locations, questConfig, airdropConfig, locationConfig, logger);
        var applied = new List<string>();

        foreach (var id in config.Applied.Events)
        {
            if (EventCatalog.Past(id) is not { } def) continue;
            if (def.MissingRequirement is { } missing)
            {
                logger.Warning($"{ConfigService.LogPrefix} {def.Name} needs {missing.Name}, skipped");
                continue;
            }
            try
            {
                def.OnLoad?.Invoke(context);
                if (def.FreeAccess.Length > 0) Effects.RemoveAccessKeys(context, def.FreeAccess);
                if (def.Mortar.Length > 0) Effects.EnableMortarStrike(context, def.Mortar);
                foreach (var quest in def.Quests)
                {
                    questConfig.EventQuests?.Remove(new MongoId(quest));
                    RemoveRetiredLevelLock(quest);
                }
                applied.Add(def.Name);
            }
            catch (Exception ex)
            {
                logger.Error($"{ConfigService.LogPrefix} {def.Name} could not be applied: {ex.Message}");
            }
        }

        if (SeasonalState.Started(config, calendar).DisableSummonEvent && globals.Configuration?.EventSettings is { } eventSettings)
            eventSettings.EventActive = false;

        ApplyMechanics(config.Applied.Mechanics);

        logger.Success(applied.Count == 0
            ? $"{ConfigService.LogPrefix} No past events picked"
            : $"{ConfigService.LogPrefix} Past events: {string.Join(", ", applied)}");
        return Task.CompletedTask;
    }

    /// <summary>The game retires some event quests (Import Control) with a "level 99" start condition instead of the event list.</summary>
    private void RemoveRetiredLevelLock(string questId)
    {
        if (templates.Quests?.TryGetValue(new MongoId(questId), out var quest) != true) return;
        quest!.Conditions.AvailableForStart?.RemoveAll(c => c.ConditionType == "Level" && c.Value >= RetiredLevel);
    }

    private const double RetiredLevel = 79;

    /// <summary>Only values that differ from the game's are written, so another mod's change stays when nothing was asked.</summary>
    private void ApplyMechanics(MechanicsSettings m)
    {
        var defaults = new MechanicsSettings();
        if (globals.Configuration?.EventSettings is { } ritual)
        {
            if (m.RitualExitTime != defaults.RitualExitTime) ritual.ExitTimeMultiplier = m.RitualExitTime;
            if (m.RitualStamina != defaults.RitualStamina) ritual.StaminaMultiplier = m.RitualStamina;
        }
        if (globals.Configuration?.Health?.Effects?.ZombieInfection is { } toxin)
        {
            if (m.ToxinDehydration != defaults.ToxinDehydration) toxin.Dehydration = -m.ToxinDehydration;
            if (m.ToxinHearing != defaults.ToxinHearing) toxin.HearingDebuffPercentage = m.ToxinHearing / 100;
        }
    }
}
