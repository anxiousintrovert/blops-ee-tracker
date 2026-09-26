param(
    [string]$PlutoniumT5Storage = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium\storage\t5')
)

$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot 'ee_tracker_test_points.gsc'
$scriptsDirectory = Join-Path $PlutoniumT5Storage 'raw\scripts\sp'
$mapCodes = @(
    'zombie_cod5_prototype', # Nacht der Untoten
    'zombie_cod5_asylum',   # Verruckt
    'zombie_cod5_sumpf',    # Shi No Numa
    'zombie_cod5_factory',  # Der Riese
    'zombie_theater',       # Kino der Toten
    'zombie_pentagon',      # Five
    'zombie_cosmodrome',    # Ascension
    'zombie_coast',         # Call of the Dead
    'zombie_temple',        # Shangri-La
    'zombie_moon'           # Moon
)

foreach ($mapCode in $mapCodes) {
    $mapDirectory = Join-Path $scriptsDirectory $mapCode
    $targetPath = Join-Path $mapDirectory 'ee_tracker_test_points.gsc'
    New-Item -ItemType Directory -Path $mapDirectory -Force | Out-Null
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
    Write-Output "Installed points helper for $mapCode at $targetPath"
}

Write-Output 'Start a new match or restart the map, then enter: ee_tracker_test_points 1'
Write-Output 'Each trigger adds 100000 points to every connected player.'
