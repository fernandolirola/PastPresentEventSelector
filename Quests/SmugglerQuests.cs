using System.Reflection;
using PPES.Factions;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace PPES.Quests;

/// <summary>
/// Adds the Smugglers 2024 and 2026 quests (built by tools/gen_smuggler_quests.py) and the loot they need.
/// The quests are always in the table, so accepted ones survive the event ending; SPT hides them as event
/// quests and EventApplier, which runs right after, shows them while their event is picked.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 4999000)]
public sealed class SmugglerQuests(
    TemplateTable templates,
    LocaleTable locales,
    HideoutTable hideout,
    BotTable botTable,
    QuestConfig questConfig,
    InventoryConfig inventoryConfig,
    JsonUtil json,
    ISptLogger<SmugglerQuests> logger) : IOnLoad
{
    public const string Walter = "674647f38466ebb03408b291";
    public static readonly string[] Quests2024 = [Walter, "6746480cd0b2f8eb9b034e3e", "6746480040ea3d1d4f0e5d51"];
    public static readonly string[] Quests2026 = ["69ae8e9d8d5952d853d791a1", "f4411dd1f99ce3678f7bd579", "34f3c3bb54f8d18fd4a320c3", "0c472b7ba2915b42ba295a26", "68a6dfb24982a975f038440b"];

    private const string LockedCase = "6740987b89d5e1ddc603f4f0";
    private const string CaseKey = "67449b6c89d5e1ddc603f504";
    private const string Contraband = "67408903268737ef6908d432";
    private const string OpenedCase = "674078c4a9c9adf0450d59f9";
    // workbench recipes from the 2024 event, unlocked by Walter's reward
    private static readonly string[] WalterRecipes = ["67449c79268737ef6908d636", "6745925da9c9adf0450d5bca"];

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (!FactionCatalog.IsReady(FactionCatalog.Smuggler))
        {
            logger.Warning($"{ConfigService.LogPrefix} Smugglers faction missing, the Smugglers quests were not added");
            return Task.CompletedTask;
        }

        try
        {
            var quests = json.Deserialize<Dictionary<MongoId, Quest>>(Resource("smugglerQuests.json"))!;
            foreach (var (id, quest) in quests)
            {
                templates.Quests[id] = quest;
                // an end date in the past hides the quest until its event is picked
                questConfig.EventQuests ??= [];
                questConfig.EventQuests[id] = new EventQuestData { Name = quest.QuestName, Season = SeasonalEventType.None, StartTimestamp = 0, EndTimestamp = 1, Yearly = false };
            }
            AddLocales(json.Deserialize<Dictionary<string, Dictionary<string, string>>>(Resource("smugglerLocales.json"))!);
            UnlockRecipesWith(Walter);
            AddSmugglerLoot();
            if (inventoryConfig.RandomLootContainers.TryGetValue(OpenedCase, out var opened) && opened.RewardTplPool is { } pool)
                pool[Contraband] = pool.Values.DefaultIfEmpty(1).Sum();
            logger.Info($"{ConfigService.LogPrefix} Smugglers quests ready ({quests.Count})");
        }
        catch (Exception ex)
        {
            logger.Error($"{ConfigService.LogPrefix} Smugglers quests could not be added: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    private static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"PPES.Quests.{name}")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void AddLocales(Dictionary<string, Dictionary<string, string>> texts)
    {
        var english = texts["en"];
        foreach (var (language, lazy) in locales.Global)
        {
            var own = texts.GetValueOrDefault(language is "pt" ? "po" : language);
            lazy.AddTransformer(data =>
            {
                if (data is null) return data;
                // only missing keys, so the 2024 quests keep the game's own translated texts
                foreach (var (key, value) in english)
                    data.TryAdd(key, own?.GetValueOrDefault(key) ?? value);
                return data;
            });
        }
    }

    private void UnlockRecipesWith(string questId)
    {
        foreach (var recipe in hideout.Production.Recipes ?? [])
        {
            if (!WalterRecipes.Contains(recipe.Id.ToString())) continue;
            foreach (var requirement in recipe.Requirements ?? [])
                if (requirement.Type == "QuestComplete") requirement.QuestId = questId;
        }
    }

    private void AddSmugglerLoot()
    {
        if (!botTable.Types.TryGetValue(FactionCatalog.Smuggler.ToLowerInvariant(), out var type) || type?.BotInventory.Items?.Backpack is not { } backpack)
            return;
        // the Rogue loadout they are cloned from often has no backpack, and the cargo only goes there
        if (type.BotChances?.EquipmentChances is { } chances) chances["Backpack"] = 100;
        // share of backpack picks, so a smuggler carries event cargo often but not always
        foreach (var (tpl, share) in new[] { (LockedCase, 0.3), (Contraband, 0.3), (CaseKey, 0.2) })
        {
            var others = backpack.Where(p => p.Key != tpl).Sum(p => p.Value);
            backpack[tpl] = Math.Max(1, Math.Round(others * share / (1 - share)));
        }
    }
}
