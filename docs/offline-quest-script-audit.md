# BO1 main quest offline script audit

## Scope and result

OpenAssetTools Unlinker v0.33.0 read the installed Call of the Dead (`zombie_coast`), Shangri-La (`zombie_temple`), and Moon (`zombie_moon`) T5 FastFiles offline. The map and patch FastFiles contain readable GSC scripts. The quest walk-through text in `data/bo1-main-quest-flows.json` remains the player-facing description; this audit records where the retail scripts corroborate it and which internal state signals they expose.

The scripts and full extraction output stay in ignored `work/` directories. No stock or third-party script was copied into the tracked source tree or modified in place. The installed Any Player EE v2.3.1 source was inspected as a separate external source. It does not include Call of the Dead scripts.

This pass covers the three main quests only. Optional side easter eggs such as hidden songs, radios, and ambient interactions are intentionally deferred to a later version. Their scripts may appear in the offline extraction, but they are not part of these main quest paths.

## Local inputs

| Map | Base FastFile SHA-256 | Patch FastFile SHA-256 | Main quest GSC |
| --- | --- | --- | --- |
| Call of the Dead (`zombie_coast`) | `318BD8D03FD4F660E75B35C6A26F0107547973BA38EF549A52D75A4F74AEF8E3` | `D0A3F755034DD750A9DA5ABBB73D3B68F2630AC51DAAD77E69D34738998E679C` | `maps/zombie_coast_eggs.gsc` |
| Shangri-La (`zombie_temple`) | `FA7FDCD37FDE992CE8506E7C7C35B3184792EDDDB7AEF83952FA48625C5EE4B4` | `F21C08D870230ADFDFD1CD39DAB87E8BBDEBE645CBCAFDBD7BB87DA52021FA4` | `maps/zombie_temple_sq.gsc` |
| Moon (`zombie_moon`) | `05917F5E2FFCF2DCF2F9FC420DF7C12C736CB9E2BB8413480E095E2DF0815A3F` | `D85D734136005363014E0E2BA76299CB00222728561D23C37788C159A3EC3560` | `maps/zombie_moon_sq.gsc` plus `maps/zombie_moon_sq_*.gsc` helpers |

The patched scripts should be treated as the installed retail baseline. These hashes identify the local inputs; another game build may contain different scripts.

## Call of the Dead — Stand-In and Ensemble Cast

The header of `zombie_coast_eggs.gsc` names the intended sequence and branch explicitly:

1. Shared: Fuse Fun (door fuse).
2. Shared: Holy Grenade (four generator targets).
3. Solo Stand-In: Bring up the Sub / lighthouse signal; jump to the shared late stages.
4. Co-op Ensemble Cast: Drink Up (vodka), Art Critic (four pressure locations), Musical Chairs (ship controls and signal sequence), Pure Harmony (lighthouse dials).
5. Shared: Sacrificial Resurrection (raise and recover the V-R11 human / Golden Rod) and Damn Machines (restart the fuse box and finish).

The controller starts shared handlers for the fuse, grenade targets, ship interaction, and finale. It starts Drink Up, Art Critic, and Pure Harmony only when more than one player is present. The source therefore resolves the older guide disagreement: destroying the four generators is a shared stage, not the Ensemble Cast-only stage. The existing data node was adjusted to reflect that and the Stand-In path now includes it. The co-op controller sequence has distinct Drink Up, Art Critic, Musical Chairs, and Pure Harmony stages; the solo header uses Bring Up the Sub before the shared finale. The tracker flow now uses those actual branches and does not split Musical Chairs into extra duplicated stages.

The script initializes compact flags, including `ffs`/`ffd` for fuse progression, `hgs`, `hg0`–`hg3`, and `hgd` for the four-generator sequence, `bd` for vodka delivery, `aca` for Art Critic progression, `mcs` for Musical Chairs, and `mm` for Pure Harmony. These are internal flags, not yet proven readable from the companion observer. `zombie_coast_achievement.gsc` waits for `coast_easter_egg_achieved` before recording the map sidequest completion. That is a candidate final success signal; live capture is still unverified.

