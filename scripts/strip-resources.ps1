#Requires -Version 5.1
<#
.SYNOPSIS
    Strips unused embedded resources from osu.Game.Resources.dll in a publish folder,
    replacing it with a smaller version that still contains everything an editor needs.

.DESCRIPTION
    Removed categories and approximate savings:
      Tracks/*              Background music tracks        ~10.2 MB
      Samples/Multiplayer   Multiplayer sounds              ~7.0 MB
      Samples/Results       Results-screen sounds           ~5.7 MB
      Textures/Intro        Intro-screen textures           ~5.6 MB
      Skins/Retro           Classic (retro) skin            ~2.0 MB
      Samples/DailyChallenge                                ~0.9 MB
      Samples/Intro         Intro-screen sounds             ~0.6 MB
      Samples/MedalSplash                                   ~0.2 MB
      Textures/MedalSplash                                 ~0.03 MB
    Total: ~32 MB

    Requires .NET 9 SDK (uses PersistedAssemblyBuilder).

.PARAMETER PublishDir
    Path to the publish output folder. Defaults to UnbeatableStandaloneEditor\publish.

.EXAMPLE
    .\strip-resources.ps1
    .\strip-resources.ps1 -PublishDir "path\to\publish"
#>
param(
    [string]$PublishDir = (Join-Path (Split-Path $PSScriptRoot -Parent) "UnbeatableStandaloneEditor\publish")
)

$ErrorActionPreference = "Stop"

$toolProject = Join-Path $PSScriptRoot "..\tools\strip-resources\strip-resources.csproj"
$sourceDll   = Join-Path $PublishDir "osu.Game.Resources.dll"
$outputDll   = Join-Path $PublishDir "osu.Game.Resources.dll.stripped"
$backupDll   = Join-Path $PublishDir "osu.Game.Resources.dll.bak"

if (!(Test-Path $sourceDll)) {
    Write-Error "osu.Game.Resources.dll not found at: $sourceDll"
    exit 1
}

$originalSize = (Get-Item $sourceDll).Length
$originalSizeMB = [math]::Round($originalSize/(1024 * 1024),1)
Write-Host ("==> Stripping osu.Game.Resources.dll  ({0} MB)" -f $originalSizeMB) -ForegroundColor Cyan

Copy-Item $sourceDll $backupDll -Force

dotnet run --project $toolProject -c Release -- $sourceDll $outputDll

if ($LASTEXITCODE -ne 0) {
    Write-Host "Strip tool failed - restoring original" -ForegroundColor Red
    Remove-Item $outputDll -ErrorAction SilentlyContinue
    exit $LASTEXITCODE
}

Move-Item $outputDll $sourceDll -Force
Remove-Item $backupDll -Force

$newSize = (Get-Item $sourceDll).Length
$newSizeMB = [math]::Round($newSize/(1024 * 1024),1)
$saved   = ($originalSize - $newSize) / (1024 * 1024)
$savedMB = [math]::Round($saved,1)

Write-Host ""
Write-Host "==> osu.Game.Resources.dll stripped" -ForegroundColor Green
Write-Host ("    {0} MB  ->  {1} MB  (saved {2} MB)" -f $originalSizeMB, $newSizeMB, $savedMB)
