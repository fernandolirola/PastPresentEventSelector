using PPES.Factions;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace PPES.Events;

public static partial class EventCatalog
{
    private const string CustomsZones = "ZoneDormitory,ZoneGasStation,ZoneScavBase";
    // ZoneRailStrorage is SPT's stock Reserve zone key; keep its spelling for game compatibility.
    private const string ReserveZones = "ZoneRailStrorage,ZonePTOR2,ZoneBarrack,ZoneSubStorage";
    private const string LabZones = "BotZoneFloor1,BotZoneFloor2";
    private const string BlackDivLab = "BotZoneFloor2,BotZoneFloor1,BotZoneBasement";

    // spread-out zones per map for the faction groups, so they do not all start on top of the players
    internal static readonly Dictionary<string, string> FactionZones = new(StringComparer.OrdinalIgnoreCase)
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
    internal static readonly (string Boss, string Home)[] ScavBosses =
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

    public static readonly IReadOnlyList<PastEventDef> PastEvents =
    [
        new("shiftingBalance", "Shifting Balance", "Sep 2026", EventTag.Simulated,
            "Glukhar moves from Reserve to the water treatment plant on Lighthouse and is there in every raid. Unlocks the recreated quest chain Fog of War.",
            "O Glukhar sai da Reserve para a esta\u00e7\u00e3o de tratamento de \u00e1gua da Lighthouse e aparece em toda raid. Libera a cadeia de quests recriada Fog of War.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("rezervbase")) ctx.Remove("bossGluhar");
                if (ctx.Is("lighthouse")) ctx.AddFrom("rezervbase", "bossGluhar", "Zone_TreatmentContainers", 100);
            },
            Group: "glukhar",
            Maps: ["lighthouse", "rezervbase"],
            Kinds: EventKind.Bosses,
            Touches: ["glukhar"],
            RumorEn: "My guys at the water plant on Lighthouse say Glukhar moved in there with his whole crew.",
            RumorPt: "Meus contatos na esta\u00e7\u00e3o de \u00e1gua da Lighthouse dizem que o Glukhar se mudou pra l\u00e1 com a turma toda.",
            Wiki: "Shifting_Balance_(8_September_2026)"),

        new("lighthouseAirdrops", "Lighthouse Airdrops", "Sep 2026", EventTag.Simulated,
            "Lighthouse gets an airdrop in every raid, sometimes two. The extra valuable loot is not included.",
            "A Lighthouse recebe airdrop em toda raid, \u00e0s vezes dois. O loot extra valioso n\u00e3o est\u00e1 inclu\u00eddo.",
            [],
            OnRaid: ctx =>
            {
                if (!ctx.Is("lighthouse")) return;
                foreach (var airdrop in ctx.Location.AirdropParameters ?? [])
                {
                    airdrop.PlaneAirdropChance = 1;
                    airdrop.PlaneAirdropMax = Math.Max(airdrop.PlaneAirdropMax ?? 1, 2);
                }
            },
            Maps: ["lighthouse"],
            Kinds: EventKind.Airdrops,
            Touches: ["airdrop"],
            RumorEn: "Planes keep circling Lighthouse. Keep your eyes on the sky over there.",
            RumorPt: "Os avi\u00f5es n\u00e3o param de rodar a Lighthouse. Fique de olho no c\u00e9u por l\u00e1.",
            Wiki: "Lighthouse_Airdrops_(26_September_2026)"),

        new("scourgeIncursion", "Scourge Incursion", "Oct 2026", EventTag.Simulated,
            "Scourge, an arena fighter with his own guards, is in every Lab raid. Without this event he keeps the SCOURGE mod's own spawn chance. The quests, items and voice come from the SCOURGE mod. Needs the SCOURGE mod.",
            "O Scourge, um lutador da arena com os pr\u00f3prios guardas, aparece em toda raid na Lab. Sem este evento ele segue a chance de spawn do pr\u00f3prio mod SCOURGE. As quests, itens e a voz v\u00eam do mod SCOURGE. Precisa do mod SCOURGE.",
            [],
            OnRaid: ctx =>
            {
                if (!ctx.Is("laboratory")) return;
                Effects.ForceBoss(ctx, ModRequirements.ScourgeBoss, "followerScourgeEventLab", "4", LabZones);
            },
            Needs: new(ModRequirements.Scourge),
            Maps: ["laboratory"],
            Kinds: EventKind.Bosses,
            RumorEn: "Ref lost one of his fighters in the Lab. They call him Scourge, and he brought friends.",
            RumorPt: "O Ref perdeu um dos lutadores dele na Lab. Chamam ele de Scourge, e ele levou amigos."),

