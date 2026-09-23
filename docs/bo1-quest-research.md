# BO1 main quest research and flow decisions

## Coverage at this pass

The first catalog contains the four multi-step BO1 main quests: Ascension, Call of the Dead, Shangri-La, and Moon. Steps live in `data/bo1-main-quest-flows.json`. Other BO1 maps remain in the product scope, but their standalone side quests, songs, radios, and map secrets must be catalogued as separate map-scoped flows; they are not represented as main quests here.

BO1 map coverage to eventually catalog: Nacht der Untoten, Verrückt, Shi No Numa, Der Riese, Kino der Toten, Five, Ascension, Call of the Dead, Shangri-La, Moon. The first four are World at War maps included as BO1 Zombies content. Kino and Five do not have the same full multi-stage main quest structure as the four DLC quest maps; their side EEs must not be padded with another map's generic quest steps.

## Source base and confidence

- The walkthroughs are independently written community guides, not official game manuals. Their descriptions are paraphrased into short tracker instructions.
- [CODZombie: BO1 main quests](https://codzombie.com/games/black-ops) enumerates the four-quest release sequence.
- [CODZombie: Casimir Mechanism](https://www.codzombie.com/quests/ascension-casimir-mechanism) and [mmmrkennedy's Ascension guide](https://mmmrkennedy.com/games/BO1/ascension/ascension_guide) provide the player-facing walkthrough and equipment context. The stock GSC expands the walkthrough into six separately flagged quest events; see the local audit below.
- [Zombies Codex: Call of the Dead](https://www.zombiescodex.com/black-ops/call-of-the-dead/) gives a branched Stand-in / Ensemble Cast route, with detailed door, fuse, generator, radio, submarine, and finale steps. It cites CoD Wiki/GameRant corroboration. A separate [XCIBE95X Stand-in guide](https://xcibe95x.com/articles/bo1-call-of-the-dead-easter-egg-guide) describes a different solo generator sequence. The extracted stock quest header resolves this: the four-generator Holy Grenade stage is shared before the solo/co-op branch.
- [mmmrkennedy: Shangri-La](https://mmmrkennedy.com/games/BO1/shangri_la/shangri_la_guide) provides Eclipse, tile, slide, gas, Spikemore, dial, gong, dynamite, and Focusing Stone steps. It is a BO1/BO3 combined page; BO1-only versus BO3-only differences are excluded from the BO1 flow.
- [COD: Zombies Guides: Moon](https://www.codzombiesguides.com/main-quests/black-ops-1/moon/) gives the full BO1 branch through Big Bang Theory, including Samantha Says, lab hacking, Excavator Pi, Vril Sphere, MPD, plates/cable, soul swap, and rocket launch. [CODZombie: Moon](https://codzombie.com/quests/moon-richtofens-grand-scheme) is a useful overview, but compresses the underlying step sequence; the detailed guide is used for flow decomposition.
- The supplied Hadi77KSA forum HTML links to [T5 Any Player EE Scripts](https://github.com/Hadi77KSA/T5-Any-Player-EE-Scripts). Read-only inspection of v2.3.1 confirms the Ascension `sync_button_pressed` interaction notification and Moon/Shangri-La overrides. Stock script evidence from all four main quest maps is documented in [offline-quest-script-audit.md](offline-quest-script-audit.md); Ascension's detailed flag analysis remains in [ascension-offline-script-audit.md](ascension-offline-script-audit.md).

## No duplicated steps

Every step has a stable ID of the form `bo1.<map>.<quest-or-branch>.<step>`. Shared logic appears once as a node and paths reference that node; path references are not copied step definitions. The test suite checks globally unique node IDs, map prefixes, valid path references, and no repeated step within one path.

## Ascension source correction

The installed stock `maps/zombie_cosmodrome_eggs.gsc` names six state flags: `target_teleported`, `rerouted_power`, `switches_synced`, `pressure_sustained`, `passkey_confirmed`, and `weapons_combined`. These map one-to-one to six Ascension quest nodes in the flow data. The companion should observe these success states rather than promote `sync_button_pressed`, which is emitted for an interaction and may repeat while Use remains held. The stock script starts its event handlers concurrently; its comments express intended player order, while some prerequisites are enforced by map access and equipment rather than a hardcoded previous-flag check. The flow shows intended order while retaining a separate success signal for each node.

## Disagreement and variant rules

- Call of the Dead walkthroughs differ on the four generators. The stock script header resolves the branch: the Holy Grenade generator stage is shared by Stand-In and Ensemble Cast. The GSC flags are source evidence, but automatic companion detection remains unverified.
- Shangri-La vanilla button and waterslide gates require four players. Any Player EE has configurable button/solo behavior. The tracker must use confirmed variant/config evidence; an unknown variant must not inherit a guessed button count.
- Moon's installed Any Player EE v2.3.1 source changes the prior-map eligibility check through `any_player_ee_moon_generator`. The stock coordinator still waits for the Moon-local `vg_charged` flag, so this setting is not treated as proof that the plates, cable, or charging steps disappear. The effective runtime value and SR package behavior remain to be confirmed.
- Quest completion milestones can differ: Moon marks Cryogenic Slumber Party at the soul swap, then Big Bang Theory only after the final rocket sequence. Both must be represented in the same map flow without treating the first as the full ending.
