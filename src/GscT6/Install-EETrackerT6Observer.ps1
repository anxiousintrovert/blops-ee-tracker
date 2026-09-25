param(
    [string]$PlutoniumT6Storage = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium\storage\t6')
)

$ErrorActionPreference = 'Stop'
$maps = @(
    @{ Map = 'zm_transit'; Source = 'tracker_transit.gsc'; Target = 'ee_tracker_transit.gsc' },
    @{ Map = 'zm_highrise'; Source = 'tracker_highrise.gsc'; Target = 'ee_tracker_highrise.gsc' },
    @{ Map = 'zm_buried'; Source = 'tracker_buried.gsc'; Target = 'ee_tracker_buried.gsc' },
    @{ Map = 'zm_prison'; Source = 'tracker_prison.gsc'; Target = 'ee_tracker_prison.gsc' },
    @{ Map = 'zm_tomb'; Source = 'tracker_tomb.gsc'; Target = 'ee_tracker_tomb.gsc' }
)
$scriptdataDirectory = Join-Path $PlutoniumT6Storage 'raw\scriptdata'

New-Item -ItemType Directory -Path $scriptdataDirectory -Force | Out-Null
foreach ($map in $maps) {
    $sourcePath = Join-Path $PSScriptRoot $map.Source
    $targetDirectory = Join-Path $PlutoniumT6Storage ("scripts\zm\" + $map.Map)
    $targetPath = Join-Path $targetDirectory $map.Target
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
    if ($map.Map -ne 'zm_prison') {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'quest_part_hooks.gsc') -Destination (Join-Path $targetDirectory 'quest_part_hooks.gsc') -Force
    }
    Write-Output "Installed $($map.Map) observer at $targetPath"
}

Write-Output "Observers load on new matches for their maps. Telemetry output: $scriptdataDirectory\ee-tracker-bo2.jsonl"
Write-Output 'Select BO2 in EETracker before launching a new match; the app starts the collector automatically.'
