[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'AionSpeedrunOverlay\AionSpeedrunOverlay.csproj'
[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$versionNode = $project.SelectSingleNode('/Project/PropertyGroup/Version')

if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw 'The project version is missing.'
}

$version = $versionNode.InnerText.Trim()
if ($version -notmatch '^[0-9A-Za-z][0-9A-Za-z.-]+$') {
    throw "The project version is not safe for a package name: $version"
}

$packageName = "AionSpeedrunOverlay-$version-win-x64"
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'artifacts'))
$publishDirectory = [IO.Path]::GetFullPath((Join-Path $artifactsRoot $packageName))
$archivePath = [IO.Path]::GetFullPath((Join-Path $artifactsRoot "$packageName.zip"))
$archiveChecksumPath = "$archivePath.sha256"

if (-not $publishDirectory.StartsWith(
        $artifactsRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The publish directory resolved outside the artifacts directory.'
}

$requiredRepositoryFiles = @(
    'README.md',
    'PRIVACY.md',
    'CHANGELOG.md',
    'THIRD_PARTY_NOTICES.md',
    'LICENSE',
    'licenses\Apache-2.0.txt',
    'licenses\MIT.txt',
    'licenses\Leptonica.txt'
)

foreach ($relativePath in $requiredRepositoryFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $relativePath))) {
        throw "Required release file is missing: $relativePath"
    }
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

foreach ($path in @($archivePath, $archiveChecksumPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:SatelliteResourceLanguages=en `
    -p:DebugType=None

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Get-ChildItem -LiteralPath $publishDirectory -Recurse -File `
    -Filter 'opencv_videoio_ffmpeg*.dll' |
    Remove-Item -Force

foreach ($relativePath in @(
        'README.md',
        'PRIVACY.md',
        'CHANGELOG.md',
        'THIRD_PARTY_NOTICES.md',
        'LICENSE')) {
    Copy-Item `
        -LiteralPath (Join-Path $PSScriptRoot $relativePath) `
        -Destination $publishDirectory
}

Copy-Item `
    -LiteralPath (Join-Path $PSScriptRoot 'licenses') `
    -Destination $publishDirectory `
    -Recurse

Set-Content -LiteralPath (Join-Path $publishDirectory 'VERSION.txt') -Value $version

$requiredPublishedFiles = @(
    'AionSpeedrunOverlay.exe',
    'Tesseract.dll',
    'x64\leptonica-1.82.0.dll',
    'x64\tesseract50.dll',
    'Data\aion2_asmodian_mythic_quests_lvl1-45.json',
    'Data\level-less-contexts.json',
    'tessdata\eng.traineddata',
    'README.md',
    'PRIVACY.md',
    'THIRD_PARTY_NOTICES.md',
    'LICENSE'
)

foreach ($relativePath in $requiredPublishedFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishDirectory $relativePath))) {
        throw "Published file is missing: $relativePath"
    }
}

$forbiddenNames = @(
    'Collect-Test-Results.ps1',
    'TEAM_TESTING.md',
    'TEST_REPORT.txt',
    'notes-overrides.json',
    'personal-best.json'
)

$forbiddenPublishedFiles = Get-ChildItem -LiteralPath $publishDirectory -Recurse -File |
    Where-Object {
        $_.Extension -eq '.pdb' -or
        $_.Name -in $forbiddenNames -or
        $_.Name -like 'opencv_videoio_ffmpeg*.dll'
    }

if ($forbiddenPublishedFiles) {
    throw "Forbidden release files found: $($forbiddenPublishedFiles.FullName -join ', ')"
}

$manifestPath = Join-Path $publishDirectory 'SHA256SUMS.txt'
$manifestLines = Get-ChildItem -LiteralPath $publishDirectory -Recurse -File |
    Where-Object { $_.FullName -ne $manifestPath } |
    Sort-Object FullName |
    ForEach-Object {
        $relativePath = [IO.Path]::GetRelativePath(
            $publishDirectory,
            $_.FullName).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $relativePath"
    }

Set-Content -LiteralPath $manifestPath -Value $manifestLines
Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $archivePath

$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content `
    -LiteralPath $archiveChecksumPath `
    -Value "$archiveHash  $([IO.Path]::GetFileName($archivePath))"

Write-Host "Release package created: $archivePath"
Write-Host "SHA-256 checksum: $archiveChecksumPath"
