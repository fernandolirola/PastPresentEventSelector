from lib import *
from events_quick import RUSSIAN_WEAPONS

T = TRADERS
ROGUE = ["exUsec"]
GLUKHAR_GUARDS = ["followerGluharAssault", "followerGluharScout", "followerGluharSecurity", "followerGluharSnipe"]
INTEL = "5c12613b86f7743bbe2c3f76"
CAMERA = tpl("WI-FI Camera")


def s(en_d, pt_d, en_s, pt_s):
    return {"desc": (en_d, pt_d), "success": (en_s, pt_s)}


# ---------------- Shifting Balance ----------------
E = "shiftingBalance"
add(
    Q("fogOfWar", E, [visit(f"Scout the Rogues' positions from the roof of the {n} water treatment plant facility on Lighthouse",
                            f"Observe as posições dos Rogues do telhado do {p} prédio da estação de tratamento de água na Lighthouse", f"qlight_extension_prapor1_exploration{i}")
                      for i, n, p in ((1, "first", "primeiro"), (2, "second", "segundo"), (3, "third", "terceiro"))],
      name="Fog of War", level=15,
      **s("The balance on Lighthouse is shifting. The Rogues are spread thin and I want to move in. Climb up and tell me what they have left.",
          "O equilíbrio na Lighthouse está mudando. Os Rogues estão espalhados e eu quero entrar. Suba e me diga o que sobrou para eles.",
          "Fewer than I expected. Good.", "Menos do que eu esperava. Ótimo.")),
    Q("numberUnavailable", E, [
        stash("Stash Electronic components at Prapor's new camp in the yacht club cottage on Lighthouse", "Esconda componentes eletrônicos no novo acampamento do Prapor no chalé do iate clube na Lighthouse", tpl("Electronic components"), "em_quest5_1", 3),
        stash("Stash a Military cable at Prapor's new camp in the yacht club cottage on Lighthouse", "Esconda um cabo militar no novo acampamento do Prapor no chalé do iate clube na Lighthouse", tpl("Military cable"), "em_quest5_1"),
        stash("Stash Capacitors at Prapor's new camp in the yacht club cottage on Lighthouse", "Esconda capacitores no novo acampamento do Prapor no chalé do iate clube na Lighthouse", tpl("Capacitors"), "em_quest5_1", 3)],
      name="Number Temporarily Unavailable", after="fogOfWar",
      **s("My boys need a working radio at the new camp. Bring the parts and leave them there.",
          "Meus rapazes precisam de um rádio funcionando no novo acampamento. Traga as peças e deixe lá.",
          "Now they can call home.", "Agora eles podem ligar para casa.")),
    Q("allInclusive", E, [
        kill("Eliminate any targets on Lighthouse", "Elimine quaisquer alvos na Lighthouse", 15, "Any", map="lighthouse"),
        flare("Launch a signal flare at the train station warehouse on Lighthouse", "Dispare um sinalizador no armazém da estação de trem na Lighthouse", "Final_flare_btr_2"),
        extract("Use the transit from Lighthouse to Reserve", "Use o trânsito da Lighthouse para a Reserve", "lighthouse", status=("Transit",))],
      name="All-Inclusive Support", after="numberUnavailable",
      **s("Clear the way, signal my people and then head to Reserve. We are expanding.",
          "Abra caminho, sinalize para o meu pessoal e depois vá para a Reserve. Estamos expandindo.",
          "Full service. My people are moving in.", "Serviço completo. Meu pessoal está entrando.")),
    Q("familiarFace", E, [extract("Extract from Lighthouse through the Armored Train while wearing a Kirasa-N body armor", "Saia da Lighthouse pelo trem blindado usando um colete Kirasa-N",
                                  "lighthouse", exit_name="EXFIL_Train", wear=tpls("BNTI Kirasa-N body armor", "BNTI Kirasa-N body armor (Green)"))],
      name="A Familiar Face...", after="allInclusive",
      **s("The train crew knows a certain armor from the old days. Ride out wearing a Kirasa and they will think you are one of theirs.",
          "A tripulação do trem conhece um certo colete dos velhos tempos. Saia usando uma Kirasa e eles vão achar que você é um deles.",
          "They bought it. A familiar face opens doors.", "Eles caíram. Um rosto conhecido abre portas.")),
    Q("scopeClearing", E, [mark(f"Plant the {n} WI-FI Camera at the specified spot on Lighthouse", f"Instale a {p} WI-FI Camera no local indicado na Lighthouse", f"meh_53_wificamera_area_mark_{i}", "WI-FI Camera")
                           for i, n, p in ((1, "first", "primeira"), (2, "second", "segunda"), (3, "third", "terceira"))],
      name="Scope the Clearing", after="familiarFace",
      **s("I want to see who uses the smuggler routes now that the Rogues are weak. Put cameras up.",
          "Quero ver quem usa as rotas dos contrabandistas agora que os Rogues estão fracos. Coloque as câmeras.",
          "Clear picture. Thank you.", "Imagem limpa. Obrigado.")),
    Q("loseTail", E, [
        kill("Eliminate Glukhar's guards on Lighthouse", "Elimine os guardas do Glukhar na Lighthouse", 5, "Savage", GLUKHAR_GUARDS, map="lighthouse"),
        kill("Locate and neutralize Glukhar on Lighthouse", "Encontre e neutralize o Glukhar na Lighthouse", 1, "Savage", ["bossGluhar"], map="lighthouse")],
      name="Lose the Tail", after="allInclusive",
      **s("Glukhar followed us to Lighthouse. Lose him, permanently.", "O Glukhar nos seguiu até a Lighthouse. Livre-se dele, de vez.",
          "No more tail.", "Sem mais ninguém na nossa cola.")),
    Q("malachite", E, [
        visit("Search the radar station commandant's office on Lighthouse for the BEAR operative's file", "Procure a ficha do operador BEAR no escritório do comandante da estação de radar na Lighthouse", "qlight_extension_bariga1_exploration1"),
        handover("Hand over the found intel", "Entregue as informações encontradas", INTEL)],
      name="Callsign Malachite", after="loseTail",
      **s("A BEAR operative called Malachite worked with the Rogues. The commandant kept files on everyone. Find his.",
          "Um operador BEAR chamado Malaquita trabalhava com os Rogues. O comandante guardava fichas de todo mundo. Encontre a dele.",
          "So that is who he was.", "Então era ele.")),
    Q("easeMove", E, [mark(f"Locate and mark the {n} Rogue vehicle with an MS2000 Marker on Lighthouse", f"Encontre e marque o {p} veículo dos Rogues com um MS2000 Marker na Lighthouse", f"qlight_mark_vech{i}")
                      for i, n, p in ((1, "first", "primeiro"), (2, "second", "segundo"), (3, "third", "terceiro"))],
      name="Ease the Move", after="malachite",
      **s("The Rogues will try to move their gear. Mark their vehicles so my people know what to take.",
          "Os Rogues vão tentar mover o equipamento. Marque os veículos para o meu pessoal saber o que levar.",
          "We will take it from here.", "Daqui em diante é com a gente.")),
    Q("cutRoot", E, [kill("Eliminate Rogues on Lighthouse", "Elimine Rogues na Lighthouse", 15, "Savage", ROGUE, map="lighthouse")],
      name="Cut Down to the Root", after="malachite", rivals=["preserve"],
      **s("The Rogues poisoned the water plant once. Cut them out, all of them.", "Os Rogues já envenenaram a estação de água uma vez. Acabe com eles, todos.",
          "The forest breathes easier.", "A floresta respira melhor.")),
    Q("preserve", E, [kill("Eliminate PMC operatives on Lighthouse", "Elimine operadores PMC na Lighthouse", 7, "AnyPmc", map="lighthouse")],
      name="Preserve a Valuable Piece", after="malachite", rivals=["cutRoot"],
      **s("The Rogues are still useful to me. Keep the PMCs from finishing them.", "Os Rogues ainda me são úteis. Impeça os PMCs de acabarem com eles.",
          "The balance holds.", "O equilíbrio se mantém.")),
)

