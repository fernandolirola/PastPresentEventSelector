using PPES.Factions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace PPES.Events;

public enum EventTag
{
    /// <summary>SPT already has everything; PPES only switches it on.</summary>
    Ready,
    /// <summary>Recreated with server data (spawns, globals, traders).</summary>
    Simulated,
    /// <summary>Dormant game code switched back on; may not work on every map.</summary>
    Experimental,
}

public sealed record SeasonalEventDef(string Id, string NameEn, string NamePt, string[] SptNames, string DescEn, string DescPt);

/// <param name="Gated">SPT hides this quest outside the event, so it is the one PPES unlocks; the rest of the chain follows from it.</param>
public sealed record QuestLink(string Name, string Url, bool Gated);

public sealed record PastEventDef(
    string Id,
    string Name,
    string When,
    EventTag Tag,
    string DescEn,
    string DescPt,
    string[] QuestIds,
    Action<LoadContext>? OnLoad = null,
    Action<RaidContext>? OnRaid = null,
    string? Group = null,
    ModNeed? Needs = null,
    ModNeed? AlsoNeeds = null)
{
    public IEnumerable<ModNeed> AllNeeds => new[] { Needs, AlsoNeeds }.OfType<ModNeed>();

    /// <summary>Load-time edits and quest unlocks only take effect on server start; raid edits apply from the next raid.</summary>
    public bool NeedsRestart => OnLoad is not null || Quests.Length > 0;

    /// <summary>The event's quests: its own list plus the recreated ones EventQuests loaded for it.</summary>
    public string[] Quests => EventCatalog.ExtraQuests.TryGetValue(Id, out var extra) ? [.. QuestIds, .. extra] : QuestIds;
}

public static class EventCatalog
{
    internal static readonly Dictionary<string, List<string>> ExtraQuests = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Event key of the Halloween 2026 quests, which belong to the Halloween card rather than a past event.</summary>
    public const string Halloween2026 = "halloween2026";

    private const string CustomsZones = "ZoneDormitory,ZoneGasStation,ZoneScavBase";
    // ZoneRailStrorage is SPT's stock Reserve zone key; keep its spelling for game compatibility.
    private const string ReserveZones = "ZoneRailStrorage,ZonePTOR2,ZoneBarrack,ZoneSubStorage";
    private const string LabZones = "BotZoneFloor1,BotZoneFloor2";
    private const string BlackDivLab = "BotZoneFloor2,BotZoneFloor1,BotZoneBasement";

