using System.Text.RegularExpressions;

namespace PPES.Events;

[Flags]
public enum EventKind
{
    None = 0,
    Bosses = 1,
    Quests = 2,
    Traders = 4,
    Flea = 8,
    Airdrops = 16,
    Hideout = 32,
    Rules = 64,
    Access = 128,
    Hazard = 256,
}

/// <param name="Touches">What the event changes, so the page can point out two events changing the same thing.</param>
/// <param name="RumorEn">Line a trader sends at login while the event is on.</param>
public sealed record EventMeta(string[] Maps, EventKind Kinds, string[] Touches, string RumorEn, string RumorPt);

public static partial class EventDetails
{
    public sealed record MapInfo(string Id, string NameEn, string NamePt);

    public static readonly IReadOnlyList<MapInfo> Maps =
    [
        new("bigmap", "Customs", "Customs"),
        new("factory4_day", "Factory (day)", "Factory (dia)"),
        new("factory4_night", "Factory (night)", "Factory (noite)"),
        new("woods", "Woods", "Woods"),
        new("shoreline", "Shoreline", "Shoreline"),
        new("interchange", "Interchange", "Interchange"),
        new("rezervbase", "Reserve", "Reserve"),
        new("laboratory", "The Lab", "Lab"),
        new("lighthouse", "Lighthouse", "Lighthouse"),
        new("tarkovstreets", "Streets", "Streets"),
        new("sandbox", "Ground Zero", "Ground Zero"),
        new("sandbox_high", "Ground Zero 21+", "Ground Zero 21+"),
        new("labyrinth", "Labyrinth", "Labirinto"),
    ];

    private static readonly string[] Outdoor = ["bigmap", "woods", "shoreline", "interchange", "rezervbase", "lighthouse", "tarkovstreets", "sandbox", "sandbox_high"];
    private static readonly string[] AllMaps = Maps.Select(m => m.Id).ToArray();

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

