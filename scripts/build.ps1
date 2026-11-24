<#
.SYNOPSIS
    Builds the Windows x64 player (RootAccess/Builds/Windows/RootAccess.exe).
.PARAMETER Development
    Development build (profiler, script debugging).
.PARAMETER RebuildContent
    Regenerate assets, prefabs and scenes (SceneBuilder.BuildAll) before building.
#>
[CmdletBinding()]
param(
    [switch]$Development,
    [switch]$RebuildContent
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

if ($RebuildContent) {
    $content = Invoke-UnityBatch -Step 'build-content' -ExecuteMethod 'RootAccess.EditorTools.SceneBuilder.BuildAll'
    if ($content.ExitCode -ne 0 -or $content.Problems.Count -gt 0) { Write-Error 'Content build failed.'; exit 1 }
}

$extra = @()
if ($Development) { $extra += '-ra-development' }
$result = Invoke-UnityBatch -Step 'build-windows' -ExecuteMethod 'RootAccess.EditorTools.BuildScript.BuildWindows' -ExtraArgs $extra

$exe = Join-Path (Get-ProjectPath) 'Builds\Windows\RootAccess.exe'
if ($result.ExitCode -ne 0 -or $result.Problems.Count -gt 0 -or -not (Test-Path $exe)) {
    Write-Host '[build] FAILED. Last 60 log lines:' -ForegroundColor Red
    Get-Content $result.Log -Tail 60 | ForEach-Object { Write-Host "    $_" }
    exit 1
}

$folder = Split-Path $exe -Parent
$sizeMb = [math]::Round(((Get-ChildItem $folder -Recurse -File | Measure-Object Length -Sum).Sum) / 1MB, 1)
Write-Host "[build] OK -> $exe ($sizeMb MB build folder, $($result.Seconds)s)" -ForegroundColor Green
exit 0