# ---------------- The TerraGroup Trail ----------------
E = "terraGroupTrail"
MAP_NAMES = (("streets", "Streets of Tarkov", "Streets of Tarkov"), ("interchange", "Interchange", "Interchange"), ("customs", "Customs", "Customs"),
             ("factory", "Factory", "Factory"), ("woods", "Woods", "Woods"), ("reserve", "Reserve", "Reserve"), ("lighthouse", "Lighthouse", "Lighthouse"),
             ("shoreline", "Shoreline", "Shoreline"))
LAPTOP = tpl("Toughbook reinforced laptop")
FLASH = "590c621186f774138d11ea29"
add(
    Q("trail1", E, [
        visit("Locate Fence's men who went missing at the Health Resort on Shoreline", "Encontre os homens do Fence que sumiram no Health Resort da Shoreline", "ter_023_area_1_1"),
        extract("Survive and extract from the location", "Sobreviva e saia do mapa", "shoreline")],
      name="The TerraGroup Trail - Part 1", after=[("debut", (2, 3, 4))],
      **s("Some of my guys went to the Health Resort and never came back. Find out what happened.",
          "Alguns dos meus caras foram ao Health Resort e nunca voltaram. Descubra o que aconteceu.",
          "Dead, and not by bullets. Strange business.", "Mortos, e não por bala. Negócio estranho.")),
    Q("trail2", E, [
        kill("Eliminate Scavs acting strangely on Shoreline", "Elimine Scavs agindo de forma estranha na Shoreline", 2, "Savage", map="shoreline"),
        visit("Find the source of their abnormal behavior in the reservoir room of the Health Resort", "Encontre a origem do comportamento anormal na sala dos reservatórios do Health Resort", "place_peacemaker_009_2"),
        handover("Hand over the found evidence (a Blood sample)", "Entregue a prova encontrada (uma amostra de sangue)", tpl("Blood sample"))],
      name="The TerraGroup Trail - Part 2", after="trail1",
      **s("Fence says his men died strangely. I have seen Scavs on Shoreline act like animals. Find out why, and bring me a sample.",
          "O Fence diz que os homens dele morreram de forma estranha. Já vi Scavs na Shoreline agindo como bichos. Descubra o porquê e me traga uma amostra.",
          "TerraGroup chemistry. I knew it.", "Química da TerraGroup. Eu sabia.")),
    Q("trail3", E, [
        visit("Locate the helicopter at the water treatment plant on Lighthouse", "Encontre o helicóptero na estação de tratamento de água na Lighthouse", "qlight_pr1_heli1_find"),
        find("Obtain a reinforced laptop", "Consiga um laptop reforçado", LAPTOP)],
      name="The TerraGroup Trail - Part 3", after="trail2",
      **s("TerraGroup people were seen by the helicopter on Lighthouse. They carried a laptop. Find one.",
          "Gente da TerraGroup foi vista perto do helicóptero na Lighthouse. Eles carregavam um laptop. Encontre um.",
          "Keep it safe, we will need it.", "Guarde bem, vamos precisar.")),
    Q("trail4", E, [
        visit("Locate the Rogues' meeting spot by the breakwater on Shoreline", "Encontre o ponto de encontro dos Rogues perto do quebra-mar na Shoreline", "place_peacemaker_001"),
        kill("Disrupt the deal", "Atrapalhe o negócio", 3, "Any", map="shoreline"),
        extract("Survive and extract from the location", "Sobreviva e saia do mapa", "shoreline")],
      name="The TerraGroup Trail - Part 4", after="trail3",
      **s("The Rogues are buying from TerraGroup. Find where they meet and ruin the deal.",
          "Os Rogues estão comprando da TerraGroup. Descubra onde se encontram e estrague o negócio.",
          "No deal today.", "Nada de negócio hoje.")),
    Q("trail5", E, [handover("Hand over the laptop to Mechanic for decoding", "Entregue o laptop para o Mechanic decodificar", LAPTOP)],
      name="The TerraGroup Trail - Part 5", trader=T["Mechanic"], after="trail4",
      **s("Bring me that laptop. TerraGroup encryption is old but stubborn.", "Me traga aquele laptop. A criptografia da TerraGroup é antiga, mas teimosa.",
          "Done. There are names in here.", "Pronto. Tem nomes aqui.")),
    Q("trail6", E, [handover("Hand over Physical Bitcoins", "Entregue Physical Bitcoins", tpl("Physical Bitcoin"), 5)],
      name="The TerraGroup Trail - Part 6", trader=T["Fence"], after="trail5",
      **s("The names on that laptop cost money to follow. Pay up and I will open my network.",
          "Seguir os nomes daquele laptop custa dinheiro. Pague e eu abro minha rede.",
          "Now we are partners.", "Agora somos sócios.")),
    Q("trail7", E, [handover("Pay Peacekeeper for his cooperation", "Pague o Peacekeeper pela cooperação dele", USD, 3000)],
      name="The TerraGroup Trail - Part 7", trader=T["Peacekeeper"], after="trail6",
      **s("One of the names is a UN officer. I can make him disappear from the lists, for a price.",
          "Um dos nomes é um oficial da ONU. Posso fazer ele sumir das listas, por um preço.",
          "A pleasure doing business.", "Um prazer fazer negócios.")),
    Q("trail8", E, [kill(f"Eliminate Scavs while using Russian weapons on {en}", f"Elimine Scavs usando armas russas em {pt}", 10, "Savage", map=k, weapons=RUSSIAN_WEAPONS)
                    for k, en, pt in MAP_NAMES],
      name="The TerraGroup Trail - Part 8", after="trail7",
      **s("TerraGroup wants this to look like an army operation. Let us give them exactly that, with Russian weapons, across the whole region.",
          "A TerraGroup quer que isso pareça uma operação do exército. Vamos dar exatamente isso, com armas russas, em toda a região.",
          "Nobody will suspect the UN now.", "Ninguém vai suspeitar da ONU agora.")),
    Q("trail9", E, [
        stash("Stash Bottles of Dan Jackiel whiskey at the specified place on Interchange", "Esconda garrafas de uísque Dan Jackiel no local indicado no Interchange", tpl("Bottle of Dan Jackiel whiskey"), "place_merch_020_1", 2),
        stash("Stash a SP-8 Survival Machete at the laboratory storage room on Factory", "Esconda um facão SP-8 no depósito do laboratório da Factory", tpl("SP-8 Survival Machete"), "Place_accurate_tools"),
        stash("Stash a Desert Tech MDR 7.62x51 assault rifle in the trash on the 3rd floor of the dorm on Customs", "Esconda um fuzil Desert Tech MDR 7.62x51 no lixo do 3º andar do dormitório na Customs", tpl("Desert Tech MDR 7.62x51 assault rifle"), "Q019_3"),
        stash("Stash EOTech HHS-1 hybrid sights at the sawmill docks on Woods", "Esconda miras EOTech HHS-1 no cais da serraria na Woods", tpls("EOTech HHS-1 hybrid sight", "EOTech HHS-1 hybrid sight (Tan)"), "place_THX_15", 2),
        stash("Stash L3Harris GPNVG-18 night vision goggles at the sandbags next to the LAV III on Reserve", "Esconda óculos de visão noturna L3Harris GPNVG-18 nos sacos de areia ao lado do LAV III na Reserve", tpl("L3Harris GPNVG-18 night vision goggles"), "Place_item_tools_rezerve")],
      name="The TerraGroup Trail - Part 9", after="trail8",
      **s("My informants need paying, and they all have expensive tastes. Leave the gifts where they will find them.",
          "Meus informantes precisam ser pagos, e todos têm gostos caros. Deixe os presentes onde eles vão encontrar.",
          "They are talking now.", "Agora eles estão falando.")),
    Q("trail10", E, [
        kill("Eliminate any targets while using a VSS Vintorez on Streets of Tarkov", "Elimine quaisquer alvos usando um VSS Vintorez em Streets of Tarkov", 30, "Any", map="streets", weapons=[tpl("VSS Vintorez 9x39 special sniper rifle")]),
        stash("Plant Sanitar's bag under Kaban's office on Streets of Tarkov", "Plante a bolsa do Sanitar embaixo do escritório do Kaban em Streets of Tarkov", tpl("Sanitar's bag"), "quest_city_trotil2")],
      name="The TerraGroup Trail - Part 10", after="trail9",
      **s("Kaban and Sanitar are both in TerraGroup's pocket. Turn them against each other.",
          "O Kaban e o Sanitar estão os dois no bolso da TerraGroup. Coloque um contra o outro.",
          "Let them tear each other apart.", "Que eles se destruam.")),
    Q("trail11", E, [
        mark("Plant a radio repeater inside the Mi-8 helicopter at the water treatment plant on Lighthouse", "Instale um repetidor de rádio dentro do helicóptero Mi-8 na estação de tratamento de água da Lighthouse", "meh_42_radio_area_mark_1", "Radio repeater"),
        mark("Plant a radio repeater inside the abandoned USEC cottage on Lighthouse", "Instale um repetidor de rádio dentro do chalé USEC abandonado na Lighthouse", "meh_42_radio_area_mark_2", "Radio repeater"),
        mark("Plant a radio repeater on the hill in front of the Lighthouse island", "Instale um repetidor de rádio no morro em frente à ilha da Lighthouse", "meh_42_radio_area_mark_3", "Radio repeater"),
        mark("Plant a radio repeater on the roof of the landing stage on Lighthouse", "Instale um repetidor de rádio no telhado do cais da Lighthouse", "meh_42_radio_area_mark_4", "Radio repeater")],
      name="The TerraGroup Trail - Part 11", after="trail6",
      **s("The laptop talks to something on Lighthouse. I need repeaters up to catch the signal.",
          "O laptop se comunica com algo na Lighthouse. Preciso de repetidores para pegar o sinal.",
          "Signal is coming through.", "O sinal está chegando.")),
    Q("trail12", E, [handover(f"Hand over {en}", f"Entregue {pt}", tpl(n)) for n, en, pt in (
        ("Piece of plexiglass", "a Piece of plexiglass", "um pedaço de acrílico"), ("Insulating tape", "an Insulating tape", "uma fita isolante"),
        ("Military circuit board", "a Military circuit board", "uma placa de circuito militar"),
        ("Military COFDM Wireless Signal Transmitter", "a Military COFDM Wireless Signal Transmitter", "um transmissor militar COFDM"))],
      name="The TerraGroup Trail - Part 12", trader=T["Mechanic"], after="trail11",
      **s("To read that signal I need to build a receiver. Bring me the parts.", "Para ler esse sinal preciso montar um receptor. Me traga as peças.",
          "It works.", "Funciona.")),
    Q("trail13", E, [
        find("Obtain a Secure Flash drive", "Consiga um Secure Flash drive", FLASH),
        visit("Reflash the radio transmitter near the Lighthouse peninsula", "Reprograme o transmissor de rádio perto da península da Lighthouse", "meh_48_transponder_area_check_1"),
        handover("Hand over the flash drive to Mechanic", "Entregue o pendrive para o Mechanic", FLASH),
        visit("Visit the Lighthouse building", "Visite o prédio do farol", "meh_50_visit_area_check_1")],
      name="The TerraGroup Trail - Part 13", after="trail12",
      **s("The transmitter on the peninsula needs new firmware. Load it, bring me the drive, and then go see the Lighthouse keeper. He is expecting you.",
          "O transmissor na península precisa de firmware novo. Carregue, me traga o pendrive e depois vá ver o faroleiro. Ele está esperando você.",
          "The keeper will take it from here.", "O faroleiro assume daqui.")),
    Q("trail14", E, [
        visit("Search the weapon testing area in The Lab for Reagent Omega", "Procure o Reagente Ômega na área de testes de armas da Lab", "quest_zone_keeper7_saferoom"),
        visit("Search the human experiment room in The Lab for Reagent Beta", "Procure o Reagente Beta na sala de experimentos humanos da Lab", "quest_zone_keeper7_test"),
        visit("Search Sanitar's workplace in The Lab for Reagent Alpha", "Procure o Reagente Alfa no local de trabalho do Sanitar na Lab", "peace_027_area"),
        handover("Hand over a reagent sample (Blood sample)", "Entregue uma amostra de reagente (amostra de sangue)", tpl("Blood sample"))],
      name="The TerraGroup Trail - Part 14", after="trail13",
      **s("The reagents that changed those Scavs came from the Lab. Find where they were kept and bring me a sample.",
          "Os reagentes que mudaram aqueles Scavs vieram da Lab. Descubra onde eram guardados e me traga uma amostra.",
          "Now I understand what they made.", "Agora entendo o que eles fizeram.")),
    Q("trail15", E, [
        visit("Obtain the first part of the encoded intel at the missing courier crew on Woods", "Pegue a primeira parte das informações codificadas com a equipe de mensageiros desaparecida na Woods", "Lost_caravan"),
        visit("Obtain the second part of the encoded intel at the van on Woods", "Pegue a segunda parte das informações codificadas na van da Woods", "ter_015_area_1"),
        visit("Transfer the first part of the data from the server room in The Lab", "Transfira a primeira parte dos dados da sala de servidores da Lab", "Server_room"),
        visit("Transfer the second part of the data at the hidden recording studio on Lighthouse", "Transfira a segunda parte dos dados no estúdio de gravação escondido na Lighthouse", "qlight_extension_mechanik1_exploration1")],
      name="The TerraGroup Trail - Part 15", after="trail10", needs=[("trail14", (4,))],
      **s("Everything points to one last data cache. Collect the pieces and send them out before TerraGroup wipes them.",
          "Tudo aponta para um último arquivo de dados. Junte as partes e envie antes que a TerraGroup apague.",
          "We have everything now.", "Agora temos tudo.")),
    Q("trail16", E, [handover("Hand over found in raid Obdolbos cocktail injectors", "Entregue injetores Obdolbos encontrados na raid", tpl("Obdolbos cocktail injector"), 10, fir=True)],
      name="The TerraGroup Trail - Part 16", after="trail15",
      **s("Those injectors are the street version of the TerraGroup reagent. Get them off the streets.",
          "Esses injetores são a versão de rua do reagente da TerraGroup. Tire eles das ruas.",
          "Fewer poisoned Scavs, at least.", "Menos Scavs envenenados, pelo menos.")),
    Q("trail17", E, [stash("Place a WI-FI Camera on the third floor of the military headquarters on Reserve", "Coloque uma WI-FI Camera no terceiro andar do quartel-general militar na Reserve", CAMERA, "Zone_leeter_final")],
      name="The TerraGroup Trail - Part 17", after="trail15",
      **s("TerraGroup's last people are hiding on Reserve. Put a camera where they meet.",
          "O último pessoal da TerraGroup está escondido na Reserve. Coloque uma câmera onde eles se reúnem.",
          "Got them.", "Peguei eles.")),
    Q("trail18", E, [kill("Eliminate Raiders while using Russian weapons", "Elimine Raiders usando armas russas", 25, "Savage", ["pmcBot"], weapons=RUSSIAN_WEAPONS)],
      name="The TerraGroup Trail - Part 18", after="trail15",
      **s("The Raiders are TerraGroup's guards now. Make it look like the army is cleaning house.",
          "Os Raiders são os guardas da TerraGroup agora. Faça parecer que o exército está fazendo uma limpeza.",
          "The army will get the blame. Perfect.", "O exército vai levar a culpa. Perfeito.")),
    Q("trail19", E, [
        kill("Eliminate any targets during night time on Shoreline (in one raid)", "Elimine quaisquer alvos durante a noite na Shoreline (em uma raid)", 7, "Any", map="shoreline", daytime=(22, 5), one_session=True),
        visit("Signal the ship waiting in the bay from the breakwater on Shoreline", "Sinalize para o navio que espera na baía a partir do quebra-mar na Shoreline", "place_peacemaker_001"),
        extract("Survive and extract from the location", "Sobreviva e saia do mapa", "shoreline")],
      name="The TerraGroup Trail - Part 19", after="trail16", needs=[("trail17", (4,))],
      **s("A ship is waiting to take the evidence out of Tarkov. Clear the shore at night and give the signal.",
          "Um navio está esperando para levar as provas para fora de Tarkov. Limpe a costa à noite e dê o sinal.",
          "The trail ends here. TerraGroup will answer for this.", "A trilha termina aqui. A TerraGroup vai responder por isso.")),
)
