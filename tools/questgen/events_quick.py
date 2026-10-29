import json
import re

from lib import *

SKIER, PEACEKEEPER, PRAPOR, MECHANIC, RAGMAN = (TRADERS[k] for k in ("Skier", "Peacekeeper", "Prapor", "Mechanic", "Ragman"))
LOCKED_CASE, CASE_KEY, CONTRABAND = "6740987b89d5e1ddc603f4f0", "67449b6c89d5e1ddc603f504", "67408903268737ef6908d432"
OPENED_CASE, OPENED_BOX = "674078c4a9c9adf0450d59f9", "674098588466ebb03408b210"
SEALED_CASES = sorted(i for i, x in items.items() if x["_parent"] == "62f109593b54472778797866" and x["_props"].get("Width") == 5 and x["_props"].get("Height") == 2)
SMUGGLER, GUS, BASMACH = ["ppesSmuggler"], ["ppesGus"], ["ppesBasmach"]


def existing(name):
    for qid, x in stock.items():
        if norm(loc.get(f"{qid} name", x["QuestName"])) == norm(name):
            return qid
    raise KeyError(name)


for key, name in [("burningRubber", "Burning Rubber"), ("caughtRedHanded", "Caught Red-Handed"), ("debut", "Debut"),
                  ("tarkovMystery", "The Tarkov Mystery"), ("fishingGear", "Fishing Gear")]:
    EXISTING[key] = existing(name)


def story(en_desc, pt_desc, en_ok, pt_ok):
    return {"desc": (en_desc, pt_desc), "success": (en_ok, pt_ok)}


# ---------------- Smugglers (2024): original ids and texts ----------------
add(
    Q("walter", "smugglers2024", [handover("Locate and hand over the locked cases with cargo", "Encontre e entregue as maletas trancadas com a carga", LOCKED_CASE, 3, cid="67464892f25c4371c2242a1a")],
      name="That’s a Great Plan, Walter", after=[("burningRubber", (2, 4))], ids=["674647f38466ebb03408b291"], crafts=[OPENED_CASE, OPENED_BOX]),
    Q("chop", "smugglers2024", [kill('Eliminate the "Smugglers"', 'Elimine os "Contrabandistas"', 25, "Savage", SMUGGLER, cid="67464973800dfc0ed382cc34")],
      name="Chop the Thieving Hand", after="walter", ids=["6746480cd0b2f8eb9b034e3e"]),
    Q("shell", "smugglers2024", [handover("Hand over Skier's contraband from the locked cases", "Entregue o contrabando do Skier tirado das maletas trancadas", CONTRABAND, 15, cid="67464916bbc0297381fcf753")],
      name="Shell Game", after="chop", ids=["6746480040ea3d1d4f0e5d51"]),
)
for q in ALL[-3:]:
    q.requires = "ppesSmuggler"

