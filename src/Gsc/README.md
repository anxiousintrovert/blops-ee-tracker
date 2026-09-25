# T5 map observers

The package includes four Plutonium T5 map-scoped observers:

- `tracker.gsc` — Ascension (`zombie_cosmodrome`)
- `tracker_coast.gsc` — Call of the Dead (`zombie_coast`)
- `tracker_temple.gsc` — Shangri-La (`zombie_temple`)
- `tracker_moon.gsc` — Moon (`zombie_moon`)

Run `Install-EETrackerObserver.ps1` to install all four observers and create the shared `raw/scriptdata` directory. Start a new map after installing. Run `Uninstall-EETrackerObserver.ps1` to remove only the observers. Both scripts accept an optional `-PlutoniumT5Storage` path for non-default storage folders.

The desktop app follows the shared local `raw/scriptdata/ee-tracker.jsonl` file. The feed does not synchronize between players over a network.

Ascension has live observer evidence through multiple quest flags. The other three map observers have source-reviewed signals but still need live-match verification. Quest completion continues to follow stock success flags and stage events; substep counters are informational and do not advance the main quest.

The observers also emit `side_egg_step` records when the full stock music trigger has completed: Ascension's third bear, Call of the Dead/Shangri-La/Moon's third meteor. Since stock exposes only the count, the observer marks the whole three-item checklist together and does not claim which named location was activated first. This side signal is independent of main-quest progression and still needs a fresh private-match check after installation.

The mission view now has a quest-item inventory panel. T5 observers poll the current players and emit changes only for map-allowlisted main-quest weapons and tacticals from `data/quest-item-catalog.json`; ordinary weapon loadouts are omitted. Player numbers are lobby-list slots and can change when players leave or reconnect. The weapon identifiers and inventory getter behavior still need private-match verification on each map. The feed is local to each player's Plutonium storage and is not relayed between separate companion-app installations.
