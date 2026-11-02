# Changelog

## 1.7.3

- **End-of-raid fix:** dying to a PPES faction bot (Cleanup Crew, Smugglers, Pillagers, Gus, Basmach) no longer makes the server reject the raid result (`/client/match/local/end`, `DeathCause.Role`). The faction role names are converted to their ids before the server reads the request.

## 1.7.2

- **Ritual group arrival:** prepare ritual escorts before releasing their leader, then reuse the prepared profiles through native spawning. This avoids a lone priest starting while the remaining five prepare. Initial loading can still delay the group; preparation failure falls back to native spawning.

- **UI fixes:** clean pages follow saves from other tabs; stale drafts cannot overwrite newer settings. Server-side Mechanics and seasonal rotation edits now show restart requirements. Quest lists expand by click or keyboard, mobile spawn tables retain escort controls, invalid new-spawn escorts are rejected, and clearing an escort restores its default without resetting other overrides. Raid previews honor map and Halloween free-Lab access, refresh when that preference changes, and highlight escort-only edits.
- **Bot behavior with zombies:** choose Normal AI (the PPES default) or Hide from zombies (the game's behavior) in Halloween settings. Normal AI suppresses only the Halloween hiding layer and leaves infection, spawning, hostility and the active AI in control. Requires the updated client plugin; changes apply within 30 seconds of saving without a server restart.
- **Sidebar workspace:** Overview, Seasonal events, Past events, Raid setup and Settings replace the long page while retaining the original theme and controls. Unsaved selections stay intact between menus. Seasonal events use a picker and detail panel, Halloween's secondary options can be expanded, and Mechanics, Rotation, Presets and In-game notice have separate settings menus.
- **Card sizing:** panels use their own content height. Mechanics fields form a responsive grid, and the navigation reflows on smaller screens.

## 1.7.1

- **Raider region editing:** removing a region now removes every Raider spawn definition tied to that region, including extraction, lever and patrol-triggered waves, without moving or deleting the waves in regions that remain.
- **Region selectors:** every map now uses a real Add region dropdown with removable region chips, both when editing existing spawns and when adding a new boss or faction. Changing maps refreshes the dropdown with that map's regions.
- **Ground Zero regions:** both Ground Zero variants expose their stock `ZoneSandbox` region even though the regular map leaves `OpenZones` empty in the server database.
- **Clearer raid preview:** internal duplicate spawn definitions no longer appear as misleading `x4` group counts.
- **Planned factions and infected:** the preview includes factions introduced by the current unsaved event selection. Halloween previews show the infected entries and the map's infection percentage, such as Factory's stock 50%, before the server restart.

## 1.7.0

- **Zombie reinforcement waves:** optional Halloween setting continues spawning infected through the raid instead of stopping after the stock finite schedule. Wave size and interval have editable minimum and maximum values; differing values randomize every wave, along with infected type and zone.
- **MoreBotsAPI with ABPS:** PPES now preserves registered custom-faction spawn entries when ABPS replaces the map lists at startup and after every raid. The client also gives ABPS adaptive headroom for the peak number of active MoreBotsAPI custom roles, so initial custom factions do not consume the slots reserved for later Scav and custom waves. It guards MoreBotsAPI 2.1.1's global bot-created callback when another mod creates a bot without a spawn ID, preventing that exception from interrupting the foreign spawn.
- **Built-in event factions:** Cleanup Crew, Smugglers, Pillagers, Gus and Basmach no longer require MoreBotsAPI. PPES now registers only its own bot roles, hostility and optional SAIN support, without a global bot or wave controller.
- **Halloween page:** shows whether zombie spawning is actually on, explains which option enabled it and displays option descriptions and dependencies directly in the card.

## 1.6.1

- **Spawn editor:** clearing a zone field now removes the zone override and restores the map's original zones.
- **Raid preview:** zones can now be removed one at a time without replacing the rest of the list.
- **Reserve zone label:** the preview displays SPT's `ZoneRailStrorage` key as "Rail Storage" while keeping the original key for compatibility.

## 1.6.0

- **Mechanics panel:** exact values for event mechanics, with the game's own as the default and each row tagged with when it applies.
  - **Weapon jams** (needs the new client files, applies from the next raid): how many seconds Kollontay's jam lasts, in every raid since he is a regular Streets boss, and how long Vengeful Zryachiy's lasts. The game's Vengeful Zryachiy jam never ends, so it is set to -1; 0 turns a jam off and his panic stays.
  - **Halloween ritual:** how many times longer extracts take and how many times faster stamina drains.
  - **Zombie infection:** hydration lost per second and the hearing lost.
  - The ritual and infection values apply after a server restart.

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