        new("blackout", "Blackout", "Jul 2026", EventTag.Simulated,
            "Wedge and his Black Division men are in every Lab raid, with another Black Division team at the gates. With Vultify's Blackout mod, every Lab raid also goes dark, with its gate-only extracts, keypad codes and arsenal key. Unlocks the recreated quest chain Wrench and Waterboarding. Needs the Black Division mod.",
            "O Wedge e os homens da Black Division aparecem em toda raid na Lab, com outro time da Black Division nos port\u00f5es. Com o mod Blackout do Vultify, toda raid na Lab tamb\u00e9m fica no escuro, com s\u00f3 os port\u00f5es como sa\u00edda, c\u00f3digos nos teclados e a chave do arsenal. Libera a cadeia de quests recriada Wrench and Waterboarding. Precisa do mod Black Division.",
            [],
            OnRaid: ctx =>
            {
                if (!ctx.Is("laboratory")) return;
                VultifyBlackout.Force(ctx);
                Effects.ForceBoss(ctx, BotRoles.Wedge, "blackDivIb", "3,3,3,4,4,6", BlackDivLab);
                Effects.AddGroup(ctx, BotRoles.BlackDivAssault, "2,3", "BotZoneGate1,BotZoneGate2", 100);
            },
            Needs: new(ModRequirements.BlackDivision),
            AlsoNeeds: new(ModRequirements.Blackout, "the darkness, the gate-only extracts and the keypad codes", "o apag\u00e3o, a sa\u00edda s\u00f3 pelos port\u00f5es e os c\u00f3digos dos teclados"),
            Maps: ["laboratory"],
            Kinds: EventKind.Bosses,
            RumorEn: "The Lab went dark and men in black are walking its halls. Their leader is called Wedge.",
            RumorPt: "A Lab apagou e uns homens de preto andam pelos corredores. O l\u00edder se chama Wedge.",
            Wiki: "Blackout_(14_July_2026)"),

        new("bonusExp", "Bonus EXP Weekend", "May to Jul 2026", EventTag.Simulated,
            "You earn about 25% more experience from raids.",
            "Voc\u00ea ganha cerca de 25% a mais de experi\u00eancia nas raids.",
            [],
            OnLoad: ctx =>
            {
                var end = ctx.Globals.Configuration!.Exp!.MatchEnd!;
                end.SurvivedMultiplier *= 1.25;
                end.RunnerMultiplier *= 1.25;
                end.MiaMultiplier *= 1.25;
                end.KilledMultiplier *= 1.25;
                end.LeftMultiplier *= 1.25;
            },
            Kinds: EventKind.Rules,
            RumorEn: "Good weekend to learn, friend. Every raid teaches a bit more than usual.",
            RumorPt: "Bom fim de semana pra aprender, amigo. Cada raid ensina um pouco mais que o normal.",
            Wiki: "Bonus_EXP_weekend_(24_July_2026)"),

        new("wildWildWoods", "Wild Wild Woods", "Jun 2026", EventTag.Simulated,
            "Shturman is in every Woods raid, the Scav cooldown is four times shorter and Woods has half again as much loose loot and a quarter more in containers. Unlocks the recreated quest Wild Wild Woods. The barters are not included.",
            "O Shturman aparece em toda raid na Woods, o tempo de espera do Scav fica quatro vezes menor e a Woods tem metade a mais de loot solto e um quarto a mais nos cont\u00eaineres. Libera a quest recriada Wild Wild Woods. As trocas n\u00e3o est\u00e3o inclu\u00eddas.",
            [],
            OnLoad: ctx =>
            {
                ctx.Globals.Configuration!.SavagePlayCooldown /= 4;
                Effects.ScaleLoot(ctx, "woods", 1.5, 1.25);
            },
            OnRaid: ctx =>
            {
                if (ctx.Is("woods")) ctx.SetChance("bossKojaniy", 100);
            },
            Maps: ["woods"],
            Kinds: EventKind.Bosses | EventKind.Rules,
            Touches: ["shturman"],
            RumorEn: "Shturman never leaves the sawmill now, and my scavs are ready to go back out much sooner.",
            RumorPt: "O Shturman n\u00e3o sai mais da serraria, e meus scavs voltam pra rua bem mais r\u00e1pido.",
            Wiki: "Wild_Wild_Woods_(25_June_2026)"),

        new("tarkovHospitality", "Tarkov Hospitality", "Jun 2026", EventTag.Simulated,
            "Reshala, Sanitar and Shturman are in every raid on their maps, the Goons stay away, as in the PvE version, and Ref's Cleanup Crew holds the Customs dorms, the Shoreline smugglers' base and the Woods village. In the live event the crew ignored PMCs; here they fight everyone. Unlocks the recreated quest chain Guardian of Nature.",
            "Reshala, Sanitar e Shturman aparecem em toda raid nos seus mapas, os Goons somem, como na vers\u00e3o PvE, e a Cleanup Crew do Ref ocupa os dormit\u00f3rios da Customs, a base dos contrabandistas da Shoreline e a vila da Woods. No evento original eles ignoravam PMCs; aqui eles atacam todo mundo. Libera a cadeia de quests recriada Guardian of Nature.",
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
            Group: "goons",
            Maps: ["bigmap", "shoreline", "woods", "lighthouse"],
            Kinds: EventKind.Bosses,
            Touches: ["goons", "reshala", "sanitar", "shturman"],
            RumorEn: "Reshala, Sanitar and Shturman are all home and receiving guests, and Ref's men are taking over spots for his fights. The Goons went quiet.",
            RumorPt: "Reshala, Sanitar e Shturman est\u00e3o em casa recebendo visitas, e os homens do Ref est\u00e3o tomando lugares pras lutas dele. Os Goons sumiram.",
            Wiki: "Tarkov_Hospitality_(5_June_2026)"),