# ---------------- Smugglers (2026): new texts ----------------
S26 = ["69ae8e9d8d5952d853d791a1", "f4411dd1f99ce3678f7bd579", "34f3c3bb54f8d18fd4a320c3", "0c472b7ba2915b42ba295a26", "68a6dfb24982a975f038440b"]
add(
    Q("bulldogs", "smugglers2026", [
        kill("Eliminate a smuggler on any location", "Elimine um contrabandista em qualquer mapa", 1, "Savage", SMUGGLER),
        visit("Locate the smuggler's camp on Shoreline", "Encontre o acampamento dos contrabandistas na Shoreline", "q14_10_point_area"),
        handover("Hand over a contraband box as evidence of the smugglers' ties to Edward", "Entregue uma caixa de contrabando como prova da ligação dos contrabandistas com o Edward", CONTRABAND)],
      name="Bulldogs Under The Rug", level=15, ids=[S26[0]],
      **story("Someone across the border wants my seat at the council, and I think he is using the smugglers to get it. Airdrops are up, and so is smuggler traffic around the bay. Thin out their ranks, find the camp they use on Shoreline and bring me something that proves who they work for.",
              "Alguém do outro lado da fronteira quer o meu lugar no conselho, e acho que está usando os contrabandistas para isso. Os airdrops aumentaram, e o movimento de contrabandistas na baía também. Diminua o número deles, encontre o acampamento que usam na Shoreline e me traga algo que prove para quem trabalham.",
              "So it is true. He is moving goods on my ground and buying the local gangs with them. We will deal with this.",
              "Então é verdade. Ele está passando mercadoria no meu território e comprando as gangues locais com ela. Vamos resolver isso.")),
    Q("delivery", "smugglers2026", [
        handover("Obtain and hand over the locked smugglers' case", "Consiga e entregue a maleta trancada dos contrabandistas", LOCKED_CASE),
        handover("Obtain and hand over the key to the locked case", "Consiga e entregue a chave da maleta trancada", CASE_KEY)],
      name="Accept Delivery", after="bulldogs", ids=[S26[1]],
      **story("I need to see what is in those shipments. The smugglers split everything into small locked cases, the same trick Skier and I once used. Bring me one of their cases and a key that opens it.",
              "Preciso ver o que tem nessas cargas. Os contrabandistas dividem tudo em maletas pequenas e trancadas, o mesmo truque que o Skier e eu já usamos. Me traga uma das maletas deles e uma chave que abra.",
              "Thermal optics, and only part of a set. They split the gear itself, not just the shipments. Clever.",
              "Miras térmicas, e só parte de um conjunto. Eles dividem o próprio equipamento, não só as cargas. Espertos.")),
    Q("silver", "smugglers2026", [
        kill("Eliminate 25 smugglers", "Elimine 25 contrabandistas", 25, "Savage", SMUGGLER),
        handover("Obtain and hand over the contraband boxes", "Consiga e entregue as caixas de contrabando", CONTRABAND, 3),
        handover("Obtain and hand over the sealed weapon cases", "Consiga e entregue as caixas lacradas de armas", SEALED_CASES, 3)],
      name="Thirty Pieces of Silver", after="delivery", ids=[S26[2]],
      **story("I still do not know who receives all of this. Bring me more of their cargo, contraband and sealed weapon cases, and I will trace the serial numbers. And make it clear to the smugglers that business with Europe goes through me.",
              "Ainda não sei quem recebe tudo isso. Me traga mais carga deles, contrabando e caixas lacradas de armas, e eu vou rastrear os números de série. E deixe claro para os contrabandistas que negócio com a Europa passa por mim.",
              "The paperwork looks clean, which is exactly what a professional would make it look like. Most of it is soldier kit. I know who wanted it. Rest for now.",
              "A papelada parece limpa, exatamente como um profissional faria parecer. A maior parte é equipamento de soldado. Eu sei quem queria isso. Descanse por enquanto.")),
    Q("restock", "smugglers2026", [handover("Obtain and hand over the sealed weapon cases", "Consiga e entregue as caixas lacradas de armas", SEALED_CASES, 10)],
      name="Restocking Supplies", after="silver", ids=[S26[3]],
      **story("I hear NATO crates are falling into the city and Peacekeeper has nothing to do with it. I do not care about the politics. If there are weapons in those cases, I want to look at them. Bring me some.",
              "Ouvi dizer que caixas da OTAN estão caindo na cidade e o Peacekeeper não tem nada a ver com isso. Não me importo com a política. Se tem armas nessas caixas, quero dar uma olhada. Me traga algumas.",
              "That will do. Most of these are in perfect shape, the rest only need cleaning. Thanks.",
              "Isso basta. A maioria está perfeita, o resto só precisa de uma limpeza. Obrigado.")),
    Q("root", "smugglers2026", [stash("Stash the contraband boxes near the green shipping container at the sawmill on Woods", "Esconda as caixas de contrabando perto do contêiner verde da serraria na Woods", CONTRABAND, "place_skier_12_3", 10)],
      name="Root of All Evil", after="restock", ids=[S26[4]],
      **story("Brother, I am in trouble. That man across the border found my weak spot, and now Peacekeeper wants to put him away for good. He needs proof, and one box proves nothing. Take ten contraband boxes to the sawmill on Woods and leave them by the cabins there.",
              "Irmão, estou encrencado. Aquele homem do outro lado da fronteira achou meu ponto fraco, e agora o Peacekeeper quer tirá-lo de cena de vez. Ele precisa de provas, e uma caixa só não prova nada. Leve dez caixas de contrabando para a serraria na Woods e deixe perto das cabanas.",
              "You saved me again! With this he should be gone for a long time. Things will calm down, eventually.",
              "Você me salvou de novo! Com isso ele deve sumir por um bom tempo. As coisas vão se acalmar, uma hora.")),
)
for q in ALL[-5:]:
    q.requires = "ppesSmuggler"

# ---------------- Sanitar Event: Letters, from SPT's archived template ----------------
_arch = json.loads((DB / "templates/archivedQuests.json").read_text(encoding="utf-8"))["636a2f5759cb834f8a5825d6"]
_letters = []
for c in _arch["conditions"]["AvailableForFinish"]:
    k = c["_props"]["counter"]["conditions"][0]["_props"]
    _letters.append(kill(loc.get(c["_props"]["id"], ""), "", 1, k["target"], k["savageRole"], weapons=k["weapon"], cid=c["_props"]["id"]))
add(Q("letters", "sanitarEvent", _letters, name="Letters", ids=["636a2f5759cb834f8a5825d6"], level=1))

# ---------------- Softcore: Staying Light ----------------
add(Q("stayingLight", "softcore", [kill("Eliminate any targets", "Elimine quaisquer alvos", 100, "Any")], name="Staying Light", trader=PRAPOR))

