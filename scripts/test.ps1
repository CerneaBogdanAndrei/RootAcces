<#
.SYNOPSIS
    Runs the EditMode and/or PlayMode test suites in batchmode and prints a summary parsed from the NUnit XML.
.PARAMETER Platform
    EditMode, PlayMode or All (default).
.PARAMETER Filter
    Optional Unity -testFilter (semicolon-separated names or a regex), e.g. "AStarPathfinderTests".
.PARAMETER NoGraphics
    Run PlayMode without a graphics device (faster; URP lights/shaders are not exercised).
.EXAMPLE
    ./scripts/test.ps1
    ./scripts/test.ps1 -Platform EditMode -Filter QuestLogTests
#>
[CmdletBinding()]
param(
    [ValidateSet('EditMode', 'PlayMode', 'All')][string]$Platform = 'All',
    [string]$Filter,
    [switch]$NoGraphics
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$resultsDir = Join-Path (Get-ProjectPath) 'Logs\TestResults'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$platforms = if ($Platform -eq 'All') { @('EditMode', 'PlayMode') } else { @($Platform) }

function Read-TestResults([string]$xmlPath) {
    [xml]$xml = Get-Content -Raw -Path $xmlPath
    $run = $xml.'test-run'
    $failures = @($xml.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
            $message = if ($_.failure -and $_.failure.message) { $_.failure.message.InnerText.Trim() } else { '' }
            [pscustomobject]@{ Name = $_.fullname; Message = ($message -split "`n" | Select-Object -First 3) -join ' | ' }
        })
    return [pscustomobject]@{
        Total    = [int]$run.total
        Passed   = [int]$run.passed
        Failed   = [int]$run.failed
        Skipped  = [int]$run.skipped
        Seconds  = [math]::Round([double]$run.duration, 1)
        Failures = $failures
    }
}

$summary = @()
$exitCode = 0
foreach ($p in $platforms) {
    $xml = Join-Path $resultsDir "$p.xml"
    if (Test-Path $xml) { Remove-Item $xml -Force }
    $extra = @('-runTests', '-testPlatform', $p, '-testResults', $xml)
    if ($Filter) { $extra += @('-testFilter', $Filter) }
    $withGraphics = ($p -eq 'PlayMode') -and -not $NoGraphics

    # -runTests quits by itself; expected exceptions inside tests are logged, so the XML is the source of truth.
    $run = Invoke-UnityBatch -Step "test-$($p.ToLowerInvariant())" -ExtraArgs $extra -NoQuit -WithGraphics:$withGraphics -AllowProblems
    $compileErrors = @($run.Problems | Where-Object { $_ -match 'error CS\d{4}|Compilation failed|compiler errors' })

    if (-not (Test-Path $xml)) {
        Write-Host "[test] $p : no results file (exit $($run.ExitCode)). Log: $($run.Log)" -ForegroundColor Red
        $compileErrors | Select-Object -First 20 | ForEach-Object { Write-Host "    ! $_" }
        Get-Content $run.Log -Tail 40 | ForEach-Object { Write-Host "    $_" }
        $summary += [pscustomobject]@{ Platform = $p; Total = 0; Passed = 0; Failed = 0; Skipped = 0; Seconds = 0; Status = 'NO RESULTS' }
        $exitCode = 1
        continue
    }

    $result = Read-TestResults $xml
    $status = if ($result.Failed -eq 0 -and $result.Total -gt 0 -and $compileErrors.Count -eq 0) { 'PASS' } else { 'FAIL' }
    if ($status -ne 'PASS') { $exitCode = 1 }
    $summary += [pscustomobject]@{
        Platform = $p; Total = $result.Total; Passed = $result.Passed; Failed = $result.Failed
        Skipped = $result.Skipped; Seconds = $result.Seconds; Status = $status
    }
    foreach ($failure in $result.Failures) {
        Write-Host "[test] FAILED $($failure.Name)" -ForegroundColor Red
        if ($failure.Message) { Write-Host "       $($failure.Message)" }
    }
}

Write-Host ''
$summary | Format-Table -AutoSize | Out-String | Write-Host
Write-Host "[test] Results: $resultsDir"
exit $exitCode
