<#
.SYNOPSIS
    Captures in-game screenshots from the built player for documentation, one launch per shot, using the -ra-* automation
    switches (scene, spawn point, interact with a named object, broadcast UI commands, screenshot delay).
.PARAMETER Only
    Capture only the shots whose name matches one of these wildcards (e.g. "terminal-*").
.PARAMETER OutDir
    Output folder (default: Documentatie/screenshots).
.EXAMPLE
    ./scripts/capture.ps1
    ./scripts/capture.ps1 -Only terminal-*,hack-* -OutDir RootAccess/Logs/capture
#>
[CmdletBinding()]
param(
    [string[]]$Only = @('*'),
    [string]$OutDir,
    [int]$Width = 1920,
    [int]$Height = 1080
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$exe = Join-Path (Get-ProjectPath) 'Builds\Windows\RootAccess.exe'
if (-not (Test-Path $exe)) { Write-Error "Build not found: $exe (run scripts/build.ps1)"; exit 1 }
if (-not $OutDir) { $OutDir = Join-Path (Get-RepoRoot) 'Documentatie\screenshots' }
if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutDir)) }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$logDir = Join-Path (Get-ProjectPath) 'Logs\capture'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

# Name, Scene ('' = main menu), Spawn, Interact (object name), Commands (comma list), Delay (s) before the screenshot.
$shots = @(
    @{ Name = '01-meniu-principal';        Scene = '';                 Spawn = '';             Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '02-setari';                 Scene = '';                 Spawn = '';             Interact = '';              Command = 'settings';  Delay = 3 }
    @{ Name = '03-hub';                    Scene = 'Hub';              Spawn = 'hub_center';   Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '04-terminal-inventar';      Scene = 'Hub';              Spawn = 'hub_center';   Interact = '';              Command = 'inventory'; Delay = 2.5 }
    @{ Name = '05-terminal-abilitati';     Scene = 'Hub';              Spawn = 'hub_center';   Interact = 'SkillTerminal'; Command = '';          Delay = 2.5 }
    @{ Name = '06-terminal-magazin';       Scene = 'Hub';              Spawn = 'hub_center';   Interact = 'ShopTerminal';  Command = '';          Delay = 2.5 }
    @{ Name = '07-terminal-contracte';     Scene = 'Hub';              Spawn = 'hub_center';   Interact = 'JobBoard';      Command = '';          Delay = 2.5 }
    @{ Name = '08-terminal-misiuni';       Scene = 'Hub';              Spawn = 'hub_center';   Interact = '';              Command = 'quests';    Delay = 2.5 }
    @{ Name = '09-nivel1-parc';            Scene = 'Level01_Rooftops'; Spawn = 'l1_park';      Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '10-nivel1-securitate';      Scene = 'Level01_Rooftops'; Spawn = 'l1_security';  Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '11-hacking-pattern-memory'; Scene = 'Level01_Rooftops'; Spawn = 'l1_nodes';     Interact = 'Terminal_Door'; Command = '';          Delay = 3.2 }
    @{ Name = '12-hacking-port-scan';      Scene = 'Level01_Rooftops'; Spawn = 'l1_security';  Interact = 'Terminal_Camera'; Command = '';        Delay = 3 }
    @{ Name = '13-hacking-wire-connect';   Scene = 'Level01_Rooftops'; Spawn = 'l1_nodes';     Interact = 'Terminal_Data'; Command = '';          Delay = 3 }
    @{ Name = '14-nivel1-noduri';          Scene = 'Level01_Rooftops'; Spawn = 'l1_nodes';     Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '15-nivel1-drona';           Scene = 'Level01_Rooftops'; Spawn = 'l1_drone';     Interact = '';              Command = '';          Delay = 4 }
    @{ Name = '16-pauza';                  Scene = 'Hub';              Spawn = 'hub_center';   Interact = '';              Command = 'pause';     Delay = 2.5 }
    @{ Name = '17-panou-barem-f1';         Scene = 'Hub';              Spawn = 'hub_center';   Interact = '';              Command = 'rubric';    Delay = 2.5 }
    @{ Name = '18-game-over';              Scene = 'Hub';              Spawn = 'hub_center';   Interact = '';              Command = 'gameover';  Delay = 3 }
    @{ Name = '19-credite';                Scene = 'Credits';          Spawn = '';             Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '20-nivel1-start';           Scene = 'Level01_Rooftops'; Spawn = 'l1_start';     Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '21-nivel1-acoperisuri';     Scene = 'Level01_Rooftops'; Spawn = 'l1_movement';  Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '22-terminal-calatorie';     Scene = 'Hub';              Spawn = 'hub_center';   Interact = 'FastTravel_Levels'; Command = '';      Delay = 2.5 }
    @{ Name = '23-nivel2-hol';             Scene = 'Level02_Office';   Spawn = 'l2_start';     Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '24-nivel2-securitate';      Scene = 'Level02_Office';   Spawn = 'l2_security';  Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '25-nivel2-lift';            Scene = 'Level02_Office';   Spawn = 'l2_elevator';  Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '26-nivel2-etaj';            Scene = 'Level02_Office';   Spawn = 'l2_upper';     Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '27-nivel2-seif';            Scene = 'Level02_Office';   Spawn = 'l2_vault';     Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '28-nivel3-servere';         Scene = 'Level03_ServerFarm'; Spawn = 'l3_start';   Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '29-nivel3-noduri';          Scene = 'Level03_ServerFarm'; Spawn = 'l3_nodes';   Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '30-nivel4-apa';             Scene = 'Level04_Sewers';   Spawn = 'l4_water';     Interact = '';              Command = '';          Delay = 4 }
    @{ Name = '31-nivel4-stavila';         Scene = 'Level04_Sewers';   Spawn = 'l4_sluice';    Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '32-nivel5-sediu';           Scene = 'Level05_HQ';       Spawn = 'l5_start';     Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '33-nivel5-lift';            Scene = 'Level05_HQ';       Spawn = 'l5_elevator';  Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '34-cutscene-intro-titlu';   Scene = 'Intro';            Spawn = '';             Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '35-cutscene-intro';         Scene = 'Intro';            Spawn = '';             Interact = '';              Command = '';          Delay = 9 }
    @{ Name = '36-cutscene-final';         Scene = 'Finale';           Spawn = '';             Interact = '';              Command = '';          Delay = 11 }
    @{ Name = '37-meniu-coop';             Scene = '';                 Spawn = '';             Interact = '';              Command = 'coop';      Delay = 3 }
    @{ Name = '38-coop-ecran-impartit';    Scene = 'Level01_Rooftops'; Spawn = 'l1_security';  Interact = '';              Command = '';          Delay = 4; Coop = 'local' }
    @{ Name = '40-terminal-jurnale';       Scene = 'Hub';              Spawn = 'hub_center';   Interact = '';              Command = 'lore';      Delay = 2.5 }
    @{ Name = '41-nivel1-ventilatie';      Scene = 'Level01_Rooftops'; Spawn = 'l1_vent';      Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '42-nivel6-antena';          Scene = 'Level06_Spire';    Spawn = 'l6_start';     Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '43-nivel6-nacela';          Scene = 'Level06_Spire';    Spawn = 'l6_gondola';   Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '44-nivel6-ventilator';      Scene = 'Level06_Spire';    Spawn = 'l6_fans';      Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '45-nivel6-grilaje';         Scene = 'Level06_Spire';    Spawn = 'l6_grates';    Interact = '';              Command = '';          Delay = 3 }
    @{ Name = '46-nivel6-transmisie';      Scene = 'Level06_Spire';    Spawn = 'l6_transmitter'; Interact = '';              Command = '';          Delay = 3 }
)
for ($i = 1; $i -le 14; $i++) {
    $room = 'room_{0:D2}' -f $i
    $shots += @{ Name = ('barem-camera-{0:D2}' -f $i); Scene = 'RubricShowcase'; Spawn = $room; Interact = ''; Command = ''; Delay = 3 }
}

