# BO2 Any Player EE guidance

EETracker includes step guidance for Hadi77KSA's [BO2 Any Player EE GitHub releases](https://github.com/Hadi77KSA/Plutonium-T6-Any-Player-EE-Scripts/releases), using the [v2.4.1 release](https://github.com/Hadi77KSA/Plutonium-T6-Any-Player-EE-Scripts/releases/tag/v2.4.1) audited for these instructions. The pinned source checkout used for this audit is commit `eb9784f6d5531ed72217d68a8d5ff25c4f453dfa`. The external gameplay mod is not bundled with EETracker; it must be installed by the host.

The objective page reads the current lobby player count and adds matching guidance to affected objectives. It labels every entry with its mod name because EETracker does not yet detect whether a particular third-party GSC mod is loaded. Treat the guidance as conditional: follow it only when that script/package is loaded on the host. With player count unavailable, the UI lists all count variants. Full Quest shows all count-specific notes per step.

## Base Any Player EE changes

| Map / objective | Player-count behavior documented by the release |
| --- | --- |
| TranZit · Maxis tower and lamp-post steps | Solo can use one Turbine for the tower/lamp-post behavior. The base package does not change Richtofen's four EMP lights. |
| Die Rise · Nav table | The mod builds a missing NAV table by default. The host can disable this with `any_player_ee_highrise_nav 0`. |
| Die Rise · elevator symbols | 1/2/3 players require 1/2/3 of the four symbols; 4 players still require all four. |
| Die Rise · floor-symbol puzzle | 1/2/3 players require 1/2/3 of the four symbols; 4 players still require all four. A separate optional patch prevents a wrong choice from resetting the puzzle. |
| Die Rise · Maxis Trample Steams | With 3 or fewer players, both balls can use the same set. Solo needs one pad only once the first ball is already flinging; on 3-player games the first ball must be flinging before the second can use the lone pad. 4 players use stock behavior. |
| Die Rise · Richtofen Trample Steams | Required symbol/pad count follows lobby size: one per player (up to the four stock symbols). |
| Buried · Maxis wisp | At 1–2 players, wisp energy regenerates; it does not need to rely on nearby zombies for that energy. Continue the route's stock sign and interaction sequence. |
| Buried · Maxis Bells | At 1–2 players, the time limit is removed; a failed attempt still resets. |
| Buried · Richtofen Time Bomb / Round Infinity | At 1–4 players, every lobby player must be near the Guillotine/Time Bomb. Above four players, exactly four players must be nearby. |
| Buried · Sharpshooter | Release defaults: 1 player, 20 targets (Candy Shop); 2 players, 39 (Candy Shop + Saloon); 3+ players, all 84. `any_player_ee_buried_ows` can override the target threshold. |
| Buried · optional Super Easter Egg | Collective NAVcards across TranZit/Die Rise/Buried and consistent faction completion by all lobby players are required. With more than four players, the mod checks only the player progress represented on the Buried box. This post-quest activity is documented in the completion step but is not yet an independently observed flow. |
| Origins · One-Inch Punch | In 5+ player lobbies, extra muddy tablet rewards spawn by the challenge boxes so players can obtain the One-Inch Punch and finish the objective. |

The release FAQ lists the Die Rise gameplay script for 1–3 player lobbies, not 5+ player lobbies. The UI calls out that boundary instead of extending reduced-count rules beyond the documented range. Four-player behavior is the stock requirement.

## Separate optional scripts in the linked release

These are not changes from the base Any Player EE gameplay package, so the UI labels them separately:

- **TranZit Extra:** the solo Richtofen EMP step uses two EMPs instead of four.
- **Die Rise Extra:** for fewer than four players on Maxis Trample Steams, extra pads can be picked up and zombies ignore deployed pads.
- **Buried Extra:** optional solo Bells auto-completion and an optional three-player sharpshooter target-choice behavior.
- **Mob of the Dead solo script:** after all mobsters' numbers are entered at the Citadel keypad in solo, it spawns a bot to allow the finale. This is a separate script; the base Any Player EE package does not modify Mob of the Dead.
- **Origins Extra:** changes the Rain Fire button timeout to 35 seconds; this is not a player-count rule.

## Configuration and evidence boundary

The released scripts expose settings for Die Rise elevator and floor-symbol requirements; Maxis/Richtofen Trample Steams; Buried Maxis wisp and bells; Buried Richtofen Round Infinity; Buried sharpshooter target minimum; and Super EE player checks. These values are host-configurable, so the UI's count-based guidance describes release defaults. If the host changed a DVAR, follow the configured value displayed by the host/mod rather than the default text.

The current T6 observer reports map, round, player count, and stock quest signals, but not the active Any Player EE script identity or custom DVAR values. It therefore must not claim that the mod is loaded or that a configured override is known. Quest completion remains driven by stock success flags; this guidance changes only the displayed instructions.

## Sources

- [BO2 Any Player EE GitHub releases](https://github.com/Hadi77KSA/Plutonium-T6-Any-Player-EE-Scripts/releases)
- [Plutonium release discussion](https://forum.plutonium.pw/topic/32568/release-zm-any-player-easter-egg-mods)
- [GitHub v2.4.1 release](https://github.com/Hadi77KSA/Plutonium-T6-Any-Player-EE-Scripts/releases/tag/v2.4.1)
- [GitHub source repository](https://github.com/Hadi77KSA/Plutonium-T6-Any-Player-EE-Scripts)
