param(
    [string]$PlutoniumT6Storage = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium\storage\t6')
)

$ErrorActionPreference = 'Stop'
$targetPath = Join-Path $PlutoniumT6Storage 'scripts\zm\ee_tracker_devtools.gsc'
if (Test-Path -LiteralPath $targetPath) {
    Remove-Item -LiteralPath $targetPath -Force
    Write-Output "Removed development shortcuts at $targetPath"
} else {
    Write-Output 'EETracker development shortcuts are not installed.'
}
