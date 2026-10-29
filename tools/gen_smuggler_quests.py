# Builds Quests/smugglerQuests.json and Quests/smugglerLocales.json from the SPT database.
# Run: python -I tools/gen_smuggler_quests.py [path to SPT_Data/database]
import hashlib
import json
import pathlib
import sys

DB = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else r"C:\SPT\SPT_Runtime\SPT_Data\database")
OUT = pathlib.Path(__file__).resolve().parent.parent / "Quests"

items = json.loads((DB / "templates/items.json").read_text(encoding="utf-8"))
presets = json.loads((DB / "globals.json").read_text(encoding="utf-8"))["ItemPresets"]
stock = json.loads((DB / "templates/quests.json").read_text(encoding="utf-8"))


def oid(*seed):
    # stable ids so regenerating never breaks profiles that hold these quests
    return hashlib.md5(("ppes-smugglers:" + ":".join(seed)).encode()).hexdigest()[:24]


SKIER, PEACEKEEPER, MECHANIC, RAGMAN = "58330581ace78e27b8b10cee", "5935c25fb3acc3127c3d8cd9", "5a7c2eca46aef81a7ca2145d", "5ac3b934156ae10c4430e83c"
RUB, USD, EUR = "5449016a4bdc2d6f028b456f", "5696686a4bdc2da3298b456a", "569668774bdc2da2298b4568"
LOCKED_CASE, CASE_KEY, CONTRABAND = "6740987b89d5e1ddc603f4f0", "67449b6c89d5e1ddc603f504", "67408903268737ef6908d432"
OPENED_CASE, OPENED_BOX = "674078c4a9c9adf0450d59f9", "674098588466ebb03408b210"
SEALED_CASES = sorted(i for i, x in items.items() if x["_name"].startswith("weapon_case_sealed") or (x["_parent"] == "62f109593b54472778797866" and x["_props"].get("Width") == 5 and x["_props"].get("Height") == 2))
SMUGGLER = ["ppesSmuggler"]

WALTER, CHOP, SHELL = "674647f38466ebb03408b291", "6746480cd0b2f8eb9b034e3e", "6746480040ea3d1d4f0e5d51"
BULLDOGS, DELIVERY, SILVER, RESTOCK, ROOT = (oid("2026", n) for n in ("bulldogs", "delivery", "silver", "restock", "root"))


def icon(trader):
    return next(q["image"] for q in stock.values() if q["traderId"] == trader)


def tree(tpl, key):
    """Item tree for a reward: weapons from their default preset, armor with its default plates, ammo packs filled."""
    root = oid(key, "root")
    preset = next((p for p in presets.values() if p.get("_encyclopedia") == tpl), None)
    if preset:
        ids = {}
        out = []
        for n, it in enumerate(preset["_items"]):
            ids[it["_id"]] = oid(key, str(n)) if it["_id"] != preset["_parent"] else root
        for it in preset["_items"]:
            new = {"_id": ids[it["_id"]], "_tpl": it["_tpl"]}
            if "parentId" in it and it["parentId"] in ids:
                new["parentId"], new["slotId"] = ids[it["parentId"]], it["slotId"]
            out.append(new)
        out[0]["upd"] = {"StackObjectsCount": 1}
        return out
    props = items[tpl]["_props"]
    out = [{"_id": root, "_tpl": tpl, "upd": {"StackObjectsCount": 1}}]
    for slot in props.get("Slots") or []:
        filters = slot["_props"].get("filters") or [{}]
        plate = filters[0].get("Plate")
        if plate:
            out.append({"_id": oid(key, slot["_name"]), "_tpl": plate, "parentId": root, "slotId": slot["_name"]})
    for stack in props.get("StackSlots") or []:
        ammo = stack["_props"]["filters"][0]["Filter"][0]
        out.append({"_id": oid(key, "ammo"), "_tpl": ammo, "parentId": root, "slotId": "cartridges", "upd": {"StackObjectsCount": stack["_max_count"]}})
    return out


