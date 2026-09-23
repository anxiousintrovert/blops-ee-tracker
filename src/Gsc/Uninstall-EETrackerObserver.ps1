param(
    [string]$PlutoniumT5Storage = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium\storage\t5')
)

$ErrorActionPreference = 'Stop'

$observers = @(
    @{ Map = 'Ascension'; Path = 'raw\scripts\sp\zombie_cosmodrome\ee_tracker.gsc' },
    @{ Map = 'Call of the Dead'; Path = 'raw\scripts\sp\zombie_coast\ee_tracker.gsc' },
    @{ Map = 'Shangri-La'; Path = 'raw\scripts\sp\zombie_temple\ee_tracker.gsc' },
    @{ Map = 'Moon'; Path = 'raw\scripts\sp\zombie_moon\ee_tracker.gsc' }
)

foreach ($observer in $observers) {
    $path = Join-Path $PlutoniumT5Storage $observer.Path
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Remove-Item -LiteralPath $path -Force
        Write-Output "Removed the EE Tracker observer for $($observer.Map)."
    }
}

Write-Output 'The ee-tracker.jsonl session log was kept.'
