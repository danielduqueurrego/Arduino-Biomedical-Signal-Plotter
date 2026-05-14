[CmdletBinding()]
param(
    [string]$Version = 'v0.1.0',
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$packageName = "Biomedical-Instrumentation-Signal-Plotter-$Version-$Runtime"
$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
$appProject = Join-Path $repoRoot 'src\BiomedicalSignalPlotter\BiomedicalSignalPlotter.csproj'
$artifactsRoot = Join-Path $repoRoot 'artifacts'
$releaseRoot = Join-Path $artifactsRoot $packageName
$publishDir = Join-Path $releaseRoot 'app'
$zipPath = Join-Path $artifactsRoot "$packageName.zip"

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host ''
    Write-Host "> $FileName $($Arguments -join ' ')"
    & $FileName @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FileName $($Arguments -join ' ')"
    }
}

function Copy-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Recurse
}

if (-not (Test-Path -LiteralPath $appProject)) {
    throw "App project not found: $appProject"
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

if (Test-Path -LiteralPath $releaseRoot) {
    Remove-Item -LiteralPath $releaseRoot -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null

Push-Location $repoRoot
try {
    Invoke-CheckedCommand -FileName 'dotnet' -Arguments @('restore')
    Invoke-CheckedCommand -FileName 'dotnet' -Arguments @('build', '--configuration', $Configuration, '--no-restore')
    Invoke-CheckedCommand -FileName 'dotnet' -Arguments @('test', '--configuration', $Configuration, '--no-build')
    Invoke-CheckedCommand -FileName 'dotnet' -Arguments @(
        'publish',
        $appProject,
        '--configuration',
        $Configuration,
        '--runtime',
        $Runtime,
        '--self-contained',
        'true',
        '--output',
        $publishDir,
        '/p:PublishSingleFile=true',
        '/p:IncludeNativeLibrariesForSelfExtract=true',
        '/p:EnableCompressionInSingleFile=true'
    )
}
finally {
    Pop-Location
}

$firmwareSource = Join-Path $repoRoot 'firmware'
if (Test-Path -LiteralPath $firmwareSource) {
    Copy-Directory -Source $firmwareSource -Destination (Join-Path $releaseRoot 'firmware')
}

$appAssetsSource = Join-Path $repoRoot 'src\BiomedicalSignalPlotter\Assets'
if (Test-Path -LiteralPath $appAssetsSource) {
    Copy-Directory -Source $appAssetsSource -Destination (Join-Path $publishDir 'Assets')
}

$docsSource = Join-Path $repoRoot 'docs'
if (Test-Path -LiteralPath $docsSource) {
    Copy-Directory -Source $docsSource -Destination (Join-Path $releaseRoot 'docs')
}

$readmeSource = Join-Path $repoRoot 'README.md'
if (Test-Path -LiteralPath $readmeSource) {
    Copy-Item -LiteralPath $readmeSource -Destination $releaseRoot
}

$scriptsDestination = Join-Path $releaseRoot 'scripts'
New-Item -ItemType Directory -Path $scriptsDestination -Force | Out-Null

$uploadScriptSource = Join-Path $repoRoot 'scripts\upload-uno-r4-wifi.ps1'
if (Test-Path -LiteralPath $uploadScriptSource) {
    Copy-Item -LiteralPath $uploadScriptSource -Destination $scriptsDestination
}

$licenseNames = @('LICENSE', 'LICENSE.md', 'LICENSE.txt')
foreach ($licenseName in $licenseNames) {
    $licenseSource = Join-Path $repoRoot $licenseName
    if (Test-Path -LiteralPath $licenseSource) {
        Copy-Item -LiteralPath $licenseSource -Destination $releaseRoot
    }
}

Compress-Archive -LiteralPath $releaseRoot -DestinationPath $zipPath -CompressionLevel Optimal -Force

Write-Host ''
Write-Host "Release folder: $releaseRoot"
Write-Host "Release ZIP:    $zipPath"
Write-Host 'Windows package complete.'
