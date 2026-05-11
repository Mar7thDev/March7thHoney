param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Framework = "net9.0",
    [string]$OutputRoot = "Publish",
    [string]$Mode = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "Program/Program.csproj"

if (-not (Test-Path $projectPath)) {
    throw "Cannot find project: $projectPath"
}

$outputDir = Join-Path $scriptDir "$OutputRoot/$Runtime"
if (Test-Path $outputDir) {
    Remove-Item -LiteralPath $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir | Out-Null

$isPublic = $false
if ([string]::IsNullOrWhiteSpace($Mode)) {
    Write-Host "Select publish mode:"
    Write-Host "  1) Private mode"
    Write-Host "  2) Public mode"
    $choice = Read-Host "Enter 1 or 2"
    $Mode = $choice
}

switch ($Mode.Trim().ToLowerInvariant()) {
    "1" { $isPublic = $false }
    "private" { $isPublic = $false }
    "2" { $isPublic = $true }
    "public" { $isPublic = $true }
    default { throw "Invalid mode: $Mode. Use 1/2 or private/public." }
}

$publishArgs = @(
    "publish"
    $projectPath
    "-c", $Configuration
    "-f", $Framework
    "-r", $Runtime
    "--self-contained", "true"
    "-o", $outputDir
    "/p:PublishSingleFile=true"
    "/p:IncludeNativeLibrariesForSelfExtract=true"
    "/p:EnableCompressionInSingleFile=true"
    "/p:PublishReadyToRun=true"
    "/p:PublishTrimmed=false"
)

if ($isPublic) {
    $publishArgs += "/p:DefineConstants=PUBLIC_MODE"
}

Write-Host "Publishing single-file self-contained build..."
Write-Host "Project: $projectPath"
Write-Host "Runtime: $Runtime"
Write-Host "Configuration: $Configuration"
Write-Host "Framework: $Framework"
Write-Host "Output: $outputDir"
if ($isPublic) {
    Write-Host "Mode: PUBLIC_MODE"
} else {
    Write-Host "Mode: PRIVATE"
}

dotnet @publishArgs

$repoConfigDir = Join-Path $scriptDir "Config"
$publishConfigDir = Join-Path $outputDir "Config"
$repoCommonSecurityDir = Join-Path $scriptDir "Common/Security"
$repoCommonUtilSecurityDir = Join-Path $scriptDir "Common/Util/Security"
$publishCommonDir = Join-Path $outputDir "Common"
$publishCommonSecurityDir = Join-Path $publishCommonDir "Security"

Get-ChildItem -Path $outputDir -Filter "*.pdb" -File -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -Path $outputDir -Filter "appsettings*.json" -File -ErrorAction SilentlyContinue | Remove-Item -Force

if (Test-Path $publishConfigDir) {
    Remove-Item -LiteralPath $publishConfigDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishConfigDir | Out-Null

if (Test-Path $repoConfigDir) {
    $resourceCachePath = Join-Path $repoConfigDir "Resource.cache"
    if (Test-Path $resourceCachePath) {
        Copy-Item -Path $resourceCachePath -Destination (Join-Path $publishConfigDir "Resource.cache") -Force
    }

    $jsonDir = Join-Path $repoConfigDir "Json"
    if (Test-Path $jsonDir) {
        Copy-Item -Path $jsonDir -Destination (Join-Path $publishConfigDir "Json") -Recurse
    }

    $handbookDir = Join-Path $repoConfigDir "Handbook"
    if (Test-Path $handbookDir) {
        Copy-Item -Path $handbookDir -Destination (Join-Path $publishConfigDir "Handbook") -Recurse
    }

    $securityDir = Join-Path $repoConfigDir "Security"
    if (Test-Path $securityDir) {
        Copy-Item -Path $securityDir -Destination (Join-Path $publishConfigDir "Security") -Recurse
    }
}

if (Test-Path $publishCommonSecurityDir) {
    Remove-Item -LiteralPath $publishCommonSecurityDir -Recurse -Force
}

$securitySourceDir = $null
if (Test-Path $repoCommonSecurityDir) {
    $securitySourceDir = $repoCommonSecurityDir
} elseif (Test-Path $repoCommonUtilSecurityDir) {
    $securitySourceDir = $repoCommonUtilSecurityDir
}

if ($securitySourceDir) {
    if (-not (Test-Path $publishCommonDir)) {
        New-Item -ItemType Directory -Path $publishCommonDir | Out-Null
    }

    Copy-Item -Path $securitySourceDir -Destination $publishCommonSecurityDir -Recurse
}

Write-Host ""
Write-Host "Publish completed."
Write-Host "Output directory: $outputDir"
