param(
    [string]$PlutoniumRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Plutonium'),
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'
$consoleLog = Join-Path $PlutoniumRoot 'console.log'
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PlutoniumRoot 'storage\t6\raw\scriptdata\ee-tracker-bo2.jsonl'
}

if (-not (Test-Path -LiteralPath $consoleLog)) {
    throw "Plutonium console log not found: $consoleLog"
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($OutputPath, '', $utf8NoBom)
Write-Output "Capturing [EETrackerT6] records from $consoleLog to $OutputPath"
Write-Output 'Start a new BO2 Zombies match on an observer-supported map. The EETracker app manages this collector automatically.'

Get-Content -LiteralPath $consoleLog -Tail 5000 -Wait | ForEach-Object {
    if ($_ -match '^\[EETrackerT6\] (\{.*\})\s*$') {
        $json = $Matches[1]
        try {
            $null = $json | ConvertFrom-Json -ErrorAction Stop
            [System.IO.File]::AppendAllText($OutputPath, $json + [Environment]::NewLine, $utf8NoBom)
        } catch {
            Write-Warning "Ignored invalid EETracker T6 record: $_"
        }
    }
}
