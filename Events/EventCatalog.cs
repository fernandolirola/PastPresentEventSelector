using PPES.Quests;

namespace PPES.Events;

public static class SeasonalIds
{
    public const string Halloween = "halloween";
    public const string Christmas = "christmas";
    public const string NewYears = "newyears";
    public const string AprilFools = "aprilfools";
}

public static partial class EventCatalog
{
    private const string WikiBase = "https://escapefromtarkov.fandom.com/wiki/";

    // wiki page titles; the game's own names differ in places (Cyrillic letters, "Hustle" twice)
    private static readonly Dictionary<string, string> QuestTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["647710905320c660d91c15a5"] = "Hint",
        ["6863cf925eaeb676d50a9651"] = "Friend from Norvinsk - Part 1",
        ["64916da7ad4e722c106f2345"] = "Failed Setup",
        ["66e01aca214f88109006a4b5"] = "Into the Inferno",
        ["66a74c628410476dd65543be"] = "Gunsmith - Special Order",
        ["668bcccc167d507eb01a268b"] = "Import Control",
        ["6672ec2a2b6f3b71be794cc5"] = "A Key to Salvation",
        ["6658a15615cbb1b2c6014d5b"] = "Hustle (2024)",
        ["6603fe74e773dcf3b0099f88"] = "The Tarkov Mystery",
        ["655e427b64d09b4122018228"] = "The Punisher - Harvest",
        ["649570491bb4d158bc4d0168"] = "Tourists",
        ["649567f359eab30d1b7c9585"] = "Hustle (2023)",
        ["649af47d717cb30e7e4b5e26"] = "Cocktail Tasting",
        ["64764abcd125ab430a14ccb5"] = "Bloodhounds (quest)",
        ["641dbfd7f43eda9d810d7137"] = "Important Patient",
        ["66e3e2ee2136472d220bcb36"] = "Night of The Cult",
        ["670404a2ea1caa8f2e0be106"] = "Don\u2019t Believe Your Eyes",
        ["67190f6c1b3f4964d90d71e9"] = "Global Threat",
        ["675031be899713ccad00060c"] = "Christmas Dinner",
        ["675031d3884e1da4a90b3bc9"] = "Chilly",
        ["6750320e23fc8fd9cc087d14"] = "Holiday Beyond the Means",
        ["67503247622398376d0b57cd"] = "Khorovod",
        ["675031e1f300496cc4104450"] = "Hide in Plain Sight",
        ["675031f57775aada6b0f96a1"] = "Cheer Up",
        ["674647f38466ebb03408b291"] = "That\u2019s a Great Plan, Walter",
        ["6746480cd0b2f8eb9b034e3e"] = "Chop the Thieving Hand",
        ["6746480040ea3d1d4f0e5d51"] = "Shell Game",
        ["69ae8e9d8d5952d853d791a1"] = "Bulldogs Under The Rug",
        ["f4411dd1f99ce3678f7bd579"] = "Accept Delivery",
        ["34f3c3bb54f8d18fd4a320c3"] = "Thirty Pieces of Silver",
        ["0c472b7ba2915b42ba295a26"] = "Restocking Supplies",
        ["68a6dfb24982a975f038440b"] = "Root of All Evil",
    };

    public static readonly IReadOnlyList<SeasonalEventDef> Seasonal =
    [
        new(SeasonalIds.Halloween, "Halloween", "Halloween", ["halloween"],
            "Zombies, cultist bosses, pumpkins, Tagilla with a scythe and the summoning ritual. Halloween quests unlock. The 2026 outbreak part spreads the infected to every map and adds the five Halloween 2026 quests.",
            "Zumbis, chefes cultistas, ab\u00f3boras, Tagilla com foice e o ritual de invoca\u00e7\u00e3o. Libera as quests de Halloween. A parte Surto de 2026 espalha os infectados por todos os mapas e adiciona as cinco quests de Halloween 2026.",
            "Strange things after dark. Pumpkins, cultists and worse.",
            "Coisas estranhas depois que escurece. Ab\u00f3boras, cultistas e coisa pior.",
            Wiki: "Halloween_2025_(22_October_2025)",
            GatedQuests: ["66e3e2ee2136472d220bcb36", "670404a2ea1caa8f2e0be106", "67190f6c1b3f4964d90d71e9"],
            RecreatedQuests: "halloween2026"),
        new(SeasonalIds.Christmas, "Christmas", "Natal", ["christmas", "christmas January"],
            "Santa on the maps, a decorated hideout, the Khorovod tree event, winter gear and the Christmas quests.",
            "Papai Noel nos mapas, hideout decorado, evento da \u00e1rvore (Khorovod), roupas de inverno e as quests de Natal.",
            "Santa is walking the maps with his bag. Don't shoot him, I mean it.",
            "O Papai Noel est\u00e1 andando pelos mapas com o saco. N\u00e3o atire nele, \u00e9 s\u00e9rio.",
            Wiki: "Khorovod_(26_December_2024)",
            GatedQuests: ["675031be899713ccad00060c", "675031d3884e1da4a90b3bc9", "6750320e23fc8fd9cc087d14", "67503247622398376d0b57cd", "675031e1f300496cc4104450", "675031f57775aada6b0f96a1"]),
        new(SeasonalIds.NewYears, "New Year", "Ano Novo", ["newyears"],
            "Decorated hideout, Khorovod and Santa for New Year's Day.",
            "Hideout decorado, Khorovod e Papai Noel no dia de Ano Novo.",
            "Happy New Year. Even here, people dance around the tree.",
            "Feliz Ano Novo. At\u00e9 aqui o pessoal dan\u00e7a em volta da \u00e1rvore.",
            Wiki: "Kolotun_(24_December_2025)"),
        new(SeasonalIds.AprilFools, "April Fools", "1\u00ba de abril", ["April fools"],
            "Winter weather, Santa and his gifts, the summoning ritual and the Halloween cultists, all at once.",
            "Clima de inverno, Papai Noel e seus presentes, o ritual de invoca\u00e7\u00e3o e os cultistas do Halloween, tudo junto.",
            "Nothing makes sense today. Snow, cultists, gifts. Don't ask.",
            "Nada faz sentido hoje. Neve, cultistas, presentes. N\u00e3o pergunte.",
            Wiki: "April_Fools_(1_April_2026)"),
    ];

    public static readonly IReadOnlyDictionary<string, (string En, string Pt)> TouchNames = new Dictionary<string, (string, string)>
    {
        ["glukhar"] = ("Glukhar", "Glukhar"),
        ["goons"] = ("the Goons", "os Goons"),
        ["flea"] = ("the flea market", "o mercado"),
        ["labKeys"] = ("Lab access", "a entrada da Lab"),
        ["airdrop"] = ("airdrop chance", "a chance de airdrop"),
        ["sanitar"] = ("Sanitar", "Sanitar"),
        ["tagilla"] = ("Tagilla", "Tagilla"),
        ["killa"] = ("Killa", "Killa"),
        ["shturman"] = ("Shturman", "Shturman"),
        ["reshala"] = ("Reshala", "Reshala"),
        ["cultists"] = ("the Cultists", "os Cultistas"),
    };

    public static readonly IReadOnlyList<BuiltInPreset> Presets =
    [
        new("bossHunt", "Boss hunt", "Ca\u00e7ada de chefes", "Bosses everywhere, every raid.", "Chefes por todo lado, em toda raid.",
            ["permanentBossPresence", "meetingOfBosses", "bossesGathering", "brothersReunited", "cultistPilgrimage", "bloodhounds", "arenaCleanupFactory"]),
        new("casual", "Casual", "Casual", "Less grind: open flea, instant hideout, more XP and loot from the sky.", "Menos grind: flea aberto, hideout na hora, mais XP e loot caindo do c\u00e9u.",
            ["softcore", "instantHideout", "fleaPromotion", "boostedSkills", "unlockedTraders", "bonusExp", "airdropTraffic", "weaponCaseAirdrops"]),
        new("hardcore", "Hardcore", "Hardcore", "Closed flea, heat, mortars and cultists every night.", "Flea fechado, calor, morteiros e cultistas toda noite.",
            ["closedFlea", "heatwave", "mortarStrike", "cultistPilgrimage", "permanentBossPresence"]),
        new("questHunter", "Quest hunter", "Ca\u00e7ador de quests", "Every event that unlocks quests in your game.", "Todos os eventos que liberam quests no seu jogo.",
            ["targradTales", "mortarStrike", "supplyAndDemand", "armsRace", "keyToSalvation", "hustle", "tarkovMystery", "ammoShortage", "bossesAndRogues", "hardcoreMode", "bloodhounds", "bandages", "cultistPilgrimage", "permanentBossPresence"]),
        new("factions", "New factions", "Fac\u00e7\u00f5es novas", "Cleanup Crew, Smugglers and Pillagers out on the maps.", "Cleanup Crew, Smugglers e Pillagers pelos mapas.",
            ["arenaCleanupFactory", "tarkovHospitality", "smugglers2026", "casusBelli"]),
        new("vanilla", "Vanilla", "Original", "Nothing from the past. Seasonal events back on Auto.", "Nada do passado. Sazonais de volta no Autom\u00e1tico.",
            [], ResetSeasonal: true),
    ];

    public static SeasonalEventDef? SeasonalById(string id) =>
        Seasonal.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    public static PastEventDef? Past(string id) =>
        PastEvents.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));

    public static bool IsKnownPast(string id) => Past(id) is not null;

    public static string? WikiUrl(string? anchor) => anchor is null ? null : WikiBase + "Events#" + anchor;

    public static IReadOnlyList<QuestLink> QuestLinks(PastEventDef e) => QuestLinks(e.Id, e.Quests, []);

    public static IReadOnlyList<QuestLink> QuestLinks(SeasonalEventDef s) =>
        QuestLinks(s.Id, s.GatedQuests, s.RecreatedQuests is { } key ? QuestCatalog.EnglishNames(QuestCatalog.RecreatedFor(key)) : []);

    /// <summary>The event's whole quest list from the wiki, plus the quests PPES unlocks, gated ones first.</summary>
    private static IReadOnlyList<QuestLink> QuestLinks(string eventId, IEnumerable<string> gatedIds, IEnumerable<string> recreatedNames)
    {
        var gated = gatedIds.Where(QuestTitles.ContainsKey).Select(id => QuestTitles[id]).ToList();
        var names = gated.Concat(WikiEventQuests.ByEvent.GetValueOrDefault(eventId) ?? []).Concat(recreatedNames).Distinct(StringComparer.OrdinalIgnoreCase);
        return names
            .Select(name => new QuestLink(name, WikiBase + Uri.EscapeDataString(name.Replace(' ', '_')), gated.Contains(name, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }
}
