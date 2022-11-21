#Requires -Version 7.0


# ================
# EXIT CODES
# ================
# 0  = NuGet package sources prepared successfully.
# 1  = Missing required build infrastructure.
# 2  = Invalid command-line argument.
# 3  = Invalid NuGet package source configuration.
# 4  = Failed to prepare local NuGet package source.



param(
    [string] $NuGetConfigFilePath = (Join-Path -Path (Split-Path -Parent $PSScriptRoot) -ChildPath 'nuget.config'),

    [string] $LocalPackageSourceKeyPrefix = 'Local Package Artifacts'
)

$ErrorActionPreference = 'Stop'



################################################################################
# Workflow Operations
################################################################################


function Test-PackageSourceIsDisabled {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [xml] $NuGetConfig,

        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $PackageSourceKey
    )

    $disabledPackageSources = @($NuGetConfig.configuration.disabledPackageSources.add)

    foreach ($disabledPackageSource in $disabledPackageSources) {
        $disabledPackageSourceKey = [string] $disabledPackageSource.key
        $disabledPackageSourceValue = [string] $disabledPackageSource.value

        if ($disabledPackageSourceKey -eq $PackageSourceKey -and
            $disabledPackageSourceValue -ieq 'true') {
            return $true
        }
    }

    return $false
}

function Resolve-LocalPackageSourceFolderPath {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $NuGetConfigFolderPath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $PackageSourceValue
    )

    $packageSourceUri = $null

    $isAbsoluteUri = `
        [System.Uri]::TryCreate(
            $PackageSourceValue,
            [System.UriKind]::Absolute,
            [ref] $packageSourceUri
        )

    if ($isAbsoluteUri -and $packageSourceUri.Scheme -ne 'file') {
        return $null
    }

    if ($isAbsoluteUri -and $packageSourceUri.Scheme -eq 'file') {
        return $packageSourceUri.LocalPath
    }

    if ([System.IO.Path]::IsPathRooted($PackageSourceValue)) {
        return [System.IO.Path]::GetFullPath($PackageSourceValue)
    }

    return (
        [System.IO.Path]::GetFullPath(
            [System.IO.Path]::Combine($NuGetConfigFolderPath, $PackageSourceValue)
        )
    )
}

function Test-PackageSourceKeyMatchesPrefix {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $PackageSourceKey,

        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $PackageSourceKeyPrefix
    )

    return (
        $PackageSourceKey -eq $PackageSourceKeyPrefix -or
        $PackageSourceKey.StartsWith("$PackageSourceKeyPrefix (", [System.StringComparison]::Ordinal)
    )
}

function Test-HasValue {
    param(
        [AllowNull()]
        [object] $Value
    )

    if ($null -eq $Value) {
        return $false
    }

    if ($Value -is [string]) {
        return -not [string]::IsNullOrWhiteSpace($Value)
    }

    return $true
}



################################################################################
# Main
################################################################################


Write-Host


if (-not (Test-HasValue $NuGetConfigFilePath)) {
    Write-Host 'NuGet configuration (nuget.config) file path must not be empty.'
    Write-Host
    exit 2
}

if (-not (Test-HasValue $LocalPackageSourceKeyPrefix)) {
    Write-Host 'Local package source key prefix must not be empty.'
    Write-Host
    exit 2
}

if (-not (Test-Path -LiteralPath $NuGetConfigFilePath -PathType Leaf)) {
    Write-Host "Missing nuget.config file '$NuGetConfigFilePath'."
    Write-Host
    exit 1
}


$nuGetConfigFolderPath = Split-Path -Parent $NuGetConfigFilePath

[xml] $nuGetConfig = Get-Content -LiteralPath $NuGetConfigFilePath

$packageSources = @($nuGetConfig.configuration.packageSources.add)

Write-Host 'NuGet configuration (nuget.config) file path:'
Write-Host "  $NuGetConfigFilePath"
Write-Host 'Local package source key prefix:'
Write-Host "  $LocalPackageSourceKeyPrefix"
Write-Host

$disabledPackageSources = @()
$invalidPackageSources = @()
$skippedPackageSources = @()
$resolvedLocalPackageSources = @()

foreach ($packageSource in $packageSources) {
    $packageSourceKey = [string] $packageSource.key
    $packageSourceValue = [string] $packageSource.value

    if (Test-PackageSourceIsDisabled `
            -NuGetConfig $nuGetConfig `
            -PackageSourceKey $packageSourceKey) {
        $disabledPackageSources += $packageSource

        continue
    }

    if (-not (Test-HasValue $packageSourceValue)) {
        $invalidPackageSources += $packageSource

        continue
    }

    $packageSourceFolderPath = `
        Resolve-LocalPackageSourceFolderPath `
            -NuGetConfigFolderPath $nuGetConfigFolderPath `
            -PackageSourceValue $packageSourceValue

    if ($null -ne $packageSourceFolderPath) {
        $resolvedLocalPackageSources += [PSCustomObject] @{
            PackageSource = $packageSource
            FolderPath    = $packageSourceFolderPath
        }
    }
    else {
        $skippedPackageSources += $packageSource
    }
}

