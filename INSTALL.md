# Install Blops EE Tracker

## Requirements

- Windows 10 or 11, 64-bit.
- Plutonium T5/T6 with Black Ops Zombies for live tracking. The app can also run built-in replays without Plutonium.

The desktop release includes the .NET 8 runtime and does not need a separate .NET installation.

## 1. Download the desktop client

1. Open the [latest release](https://github.com/anxiousintrovert/blops-ee-tracker/releases/latest).
2. Download `BlopsEETracker-v0.1.0-win-x64.zip` and extract the whole archive to a folder you can keep, such as `Documents\Blops EE Tracker`.
3. Run `EETracker.App.exe` from the extracted folder. Keep the other files beside it; the package includes map data, replay samples, and the GSC install helpers.

The app starts following the local Plutonium session feed automatically. To try it without a live match, open **Settings** and load one of the built-in map previews.

## 2. Install the Plutonium T5 observers

The desktop archive includes the observers under `gsc`. You can also download the smaller `BlopsEETracker-v0.1.0-GSC.zip` package from the same release.

1. Extract the GSC package, or use the `gsc` folder from the desktop package.
2. Open PowerShell in the extracted `gsc` folder.
3. Run:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-EETrackerObserver.ps1
   ```

The installer copies the four map observers into Plutonium's map-scoped raw-script folders and creates the local `raw\scriptdata` directory. It does not replace stock game scripts. Start a new Zombies match after installing so the map observer loads.

The installer supports a custom T5 storage folder for testing or non-default setups:

```powershell
.\Install-EETrackerObserver.ps1 -PlutoniumT5Storage 'D:\Plutonium\storage\t5'
```

The desktop package also includes BO2 T6 observers under `gsc\t6`. To install them, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\t6\Install-EETrackerT6Observer.ps1
```

The standalone GSC archive contains both the T5 and T6 observer folders and their separate installers/uninstallers. T6 callback pickup hooks are compiled and source-reviewed, but still need private-match verification. The observer feed remains local to this computer.

## 3. Confirm the connection

With the game running on one of the four supported maps, the desktop client should show the map, round, player count, and connection state. If it stays at **No Game Signal**, check that the matching observer exists under `%LOCALAPPDATA%\Plutonium\storage\t5\raw\scripts\sp`, file I/O is enabled for the match, and you started a new match after installing.

The feed is local to that computer. Another player's app does not receive your quest events automatically; network sharing is not part of this release.

## Uninstall

Run the included `Uninstall-EETrackerObserver.ps1` from PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-EETrackerObserver.ps1
```

It removes only the four EE Tracker observer files and leaves the session log in place. Delete the extracted desktop/GSC folders yourself if you no longer need the app.

## Current verification status

Ascension has live observer evidence. The Call of the Dead, Shangri-La, and Moon observers are packaged and source-reviewed; those maps still need live-match checks for observer loading and their newer progress signals. See [the verification record](docs/live-verification.md).
