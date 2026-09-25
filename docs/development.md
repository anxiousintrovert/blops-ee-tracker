# Build from source

## Requirements

- Windows 10 or 11, 64-bit.
- .NET 8 SDK with Windows Desktop targeting support.

## Build and run

From the repository root:

```powershell
dotnet build EETracker.sln -c Release
dotnet run --project src/EETracker.App/EETracker.App.csproj -c Release
```

## Publish a portable desktop package

```powershell
dotnet publish src/EETracker.App/EETracker.App.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
  -o dist/desktop
```

The app project copies the quest-flow data, replay samples, GSC observers, and observer install/uninstall scripts beside the application. Keep those files in the published archive.

## GSC observer package

The four T5 map-scoped observers and install helpers live in `src/Gsc`; the five T6 map-scoped observers and helpers live in `src/GscT6`. Each installer accepts an optional storage path, which is useful for isolated install checks. Installers copy only EETracker observer files and create the shared telemetry directory; uninstallers preserve session telemetry. T6 callback wrappers need private-match verification.

## Quest-flow changes

The map step sequences are data in `data/bo1-main-quest-flows.json`; the observers report game signals in `src/Gsc`. The UI projects this state and does not define its own quest order. Any gameplay or quest-flow edits require separate review and verification.

## Current limitations

Live observer evidence is strongest for Ascension. The other three observers have source-reviewed signals but still need live-match validation. See [the verification ledger](live-verification.md). The feed is a local file and does not synchronize between players over a network.
