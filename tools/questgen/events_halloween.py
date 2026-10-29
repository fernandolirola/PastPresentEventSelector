from lib import *

SKIER, THERAPIST = TRADERS["Skier"], TRADERS["Therapist"]
INFECTED = ["infectedAssault", "infectedCivil", "infectedLaborant", "infectedPmc", "infectedTagilla"]
# items of the 2024 Halloween quests, which still have spawn points in the Lab
DIRTY_BLOOD, VACCINE = "6707cc67cc1667e49e0f7232", "6707cf827d279daad80fa95f"
MED_TOOLS = "619cc01e0a7c3a1a2731940c"


def story(en_desc, pt_desc, en_ok, pt_ok):
    return {"desc": (en_desc, pt_desc), "success": (en_ok, pt_ok)}


# ---------------- Halloween 2026 ----------------
# The live event added Infected Factory and Infected Lab; here the quests use the normal Factory and Lab, where the
# Halloween 2026 part brings the infected. QBZ-191 is not in SPT, so The Best Defense gives an M4A1 unless WTT-ContentBackport is installed.
add(
    Q("hwRumors", "halloween2026", [
        kill("Eliminate 10 infected on Factory", "Elimine 10 infectados na Factory", 10, "Savage", INFECTED, map="factory")],
      name="Halloween: Rumors", trader=SKIER, ids=[],
      rewards=(26000, (EUR, 1800), [], [(tpl("MPS Auto Assault-12 Gen 2 12ga automatic shotgun"), 1), (tpl("AA-12 12ga 20-round drum magazine"), 3),
                                         (tpl("12/70 SuperFormance HP slug ammo pack (25 pcs)"), 4)]),
      **story("Alright mate? Got a job for you. Rumors are spreading all over the region about some new infection, people going crazy from it, and only in certain areas. We've had this kind of thing here before. Go check the factory, then come back and tell me what you've seen. And if you bump into any of those nutters, put 'em down.",
              "Beleza, parceiro? Tenho um serviço pra você. Tão espalhando boato pela região toda sobre uma infecção nova, gente ficando maluca, e só em certas áreas. Já tivemos esse tipo de coisa por aqui. Vai dar uma olhada na fábrica e depois me conta o que viu. E se esbarrar em algum desses malucos, derruba.",
              "You run into any nutters? They come charging at you and all? What a bloody mess... Right, we'll have to think this through.",
              "Encontrou algum maluco? Vieram correndo pra cima de você? Que bagunça... Certo, vamos ter que pensar nisso com calma.")),
    Q("hwBestDefense", "halloween2026", [
        visit("Locate the quarantined section in the Lab", "Encontre a seção em quarentena na Lab", "Halloween_lab_section2"),
        kill("Eliminate 10 infected in the Lab", "Elimine 10 infectados na Lab", 10, "Savage", INFECTED, map="lab")],
      name="Halloween: The Best Defense", trader=SKIER, after="hwRumors", ids=[],
      rewards=(36000, (EUR, 2700), [], [(tpl("Colt M4A1 5.56x45 assault rifle"), 1), (tpl("AR-15 5.56x45 Magpul PMAG 30 GEN M3 STANAG 30-round magazine"), 3),
                                         (tpl("5.56x45mm M855A1 ammo pack (50 pcs)"), 4)]),
      **story("Even the gangs are joining forces to stop this infection and the psychos from the lab and the factory. I had a crew go into the lab just before it all kicked off. They checked in once, then the line went dead. All I got was a shout that the lights had gone out. Get over there and have a look. Might be someone left alive.",
              "Até as gangues tão se juntando pra segurar essa infecção e os psicopatas da lab e da fábrica. Eu tinha uma equipe que entrou na lab logo antes de tudo começar. Deram notícia uma vez e depois a linha caiu. Só ouvi um grito dizendo que as luzes tinham apagado. Vai lá dar uma olhada. Pode ter alguém vivo.",
              "No one? Useless lot. They've gone and lost me a whole load of loot... Ah, well. Here's your cut for the trouble.",
              "Ninguém? Bando de inúteis. Perderam uma carga inteira de loot minha... Fazer o quê. Aqui está a sua parte pelo trabalho.")),
    Q("hwFollowUp", "halloween2026", [
        find("Obtain the infected blood sample in the Lab", "Consiga a amostra de sangue infectado na Lab", DIRTY_BLOOD, fir=False),
        handover("Hand over the infected blood sample", "Entregue a amostra de sangue infectado", DIRTY_BLOOD),
        handover("Hand over Medical tools to open the samples", "Entregue Medical tools para abrir as amostras", MED_TOOLS)],
      name="Halloween: Follow-Up Examination", trader=THERAPIST, after=[("hwBestDefense", (2, 3, 4))], ids=[],
      rewards=(40000, (EUR, 2700), [], [(tpl("TerraGroup Labs access keycard"), 2), (tpl("Adrenaline injector"), 2), (tpl("Trimadol stimulant injector"), 2)]),
      **story("Good afternoon. Yes, I am aware of the infection. Something similar happened not so long ago, and we managed to stop it then. First we need to know whether this is the same pathogen. Bring me a blood sample from someone who died from the virus on their own, so I can be sure it had time to develop. Look for the lab personnel in protective suits. I suspect they were the first to bear the brunt of the outbreak.",
              "Boa tarde. Sim, estou sabendo da infecção. Algo parecido aconteceu não faz muito tempo, e conseguimos conter na época. Primeiro precisamos saber se é o mesmo patógeno. Me traga uma amostra de sangue de alguém que morreu do vírus sozinho, para eu ter certeza de que ele teve tempo de se desenvolver. Procure o pessoal do laboratório com roupas de proteção. Desconfio que foram os primeiros atingidos.",
              "Excellent work. I'll begin the analysis as soon as possible.",
              "Excelente trabalho. Vou começar a análise o quanto antes.")),
    Q("hwUnexpected", "halloween2026", [
        find("Locate and obtain the vaccine shipment in the Lab", "Encontre e pegue o lote de vacina na Lab", VACCINE, fir=False),
        stash("Stash the vaccine shipment at the drop point on Customs", "Esconda o lote de vacina no ponto de entrega na Customs", VACCINE, "Halloween_zone_for_antivirus(bigmap)")],
      name="Halloween: Unexpected Help", trader=SKIER, after="hwFollowUp", ids=[], rivals=[("hwInitial", [4])],
      rewards=(38000, (EUR, 2700), [], [(tpl("Ammunition case"), 1)]),
      **story("While Therapist and her clever lot are still scratching their heads, I've already sorted the problem. Got my hands on a proper vaccine from people I've worked with for years. They reckon it was made during the last outbreak and never made it here. Pick up a batch in the lab and stash it on Customs. Someone's waiting for it there. And don't you lose that shipment.",
              "Enquanto a Therapist e a turma esperta dela ainda tão coçando a cabeça, eu já resolvi o problema. Arrumei uma vacina de verdade com um pessoal com quem trabalho há anos. Dizem que foi feita no último surto e nunca chegou aqui. Pega um lote na lab e esconde na Customs. Tem alguém esperando lá. E não vai perder essa carga.",
              "My lads have already picked up the goods. Lovely stuff! Now the money'll come rolling in. What, you thought I was doing this out of the goodness of my heart?",
              "Meus rapazes já pegaram a mercadoria. Beleza! Agora o dinheiro vai entrar. O quê, achou que eu tava fazendo isso de bom coração?")),
    Q("hwInitial", "halloween2026", [
        find("Locate and obtain the vaccine shipment in the Lab", "Encontre e pegue o lote de vacina na Lab", VACCINE, fir=False),
        handover("Hand over the vaccine shipment", "Entregue o lote de vacina", VACCINE)],
      name="Halloween: Initial Examination", trader=THERAPIST, after=[("hwUnexpected", (2, 3))], ids=[], rivals=[("hwUnexpected", [4])],
      rewards=(38000, (RUB, 200000), [], [(tpl("Documents case"), 1)]),
      **story("Hello. I know Skier asked you to deliver a batch of vaccine. We still haven't identified the strain, and nobody has checked where that preparation came from or how it was stored. If you give it to him, it goes straight to the public. I'm asking you to bring the batch to me instead. I need to examine it before anyone starts using it.",
              "Olá. Sei que o Skier pediu para você entregar um lote de vacina. Ainda não identificamos a cepa, e ninguém verificou de onde veio esse preparado nem como foi guardado. Se você entregar para ele, vai direto para a população. Peço que traga o lote para mim. Preciso examinar antes que alguém comece a usar.",
              "I knew you had a good head on your shoulders. It's a shame there are so few people like you.",
              "Eu sabia que você tinha juízo. Pena que existam tão poucas pessoas assim.")),
)

# WTT-ContentBackport has the real rewards of The Best Defense; they replace the stand-ins when it is installed
SWAPS += [("hwBestDefense", tpl("Colt M4A1 5.56x45 assault rifle"), "69f9ebbcaae020b0db02f65d"),
          ("hwBestDefense", tpl("AR-15 5.56x45 Magpul PMAG 30 GEN M3 STANAG 30-round magazine"), "69f9ec5a7596e93e340c0a1e"),
          ("hwBestDefense", tpl("5.56x45mm M855A1 ammo pack (50 pcs)"), "6a9961d053d704c9d005660e")]
