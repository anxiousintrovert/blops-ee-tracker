# Zombies Tracker desktop layout

The second-monitor mission screen uses one WPF shell for Ascension, Call of the Dead, Shangri-La, and Moon. Original map artwork fills the banner beside round, player, and connection cards. Current Objective, Preparation, Next Step, and the stage timeline use the same native WPF components across maps. Settings and diagnostics remain separate views.

- The current title and instruction come directly from the quest engine. The next card shows the next script-defined step and its existing instruction.
- Preparation rows display the engine's existing requirements. A green check appears only for a confirmed simple power requirement; other requirements stay open for the player to check. Route guidance is labeled as information.
- Discrete objective checkpoints use check and waiting states from observed quest signals. Incremental trackers use a progress ring and bar from the active tracker state. Moon canisters use four bars from soul-fill telemetry. The pressure countdown uses its observed timer value.
- Progress controls occupy the open middle of the objective card. At narrower widths, tracker details move below the ring. Preparation rows and the next-step text use their card height so short entries do not cluster at the top.
- The timeline preserves the exact node order and statuses projected by the quest engine. The visual style uses condensed headings, dark gradient panels, thin borders, and gold and green status accents inspired by the approved mockups.
- Player and character assignments are not inferred from player count. The roster remains absent until the quest state provides structured assignments.
- Banner panoramas are original AI-generated environment art made for this app with OpenAI image generation. They are decorative assets only and contain no game UI or logos; no external image sources are used.

The UI changes do not alter the quest flow JSON, telemetry protocol, or quest engine transitions. Rendered preview captures:

- [Ascension](screenshots/EETracker-artdirection-ascension.png)
- [Call of the Dead](screenshots/EETracker-artdirection-call-of-the-dead.png)
- [Shangri-La](screenshots/EETracker-artdirection-shangri-la.png)
- [Moon](screenshots/EETracker-artdirection-moon.png)
