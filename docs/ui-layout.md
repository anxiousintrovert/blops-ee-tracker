# Zombies Tracker desktop layout

The second-monitor mission screen uses one WPF shell for all four BO1 maps and all five BO2 maps. Original map artwork fills the banner beside round, player, and connection cards. Current Objective, Preparation, Next Step, and the stage timeline use the same native WPF components across maps. Settings and diagnostics remain separate views.

- The current title and instruction come directly from the quest engine. The next card shows the next script-defined step and its existing instruction.
- Preparation rows display the engine's existing requirements. A green check appears only for a confirmed simple power requirement; other requirements stay open for the player to check. Route guidance is labeled as information.
- Discrete objective checkpoints use check and waiting states from observed quest signals. Incremental trackers use one progress indicator with the active tracker state. Moon canisters use four full-width bars from soul-fill telemetry. The pressure countdown uses its observed timer value.
- Objective details and tracker cards expand across the available objective-card width. Long details scroll within that panel. The Next Step card preserves its wrapped title, instructions, and Previous line in its own scroll area.
- The timeline preserves the exact node order and statuses projected by the quest engine. Its step cards scroll horizontally and keep current and upcoming labels readable, with a compact current-step count in the header. Preparation rows retain an independent scroll area.
- Player and character assignments are not inferred from player count. The roster remains absent until the quest state provides structured assignments.
- Banner panoramas are original AI-generated environment art made for this app with OpenAI image generation. They are decorative assets only and contain no game UI or logos; no external image sources are used.

The UI changes do not alter quest flow order, telemetry protocol, or quest engine transitions. Rendered preview captures from replay fixtures (UI previews, not evidence of live-match observer behavior):

- BO1: [Ascension](screenshots/eetracker-bo1-ascension.png), [Call of the Dead](screenshots/eetracker-bo1-call-of-the-dead.png), [Shangri-La](screenshots/eetracker-bo1-shangri-la.png), [Moon](screenshots/eetracker-bo1-moon.png)
- BO2: [TranZit](screenshots/eetracker-bo2-tranzit.png), [Die Rise](screenshots/eetracker-bo2-die-rise.png), [Buried](screenshots/eetracker-bo2-buried.png), [Mob of the Dead](screenshots/eetracker-bo2-mob-of-the-dead.png), [Origins](screenshots/eetracker-bo2-origins.png)