$selected = @($shots | Where-Object { $shot = $_; @($Only | Where-Object { $shot.Name -like $_ }).Count -gt 0 })
$failed = 0
foreach ($shot in $selected) {
    $png = Join-Path $OutDir "$($shot.Name).png"
    $log = Join-Path $logDir "$($shot.Name).log"
    Remove-Item $png, $log -ErrorAction SilentlyContinue
    $delay = [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, '{0}', $shot.Delay)
    $playerArgs = @('-screen-fullscreen', '0', '-screen-width', "$Width", '-screen-height', "$Height", '-logFile', "`"$log`"",
        '-ra-newgame', '-ra-default-settings', '-ra-screenshot', "`"$png`"", '-ra-screenshot-delay', $delay, '-ra-quit-after', "$([int]$shot.Delay + 5)")
    if ($shot.Scene) { $playerArgs += @('-ra-scene', $shot.Scene) }
    if ($shot.Spawn) { $playerArgs += @('-ra-spawn', $shot.Spawn) }
    if ($shot.Interact) { $playerArgs += @('-ra-interact', $shot.Interact) }
    if ($shot.ContainsKey('Coop')) { $playerArgs += @('-ra-coop', $shot.Coop) }
    if ($shot.Command) { $playerArgs += @('-ra-command', $shot.Command) }

    $process = Start-Process -FilePath $exe -ArgumentList ($playerArgs -join ' ') -PassThru
    if (-not $process.WaitForExit(([int]$shot.Delay + 40) * 1000)) { $process.Kill(); $failed++; Write-Host "[capture] $($shot.Name): TIMEOUT" -ForegroundColor Red; continue }
    $ok = Test-Path $png
    if (-not $ok) { $failed++ }
    $exceptions = @(if (Test-Path $log) { Select-String -Path $log -Pattern 'Exception' | ForEach-Object { $_.Line.Trim() } })
    $state = if ($ok -and $exceptions.Count -eq 0) { 'OK' } else { 'CHECK' }
    Write-Host ("[capture] {0,-28} {1}  exceptions={2}" -f $shot.Name, $state, $exceptions.Count)
    $exceptions | Select-Object -First 3 | ForEach-Object { Write-Host "    ! $_" }
}

Write-Host "[capture] $($selected.Count - $failed)/$($selected.Count) screenshots in $OutDir"
if ($failed -gt 0) { exit 1 }
exit 0
