# Ascension offline GSC audit

## Result

OpenAssetTools Unlinker v0.33.0 successfully recognized the installed `zombie_cosmodrome.ff` as T5 and dumped readable GSC rawfiles without launching the game or injecting into its process. Ascension's main FastFile contains `maps/zombie_cosmodrome_eggs.gsc`; the map entry script `maps/zombie_cosmodrome.gsc` starts it. The map patch does not replace the quest GSC in this install.

The extraction also dumped the shared Zombies script files. All four Unlinker runs completed with zero warnings and zero errors:

| Installed input FastFile | SHA-256 |
| --- | --- |
| `zone/common/zombie_cosmodrome.ff` | `2680EB4A44CED7D8AEA7F85ACD12C888C42E337C4711ADC050870087DA795F04` |
| `zone/common/zombie_cosmodrome_patch.ff` | `0085AC3F2F381734451A0A6899550770D3E9615B9ECC98B0CD570AB3C474B37C` |
| `zone/common/common_zombie.ff` | `F812069EF2FF446B97051E75D6A5EE49687A8085218B8F79DC2D8237E543117E` |
| `zone/common/common_zombie_patch.ff` | `3311182F5CB9AF07D69D628B8223AB43E47992E4951B46E761D4F0152F33787E` |

OAT and extracted files are kept under the ignored `work/` folder. The stock game code is not copied into tracked EETracker source. The main quest extraction is at `work/ascension-extract/dump-main/maps/zombie_cosmodrome_eggs.gsc`; the full output directories are named `dump-main`, `dump-map-patch`, `dump-shared`, and `dump-shared-patch`.

## Stock quest state flow

The stock quest script's header describes six ordered nodes. Each has a named completion flag, now represented once in `data/bo1-main-quest-flows.json`:

| Order | Step | Success flag | Source behavior |
| ---: | --- | --- | --- |
| 1 | Teleport the transformer with a Gersh Device at the sparking target outside the base entrance | `target_teleported` | The Gersh Device must hit the target trigger; the transformer moves to the Storage lander area. |
| 2 | Activate the Casimir monitor terminal in Storage after restoring map power | `rerouted_power` | A player uses the terminal trigger; the monitor changes to its logo state. |
| 3 | Synchronize the four switches during a Space Monkey round | `switches_synced` | Vanilla checks four pressed switch structs within a 500 ms window. Each Use hold emits `sync_button_pressed`, which is only an interaction signal. |
| 4 | Hold the pressure area at the rocket pad continuously for 120 seconds | `pressure_sustained` | The script checks every current player; if anyone leaves, the timer resets. |
| 5 | Collect the passkey letters L-U-N-A in order | `passkey_confirmed` | The stock lander route is Centrifuge→Storage (L), Storage→Centrifuge (U), Centrifuge→Catwalk (N), Catwalk→Storage (A). Missed or out-of-order progress resets. |
| 6 | Complete the marked focal-point weapon combination | `weapons_combined` | A Gersh Device starts the check; the upgraded Ray Gun, upgraded Thundergun, and Matryoshka Dolls are checked before Gersh is released. |

The four-player monkey-round condition is 500 ms in stock. The 120-second pressure check is present in the base quest code and is not modified by the inspected Any Player EE script. Power is a map-state flag named `power_on` in the map/shared scripts; it is not a seventh main-quest flag.

The quest `init()` starts its six event handlers concurrently. The source comments present the intended player order, but the handlers do not all wait for the previous quest flag; some gating comes from map access and objective availability. In particular, the terminal handler starts at map init and does not itself wait on `power_on`, despite its comment describing the terminal after power is restored. Therefore the tracker can show an ordered walkthrough while treating each named success flag as its own evidence and avoiding the assumption that every later handler is literally disabled until a previous flag is set.

## Comparison with installed Any Player EE v2.3.1

The mod's `maps/zombie_cosmodrome_eggs.gsc` preserves all six stock completion flags. A source diff shows added behavior for:

- monkey switch thresholds and timeout via `any_player_ee_cosmodrome_buttons` and `_timeout`;
- solo lander-letter pickup via `any_player_ee_cosmodrome_lander_1p`;
- the solo Matryoshka Doll check via `any_player_ee_cosmodrome_combo_doll_1p`;
- a mod-loaded player message.

It does not change the identity of the success flags. The start message and DVAR values are not sufficient by themselves to determine whether the standard or SR settings are active; preserve `Unknown` until the effective configuration is positively established.

## Validation boundary

This proves the local FastFiles can be read offline and gives source-grounded quest signals. It does not prove Plutonium loads the mod in a live match, that a separately loaded observer can see these flags, or that the observer can write a companion-readable event stream. Those remain live verification items in `live-verification.md`.
