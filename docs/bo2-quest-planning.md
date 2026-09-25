# Black Ops II quest tracking plan

## Research boundary

This document records the BO2 research, observer implementation, and remaining validation work. All five BO2 main quests now have flow catalog entries and map-scoped Plutonium T6 observers. The route observers use stock quest state to select the observed faction or ending. Observer loading and console telemetry still need a private-match check. The guides below are walkthrough references; stock runtime signals are reviewed separately against local decompiled scripts.

## Map and quest catalog

| Map | Main quest | Routes / ending | Super Easter Egg role |
| --- | --- | --- | --- |
| TranZit (Green Run) | Tower of Babble | Mutually exclusive Maxis or Richtofen path | Victis trilogy completion and its first per-map side record |
| Die Rise | High Maintenance | Shared opening, Maxis or Richtofen branch, shared ending | Victis trilogy completion and its second per-map side record |
| Buried | Mined Games | Shared opening, Maxis or Richtofen branch, shared ending | Victis trilogy completion and its third per-map side record; Endgame activation occurs here |
| Mob of the Dead | Pop Goes the Weasel | One quest with two final outcomes: continue or break the cycle | Separate from the Victis Maxis/Richtofen Super Easter Egg matrix |
| Origins | Little Lost Girl | One main quest; no Maxis/Richtofen branch | Separate quest, not a Victis Super Easter Egg row |

Do not interpret “two paths in BO2” as a global choice for every map. The faction split is specific to the Victis quests on TranZit, Die Rise, and Buried. Mob of the Dead has its own player-dependent ending; Origins has one main quest.

## Proposed tracker model

Keep the active match walkthrough and persistent completion ledger as separate state:

1. **Active quest flow:** Map-scoped ordered nodes with shared nodes referenced by both route sequences. Each Victis quest has explicit `maxis` and `richtofen` paths. A route choice is unset until the team reaches and makes that choice; do not infer the selected route from player profile history.
2. **Per-player profile records:** For each local player profile, retain the game's currently saved side independently for TranZit, Die Rise, and Buried. The retail scripts use one `*_last_completed` value per map/profile (`1` Richtofen, `2` Maxis); completing the other side overwrites that map's saved side. Suggested current value: `unknown`, `not_completed`, `maxis`, or `richtofen`, with provenance and last-confirmed time. If the app also records a user's lifetime history of both routes, store that as a separate local event history; do not mistake it for the game's current Super Easter Egg state.
3. **NAVcard state:** Track the directed links separately from quest sides: Buried card inserted in TranZit, TranZit card inserted in Die Rise, and Die Rise card inserted in Buried. Retail script registration confirms each map accepts its own card plus the preceding-map card in this loop. Record `unknown`, `not_inserted`, or `inserted`; allow manual confirmation until a verified signal source exists.
4. **Readiness projection:** For each player show the three currently saved side records and the current lobby's NAVcard links. The decompiled Buried Endgame code evaluates the whole machine: it needs all 12 side cells lit with no mixture of blue (Richtofen) and orange (Maxis), plus the three NAVcard indicators. Show Maxis- or Richtofen-aligned readiness only when the observed/manual roster state supports it; keep unknown and mixed states explicit. Final Endgame activation is a separate event.
5. **Lobby readiness:** Render the Buried courthouse state as four player rows by three map columns, matching the retail loop (`player` outer iteration, then map index 0=TranZit, 1=Die Rise, 2=Buried). Each cell can be `unknown`, `incomplete`, `Maxis`, or `Richtofen`; card indicators are shown separately. Keep lobby roster identity explicit; do not assign persistent records to player slots across sessions without evidence.

Profile data should be user-editable/manual at first unless the game provides a verified account-progress source. The BO2 scripts read and write these fields through the engine's `PlayerStatsList` stat API (`getdstat`/`setdstat`), so the game carries them across maps for each player profile. This extraction does not show a plain local file or supported external API that EETracker can read at the menu, nor a durable identity mapping between game profiles and tracker profiles. Keep the app's own event history separate from the game's mutable saved state.

## Data shape proposal

Preserve the BO1 flow model and add a game catalog rather than putting BO2 into the BO1 file. BO1 already uses stable node IDs, path references, requirements, and one flow per map. A BO2 flow can follow the same pattern:

```json
{
  "schemaVersion": 1,
  "gameId": "bo2",
  "maps": [{
    "mapId": "die_rise",
    "quests": [{
      "questId": "high_maintenance",
      "kind": "main",
      "nodes": [],
      "paths": {
        "maxis": ["shared.opening", "maxis.branch", "shared.ending"],
        "richtofen": ["shared.opening", "richtofen.branch", "shared.ending"]
      }
    }]
  }]
}
```

