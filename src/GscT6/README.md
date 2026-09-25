# Black Ops II T6 observers

The five map-scoped observers emit version 1 JSON telemetry tagged with `[EETrackerT6]`. They use stock stage transitions, flags, profile stats, or completion notifications from the locally decompiled T6 scripts. The flow catalog is `data/bo2-main-quest-flows.json`.

| Map | Main quest | Route handling |
| --- | --- | --- |
| TranZit | Tower of Babble | Shared power and sidequest opening; stage 1 selects the route, later stock stages confirm objectives |
| Die Rise | High Maintenance | Shared opening, stock branch stage, stock completion notification |
| Buried | Mined Games | Stock tower flag selects Maxis / Richtofen; the live lobby exposes all three Victis saved-side/NAVcard rows |
| Mob of the Dead | Pop Goes the Weasel | Shared quest; stock showdown winner selects continue / break cycle |
| Origins | Little Lost Girl | Single path; stock step counter and release flag |

## Install and collect

Run `Install-EETrackerT6Observer.ps1` to copy the five observers into their individual Plutonium map script folders. Then select BO2 in EETracker before starting a new match. The app starts the telemetry collector when the BO2 tab is selected and stops it when switching back to BO1 or closing the app. Buried also snapshots all three Victis saved-side and NAVcard stats for each current-lobby player in Diagnostics.

The observer install target follows Plutonium's T6 layout under `%LOCALAPPDATA%\Plutonium\storage\t6\scripts\zm\<map>`. The collector reads tagged lines from `%LOCALAPPDATA%\Plutonium\console.log` and writes `storage\t6\raw\scriptdata\ee-tracker-bo2.jsonl`. For manual collector startup:

```powershell
powershell -ExecutionPolicy Bypass -File .\src\GscT6\Collect-EETrackerT6Telemetry.ps1
```

TranZit stage 1 is a route-start signal only. The Maxis Turbine objective completes at stock stage 2, after both Turbines reach the pylon; Richtofen's pylon objective completes at stock stage 2. The flow also tracks power-on as a shared opening step before the Tower of Babble branch.

The mission view also shows map-scoped quest inventory. All five T6 observers poll current players for quest weapons and tacticals. On TranZit, Die Rise, Buried, and Origins, the observer replaces shared buildable/craftable tracking callbacks to report a carried/dropped part and the carrier's current lobby slot. The current player position is approximate location context, not the loose part's exact spawn location. Mob additionally emits team-confirmed Warden's Key/Icarus stock flags; these do not identify which player carried those parts. Origins possible staff-part areas come from the quest guide. Player slot numbers can change when lobby membership changes. The telemetry feed is local to each client and is not relayed to other players. These callback hooks are source-reviewed and compiled, but require private-match verification per map.

The TranZit observer checks its three Music EE checklist rows when stock `meteor_counter` reaches 3. The Mob observer checks the Rusty Cage rows at the same three-trigger completion, the Where Are We Going rows on the stock `nixie_935` event, and the blue skull hunt rows when `warden_blundergat_obtained` confirms its reward. The Origins observer checks Shepherd of Fire's three radio rows when `found_ee_radio_count` reaches 3. These are source-reviewed signals and still require a private match. Other catalog entries remain manual until a stable stock completion hook is audited.

Run `Uninstall-EETrackerT6Observer.ps1` to remove only the five map observers; it preserves the telemetry file.

## Private-match development shortcuts

The test helper is separate and is **not** installed with the observers. To install it for a private test match:

```powershell
powershell -ExecutionPolicy Bypass -File .\src\GscT6\Install-EETrackerT6DevTools.ps1
```

In the Plutonium game console, issue one of these commands:

```text
ee_dev_points 10000
ee_dev_weapon ray_gun_zm
```

`ee_dev_points` adds the requested amount (capped at 50,000 per command) to each connected player's match score. `ee_dev_weapon` gives and equips the named weapon for each connected player. The helper does not emit quest events or set quest completion flags. Uninstall with `Uninstall-EETrackerT6DevTools.ps1`.

## Evidence boundary

The observer state reads and quest ordering were reviewed against the locally installed/decompiled T6 scripts and guide references in `docs/bo2-quest-planning.md`. This is source-reviewed instrumentation, not private-match validation. Script loading, console-log routing, objective transitions, ending detection, and the dev commands still need confirmation in the user's Steam/Plutonium setup. Some objectives are observable only at stock stage boundaries. The Buried lobby snapshot is not a durable player identity or menu-time reader, and the app does not yet calculate Super Easter Egg readiness or preserve user-owned completion history.