The Call of the Dead observer now samples the stock flags `power_on`, `ffs`, `ffd`, `hgd`, `bd`, `aca`, `mcs`, `mm`, and `re`, then listens for `coast_easter_egg_achieved`. The shared game feed carries the map code `zombie_coast`; the reducer selects Stand-In for one player and Ensemble Cast for multiple players, following the stock `_e_group = players.size > 1` branch. The matching flow steps now reference these success signals. This wiring is compiled but not yet verified in a live Call of the Dead match.

## Shangri-La — Time Travel Will Tell

`zombie_temple.gsc` starts `zombie_temple_sq::start_temple_sidequest()`. The quest controller declares one sidequest and delegates the individual tasks to helpers for Brock and Gary, tile matching, waterslide, gas/leaks, tunnel holes, sundial/dials, gongs, and dynamite/crystal handling.

Unlike Ascension, most objectives do not have a dedicated persistent success flag. The shared sidequest framework starts and completes the currently active stage; the map controller uses a generic `stage_completed("sq", level._last_stage_started)` path. The observer now uses source flags for Brock/Gary setup, tile progression, waterslide, gas, tunnel holes, gongs, dynamite, and meteor shrink, and listens for stock helper-stage completion events. These source-to-guide mappings are built but still require live verification; where a flag is only an intermediate cue, the flow does not mark the corresponding node complete.

The offline source confirms the quest completion notification `temple_sidequest_achieved`. It also confirms four gongs are randomized as correct each match, so the tracker must not publish a fixed correct-gong list.

The Any Player EE v2.3.1 source replaces the corresponding temple scripts externally and uses configurable DVAR logic for Eclipse buttons, tile behavior, and water-slide requirements. Those settings must remain variant/configuration-dependent; `Unknown` must not inherit a guessed player count. The external archive remains unmodified.

## Moon — Richtofen's Grand Scheme / Big Bang Theory

The stock quest is coordinated by `zombie_moon_sq.gsc` with helper modules for Samantha Says, control panels, the Vril Generator, excavator/tank steps, and the Gersh device. The controller waits for power, then advances through named internal stages and explicit completion waits. Relevant stage names include `ss1`, `osc`, `sc`, `sc2`, and `ss2`; supporting parallel quest groups use `be`, `tanks`, and `ctvg` stages. The map observer now watches the corresponding stock flags and stage notifications and links them to the reviewed nodes.

The root script initializes `ss1`, `vg_charged`, `soul_swap_done`, and `be2` among other flags. It waits on helper events such as `sq_osc_over`, `sq_sc_over`, `sq_sc2_over`, and `sq_ss2_over`. The final rocket path emits `moon_sidequest_big_bang_achieved`. The observer reads the tank trigger entities' `fill` and `max_fill` counters for the four soul-fill bars. Samantha Says stores its generated answer in a function-local variable, but displays each prompt by changing the terminal model to one of four named color models; the observer samples stable, matching display models rather than reading that private sequence variable. This color path and the tank counters are built and await live verification.

The Any Player EE source changes the previous-map Vril Generator eligibility through `any_player_ee_moon_generator` and still uses the same main sidequest coordinator. This is a prerequisite/eligibility variation, not evidence that the Moon quest's own generator construction and charging stages disappear. The flow therefore keeps the plates, cable, and generator-charge steps in both variants; only the documented prior-map prerequisite may vary. Apply no exact behavior until the effective mod/configuration is confirmed.

`soul_swap_done` and `moon_sidequest_big_bang_achieved` are distinct events in the flow. The latter is emitted during the ending rocket sequence, so the tracker must not mark the final Big Bang Theory ending complete at the earlier soul-swap milestone.

## Detection boundary

Offline extraction establishes the source code and candidate signals. The Call of the Dead, Shangri-La, and Moon observers and reducers are now wired to the reviewed signals, but those three maps' in-game loading and event capture still need live verification. Do not promote interaction triggers, intermediate flags, or guide cues into automatic completion events without verifying their success semantics.
