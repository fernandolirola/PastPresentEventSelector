from lib import *

T = TRADERS
PILLAGER, CREW, ROGUE = ["ppesPillager"], ["ppesCleanupCrew"], ["exUsec"]
BLACKDIV = ["blackDivAssault", "bossWedge", "blackDivIb"]
INTEL = "5c12613b86f7743bbe2c3f76"
FLASH = "590c621186f774138d11ea29"
CQCM = {n: tpl(f"Atomic Defense CQCM ballistic mask ({n})") for n in
        ("Smile", "Stop Me", "Scars", "Target", "Skull", "Demon", "El Día de Muertos", "Ping", "Loui Peeton", "Toxic", "Demonic Face")}
PAINTED = sorted(CQCM.values())
JAMMER, CAMERA, ARENA, GREEN = tpl("Signal Jammer"), tpl("WI-FI Camera"), tpl("Armband (ARENA)"), tpl("Armband (Green)")


def s(en_d, pt_d, en_s, pt_s):
    return {"desc": (en_d, pt_d), "success": (en_s, pt_s)}


# ---------------- Casus belli ----------------
E = "casusBelli"
add(
    Q("casusBelli", E, [
        kill("Eliminate pillagers", "Elimine saqueadores", 5, "Savage", PILLAGER),
        handover("Hand over the found in raid painted CQCM masks", "Entregue as máscaras CQCM pintadas encontradas na raid", PAINTED, 3, fir=True)],
      name="Casus belli", level=15, requires="ppesPillager",
      **s("Some gang calling themselves pillagers is robbing my fighters on their way to the Arena. They wear painted masks, like they are on a stage. Take a few of them down and bring me their masks.",
          "Uma gangue que se chama de saqueadores está roubando meus lutadores no caminho para a Arena. Eles usam máscaras pintadas, como se estivessem num palco. Derrube alguns e me traga as máscaras deles.",
          "Cheap paint, real steel. They will think twice now.", "Tinta barata, aço de verdade. Agora eles vão pensar duas vezes.")),
    Q("backupPlan", E, [
        stash("Stash the Grizzly medkit inside the old sawmill hangar on Woods", "Esconda o kit médico Grizzly dentro do galpão da serraria velha na Woods", tpl("Grizzly medical kit"), "em_quest4_1"),
        extract("Transit from Woods to Customs", "Faça o trânsito da Woods para a Customs", "woods", status=("Transit",)),
        kill("Eliminate any targets on Customs", "Elimine quaisquer alvos na Customs", 6, "Any", map="customs"),
        extract("Survive and extract from Customs", "Sobreviva e saia da Customs", "customs")],
      name="Backup Plan", after="casusBelli",
      **s("If the pillagers come for my people again, they need a way out. Leave a medkit at the sawmill on Woods, then go to Customs the back way and clear the road.",
          "Se os saqueadores vierem atrás do meu pessoal de novo, eles precisam de uma saída. Deixe um kit médico na serraria da Woods, depois vá para a Customs pelo caminho de trás e limpe a estrada.",
          "Good. Now my fighters have a way home.", "Ótimo. Agora meus lutadores têm um caminho de volta.")),
    Q("takingMine", E, [
        kill("Eliminate pillagers", "Elimine saqueadores", 10, "Savage", PILLAGER),
        stash("Stash the painted CQCM masks at the sunken village on Shoreline", "Esconda as máscaras CQCM pintadas na vila submersa da Shoreline", PAINTED, "unkown_mark_2", 4)],
      name="Taking What's Mine", after="backupPlan", rivals=["runAground"], requires="ppesPillager",
      **s("Skier thinks he can play both sides and arm the pillagers against me. Hit them hard and leave their masks where his couriers will find them. Do not help Skier with whatever he is planning.",
          "O Skier acha que pode jogar dos dois lados e armar os saqueadores contra mim. Bata forte neles e deixe as máscaras onde os mensageiros dele vão encontrar. Não ajude o Skier no que ele está planejando.",
          "He got the message. Everyone did.", "Ele entendeu o recado. Todo mundo entendeu.")),
    Q("morituri", E, [
        visit("Investigate the kart track on Interchange", "Investigue a pista de kart no Interchange", "event_labyrinth_02_mech_place_02"),
        visit("Investigate the children's playground on Interchange", "Investigue o parquinho no Interchange", "event_labyrinth_02_mech_place_03"),
        kill("Eliminate PMC operatives while wearing a painted CQCM mask (El Día de Muertos, Scars or Target)", "Elimine operadores PMC usando uma máscara CQCM pintada (El Día de Muertos, Scars ou Target)",
             10, "AnyPmc", wear=[CQCM["El Día de Muertos"], CQCM["Scars"], CQCM["Target"]])],
      name="Morituri te salutant", trader=T["Ref"], after="takingMine",
      **s("The new Arena rewards were supposed to be hidden on Interchange. Check the places my contact named, then show the world who those masks belong to now.",
          "As novas recompensas da Arena deviam estar escondidas no Interchange. Confira os lugares que meu contato indicou, depois mostre ao mundo de quem essas máscaras são agora.",
          "Those who are about to die salute you. Well done, gladiator.", "Os que vão morrer te saúdam. Muito bem, gladiador.")),
    Q("shortHands", E, [kill("Eliminate PMC operatives on Woods, Customs, Interchange, Lighthouse or Shoreline", "Elimine operadores PMC na Woods, Customs, Interchange, Lighthouse ou Shoreline",
                             5, "AnyPmc", map=["woods", "customs", "interchange", "lighthouse", "shoreline"])],
      name="Short on Hands", level=15,
      **s("Ref is bleeding me dry with his Arena games and I am short on hands. Thin out the competition around my routes.",
          "O Ref está me sugando com os jogos da Arena dele e estou sem gente. Diminua a concorrência nas minhas rotas.",
          "That will keep them busy for a while.", "Isso vai mantê-los ocupados por um tempo.")),
    Q("runAground", E, [
        stash("Stash 6L18 magazines at the specified spot on Customs", "Esconda carregadores 6L18 no local indicado na Customs", tpl("AK-74 5.45x39 6L18 45-round magazine"), "Sniper_1", 2),
        stash("Stash 5.45x39mm PS gs ammo packs (120 pcs) at the specified spot on Customs", "Esconda caixas de munição 5.45x39mm PS gs (120) no local indicado na Customs", tpls("5.45x39mm PS gs ammo pack (120 pcs)") + ["57372e94245977685648d3e1"], "Sniper_3", 2),
        stash("Stash RGD-5 hand grenades at the specified spot on Customs", "Esconda granadas RGD-5 no local indicado na Customs", tpl("RGD-5 hand grenade"), "Sniper_4", 2)],
      name="Run Aground", after="shortHands", rivals=["takingMine"],
      **s("The pillagers are going to ambush Ref's convoy on Customs and I am the one supplying them. Drop the gear where they will pick it up. And stay out of Ref's way while you do it.",
          "Os saqueadores vão emboscar o comboio do Ref na Customs e sou eu quem está abastecendo eles. Deixe o equipamento onde eles vão pegar. E fique longe do Ref enquanto faz isso.",
          "The trap is set. Ref will not see it coming.", "A armadilha está pronta. O Ref não vai ver chegando.")),
    Q("habet", E, [
        mark("Mark the first stash with new Arena rewards with an MS2000 Marker on Interchange", "Marque o primeiro esconderijo com as novas recompensas da Arena com um MS2000 Marker no Interchange", "place_merch_21_1"),
        mark("Mark the second stash with new Arena rewards with an MS2000 Marker on Interchange", "Marque o segundo esconderijo com as novas recompensas da Arena com um MS2000 Marker no Interchange", "place_merch_21_2"),
        kill("Eliminate PMC operatives while wearing a painted CQCM mask (Demon, Skull or Demonic Face)", "Elimine operadores PMC usando uma máscara CQCM pintada (Demon, Skull ou Demonic Face)",
             10, "AnyPmc", wear=[CQCM["Demon"], CQCM["Skull"], CQCM["Demonic Face"]])],
      name="Habet, hoc habet!", after="runAground",
      **s("Ref's precious Arena rewards are stashed on Interchange. Mark them for my people, then wear one of those masks and make some noise.",
          "As preciosas recompensas da Arena do Ref estão escondidas no Interchange. Marque para o meu pessoal, depois use uma dessas máscaras e faça barulho.",
          "He has been hit, he has really been hit! Ha!", "Ele foi atingido, ele foi atingido mesmo! Ha!")),
)

