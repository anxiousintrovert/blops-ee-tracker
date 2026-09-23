param(
    [string]$PlutoniumT5Storage = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium\storage\t5')
)

$ErrorActionPreference = 'Stop'

$storageDirectory = Join-Path $PlutoniumT5Storage 'raw\scripts\sp'
$ascensionSourcePath = Join-Path $PSScriptRoot 'tracker.gsc'
$coastSourcePath = Join-Path $PSScriptRoot 'tracker_coast.gsc'
$templeSourcePath = Join-Path $PSScriptRoot 'tracker_temple.gsc'
$moonSourcePath = Join-Path $PSScriptRoot 'tracker_moon.gsc'
$ascensionScriptDirectory = Join-Path $storageDirectory 'zombie_cosmodrome'
$coastScriptDirectory = Join-Path $storageDirectory 'zombie_coast'
$templeScriptDirectory = Join-Path $storageDirectory 'zombie_temple'
$moonScriptDirectory = Join-Path $storageDirectory 'zombie_moon'
$scriptdataDirectory = Join-Path $PlutoniumT5Storage 'raw\scriptdata'
$ascensionTargetPath = Join-Path $ascensionScriptDirectory 'ee_tracker.gsc'
$coastTargetPath = Join-Path $coastScriptDirectory 'ee_tracker.gsc'
$templeTargetPath = Join-Path $templeScriptDirectory 'ee_tracker.gsc'
$moonTargetPath = Join-Path $moonScriptDirectory 'ee_tracker.gsc'

New-Item -ItemType Directory -Path $ascensionScriptDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $coastScriptDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $templeScriptDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $moonScriptDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $scriptdataDirectory -Force | Out-Null
Copy-Item -LiteralPath $ascensionSourcePath -Destination $ascensionTargetPath -Force
Copy-Item -LiteralPath $coastSourcePath -Destination $coastTargetPath -Force
Copy-Item -LiteralPath $templeSourcePath -Destination $templeTargetPath -Force
Copy-Item -LiteralPath $moonSourcePath -Destination $moonTargetPath -Force
Write-Output "Installed the Ascension observer at $ascensionTargetPath"
Write-Output "Installed the Call of the Dead observer at $coastTargetPath"
Write-Output "Installed the Shangri-La observer at $templeTargetPath"
Write-Output "Installed the Moon observer at $moonTargetPath"