def reward_items(qid, tpl, count):
    key = f"{qid}:{tpl}"
    if tpl in (RUB, USD, EUR):
        root = oid(key)
        return {"items": [{"_id": root, "_tpl": tpl, "upd": {"StackObjectsCount": count}}], "target": root, "value": count}
    stackable = items[tpl]["_props"].get("StackMaxSize", 1) > 1 and not items[tpl]["_props"].get("StackSlots")
    if stackable:
        root = oid(key)
        return {"items": [{"_id": root, "_tpl": tpl, "upd": {"StackObjectsCount": count}}], "target": root, "value": count}
    all_items = []
    for n in range(count):
        all_items += tree(tpl, f"{key}:{n}")
    return {"items": all_items, "target": all_items[0]["_id"], "value": count}


def base_reward(qid, kind, n):
    return {"availableInGameEditions": [], "gameMode": ["regular", "pve"], "id": oid(qid, "reward", str(n)), "isHidden": False, "type": kind, "unknown": False}


def rewards(qid, exp, money, standing=None, things=(), crafts=()):
    out, n = [], 0
    r = base_reward(qid, "Experience", n); r["value"] = exp; out.append(r); n += 1
    if standing:
        r = base_reward(qid, "TraderStanding", n); r["target"], r["value"] = standing; out.append(r); n += 1
    for tpl, count in [money, *things]:
        r = base_reward(qid, "Item", n); r.update({"findInRaid": tpl not in (RUB, USD, EUR), "isEncoded": False}); r.update(reward_items(qid, tpl, count)); out.append(r); n += 1
    for product in crafts:
        r = base_reward(qid, "ProductionScheme", n)
        root = oid(qid, "craft", product)
        r.update({"items": [{"_id": root, "_tpl": product, "upd": {"StackObjectsCount": 1}}], "target": root, "loyaltyLevel": 1, "traderId": 10})
        out.append(r); n += 1
    return out


def cond(qid, n, kind, **extra):
    c = {"conditionType": kind, "dynamicLocale": False, "globalQuestCounterId": "", "id": oid(qid, "cond", str(n)), "index": n, "parentId": "", "visibilityConditions": []}
    c.update(extra)
    return c


def handover(qid, n, targets, value):
    return cond(qid, n, "HandoverItem", dogtagLevel=0, isEncoded=False, maxDurability=100, minDurability=0, onlyFoundInRaid=False, target=targets, value=value)


def kills(qid, n, value):
    counter = {"id": oid(qid, "counter", str(n)), "conditions": [{
        "bodyPart": [], "compareMethod": ">=", "conditionType": "Kills", "daytime": {"from": 0, "to": 0},
        "distance": {"compareMethod": ">=", "value": 0}, "dynamicLocale": False, "enemyEquipmentExclusive": [],
        "enemyEquipmentInclusive": [], "enemyHealthEffects": [], "id": oid(qid, "kills", str(n)), "resetOnSessionEnd": False,
        "savageRole": SMUGGLER, "target": "Savage", "value": 1, "weapon": [], "weaponCaliber": [],
        "weaponModsExclusive": [], "weaponModsInclusive": []}]}
    return cond(qid, n, "CounterCreator", completeInSeconds=0, counter=counter, doNotResetIfCounterCompleted=False,
                isNecessary=False, isResetOnConditionFailed=False, oneSessionOnly=False, type="Elimination", value=value)


def visit(qid, n, zone):
    counter = {"id": oid(qid, "counter", str(n)), "conditions": [{"conditionType": "VisitPlace", "dynamicLocale": False, "id": oid(qid, "visit", str(n)), "target": zone, "value": 1}]}
    return cond(qid, n, "CounterCreator", completeInSeconds=0, counter=counter, doNotResetIfCounterCompleted=False,
                isNecessary=False, isResetOnConditionFailed=False, oneSessionOnly=False, type="Exploration", value=1)


def stash(qid, n, targets, value, zone):
    return cond(qid, n, "LeaveItemAtLocation", dogtagLevel=0, isEncoded=False, maxDurability=100, minDurability=0,
                onlyFoundInRaid=False, plantTime=10, target=targets, value=value, zoneId=zone)


