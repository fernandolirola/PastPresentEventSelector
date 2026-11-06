namespace PPES.Events;

/// <param name="Keycard">The map can ask for a keycard to enter.</param>
public sealed record MapInfo(string Id, string NameEn, string NamePt, bool Keycard = false);

public static class GameMaps
{
    public static readonly IReadOnlyList<MapInfo> All =
    [
        new("bigmap", "Customs", "Customs"),
        new("factory4_day", "Factory (day)", "Factory (dia)"),
        new("factory4_night", "Factory (night)", "Factory (noite)"),
        new("woods", "Woods", "Woods"),
        new("shoreline", "Shoreline", "Shoreline"),
        new("interchange", "Interchange", "Interchange"),
        new("rezervbase", "Reserve", "Reserve"),
        new("laboratory", "The Lab", "Lab", Keycard: true),
        new("lighthouse", "Lighthouse", "Lighthouse"),
        new("tarkovstreets", "Streets", "Streets"),
        new("sandbox", "Ground Zero", "Ground Zero"),
        new("sandbox_high", "Ground Zero 21+", "Ground Zero 21+"),
        new("labyrinth", "Labyrinth", "Labirinto", Keycard: true),
    ];

    public static readonly string[] AllIds = All.Select(m => m.Id).ToArray();

    public static readonly string[] Outdoor = ["bigmap", "woods", "shoreline", "interchange", "rezervbase", "lighthouse", "tarkovstreets", "sandbox", "sandbox_high"];

    /// <summary>Maps with loot containers (every map but the Labyrinth).</summary>
    public static readonly string[] WithLoot = AllIds.Where(id => id != "labyrinth").ToArray();

    public static MapInfo? Get(string id) => All.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The maps a key of SPT's seasonal settings stands for: "factory4" and "sandbox" cover both of their variants,
    /// the same mapping SPT uses for the zombie settings.
    /// </summary>
    public static IEnumerable<string> FromSeasonalKey(string key) => key.ToLowerInvariant() switch
    {
        "factory4" => ["factory4_day", "factory4_night"],
        "sandbox" => ["sandbox", "sandbox_high"],
        var id => [id],
    };
}