# ---------------- Tarkov Hospitality ----------------
E = "tarkovHospitality"
REF_MAPS = ["woods", "customs", "shoreline"]
add(
    Q("guardian", E, [
        visit("Locate the temporary camp on Woods where Ref's plans were seen", "Encontre o acampamento temporário na Woods onde os planos do Ref foram vistos", "pr_scout_base"),
        visit("(Optional) Locate the position of Ref's mercenaries in the dorms on Customs", "(Opcional) Encontre a posição dos mercenários do Ref nos dormitórios da Customs", "room214"),
        visit("(Optional) Locate the position of Ref's mercenaries at the smugglers' base on Shoreline", "(Opcional) Encontre a posição dos mercenários do Ref na base dos contrabandistas na Shoreline", "q14_10_point_area"),
        handover("Hand over the found intel", "Entregue as informações encontradas", INTEL)],
      name="Guardian of Nature", level=10,
      **s("Strangers in clean uniforms are setting up camps in my woods. Ref's people, I bet. Find out what they are planning and bring me anything on paper.",
          "Estranhos de uniforme limpo estão montando acampamentos na minha floresta. Gente do Ref, aposto. Descubra o que estão planejando e me traga qualquer coisa no papel.",
          "Fight pits in my forest. Not while I am alive.", "Arenas de luta na minha floresta. Não enquanto eu estiver vivo.")),
    Q("adaptive", E, [kill("Eliminate PMC operatives at Ref's camps on Woods, Customs or Shoreline", "Elimine operadores PMC nos acampamentos do Ref na Woods, Customs ou Shoreline", 5, "AnyPmc", map=REF_MAPS)],
      name="Adaptive Diplomacy", after="guardian", requires="ppesCleanupCrew",
      fail=[kill("Do not kill Ref's cleanup crew", "Não mate a equipe de limpeza do Ref", 1, "Savage", CREW)],
      **s("Jaeger is sniffing around my camps. Show my crew you are on our side: keep the PMCs away from them, and do not touch my people.",
          "O Jaeger está fuçando nos meus acampamentos. Mostre para a minha equipe que você está do nosso lado: mantenha os PMCs longe deles e não toque no meu pessoal.",
          "My boys say you are good company. Keep it that way.", "Meus rapazes dizem que você é boa companhia. Continue assim.")),
    Q("distributed", E, [
        kill("Eliminate members of the cleanup crew at Ref's camp on Woods", "Elimine membros da equipe de limpeza no acampamento do Ref na Woods", 6, "Savage", CREW, map="woods"),
        kill("Eliminate members of the cleanup crew at Ref's camp on Shoreline", "Elimine membros da equipe de limpeza no acampamento do Ref na Shoreline", 6, "Savage", CREW, map="shoreline"),
        kill("Eliminate members of the cleanup crew at Ref's camp on Customs", "Elimine membros da equipe de limpeza no acampamento do Ref na Customs", 6, "Savage", CREW, map="customs")],
      name="Distributed Strike", after="adaptive", requires="ppesCleanupCrew",
      **s("Enough talking. Hit Ref's crews at all three camps at once, so he cannot move men from one to the other.",
          "Chega de conversa. Ataque as equipes do Ref nos três acampamentos, para ele não poder mover gente de um para o outro.",
          "That hurt him. Now we finish it.", "Isso doeu nele. Agora terminamos.")),
    Q("dishCold", E, [
        stash("Plant a Signal jammer in the trash next to the stairs on the 3rd floor of the dorm on Customs", "Plante um Signal jammer no lixo ao lado da escada no 3º andar do dormitório na Customs", JAMMER, "Q019_3"),
        stash("Plant a Signal jammer in the microwave on the 3rd floor of the dorm on Customs", "Plante um Signal jammer no micro-ondas do 3º andar do dormitório na Customs", JAMMER, "place_skier_12_2"),
        mark("Plant a Signal jammer at the first specified place on Shoreline", "Plante um Signal jammer no primeiro local indicado na Shoreline", "place_SIGNAL_03_1", "Signal Jammer"),
        mark("Plant a Signal jammer at the second specified place on Shoreline", "Plante um Signal jammer no segundo local indicado na Shoreline", "place_SIGNAL_03_2", "Signal Jammer"),
        stash("Plant a Signal jammer at the specified location on Woods", "Plante um Signal jammer no local indicado na Woods", JAMMER, "Reb_keeper")],
      name="Dish Served Cold", after="distributed",
      **s("Ref owes me, and he owes Jaeger more. Let us cut his camps off from each other: jammers in the dorms, at the smugglers' base and in the woods.",
          "O Ref me deve, e deve mais ainda ao Jaeger. Vamos isolar os acampamentos dele: bloqueadores nos dormitórios, na base dos contrabandistas e na floresta.",
          "Nobody hears anybody now. Revenge is a dish best served cold.", "Ninguém ouve ninguém agora. A vingança é um prato que se come frio.")),
    Q("centuryFinale", E, [kill(f"Eliminate PMC operatives while wearing an ARENA armband on {m}", f"Elimine operadores PMC usando a braçadeira ARENA na {m}", 2, "AnyPmc", map=k, wear=[ARENA])
                           for k, m in (("woods", "Woods"), ("shoreline", "Shoreline"), ("customs", "Customs"))],
      name="Project of the Century (Finale)", after="dishCold", rivals=["wolfFinale"],
      **s("My project is bigger than Jaeger's grudges. Wear my colors and prove the Arena belongs out here.",
          "Meu projeto é maior que as mágoas do Jaeger. Use as minhas cores e prove que a Arena pertence a este lugar.",
          "The crowd loves you. The project goes on.", "A plateia te ama. O projeto continua.")),
    Q("centuryAfter", E, [kill(f"Eliminate PMC operatives while wearing an ARENA armband on {m}", f"Elimine operadores PMC usando a braçadeira ARENA na {m}", 5, "AnyPmc", map=k, wear=[ARENA])
                          for k, m in (("shoreline", "Shoreline"), ("customs", "Customs"), ("woods", "Woods"))],
      name="Project of the Century (Consequences)", after=[("centuryFinale", (2, 3, 4))],
      **s("Now everyone knows whose side you are on. Keep the armband on and keep winning.",
          "Agora todos sabem de que lado você está. Continue com a braçadeira e continue vencendo.",
          "A true Arena champion.", "Um verdadeiro campeão da Arena.")),
    Q("wolfFinale", E, [kill(f"Eliminate PMC operatives while wearing a Green armband on {m}", f"Elimine operadores PMC usando a braçadeira verde na {m}", 2, "AnyPmc", map=k, wear=[GREEN])
                        for k, m in (("woods", "Woods"), ("shoreline", "Shoreline"), ("customs", "Customs"))],
      name="Wolf's Grip (Finale)", after="dishCold", rivals=["centuryFinale"],
      **s("Put on the green band and show the people of Tarkov that the forest is not a stage.",
          "Coloque a braçadeira verde e mostre ao povo de Tarkov que a floresta não é um palco.",
          "The wolves have spoken.", "Os lobos falaram.")),
    Q("wolfAfter", E, [kill(f"Eliminate PMC operatives while wearing a Green armband on {m}", f"Elimine operadores PMC usando a braçadeira verde na {m}", 5, "AnyPmc", map=k, wear=[GREEN])
                       for k, m in (("customs", "Customs"), ("shoreline", "Shoreline"), ("woods", "Woods"))],
      name="Wolf's Grip (Consequences)", after=[("wolfFinale", (2, 3, 4))],
      **s("Keep the green band on. The pack hunts together.", "Continue com a braçadeira verde. A matilha caça junta.",
          "Good hunting.", "Boa caçada.")),
)

