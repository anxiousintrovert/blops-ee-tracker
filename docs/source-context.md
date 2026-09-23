# Supplied Any Player EE source context

The supplied Plutonium forum page is the topic **[Release][ZM] Solo and Any Player Easter Egg Mods**, by Hadi77KSA, first published 2023-10-06 and marked modified 2026-09-01. The page describes (but does not include source code for) the following:

- Any Player EE targets Ascension, Shangri-La, and Moon for any player count.
- Any Player EE SR targets Ascension and Shangri-La for speedruns.
- Ascension monkey-round buttons: Any Player EE requires as many buttons as players; SR requires all four within 100 seconds when fewer than four players.
- Lander letters: Any Player EE changes solo behavior; co-op is unchanged.
- Orb damage: Any Player EE removes the Matryoshka Doll requirement in solo, but retains Gersh Device, Zeus Cannon, and Porter's X2 Ray Gun; co-op is unchanged.
- Forum-listed DVARs include `any_player_ee_cosmodrome_buttons` and `_timeout`, plus other Ascension/Shangri-La/Moon settings. Defaults differ across vanilla and mod variants; DVAR presence/value alone does not identify variant reliably.
- The post says the mod shows a colored loaded message and describes `flashScriptHashes; scriptHashes` as a console method to check loaded scripts.
- Moon's Vril Generator behavior is changed by Any Player EE; SR's described scope omits Moon.

The captured page links to Hadi77KSA's source repository. A read-only review of tag `v2.3.1` found `any_player_ee/maps/zombie_cosmodrome_eggs.gsc`; in `wait_for_sync_use`, the source raises `level notify( "sync_button_pressed" )` after `UseButtonPressed()` while a player touches the button trigger. This is an interaction candidate only: it carries no button identity, may repeat while Use is held, and does not prove the switch sequence succeeded. `switch_watcher` evaluates success separately. The public configuration alone does not conclusively identify the active variant.

A Plutonium T5 changelog documents sandboxed GSC file I/O (`fs_fopen`, `fs_writeline`, and `fs_fclose`) under `scriptdata`, with `scr_allowFileIo` enabled by default unless a server disables it. This established a documented transport option, not by itself a verified end-to-end adapter. The local observer uses the `raw/scriptdata` location.

Stock Ascension scripts were also extracted offline from the game FastFiles with OpenAssetTools Unlinker. The results and hashes are recorded in [the Ascension offline script audit](ascension-offline-script-audit.md); the stock and Any Player EE source retain the same six quest-success flags, while the mod changes selected player-count rules.

Sources: [Hadi77KSA/T5-Any-Player-EE-Scripts v2.3.1](https://github.com/Hadi77KSA/T5-Any-Player-EE-Scripts/tree/v2.3.1/any_player_ee/maps), [v2.3.1 release and install instructions](https://github.com/Hadi77KSA/T5-Any-Player-EE-Scripts/releases/tag/v2.3.1), [Plutonium T5 mod loading guide](https://plutonium.pw/docs/modding/loading-mods/#loading-mods--custom-zombies-maps-for-bo1).

The inspected third-party release source and extracted scripts are not included in this project.
