using System.Reflection;
using System.Text.Json;

namespace PPES.Quests;

/// <summary>The recreated event quests built by tools/questgen, as embedded in this assembly.</summary>
internal static class QuestCatalog
{
    public sealed record Swap(string Quest, string From, string To);

    /// <param name="Events">Quest ids per event key (a past event id, or a key a seasonal event names).</param>
    /// <param name="Requires">Quest id to the mod or PPES faction role it needs.</param>
    /// <param name="Aliases">Route copy of a quest to the quest whose texts it shows.</param>
    public sealed record Metadata(Dictionary<string, string> Aliases, Dictionary<string, List<string>> Events, Dictionary<string, string> Requires, List<Swap>? Swaps);

    private static readonly Lazy<Metadata> MetaLazy = new(() =>
        JsonSerializer.Deserialize<Metadata>(Resource("eventQuestMeta.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> LocalesLazy = new(() =>
        JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(Resource("eventLocales.json"))!);

    public static Metadata Meta => MetaLazy.Value;

    public static Dictionary<string, Dictionary<string, string>> Locales => LocalesLazy.Value;

    public static string QuestsJson => Resource("eventQuests.json");

    public static IReadOnlyList<string> RecreatedFor(string eventKey) => Meta.Events.GetValueOrDefault(eventKey) ?? [];

    /// <summary>The recreated quests of an event this server loads: the ones whose mod or faction is there.</summary>
    public static IEnumerable<string> LoadedFor(string eventKey) =>
        RecreatedFor(eventKey).Where(id => !Meta.Requires.TryGetValue(id, out var need) || Events.ModRequirements.Has(need));

    public static IEnumerable<string> EnglishNames(IEnumerable<string> questIds) =>
        questIds.Select(id => Locales["en"].GetValueOrDefault($"{id} name")).OfType<string>();

    private static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"PPES.Quests.{name}")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
