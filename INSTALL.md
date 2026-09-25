# Install Blops EE Tracker

## Requirements

- Windows 10 or 11, 64-bit.
- Plutonium T5/T6 with Black Ops Zombies for live tracking. The app can also run built-in replays without Plutonium.

The desktop release includes the .NET 8 runtime and does not need a separate .NET installation.

## 1. Download and launch EETracker

1. Open the [latest release](https://github.com/anxiousintrovert/blops-ee-tracker/releases/latest).
2. Download `EETracker-0.2.0-win-x64.zip` and extract the whole archive to a folder you can keep, such as `Documents\Blops EE Tracker`.
3. Run `EETracker.App.exe` from the extracted folder. Keep the other extracted files beside it; the package includes map data, replay samples, and observer installers.

The app starts following the local Plutonium session feed automatically. To try it without a live match, open **Settings** and load one of the built-in map previews.

## 2. Install the tracker observers

The desktop archive includes the observer scripts under `gsc`. You can instead download the separate `EETracker-0.2.0-GSC.zip` package from the release. These observers are EETracker telemetry scripts; they are separate from the BO1 gameplay mod linked below.

1. Extract the GSC package, or use the `gsc` folder from the desktop package.
2. Open PowerShell in the extracted `gsc` folder. In File Explorer, open that folder, click the address bar, type `powershell`, and press Enter.
3. For BO1, install the T5 observers:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-EETrackerObserver.ps1
   ```

The installer copies the four BO1 map observers into Plutonium's map-scoped raw-script folders and creates the local `raw\scriptdata` directory. It does not replace stock game scripts. Start a new Zombies match after installing so the map observer loads.

The installer supports a custom T5 storage folder for testing or non-default setups:

```powershell
.\Install-EETrackerObserver.ps1 -PlutoniumT5Storage 'D:\Plutonium\storage\t5'
```

For BO2, install the T6 observers from the same PowerShell window by running:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\t6\Install-EETrackerT6Observer.ps1
```

The standalone GSC archive contains both the T5 and T6 observer folders and their separate installers/uninstallers. T6 callback pickup hooks are compiled and source-reviewed, but still need private-match verification. The observer feed remains local to this computer. If you only play one game, you only need to run that game's installer.

## 3. BO1 gameplay mod (optional, when playing Any Player EE)

EETracker does not include or install gameplay mods. The BO1 Any Player EE gameplay mod used with the BO1 guides is maintained separately by [Hadi77KSA](https://github.com/Hadi77KSA/T5-Any-Player-EE-Scripts); download it from the project's [latest releases](https://github.com/Hadi77KSA/T5-Any-Player-EE-Scripts/releases). Choose the regular `release_main.zip` for the standard Any Player EE scripts. The `release_speedruns.zip` is an alternate ruleset intended for speedruns.

For Plutonium, follow the mod author's [installation instructions](https://github.com/Hadi77KSA/T5-Any-Player-EE-Scripts#installation) and Plutonium's [T5 mod loading guide](https://plutonium.pw/docs/modding/loading-mods/#loading-mods--custom-zombies-maps-for-bo1). Each player joining the same modded match should use the same mod or have no conflicting mod loaded. EETracker only observes the match; installing this gameplay mod is your choice and isn't required just to run the companion app.

## 4. Confirm the connection

With the game running on a supported map, the desktop client should show the map, round, player count, and connection state. If it stays at **No Game Signal**, check that the matching observer exists under `%LOCALAPPDATA%\Plutonium\storage\t5\raw\scripts\sp` for BO1 or `%LOCALAPPDATA%\Plutonium\storage\t6\raw\scripts\sp` for BO2, file I/O is enabled for the match, and you started a new match after installing.

The feed is local to that computer. Another player's app does not receive your quest events automatically; network sharing is not part of this release.

## Uninstall the EETracker observers

Run the included `Uninstall-EETrackerObserver.ps1` from PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-EETrackerObserver.ps1
```

The T5 uninstaller removes only the four EETracker observer files and leaves the session log in place. For BO2, run `t6\Uninstall-EETrackerT6Observer.ps1` instead. Delete the extracted desktop/GSC folders yourself if you no longer need the app. Remove the Any Player EE gameplay mod separately by following its project instructions.

## Current verification status

Ascension has live observer evidence. The Call of the Dead, Shangri-La, and Moon observers are packaged and source-reviewed; those maps still need live-match checks for observer loading and their newer progress signals. See [the verification record](docs/live-verification.md).