# ---------------- Wild Wild Woods ----------------
BADGE = tpl("Silver Badge")
add(Q("wildWildWoods", "wildWildWoods", [
    find("Find Silver Badges in raid", "Encontre Silver Badges na raid", BADGE, 12),
    handover("Hand over the found in raid Silver Badges", "Entregue as Silver Badges encontradas na raid", BADGE, 12, fir=True)],
    name="Wild Wild Woods", trader=TRADERS["Jaeger"], level=10,
    **story("Somebody on Woods is playing sheriff, handing out silver badges like it is the Wild West. Collect a dozen of them before this nonsense gets someone killed. I want to know who is behind it.",
            "Alguém na Woods está brincando de xerife, distribuindo distintivos de prata como se fosse o Velho Oeste. Junte uma dúzia deles antes que essa bobagem mate alguém. Quero saber quem está por trás disso.",
            "Twelve tin stars. Whoever made these had too much free time. Good work, partner.",
            "Doze estrelas de lata. Quem fez isso tinha tempo livre demais. Bom trabalho, parceiro.")))

# ---------------- The Bosses Gathering ----------------
add(Q("bossesGathering", "bossesGathering", [
    kill("Locate and eliminate a boss at the meeting on Reserve", "Encontre e elimine um chefe na reunião da Reserve", 1, "Savage",
         ["bossBully", "bossKilla", "bossGluhar", "bossKojaniy", "bossSanitar", "bossTagilla", "bossKolontay"], map="reserve"),
    extract("Survive and extract from Reserve", "Sobreviva e saia da Reserve", "reserve")],
    name="The Bosses gathering", level=15,
    **story("My people say every boss in the region is meeting on Reserve. Something big is being decided there, and I want a front row seat. Go there, put one of them down so they know we are watching, and get out alive.",
            "Meu pessoal diz que todos os chefes da região estão se reunindo na Reserve. Algo grande está sendo decidido lá, e eu quero assistir de perto. Vá lá, derrube um deles para saberem que estamos de olho, e saia vivo.",
            "So the rumors were true. Now they know someone is watching. Good.",
            "Então os boatos eram verdade. Agora eles sabem que alguém está de olho. Ótimo.")))

# ---------------- Arms Race: Gus and Basmach are recreated as PPES bosses ----------------
_weapons = category("5422acb9af1c889c16000029")
_weapons = [i for i in _weapons if items[i]["_type"] == "Item" and items[i]["_props"].get("weapClass")]
RUSSIAN = re.compile(r"\b(AK|AKS|AKM|AKMN|AKMS|AKMSN|RPK|PKM|PKP|SVD|SVDS|SKS|OP-SKS|VSS|AS VAL|VAL|Vintorez|PP-19|PP-91|PP-9|Kedr|PPSh|Mosin|SV-98|SR-1MP|SR-2M|SR-3M|9A-91|Saiga|Vepr|VPO|MP-133|MP-153|MP-155|MP-43|MP-18|MP-443|MP-9|TOZ|KS-23|Makarov|PM|PB|APS|APB|TT|RSh-12|ASh-12|NSV|Utyos|AGS|KBP|Molot|Izhmash|Lebedev|PL-15|OTs|Kalashnikov|Simonov|Dragunov|Baikal|Yarygin|Stechkin|RPD|AK-74|AK-74M|AK-74N|AK-101|AK-102|AK-103|AK-104|AK-105|AK-12|AK-15|AK-19|AK-308|AK-545|AKS-74|AKS-74U|AKS-74N|AKS-74UB|AKS-74UN|SAG|PP-2000|Bizon|Vityaz|SR-25)\b")
_names_en = {i: loc.get(f"{i} Name", "") for i in _weapons}
RUSSIAN_WEAPONS = sorted(i for i, n in _names_en.items() if RUSSIAN.search(n) and "SR-25" not in n)
WESTERN_WEAPONS = sorted(i for i in _weapons if i not in RUSSIAN_WEAPONS and _names_en[i])

add(
    Q("smallBusiness", "armsRace", [
        kill("Locate and eliminate Basmach on Streets of Tarkov", "Encontre e elimine o Basmach em Streets of Tarkov", 1, "Savage", BASMACH, map="streets"),
        kill("Locate and eliminate Gus on Streets of Tarkov", "Encontre e elimine o Gus em Streets of Tarkov", 1, "Savage", GUS, map="streets")],
      name="Risks of Small Business", after="caughtRedHanded", exclusive="armsRaceSide", requires="ppesGus"),
    Q("protection", "armsRace", [kill("Eliminate PMC operatives on Streets of Tarkov", "Elimine operadores PMC em Streets of Tarkov", 3, "AnyPmc", map="streets")],
      name="Protection of Interests", after="caughtRedHanded", exclusive="armsRaceSide", requires="ppesGus",
      fail=[kill("Do not kill Gus or Basmach while the quest is active", "Não mate o Gus nem o Basmach enquanto a quest estiver ativa", 1, "Savage", GUS + BASMACH)]),
    Q("western", "armsRace", [kill("Eliminate targets while using Western weapons", "Elimine alvos usando armas ocidentais", 50, "Any", weapons=WESTERN_WEAPONS)],
      name="Western Quality Standards", trader=PEACEKEEPER, after="smallBusiness"),
    Q("timeTested", "armsRace", [kill("Eliminate targets while using Russian weapons", "Elimine alvos usando armas russas", 50, "Any", weapons=RUSSIAN_WEAPONS)],
      name="Time-Tested", trader=PRAPOR, after="protection"),
)
