# T5 map observers

The package includes four Plutonium T5 map-scoped observers:

- `tracker.gsc` — Ascension (`zombie_cosmodrome`)
- `tracker_coast.gsc` — Call of the Dead (`zombie_coast`)
- `tracker_temple.gsc` — Shangri-La (`zombie_temple`)
- `tracker_moon.gsc` — Moon (`zombie_moon`)

Run `Install-EETrackerObserver.ps1` to install all four observers and create the shared `raw/scriptdata` directory. Start a new map after installing. Run `Uninstall-EETrackerObserver.ps1` to remove only the observers. Both scripts accept an optional `-PlutoniumT5Storage` path for non-default storage folders.

The desktop app follows the shared local `raw/scriptdata/ee-tracker.jsonl` file. The feed does not synchronize between players over a network.

Ascension has live observer evidence through multiple quest flags. The other three map observers have source-reviewed signals but still need live-match verification. Quest completion continues to follow stock success flags and stage events; substep counters are informational and do not advance the main quest.
