param(
    [string]$PlutoniumT5Storage = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium\storage\t5')
)

$ErrorActionPreference = 'Stop'
$scriptsDirectory = Join-Path $PlutoniumT5Storage 'raw\scripts\sp'
$mapCodes = @(
    'zombie_cod5_prototype',
    'zombie_cod5_asylum',
    'zombie_cod5_sumpf',
    'zombie_cod5_factory',
    'zombie_theater',
    'zombie_pentagon',
    'zombie_cosmodrome',
    'zombie_coast',
    'zombie_temple',
    'zombie_moon'
)

foreach ($mapCode in $mapCodes) {
    $targetPath = Join-Path (Join-Path $scriptsDirectory $mapCode) 'ee_tracker_test_points.gsc'
    if (Test-Path -LiteralPath $targetPath -PathType Leaf) {
        Remove-Item -LiteralPath $targetPath -Force
        Write-Output "Removed points helper for $mapCode"
    }
}