# ---------------- Full Speed Ahead ----------------
E = "fullSpeedAhead"
RADAR = tpl("Radar station spare parts")
add(
    Q("noSmoke", E, [
        extract("Survive and extract from Shoreline", "Sobreviva e saia da Shoreline", "shoreline"),
        extract("Survive and extract from Lighthouse", "Sobreviva e saia da Lighthouse", "lighthouse"),
        extract("Survive and extract from Reserve", "Sobreviva e saia da Reserve", "reserve"),
        kill("Eliminate Rogues on any location", "Elimine Rogues em qualquer mapa", 10, "Savage", ROGUE)],
      name="No Smoke Without Fire", level=15,
      **s("The Rogues are moving equipment along the coast and I want to know where. Walk the routes, Shoreline, Lighthouse and Reserve, and make them nervous.",
          "Os Rogues estão levando equipamento pela costa e quero saber para onde. Percorra as rotas, Shoreline, Lighthouse e Reserve, e deixe eles nervosos.",
          "Where there is smoke, there is fire. Now we know where to look.", "Onde há fumaça, há fogo. Agora sabemos onde procurar.")),
    Q("stealing", E, [
        stash("Stash Radar station spare parts inside the old sawmill hangar on Woods", "Esconda peças da estação de radar dentro do galpão da serraria velha na Woods", RADAR, "em_quest4_1", 2),
        stash("Stash Radar station spare parts in the bunkhouse at the trailer park on Customs", "Esconda peças da estação de radar no alojamento do estacionamento de trailers na Customs", RADAR, "extraction_zone_zibbo", 2),
        stash("Stash Radar station spare parts inside the barber shop on Streets of Tarkov", "Esconda peças da estação de radar dentro da barbearia em Streets of Tarkov", RADAR, "quest_zone_hide_barber1", 2)],
      name="Stealing the Stolen", after="noSmoke",
      **s("The Rogues stole those radar parts from the Lighthouse station. I will hand them to people who can use them. Leave them at the drop points.",
          "Os Rogues roubaram essas peças da estação de radar da Lighthouse. Vou entregar para quem sabe usar. Deixe nos pontos de entrega.",
          "Stolen from thieves. Nobody will complain.", "Roubado de ladrões. Ninguém vai reclamar.")),
    Q("alloy", E, [
        stash("Plant a WI-FI Camera inside the weather station on Shoreline", "Instale uma WI-FI Camera dentro da estação meteorológica na Shoreline", CAMERA, "em_quest4_3"),
        extract("Transit from Shoreline to Lighthouse", "Faça o trânsito da Shoreline para a Lighthouse", "shoreline", status=("Transit",)),
        stash("Plant a WI-FI Camera on the second floor of the yacht club cottage on Lighthouse", "Instale uma WI-FI Camera no segundo andar do chalé do iate clube na Lighthouse", CAMERA, "em_quest5_1")],
      name="Alloy", after="stealing",
      **s("I want eyes on the coast road between Shoreline and Lighthouse. Put cameras on both ends.",
          "Quero olhos na estrada costeira entre a Shoreline e a Lighthouse. Coloque câmeras nas duas pontas.",
          "Now nothing moves on that road without me knowing.", "Agora nada passa naquela estrada sem eu saber.")),
    Q("mayday", E, [
        kill("Eliminate Rogues", "Elimine Rogues", 14, "Savage", ROGUE),
        handover("Hand over the found Rogue intel", "Entregue as informações encontradas com os Rogues", INTEL)],
      name="Mayday", trader=T["Peacekeeper"], after="alloy",
      **s("The cameras caught a Rogue convoy calling for help on the radio. Answer them the way they deserve and bring me whatever intel they carry.",
          "As câmeras pegaram um comboio dos Rogues pedindo ajuda pelo rádio. Responda como eles merecem e me traga as informações que carregam.",
          "Mayday, indeed. Thank you.", "Mayday, de fato. Obrigado.")),
)

