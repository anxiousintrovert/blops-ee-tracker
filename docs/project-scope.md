# EETracker project scope

## Initial game: Black Ops 1

The intended first-game coverage is all BO1 Zombies Easter Egg quests, not Ascension alone. The first researched main-quest catalog is the four multi-step quest maps:

- Ascension
- Call of the Dead
- Shangri-La
- Moon
- Five and Kino der Toten: side Easter Egg and setup flows (their side quests must be catalogued separately)
- WaW classics in BO1 (Nacht der Untoten, Verrückt, Shi No Numa, Der Riese): map-specific side Easter Egg flows

The researched main quest paths are stored in `data/bo1-main-quest-flows.json`; unresolved source conflicts are marked inline. This is research data, not a claim that all paths are implemented in the UI or verified against a live match. The first implementation milestone remains one vertical slice: Ascension status detection and one monkey-round button quest signal. Side Easter Egg catalogs for other BO1 maps are a separate research pass.

## Future games

Keep game identity, map identity, quest definitions, variant rules, and telemetry adapters separate from the shared WPF presentation and event transport. BO2 and BO3 are future additions; do not assume their mod loaders, script APIs, telemetry access, or quest behavior match BO1. Each game needs its own source audit and evidence ledger before live hooks are claimed.

## Architecture boundary

- `EETracker.Core`: common event schema, replay, deterministic reduction, and reusable quest-state primitives.
- Per-game/map quest modules: event interpretation, dependency graph, variant-specific rules, and manual confirmation rules.
- Game instrumentation: separate, host-side GSC or other verified telemetry adapter per supported runtime.
- `EETracker.App`: presentation only; no game-specific quest logic belongs in UI code.
