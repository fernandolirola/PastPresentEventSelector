# Past & Present Event Selector (PPES)

Pick which events are active on your server, from a page in the server panel.

- **Seasonal events** (Halloween, Christmas, New Year, April Fools) follow their dates on their own. Each one has three modes: **Auto** (on during its dates), **On** (forced, any day) and **Off**.
- **Halloween parts:** the Zryachiy ritual (2023, with Vengeful Zryachiy), the zombies and infected (2024) and the night of the cult (2025) can be switched on or off on their own. The zombies are off by default, as in the server's stock config. With zombies on, the Lab can keep only the infected, as in stock, and Halloween can open the Lab without a keycard. The **2026 outbreak** part brings the infected to every map with Infected Tagilla, no Scav karma loss and the five Halloween 2026 quests, and the **Halloween loot** part adds pumpkins, signal pistols and acid green flares.
- **Christmas and New Year parts:** Santa on the maps, the hideout tree, the Khorovod tree event (2024) and winter clothes on bots, each on or off.
- **April Fools parts:** winter weather, Santa and the ritual with the cultists, each on or off.
- **44 past events** from 2021 to 2026, recreated from server data: boss gatherings and relocations, cultist nights, Free Labs, instant hideout building, flea market changes, bonus experience, weapon case airdrops, the mortar strike and more. Event quests that are still in the game data are unlocked with their event.
- **New factions** through MoreBotsAPI: the **Cleanup Crew** (Arena Cleanup Crew on Factory, Tarkov Hospitality), the **Smugglers** (Smugglers 2024 and 2026) and the **Pillagers** (Casus belli). Each is its own enemy faction that fights everyone else, with its own name on the kill list and SAIN support.
- **98 recreated event quests** unlock with their events: The Tarkov Mystery (with a Skier or Peacekeeper route), The TerraGroup Trail, Shifting Balance, Smugglers 2024 and 2026, Tarkov Hospitality, Casus belli, Down the Rabbit Hole, Blackout, Arms Race (with the arms dealers Gus and Basmach on Streets), Full Speed Ahead, Letters, Staying Light, Wild Wild Woods and The Bosses gathering. Texts still in the game are used in every language; the rest are in English and Portuguese. Accepted quests stay in your profile after the event ends.
- **Blackout** (2026): Wedge and Black Division in every Lab raid, when the Black Division mod is installed.
- Every card links to its section on the community wiki, and the quest button lists the whole quest chain of the event with a link to each quest, marking the ones your game does not have.

Open `https://127.0.0.1:6969/ppes` while the server is running. The page is in English and Portuguese.

## Page features

- **Raid preview:** pick a map and see the bosses, groups, airdrop chance, mortars and keycard rules the next raid would have with your current picks, saved or not.
- **Spawn editor:** in the raid preview, press Edit to change any boss or group's chance, zones and escort, remove it, or add a boss or faction to the map. Your changes apply after the events, from the next raid.
- **Filters:** search, filter by map, by type (bosses, quests, traders, flea, airdrops, hideout, rules, map access, hazards) and by tag. Cards or a compact list, grouped by year.
- **Impact and conflicts:** each event shows the maps and systems it touches, which events it cannot run with, and which picked events change the same thing.
- **Presets:** Boss hunt, Casual, Hardcore, Quest hunter, New factions and Vanilla, plus your own.
- **Automatic rotation:** draws extra events every hour, day or week, on weekends, or every few raids. Only events that need no restart are drawn.
- **Seasonal rotation:** one random seasonal event per day, week or month instead of the calendar, sometimes none for a break. A new period applies after a server restart.
- **In-game notice:** at login Fence sends a message with rumors about the active events, again only when they change.
- **Countdowns:** seasonal events in Auto mode show how many days until they start or end.

## Event tags

| Tag | Meaning |
|---|---|
| Ready | The server already has everything, PPES only switches it on (mostly event quests). |
| Simulated | Recreated with server data: boss spawns, globals, traders, hideout, flea. |
| Experimental | Old game code switched back on (Mortar Strike). Test it before relying on it. |

## When changes apply

- Boss and airdrop events apply from the **next raid**, no restart needed.
- Seasonal events, quest unlocks and globals, trader, hideout and flea changes need a **server restart**. The page tells you when.

Nothing is written to the game's files. Every change is made in memory, and spawn changes only touch the copy of the map built for each raid.

## Requirements

- **MoreBotsAPI** (2.1.1 or newer), server and client parts. PPES uses it to add its factions.

Optional, and the page marks each event that uses one, green when installed and orange when not:

- **Black Division** for the Blackout event.
- **SCOURGE** for the Scourge Incursion event.

## Compatibility

- **SAIN:** SAIN keeps zombies from hitting (Solarint/SAIN#278); the PPES client plugin fixes that. Without SAIN it does nothing.
- **WTT-ContentBackport:** optional. When installed, The Best Defense gives the QBZ-191, its magazines and DBX95 ammo instead of the M4A1 stand-in.
- **ABPS (Bot Placement System):** works together. ABPS rebuilds boss spawns after every raid and leaves out the seasonal event bosses; PPES adds its boss events and the active seasonal bosses back at the start of each raid.
- **ORBIT:** no conflict. Its Ghost Mode puts bots farther than its sleep distance to sleep, event bosses included, and it moves the Goons itself, so event bosses can look like they vanished until you get closer.
- **SVM:** if SVM's Raids section forces Christmas or Halloween, it overrides the seasonal choice here. The page shows a warning when that happens. SVM boss events stack with PPES boss events.
- Only one main seasonal event drives event quests and items at a time. Turning on Halloween and Christmas together works, but expect odd results.

## Install

Install MoreBotsAPI first, then extract the zip into the game folder (the one with `SPT_Runtime`). It adds `SPT_Runtime/user/mods/PastPresentEventSelector`, a small client prepatcher, `BepInEx/patchers/PastPresentEventSelector`, which tells the game about the new factions, and a small client plugin, `BepInEx/plugins/PastPresentEventSelector`, which lets zombies attack again when SAIN is installed. Every player who joins your server needs the client files too.

## Português

Escolha quais eventos ficam ativos pela página `https://127.0.0.1:6969/ppes`. Os sazonais seguem as datas sozinhos (Automático), mas podem ser forçados (Ligado) ou desligados. Os 44 eventos antigos foram recriados com os dados do servidor, incluindo as facções Cleanup Crew, Smugglers e Pillagers (precisa do MoreBotsAPI), o Blackout com o Wedge (com o mod Black Division) e o Scourge Incursion (com o mod SCOURGE). Black Division e SCOURGE são opcionais: a página marca cada evento que usa um deles, em verde quando está instalado e em laranja quando não. 98 quests de eventos foram recriadas e liberam com o evento, como The Tarkov Mystery, The TerraGroup Trail, Shifting Balance e as dos Smugglers. Na prévia da raid, o botão Editar muda chance, zonas e escolta de chefes e grupos, tira ou adiciona chefes e facções. O Halloween ganhou o Surto de 2026 (infectados em todos os mapas e as 5 quests de 2026) e o loot de Halloween. A rotação agora pode ser por hora, dia ou a cada tantas raids, e os sazonais podem ter rotação própria. Eventos de boss e airdrop valem na próxima raid; o resto pede reiniciar o servidor. Cada card tem link para a wiki e as quests liberadas. Nada é gravado nos arquivos do jogo.

## License

MIT
