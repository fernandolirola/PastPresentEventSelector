using MoreBotsLoadOrder = MoreBotsServer.MoreBotsLoadOrder;
using MoreBotsServer.Interop;
using MoreBotsServer.Models;
using MoreBotsServer.Services;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Factions;

/// <summary>
/// Builds the server side of the PPES factions: a bot type cloned from a close stock type, its bot config,
/// the MoreBotsAPI faction with its enemies, the SAIN entry and the name shown on the kill list.
/// Runs right after MoreBotsAPI and the mods using it (Black Division) have loaded their bots.
/// </summary>
[Injectable(TypePriority = MoreBotsLoadOrder.LoadBots + 1)]
public sealed class FactionSetup(
    BotTable botTable,
    BotConfig botConfig,
    LocaleTable locales,
    MoreBotsCustomBotTypeService botTypes,
    FactionService factions,
    SainInteropRegistration sain,
    ICloner cloner,
    ISptLogger<FactionSetup> logger) : IOnLoad
{
    // everyone the new factions fight, when that faction exists
    private static readonly string[] Hostile = ["savage", "scavs", "rogues", "raiders", "usec", "bear", "infected", "blackdiv", "ruaf"];

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        FactionCatalog.BlackDivision = botTable.Types.ContainsKey("bosswedge");

        var loaded = new List<FactionDef>();
        foreach (var def in FactionCatalog.All)
        {
            try
            {
                if (AddBotType(def)) loaded.Add(def);
            }
            catch (Exception ex)
            {
                logger.Error($"{ConfigService.LogPrefix} faction {def.NameEn} could not be created: {ex.Message}");
            }
        }

        var roles = loaded.Select(d => d.Role).ToList();
        var enemies = Hostile.Where(factions.Factions.ContainsKey).ToList();
        foreach (var def in loaded)
        {
            foreach (var other in loaded.Where(o => o != def))
                factions.AddEnemyByFaction([def.Role], other.Faction);
            foreach (var enemy in enemies)
            {
                factions.AddEnemyByFaction([def.Role], enemy);
                factions.AddEnemyByFaction(enemy, def.Faction);
            }
            FactionCatalog.MarkReady(def.Role);
        }

        AddLocales(loaded);
        if (loaded.Count > 0)
            logger.Info($"{ConfigService.LogPrefix} Factions ready: {string.Join(", ", loaded.Select(d => d.NameEn))}" +
                        (FactionCatalog.BlackDivision ? " (Black Division found)" : ""));
        return Task.CompletedTask;
    }

    private bool AddBotType(FactionDef def)
    {
        var sourceKey = def.Sources.FirstOrDefault(botTable.Types.ContainsKey);
        if (sourceKey is null || botTable.Types[sourceKey] is not { } source)
        {
            logger.Warning($"{ConfigService.LogPrefix} no source bot type for {def.NameEn}, faction skipped");
            return false;
        }

        var key = def.Role.ToLowerInvariant();
        var type = cloner.Clone(source)!;
        foreach (var difficulty in type.BotDifficulty.Values)
        {
            if (difficulty?.Mind is not { } mind) continue;
            // the source's friends would otherwise leave this faction alone
            mind.FriendlyBotTypes = [];
            mind.WarnBotTypes = [];
            mind.RevengeBotTypes = [(WildSpawnType)def.Id];
        }
        botTable.Types[key] = type;
        if (!botTypes.LoadedBotTypes.Contains(key)) botTypes.LoadedBotTypes.Add(key);
        // lowercase so MoreBotsAPI finds the type in the bot table when it sets enemies
        botTypes.AddCustomWildSpawnTypeNames(new Dictionary<int, string> { [def.Id] = key });

        CopyConfig(sourceKey, def.Role, key);

        factions.Factions[def.Faction] = new Faction { Name = def.Faction, BotTypes = [(WildSpawnType)def.Id] };
        sain.RegisterBotType(new MoreBotsSainBotTypeRegistration
        {
            Name = def.NameEn,
            WildSpawnType = def.Id,
            BotDbKey = def.Role,
            Section = "PPES",
            Description = def.DescEn,
            DifficultyModifier = 1f,
            BrainsToApply = ["PMC", "ExUsec"],
            LayersToRemove = ["Request", "KnightFight", "PmcBear", "PmcUsec", "ExURequest", "StationaryWS"],
        });
        return true;
    }

    private void CopyConfig(string sourceKey, string role, string key)
    {
        // bot.json mixes camelCase and lowercase keys between sections
        if (Find(botConfig.PresetBatch, sourceKey) is { } batch) botConfig.PresetBatch[role] = batch;
        if (Find(botConfig.Durability.BotDurabilities, sourceKey) is { } durability)
            botConfig.Durability.BotDurabilities[key] = cloner.Clone(durability)!;
        if (Find(botConfig.Equipment, sourceKey) is { } equipment)
            botConfig.Equipment[key] = cloner.Clone(equipment);
        if (Find(botConfig.ItemSpawnLimits, sourceKey) is { } limits)
            botConfig.ItemSpawnLimits[key] = cloner.Clone(limits)!;
        if (Find(botConfig.CurrencyStackSize, sourceKey) is { } currency)
            botConfig.CurrencyStackSize[key] = cloner.Clone(currency)!;
    }

    private static T? Find<T>(Dictionary<string, T> dict, string key) =>
        dict.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)).Value;

    private void AddLocales(IReadOnlyList<FactionDef> loaded)
    {
        if (loaded.Count == 0) return;
        foreach (var (language, lazy) in locales.Global)
        {
            var portuguese = language is "po" or "pt";
            lazy.AddTransformer(data =>
            {
                if (data is null) return data;
                foreach (var def in loaded)
                    data[$"ScavRole/{def.ScavRole}"] = portuguese ? def.NamePt : def.NameEn;
                return data;
            });
        }
    }
}