# ---------------- Blackout ----------------
E = "blackout"
LAB_ANY = ("Survived", "Runner", "Killed", "Left", "MissingInAction")
add(
    Q("wrench", E, [extract("Visit The Lab", "Visite a Lab", "lab", status=LAB_ANY)],
      name="Wrench and Waterboarding", level=20, requires="blackdiv",
      **s("The Lab went dark and some men in black moved in. Go take a look. I want to know who they are before they get comfortable.",
          "A Lab apagou e uns homens de preto se instalaram. Vá dar uma olhada. Quero saber quem são antes que fiquem à vontade.",
          "Black Division, they call themselves. Lovely.", "Black Division, é como se chamam. Que lindo.")),
    Q("keyDoor", E, [
        visit("Locate the emergency access code in the control room in The Lab", "Encontre o código de acesso de emergência na sala de controle da Lab", "Control_room"),
        visit("Check if the emergency access code works in the server room", "Confira se o código de acesso de emergência funciona na sala de servidores", "Server_room")],
      name="Key to Every Door", after="wrench", requires="blackdiv",
      **s("When the power died, the Lab locked itself. There has to be an emergency code somewhere. Find it and see if it opens anything.",
          "Quando a energia caiu, a Lab se trancou. Tem que haver um código de emergência em algum lugar. Encontre e veja se abre alguma coisa.",
          "One key for every door. Now we are talking.", "Uma chave para todas as portas. Agora sim.")),
    Q("bloodType", E, [
        find("Find Blood samples in raid", "Encontre amostras de sangue na raid", tpl("Blood sample"), 2),
        handover("Hand over the Blood samples", "Entregue as amostras de sangue", tpl("Blood sample"), 2, fir=True),
        visit("Locate Sanitar's workplace in The Lab", "Encontre o local de trabalho do Sanitar na Lab", "peace_027_area")],
      name="My Blood Type Mark is on the Sleeve", after="keyDoor", requires="blackdiv",
      **s("Those soldiers are after the TG samples, I am sure of it. Bring me what you can find, and check Sanitar's room while you are there.",
          "Esses soldados estão atrás das amostras da TG, tenho certeza. Me traga o que encontrar, e confira a sala do Sanitar enquanto estiver lá.",
          "These will not end up in the wrong hands now.", "Essas não vão parar nas mãos erradas agora.")),
    Q("patch", E, [
        visit("Check the security systems in the control room in The Lab", "Verifique os sistemas de segurança na sala de controle da Lab", "Control_room"),
        visit("Check the security systems in the server room in The Lab", "Verifique os sistemas de segurança na sala de servidores da Lab", "Server_room"),
        visit("Check the security systems in the hazard dome in The Lab", "Verifique os sistemas de segurança na cúpula de risco da Lab", "Dome"),
        kill("Eliminate Black Division operatives", "Elimine operadores da Black Division", 10, "Savage", BLACKDIV)],
      name="Patch the Vulnerability", after="bloodType", requires="blackdiv",
      **s("The Black Division walked in through the security systems. Check every node they could use and remove the people using them.",
          "A Black Division entrou pelos sistemas de segurança. Confira cada ponto que eles possam usar e tire de lá quem está usando.",
          "The hole is patched. For now.", "O buraco está tapado. Por enquanto.")),
    Q("cantHave", E, [kill("Eliminate any targets in The Lab", "Elimine quaisquer alvos na Lab", 30, "Any", map="lab")],
      name="If I Can't Have It", after="patch", requires="blackdiv",
      **s("If nobody can have that place, then nobody will. Clear it out.", "Se ninguém pode ter aquele lugar, então ninguém vai ter. Limpe tudo.",
          "Quiet down there at last.", "Finalmente silêncio lá embaixo.")),
)