    // spread-out zones per map for the faction groups, so they do not all start on top of the players
    private static readonly Dictionary<string, string> FactionZones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bigmap"] = "ZoneCustoms,ZoneFactoryCenter,ZoneOldAZS",
        ["woods"] = "ZoneBrokenVill,ZoneClearVill,ZoneRoad",
        ["shoreline"] = "ZoneSmuglers,ZoneBusStation,ZoneRailWays",
        ["interchange"] = "ZoneTrucks,ZonePowerStation,ZoneOLIPark",
        ["lighthouse"] = "Zone_Containers,Zone_Village,Zone_LongRoad",
        ["tarkovstreets"] = "ZoneConstruction,ZoneFactory,ZoneSW01",
        ["sandbox_high"] = "ZoneSandbox",
        ["rezervbase"] = "ZoneBunkerStorage,ZonePTOR1,ZoneRailStrorage",
    };

    private static readonly string[] Smugglers2024Maps = ["bigmap", "woods", "shoreline", "interchange", "lighthouse", "tarkovstreets", "rezervbase", "sandbox_high"];
    private static readonly string[] Smugglers2026Maps = ["bigmap", "woods", "shoreline", "interchange", "lighthouse", "tarkovstreets", "sandbox_high"];
    private static readonly string[] PillagerMaps = ["bigmap", "interchange", "lighthouse", "shoreline", "woods"];

    // home map of each boss, used to bring them to other maps
    private static readonly (string Boss, string Home)[] ScavBosses =
    [
        ("bossBully", "bigmap"),
        ("bossKojaniy", "woods"),
        ("bossSanitar", "shoreline"),
        ("bossGluhar", "rezervbase"),
        ("bossKilla", "interchange"),
        ("bossTagilla", "factory4_day"),
        ("bossKolontay", "tarkovstreets"),
    ];

    private static readonly string[] MainTraders =
    [
        "54cb50c76803fa8b248b4571", // Prapor
        "54cb57776803fa99248b456e", // Therapist
        "58330581ace78e27b8b10cee", // Skier
        "5935c25fb3acc3127c3d8cd9", // Peacekeeper
        "5a7c2eca46aef81a7ca2145d", // Mechanic
        "5ac3b934156ae10c4430e83c", // Ragman
        "5c0647fdd443bc2504c2d371", // Jaeger
        "6617beeaa9cfa777ca915b7c", // Ref
    ];

    private const string WikiBase = "https://escapefromtarkov.fandom.com/wiki/";

    // section anchors of the wiki Events page, checked against the wiki API
    private static readonly Dictionary<string, string> WikiSections = new(StringComparer.OrdinalIgnoreCase)
    {
        ["halloween"] = "Halloween_2025_(22_October_2025)",
        ["christmas"] = "Khorovod_(26_December_2024)",
        ["newyears"] = "Kolotun_(24_December_2025)",
        ["aprilfools"] = "April_Fools_(1_April_2026)",
        ["shiftingBalance"] = "Shifting_Balance_(8_September_2026)",
        ["lighthouseAirdrops"] = "Lighthouse_Airdrops_(26_September_2026)",
        ["bonusExp"] = "Bonus_EXP_weekend_(24_July_2026)",
        ["wildWildWoods"] = "Wild_Wild_Woods_(25_June_2026)",
        ["tarkovHospitality"] = "Tarkov_Hospitality_(5_June_2026)",
        ["fullSpeedAhead"] = "Full_Speed_Ahead_(15_May_2026)",
        ["arenaCleanupFactory"] = "Arena_Cleanup_Crew_on_Factory_(1_May_2026)",
        ["blackout"] = "Blackout_(14_July_2026)",
        ["casusBelli"] = "Casus_belli_(9_April_2026)",
        ["smugglers2026"] = "Smugglers_2026_(12_March_2026)",
        ["smugglers2024"] = "Smugglers_2024_(6_December_2024)",
        ["downTheRabbitHole"] = "Down_the_Rabbit_Hole_(2_December_2023)",
        ["weaponCaseAirdrops"] = "Weapon_Case_Airdrops_(19_June_2023)",
        ["reshalaVsGlukhar"] = "Reshala_vs_Glukhar_(8_May_2026)",
        ["permanentBossPresence"] = "Permanent_Boss_Presence_(29_July_2026)",
        ["softcore"] = "Softcore_(8_October_2025)",
        ["targradTales"] = "Targrad_Tales_(20_August_2025)",
        ["cultistPilgrimage"] = "Cultist_Pilgrimage_(18_December_2024)",
        ["mortarStrike"] = "Mortar_Strike_(26_September_2024)",
        ["closedFlea"] = "Closed_Flea_Market_(13_August_2024)",
        ["supplyAndDemand"] = "Supply_and_Demand_(2_August_2024)",
        ["armsRace"] = "Arms_Race_(16_July_2024)",
        ["keyToSalvation"] = "A_Key_to_Salvation_(24_June_2024)",
        ["hustle"] = "Hustle_(31_May_2024)",
        ["tarkovMystery"] = "The_Tarkov_Mystery_(15_May_2024)",
        ["brothersReunited"] = "Brothers_Reunited_(17_April_2024)",
        ["freeLabs"] = "Free_Lab_Access_Event_(12_May_2024)",
        ["unlockedTraders"] = "Unlocked_Traders_%26_Discounts_(25_December_2024)",
        ["ammoShortage"] = "Ammo_Shortage_(25_November_2023)",
        ["fleaPromotion"] = "Flea_Market_Promotion_(22_December_2023)",
        ["instantHideout"] = "Instant_Hideout_Building_(23_December_2023)",
        ["bossesAndRogues"] = "Bosses_%26_Rogues_Wander_(3_July_2023)",
        ["terraGroupTrail"] = "The_TerraGroup_Trail_(28_July_2023)",
        ["hardcoreMode"] = "Hardcore_Mode_(7_July_2023)",
        ["bloodhounds"] = "Bloodhounds_Reveal_(9_June_2023)",
        ["bandages"] = "Bandages_Event_(7_April_2023)",
        ["scavOnLabs"] = "Scav_On_Labs_Event_(4_December_2022)",
        ["boostedSkills"] = "Boosted_Skill_Leveling_Event_(17_December_2022)",
        ["sanitarEvent"] = "Sanitar_Event_(11_November_2022)",
        ["airdropTraffic"] = "Airdrops_(19_March_2026)",
        ["meetingOfBosses"] = "Meeting_of_the_Bosses_(Day_3)",
        ["heatwave"] = "Heatwave_(29_July_2021)",
        ["bossesGathering"] = "June_2021_Pre-Wipe_Event_(20_June_2021)",
    };

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
        ["670404a2ea1caa8f2e0be106"] = "Don’t Believe Your Eyes",
        ["67190f6c1b3f4964d90d71e9"] = "Global Threat",
        ["675031be899713ccad00060c"] = "Christmas Dinner",
        ["675031d3884e1da4a90b3bc9"] = "Chilly",
        ["6750320e23fc8fd9cc087d14"] = "Holiday Beyond the Means",
        ["67503247622398376d0b57cd"] = "Khorovod",
        ["675031e1f300496cc4104450"] = "Hide in Plain Sight",
        ["675031f57775aada6b0f96a1"] = "Cheer Up",
        ["674647f38466ebb03408b291"] = "That’s a Great Plan, Walter",
        ["6746480cd0b2f8eb9b034e3e"] = "Chop the Thieving Hand",
        ["6746480040ea3d1d4f0e5d51"] = "Shell Game",
        ["69ae8e9d8d5952d853d791a1"] = "Bulldogs Under The Rug",
        ["f4411dd1f99ce3678f7bd579"] = "Accept Delivery",
        ["34f3c3bb54f8d18fd4a320c3"] = "Thirty Pieces of Silver",
        ["0c472b7ba2915b42ba295a26"] = "Restocking Supplies",
        ["68a6dfb24982a975f038440b"] = "Root of All Evil",
    };

    // seasonal quests SPT shows only while the event is on (configs/quest.json)
    private static readonly Dictionary<string, string[]> SeasonalQuestIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["halloween"] = ["66e3e2ee2136472d220bcb36", "670404a2ea1caa8f2e0be106", "67190f6c1b3f4964d90d71e9"],
        ["christmas"] = ["675031be899713ccad00060c", "675031d3884e1da4a90b3bc9", "6750320e23fc8fd9cc087d14", "67503247622398376d0b57cd", "675031e1f300496cc4104450", "675031f57775aada6b0f96a1"],
    };

    public static string? WikiUrl(string eventId) =>
        WikiSections.TryGetValue(eventId, out var anchor) ? WikiBase + "Events#" + anchor : null;

    /// <summary>The event's whole quest list from the wiki, plus the quests PPES unlocks, gated ones first.</summary>
    public static IReadOnlyList<QuestLink> EventQuests(string eventId, IEnumerable<string> gatedIds)
    {
        var gated = gatedIds.Where(QuestTitles.ContainsKey).Select(id => QuestTitles[id]).ToList();
        var names = gated.Concat(WikiEventQuests.ByEvent.GetValueOrDefault(eventId) ?? [])
            .Concat(eventId == "halloween" ? Halloween2026Quests : []).Distinct(StringComparer.OrdinalIgnoreCase);
        return names
            .Select(name => new QuestLink(name, WikiBase + Uri.EscapeDataString(name.Replace(' ', '_')), gated.Contains(name, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }

    public static IReadOnlyList<QuestLink> SeasonalQuests(string seasonalId) =>
        EventQuests(seasonalId, SeasonalQuestIds.GetValueOrDefault(seasonalId) ?? []);

    // Halloween 2026 quests recreated by PPES (tools/questgen/events_halloween.py)
    private static readonly string[] Halloween2026Quests = ["Halloween: Rumors", "Halloween: The Best Defense", "Halloween: Follow-Up Examination",
        "Halloween: Unexpected Help", "Halloween: Initial Examination"];

    public static readonly IReadOnlyList<SeasonalEventDef> Seasonal =
    [
        new("halloween", "Halloween", "Halloween", ["halloween"],
            "Zombies, cultist bosses, pumpkins, Tagilla with a scythe and the summoning ritual. Halloween quests unlock. The 2026 outbreak part spreads the infected to every map and adds the five Halloween 2026 quests.",
            "Zumbis, chefes cultistas, abóboras, Tagilla com foice e o ritual de invocação. Libera as quests de Halloween. A parte Surto de 2026 espalha os infectados por todos os mapas e adiciona as cinco quests de Halloween 2026."),
        new("christmas", "Christmas", "Natal", ["christmas", "christmas January"],
            "Santa on the maps, a decorated hideout, the Khorovod tree event, winter gear and the Christmas quests.",
            "Papai Noel nos mapas, hideout decorado, evento da árvore (Khorovod), roupas de inverno e as quests de Natal."),
        new("newyears", "New Year", "Ano Novo", ["newyears"],
            "Decorated hideout, Khorovod and Santa for New Year's Day.",
            "Hideout decorado, Khorovod e Papai Noel no dia de Ano Novo."),
        new("aprilfools", "April Fools", "1º de abril", ["April fools"],
            "Winter weather, Santa and his gifts, the summoning ritual and the Halloween cultists, all at once.",
            "Clima de inverno, Papai Noel e seus presentes, o ritual de invocação e os cultistas do Halloween, tudo junto."),
    ];

    public static readonly IReadOnlyList<PastEventDef> PastEvents =
    [
        new("shiftingBalance", "Shifting Balance", "Sep 2026", EventTag.Simulated,
            "Glukhar moves from Reserve to the water treatment plant on Lighthouse and is there in every raid. Unlocks the recreated quest chain Fog of War.",
            "O Glukhar sai da Reserve para a estação de tratamento de água da Lighthouse e aparece em toda raid. Libera a cadeia de quests recriada Fog of War.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("rezervbase")) ctx.Remove("bossGluhar");
                if (ctx.Is("lighthouse")) ctx.AddFrom("rezervbase", "bossGluhar", "Zone_TreatmentContainers", 100);
            },
            Group: "glukhar"),

        new("lighthouseAirdrops", "Lighthouse Airdrops", "Sep 2026", EventTag.Simulated,
            "Lighthouse gets an airdrop in every raid, sometimes two. The extra valuable loot is not included.",
            "A Lighthouse recebe airdrop em toda raid, às vezes dois. O loot extra valioso não está incluído.",
            [],
            OnRaid: ctx =>
            {
                if (!ctx.Is("lighthouse")) return;
                foreach (var airdrop in ctx.Location.AirdropParameters ?? [])
                {
                    airdrop.PlaneAirdropChance = 1;
                    airdrop.PlaneAirdropMax = Math.Max(airdrop.PlaneAirdropMax ?? 1, 2);
                }
            }),

        new("scourgeIncursion", "Scourge Incursion", "Oct 2026", EventTag.Simulated,
            "Scourge, an arena fighter with his own guards, is in every Lab raid. Without this event he keeps the SCOURGE mod's own spawn chance. The quests, items and voice come from the SCOURGE mod. Needs the SCOURGE mod.",
            "O Scourge, um lutador da arena com os próprios guardas, aparece em toda raid na Lab. Sem este evento ele segue a chance de spawn do próprio mod SCOURGE. As quests, itens e a voz vêm do mod SCOURGE. Precisa do mod SCOURGE.",
            [],
            OnRaid: ctx =>
            {
                // without the mod the game does not know this boss and the raid fails to load
                if (!ctx.Is("laboratory") || !ModRequirements.ScourgeLoaded) return;
                Effects.ForceBoss(ctx, ModRequirements.ScourgeBoss, "followerScourgeEventLab", "4", LabZones);
            },
            Needs: new(ModRequirements.Scourge)),

        new("blackout", "Blackout", "Jul 2026", EventTag.Simulated,
            "Wedge and his Black Division men are in every Lab raid, with another Black Division team at the gates. With Vultify's Blackout mod, every Lab raid also goes dark, with its gate-only extracts, keypad codes and arsenal key. Unlocks the recreated quest chain Wrench and Waterboarding. Needs the Black Division mod.",
            "O Wedge e os homens da Black Division aparecem em toda raid na Lab, com outro time da Black Division nos portões. Com o mod Blackout do Vultify, toda raid na Lab também fica no escuro, com só os portões como saída, códigos nos teclados e a chave do arsenal. Libera a cadeia de quests recriada Wrench and Waterboarding. Precisa do mod Black Division.",
            [],
            OnRaid: ctx =>
            {
                if (!ctx.Is("laboratory")) return;
                VultifyBlackout.Force(ctx);
                if (!FactionCatalog.BlackDivision) return;
                Effects.ForceBoss(ctx, "bossWedge", "blackDivIb", "3,3,3,4,4,6", BlackDivLab);
                Effects.AddGroup(ctx, "blackDivAssault", "2,3", "BotZoneGate1,BotZoneGate2", 100);
            },
            Needs: new(ModRequirements.BlackDivision),
            AlsoNeeds: new(ModRequirements.Blackout, "the darkness, the gate-only extracts and the keypad codes", "o apagão, a saída só pelos portões e os códigos dos teclados")),

        new("bonusExp", "Bonus EXP Weekend", "May to Jul 2026", EventTag.Simulated,
            "You earn about 25% more experience from raids.",
            "Você ganha cerca de 25% a mais de experiência nas raids.",
            [],
            OnLoad: ctx =>
            {
                var end = ctx.Globals.Configuration!.Exp!.MatchEnd!;
                end.SurvivedMultiplier *= 1.25;
                end.RunnerMultiplier *= 1.25;
                end.MiaMultiplier *= 1.25;
                end.KilledMultiplier *= 1.25;
                end.LeftMultiplier *= 1.25;
            }),

        new("wildWildWoods", "Wild Wild Woods", "Jun 2026", EventTag.Simulated,
            "Shturman is in every Woods raid, the Scav cooldown is four times shorter and Woods has half again as much loose loot and a quarter more in containers. Unlocks the recreated quest Wild Wild Woods. The barters are not included.",
            "O Shturman aparece em toda raid na Woods, o tempo de espera do Scav fica quatro vezes menor e a Woods tem metade a mais de loot solto e um quarto a mais nos contêineres. Libera a quest recriada Wild Wild Woods. As trocas não estão incluídas.",
            [],
            OnLoad: ctx =>
            {
                ctx.Globals.Configuration!.SavagePlayCooldown /= 4;
                Effects.ScaleLoot(ctx, "woods", 1.5, 1.25);
            },
            OnRaid: ctx =>
            {
                if (ctx.Is("woods")) ctx.SetChance("bossKojaniy", 100);
            }),

        new("tarkovHospitality", "Tarkov Hospitality", "Jun 2026", EventTag.Simulated,
            "Reshala, Sanitar and Shturman are in every raid on their maps, the Goons stay away, as in the PvE version, and Ref's Cleanup Crew holds the Customs dorms, the Shoreline smugglers' base and the Woods village. In the live event the crew ignored PMCs; here they fight everyone. Unlocks the recreated quest chain Guardian of Nature.",
            "Reshala, Sanitar e Shturman aparecem em toda raid nos seus mapas, os Goons somem, como na versão PvE, e a Cleanup Crew do Ref ocupa os dormitórios da Customs, a base dos contrabandistas da Shoreline e a vila da Woods. No evento original eles ignoravam PMCs; aqui eles atacam todo mundo. Libera a cadeia de quests recriada Guardian of Nature.",
            [],
            OnRaid: ctx =>
            {
                ctx.Remove("bossKnight");
                if (ctx.Is("bigmap"))
                {
                    ctx.SetChance("bossBully", 100);
                    Effects.AddGroup(ctx, FactionCatalog.CleanupCrew, "2,3", "ZoneDormitory", 100);
                }
                if (ctx.Is("shoreline"))
                {
                    ctx.SetChance("bossSanitar", 100);
                    Effects.AddGroup(ctx, FactionCatalog.CleanupCrew, "2,3", "ZoneSmuglers", 100);
                }
                if (ctx.Is("woods"))
                {
                    ctx.SetChance("bossKojaniy", 100);
                    Effects.AddGroup(ctx, FactionCatalog.CleanupCrew, "2,3", "ZoneBrokenVill", 100);
                }
            },
            Group: "goons"),

        new("fullSpeedAhead", "Full Speed Ahead", "May 2026", EventTag.Simulated,
            "Rogues patrol Shoreline and Reserve, Glukhar is in every Reserve raid and the Goons only roam Shoreline, from the gas station to the power station. Unlocks the recreated quest chain No Smoke Without Fire.",
            "Rogues patrulham a Shoreline e a Reserve, o Glukhar aparece em toda raid na Reserve e os Goons só andam pela Shoreline, do posto de gasolina à hidrelétrica. Libera a cadeia de quests recriada No Smoke Without Fire.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap", "woods", "lighthouse")) ctx.Remove("bossKnight");
                if (ctx.Is("rezervbase"))
                {
                    ctx.AddFrom("rezervbase", "bossGluhar", null, 100);
                    Effects.AddGroup(ctx, "exUsec", "2,3", ReserveZones, 100);
                }
                if (ctx.Is("shoreline"))
                {
                    ctx.Remove("bossKnight");
                    ctx.AddFrom("bigmap", "bossKnight", "ZoneGasStation,ZonePowerStation", 100);
                    Effects.AddGroup(ctx, "exUsec", "2,3", "ZoneMeteoStation,ZonePowerStation", 100);
                }
            },
            Group: "goons"),

        new("arenaCleanupFactory", "Arena Cleanup Crew on Factory", "May 2026", EventTag.Simulated,
            "Tagilla and Killa team up in every Day Factory raid, and two Cleanup Crew groups of 3 or 4 take the place of the regular Scavs.",
            "Tagilla e Killa juntos em toda raid na Factory de dia, e dois grupos de 3 ou 4 da Cleanup Crew tomam o lugar dos Scavs normais.",
            [],
            OnRaid: ctx =>
            {
                if (!ctx.Is("factory4_day")) return;
                ctx.SetChance("bossTagilla", 100);
                ctx.AddFrom("interchange", "bossKilla", "BotZone", 100);
                if (Effects.AddGroup(ctx, FactionCatalog.CleanupCrew, "2,3", "BotZone", 100, groups: 2))
                    ctx.Location.Waves?.RemoveAll(w => w.WildSpawnType == WildSpawnType.assault);
            }),

        new("reshalaVsGlukhar", "Reshala vs Glukhar", "May 2026", EventTag.Simulated,
            "Reshala is in every Customs raid and Glukhar moves from Reserve to Customs to fight him.",
            "Reshala aparece em toda raid na Customs e o Glukhar sai da Reserve para enfrentá-lo lá.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("rezervbase")) ctx.Remove("bossGluhar");
                if (!ctx.Is("bigmap")) return;
                ctx.SetChance("bossBully", 100);
                ctx.AddFrom("rezervbase", "bossGluhar", CustomsZones, 100);
            },
            Group: "glukhar"),

        new("casusBelli", "Casus belli", "Apr 2026", EventTag.Simulated,
            "Pillagers, a new enemy faction, roam Customs, Interchange, Lighthouse, Shoreline and Woods in groups of 3 or 4. Unlocks the recreated quest chains Casus belli (Ref) and Short on Hands (Skier), and Pillagers often wear painted CQCM masks.",
            "Os Pillagers, uma facção inimiga nova, andam em grupos de 3 ou 4 pela Customs, Interchange, Lighthouse, Shoreline e Woods. Libera as cadeias de quests recriadas Casus belli (Ref) e Short on Hands (Skier), e os Pillagers costumam usar máscaras CQCM pintadas.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is(PillagerMaps)) Effects.AddFactionGroup(ctx, FactionCatalog.Pillager, "2,3");
            }),

        new("smugglers2026", "Smugglers (2026)", "Mar 2026", EventTag.Simulated,
            "Smugglers come back in groups of 3 or 4 on Customs, Woods, Shoreline, Interchange, Lighthouse, Streets and Ground Zero 21+, every Scav boss is on Reserve and airdrops carry sealed weapon cases. Unlocks the recreated quest chain Bulldogs Under The Rug. The flare-called drops are not included.",
            "Os Smugglers voltam em grupos de 3 ou 4 na Customs, Woods, Shoreline, Interchange, Lighthouse, Streets e Ground Zero 21+, todo chefe Scav aparece na Reserve e os airdrops trazem caixas lacradas de armas. Libera a cadeia de quests recriada Bulldogs Under The Rug. Os airdrops chamados com sinalizador não estão incluídos.",
            [],
            OnLoad: ctx => Effects.EnsureWeaponCrates(ctx, 1, 2),
            OnRaid: ctx =>
            {
                if (ctx.Is(Smugglers2026Maps)) Effects.AddFactionGroup(ctx, FactionCatalog.Smuggler, "2,3");
                if (ctx.Is("rezervbase")) Effects.GatherScavBosses(ctx, "rezervbase", ReserveZones);
            },
            Group: "smugglers"),

        new("permanentBossPresence", "Permanent Boss Presence", "2023 to 2026", EventTag.Simulated,
            "Every boss spawns on its own maps in every raid (Partisan keeps his own rules). Also unlocks the quest Hint.",
            "Todo chefe aparece nos seus mapas em toda raid (o Partisan segue as regras dele). Também libera a quest Hint.",
            ["647710905320c660d91c15a5"],
            OnRaid: ctx =>
            {
                foreach (var spawn in ctx.Location.BossLocationSpawn ?? [])
                    if (spawn.BossName is { } name && name.StartsWith("boss", StringComparison.OrdinalIgnoreCase)
                        && name != "bossPartisan" && string.IsNullOrEmpty(spawn.TriggerId))
                        spawn.BossChance = 100;
            }),

        new("softcore", "Softcore", "Oct 2025", EventTag.Simulated,
            "The Lab and the Labyrinth need no keycard and you can carry 50% more before being overweight. Unlocks the recreated quest Staying Light.",
            "A Lab e o Labirinto não pedem cartão e você carrega 50% a mais antes de ficar com sobrepeso. Libera a quest recriada Staying Light.",
            [],
            OnLoad: ctx =>
            {
                Effects.RemoveAccessKeys(ctx, "laboratory", "labyrinth");
                Effects.ScaleOverweight(ctx, 1.5);
            }),

        new("targradTales", "Targrad Tales", "Aug 2025", EventTag.Ready,
            "Unlocks the quest chain Friend from Norvinsk.",
            "Libera a cadeia de quests Friend from Norvinsk.",
            ["6863cf925eaeb676d50a9651"]),

        new("cultistPilgrimage", "Cultist Pilgrimage", "Dec 2024", EventTag.Simulated,
            "Cultists are in every night raid on Customs, Woods, Shoreline and Night Factory. Also unlocks the quest Failed Setup.",
            "Cultistas em toda raid noturna na Customs, Woods, Shoreline e Factory à noite. Também libera a quest Failed Setup.",
            ["64916da7ad4e722c106f2345"],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap", "woods", "shoreline", "factory4_night")) ctx.SetChance("sectantPriest", 100);
            }),

        new("smugglers2024", "Smugglers (2024)", "Dec 2024", EventTag.Simulated,
            "Smugglers, a new enemy faction, show up in groups of 3 on every map except Ground Zero for low levels and the Lab, carrying locked cases, case keys and contraband. Unlocks the recreated quest chain That’s a Great Plan, Walter.",
            "Os Smugglers, uma facção inimiga nova, aparecem em grupos de 3 em todo mapa, menos o Ground Zero de nível baixo e a Lab, levando maletas trancadas, chaves de maleta e contrabando. Libera a cadeia de quests recriada That’s a Great Plan, Walter.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is(Smugglers2024Maps)) Effects.AddFactionGroup(ctx, FactionCatalog.Smuggler, "2");
            },
            Group: "smugglers"),

        new("mortarStrike", "Mortar Strike", "Sep 2024", EventTag.Experimental,
            "Mortar barrages hit Customs, Woods, Shoreline and Reserve, with a special airdrop carrying radar parts. Unlocks the quest Into the Inferno.",
            "Bombardeios de morteiro na Customs, Woods, Shoreline e Reserve, com um airdrop especial com peças do radar. Libera a quest Into the Inferno.",
            ["66e01aca214f88109006a4b5"],
            OnLoad: ctx => Effects.EnableMortarStrike(ctx, ["bigmap", "Woods", "Shoreline", "RezervBase"])),

        new("closedFlea", "Closed Flea Market", "Aug 2024", EventTag.Simulated,
            "The flea market only opens at level 99, so in practice it is closed.",
            "O mercado só abre no nível 99, ou seja, na prática fica fechado.",
            [],
            OnLoad: ctx => ctx.Globals.Configuration!.RagFair!.MinUserLevel = 99,
            Group: "flea"),

        new("supplyAndDemand", "Supply and Demand", "Aug 2024", EventTag.Ready,
            "Unlocks the quest Gunsmith - Special Order. Mechanic's price increase is not included.",
            "Libera a quest Gunsmith - Special Order. O aumento de preços do Mechanic não está incluído.",
            ["66a74c628410476dd65543be"]),

        new("armsRace", "Arms Race", "Jul 2024", EventTag.Ready,
            "Unlocks the quest Import Control and its recreated follow-ups. The arms dealers Gus and Basmach are on Streets in every raid.",
            "Libera a quest Import Control e as continuações recriadas. Os traficantes de armas Gus e Basmach aparecem em toda raid na Streets.",
            ["668bcccc167d507eb01a268b"],
            OnRaid: ctx =>
            {
                if (!ctx.Is("tarkovstreets")) return;
                foreach (var dealer in new[] { FactionCatalog.Gus, FactionCatalog.Basmach })
                    if (Effects.AddGroup(ctx, dealer, "2", FactionZones["tarkovstreets"], 100))
                        ctx.Location.BossLocationSpawn!.Last().BossEscortType = "followerKolontaySecurity";
            }),

        new("keyToSalvation", "A Key to Salvation", "Jun 2024", EventTag.Ready,
            "Unlocks the quest A Key to Salvation.",
            "Libera a quest A Key to Salvation.",
            ["6672ec2a2b6f3b71be794cc5"]),

        new("hustle", "Hustle", "May 2024", EventTag.Simulated,
            "All Scav bosses, the Goons and the Cultists gather on Reserve in every raid. Unlocks the quest Hustle.",
            "Todos os chefes Scav, os Goons e os Cultistas se reúnem na Reserve em toda raid. Libera a quest Hustle.",
            ["6658a15615cbb1b2c6014d5b"],
            OnRaid: ctx =>
            {
                if (!ctx.Is("rezervbase")) return;
                Effects.GatherScavBosses(ctx, "rezervbase", ReserveZones);
                ctx.AddFrom("bigmap", "bossKnight", ReserveZones, 100);
                ctx.AddFrom("bigmap", "sectantPriest", ReserveZones, 100);
            }),

        new("tarkovMystery", "The Tarkov Mystery", "May 2024", EventTag.Simulated,
            "Peacekeeper's troops patrol Shoreline and Skier's troops patrol Customs in every raid. Unlocks the quest The Tarkov Mystery and its recreated follow-up chain, with a Skier or Peacekeeper route, and encrypted flash drives turn up in drawers, jackets and computers.",
            "Tropas do Peacekeeper na Shoreline e do Skier na Customs em toda raid. Libera a quest The Tarkov Mystery e a continuação recriada, com uma rota do Skier ou do Peacekeeper, e pendrives criptografados aparecem em gavetas, jaquetas e computadores.",
            ["6603fe74e773dcf3b0099f88"],
            OnLoad: ctx => Effects.AddStaticLoot(ctx, "660bbc47c38b837877075e47", 2),
            OnRaid: ctx =>
            {
                if (ctx.Is("shoreline")) ctx.SetChance("peacemaker", 100);
                if (ctx.Is("bigmap"))
                    Effects.AddGroup(ctx, "skier", "2,2,3,3,3,4,4,5", "ZoneScavBase,ZoneDormitory,ZoneFactoryCenter,ZoneCustoms,ZoneBrige", 100);
            }),

        new("brothersReunited", "Brothers Reunited", "Apr 2024", EventTag.Simulated,
            "Tagilla joins Killa at the Kiba store on Interchange in every raid.",
            "O Tagilla se junta ao Killa na loja Kiba, na Interchange, em toda raid.",
            [],
            OnRaid: ctx =>
            {
                if (!ctx.Is("interchange")) return;
                ctx.SetChance("bossKilla", 100);
                ctx.AddFrom("factory4_day", "bossTagilla", "ZoneCenter,ZoneCenterBot", 100);
            }),

        new("freeLabs", "Free Labs", "2022 to 2024", EventTag.Simulated,
            "Entering the Lab does not need a keycard.",
            "Entrar na Lab não precisa de cartão.",
            [],
            OnLoad: ctx => Effects.RemoveAccessKeys(ctx, "laboratory")),

        new("unlockedTraders", "Unlocked Traders", "2023, 2024", EventTag.Simulated,
            "The main traders are at loyalty level 4 regardless of your progress. Turning it off returns your real levels. Discounts are not included.",
            "Os traders principais ficam no nível de lealdade 4, independente do seu progresso. Ao desligar, voltam os seus níveis reais. Descontos não estão incluídos.",
            [],
            OnLoad: ctx => Effects.UnlockTraders(ctx, MainTraders)),

        new("downTheRabbitHole", "Down the Rabbit Hole", "Dec 2023", EventTag.Simulated,
            "All Scav bosses gather at the LexOs car dealership on Streets in every raid. Unlocks the recreated quest chain Down the Rabbit Hole.",
            "Todos os chefes Scav se reúnem na concessionária LexOs, na Streets, em toda raid. Libera a cadeia de quests recriada Down the Rabbit Hole.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("tarkovstreets")) Effects.GatherScavBosses(ctx, "tarkovstreets", "ZoneCarShowroom");
            }),

        new("ammoShortage", "Ammo Shortage", "Nov 2023", EventTag.Ready,
            "Unlocks the quest The Punisher - Harvest. The flea and trader ammo limits are not included.",
            "Libera a quest The Punisher - Harvest. As restrições de munição no flea e nos traders não estão incluídas.",
            ["655e427b64d09b4122018228"]),

        new("fleaPromotion", "Flea Market Promotion", "Dec 2023", EventTag.Simulated,
            "The flea market opens at any level and its fees drop to a quarter.",
            "O mercado abre em qualquer nível e as taxas caem para um quarto.",
            [],
            OnLoad: ctx =>
            {
                var flea = ctx.Globals.Configuration!.RagFair!;
                flea.MinUserLevel = 1;
                flea.CommunityTax *= 0.25f;
                flea.CommunityItemTax *= 0.25f;
                flea.CommunityRequirementTax *= 0.25f;
            },
            Group: "flea"),

        new("instantHideout", "Instant Hideout Building", "2022 to 2024", EventTag.Simulated,
            "Hideout modules and upgrades are built instantly. Their item requirements stay the same.",
            "Módulos e melhorias do hideout ficam prontos na hora. Os itens exigidos continuam os mesmos.",
            [],
            OnLoad: Effects.InstantHideout),

        new("bossesAndRogues", "Bosses & Rogues Wander", "Jul 2023", EventTag.Simulated,
            "The Goons are in every Day Factory raid. Unlocks the quests Tourists and Hustle (2023).",
            "Os Goons aparecem em toda raid na Factory de dia. Libera as quests Tourists e Hustle (2023).",
            ["649570491bb4d158bc4d0168", "649567f359eab30d1b7c9585"],
            OnRaid: ctx =>
            {
                if (ctx.Is("factory4_day")) ctx.AddFrom("bigmap", "bossKnight", "BotZone", 100);
            }),

        new("terraGroupTrail", "The TerraGroup Trail", "Jul 2023", EventTag.Simulated,
            "The Goons leave their maps to guard Sanitar on Shoreline, and both are there in every raid. Unlocks the recreated quest chain The TerraGroup Trail.",
            "Os Goons saem dos seus mapas para proteger o Sanitar na Shoreline, e os dois aparecem em toda raid. Libera a cadeia de quests recriada The TerraGroup Trail.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap", "woods", "lighthouse")) ctx.Remove("bossKnight");
                if (!ctx.Is("shoreline")) return;
                ctx.SetChance("bossSanitar", 100);
                ctx.Remove("bossKnight");
                ctx.AddFrom("bigmap", "bossKnight", "ZoneSanatorium1,ZoneSanatorium2", 100);
            },
            Group: "goons"),

        new("hardcoreMode", "Hardcore Mode", "Jul 2023", EventTag.Ready,
            "Unlocks the quest Cocktail Tasting. The Obdolbos changes are not included.",
            "Libera a quest Cocktail Tasting. As mudanças no Obdolbos não estão incluídas.",
            ["649af47d717cb30e7e4b5e26"]),

        new("bloodhounds", "Bloodhounds Reveal", "Jun 2023", EventTag.Simulated,
            "The Bloodhounds are in every raid on Customs, Woods and Shoreline. Unlocks the quest Bloodhounds.",
            "Os Bloodhounds aparecem em toda raid na Customs, Woods e Shoreline. Libera a quest Bloodhounds.",
            ["64764abcd125ab430a14ccb5"],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap", "woods")) ctx.SetChance("arenaFighterEvent", 100);
                if (ctx.Is("shoreline"))
                    Effects.AddGroup(ctx, "arenaFighterEvent", "3,4", "ZoneStartVillage,ZoneGasStation,ZoneForestGasStation,ZonePowerStation", 100);
            }),

        new("weaponCaseAirdrops", "Weapon Case Airdrops", "Jun 2023", EventTag.Simulated,
            "Every airdrop carries two to four sealed weapon cases. Turn on Airdrop Traffic too to get one in every raid.",
            "Todo airdrop traz de duas a quatro caixas lacradas de armas. Ligue também o Airdrop Traffic para ter um em toda raid.",
            [],
            OnLoad: ctx => Effects.EnsureWeaponCrates(ctx, 2, 4)),

        new("bandages", "Bandages Event", "Apr 2023", EventTag.Ready,
            "Unlocks the community quest Important Patient.",
            "Libera a quest comunitária Important Patient.",
            ["641dbfd7f43eda9d810d7137"]),

        new("scavOnLabs", "Scav on Labs", "Dec 2022", EventTag.Simulated,
            "Glukhar and his guards move from Reserve to the Lab and are there in every raid. Scav access to the Lab is not included.",
            "O Glukhar e seus guardas saem da Reserve para a Lab e aparecem em toda raid. A entrada de Scav na Lab não está incluída.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("rezervbase")) ctx.Remove("bossGluhar");
                if (ctx.Is("laboratory")) ctx.AddFrom("rezervbase", "bossGluhar", LabZones, 100);
            },
            Group: "glukhar"),

        new("boostedSkills", "Boosted Skill Leveling", "Dec 2022", EventTag.Simulated,
            "Character and weapon skills level up ten times faster.",
            "Habilidades do personagem e das armas sobem dez vezes mais rápido.",
            [],
            OnLoad: ctx =>
            {
                var skills = ctx.Globals.Configuration!.SkillsSettings!;
                skills.SkillProgressRate *= 10;
                skills.WeaponSkillProgressRate *= 10;
            }),

        new("sanitarEvent", "Sanitar Event", "Nov 2022", EventTag.Simulated,
            "Sanitar and four or five guards are on Customs in every raid. Unlocks the quest Letters.",
            "O Sanitar e quatro ou cinco guardas aparecem na Customs em toda raid. Libera a quest Letters.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap")) ctx.AddFrom("shoreline", "bossSanitar", CustomsZones, 100, "4,5");
            }),

        new("airdropTraffic", "Airdrop Traffic", "Jun 2022, Mar 2026", EventTag.Simulated,
            "An airdrop comes in every raid on the outdoor maps.",
            "Um airdrop cai em toda raid nos mapas abertos.",
            [],
            OnRaid: ctx =>
            {
                foreach (var airdrop in ctx.Location.AirdropParameters ?? [])
                    airdrop.PlaneAirdropChance = 1;
            }),

        new("meetingOfBosses", "Meeting of the Bosses", "Jun 2022", EventTag.Simulated,
            "All Scav bosses are on Customs in every raid.",
            "Todos os chefes Scav aparecem na Customs em toda raid.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap")) Effects.GatherScavBosses(ctx, "bigmap", CustomsZones);
            }),

        new("heatwave", "Heatwave", "Jul 2021", EventTag.Simulated,
            "You get thirsty twice as fast in raid.",
            "Você fica com sede duas vezes mais rápido na raid.",
            [],
            OnLoad: ctx => ctx.Globals.Configuration!.Health!.Effects!.Existence!.HydrationDamage *= 2),

        new("bossesGathering", "The Bosses Gathering", "Jun 2021", EventTag.Simulated,
            "All Scav bosses are on Reserve in every raid. Unlocks the recreated quest The Bosses gathering.",
            "Todos os chefes Scav aparecem na Reserve em toda raid. Libera a quest recriada The Bosses gathering.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("rezervbase")) Effects.GatherScavBosses(ctx, "rezervbase", ReserveZones);
            }),
    ];

    public static SeasonalEventDef? SeasonalById(string id) =>
        Seasonal.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    public static PastEventDef? Past(string id) =>
        PastEvents.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));

    public static bool IsKnownPast(string id) => Past(id) is not null;

    /// <summary>The mod an event needs as a whole that is not installed, or null when it can run.</summary>
    public static ModRequirement? MissingRequirement(PastEventDef e) =>
        e.AllNeeds.Where(n => !n.Partial && !ModRequirements.IsInstalled(n.Mod)).Select(n => ModRequirements.Get(n.Mod)).FirstOrDefault();

    internal static class Effects
    {
        private static readonly string[] AllMaps = ["bigmap", "woods", "shoreline", "interchange", "lighthouse", "rezervbase", "tarkovstreets",
            "factory4_day", "factory4_night", "laboratory", "sandbox", "sandbox_high"];

        // drawers, jackets and PC blocks
        private static readonly HashSet<string> SmallContainers = ["5675838d4bdc2d95058b456e", "578f87b7245977356274f2cd", "578f8778245977358849a9b5",
            "5914944186f774189e5e76c2", "5937ef2b86f77408a47244b3", "59139c2186f77411564f8e42"];

        /// <summary>Adds an item to small loot containers on every map, at about <paramref name="percent"/>% of each container's picks.</summary>
        public static void AddStaticLoot(LoadContext ctx, string tpl, double percent)
        {
            foreach (var map in AllMaps)
            {
                ctx.Locations.GetLocation(map)?.StaticLoot?.AddTransformer(data =>
                {
                    if (data is null) return data;
                    foreach (var (container, details) in data)
                    {
                        if (!SmallContainers.Contains(container.ToString()) || details.ItemDistribution is not { } dist) continue;
                        if (dist.Any(d => d.Tpl == tpl)) continue;
                        var sum = dist.Sum(d => d.RelativeProbability ?? 0);
                        details.ItemDistribution = dist.Append(new ItemDistribution { Tpl = tpl, RelativeProbability = (float)Math.Max(1, Math.Round(sum * percent / (100 - percent))) }).ToList();
                    }
                    return data;
                });
            }
        }

        /// <summary>Multiplies SPT's loot multipliers for a map, on top of what the user or other mods set.</summary>
        public static void ScaleLoot(LoadContext ctx, string map, double loose, double containers)
        {
            if (ctx.LocationConfig.LooseLootMultiplier.TryGetValue(map, out var l)) ctx.LocationConfig.LooseLootMultiplier[map] = l * loose;
            if (ctx.LocationConfig.StaticLootMultiplier.TryGetValue(map, out var s)) ctx.LocationConfig.StaticLootMultiplier[map] = s * containers;
        }

        public static void EnsureWeaponCrates(LoadContext ctx, int min, int max)
        {
            foreach (var (type, loot) in ctx.AirdropConfig.Loot)
            {
                if (type is "radar" or "toiletPaper" || loot.WeaponCrateCount is null) continue;
                loot.WeaponCrateCount.Min = Math.Max(loot.WeaponCrateCount.Min, min);
                loot.WeaponCrateCount.Max = Math.Max(loot.WeaponCrateCount.Max, max);
            }
        }

        public static void GatherScavBosses(RaidContext ctx, string map, string zones)
        {
            foreach (var (boss, home) in ScavBosses)
                ctx.AddFrom(home, boss, home == map ? null : zones, 100);
        }

        /// <summary>Adds groups of a role until the map has <paramref name="groups"/> of them in these zones. False when the role cannot spawn.</summary>
        public static bool AddGroup(RaidContext ctx, string role, string escortAmount, string zones, double chance, int groups = 1)
        {
            if (FactionCatalog.ByRole(role) is not null && !FactionCatalog.IsReady(role)) return false;
            var existing = ctx.Location.BossLocationSpawn?.Count(b => b.BossName == role && b.BossZone == zones) ?? 0;
            if (existing > 0) ctx.SetChance(role, chance);
            for (var i = existing; i < groups; i++)
            {
                if (ctx.CloneAnySpawn() is not { } spawn) return false;
                spawn.BossName = role;
                spawn.BossEscortType = role;
                spawn.BossEscortAmount = escortAmount;
                spawn.BossDifficulty = "normal";
                spawn.BossEscortDifficulty = "normal";
                spawn.BossZone = zones;
                spawn.BossChance = chance;
                spawn.Supports = [];
                spawn.TriggerId = "";
                spawn.TriggerName = "";
                spawn.Time = -1;
                spawn.IgnoreMaxBots = true;
                ctx.AddSpawn(spawn);
            }
            return true;
        }

        public static void AddFactionGroup(RaidContext ctx, string role, string escortAmount) =>
            AddGroup(ctx, role, escortAmount, FactionZones.GetValueOrDefault(ctx.MapId, ""), 100);

        /// <summary>Makes a boss spawn every raid, keeping a mod's own entry (its trigger included) when there is one.</summary>
        public static void ForceBoss(RaidContext ctx, string boss, string escort, string escortAmount, string zones)
        {
            var spawns = ctx.Location.BossLocationSpawn?.Where(b => string.Equals(b.BossName, boss, StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
            if (spawns.Count > 0)
            {
                foreach (var spawn in spawns) spawn.BossChance = 100;
                return;
            }
            if (AddGroup(ctx, boss, escortAmount, zones, 100))
            {
                var added = ctx.Location.BossLocationSpawn!.Last();
                added.BossEscortType = escort;
                added.ForceSpawn = true;
            }
        }

        public static void RemoveAccessKeys(LoadContext ctx, params string[] maps)
        {
            foreach (var map in maps)
            {
                if (ctx.Locations.GetLocation(map)?.Base is not { } location) continue;
                location.AccessKeys = [];
                location.AccessKeysPvE = [];
            }
        }

        public static void ScaleOverweight(LoadContext ctx, double factor)
        {
            var stamina = ctx.Globals.Configuration!.Stamina!;
            stamina.BaseOverweightLimits = Scale(stamina.BaseOverweightLimits);
            stamina.WalkOverweightLimits = Scale(stamina.WalkOverweightLimits);
            stamina.SprintOverweightLimits = Scale(stamina.SprintOverweightLimits);
            stamina.WalkSpeedOverweightLimits = Scale(stamina.WalkSpeedOverweightLimits);

            Vector3 Scale(Vector3 v) => v with { X = (float)(v.X * factor), Y = (float)(v.Y * factor) };
        }

        public static void UnlockTraders(LoadContext ctx, IEnumerable<string> traderIds)
        {
            foreach (var id in traderIds)
            {
                if (ctx.Traders.GetTrader(new MongoId(id))?.Base?.LoyaltyLevels is not { } levels) continue;
                foreach (var level in levels)
                {
                    level.MinLevel = 0;
                    level.MinSalesSum = 0;
                    level.MinStanding = 0;
                }
            }
        }

        public static void InstantHideout(LoadContext ctx)
        {
            foreach (var area in ctx.Hideout.Areas ?? [])
            {
                if (area.Stages is null) continue;
                foreach (var stage in area.Stages.Values)
                    stage.ConstructionTime = 0;
            }
        }

        public static void EnableMortarStrike(LoadContext ctx, IEnumerable<string> maps)
        {
            var configs = ctx.Globals.Configuration?.ArtilleryShelling?.ArtilleryMapsConfigs;
            if (configs is null)
            {
                ctx.Logger.Warning($"{Services.ConfigService.LogPrefix} globals has no ArtilleryShelling section, Mortar Strike skipped");
                return;
            }
            foreach (var map in maps)
            {
                if (!configs.TryGetValue(map, out var settings) || settings is null) continue;
                settings.PlanedShellingOn = true;
                if (settings.ArtilleryShellingAirDropSettings is { } airdrop) airdrop.UseAirDrop = true;
            }
        }
    }
}
