$ErrorActionPreference = 'Stop'

$expectedFqbn = 'arduino:renesas_uno:unor4wifi'
$sketchPath = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\firmware\arduino\TwoChannelCsvStreamer')

if (-not (Get-Command arduino-cli -ErrorAction SilentlyContinue)) {
    throw 'Arduino CLI is required to upload firmware. Install Arduino CLI and make sure arduino-cli is available on PATH.'
}

arduino-cli version

Write-Host 'Detecting connected Arduino boards...'
$boardListJson = arduino-cli board list --json
$boardList = $boardListJson | ConvertFrom-Json

$matches = @()
foreach ($detectedPort in $boardList.detected_ports) {
    foreach ($board in @($detectedPort.matching_boards)) {
        if ($board.fqbn -eq $expectedFqbn -or $board.name -eq 'Arduino UNO R4 WiFi') {
            $matches += [pscustomobject]@{
                Port = $detectedPort.port.address
                Fqbn = $board.fqbn
                Name = $board.name
            }
        }
    }
}

if ($matches.Count -eq 0) {
    throw "No Arduino UNO R4 WiFi was detected. Run 'arduino-cli board list' and confirm the board is connected."
}

if ($matches.Count -gt 1) {
    $ports = ($matches | ForEach-Object { "$($_.Name) on $($_.Port) [$($_.Fqbn)]" }) -join '; '
    throw "Multiple Arduino UNO R4 WiFi boards were detected: $ports. Disconnect extras or upload manually with arduino-cli upload -p <PORT> --fqbn $expectedFqbn $sketchPath"
}

$board = $matches[0]
Write-Host "Using $($board.Name) on $($board.Port) with FQBN $($board.Fqbn)."

arduino-cli compile --fqbn $board.Fqbn $sketchPath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

arduino-cli upload -p $board.Port --fqbn $board.Fqbn $sketchPath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host 'Upload complete.'
