<#
.SYNOPSIS
    End-to-end LAN co-op check on one PC: starts the built game twice, a host (Infiltrator in Level 1) and a client
    (Operator console) joined on 127.0.0.1, lets the client's console act on a door through Netcode, then verifies both
    Player.logs (connection, level map, door opened on the host) and saves the client's screenshot.
.PARAMETER OutDir
    Logs and screenshots (default: RootAccess/Logs/coop).
.EXAMPLE
    ./scripts/coop-lan-smoke.ps1
#>
[CmdletBinding()]
param(
    [string]$OutDir,
    [string]$Scene = 'Level01_Rooftops',
    [string]$Spawn = 'l1_security'
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$exe = Join-Path (Get-ProjectPath) 'Builds\Windows\RootAccess.exe'
if (-not (Test-Path $exe)) { Write-Error "Build not found: $exe (run scripts/build.ps1)"; exit 1 }
if (-not $OutDir) { $OutDir = Join-Path (Get-ProjectPath) 'Logs\coop' }
# The game resolves relative paths against its own folder: hand it absolute ones.
if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutDir)) }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$hostLog = Join-Path $OutDir 'host.log'
$clientLog = Join-Path $OutDir 'client.log'
$hostShot = Join-Path $OutDir 'host.png'
$clientShot = Join-Path $OutDir 'operator.png'
Remove-Item $hostLog, $clientLog, $hostShot, $clientShot -ErrorAction SilentlyContinue

$common = @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720', '-ra-default-settings')
$hostArgs = $common + @('-logFile', "`"$hostLog`"", '-ra-newgame', '-ra-coop', 'host', '-ra-scene', $Scene, '-ra-spawn', $Spawn,
    '-ra-screenshot', "`"$hostShot`"", '-ra-screenshot-delay', '22', '-ra-quit-after', '32')
$clientArgs = $common + @('-logFile', "`"$clientLog`"", '-ra-coop', 'join', '-ra-coop-address', '127.0.0.1',
    '-ra-command', 'operator-act', '-ra-command-delay', '8', '-ra-screenshot', "`"$clientShot`"", '-ra-screenshot-delay', '11', '-ra-quit-after', '22')

Write-Host '[coop] starting host (Infiltrator)...'
$hostProcess = Start-Process -FilePath $exe -ArgumentList ($hostArgs -join ' ') -PassThru
Start-Sleep -Seconds 8
Write-Host '[coop] starting client (Operator) on 127.0.0.1...'
$clientProcess = Start-Process -FilePath $exe -ArgumentList ($clientArgs -join ' ') -PassThru

foreach ($process in @($clientProcess, $hostProcess)) {
    if (-not $process.WaitForExit(60000)) { $process.Kill(); Write-Host "[coop] process $($process.Id) timed out (killed)" -ForegroundColor Red }
}

function Test-Log([string]$path, [string]$pattern) { return (Test-Path $path) -and ((Select-String -Path $path -Pattern $pattern -SimpleMatch -Quiet)) }

$checks = [ordered]@{
    'host listening'                 = Test-Log $hostLog '[CoopSession] LAN host listening'
    'client joined'                  = Test-Log $clientLog '[CoopSession] Joining 127.0.0.1'
    'host saw the Operator connect'  = Test-Log $hostLog '[CoopSession] Peer connected: 1'
    'Operator console had targets'   = (Test-Path $clientLog) -and ((Select-String -Path $clientLog -Pattern 'Automation: acted on ([0-9-]+) \(([1-9][0-9]*) targets\)' -Quiet))
    'host applied the Operator door' = Test-Log $hostLog '[CoopSession] Operator acted on Door'
    'no exceptions on host'          = -not (Select-String -Path $hostLog -Pattern 'Exception' -Quiet)
    'no exceptions on client'        = -not (Select-String -Path $clientLog -Pattern 'Exception' -Quiet)
    'Operator screenshot'            = Test-Path $clientShot
}

$failed = 0
foreach ($check in $checks.GetEnumerator()) {
    $ok = [bool]$check.Value
    if (-not $ok) { $failed++ }
    Write-Host ("[coop] {0,-32} {1}" -f $check.Key, $(if ($ok) { 'OK' } else { 'FAIL' })) -ForegroundColor $(if ($ok) { 'Green' } else { 'Red' })
}

Write-Host "[coop] logs and screenshots in $OutDir"
exit $failed