    private static readonly Dictionary<string, EventMeta> Meta = new(StringComparer.OrdinalIgnoreCase)
    {
        ["shiftingBalance"] = new(["lighthouse", "rezervbase"], EventKind.Bosses, ["glukhar"],
            "My guys at the water plant on Lighthouse say Glukhar moved in there with his whole crew.",
            "Meus contatos na estação de água da Lighthouse dizem que o Glukhar se mudou pra lá com a turma toda."),
        ["lighthouseAirdrops"] = new(["lighthouse"], EventKind.Airdrops, ["airdrop"],
            "Planes keep circling Lighthouse. Keep your eyes on the sky over there.",
            "Os aviões não param de rodar a Lighthouse. Fique de olho no céu por lá."),
        ["bonusExp"] = new([], EventKind.Rules, [],
            "Good weekend to learn, friend. Every raid teaches a bit more than usual.",
            "Bom fim de semana pra aprender, amigo. Cada raid ensina um pouco mais que o normal."),
        ["wildWildWoods"] = new(["woods"], EventKind.Bosses | EventKind.Rules, ["shturman"],
            "Shturman never leaves the sawmill now, and my scavs are ready to go back out much sooner.",
            "O Shturman não sai mais da serraria, e meus scavs voltam pra rua bem mais rápido."),
        ["tarkovHospitality"] = new(["bigmap", "shoreline", "woods", "lighthouse"], EventKind.Bosses, ["goons", "reshala", "sanitar", "shturman"],
            "Reshala, Sanitar and Shturman are all home and receiving guests, and Ref's men are taking over spots for his fights. The Goons went quiet.",
            "Reshala, Sanitar e Shturman estão em casa recebendo visitas, e os homens do Ref estão tomando lugares pras lutas dele. Os Goons sumiram."),
        ["fullSpeedAhead"] = new(["shoreline", "rezervbase", "bigmap", "woods", "lighthouse"], EventKind.Bosses, ["goons", "glukhar"],
            "Rogues are on Shoreline and Reserve, Glukhar holds the base and the Goons roam between the gas station and the power plant.",
            "Rogues na Shoreline e na Reserve, o Glukhar segura a base e os Goons rondam entre o posto e a hidrelétrica."),
        ["arenaCleanupFactory"] = new(["factory4_day"], EventKind.Bosses, ["tagilla", "killa"],
            "Ref sent a cleanup crew to the factory, with Tagilla and Killa in charge. Don't go in there alone.",
            "O Ref mandou uma equipe de limpeza pra fábrica, com o Tagilla e o Killa no comando. Não entre lá sozinho."),
        ["blackout"] = new(["laboratory"], EventKind.Bosses, [],
            "The Lab went dark and men in black are walking its halls. Their leader is called Wedge.",
            "A Lab apagou e uns homens de preto andam pelos corredores. O líder se chama Wedge."),
        ["casusBelli"] = new(["bigmap", "interchange", "lighthouse", "shoreline", "woods"], EventKind.Bosses, [],
            "Pillagers are stripping Customs, Interchange, Lighthouse, Shoreline and Woods. They shoot first.",
            "Os Pillagers estão saqueando Customs, Interchange, Lighthouse, Shoreline e Woods. Eles atiram primeiro."),
        ["smugglers2026"] = new(["bigmap", "woods", "shoreline", "interchange", "lighthouse", "tarkovstreets", "sandbox_high", "rezervbase"], EventKind.Bosses, [],
            "The smugglers are back on the roads, and every boss is holed up at Reserve.",
            "Os contrabandistas voltaram pras estradas, e todos os chefes estão entocados na Reserve."),
        ["smugglers2024"] = new(["bigmap", "woods", "shoreline", "interchange", "lighthouse", "tarkovstreets", "rezervbase", "sandbox_high"], EventKind.Bosses, [],
            "Armed smugglers are moving goods on every map. Leave their cargo alone.",
            "Contrabandistas armados estão levando carga por todos os mapas. Deixe a carga deles em paz."),
        ["reshalaVsGlukhar"] = new(["bigmap", "rezervbase"], EventKind.Bosses, ["glukhar", "reshala"],
            "Glukhar came to Customs to settle things with Reshala. Bring popcorn and armor.",
            "O Glukhar veio pra Customs acertar as contas com o Reshala. Leve pipoca e colete."),
        ["permanentBossPresence"] = new(AllMaps, EventKind.Bosses, [],
            "Every boss in the region is at home today. Every single one.",
            "Todos os chefes da região estão em casa hoje. Todos mesmo."),
        ["softcore"] = new(["laboratory", "labyrinth"], EventKind.Access | EventKind.Rules, ["labKeys"],
            "The Lab doors are open and the packs feel lighter. Strange days.",
            "As portas da Lab estão abertas e as mochilas parecem mais leves. Dias estranhos."),
        ["targradTales"] = new([], EventKind.Quests, [],
            "An old friend from Norvinsk is asking around for help.",
            "Um velho amigo de Norvinsk está procurando ajuda."),
        ["cultistPilgrimage"] = new(["bigmap", "woods", "shoreline", "factory4_night"], EventKind.Bosses, ["cultists"],
            "The cultists walk every night now. Customs, Woods, Shoreline, even the factory.",
            "Os cultistas andam toda noite agora. Customs, Woods, Shoreline e até a fábrica."),
        ["mortarStrike"] = new(["bigmap", "woods", "shoreline", "rezervbase"], EventKind.Hazard | EventKind.Airdrops, [],
            "Someone set up mortars. When the alarm sounds, run. Watch for the radar parts airdrop.",
            "Alguém montou morteiros. Quando o alarme tocar, corra. Fique de olho no airdrop com peças do radar."),
        ["closedFlea"] = new([], EventKind.Flea, ["flea"],
            "The flea market is closed for anyone without real connections.",
            "O mercado está fechado pra quem não tem contatos de verdade."),
        ["supplyAndDemand"] = new([], EventKind.Quests, [],
            "Mechanic has a special order and he pays well for it.",
            "O Mechanic tem uma encomenda especial e paga bem por ela."),
        ["armsRace"] = new([], EventKind.Quests, [],
            "Western guns are flooding in. Peacekeeper wants to know who brings them.",
            "Armas ocidentais estão chegando aos montes. O Peacekeeper quer saber quem traz."),
        ["keyToSalvation"] = new([], EventKind.Quests, [],
            "Mechanic is paying for military batteries. Ask him why.",
            "O Mechanic está pagando por baterias militares. Pergunte o porquê."),
        ["hustle"] = new(["rezervbase"], EventKind.Bosses, ["goons", "cultists"],
            "All the bosses, the Goons and the cultists met at the Reserve helicopter. Something big is going on.",
            "Todos os chefes, os Goons e os cultistas se reuniram no helicóptero da Reserve. Tem coisa grande acontecendo."),
        ["tarkovMystery"] = new(["shoreline", "bigmap"], EventKind.Bosses, [],
            "Peacekeeper's men are on Shoreline and Skier's boys took Customs.",
            "Os homens do Peacekeeper estão na Shoreline e os do Skier tomaram a Customs."),
        ["brothersReunited"] = new(["interchange"], EventKind.Bosses, ["tagilla", "killa"],
            "Tagilla found his brother at the Kiba store. They are not selling guns.",
            "O Tagilla achou o irmão na loja Kiba. Eles não estão vendendo armas."),
        ["freeLabs"] = new(["laboratory"], EventKind.Access, ["labKeys"],
            "The Lab gate is open. No keycard needed, for now.",
            "O portão da Lab está aberto. Sem cartão, por enquanto."),
        ["unlockedTraders"] = new([], EventKind.Traders, [],
            "We all decided to trust you with our best goods. Don't make us regret it.",
            "Todos resolvemos confiar nossas melhores mercadorias a você. Não nos faça arrepender."),
        ["fleaPromotion"] = new([], EventKind.Flea, ["flea"],
            "The flea market is open to everyone and the fees are almost gone.",
            "O mercado está aberto pra todos e as taxas quase sumiram."),
        ["instantHideout"] = new([], EventKind.Hideout, [],
            "My builders work fast this week. Bring the materials and it is done.",
            "Meus construtores estão rápidos esta semana. Traga o material e fica pronto."),
        ["downTheRabbitHole"] = new(["tarkovstreets"], EventKind.Bosses, [],
            "All the bosses are hiding at the LexOs dealership on Streets. Bad place for a walk.",
            "Todos os chefes estão escondidos na concessionária LexOs, na Streets. Lugar ruim pra passear."),
        ["ammoShortage"] = new([], EventKind.Quests, [],
            "Ammo is short and Prapor has a job for someone with a steady hand.",
            "Munição está em falta e o Prapor tem um serviço pra alguém de mão firme."),
        ["bossesAndRogues"] = new(["factory4_day"], EventKind.Bosses, ["goons"],
            "The Goons are visiting the factory during the day.",
            "Os Goons estão visitando a fábrica durante o dia."),
        ["terraGroupTrail"] = new(["shoreline", "bigmap", "woods", "lighthouse"], EventKind.Bosses, ["goons", "sanitar"],
            "The Goons are guarding Sanitar at the resort. Whatever he has, it is worth a lot.",
            "Os Goons estão protegendo o Sanitar no resort. O que ele tem deve valer muito."),
        ["hardcoreMode"] = new([], EventKind.Quests, [],
            "Therapist wants someone to test a new cocktail. Brave or crazy, she doesn't mind.",
            "A Therapist quer alguém pra testar um coquetel novo. Corajoso ou maluco, tanto faz."),
        ["bloodhounds"] = new(["bigmap", "woods", "shoreline"], EventKind.Bosses, [],
            "The Bloodhounds are hunting on Customs, Woods and Shoreline.",
            "Os Bloodhounds estão caçando na Customs, Woods e Shoreline."),
        ["weaponCaseAirdrops"] = new([], EventKind.Airdrops, [],
            "The planes are dropping sealed weapon cases. Find one and you will thank me.",
            "Os aviões estão largando caixas lacradas de armas. Ache uma e você vai me agradecer."),
        ["bandages"] = new([], EventKind.Quests, [],
            "An important patient needs bandages. Lots of them.",
            "Um paciente importante precisa de bandagens. Muitas."),
        ["scavOnLabs"] = new(["laboratory", "rezervbase"], EventKind.Bosses, ["glukhar"],
            "Glukhar left Reserve and took his guards into the Lab.",
            "O Glukhar saiu da Reserve e levou os guardas pra dentro da Lab."),
        ["boostedSkills"] = new([], EventKind.Rules, [],
            "Something in the air. Everything you practice sticks ten times faster.",
            "Tem algo no ar. Tudo que você treina fixa dez vezes mais rápido."),
        ["sanitarEvent"] = new(["bigmap"], EventKind.Bosses, ["sanitar"],
            "Sanitar came to Customs with his guards. Nobody knows why.",
            "O Sanitar veio pra Customs com os guardas. Ninguém sabe por quê."),
        ["airdropTraffic"] = new(Outdoor, EventKind.Airdrops, ["airdrop"],
            "Supply planes over every open map today.",
            "Aviões de suprimento sobre todos os mapas abertos hoje."),
        ["meetingOfBosses"] = new(["bigmap"], EventKind.Bosses, [],
            "All the bosses are meeting on Customs. Stay away or bring friends.",
            "Todos os chefes estão reunidos na Customs. Fique longe ou leve amigos."),
        ["heatwave"] = new([], EventKind.Rules, [],
            "It is hot out there. Bring water, more than you think.",
            "Está quente lá fora. Leve água, mais do que você acha que precisa."),
        ["bossesGathering"] = new(["rezervbase"], EventKind.Bosses, [],
            "The bosses are gathering at Reserve.",
            "Os chefes estão se reunindo na Reserve."),
    };

