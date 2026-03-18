#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes UbStandaloneEditor as a self-contained single executable with native libraries embedded.

.PARAMETER Runtime
    Target runtime identifier. Defaults to win-x64.
    Examples: win-x64, linux-x64, osx-x64, osx-arm64

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.PARAMETER Version
    Override version number for the build (e.g., "0.1.2").
    If not provided, uses version from .csproj.

.PARAMETER StripResources
    After publishing, strip unused embedded resources from osu.Game.Resources.dll
    (background music, multiplayer/results/intro sounds & textures, retro skin).
    Saves ~30+ MB. Requires .NET 9 SDK.

.EXAMPLE
    .\publish.ps1
    .\publish.ps1 -StripResources
    .\publish.ps1 -Runtime linux-x64
    .\publish.ps1 -Version 0.1.2
    .\publish.ps1 -Runtime win-x64 -Configuration Debug
#>
param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$Version = ""
)

$StripResources = $true

$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path $PSScriptRoot -Parent
$project    = Join-Path $repoRoot "UnbeatableStandaloneEditor\UnbeatableStandaloneEditor.csproj"
$outputDir  = Join-Path $repoRoot "UnbeatableStandaloneEditor\publish"

# Default to version from .csproj if not provided
if (-not $Version) {
    [xml]$csproj = Get-Content $project
    $Version = $csproj.Project.PropertyGroup.Version
}

Write-Host "==> Building UnbeatableStandaloneEditor" -ForegroundColor Cyan
Write-Host "    Project      : $project"
Write-Host "    Runtime      : $Runtime"
Write-Host "    Configuration: $Configuration"
Write-Host "    Version      : $Version"
Write-Host "    StripResources: $StripResources"
Write-Host ""

dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $outputDir `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=embedded `
    "-p:Version=$Version"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Error "Publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# --- Post-publish cleanup ---

Write-Host ""
Write-Host "==> Post-publish cleanup..." -ForegroundColor Cyan


# Remove any leftover locale satellite-assembly folders not caught by SatelliteResourceLanguages
$localeDirs = Get-ChildItem $outputDir -Directory |
    Where-Object { $_.Name -notmatch '^(runtimes|ref)$' }
foreach ($dir in $localeDirs) {
    Remove-Item $dir.FullName -Recurse -Force
    Write-Host "    Removed locale dir: $($dir.Name)"
}

# Remove stbi.lib - static import library, not loaded at runtime
$stbiLib = Join-Path $outputDir "stbi.lib"
if (Test-Path $stbiLib) {
    Remove-Item $stbiLib -Force
    Write-Host "    Removed stbi.lib"
}

# --- Optional resource stripping ---

if ($StripResources) {
    Write-Host ""
    & (Join-Path $PSScriptRoot "strip-resources.ps1") -PublishDir $outputDir
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# --- Copy CREDITS ---

$creditsSource = Join-Path $repoRoot "CREDITS"
$creditsDest   = Join-Path $outputDir "CREDITS"
if (Test-Path $creditsSource) {
    Copy-Item $creditsSource $creditsDest -Force
    Write-Host "    Copied CREDITS"
} else {
    Write-Warning "CREDITS file not found at $creditsSource -- skipping."
}

# --- Summary ---

$totalSize = (Get-ChildItem $outputDir -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host ""
Write-Host "==> Published successfully to:" -ForegroundColor Green
Write-Host "    $outputDir"
$totalSizeMB = [math]::Round($totalSize / (1024 * 1024), 1)
Write-Host "    Total size: $totalSizeMB MB"

$exe = Get-ChildItem $outputDir -Filter "UnbeatableStandaloneEditor*" -File |
       Where-Object { $_.Extension -in @(".exe", "") -or $_.Name -notmatch "\." } |
       Select-Object -First 1

if ($exe) {
    $exeSizeMB = [math]::Round($exe.Length / (1024 * 1024), 1)
    Write-Host "    Executable: $($exe.Name)  ($exeSizeMB MB)"
}
