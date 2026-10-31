using Microsoft.Extensions.DependencyInjection;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace PPES.Factions;

internal sealed class PpesFactions(
    IServiceProvider services,
    BotTable botTable,
    BotConfig botConfig,
    LocaleTable locales,
    ICloner cloner,
    ISptLogger<FactionSetup> logger)
{
    public async Task BuildAsync(CancellationToken cancellationToken)
    {
        var loaded = new List<FactionDef>();
        foreach (var def in FactionCatalog.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (AddBotType(def)) loaded.Add(def);
            }
            catch (Exception ex)
            {
                logger.Error($"{ConfigService.LogPrefix} faction {def.NameEn} could not be created: {ex.Message}");
            }
        }

        AddHostility(loaded);
        AddLocales(loaded);
        await RegisterWithSainAsync(loaded, cancellationToken);
        foreach (var def in loaded) FactionCatalog.MarkReady(def.Role);

        if (loaded.Count > 0)
            logger.Info($"{ConfigService.LogPrefix} Built-in factions ready: {string.Join(", ", loaded.Select(d => d.NameEn))}" +
                        (FactionCatalog.BlackDivision ? " (Black Division found)" : ""));
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
            mind.FriendlyBotTypes = [];
            mind.WarnBotTypes = [];
            mind.RevengeBotTypes = [(WildSpawnType)def.Id];
        }
        botTable.Types[key] = type;
        CopyConfig(sourceKey, def.Role, key);
        return true;
    }

    private void AddHostility(IReadOnlyCollection<FactionDef> loaded)
    {
        if (loaded.Count == 0) return;
        var customRoles = loaded.Select(d => (WildSpawnType)d.Id).ToArray();
        var knownRoles = botTable.Types.Keys
            .Select(key => Enum.TryParse<WildSpawnType>(key, true, out var role) ? role : (WildSpawnType?)null)
            .Where(role => role.HasValue)
            .Select(role => role!.Value)
            .Concat(customRoles)
            .Distinct()
            .ToArray();

        foreach (var (key, type) in botTable.Types)
        {
            if (type?.BotDifficulty is null) continue;
            var own = loaded.FirstOrDefault(d => key.Equals(d.Role, StringComparison.OrdinalIgnoreCase));
            var enemies = own is null ? customRoles : knownRoles.Where(role => (int)role != own.Id).ToArray();
            foreach (var difficulty in type.BotDifficulty.Values)
            {
                if (difficulty?.Mind?.EnemyBotTypes is not { } enemyTypes) continue;
                foreach (var enemy in enemies)
                    if (!enemyTypes.Contains(enemy)) enemyTypes.Add(enemy);
            }
        }
    }

    private void CopyConfig(string sourceKey, string role, string key)
    {
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

    private async Task RegisterWithSainAsync(IReadOnlyCollection<FactionDef> loaded, CancellationToken cancellationToken)
    {
        var registryType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("SAIN.ServerInterop.ISainBotTypeRegistry", false))
            .FirstOrDefault(t => t is not null);
        var registrationType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("SAIN.ServerInterop.SainBotTypeRegistration", false))
            .FirstOrDefault(t => t is not null);
        if (registryType is null || registrationType is null || services.GetService(registryType) is not { } registry) return;

        var register = registryType.GetMethod("RegisterAsync")
            ?? throw new MissingMethodException(registryType.FullName, "RegisterAsync");
        foreach (var def in loaded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var registration = Activator.CreateInstance(registrationType)
                ?? throw new InvalidOperationException("SAIN registration could not be created");
            Set(registrationType, registration, "Name", def.NameEn);
            Set(registrationType, registration, "WildSpawnType", def.Id);
            Set(registrationType, registration, "BotDbKey", def.Role);
            Set(registrationType, registration, "Section", "PPES");
            Set(registrationType, registration, "Description", def.DescEn);
            Set(registrationType, registration, "DifficultyModifier", 1f);
            Set(registrationType, registration, "BrainsToApply", new List<string> { "PMC", "ExUsec" });
            Set(registrationType, registration, "LayersToRemove", new List<string> { "Request", "KnightFight", "PmcBear", "PmcUsec", "ExURequest", "StationaryWS" });
            if (register.Invoke(registry, [registration, cancellationToken]) is Task task) await task;
        }
        logger.Info($"{ConfigService.LogPrefix} Registered {loaded.Count} built-in factions with SAIN");
    }

    private static void Set(Type type, object target, string property, object value) =>
        (type.GetProperty(property) ?? throw new MissingMemberException(type.FullName, property)).SetValue(target, value);

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