        new("fullSpeedAhead", "Full Speed Ahead", "May 2026", EventTag.Simulated,
            "Rogues patrol Shoreline and Reserve, Glukhar is in every Reserve raid and the Goons only roam Shoreline, from the gas station to the power station. Unlocks the recreated quest chain No Smoke Without Fire.",
            "Rogues patrulham a Shoreline e a Reserve, o Glukhar aparece em toda raid na Reserve e os Goons s\u00f3 andam pela Shoreline, do posto de gasolina \u00e0 hidrel\u00e9trica. Libera a cadeia de quests recriada No Smoke Without Fire.",
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
            Group: "goons",
            Maps: ["shoreline", "rezervbase", "bigmap", "woods", "lighthouse"],
            Kinds: EventKind.Bosses,
            Touches: ["goons", "glukhar"],
            RumorEn: "Rogues are on Shoreline and Reserve, Glukhar holds the base and the Goons roam between the gas station and the power plant.",
            RumorPt: "Rogues na Shoreline e na Reserve, o Glukhar segura a base e os Goons rondam entre o posto e a hidrel\u00e9trica.",
            Wiki: "Full_Speed_Ahead_(15_May_2026)"),

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
            },
            Maps: ["factory4_day"],
            Kinds: EventKind.Bosses,
            Touches: ["tagilla", "killa"],
            RumorEn: "Ref sent a cleanup crew to the factory, with Tagilla and Killa in charge. Don't go in there alone.",
            RumorPt: "O Ref mandou uma equipe de limpeza pra f\u00e1brica, com o Tagilla e o Killa no comando. N\u00e3o entre l\u00e1 sozinho.",
            Wiki: "Arena_Cleanup_Crew_on_Factory_(1_May_2026)"),

        new("reshalaVsGlukhar", "Reshala vs Glukhar", "May 2026", EventTag.Simulated,
            "Reshala is in every Customs raid and Glukhar moves from Reserve to Customs to fight him.",
            "Reshala aparece em toda raid na Customs e o Glukhar sai da Reserve para enfrent\u00e1-lo l\u00e1.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("rezervbase")) ctx.Remove("bossGluhar");
                if (!ctx.Is("bigmap")) return;
                ctx.SetChance("bossBully", 100);
                ctx.AddFrom("rezervbase", "bossGluhar", CustomsZones, 100);
            },
            Group: "glukhar",
            Maps: ["bigmap", "rezervbase"],
            Kinds: EventKind.Bosses,
            Touches: ["glukhar", "reshala"],
            RumorEn: "Glukhar came to Customs to settle things with Reshala. Bring popcorn and armor.",
            RumorPt: "O Glukhar veio pra Customs acertar as contas com o Reshala. Leve pipoca e colete.",
            Wiki: "Reshala_vs_Glukhar_(8_May_2026)"),

        new("casusBelli", "Casus belli", "Apr 2026", EventTag.Simulated,
            "Pillagers, a new enemy faction, roam Customs, Interchange, Lighthouse, Shoreline and Woods in groups of 3 or 4. Unlocks the recreated quest chains Casus belli (Ref) and Short on Hands (Skier), and Pillagers often wear painted CQCM masks.",
            "Os Pillagers, uma fac\u00e7\u00e3o inimiga nova, andam em grupos de 3 ou 4 pela Customs, Interchange, Lighthouse, Shoreline e Woods. Libera as cadeias de quests recriadas Casus belli (Ref) e Short on Hands (Skier), e os Pillagers costumam usar m\u00e1scaras CQCM pintadas.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is(PillagerMaps)) Effects.AddFactionGroup(ctx, FactionCatalog.Pillager, "2,3");
            },
            Maps: PillagerMaps,
            Kinds: EventKind.Bosses,
            RumorEn: "Pillagers are stripping Customs, Interchange, Lighthouse, Shoreline and Woods. They shoot first.",
            RumorPt: "Os Pillagers est\u00e3o saqueando Customs, Interchange, Lighthouse, Shoreline e Woods. Eles atiram primeiro.",
            Wiki: "Casus_belli_(9_April_2026)"),

        new("smugglers2026", "Smugglers (2026)", "Mar 2026", EventTag.Simulated,
            "Smugglers come back in groups of 3 or 4 on Customs, Woods, Shoreline, Interchange, Lighthouse, Streets and Ground Zero 21+, every Scav boss is on Reserve and airdrops carry sealed weapon cases. Unlocks the recreated quest chain Bulldogs Under The Rug. The flare-called drops are not included.",
            "Os Smugglers voltam em grupos de 3 ou 4 na Customs, Woods, Shoreline, Interchange, Lighthouse, Streets e Ground Zero 21+, todo chefe Scav aparece na Reserve e os airdrops trazem caixas lacradas de armas. Libera a cadeia de quests recriada Bulldogs Under The Rug. Os airdrops chamados com sinalizador n\u00e3o est\u00e3o inclu\u00eddos.",
            [],
            OnLoad: ctx => Effects.EnsureWeaponCrates(ctx, 1, 2),
            OnRaid: ctx =>
            {
                if (ctx.Is(Smugglers2026Maps)) Effects.AddFactionGroup(ctx, FactionCatalog.Smuggler, "2,3");
                if (ctx.Is("rezervbase")) Effects.GatherScavBosses(ctx, "rezervbase", ReserveZones);
            },
            Group: "smugglers",
            Maps: ["bigmap", "woods", "shoreline", "interchange", "lighthouse", "tarkovstreets", "sandbox_high", "rezervbase"],
            Kinds: EventKind.Bosses,
            RumorEn: "The smugglers are back on the roads, and every boss is holed up at Reserve.",
            RumorPt: "Os contrabandistas voltaram pras estradas, e todos os chefes est\u00e3o entocados na Reserve.",
            Wiki: "Smugglers_2026_(12_March_2026)"),

        new("permanentBossPresence", "Permanent Boss Presence", "2023 to 2026", EventTag.Simulated,
            "Every boss spawns on its own maps in every raid (Partisan keeps his own rules). Also unlocks the quest Hint.",
            "Todo chefe aparece nos seus mapas em toda raid (o Partisan segue as regras dele). Tamb\u00e9m libera a quest Hint.",
            ["647710905320c660d91c15a5"],
            OnRaid: ctx =>
            {
                foreach (var spawn in ctx.Location.BossLocationSpawn ?? [])
                    if (spawn.BossName is { } name && name.StartsWith("boss", StringComparison.OrdinalIgnoreCase)
                        && name != "bossPartisan" && string.IsNullOrEmpty(spawn.TriggerId))
                        spawn.BossChance = 100;
            },
            Maps: GameMaps.AllIds,
            Kinds: EventKind.Bosses,
            RumorEn: "Every boss in the region is at home today. Every single one.",
            RumorPt: "Todos os chefes da regi\u00e3o est\u00e3o em casa hoje. Todos mesmo.",
            Wiki: "Permanent_Boss_Presence_(29_July_2026)"),

        new("softcore", "Softcore", "Oct 2025", EventTag.Simulated,
            "The Lab and the Labyrinth need no keycard and you can carry 50% more before being overweight. Unlocks the recreated quest Staying Light.",
            "A Lab e o Labirinto n\u00e3o pedem cart\u00e3o e voc\u00ea carrega 50% a mais antes de ficar com sobrepeso. Libera a quest recriada Staying Light.",
            [],
            OnLoad: ctx => Effects.ScaleOverweight(ctx, 1.5),
            FreeAccess: ["laboratory", "labyrinth"],
            Maps: ["laboratory", "labyrinth"],
            Kinds: EventKind.Access | EventKind.Rules,
            Touches: ["labKeys"],
            RumorEn: "The Lab doors are open and the packs feel lighter. Strange days.",
            RumorPt: "As portas da Lab est\u00e3o abertas e as mochilas parecem mais leves. Dias estranhos.",
            Wiki: "Softcore_(8_October_2025)"),

        new("targradTales", "Targrad Tales", "Aug 2025", EventTag.Ready,
            "Unlocks the quest chain Friend from Norvinsk.",
            "Libera a cadeia de quests Friend from Norvinsk.",
            ["6863cf925eaeb676d50a9651"],
            Kinds: EventKind.Quests,
            RumorEn: "An old friend from Norvinsk is asking around for help.",
            RumorPt: "Um velho amigo de Norvinsk est\u00e1 procurando ajuda.",
            Wiki: "Targrad_Tales_(20_August_2025)"),

        new("cultistPilgrimage", "Cultist Pilgrimage", "Dec 2024", EventTag.Simulated,
            "Cultists are in every night raid on Customs, Woods, Shoreline and Night Factory. Also unlocks the quest Failed Setup.",
            "Cultistas em toda raid noturna na Customs, Woods, Shoreline e Factory \u00e0 noite. Tamb\u00e9m libera a quest Failed Setup.",
            ["64916da7ad4e722c106f2345"],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap", "woods", "shoreline", "factory4_night")) ctx.SetChance("sectantPriest", 100);
            },
            Maps: ["bigmap", "woods", "shoreline", "factory4_night"],
            Kinds: EventKind.Bosses,
            Touches: ["cultists"],
            RumorEn: "The cultists walk every night now. Customs, Woods, Shoreline, even the factory.",
            RumorPt: "Os cultistas andam toda noite agora. Customs, Woods, Shoreline e at\u00e9 a f\u00e1brica.",
            Wiki: "Cultist_Pilgrimage_(18_December_2024)"),

        new("smugglers2024", "Smugglers (2024)", "Dec 2024", EventTag.Simulated,
            "Smugglers, a new enemy faction, show up in groups of 3 on every map except Ground Zero for low levels and the Lab, carrying locked cases, case keys and contraband. Unlocks the recreated quest chain That\u2019s a Great Plan, Walter.",
            "Os Smugglers, uma fac\u00e7\u00e3o inimiga nova, aparecem em grupos de 3 em todo mapa, menos o Ground Zero de n\u00edvel baixo e a Lab, levando maletas trancadas, chaves de maleta e contrabando. Libera a cadeia de quests recriada That\u2019s a Great Plan, Walter.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is(Smugglers2024Maps)) Effects.AddFactionGroup(ctx, FactionCatalog.Smuggler, "2");
            },
            Group: "smugglers",
            Maps: Smugglers2024Maps,
            Kinds: EventKind.Bosses,
            RumorEn: "Armed smugglers are moving goods on every map. Leave their cargo alone.",
            RumorPt: "Contrabandistas armados est\u00e3o levando carga por todos os mapas. Deixe a carga deles em paz.",
            Wiki: "Smugglers_2024_(6_December_2024)"),

        new("mortarStrike", "Mortar Strike", "Sep 2024", EventTag.Experimental,
            "Mortar barrages hit Customs, Woods, Shoreline and Reserve, with a special airdrop carrying radar parts. Unlocks the quest Into the Inferno.",
            "Bombardeios de morteiro na Customs, Woods, Shoreline e Reserve, com um airdrop especial com pe\u00e7as do radar. Libera a quest Into the Inferno.",
            ["66e01aca214f88109006a4b5"],
            Mortar: ["bigmap", "woods", "shoreline", "rezervbase"],
            Maps: ["bigmap", "woods", "shoreline", "rezervbase"],
            Kinds: EventKind.Hazard | EventKind.Airdrops,
            RumorEn: "Someone set up mortars. When the alarm sounds, run. Watch for the radar parts airdrop.",
            RumorPt: "Algu\u00e9m montou morteiros. Quando o alarme tocar, corra. Fique de olho no airdrop com pe\u00e7as do radar.",
            Wiki: "Mortar_Strike_(26_September_2024)"),

        new("closedFlea", "Closed Flea Market", "Aug 2024", EventTag.Simulated,
            "The flea market only opens at level 99, so in practice it is closed.",
            "O mercado s\u00f3 abre no n\u00edvel 99, ou seja, na pr\u00e1tica fica fechado.",
            [],
            OnLoad: ctx => ctx.Globals.Configuration!.RagFair!.MinUserLevel = 99,
            Group: "flea",
            Kinds: EventKind.Flea,
            Touches: ["flea"],
            RumorEn: "The flea market is closed for anyone without real connections.",
            RumorPt: "O mercado est\u00e1 fechado pra quem n\u00e3o tem contatos de verdade.",
            Wiki: "Closed_Flea_Market_(13_August_2024)"),

        new("supplyAndDemand", "Supply and Demand", "Aug 2024", EventTag.Ready,
            "Unlocks the quest Gunsmith - Special Order. Mechanic's price increase is not included.",
            "Libera a quest Gunsmith - Special Order. O aumento de pre\u00e7os do Mechanic n\u00e3o est\u00e1 inclu\u00eddo.",
            ["66a74c628410476dd65543be"],
            Kinds: EventKind.Quests,
            RumorEn: "Mechanic has a special order and he pays well for it.",
            RumorPt: "O Mechanic tem uma encomenda especial e paga bem por ela.",
            Wiki: "Supply_and_Demand_(2_August_2024)"),

        new("armsRace", "Arms Race", "Jul 2024", EventTag.Ready,
            "Unlocks the quest Import Control and its recreated follow-ups. The arms dealers Gus and Basmach are on Streets in every raid.",
            "Libera a quest Import Control e as continua\u00e7\u00f5es recriadas. Os traficantes de armas Gus e Basmach aparecem em toda raid na Streets.",
            ["668bcccc167d507eb01a268b"],
            OnRaid: ctx =>
            {
                if (!ctx.Is("tarkovstreets")) return;
                foreach (var dealer in new[] { FactionCatalog.Gus, FactionCatalog.Basmach })
                    Effects.AddGroup(ctx, dealer, "2", FactionZones["tarkovstreets"], 100, escortType: "followerKolontaySecurity");
            },
            Kinds: EventKind.Quests,
            RumorEn: "Western guns are flooding in. Peacekeeper wants to know who brings them.",
            RumorPt: "Armas ocidentais est\u00e3o chegando aos montes. O Peacekeeper quer saber quem traz.",
            Wiki: "Arms_Race_(16_July_2024)"),

        new("keyToSalvation", "A Key to Salvation", "Jun 2024", EventTag.Ready,
            "Unlocks the quest A Key to Salvation.",
            "Libera a quest A Key to Salvation.",
            ["6672ec2a2b6f3b71be794cc5"],
            Kinds: EventKind.Quests,
            RumorEn: "Mechanic is paying for military batteries. Ask him why.",
            RumorPt: "O Mechanic est\u00e1 pagando por baterias militares. Pergunte o porqu\u00ea.",
            Wiki: "A_Key_to_Salvation_(24_June_2024)"),

        new("hustle", "Hustle", "May 2024", EventTag.Simulated,
            "All Scav bosses, the Goons and the Cultists gather on Reserve in every raid. Unlocks the quest Hustle.",
            "Todos os chefes Scav, os Goons e os Cultistas se re\u00fanem na Reserve em toda raid. Libera a quest Hustle.",
            ["6658a15615cbb1b2c6014d5b"],
            OnRaid: ctx =>
            {
                if (!ctx.Is("rezervbase")) return;
                Effects.GatherScavBosses(ctx, "rezervbase", ReserveZones);
                ctx.AddFrom("bigmap", "bossKnight", ReserveZones, 100);
                ctx.AddFrom("bigmap", "sectantPriest", ReserveZones, 100);
            },
            Maps: ["rezervbase"],
            Kinds: EventKind.Bosses,
            Touches: ["goons", "cultists"],
            RumorEn: "All the bosses, the Goons and the cultists met at the Reserve helicopter. Something big is going on.",
            RumorPt: "Todos os chefes, os Goons e os cultistas se reuniram no helic\u00f3ptero da Reserve. Tem coisa grande acontecendo.",
            Wiki: "Hustle_(31_May_2024)"),

        new("tarkovMystery", "The Tarkov Mystery", "May 2024", EventTag.Simulated,
            "Peacekeeper's troops patrol Shoreline and Skier's troops patrol Customs in every raid. Unlocks the quest The Tarkov Mystery and its recreated follow-up chain, with a Skier or Peacekeeper route, and encrypted flash drives turn up in drawers, jackets and computers.",
            "Tropas do Peacekeeper na Shoreline e do Skier na Customs em toda raid. Libera a quest The Tarkov Mystery e a continua\u00e7\u00e3o recriada, com uma rota do Skier ou do Peacekeeper, e pendrives criptografados aparecem em gavetas, jaquetas e computadores.",
            ["6603fe74e773dcf3b0099f88"],
            OnLoad: ctx => Effects.AddStaticLoot(ctx, "660bbc47c38b837877075e47", 2),
            OnRaid: ctx =>
            {
                if (ctx.Is("shoreline")) ctx.SetChance("peacemaker", 100);
                if (ctx.Is("bigmap"))
                    Effects.AddGroup(ctx, "skier", "2,2,3,3,3,4,4,5", "ZoneScavBase,ZoneDormitory,ZoneFactoryCenter,ZoneCustoms,ZoneBrige", 100);
            },
            Maps: ["shoreline", "bigmap"],
            Kinds: EventKind.Bosses,
            RumorEn: "Peacekeeper's men are on Shoreline and Skier's boys took Customs.",
            RumorPt: "Os homens do Peacekeeper est\u00e3o na Shoreline e os do Skier tomaram a Customs.",
            Wiki: "The_Tarkov_Mystery_(15_May_2024)"),

        new("brothersReunited", "Brothers Reunited", "Apr 2024", EventTag.Simulated,
            "Tagilla joins Killa at the Kiba store on Interchange in every raid.",
            "O Tagilla se junta ao Killa na loja Kiba, na Interchange, em toda raid.",
            [],
            OnRaid: ctx =>
            {
                if (!ctx.Is("interchange")) return;
                ctx.SetChance("bossKilla", 100);
                ctx.AddFrom("factory4_day", "bossTagilla", "ZoneCenter,ZoneCenterBot", 100);
            },
            Maps: ["interchange"],
            Kinds: EventKind.Bosses,
            Touches: ["tagilla", "killa"],
            RumorEn: "Tagilla found his brother at the Kiba store. They are not selling guns.",
            RumorPt: "O Tagilla achou o irm\u00e3o na loja Kiba. Eles n\u00e3o est\u00e3o vendendo armas.",
            Wiki: "Brothers_Reunited_(17_April_2024)"),

        new("freeLabs", "Free Labs", "2022 to 2024", EventTag.Simulated,
            "Entering the Lab does not need a keycard.",
            "Entrar na Lab n\u00e3o precisa de cart\u00e3o.",
            [],
            FreeAccess: ["laboratory"],
            Maps: ["laboratory"],
            Kinds: EventKind.Access,
            Touches: ["labKeys"],
            RumorEn: "The Lab gate is open. No keycard needed, for now.",
            RumorPt: "O port\u00e3o da Lab est\u00e1 aberto. Sem cart\u00e3o, por enquanto.",
            Wiki: "Free_Lab_Access_Event_(12_May_2024)"),

        new("unlockedTraders", "Unlocked Traders", "2023, 2024", EventTag.Simulated,
            "The main traders are at loyalty level 4 regardless of your progress. Turning it off returns your real levels. Discounts are not included.",
            "Os traders principais ficam no n\u00edvel de lealdade 4, independente do seu progresso. Ao desligar, voltam os seus n\u00edveis reais. Descontos n\u00e3o est\u00e3o inclu\u00eddos.",
            [],
            OnLoad: ctx => Effects.UnlockTraders(ctx, MainTraders),
            Kinds: EventKind.Traders,
            RumorEn: "We all decided to trust you with our best goods. Don't make us regret it.",
            RumorPt: "Todos resolvemos confiar nossas melhores mercadorias a voc\u00ea. N\u00e3o nos fa\u00e7a arrepender.",
            Wiki: "Unlocked_Traders_%26_Discounts_(25_December_2024)"),

        new("downTheRabbitHole", "Down the Rabbit Hole", "Dec 2023", EventTag.Simulated,
            "All Scav bosses gather at the LexOs car dealership on Streets in every raid. Unlocks the recreated quest chain Down the Rabbit Hole.",
            "Todos os chefes Scav se re\u00fanem na concession\u00e1ria LexOs, na Streets, em toda raid. Libera a cadeia de quests recriada Down the Rabbit Hole.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("tarkovstreets")) Effects.GatherScavBosses(ctx, "tarkovstreets", "ZoneCarShowroom");
            },
            Maps: ["tarkovstreets"],
            Kinds: EventKind.Bosses,
            RumorEn: "All the bosses are hiding at the LexOs dealership on Streets. Bad place for a walk.",
            RumorPt: "Todos os chefes est\u00e3o escondidos na concession\u00e1ria LexOs, na Streets. Lugar ruim pra passear.",
            Wiki: "Down_the_Rabbit_Hole_(2_December_2023)"),

        new("ammoShortage", "Ammo Shortage", "Nov 2023", EventTag.Ready,
            "Unlocks the quest The Punisher - Harvest. The flea and trader ammo limits are not included.",
            "Libera a quest The Punisher - Harvest. As restri\u00e7\u00f5es de muni\u00e7\u00e3o no flea e nos traders n\u00e3o est\u00e3o inclu\u00eddas.",
            ["655e427b64d09b4122018228"],
            Kinds: EventKind.Quests,
            RumorEn: "Ammo is short and Prapor has a job for someone with a steady hand.",
            RumorPt: "Muni\u00e7\u00e3o est\u00e1 em falta e o Prapor tem um servi\u00e7o pra algu\u00e9m de m\u00e3o firme.",
            Wiki: "Ammo_Shortage_(25_November_2023)"),

        new("fleaPromotion", "Flea Market Promotion", "Dec 2023", EventTag.Simulated,
            "The flea market opens at any level and its fees drop to a quarter.",
            "O mercado abre em qualquer n\u00edvel e as taxas caem para um quarto.",
            [],
            OnLoad: ctx =>
            {
                var flea = ctx.Globals.Configuration!.RagFair!;
                flea.MinUserLevel = 1;
                flea.CommunityTax *= 0.25f;
                flea.CommunityItemTax *= 0.25f;
                flea.CommunityRequirementTax *= 0.25f;
            },
            Group: "flea",
            Kinds: EventKind.Flea,
            Touches: ["flea"],
            RumorEn: "The flea market is open to everyone and the fees are almost gone.",
            RumorPt: "O mercado est\u00e1 aberto pra todos e as taxas quase sumiram.",
            Wiki: "Flea_Market_Promotion_(22_December_2023)"),

        new("instantHideout", "Instant Hideout Building", "2022 to 2024", EventTag.Simulated,
            "Hideout modules and upgrades are built instantly. Their item requirements stay the same.",
            "M\u00f3dulos e melhorias do hideout ficam prontos na hora. Os itens exigidos continuam os mesmos.",
            [],
            OnLoad: Effects.InstantHideout,
            Kinds: EventKind.Hideout,
            RumorEn: "My builders work fast this week. Bring the materials and it is done.",
            RumorPt: "Meus construtores est\u00e3o r\u00e1pidos esta semana. Traga o material e fica pronto.",
            Wiki: "Instant_Hideout_Building_(23_December_2023)"),

        new("bossesAndRogues", "Bosses & Rogues Wander", "Jul 2023", EventTag.Simulated,
            "The Goons are in every Day Factory raid. Unlocks the quests Tourists and Hustle (2023).",
            "Os Goons aparecem em toda raid na Factory de dia. Libera as quests Tourists e Hustle (2023).",
            ["649570491bb4d158bc4d0168", "649567f359eab30d1b7c9585"],
            OnRaid: ctx =>
            {
                if (ctx.Is("factory4_day")) ctx.AddFrom("bigmap", "bossKnight", "BotZone", 100);
            },
            Maps: ["factory4_day"],
            Kinds: EventKind.Bosses,
            Touches: ["goons"],
            RumorEn: "The Goons are visiting the factory during the day.",
            RumorPt: "Os Goons est\u00e3o visitando a f\u00e1brica durante o dia.",
            Wiki: "Bosses_%26_Rogues_Wander_(3_July_2023)"),

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
            Group: "goons",
            Maps: ["shoreline", "bigmap", "woods", "lighthouse"],
            Kinds: EventKind.Bosses,
            Touches: ["goons", "sanitar"],
            RumorEn: "The Goons are guarding Sanitar at the resort. Whatever he has, it is worth a lot.",
            RumorPt: "Os Goons est\u00e3o protegendo o Sanitar no resort. O que ele tem deve valer muito.",
            Wiki: "The_TerraGroup_Trail_(28_July_2023)"),

        new("hardcoreMode", "Hardcore Mode", "Jul 2023", EventTag.Ready,
            "Unlocks the quest Cocktail Tasting. The Obdolbos changes are not included.",
            "Libera a quest Cocktail Tasting. As mudan\u00e7as no Obdolbos n\u00e3o est\u00e3o inclu\u00eddas.",
            ["649af47d717cb30e7e4b5e26"],
            Kinds: EventKind.Quests,
            RumorEn: "Therapist wants someone to test a new cocktail. Brave or crazy, she doesn't mind.",
            RumorPt: "A Therapist quer algu\u00e9m pra testar um coquetel novo. Corajoso ou maluco, tanto faz.",
            Wiki: "Hardcore_Mode_(7_July_2023)"),

        new("bloodhounds", "Bloodhounds Reveal", "Jun 2023", EventTag.Simulated,
            "The Bloodhounds are in every raid on Customs, Woods and Shoreline. Unlocks the quest Bloodhounds.",
            "Os Bloodhounds aparecem em toda raid na Customs, Woods e Shoreline. Libera a quest Bloodhounds.",
            ["64764abcd125ab430a14ccb5"],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap", "woods")) ctx.SetChance("arenaFighterEvent", 100);
                if (ctx.Is("shoreline"))
                    Effects.AddGroup(ctx, "arenaFighterEvent", "3,4", "ZoneStartVillage,ZoneGasStation,ZoneForestGasStation,ZonePowerStation", 100);
            },
            Maps: ["bigmap", "woods", "shoreline"],
            Kinds: EventKind.Bosses,
            RumorEn: "The Bloodhounds are hunting on Customs, Woods and Shoreline.",
            RumorPt: "Os Bloodhounds est\u00e3o ca\u00e7ando na Customs, Woods e Shoreline.",
            Wiki: "Bloodhounds_Reveal_(9_June_2023)"),

        new("weaponCaseAirdrops", "Weapon Case Airdrops", "Jun 2023", EventTag.Simulated,
            "Every airdrop carries two to four sealed weapon cases. Turn on Airdrop Traffic too to get one in every raid.",
            "Todo airdrop traz de duas a quatro caixas lacradas de armas. Ligue tamb\u00e9m o Airdrop Traffic para ter um em toda raid.",
            [],
            OnLoad: ctx => Effects.EnsureWeaponCrates(ctx, 2, 4),
            Kinds: EventKind.Airdrops,
            RumorEn: "The planes are dropping sealed weapon cases. Find one and you will thank me.",
            RumorPt: "Os avi\u00f5es est\u00e3o largando caixas lacradas de armas. Ache uma e voc\u00ea vai me agradecer.",
            Wiki: "Weapon_Case_Airdrops_(19_June_2023)"),

        new("bandages", "Bandages Event", "Apr 2023", EventTag.Ready,
            "Unlocks the community quest Important Patient.",
            "Libera a quest comunit\u00e1ria Important Patient.",
            ["641dbfd7f43eda9d810d7137"],
            Kinds: EventKind.Quests,
            RumorEn: "An important patient needs bandages. Lots of them.",
            RumorPt: "Um paciente importante precisa de bandagens. Muitas.",
            Wiki: "Bandages_Event_(7_April_2023)"),

        new("scavOnLabs", "Scav on Labs", "Dec 2022", EventTag.Simulated,
            "Glukhar and his guards move from Reserve to the Lab and are there in every raid. Scav access to the Lab is not included.",
            "O Glukhar e seus guardas saem da Reserve para a Lab e aparecem em toda raid. A entrada de Scav na Lab n\u00e3o est\u00e1 inclu\u00edda.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("rezervbase")) ctx.Remove("bossGluhar");
                if (ctx.Is("laboratory")) ctx.AddFrom("rezervbase", "bossGluhar", LabZones, 100);
            },
            Group: "glukhar",
            Maps: ["laboratory", "rezervbase"],
            Kinds: EventKind.Bosses,
            Touches: ["glukhar"],
            RumorEn: "Glukhar left Reserve and took his guards into the Lab.",
            RumorPt: "O Glukhar saiu da Reserve e levou os guardas pra dentro da Lab.",
            Wiki: "Scav_On_Labs_Event_(4_December_2022)"),

        new("boostedSkills", "Boosted Skill Leveling", "Dec 2022", EventTag.Simulated,
            "Character and weapon skills level up ten times faster.",
            "Habilidades do personagem e das armas sobem dez vezes mais r\u00e1pido.",
            [],
            OnLoad: ctx =>
            {
                var skills = ctx.Globals.Configuration!.SkillsSettings!;
                skills.SkillProgressRate *= 10;
                skills.WeaponSkillProgressRate *= 10;
            },
            Kinds: EventKind.Rules,
            RumorEn: "Something in the air. Everything you practice sticks ten times faster.",
            RumorPt: "Tem algo no ar. Tudo que voc\u00ea treina fixa dez vezes mais r\u00e1pido.",
            Wiki: "Boosted_Skill_Leveling_Event_(17_December_2022)"),

        new("sanitarEvent", "Sanitar Event", "Nov 2022", EventTag.Simulated,
            "Sanitar and four or five guards are on Customs in every raid. Unlocks the quest Letters.",
            "O Sanitar e quatro ou cinco guardas aparecem na Customs em toda raid. Libera a quest Letters.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap")) ctx.AddFrom("shoreline", "bossSanitar", CustomsZones, 100, "4,5");
            },
            Maps: ["bigmap"],
            Kinds: EventKind.Bosses,
            Touches: ["sanitar"],
            RumorEn: "Sanitar came to Customs with his guards. Nobody knows why.",
            RumorPt: "O Sanitar veio pra Customs com os guardas. Ningu\u00e9m sabe por qu\u00ea.",
            Wiki: "Sanitar_Event_(11_November_2022)"),

        new("airdropTraffic", "Airdrop Traffic", "Jun 2022, Mar 2026", EventTag.Simulated,
            "An airdrop comes in every raid on the outdoor maps.",
            "Um airdrop cai em toda raid nos mapas abertos.",
            [],
            OnRaid: ctx =>
            {
                foreach (var airdrop in ctx.Location.AirdropParameters ?? [])
                    airdrop.PlaneAirdropChance = 1;
            },
            Maps: GameMaps.Outdoor,
            Kinds: EventKind.Airdrops,
            Touches: ["airdrop"],
            RumorEn: "Supply planes over every open map today.",
            RumorPt: "Avi\u00f5es de suprimento sobre todos os mapas abertos hoje.",
            Wiki: "Airdrops_(19_March_2026)"),

        new("meetingOfBosses", "Meeting of the Bosses", "Jun 2022", EventTag.Simulated,
            "All Scav bosses are on Customs in every raid.",
            "Todos os chefes Scav aparecem na Customs em toda raid.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("bigmap")) Effects.GatherScavBosses(ctx, "bigmap", CustomsZones);
            },
            Maps: ["bigmap"],
            Kinds: EventKind.Bosses,
            RumorEn: "All the bosses are meeting on Customs. Stay away or bring friends.",
            RumorPt: "Todos os chefes est\u00e3o reunidos na Customs. Fique longe ou leve amigos.",
            Wiki: "Meeting_of_the_Bosses_(Day_3)"),

        new("heatwave", "Heatwave", "Jul 2021", EventTag.Simulated,
            "You get thirsty twice as fast in raid.",
            "Voc\u00ea fica com sede duas vezes mais r\u00e1pido na raid.",
            [],
            OnLoad: ctx => ctx.Globals.Configuration!.Health!.Effects!.Existence!.HydrationDamage *= 2,
            Kinds: EventKind.Rules,
            RumorEn: "It is hot out there. Bring water, more than you think.",
            RumorPt: "Est\u00e1 quente l\u00e1 fora. Leve \u00e1gua, mais do que voc\u00ea acha que precisa.",
            Wiki: "Heatwave_(29_July_2021)"),

        new("bossesGathering", "The Bosses Gathering", "Jun 2021", EventTag.Simulated,
            "All Scav bosses are on Reserve in every raid. Unlocks the recreated quest The Bosses gathering.",
            "Todos os chefes Scav aparecem na Reserve em toda raid. Libera a quest recriada The Bosses gathering.",
            [],
            OnRaid: ctx =>
            {
                if (ctx.Is("rezervbase")) Effects.GatherScavBosses(ctx, "rezervbase", ReserveZones);
            },
            Maps: ["rezervbase"],
            Kinds: EventKind.Bosses,
            RumorEn: "The bosses are gathering at Reserve.",
            RumorPt: "Os chefes est\u00e3o se reunindo na Reserve.",
            Wiki: "June_2021_Pre-Wipe_Event_(20_June_2021)"),
    ];
}
