from lib import *

SKIER, PEACEKEEPER, MECHANIC, FENCE, LIGHTKEEPER = (TRADERS[k] for k in ("Skier", "Peacekeeper", "Mechanic", "Fence", "Lightkeeper"))
E = "tarkovMystery"
DRIVE, DECRYPTED, DOCS = "660bbc47c38b837877075e47", "660bbc98c38b837877075e4a", "660bc341c38b837877075e4c"
LAPTOPS = ["661421c7c1f2f548c50ee649", "661423200d240a5f5d0f679b"]
PHONE_CUSTOMS, PHONE_SHORE = "6614230055afee107f05e998", "6614217b6d9d5abcad0ff098"
LETTER, HARD_DRIVE = "6614238e0d240a5f5d0f679d", "661666458c2aa9cb1602503b"
BARTER = category("5448eb774bdc2d0a728b4567")
WEAPONS = [i for i in category("5422acb9af1c889c16000029") if items[i]["_type"] == "Item" and items[i]["_props"].get("weapClass")]


def route_trader(path):
    # copies under Obstacle Course are Skier's, under Obstruction Destruction Peacekeeper's
    return SKIER if ">obstacle>" in path or path.endswith(">obstacle") else PEACEKEEPER


add(
    Q("breakThrough", E, [handover("Hand over the decrypted flash drive", "Entregue o pendrive descriptografado", DECRYPTED)],
      name="Break Through", trader=SKIER, after="tarkovMystery", exclusive="tmRoute"),
    Q("uTurn", E, [handover("Hand over the decrypted flash drive", "Entregue o pendrive descriptografado", DECRYPTED)],
      name="U Turn", trader=PEACEKEEPER, after="tarkovMystery", exclusive="tmRoute"),
    # Mechanic decrypts the drive for either route, so his chain hangs off whichever was accepted
    Q("dh1", E, [
        handover("Hand over the encrypted flash drive", "Entregue o pendrive criptografado", DRIVE),
        find("Find PC CPUs in raid", "Encontre CPUs de PC na raid", tpl("PC CPU"), 3),
        handover("Hand over the PC CPUs", "Entregue as CPUs de PC", tpl("PC CPU"), 3, fir=True),
        find("Find RAM sticks in raid", "Encontre pentes de RAM na raid", tpl("RAM stick"), 3),
        handover("Hand over the RAM sticks", "Entregue os pentes de RAM", tpl("RAM stick"), 3, fir=True),
        find("Find SSD drives in raid", "Encontre SSDs na raid", tpl("SSD drive"), 2),
        handover("Hand over the SSD drives", "Entregue os SSDs", tpl("SSD drive"), 2, fir=True)],
      name="Decryption Hurdles - Part 1", trader=MECHANIC, after=[("breakThrough", (2, 3, 4)), ("uTurn", (2, 3, 4))], bonus=[(DECRYPTED, 1)]),
    Q("dh2", E, [
        stash("Stash a Military gyrotachometer at the designated place on Reserve", "Esconda um girotacômetro militar no local indicado na Reserve", tpl("Military gyrotachometer"), "Event_terminal_zone"),
        flare("Launch a signal flare at the roof of the radar station on Reserve", "Dispare um sinalizador no telhado da estação de radar na Reserve", "Event_terminal_zone_flare")],
      name="Decryption Hurdles - Part 2", after="dh1"),
    Q("dh3", E, [kill("Eliminate Scavs on Customs", "Elimine Scavs na Customs", 10, "Savage", map="customs")],
      name="Decryption Hurdles - Part 3", after="dh2", bonus=[(DOCS, 1)]),
    Q("takeTwo", E, [handover("Hand over Documents with decrypted data", "Entregue os documentos com os dados descriptografados", DOCS)],
      name="Take Two", trader=SKIER, after="breakThrough"),
    Q("photoHunt", E, [stash(f"Stash a WI-FI Camera at the TerraGroup HQ {spot} on Ground Zero", f"Esconda uma WI-FI Camera {pt} da sede da TerraGroup no Ground Zero",
                             tpl("WI-FI Camera"), f"quest_terminal_camera_{n}")
                       for n, spot, pt in [(1, "parking doorway", "na porta do estacionamento"), (2, "barricaded stairway", "na escada barricada"),
                                           (3, "office #3 bookshelf", "na estante do escritório 3"), (4, "office #4 corner window", "na janela do canto do escritório 4")]],
      name="Photo Hunt", after="takeTwo"),
    Q("gettingCore", E, [handover("Hand over the Documents with decrypted data", "Entregue os documentos com os dados descriptografados", DOCS)],
      name="Getting to the Core", trader=PEACEKEEPER, after="uTurn"),
    Q("noPeeking", E, [kill("Eliminate PMC operatives at the TerraGroup HQ on Ground Zero", "Elimine operadores PMC na sede da TerraGroup no Ground Zero", 5, "AnyPmc", zones=["quest_terminal_kill_zone"])],
      name="No Peeking!", after="gettingCore"),
    Q("forgotten", E, [
        visit("Locate the hideout at the cottages on Lighthouse", "Encontre o esconderijo nos chalés da Lighthouse", "quest_terminal_cottage"),
        extract("Survive and extract from the location", "Sobreviva e saia do mapa", "lighthouse")],
      name="Forgotten Acquaintance", after=["photoHunt", "noPeeking"]),
    Q("seeking", E, [
        find("Locate and obtain the hard drive inside the cottage hideout on Lighthouse", "Encontre e pegue o HD dentro do esconderijo do chalé na Lighthouse", HARD_DRIVE),
        handover("Hand over the hard drive", "Entregue o HD", HARD_DRIVE)],
      name="Seeking Information", after="forgotten"),
    Q("obstacle", E, [kill("Eliminate Peacekeeper's people on Shoreline", "Elimine o pessoal do Peacekeeper na Shoreline", 5, "Savage", ["peacemaker"], map="shoreline")],
      name="Obstacle Course", after="seeking", exclusive="tmSide"),
    Q("obstruction", E, [kill("Eliminate Skier's people on Customs", "Elimine o pessoal do Skier na Customs", 5, "Savage", ["skier"], map="customs")],
      name="Obstruction Destruction", after="seeking", exclusive="tmSide"),
    Q("radioClub", E, [
        kill("Eliminate Scavs on Woods", "Elimine Scavs na Woods", 8, "Savage", map="woods"),
        mark("Install a Radio repeater at the mountain crate on Woods", "Instale um repetidor de rádio na caixa da montanha na Woods", "meh_45_radio_area_mark_1", "Radio repeater"),
        mark("Install a Radio repeater at the mountain ravine stash on Woods", "Instale um repetidor de rádio no esconderijo da ravina na Woods", "meh_45_radio_area_mark_2", "Radio repeater"),
        mark("Install a Radio repeater at the mountain bunker on Woods", "Instale um repetidor de rádio no bunker da montanha na Woods", "meh_45_radio_area_mark_3", "Radio repeater"),
        extract("Survive and extract from the location", "Sobreviva e saia do mapa", "woods")],
      name="Radio Club", after="obstacle"),
    Q("newHope", E, [
        find("Locate and obtain The Unheard's laptop at the smugglers' base on Shoreline", "Encontre e pegue o laptop do Unheard na base dos contrabandistas na Shoreline", LAPTOPS),
        handover("Hand over the laptop", "Entregue o laptop", LAPTOPS)],
      name="A New Hope", after="obstruction"),
    Q("liaison", E, [
        visit("Locate the specified zone on Customs", "Encontre a área indicada na Customs", "quest_terminal_find_zone"),
        stash("Stash a Physical Bitcoin in the locker inside the worker's shack", "Esconda um Physical Bitcoin no armário da cabana dos trabalhadores", tpl("Physical Bitcoin"), "quest_terminal_bitcoin"),
        flare("Launch a signal flare at the weather station", "Dispare um sinalizador na estação meteorológica", "quest_terminal_flare_zone"),
        extract("Survive and extract from the location", "Sobreviva e saia do mapa", "customs")],
      name="Liaison", after="newHope"),
    Q("ceasefireInfo", E, [
        find("Locate and obtain Skier and Peacekeeper correspondence in the village on Shoreline", "Encontre e pegue a correspondência entre Skier e Peacekeeper na vila da Shoreline", LETTER),
        handover("Hand over the letter", "Entregue a carta", LETTER)],
      name="Ceasefire - Information", after=["radioClub", "liaison"]),
    Q("ceasefire2", E, [
        kill("Eliminate any 5 targets while using an M4A1, UNTAR helmet and armor on Customs", "Elimine 5 alvos quaisquer usando M4A1, capacete e colete UNTAR na Customs",
             5, "Any", map="customs", weapons=[tpl("Colt M4A1 5.56x45 assault rifle")], wear=[(tpl("UNTAR helmet"), tpl("MF-UNTAR body armor"))]),
        kill("Eliminate any 5 targets while using an AK-74N, Kora-Kulon DFL body armor and 6B47 helmet on Shoreline", "Elimine 5 alvos quaisquer usando AK-74N, colete Kora-Kulon e capacete 6B47 na Shoreline",
             5, "Any", map="shoreline", weapons=[tpl("Kalashnikov AK-74N 5.45x39 assault rifle")],
             wear=[(k, h) for k in tpls("NPP KlASS Kora-Kulon body armor (Black)", "NPP KlASS Kora-Kulon body armor (EMR)")
                   for h in tpls("6B47 Ratnik-BSh helmet (Olive Drab)", "6B47 Ratnik-BSh helmet (EMR cover)", "6B47 Ratnik-BSh helmet (EMR Arctic cover)")])],
      name="Ceasefire - Part 2", after="ceasefireInfo"),
    # killing Scavs drops Fence's standing below zero, which fails the first try and opens the last chance
    Q("embargo", E, [sell("Sell barter items to Fence", "Venda itens de troca para o Fence", None, BARTER, 15)],
      name="Trade Embargo", trader=FENCE, after="ceasefire2",
      fail=[{"compareMethod": "<", "conditionType": "TraderStanding", "dynamicLocale": False, "globalQuestCounterId": "", "parentId": "",
             "target": FENCE, "value": 0, "visibilityConditions": []}]),
    Q("embargoLast", E, [sell("Sell barter items to Fence", "Venda itens de troca para o Fence", None, BARTER, 50)],
      name="Trade Embargo - Last Chance", trader=FENCE, after=[("embargo", (5,))]),
    Q("atonement", E, [
        kill("Eliminate Scavs on Customs", "Elimine Scavs na Customs", 5, "Savage", map="customs"),
        visit("Reach the roof of the three-story dorm on Customs", "Chegue ao telhado do dormitório de três andares na Customs", "room214"),
        kill("Eliminate Scavs on Shoreline", "Elimine Scavs na Shoreline", 5, "Savage", map="shoreline"),
        visit("Reach the east wing of the Health Resort on Shoreline", "Chegue à ala leste do Health Resort na Shoreline", "place_peacemaker_008_4_N1")],
      name="Atonement", trader=route_trader, after=["embargo", "embargoLast"]),
    Q("redemption", E, [
        find("Locate and obtain The Unheard's phone on Customs", "Encontre e pegue o telefone do Unheard na Customs", [PHONE_CUSTOMS, PHONE_SHORE]),
        handover("Hand over the phone", "Entregue o telefone", [PHONE_CUSTOMS, PHONE_SHORE]),
        find("Locate and obtain The Unheard's laptop on Shoreline", "Encontre e pegue o laptop do Unheard na Shoreline", LAPTOPS),
        handover("Hand over the laptop", "Entregue o laptop", LAPTOPS)],
      name="Redemption", trader=FENCE, after="atonement", exclusive="tmEnd"),
    Q("ding", E, [
        sell("Sell any 5 weapons to Skier", "Venda 5 armas quaisquer para o Skier", SKIER, WEAPONS, 5),
        find("Locate and obtain The Unheard's phone on Customs", "Encontre e pegue o telefone do Unheard na Customs", [PHONE_CUSTOMS, PHONE_SHORE]),
        handover("Hand over the phone", "Entregue o telefone", [PHONE_CUSTOMS, PHONE_SHORE]),
        sell("Sell any 5 weapons to Peacekeeper", "Venda 5 armas quaisquer para o Peacekeeper", PEACEKEEPER, WEAPONS, 5),
        find("Locate and obtain The Unheard's laptop on Shoreline", "Encontre e pegue o laptop do Unheard na Shoreline", LAPTOPS),
        handover("Hand over the laptop", "Entregue o laptop", LAPTOPS)],
      name="Ding-Ding-Ding", trader=LIGHTKEEPER, after="atonement", exclusive="tmEnd"),
)