def after(qid, prev, status=(4,)):
    return [cond(qid, 0, "Quest", availableAfter=0, dispersion=0, status=list(status), target=prev)]


def quest(qid, name, trader, kind, start, finish, success):
    keys = ["acceptPlayerMessage", "changeQuestMessageText", "completePlayerMessage", "declinePlayerMessage", "description",
            "failMessageText", "name", "note", "startedMessageText", "successMessageText"]
    q = {k: f"{qid} {k}" for k in keys}
    q.update({
        "QuestName": name, "_id": qid, "acceptanceAndFinishingSource": "eft", "arenaLocations": [], "canShowNotificationsInGame": True,
        "conditions": {"AvailableForFinish": finish, "AvailableForStart": start, "Fail": []},
        "gameModes": [], "image": icon(trader), "instantComplete": False, "isKey": False, "location": "any",
        "progressSource": "eft", "rankingModes": [], "restartable": False,
        "rewards": {"Fail": [], "Started": [], "Success": success},
        "secretQuest": False, "side": "Pmc", "status": 0, "traderId": trader, "type": kind,
    })
    return q


quests = {}
texts = {"en": {}, "po": {}}


def add(q, cond_texts, story=None):
    quests[q["_id"]] = q
    finish = q["conditions"]["AvailableForFinish"]
    for c, (en, pt) in zip(finish, cond_texts):
        texts["en"][c["id"]], texts["po"][c["id"]] = en, pt
    qid = q["_id"]
    for k in ("note", "changeQuestMessageText"):
        texts["en"][f"{qid} {k}"] = texts["po"][f"{qid} {k}"] = ""
    if story:
        for lang, values in story.items():
            texts[lang][f"{qid} name"] = q["QuestName"]
            for k, v in values.items():
                texts[lang][f"{qid} {k}"] = v
    else:
        # the 2024 quests still have their own texts in every language
        texts["en"][f"{qid} startedMessageText"] = texts["po"][f"{qid} startedMessageText"] = ""


# Smugglers (2024): original ids, texts already in the game locales
q = quest(WALTER, "That’s a Great Plan, Walter", SKIER, "PickUp",
          [cond(WALTER, 0, "Level", compareMethod=">=", value=1)],
          [handover(WALTER, 0, [LOCKED_CASE], 3)],
          rewards(WALTER, 6200, (RUB, 40000), (SKIER, 0.03),
                  [("655746010177119f4a097ff7", 4), ("6557458f83942d705f0c4962", 8)], [OPENED_CASE, OPENED_BOX]))
add(q, [("Locate and hand over the locked cases with cargo", "Encontre e entregue as maletas trancadas com a carga")])

q = quest(CHOP, "Chop the Thieving Hand", PEACEKEEPER, "Elimination", after(CHOP, WALTER),
          [kills(CHOP, 0, 25)],
          rewards(CHOP, 11400, (RUB, 500), (PEACEKEEPER, 0.07),
                  [("5c6d46132e221601da357d56", 4), ("6570265f1419851aef03e739", 4), ("65702681bfc87b3a3409325f", 2), ("6184055050224f204c1da540", 1)]))
add(q, [("Eliminate Smugglers", "Elimine Contrabandistas")])

q = quest(SHELL, "Shell Game", SKIER, "PickUp", after(SHELL, CHOP),
          [handover(SHELL, 0, [CONTRABAND], 15)],
          rewards(SHELL, 23600, (RUB, 100000), (SKIER, 0.12),
                  [("660ea4453786cc0af808a1be", 2), ("6570254fcfc010a0f5006a22", 15), ("648984e3f09d032aa9399d53", 5),
                   ("628d0618d1ba6e4fa07ce5a4", 2), ("656e0436d44a1bb4220303a0", 2), ("661ceb1b9311543c7104149b", 1)]))
add(q, [("Hand over Skier's contraband from the locked cases", "Entregue o contrabando do Skier tirado das maletas trancadas")])

