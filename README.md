# Past & Present Event Selector (PPES)

Pick which events are active on your server, from a page in the server panel.

- **Seasonal events** (Halloween, Christmas, New Year, April Fools) follow their dates on their own. Each one has three modes: **Auto** (on during its dates), **On** (forced, any day) and **Off**.
- **Halloween parts:** the Zryachiy ritual (2023, with Vengeful Zryachiy), the zombies and infected (2024) and the night of the cult (2025) can be switched on or off on their own. The zombies are off by default, as in the server's stock config. With zombies on, the Lab can keep only the infected, as in stock, and Halloween can open the Lab without a keycard.
- **Christmas and New Year parts:** Santa on the maps, the hideout tree, the Khorovod tree event (2024) and winter clothes on bots, each on or off.
- **April Fools parts:** winter weather, Santa and the ritual with the cultists, each on or off.
- **43 past events** from 2021 to 2026, recreated from server data: boss gatherings and relocations, cultist nights, Free Labs, instant hideout building, flea market changes, bonus experience, weapon case airdrops, the mortar strike and more. Event quests that are still in the game data are unlocked with their event.
- **New factions** through MoreBotsAPI: the **Cleanup Crew** (Arena Cleanup Crew on Factory, Tarkov Hospitality), the **Smugglers** (Smugglers 2024 and 2026) and the **Pillagers** (Casus belli). Each is its own enemy faction that fights everyone else, with its own name on the kill list and SAIN support.
- **98 recreated event quests** unlock with their events: The Tarkov Mystery (with a Skier or Peacekeeper route), The TerraGroup Trail, Shifting Balance, Smugglers 2024 and 2026, Tarkov Hospitality, Casus belli, Down the Rabbit Hole, Blackout, Arms Race (with the arms dealers Gus and Basmach on Streets), Full Speed Ahead, Letters, Staying Light, Wild Wild Woods and The Bosses gathering. Texts still in the game are used in every language; the rest are in English and Portuguese. Accepted quests stay in your profile after the event ends.
- **Blackout** (2026): Wedge and Black Division in every Lab raid, when the Black Division mod is installed.
- Every card links to its section on the community wiki, and the quest button lists the whole quest chain of the event with a link to each quest, marking the ones your game does not have.

Open `https://127.0.0.1:6969/ppes` while the server is running. The page is in English and Portuguese.

## Page features

- **Raid preview:** pick a map and see the bosses, groups, airdrop chance, mortars and keycard rules the next raid would have with your current picks, saved or not.
- **Filters:** search, filter by map, by type (bosses, quests, traders, flea, airdrops, hideout, rules, map access, hazards) and by tag. Cards or a compact list, grouped by year.
- **Impact and conflicts:** each event shows the maps and systems it touches, which events it cannot run with, and which picked events change the same thing.
- **Presets:** Boss hunt, Casual, Hardcore, Quest hunter, New factions and Vanilla, plus your own.
- **Automatic rotation:** draws one to three extra events every week or every weekend, like the live events did. Only events that need no restart are drawn.
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
- **Black Division** is optional. Only the Blackout event needs it; without it the page marks the event and it does nothing.

## Compatibility

- **ABPS (Bot Placement System):** works together. ABPS rebuilds boss spawns after every raid and leaves out the seasonal event bosses; PPES adds its boss events and the active seasonal bosses back at the start of each raid.
- **ORBIT:** no conflict. Its Ghost Mode puts bots farther than its sleep distance to sleep, event bosses included, and it moves the Goons itself, so event bosses can look like they vanished until you get closer.
- **SVM:** if SVM's Raids section forces Christmas or Halloween, it overrides the seasonal choice here. The page shows a warning when that happens. SVM boss events stack with PPES boss events.
- Only one main seasonal event drives event quests and items at a time. Turning on Halloween and Christmas together works, but expect odd results.

## Install

Install MoreBotsAPI first, then extract the zip into the game folder (the one with `SPT_Runtime`). It adds `SPT_Runtime/user/mods/PastPresentEventSelector` and a small client prepatcher, `BepInEx/patchers/PastPresentEventSelector`, which tells the game about the new factions. Every player who joins your server needs the prepatcher too.

## Português

Escolha quais eventos ficam ativos pela página `https://127.0.0.1:6969/ppes`. Os sazonais seguem as datas sozinhos (Automático), mas podem ser forçados (Ligado) ou desligados. Os 43 eventos antigos foram recriados com os dados do servidor, incluindo as facções Cleanup Crew, Smugglers e Pillagers (precisa do MoreBotsAPI) e o Blackout com o Wedge (precisa do mod Black Division). 98 quests de eventos foram recriadas e liberam com o evento, como The Tarkov Mystery, The TerraGroup Trail, Shifting Balance e as dos Smugglers. Eventos de boss e airdrop valem na próxima raid; o resto pede reiniciar o servidor. Cada card tem link para a wiki e as quests liberadas. Nada é gravado nos arquivos do jogo.

## License

MIT
