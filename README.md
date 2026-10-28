# Past & Present Event Selector (PPES)

Pick which events are active on your server, from a page in the server panel.

- **Seasonal events** (Halloween, Christmas, New Year, April Fools) follow their dates on their own. Each one has three modes: **Auto** (on during its dates), **On** (forced, any day) and **Off**.
- **39 past events** from 2021 to 2026, recreated from server data: boss gatherings and relocations, cultist nights, Free Labs, instant hideout building, flea market changes, bonus experience, weapon case airdrops, the mortar strike and more. Event quests that are still in the game data are unlocked with their event.
- Every card links to its section on the community wiki, and the quest button lists the whole quest chain of the event with a link to each quest, marking the ones your game does not have.

Open `https://127.0.0.1:6969/ppes` while the server is running. The page is in English and Portuguese.

## Page features

- **Raid preview:** pick a map and see the bosses, groups, airdrop chance, mortars and keycard rules the next raid would have with your current picks, saved or not.
- **Filters:** search, filter by map, by type (bosses, quests, traders, flea, airdrops, hideout, rules, map access, hazards) and by tag. Cards or a compact list, grouped by year.
- **Impact and conflicts:** each event shows the maps and systems it touches, which events it cannot run with, and which picked events change the same thing.
- **Presets:** Boss hunt, Casual, Hardcore, Quest hunter and Vanilla, plus your own.
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

## Compatibility

- **ABPS (Bot Placement System):** works together. ABPS rebuilds boss spawns after every raid and PPES adds its boss events on top at the start of each raid.
- **SVM:** if SVM's Raids section forces Christmas or Halloween, it overrides the seasonal choice here. The page shows a warning when that happens. SVM boss events stack with PPES boss events.
- Only one main seasonal event drives event quests and items at a time. Turning on Halloween and Christmas together works, but expect odd results.

## Install

Extract the zip into the game folder (the one with `SPT_Runtime`). It only adds `SPT_Runtime/user/mods/PastPresentEventSelector`. There is no client part.

## Português

Escolha quais eventos ficam ativos pela página `https://127.0.0.1:6969/ppes`. Os sazonais seguem as datas sozinhos (Automático), mas podem ser forçados (Ligado) ou desligados. Os 39 eventos antigos foram recriados com os dados do servidor. Eventos de boss e airdrop valem na próxima raid; o resto pede reiniciar o servidor. Cada card tem link para a wiki e as quests liberadas. Nada é gravado nos arquivos do jogo.

## License

MIT