# Smugglers (2026): new quests, short original texts
q = quest(BULLDOGS, "Bulldogs Under The Rug", PEACEKEEPER, "Exploration",
          # live it needed Fishing Gear accepted; a level gate lets players outside that chain reach it
          [cond(BULLDOGS, 0, "Level", compareMethod=">=", value=15)],
          [kills(BULLDOGS, 0, 1), visit(BULLDOGS, 1, "q14_10_point_area"), handover(BULLDOGS, 2, [CONTRABAND], 1)],
          rewards(BULLDOGS, 10000, (USD, 700), None,
                  [("6165ac306ef05c2ce828ef74", 1), ("6183d53f1cb55961fa0fdcda", 6), ("6769b8e3c1a1466c850658a8", 8)]))
add(q, [("Eliminate a Smuggler on any location", "Elimine um Contrabandista em qualquer mapa"),
        ("Locate the smugglers' base on Shoreline", "Encontre a base dos contrabandistas na Shoreline"),
        ("Hand over a contraband box as evidence of the smugglers' deals", "Entregue uma caixa de contrabando como prova dos negócios dos contrabandistas")],
    {"en": {
        "description": "Someone across the border wants my seat at the council, and I think he is using the smugglers to get it. Airdrops are up, and so is smuggler traffic around the bay. Thin out their ranks, find the camp they use on Shoreline and bring me something that proves who they work for.",
        "successMessageText": "So it is true. He is moving goods on my ground and buying the local gangs with them. We will deal with this.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""},
     "po": {
        "description": "Alguém do outro lado da fronteira quer o meu lugar no conselho, e acho que está usando os contrabandistas para isso. Os airdrops aumentaram, e o movimento de contrabandistas na baía também. Diminua o número deles, encontre o acampamento que usam na Shoreline e me traga algo que prove para quem trabalham.",
        "successMessageText": "Então é verdade. Ele está passando mercadoria no meu território e comprando as gangues locais com ela. Vamos resolver isso.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""}})

q = quest(DELIVERY, "Accept Delivery", PEACEKEEPER, "PickUp", after(DELIVERY, BULLDOGS),
          [handover(DELIVERY, 0, [LOCKED_CASE], 1), handover(DELIVERY, 1, [CASE_KEY], 1)],
          rewards(DELIVERY, 11500, (USD, 1100), None, [("5c12613b86f7743bbe2c3f76", 3)]))
add(q, [("Obtain and hand over a locked smugglers' case", "Consiga e entregue uma maleta trancada dos contrabandistas"),
        ("Obtain and hand over a case key", "Consiga e entregue uma chave de maleta")],
    {"en": {
        "description": "I need to see what is in those shipments. The smugglers split everything into small locked cases, the same trick Skier and I once used. Bring me one of their cases and a key that opens it.",
        "successMessageText": "Thermal optics, and only part of a set. They split the gear itself, not just the shipments. Clever.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""},
     "po": {
        "description": "Preciso ver o que tem nessas cargas. Os contrabandistas dividem tudo em maletas pequenas e trancadas, o mesmo truque que o Skier e eu já usamos. Me traga uma das maletas deles e uma chave que abra.",
        "successMessageText": "Miras térmicas, e só parte de um conjunto. Eles dividem o próprio equipamento, não só as cargas. Espertos.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""}})

q = quest(SILVER, "Thirty Pieces of Silver", PEACEKEEPER, "Elimination", after(SILVER, DELIVERY),
          [kills(SILVER, 0, 25), handover(SILVER, 1, [CONTRABAND], 3), handover(SILVER, 2, SEALED_CASES, 3)],
          rewards(SILVER, 15000, (USD, 1400), None,
                  [("65290f395ae2ae97b80fdf2d", 1), ("65293c7a17e14363030ad308", 6), ("67600a516f01341c9106ab4c", 10)]))
add(q, [("Eliminate Smugglers", "Elimine Contrabandistas"),
        ("Obtain and hand over contraband boxes", "Consiga e entregue caixas de contrabando"),
        ("Obtain and hand over sealed weapon cases", "Consiga e entregue caixas lacradas de armas")],
    {"en": {
        "description": "I still do not know who receives all of this. Bring me more of their cargo, contraband and sealed weapon cases, and I will trace the serial numbers. And make it clear to the smugglers that business with Europe goes through me.",
        "successMessageText": "The paperwork looks clean, which is exactly what a professional would make it look like. Most of it is soldier kit. I know who wanted it. Rest for now.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""},
     "po": {
        "description": "Ainda não sei quem recebe tudo isso. Me traga mais carga deles, contrabando e caixas lacradas de armas, e eu vou rastrear os números de série. E deixe claro para os contrabandistas que negócio com a Europa passa por mim.",
        "successMessageText": "A papelada parece limpa, exatamente como um profissional faria parecer. A maior parte é equipamento de soldado. Eu sei quem queria isso. Descanse por enquanto.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""}})

q = quest(RESTOCK, "Restocking Supplies", MECHANIC, "PickUp", after(RESTOCK, SILVER),
          [handover(RESTOCK, 0, SEALED_CASES, 10)],
          rewards(RESTOCK, 13000, (EUR, 1700), (MECHANIC, 0.05),
                  [("64637076203536ad5600c990", 1), ("646372518610c40fc20204e8", 2), ("560d75f54bdc2da74d8b4573", 15)]))
add(q, [("Obtain and hand over sealed weapon cases", "Consiga e entregue caixas lacradas de armas")],
    {"en": {
        "description": "I hear NATO crates are falling into the city and Peacekeeper has nothing to do with it. I do not care about the politics. If there are weapons in those cases, I want to look at them. Bring me some.",
        "successMessageText": "That will do. Most of these are in perfect shape, the rest only need cleaning. Thanks.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""},
     "po": {
        "description": "Ouvi dizer que caixas da OTAN estão caindo na cidade e o Peacekeeper não tem nada a ver com isso. Não me importo com a política. Se tem armas nessas caixas, quero dar uma olhada. Me traga algumas.",
        "successMessageText": "Isso basta. A maioria está perfeita, o resto só precisa de uma limpeza. Obrigado.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""}})

q = quest(ROOT, "Root of All Evil", RAGMAN, "PickUp", after(ROOT, RESTOCK),
          [stash(ROOT, 0, [CONTRABAND], 10, "place_skier_12_3")],
          rewards(ROOT, 12000, (RUB, 150000), None,
                  [("5b44cf1486f77431723e3d05", 1), ("5c0e746986f7741453628fe5", 1)]))
add(q, [("Stash contraband boxes in the middle wooden cabin at the sawmill on Woods", "Esconda caixas de contrabando na cabana de madeira do meio da serraria na Woods")],
    {"en": {
        "description": "Brother, I am in trouble. That man across the border found my weak spot, and now Peacekeeper wants to put him away for good. He needs proof, and one box proves nothing. Take ten contraband boxes to the sawmill on Woods and leave them in the middle wooden cabin.",
        "successMessageText": "You saved me again! With this he should be gone for a long time. Things will calm down, eventually.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""},
     "po": {
        "description": "Irmão, estou encrencado. Aquele homem do outro lado da fronteira achou meu ponto fraco, e agora o Peacekeeper quer tirá-lo de cena de vez. Ele precisa de provas, e uma caixa só não prova nada. Leve dez caixas de contrabando para a serraria na Woods e deixe na cabana de madeira do meio.",
        "successMessageText": "Você me salvou de novo! Com isso ele deve sumir por um bom tempo. As coisas vão se acalmar, uma hora.",
        "failMessageText": "", "acceptPlayerMessage": "", "declinePlayerMessage": "", "completePlayerMessage": "",
        "startedMessageText": ""}})

OUT.mkdir(exist_ok=True)
(OUT / "smugglerQuests.json").write_text(json.dumps(quests, ensure_ascii=False, indent=1), encoding="utf-8")
(OUT / "smugglerLocales.json").write_text(json.dumps(texts, ensure_ascii=False, indent=1), encoding="utf-8")
print(len(quests), "quests,", len(SEALED_CASES), "sealed case variants,", {k: len(v) for k, v in texts.items()}, "locale keys")
print({n: v for n, v in [("bulldogs", BULLDOGS), ("delivery", DELIVERY), ("silver", SILVER), ("restock", RESTOCK), ("root", ROOT)]})
