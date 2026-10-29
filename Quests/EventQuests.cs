using System.Reflection;
using System.Text.Json;
using PPES.Events;
using PPES.Factions;
using PPES.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace PPES.Quests;

/// <summary>
/// Adds the recreated event quests (built by tools/questgen) and the loot they need.
/// The quests are always in the table, so accepted ones survive the event ending; SPT hides them as event
/// quests and EventApplier, which runs right after, shows them while their event is picked.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 4999000)]
public sealed class EventQuests(
    TemplateTable templates,
    LocaleTable locales,
    HideoutTable hideout,
    BotTable botTable,
    GlobalTable globals,
    ConfigService config,
    QuestConfig questConfig,
    InventoryConfig inventoryConfig,
    JsonUtil json,
    ISptLogger<EventQuests> logger) : IOnLoad
{
    private const string Walter = "674647f38466ebb03408b291";
    private const string LockedCase = "6740987b89d5e1ddc603f4f0";
    private const string CaseKey = "67449b6c89d5e1ddc603f504";
    private const string Contraband = "67408903268737ef6908d432";
    private const string OpenedCase = "674078c4a9c9adf0450d59f9";
    // workbench recipes from the 2024 event, unlocked by Walter's reward
    private static readonly string[] WalterRecipes = ["67449c79268737ef6908d636", "6745925da9c9adf0450d5bca"];
    // painted Atomic Defense CQCM masks the Casus belli quests ask for
    private static readonly string[] PaintedMasks = ["67a4b71ad3228756b6088ee2", "67a5c5b6dfdf568c9009af66", "67a5c5df782ce4655104db14", "67a5c5f37f52620c5b05b4d6",
        "67a5c6068fcd9fb73f0752cf", "67a5c61c7f52620c5b05b4d8", "67a5c657782ce4655104db16", "688b3bfa1ed594eccd0c45ee", "68a9a04373d52d47830759c7",
        "68a9a0b01696fb8c1e0ee9cc", "68a9a15d73d52d47830759c9"];
    private static readonly string[] QuestTextKeys = ["name", "description", "successMessageText", "failMessageText", "startedMessageText",
        "acceptPlayerMessage", "declinePlayerMessage", "completePlayerMessage", "changeQuestMessageText", "note"];

    private sealed record Swap(string Quest, string From, string To);

    private sealed record Meta(Dictionary<string, string> Aliases, Dictionary<string, List<string>> Events, Dictionary<string, string> Requires, List<Swap>? Swaps);

    /// <summary>Quests of a seasonal event: SPT shows them while that season is on, like its own Halloween quests.</summary>
    private static readonly Dictionary<string, SeasonalEventType> SeasonalEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        [EventCatalog.Halloween2026] = SeasonalEventType.Halloween,
    };

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var quests = json.Deserialize<Dictionary<MongoId, Quest>>(Resource("eventQuests.json"))!;
            var meta = JsonSerializer.Deserialize<Meta>(Resource("eventQuestMeta.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var skipped = new HashSet<string>();
            questConfig.EventQuests ??= [];
            var seasonOf = meta.Events.Where(e => SeasonalEvents.ContainsKey(e.Key))
                .SelectMany(e => e.Value.Select(id => (id, SeasonalEvents[e.Key]))).ToDictionary(x => x.id, x => x.Item2);
            var outbreak = config.Saved.Halloween.Outbreak2026;
            foreach (var (id, quest) in quests)
            {
                if (meta.Requires.TryGetValue(id.ToString(), out var need) && !Available(need))
                {
                    skipped.Add(need);
                    continue;
                }
                templates.Quests[id] = quest;
                // same entry as SPT's own Halloween quests; with the 2026 part off the end date below hides them instead
                if (seasonOf.TryGetValue(id.ToString(), out var season) && outbreak)
                    questConfig.EventQuests[id] = new EventQuestData { Name = quest.QuestName, Season = season, StartTimestamp = 1341615600000, EndTimestamp = 0, Yearly = true };
                else
                    // an end date in the past hides the quest until its event is picked
                    questConfig.EventQuests[id] = new EventQuestData { Name = quest.QuestName, Season = SeasonalEventType.None, StartTimestamp = 0, EndTimestamp = 1, Yearly = false };
            }
            foreach (var (eventId, ids) in meta.Events)
                EventCatalog.ExtraQuests[eventId] = ids.Where(i => templates.Quests.ContainsKey(i)).ToList();

            SwapRewards(meta.Swaps ?? []);
            AddLocales(json.Deserialize<Dictionary<string, Dictionary<string, string>>>(Resource("eventLocales.json"))!, meta.Aliases);
            UnlockRecipesWith(Walter);
            AddFactionLoot();
            if (inventoryConfig.RandomLootContainers.TryGetValue(OpenedCase, out var opened) && opened.RewardTplPool is { } pool)
                pool[Contraband] = pool.Values.DefaultIfEmpty(1).Sum();

            var added = EventCatalog.ExtraQuests.Values.Sum(v => v.Count);
            logger.Info($"{ConfigService.LogPrefix} Event quests ready ({added})" + (skipped.Count > 0 ? $", some skipped for missing {string.Join(", ", skipped)}" : ""));
        }
        catch (Exception ex)
        {
            logger.Error($"{ConfigService.LogPrefix} Event quests could not be added: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    private static bool Available(string need) =>
        need == EventCatalog.RequiresBlackDivision ? FactionCatalog.BlackDivision : FactionCatalog.IsReady(need);

    private static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"PPES.Quests.{name}")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void AddLocales(Dictionary<string, Dictionary<string, string>> texts, Dictionary<string, string> aliases)
    {
        var english = texts["en"];
        foreach (var (language, lazy) in locales.Global)
        {
            var own = texts.GetValueOrDefault(language is "pt" ? "po" : language);
            lazy.AddTransformer(data =>
            {
                if (data is null) return data;
                // only missing keys, so quests that kept their texts in the game keep the translations
                foreach (var (key, value) in english)
                    data.TryAdd(key, own?.GetValueOrDefault(key) ?? value);
                // route copies of a quest show the texts of the quest they copy
                foreach (var (copy, source) in aliases)
                    foreach (var k in QuestTextKeys)
                        if (data.TryGetValue($"{source} {k}", out var text))
                            data[$"{copy} {k}"] = text;
                return data;
            });
        }
    }

    /// <summary>
    /// Rewards SPT lacks are given as stand-ins; when a mod that adds the real item is installed (WTT-ContentBackport),
    /// the stand-in is replaced: a weapon by its preset, anything else by the item, with an ammo box refilled with its own rounds.
    /// </summary>
    private void SwapRewards(List<Swap> swaps)
    {
        var items = templates.Items;
        foreach (var swap in swaps)
        {
            if (!items.ContainsKey(swap.To) || !templates.Quests.TryGetValue(swap.Quest, out var quest)) continue;
            foreach (var reward in quest.Rewards?.GetValueOrDefault("Success") ?? [])
            {
                if (reward.Items is not { Count: > 0 } list || list[0].Template != swap.From) continue;
                var preset = globals.ItemPresets?.Values.FirstOrDefault(p => p.Encyclopedia == swap.To);
                if (preset?.Items is { Count: > 0 } presetItems)
                {
                    var root = list[0].Id;
                    var ids = presetItems.ToDictionary(i => i.Id, i => i.Id == presetItems[0].Id ? root : new MongoId());
                    reward.Items = presetItems.Select(i => new Item
                    {
                        Id = ids[i.Id],
                        Template = i.Template,
                        ParentId = i.ParentId is { } parent && ids.TryGetValue(parent, out var p) ? p.ToString() : null,
                        SlotId = i.Id == presetItems[0].Id ? null : i.SlotId,
                        Upd = i.Id == presetItems[0].Id ? new Upd { StackObjectsCount = 1 } : null,
                    }).ToList();
                    continue;
                }
                list[0].Template = swap.To;
                var rounds = items[swap.To].Properties?.StackSlots?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter?.FirstOrDefault();
                foreach (var child in list.Where(i => i.SlotId == "cartridges"))
                    if (rounds is { } r) child.Template = r;
            }
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

    private void AddFactionLoot()
    {
        if (botTable.Types.TryGetValue(FactionCatalog.Smuggler.ToLowerInvariant(), out var smuggler) && smuggler?.BotInventory.Items?.Backpack is { } backpack)
        {
            // the Rogue loadout they are cloned from often has no backpack, and the cargo only goes there
            if (smuggler.BotChances?.EquipmentChances is { } chances) chances["Backpack"] = 100;
            // share of backpack picks, so a smuggler carries event cargo often but not always
            foreach (var (tpl, share) in new[] { (LockedCase, 0.3), (Contraband, 0.3), (CaseKey, 0.2) })
                AddShare(backpack, tpl, share);
        }
        if (botTable.Types.TryGetValue(FactionCatalog.Pillager.ToLowerInvariant(), out var pillager) && pillager is not null
            && pillager.BotInventory.Equipment?.TryGetValue(EquipmentSlots.FaceCover, out var faces) == true && faces is not null)
        {
            if (pillager.BotChances?.EquipmentChances is { } chances) chances["FaceCover"] = Math.Max(chances.GetValueOrDefault("FaceCover"), 70);
            foreach (var mask in PaintedMasks)
                AddShare(faces, mask, 0.4 / PaintedMasks.Length);
        }
    }

    private static void AddShare(Dictionary<MongoId, double> pool, string tpl, double share)
    {
        var others = pool.Where(p => p.Key != tpl).Sum(p => p.Value);
        pool[tpl] = Math.Max(1, Math.Round(others * share / (1 - share)));
    }
}
