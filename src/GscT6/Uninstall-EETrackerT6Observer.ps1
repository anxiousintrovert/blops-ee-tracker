param(
    [string]$PlutoniumT6Storage = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium\storage\t6')
)

$ErrorActionPreference = 'Stop'
$observers = @(
    @{ Map = 'zm_transit'; Target = 'ee_tracker_transit.gsc' },
    @{ Map = 'zm_highrise'; Target = 'ee_tracker_highrise.gsc' },
    @{ Map = 'zm_buried'; Target = 'ee_tracker_buried.gsc' },
    @{ Map = 'zm_prison'; Target = 'ee_tracker_prison.gsc' },
    @{ Map = 'zm_tomb'; Target = 'ee_tracker_tomb.gsc' }
)

foreach ($observer in $observers) {
    $targetPath = Join-Path $PlutoniumT6Storage ("scripts\zm\" + $observer.Map + "\" + $observer.Target)
    if (Test-Path -LiteralPath $targetPath) {
        Remove-Item -LiteralPath $targetPath -Force
        Write-Output "Removed $($observer.Map) observer at $targetPath"
    }
    if ($observer.Map -ne 'zm_prison') {
        $hookPath = Join-Path $PlutoniumT6Storage ("scripts\zm\" + $observer.Map + "\quest_part_hooks.gsc")
        if (Test-Path -LiteralPath $hookPath) {
            Remove-Item -LiteralPath $hookPath -Force
            Write-Output "Removed $($observer.Map) quest-part hooks at $hookPath"
        }
    }
}

Write-Output 'The ee-tracker-bo2.jsonl telemetry log was kept.'
