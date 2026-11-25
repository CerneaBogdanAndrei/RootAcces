<#
.SYNOPSIS
    Creates the two Google Drive deliverables in dist/:
      RootAccess_Project.zip  - the whole repository (Unity project, scripts, docs) without generated folders
                                (Library, Temp, Obj, Logs, UserSettings, Builds, dist, .git, IDE files)
      RootAccess_Windows.zip  - the Windows build folder (RootAccess/RootAccess.exe + data), ready to unzip and play
.PARAMETER Build
    Run scripts/build.ps1 first (otherwise the existing build is packaged).
.PARAMETER IncludeGit
    Also put the .git folder in the project zip (full history; much larger).
.EXAMPLE
    ./scripts/package-for-drive.ps1 -Build
#>
[CmdletBinding()]
param(
    [switch]$Build,
    [switch]$IncludeGit
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = Get-RepoRoot
$projectPath = Get-ProjectPath
$dist = Join-Path $repoRoot 'dist'
$buildFolder = Join-Path $projectPath 'Builds\Windows'
$exe = Join-Path $buildFolder 'RootAccess.exe'

if ($Build) {
    & (Join-Path $PSScriptRoot 'build.ps1')
    if ($LASTEXITCODE -ne 0) { Write-Error 'Build failed; nothing packaged.'; exit 1 }
}
if (-not (Test-Path $exe)) { Write-Error "Build not found at $exe. Run scripts/build.ps1 (or pass -Build)."; exit 1 }
Assert-ProjectClosed

$excludedDirs = @(
    (Join-Path $repoRoot 'dist'), (Join-Path $repoRoot '_logs'), (Join-Path $repoRoot '_staging'),
    (Join-Path $projectPath 'Library'), (Join-Path $projectPath 'Temp'), (Join-Path $projectPath 'Obj'),
    (Join-Path $projectPath 'Logs'), (Join-Path $projectPath 'UserSettings'), (Join-Path $projectPath 'Builds'),
    (Join-Path $projectPath 'MemoryCaptures'), (Join-Path $projectPath 'Recordings')
)
if (-not $IncludeGit) { $excludedDirs += (Join-Path $repoRoot '.git') }
$excludedDirNames = @('.vs', '.idea')
$excludedFilePatterns = @('*.csproj', '*.sln', '*.slnx', '*.suo', '*.user', '*.userprefs', 'Thumbs.db', '.DS_Store')

function Get-PackFiles([string]$root, [string[]]$skipDirs, [string[]]$skipDirNames, [string[]]$skipPatterns) {
    $skip = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($dir in $skipDirs) { [void]$skip.Add([System.IO.Path]::GetFullPath($dir).TrimEnd('\')) }
    $files = New-Object 'System.Collections.Generic.List[string]'
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $pending.Push([System.IO.Path]::GetFullPath($root).TrimEnd('\'))
    while ($pending.Count -gt 0) {
        $dir = $pending.Pop()
        foreach ($sub in [System.IO.Directory]::EnumerateDirectories($dir)) {
            $name = [System.IO.Path]::GetFileName($sub)
            if ($skip.Contains($sub) -or $skipDirNames -contains $name) { continue }
            $pending.Push($sub)
        }
        foreach ($file in [System.IO.Directory]::EnumerateFiles($dir)) {
            $name = [System.IO.Path]::GetFileName($file)
            $excluded = $false
            foreach ($pattern in $skipPatterns) { if ($name -like $pattern) { $excluded = $true; break } }
            if (-not $excluded) { $files.Add($file) }
        }
    }
    return $files
}

function New-Zip([string]$zipPath, [string]$baseDir, $files, [string]$prefix) {
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    $base = [System.IO.Path]::GetFullPath($baseDir).TrimEnd('\')
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            $relative = $file.Substring($base.Length).TrimStart('\') -replace '\\', '/'
            if ($prefix) { $relative = "$prefix/$relative" }
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file, $relative, [System.IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally {
        $zip.Dispose()
    }
}

function Test-Zip([string]$zipPath, [string]$mustContain) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $count = $zip.Entries.Count
        $found = @($zip.Entries | Where-Object { $_.FullName -eq $mustContain }).Count -eq 1
    }
    finally {
        $zip.Dispose()
    }
    if (-not $found) { throw "$zipPath does not contain $mustContain" }
    return $count
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null

Write-Host '[package] Collecting project files...'
$projectFiles = Get-PackFiles $repoRoot $excludedDirs $excludedDirNames $excludedFilePatterns
$projectZip = Join-Path $dist 'RootAccess_Project.zip'
New-Zip $projectZip $repoRoot $projectFiles 'RootAccess_Project'
$projectEntries = Test-Zip $projectZip 'RootAccess_Project/RootAccess/ProjectSettings/ProjectVersion.txt'

Write-Host '[package] Collecting build files...'
$buildFiles = Get-PackFiles $buildFolder @() @() @() | Where-Object { $_ -notmatch '_BurstDebugInformation_DoNotShip' }
$windowsZip = Join-Path $dist 'RootAccess_Windows.zip'
New-Zip $windowsZip $buildFolder $buildFiles 'RootAccess'
$windowsEntries = Test-Zip $windowsZip 'RootAccess/RootAccess.exe'

foreach ($item in @(@($projectZip, $projectEntries), @($windowsZip, $windowsEntries))) {
    $sizeMb = [math]::Round((Get-Item $item[0]).Length / 1MB, 1)
    Write-Host ("[package] {0} ({1} MB, {2} files)" -f $item[0], $sizeMb, $item[1]) -ForegroundColor Green
}
Write-Host '[package] Upload both zips to Google Drive. Players unzip RootAccess_Windows.zip and run RootAccess\RootAccess.exe.'
exit 0
