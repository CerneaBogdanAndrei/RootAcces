<#
.SYNOPSIS
    Runs the Unity Editor in batchmode against RootAccess and validates the log.
.EXAMPLE
    ./scripts/unity-batch.ps1 -Step 10-setup -ExecuteMethod RootAccess.EditorTools.ProjectSetup.Run
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Step,
    [string]$ExecuteMethod,
    [string[]]$ExtraArgs = @(),
    [switch]$WithGraphics,
    [switch]$NoQuit
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$result = Invoke-UnityBatch -Step $Step -ExecuteMethod $ExecuteMethod -ExtraArgs $ExtraArgs -WithGraphics:$WithGraphics -NoQuit:$NoQuit
if ($result.ExitCode -ne 0 -or $result.Problems.Count -gt 0) { exit 1 }
exit 0