Do not copy shared node definitions into each route. Stable IDs should include game, map, quest, and step. Side quest catalogs (music, buildables, etc.) should be separate quests with their own scope, not mixed into the main route.

Persistent account/profile and NAVcard records belong in a versioned user-state store, not in the researched flow catalog. Keep manual confirmations labeled as manual and avoid presenting them as game-observed facts.

## Suggested implementation sequence

1. Private-match validate script loading, console-log capture, shared and route transitions, objective completion, and Mob ending detection on the user's Steam/Plutonium setup.
2. Keep the active match progression separate from the Victis player/profile ledger; add manual state with evidence labels.
3. Implement a read-only profile ledger and Super EE readiness projection with manual confirmations. Validate mixed-side, unknown, missing-card, and player-roster cases.
4. Refine intermediate counters only where source review and private-match observation confirm stable stock state.

## Implemented observer scope

- TranZit tracks the shared Tower of Babble opening, then branches to Richtofen's power-on opening or Maxis's power-off/two-Turbine first objective. Richtofen stage 1 selects Richtofen; Maxis route selection waits for the first Turbine placed at the pylon because Maxis stage 1 starts automatically when power is off. Maxis stage 2 confirms both Turbines at the pylon. It also snapshots the per-player profile counters.
- Die Rise tracks the four elevator symbols, ordered floor-symbol puzzle, sniper-ball stage, route branch, and final pylon completion. Intermediate faction objectives are marked complete when the stock tower-route flag confirms them.
- Buried shows the shared quest-start state before the first tower is built, then detects the Maxis/Richtofen tower, follows the route-specific amplifier objective, and tracks the shared lantern, wisp/time-bomb, switchboard, and sharpshooter stages.
- Mob of the Dead tracks the generator/plane setup, three bridge round trips, skull/Blundergat and spoon prerequisites, final-flight activation, and the winning side of the stock showdown. The Nixie/audio sequence is described in the active objective and recognized as complete when final-flight activation is observed.
- Origins tracks four-staff completion, stock Little Lost Girl step-counter transitions, and Samantha's release flag.

These are compiled, source-reviewed observers but are not marked live-verified. While Buried is running, the observer also reads the stock TranZit, Die Rise, and Buried saved-side/NAVcard values for every current-lobby player and displays those rows in Diagnostics. This is a lobby snapshot, not durable identity or menu-time access; a dedicated readiness projection and user-owned history remain future work.

## BO2 offline script audit (2026-09-24)

The user's local BO2 install is at `C:\Games\Call of Duty Black Ops 2`. OpenAssetTools Unlinker v0.33.0 recognized its FastFiles as T6. The extraction included shared Zombies (`common_zm.ff`, `patch_zm.ff`) plus base and patch files for TranZit (`zm_transit`), Die Rise (`zm_highrise`), Buried (`zm_buried`), Mob of the Dead (`zm_prison`), and Origins (`zm_tomb`). Each listed FastFile loaded and extracted with zero warnings and errors. The T6 map scripts were compiled binary GSC objects, so GSC Tool v1.5.1.359 was used to decompile the quest-related scripts. Selected scripts from the five maps decompiled successfully. Raw and decompiled outputs remain in ignored `work/bo2-extract/` and `work/bo2-decompiled/`; no game script was copied into tracked source.

### Super Easter Egg state confirmed in the scripts

- TranZit, Die Rise, and Buried each initialize per-player `sq_<map>_last_completed` stats. The completion handlers write `1` for Richtofen or `2` for Maxis. At quest initialization the scripts read that single value for each player and map, confirming the saved side is a mutable last-completed value rather than two simultaneously retained side bits.
- Buried's `sq_metagame()` builds a four-player by three-map grid. The player loop is outermost; map order is TranZit, Die Rise, Buried. Blue cells represent Richtofen and orange cells Maxis. Its activation check requires 12 illuminated side cells, refuses a mixture of both colors, and separately requires the three NAVcard-applied indicators.
- Each map's quest script registers the incoming NAVcard relationship: Buried's card can be inserted in TranZit, TranZit's in Die Rise, and Die Rise's in Buried. These links are separate from the side completion values.
- After the Endgame trigger is used, Buried resets all players' three `*_last_completed` stats and three `navcard_applied_*` stats, clears the machine lights, and then starts the selected faction's Endgame reward. The app should snapshot readiness and record Endgame activation before reflecting the reset state.
- The quest completion events exposed internally by the scripts include `transit_sidequest_achieved`, `highrise_sidequest_achieved`, and `buried_sidequest_achieved`; the faction-specific completion stat is a stronger route result than generic stage cues. These are candidate evidence signals only; no live telemetry or external profile identity has been verified.
- The shared `_zm_stats.gsc` wrapper reads and writes `PlayerStatsList` using engine `getdstat`/`setdstat`. This confirms cross-map profile persistence as used by the game scripts, but the current install inspection found no plain BO2 profile-stat file and does not prove an external reader can access these values outside a running game.