# ---------------- Down the Rabbit Hole ----------------
E = "downTheRabbitHole"
add(
    Q("rabbit1", E, [
        kill("Eliminate Scavs on Woods", "Elimine Scavs na Woods", 15, "Savage", map="woods"),
        find("Obtain an encrypted drive (Secure Flash drive)", "Consiga um pendrive criptografado (Secure Flash drive)", FLASH),
        handover("Hand over the device", "Entregue o dispositivo", FLASH)],
      name="Down the Rabbit Hole - Part 1", after="debut",
      **s("One of my contacts vanished on Woods with a drive full of secrets. The Scavs there know something. Make them talk and bring me a drive like his.",
          "Um contato meu sumiu na Woods com um pendrive cheio de segredos. Os Scavs de lá sabem de alguma coisa. Faça eles falarem e me traga um pendrive como o dele.",
          "Let us see how deep this goes.", "Vamos ver até onde isso vai.")),
    Q("rabbit2", E, [visit("Locate the area where the documents are stashed on Streets of Tarkov", "Encontre a área onde os documentos estão escondidos em Streets of Tarkov", "quest_zone_keeper99")],
      name="Down the Rabbit Hole - Part 2", after="rabbit1",
      **s("The drive points to a hideout on Streets. Find it.", "O pendrive aponta para um esconderijo em Streets. Encontre.",
          "So that is where he kept them.", "Então era lá que ele guardava.")),
    Q("rabbit3", E, [
        stash("Place a WI-FI Camera at Kaban's office on Streets of Tarkov", "Coloque uma WI-FI Camera no escritório do Kaban em Streets of Tarkov", tpl("WI-FI Camera"), "quest_city_trotil2"),
        handover("Hand over an Intelligence folder with the dealership documents", "Entregue uma pasta de inteligência com os documentos da concessionária", INTEL),
        stash("Stash the documents in the trash next to the stairs on the 3rd floor of the dorm on Customs", "Esconda os documentos no lixo ao lado da escada no 3º andar do dormitório na Customs", INTEL, "Q019_3"),
        flare("Shoot a signal flare at the weather station on Customs", "Dispare um sinalizador na estação meteorológica da Customs", "quest_terminal_flare_zone")],
      name="Down the Rabbit Hole - Part 3", after="rabbit2",
      **s("Kaban is in on this. I want a camera in his office, and copies of his papers left where my people can pick them up. Signal when it is done.",
          "O Kaban está metido nisso. Quero uma câmera no escritório dele, e cópias dos papéis deixadas onde meu pessoal possa pegar. Sinalize quando terminar.",
          "Kaban will regret talking so freely in his office.", "O Kaban vai se arrepender de falar tão à vontade no escritório.")),
    Q("rabbit4", E, [handover("Hand over the BEAR Buddy plush toy", "Entregue o bichinho de pelúcia BEAR Buddy", tpl("BEAR Buddy plush toy"))],
      name="Down the Rabbit Hole - Part 4", trader=T["Prapor"], after="rabbit3",
      **s("My contact's kid had a little BEAR toy. He loved it. Find one; it is the only thing he asked for.",
          "O filho do meu contato tinha um ursinho BEAR. Ele adorava. Encontre um; é a única coisa que ele pediu.",
          "Thank you. That means more than you know.", "Obrigado. Isso significa mais do que você imagina.")),
    Q("rabbit5", E, [
        stash("Stash a Secure Flash drive inside the barber shop on Streets of Tarkov", "Esconda um Secure Flash drive dentro da barbearia em Streets of Tarkov", FLASH, "quest_zone_hide_barber1"),
        stash("Stash a Secure Flash drive on the second floor of the yacht club cottage on Lighthouse", "Esconda um Secure Flash drive no segundo andar do chalé do iate clube na Lighthouse", FLASH, "em_quest5_1"),
        stash("Stash a Secure Flash drive at the specified place on Interchange", "Esconda um Secure Flash drive no local indicado no Interchange", FLASH, "place_merch_020_1"),
        stash("Stash a Secure Flash drive in the bunkhouse at the trailer park on Customs", "Esconda um Secure Flash drive no alojamento do estacionamento de trailers na Customs", FLASH, "extraction_zone_zibbo"),
        kill("Eliminate PMC operatives on Streets of Tarkov", "Elimine operadores PMC em Streets of Tarkov", 7, "AnyPmc", map="streets"),
        kill("Eliminate PMC operatives on Lighthouse", "Elimine operadores PMC na Lighthouse", 7, "AnyPmc", map="lighthouse"),
        kill("Eliminate PMC operatives on Interchange", "Elimine operadores PMC no Interchange", 7, "AnyPmc", map="interchange"),
        kill("Eliminate PMC operatives on Customs", "Elimine operadores PMC na Customs", 7, "AnyPmc", map="customs")],
      name="Down the Rabbit Hole - Part 5", after="rabbit4",
      **s("Time to spread the word. Leave copies of the data across the city, and make sure nobody follows you.",
          "Hora de espalhar a notícia. Deixe cópias dos dados pela cidade, e garanta que ninguém te siga.",
          "Now they cannot bury it.", "Agora não podem enterrar isso.")),
    Q("rabbit6", E, [kill(f"Locate and eliminate {n} in a single raid", f"Encontre e elimine {n} em uma única raid", 1, "Savage", [r], one_session=True)
                     for n, r in (("Reshala", "bossBully"), ("Glukhar", "bossGluhar"), ("Killa", "bossKilla"), ("Shturman", "bossKojaniy"),
                                  ("Sanitar", "bossSanitar"), ("Tagilla", "bossTagilla"), ("Kaban", "bossBoar"))],
      name="Down the Rabbit Hole - Part 6", after="rabbit5",
      **s("Every boss in the region had a hand in this. Every one of them pays.", "Todo chefe da região teve participação nisso. Cada um deles vai pagar.",
          "That is the bottom of the rabbit hole. It is over.", "Esse é o fundo da toca do coelho. Acabou.")),
)
