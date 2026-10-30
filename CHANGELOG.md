# Changelog

## 1.5.1

- **Blackout with Vultify's Blackout mod:** when that mod is installed, the Blackout event makes every Lab raid go dark too, with its gate-only extracts, keypad codes and arsenal key, together with Wedge and Black Division. Without the event, Vultify's mod keeps its own chance. The page shows a badge for it on the Blackout card.

## 1.5.0

- **Scourge Incursion (Oct 2026):** a new event that puts Scourge and his guards in every Lab raid. It needs the SCOURGE mod, which brings the boss, quests, items and voice; without the event Scourge keeps that mod's own spawn chance.
- **Mod badges on the page:** events that use another mod (MoreBotsAPI, Black Division, SCOURGE) show it, green when installed and orange when not, with a link to the mod on the Forge. Turning one on without its mod shows a warning.
- **Server panel card:** the mod is now listed as "Past & Present Event Selector", so its card title wraps instead of running past the card edge on the server landing page.

## 1.4.1

- **Zombie melee fix works again:** the client plugin from 1.4.0 never loaded on the current game version (the log said "BotMeleeWeaponData has no BotOwner field"), so with SAIN, unarmed and melee zombies still never hit. Now they attack.
- **Updating keeps your settings:** the zip no longer includes config.json, so extracting it over an install no longer resets your picks, spawn edits and presets. A new install still gets the defaults on the first start.

## 1.4.0

- **Editable raid preview:** press Edit to change the chance, zones and escort of any boss or group on a map, remove one, or add a boss or faction (Cleanup Crew, Smugglers, Pillagers, Gus, Basmach, Black Division, infected and more). Your changes apply after the events, from the next raid, and are marked "yours" with an undo per row and per map.
- **Halloween 2026:** a new Halloween part, 2026 outbreak, spreads the infected to every map (Customs and Interchange too), puts Infected Tagilla wherever there are infected, removes the karma loss for killing Scavs as a Scav and adds the five Halloween 2026 quests from Skier and Therapist. The Infected Factory and Infected Lab maps are not in SPT, so the quests use the normal Factory and Lab. With WTT-ContentBackport installed, The Best Defense gives the QBZ-191 as in the live event.
- **Halloween loot:** a new Halloween part puts pumpkins with sweets in drawers, jackets and computers and gives Scavs signal pistols and acid green flares, as in the 2023 to 2025 Halloweens.
- **Wild Wild Woods** now has its extra loot: half again as much loose loot on Woods and a quarter more in containers.
- **More rotation choices:** the automatic rotation can now draw every hour, every day or every few raids, besides every week and on weekends.
- **Seasonal rotation:** one random seasonal event per day, week or month instead of the calendar, with a chance of a break period with none. Events set to Off stay out of the draw. A new period applies after a server restart.
- **Zombie melee fix:** new small client plugin. With SAIN installed, zombies chased but never hit; the plugin lets them attack again. It does nothing without SAIN.

## 1.3.0

- **98 event quests recreated** (141 counting route copies), unlocked with their events: The Tarkov Mystery (23, with the Skier or Peacekeeper route), The TerraGroup Trail (19), Shifting Balance (10), Smugglers 2024 and 2026 (8), Tarkov Hospitality (8), Casus belli (7), Down the Rabbit Hole (6), Blackout (5), Arms Race (4), Full Speed Ahead (4), Letters, Staying Light, Wild Wild Woods and The Bosses gathering.
- Quests whose texts are still in the game use them in every language, including 106 original objective texts. The others have new texts in English and Portuguese.
- Objectives use only zones and items that exist in the game. Where an original spot or item is gone, a nearby zone or an existing item takes its place, and the objective text says what to do.
- New bosses for Arms Race: the arms dealers Gus and Basmach, with Kolontay's guards, on Streets.
- Smugglers always carry a backpack, often with locked cases, case keys and contraband. Opened cases can hold Skier's contraband. Smugglers (2026) airdrops carry sealed weapon cases.
- Pillagers often wear painted CQCM masks. Encrypted flash drives turn up in drawers, jackets and PCs during The Tarkov Mystery.
- Quests that need a faction or mod (MoreBotsAPI factions, Black Division) are only added when it is there. Quests accepted during an event stay in your profile after the event ends.
- The client prepatcher was updated for Gus and Basmach; every player needs the new one.

## 1.2.0

- New factions through MoreBotsAPI, each an enemy faction of its own with its name on the kill list and SAIN support: the Cleanup Crew, the Smugglers and the Pillagers.
- Arena Cleanup Crew on Factory now has two Cleanup Crew groups in place of the regular Scavs.
- Tarkov Hospitality now has Ref's Cleanup Crew at the Customs dorms, the Shoreline smugglers' base and the Woods village.
- New events: Smugglers (2024), Smugglers (2026, with every Scav boss on Reserve), Casus belli (Pillagers) and Blackout (Wedge and Black Division on the Lab, needs the Black Division mod).
- Events that need a mod you do not have are marked on the page and left out of the rotation.
- New "New factions" preset.
- PPES now needs MoreBotsAPI and ships a small client prepatcher.

## 1.1.0

- Halloween parts: switch the Zryachiy ritual (2023, with Vengeful Zryachiy), the zombies and infected (2024) and the night of the cult (2025) on or off on their own.
- Halloween also gets two more options: Lab with infected only while zombies are on (like stock), and Lab without keycard.
- Christmas and New Year parts: Santa on the maps, the hideout tree, the Khorovod tree event (2024) and winter clothes on bots, each on or off.
- April Fools parts: winter weather, Santa and the ritual with the cultists, each on or off. The April Fools description no longer mentions zombies, which the server never turns on for it.
- Seasonal event bosses now spawn with ABPS. ABPS rebuilds the boss list without them, so PPES puts the active ones back at the start of each raid, without duplicates.
- Santa now spawns at Christmas, New Year and April Fools with ABPS, using the server's own Santa maps, zones and chances (ABPS ships him at 0%).
- The raid preview now shows the seasonal event bosses and Santa, and names the ritual and special cultist bosses.
- Compatibility note for ORBIT Ghost Mode, which puts far bots (event bosses included) to sleep.

## 1.0.0

- Seasonal event selector with Auto, On and Off modes for Halloween, Christmas, New Year and April Fools.
- 39 past events (2021 to 2026): boss gatherings and relocations, cultists, Bloodhounds, Rogues, Free Labs, Softcore, instant hideout, unlocked traders, flea market changes, boosted skills, bonus experience, shorter Scav cooldown, heatwave, airdrop traffic, weapon case airdrops, Mortar Strike and event quest unlocks.
- Wiki link on every event and a quest button listing the event's whole quest chain with wiki links, marking quests missing from your game.
- Server panel page in English and Portuguese, with restart hints and compatibility notes for ABPS and SVM.
- Raid preview per map, search and filters by map and type, list view, timeline by year, impact chips and conflict notes.
- Presets (built-in and custom), weekly or weekend automatic rotation, Fence message with event rumors at login, seasonal countdowns.