    private static readonly Dictionary<string, (string En, string Pt)> SeasonalRumors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["halloween"] = ("Strange things after dark. Pumpkins, cultists and worse.", "Coisas estranhas depois que escurece. Abóboras, cultistas e coisa pior."),
        ["christmas"] = ("Santa is walking the maps with his bag. Don't shoot him, I mean it.", "O Papai Noel está andando pelos mapas com o saco. Não atire nele, é sério."),
        ["newyears"] = ("Happy New Year. Even here, people dance around the tree.", "Feliz Ano Novo. Até aqui o pessoal dança em volta da árvore."),
        ["aprilfools"] = ("Nothing makes sense today. Snow, cultists, gifts. Don't ask.", "Nada faz sentido hoje. Neve, cultistas, presentes. Não pergunte."),
    };

    public sealed record BuiltInPreset(string Id, string NameEn, string NamePt, string DescEn, string DescPt, string[] Events);

    public static readonly IReadOnlyList<BuiltInPreset> Presets =
    [
        new("bossHunt", "Boss hunt", "Caçada de chefes", "Bosses everywhere, every raid.", "Chefes por todo lado, em toda raid.",
            ["permanentBossPresence", "meetingOfBosses", "bossesGathering", "brothersReunited", "cultistPilgrimage", "bloodhounds", "arenaCleanupFactory"]),
        new("casual", "Casual", "Casual", "Less grind: open flea, instant hideout, more XP and loot from the sky.", "Menos grind: flea aberto, hideout na hora, mais XP e loot caindo do céu.",
            ["softcore", "instantHideout", "fleaPromotion", "boostedSkills", "unlockedTraders", "bonusExp", "airdropTraffic", "weaponCaseAirdrops"]),
        new("hardcore", "Hardcore", "Hardcore", "Closed flea, heat, mortars and cultists every night.", "Flea fechado, calor, morteiros e cultistas toda noite.",
            ["closedFlea", "heatwave", "mortarStrike", "cultistPilgrimage", "permanentBossPresence"]),
        new("questHunter", "Quest hunter", "Caçador de quests", "Every event that unlocks quests in your game.", "Todos os eventos que liberam quests no seu jogo.",
            ["targradTales", "mortarStrike", "supplyAndDemand", "armsRace", "keyToSalvation", "hustle", "tarkovMystery", "ammoShortage", "bossesAndRogues", "hardcoreMode", "bloodhounds", "bandages", "cultistPilgrimage", "permanentBossPresence"]),
        new("factions", "New factions", "Facções novas", "Cleanup Crew, Smugglers and Pillagers out on the maps.", "Cleanup Crew, Smugglers e Pillagers pelos mapas.",
            ["arenaCleanupFactory", "tarkovHospitality", "smugglers2026", "casusBelli"]),
        new("vanilla", "Vanilla", "Original", "Nothing from the past. Seasonal events back on Auto.", "Nada do passado. Sazonais de volta no Automático.",
            []),
    ];

    public static EventMeta Of(string eventId) => Meta.GetValueOrDefault(eventId) ?? new([], EventKind.None, [], "", "");

    public static (string En, string Pt) SeasonalRumor(string seasonalId) => SeasonalRumors.GetValueOrDefault(seasonalId);

    public static EventKind KindsOf(PastEventDef def) =>
        Of(def.Id).Kinds | (def.Quests.Length > 0 || WikiEventQuests.ByEvent.ContainsKey(def.Id) ? EventKind.Quests : EventKind.None);

    /// <summary>Latest year in the "When" label, used for the timeline.</summary>
    public static int YearOf(PastEventDef def) =>
        YearRegex().Matches(def.When).Select(m => int.Parse(m.Value)).DefaultIfEmpty(0).Max();

    [GeneratedRegex(@"\b20\d{2}\b")]
    private static partial Regex YearRegex();
}
