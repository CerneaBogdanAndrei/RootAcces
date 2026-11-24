# Shared helpers for the Root Access CLI scripts. Dot-source this file: . "$PSScriptRoot\common.ps1"

Set-StrictMode -Version Latest

$script:RepoRoot = Split-Path -Parent $PSScriptRoot
$script:ProjectPath = Join-Path $script:RepoRoot 'RootAccess'

function Get-RepoRoot { return $script:RepoRoot }

function Get-ProjectPath { return $script:ProjectPath }

function Get-ProjectEditorVersion {
    $versionFile = Join-Path $script:ProjectPath 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path $versionFile)) { throw "ProjectVersion.txt not found at $versionFile" }
    $line = Get-Content $versionFile | Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } | Select-Object -First 1
    if (-not $line) { throw "Could not read m_EditorVersion from $versionFile" }
    return ($line -replace '^m_EditorVersion:\s*', '').Trim()
}

function Get-UnityEditorPath {
    if ($env:UNITY_EDITOR_PATH -and (Test-Path $env:UNITY_EDITOR_PATH)) { return $env:UNITY_EDITOR_PATH }
    $version = Get-ProjectEditorVersion
    $candidate = Join-Path "C:\Program Files\Unity\Hub\Editor\$version" 'Editor\Unity.exe'
    if (Test-Path $candidate) { return $candidate }
    throw "Unity $version not found at $candidate. Install it (unity install $version) or set UNITY_EDITOR_PATH."
}

function Assert-ProjectClosed {
    $lock = Join-Path $script:ProjectPath 'Temp\UnityLockfile'
    if (Test-Path $lock) {
        try {
            $stream = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
            $stream.Close()
        }
        catch {
            throw "The project is open in the Unity Editor (Temp\UnityLockfile is locked). Close Unity and retry."
        }
    }
}

function Format-UnityArgument([string]$value) {
    if ($value -match '[\s"]') { return '"' + ($value -replace '"', '\"') + '"' }
    return $value
}

function Get-UnityLogProblems([string]$logPath) {
    if (-not (Test-Path $logPath)) { return @("Log file missing: $logPath") }
    $patterns = @(
        'error CS\d{4}',
        'Compilation failed',
        'Scripts have compiler errors',
        '^\s*[A-Za-z_.]*Exception(: |$)',
        'executeMethod method .* threw exception',
        '\[RootAccess\]\[FAIL\]'
    )
    $ignore = @(
        'Licensing',
        'UnityConnect',
        'Curl error',
        'Asset Pipeline Refresh'
    )
    $hits = Select-String -Path $logPath -Pattern $patterns | ForEach-Object { $_.Line.Trim() } |
        Where-Object { $line = $_; -not ($ignore | Where-Object { $line -match $_ }) } |
        Select-Object -Unique
    return @($hits)
}

function Invoke-UnityBatch {
    param(
        [Parameter(Mandatory = $true)][string]$Step,
        [string]$ExecuteMethod,
        [string[]]$ExtraArgs = @(),
        [switch]$WithGraphics,
        [switch]$NoQuit,
        [switch]$AllowProblems
    )
    $unity = Get-UnityEditorPath
    Assert-ProjectClosed
    $logDir = Join-Path $script:ProjectPath 'Logs'
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    $log = Join-Path $logDir "$Step.log"
    if (Test-Path $log) { Remove-Item $log -Force }

    $argList = @('-batchmode', '-projectPath', $script:ProjectPath, '-logFile', $log)
    if (-not $WithGraphics) { $argList += '-nographics' }
    if (-not $NoQuit) { $argList += '-quit' }
    if ($ExecuteMethod) { $argList += @('-executeMethod', $ExecuteMethod) }
    $argList += $ExtraArgs
    $argString = ($argList | ForEach-Object { Format-UnityArgument $_ }) -join ' '

    Write-Host "[unity] $Step -> $log"
    $started = Get-Date
    $process = Start-Process -FilePath $unity -ArgumentList $argString -Wait -PassThru -NoNewWindow
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    $problems = @(Get-UnityLogProblems $log)

    $result = [pscustomobject]@{
        Step     = $Step
        ExitCode = $process.ExitCode
        Seconds  = $elapsed
        Log      = $log
        Problems = $problems
    }
    Write-Host ("[unity] {0} finished: exit {1} in {2}s, {3} problem line(s)" -f $Step, $result.ExitCode, $elapsed, $problems.Count)
    if ($problems.Count -gt 0) { $problems | Select-Object -First 40 | ForEach-Object { Write-Host "  ! $_" } }
    if (-not $AllowProblems -and ($result.ExitCode -ne 0 -or $problems.Count -gt 0)) {
        Write-Host "[unity] Last 30 log lines:"
        Get-Content $log -Tail 30 | ForEach-Object { Write-Host "    $_" }
    }
    return $result
}
