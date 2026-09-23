# Live verification ledger

## Interactive substep trackers build (2026-09-23)

- The data-driven current-objective card renders stock-script-backed substeps: Call of the Dead generators, ship/submarine/beacon milestones and lighthouse progress; Shangri-La tile pairs, pressure plate, gas pipes, tunnel holes, dial matches, and correct-gong count; Moon panel hacks and Vril Device placement. Existing Ascension LUNA and pressure-pad tracking, Moon Samantha Says colors, and soul-tube bars remain available.
- Main quest completion still uses the reviewed stock success flag or stage notification. Substep counters do not advance the map flow. No guide step order was changed.
- Release build: `dotnet build EETracker.sln -c Release` succeeded with 0 warnings and 0 errors during development. The updated app, replay fixtures, data, GSC, and sample files were staged together in a local test build.
- Updated Ascension, Call of the Dead, Shangri-La, and Moon observers were installed into Plutonium's map-scoped raw-script folders for development checks. The edited coast, temple, and Moon scripts matched their installed copies by SHA-256 at that time.
- New Call of the Dead, Shangri-La, and Moon substep signals are source-derived from extracted scripts but not live-match verified. T5 compilation, map loading, progress changes, and reset behavior for each new tracker remain to be checked in-game.

## Verified from supplied materials and local files

- Supplied forum HTML describes Any Player EE/Any Player EE SR behavior summarized in [source context](source-context.md).
- Offline-extracted stock Ascension main, map-patch, shared Zombies, and shared Zombies-patch FastFiles using OpenAssetTools Unlinker v0.33.0. T5 detection succeeded and all four extractions completed with zero warnings or errors. Outputs are not part of the public release; hashes and findings are in [the Ascension offline script audit](ascension-offline-script-audit.md).
- Stock Ascension quest source defines six completion flags, mapped in `data/bo1-main-quest-flows.json`. The companion observer reads those flags plus round, player count, map identity, and power. Script loading is confirmed; a quest completion event is still outstanding.
- In Ascension, `wait_for_sync_use` emits `sync_button_pressed` after `UseButtonPressed()` while the player touches a trigger. The signal carries no button identity, can repeat while held, and is not the success event; `switch_watcher` handles evaluation separately.
- Plutonium's T5 changelog documents sandboxed GSC file I/O (`fs_fopen`, `fs_writeline`, and `fs_fclose`) under `scriptdata`, with `scr_allowFileIo` enabled by default unless a server disables it. This establishes a documented transport option, not a verified adapter by itself.
- The first supplied Plutonium startup log showed a `bad syntax` compiler error; changing populated arrays to T5's `array(...)` constructor resolved compilation in the next supplied log.
- Two later logs confirm that `ee_tracker.gsc` compiles and `main()` runs, but no feed file appeared. The observer's `level.script` guard could return before stock map initialization sets that field. The script is map-scoped, so the guard was removed. Session and status creation now run in the custom `init()` entry point, which is separately logged by T5 for other scripts. This was later verified live.
- The companion previously showed `CONNECTED` as soon as the user selected **Connect to Game** because its source setup injected a synthetic session event. That injection has been removed; the UI waits for an actual telemetry record.
- An independent T5 mod author documents the sandboxed data location as `storage\\t5\\raw\\scriptdata`, not `storage\\t5\\scriptdata`. The companion's live path and installer were corrected.
- In a fresh Ascension match, Plutonium logged the observer's `main()` and `init()`, followed by successful session-file opening and initial status writing. The real JSONL feed recorded `session_started`, positive standard `any_player_ee` variant evidence, and valid Ascension snapshots for round 1 with player count moving from 0 to 1 and power off. The companion was restarted with `--connect-game`; the user confirmed its live Ascension mission screen updated.
- During that match, the same feed recorded round changes from 1 to 2 to 3 and a power transition from off to on at round 3. Ongoing map status telemetry is therefore live. A quest completion event is still outstanding.
- The Call of the Dead flow is connected to its offline-confirmed map signals. Its observer reports the `zombie_coast` map code, session state, status snapshots, reviewed quest success flags, and the final `coast_easter_egg_achieved` notification. The reducer selects Stand-In for one player and Ensemble Cast for more than one player, matching the stock map script. Live Call of the Dead loading and quest signals remain unverified.
- Shangri-La and Moon have map-scoped observers. Shangri-La reports reviewed flags and matching stock stage notifications. Moon reports reviewed stage/flag signals, four active soul-tank fill/max counters, stable Samantha Says color displays, the Richtofen-near-Vril-Device dialogue trigger, and the final Big Bang notification. Shangri-La and Moon still need live checks, including GSC compilation, Samantha color capture, and correct mapping/order of the four tank entities.

## Live checks still required

1. Complete the first Ascension quest step in a private match. Confirm the matching stock success flag appears in the file and the objective advances once.
2. Confirm the active Any Player EE profile is classified only when the observer finds the mod hook and an exact known profile; otherwise it must remain Unknown.
3. Reconnect/restart the map and check session reset behavior. Multiplayer sharing to another player's companion remains a separate feature.
4. Load Call of the Dead and confirm its map-scoped observer starts, reports the `zombie_coast` map code, follows player count to the correct quest branch, and records the door/fuse/generator flags.
5. Load Shangri-La and confirm its observer records Eclipse entry, stock stage flags, and final completion against the displayed steps.
6. Load Moon and confirm observer load, Samantha Says displayed colors, step progression, four soul-tank counters against the in-game tanks, Richtofen's dialogue cue, and the final rocket event.

Ascension observer compilation, `main()` and `init()` execution, session-file writes, status updates, and live mission-screen updates are confirmed. Call of the Dead, Shangri-La, and Moon have source-reviewed observers, but their live load and quest signals remain unverified.
