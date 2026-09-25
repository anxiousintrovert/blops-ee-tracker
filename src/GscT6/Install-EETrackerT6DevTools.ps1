param(
    [string]$PlutoniumT6Storage = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium\storage\t6')
)

$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot 'ee_devtools.gsc'
$targetDirectory = Join-Path $PlutoniumT6Storage 'scripts\zm'
$targetPath = Join-Path $targetDirectory 'ee_tracker_devtools.gsc'
New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
Write-Output "Installed opt-in development shortcuts at $targetPath"
Write-Output 'Start a private Zombies match, then use the Plutonium console: ee_dev_points 10000'
Write-Output 'To grant a weapon to all players: ee_dev_weapon ray_gun_zm'
Write-Output 'Points are capped at 50000 per command. This helper does not mark quest steps complete.'
