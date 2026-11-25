<#
.SYNOPSIS
    Launches the built player windowed for each scene, captures an in-game screenshot and scans Player.log
    for exceptions/errors. Uses the game's -ra-* automation switches (CommandLineAutomation).
.EXAMPLE
    ./scripts/smoke-player.ps1 -Scenes MainMenu,Level01_Rooftops
#>
[CmdletBinding()]
param(
    [string[]]$Scenes = @('MainMenu', 'Hub', 'Level01_Rooftops', 'RubricShowcase', 'Credits'),
    [string]$OutDir,
    [int]$Seconds = 10,
    [string]$Spawn
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$exe = Join-Path (Get-ProjectPath) 'Builds\Windows\RootAccess.exe'
if (-not (Test-Path $exe)) { Write-Error "Build not found: $exe (run scripts/build.ps1)"; exit 1 }
if (-not $OutDir) { $OutDir = Join-Path (Get-ProjectPath) 'Logs\smoke' }
if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutDir)) }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$failed = 0
foreach ($scene in $Scenes) {
    $log = Join-Path $OutDir "$scene.log"
    $shot = Join-Path $OutDir "$scene.png"
    Remove-Item $log, $shot -ErrorAction SilentlyContinue
    $playerArgs = @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720', '-logFile', "`"$log`"",
        '-ra-screenshot', "`"$shot`"", '-ra-quit-after', "$Seconds", '-ra-newgame')
    if ($scene -ne 'MainMenu') { $playerArgs += @('-ra-scene', $scene) }
    if ($Spawn) { $playerArgs += @('-ra-spawn', $Spawn) }

    $process = Start-Process -FilePath $exe -ArgumentList ($playerArgs -join ' ') -PassThru
    if (-not $process.WaitForExit(($Seconds + 30) * 1000)) {
        $process.Kill()
        Write-Host "[smoke] $scene : TIMEOUT (killed)" -ForegroundColor Red
        $failed++
        continue
    }

    $problems = @()
    if (Test-Path $log) {
        $problems = @(Select-String -Path $log -Pattern 'Exception', 'NullReference', 'MissingReference', 'Error:', 'error\b' |
            Where-Object { $_.Line -notmatch 'Licens|d3d11|Fallback|WindowsGameCoreInput|XInput' } | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique)
    }

    $hasShot = Test-Path $shot
    $status = if ($problems.Count -eq 0 -and $hasShot) { 'OK' } else { 'CHECK' }
    if ($status -ne 'OK') { $failed++ }
    Write-Host ("[smoke] {0,-16} {1}  screenshot={2}  issues={3}" -f $scene, $status, $hasShot, $problems.Count)
    $problems | Select-Object -First 10 | ForEach-Object { Write-Host "    ! $_" }
}

if ($failed -gt 0) { exit 1 }
exit 0
