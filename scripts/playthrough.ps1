<#
.SYNOPSIS
    Automated playthrough of the built game: the bot (Scripts/Playthrough) starts a new game and plays the hideout and
    levels 1-6 along the generated route with real input, then writes Playthrough.md and screenshots.
.PARAMETER Level
    Play only this level scene (e.g. Level03_ServerFarm) instead of the whole campaign.
.PARAMETER Invulnerable
    The player ignores damage: checks the route itself (jumps, climbs, hacks, rides); hits are still counted.
.PARAMETER OutDir
    Report and screenshots folder (default: RootAccess/Logs/Playthrough[/<Level>]).
.EXAMPLE
    ./scripts/playthrough.ps1
    ./scripts/playthrough.ps1 -Level Level01_Rooftops -Invulnerable
#>
[CmdletBinding()]
param(
    [string]$Level,
    [switch]$Invulnerable,
    [string]$OutDir,
    [int]$TimeoutMinutes = 90,
    [int]$Width = 1600,
    [int]$Height = 900
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$exe = Join-Path (Get-ProjectPath) 'Builds\Windows\RootAccess.exe'
if (-not (Test-Path $exe)) { Write-Error "Build not found: $exe (run scripts/build.ps1)"; exit 1 }
if (-not $OutDir) {
    $OutDir = Join-Path (Get-ProjectPath) 'Logs\Playthrough'
    if ($Level) { $OutDir = Join-Path $OutDir $Level }
}
if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutDir)) }
if (Test-Path $OutDir) { Remove-Item (Join-Path $OutDir '*') -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$log = Join-Path $OutDir 'Player.log'

$playerArgs = @('-screen-fullscreen', '0', '-screen-width', "$Width", '-screen-height', "$Height", '-logFile', "`"$log`"",
    '-ra-default-settings', '-ra-playthrough-out', "`"$OutDir`"")
if ($Level) { $playerArgs += @('-ra-playthrough-level', $Level) } else { $playerArgs += '-ra-playthrough' }
if ($Invulnerable) { $playerArgs += '-ra-invulnerable' }

Write-Host "[playthrough] $(if ($Level) { $Level } else { 'campaign' })$(if ($Invulnerable) { ' (invulnerable)' }) -> $OutDir"
$started = Get-Date
$process = Start-Process -FilePath $exe -ArgumentList ($playerArgs -join ' ') -PassThru
if (-not $process.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    $process.Kill()
    Write-Host "[playthrough] TIMEOUT after $TimeoutMinutes min" -ForegroundColor Red
}

$report = Join-Path $OutDir 'Playthrough.md'
$elapsed = [int]((Get-Date) - $started).TotalSeconds
if (-not (Test-Path $report)) { Write-Host "[playthrough] no report written ($elapsed s); see $log" -ForegroundColor Red; exit 1 }
Write-Host "[playthrough] done in $elapsed s; report: $report"
Get-Content $report -Encoding UTF8 | Select-Object -First 12