if ($disabledPackageSources.Count -gt 0) {
    Write-Host 'Disabled NuGet package source(s):'

    foreach ($packageSource in $disabledPackageSources) {
        Write-Host "  $([string] $packageSource.key) →"
        Write-Host "    $([string] $packageSource.value)"
    }

    Write-Host
}

if ($invalidPackageSources.Count -gt 0) {
    Write-Host 'Invalid NuGet package source(s):'

    foreach ($packageSource in $invalidPackageSources) {
        Write-Host "  $([string] $packageSource.key)"
    }

    Write-Host
}

if ($skippedPackageSources.Count -gt 0) {
    Write-Host 'Skipped non-local NuGet package source(s):'

    foreach ($packageSource in $skippedPackageSources) {
        Write-Host "  $([string] $packageSource.key) →"
        Write-Host "    $([string] $packageSource.value)"
    }

    Write-Host
}

$matchingLocalPackageSources = @(
    $resolvedLocalPackageSources |
    Where-Object {
        Test-PackageSourceKeyMatchesPrefix `
            -PackageSourceKey ([string] $_.PackageSource.key) `
            -PackageSourceKeyPrefix $LocalPackageSourceKeyPrefix
    }
)

if ($matchingLocalPackageSources.Count -eq 0) {
    if ($resolvedLocalPackageSources.Count -gt 0) {
        Write-Host 'No enabled local NuGet package sources with such key prefix exist.'
        Write-Host
        exit 3
    }
    else {
        Write-Host 'No enabled local NuGet package sources found.'
        Write-Host 'Nothing to prepare.'
        Write-Host
        exit 0
    }
}

Write-Host "Found $($matchingLocalPackageSources.Count) enabled local NuGet package source(s) with such key prefix."
Write-Host

$preparedLocalPackageSources = @()

foreach ($localPackageSource in $matchingLocalPackageSources) {
    $packageSource = $localPackageSource.PackageSource
    $packageSourceKey = [string] $packageSource.key
    $packageSourceFolderPath = $localPackageSource.FolderPath

    if (Test-Path -LiteralPath $packageSourceFolderPath -PathType Leaf) {
        Write-Host "NuGet package source '$packageSourceKey' resolves to an existing file: $packageSourceFolderPath"
        Write-Host
        exit 4
    }

    if (-not (Test-Path -LiteralPath $packageSourceFolderPath -PathType Container)) {
        New-Item -ItemType Directory -Path $packageSourceFolderPath -Force | Out-Null
    }

    $preparedLocalPackageSources += $localPackageSource
}

Write-Host 'Prepared local NuGet package source(s):'

foreach ($localPackageSource in $preparedLocalPackageSources) {
    $packageSource = $localPackageSource.PackageSource
    $packageSourceKey = [string] $packageSource.key
    $packageSourceFolderPath = $localPackageSource.FolderPath

    Write-Host "  $packageSourceKey →"
    Write-Host "    $packageSourceFolderPath"
}

Write-Host


exit 0