### FastFile input hashes

| FastFile | SHA-256 |
| --- | --- |
| `common_zm.ff` | `B0663E765B50A17E3E7A53E2675CA18A1EECA186E0356DF74E9B2C615F09CE9E` |
| `patch_zm.ff` | `F5A27345A61DE5FABD735F45F05B127E95AD9A488619BD7FBC96AC14C5AB4D47` |
| `zm_transit.ff` | `A77B22580D701C7206134003FC89468028E8F7F3AE520D5AA53B437AA2A7C38D` |
| `zm_transit_patch.ff` | `71668A58DD0AA278F6CF47A5A579588C92279B6D98E7D61DBC345C0DDE1F4D5B` |
| `zm_highrise.ff` | `F054324D76D164CD73070F6FBE628DE41DD018A81BCF874B78726409577FC01D` |
| `zm_highrise_patch.ff` | `30A5A6C8813DBC04F368CFC908FF8B4514EFE2BCF73F87CA6517E9B84229079E` |
| `zm_buried.ff` | `3DAB5FEC484415B9D72098A581FA9CF59A5407612DC21B8BD89CC32BFD634E81` |
| `zm_buried_patch.ff` | `C27F6666DBC61EB800E3350DA60E63DB163FBAF72D7F9C1D3EFED46D2D6EBD13` |
| `zm_prison.ff` | `EBFD53156542BC61E842087879475C7515851DDCC6D4F0AE743099590177DCB1` |
| `zm_prison_patch.ff` | `9406051D93926944167AF3824DDB9BA2F11C6B40B16DF9D752DCB832B48BA109` |
| `zm_tomb.ff` | `663C2F6E275FB6A8A2844BCC0E31808ABE681CE7C2A2DDC6F616EF98FC7CD0B5` |
| `zm_tomb_patch.ff` | `770D8A2C3558C9E4C981D8D6DB6BD67698111F01E8BE32423C66B71719B4C2CA` |

This is offline script evidence from the installed files, not proof that a companion observer can read those runtime values or identify players in a live BO2 lobby. Decompiler output is a readable reconstruction of bytecode and should be checked against surrounding control flow before treating a detail as exact source intent.

## Research notes and sources

- [TranZit main quest guide](https://www.codzombiesguides.com/main-quests/black-ops-2/tranzit/) describes Tower of Babble's Maxis and Richtofen requirements and route-specific steps.
- [Die Rise main quest guide](https://www.codzombiesguides.com/main-quests/black-ops-2/die-rise/) describes High Maintenance's shared opening, mid-quest route choice, separate branches, and shared final step.
- [Buried main quest guide](https://www.codzombiesguides.com/main-quests/black-ops-2/buried/) describes Mined Games' two routes and common ending.
- [Buried Endgame guide](https://www.callofdutyzombies.com/easteregg/guides/call_of_duty_black_ops_ii_zombies/451_main-easter-eggs/buried_mined_games/endgame/unlimited-power-r211/) describes the 3-by-4 courthouse grid, map-by-map side colors, and all-three-maps same-side condition.
- [Mob of the Dead guide](https://www.codzombiesguides.com/main-quests/black-ops-2/mob-of-the-dead/) describes Pop Goes the Weasel's separate cycle outcome and multiplayer ending.
- [Origins main quest guide](https://www.codzombiesguides.com/main-quests/black-ops-2/origins/) describes Little Lost Girl as a separate quest.
- [Tower of Babble overview](https://www.codzombie.com/quests/tranzit-tower-of-babble) also describes the choice as map-specific and mutually exclusive within the quest.

Guide-derived details can conflict and should be rechecked during the per-map source audit. In particular, claims about profile overwrites when a player joins an in-progress session, Endgame resets, minimum player counts, and exact NAVcard/profile persistence should be verified on the user's target game/runtime before the ledger treats them as hard rules.
