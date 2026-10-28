using System.Text;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace PPES.Services;

/// <summary>
/// Tells whether a wiki quest exists in this server's quest table, matched by English name so quests added
/// by other mods (QuestBackport) count too. Names are compared loosely: the game spells some with Cyrillic
/// look-alike letters and the wiki adds suffixes like "(quest)" or "(2024)".
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class QuestAvailability(TemplateTable templates, LocaleTable locales)
{
    private HashSet<string>? _names;

    public bool Exists(string wikiName) => Names.Contains(Normalize(StripWikiSuffix(wikiName)));

    private HashSet<string> Names => _names ??= Build();

    private HashSet<string> Build()
    {
        var names = new HashSet<string>();
        var english = locales.Global.TryGetValue("en", out var en) ? en.Value : null;
        foreach (var (id, quest) in templates.Quests ?? [])
        {
            if (english is not null && english.TryGetValue($"{id} name", out var localized)) names.Add(Normalize(localized));
            if (quest.QuestName is { } internalName) names.Add(Normalize(internalName));
        }
        return names;
    }

    private static string StripWikiSuffix(string name)
    {
        var paren = name.LastIndexOf(" (", StringComparison.Ordinal);
        if (paren < 0 || !name.EndsWith(')')) return name;
        var suffix = name[(paren + 2)..^1];
        return suffix == "quest" || int.TryParse(suffix, out _) ? name[..paren] : name;
    }

    private static string Normalize(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var raw in name.ToLowerInvariant())
        {
            var c = raw switch
            {
                'а' => 'a', 'с' => 'c', 'е' => 'e', 'о' => 'o', 'р' => 'p', 'х' => 'x', 'у' => 'y', 'к' => 'k', 'м' => 'm', 'т' => 't', 'в' => 'b', 'н' => 'h',
                _ => raw,
            };
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }
}
