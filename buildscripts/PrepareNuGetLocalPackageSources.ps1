#Requires -Version 7.0


# ================
# EXIT CODES
# ================
# 0  = NuGet package sources prepared successfully.
# 1  = Missing required build infrastructure.
# 2  = Invalid command-line argument.
# 3  = No enabled local package source matches the configured key prefix.
# 4  = Failed to prepare local NuGet package source.



param(
    [string] $NuGetConfigFilePath = (Join-Path -Path (Split-Path -Parent $PSScriptRoot) -ChildPath 'nuget.config'),

    [string] $LocalPackageSourceKeyPrefix = 'Local Package Artifacts',

    [System.StringComparison] $LocalPackageSourceKeyPrefixComparison = [System.StringComparison]::OrdinalIgnoreCase
)

$ErrorActionPreference = 'Stop'



################################################################################
# Workflow Operations
################################################################################


function Test-PackageSourceIsDisabled {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [PSCustomObject[]] $DisabledPackageSources,

        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $PackageSourceKey
    )

    foreach ($packageSource in $DisabledPackageSources) {
        if ($packageSource.Key -eq $PackageSourceKey -and
            $packageSource.Value -ieq 'true') {
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
        [string] $PackageSourceKeyPrefix,

        [Parameter(Mandatory)]
        [System.StringComparison] $PackageSourceKeyPrefixComparison
    )

    return (
        [string]::Equals($PackageSourceKey, $PackageSourceKeyPrefix, $PackageSourceKeyPrefixComparison) -or
        $PackageSourceKey.StartsWith("$PackageSourceKeyPrefix (", $PackageSourceKeyPrefixComparison)
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

if (-not (Test-Path -LiteralPath $NuGetConfigFilePath -PathType Leaf)) {
    Write-Host "NuGet configuration (nuget.config) file not found: `"$NuGetConfigFilePath`"."
    Write-Host

    exit 1
}

if (-not (Test-HasValue $LocalPackageSourceKeyPrefix)) {
    Write-Host 'Local package source key prefix must not be empty.'
    Write-Host

    exit 2
}


$NuGetConfigFolderPath = Split-Path -Parent $NuGetConfigFilePath

[xml] $NuGetConfig = Get-Content -LiteralPath $NuGetConfigFilePath

$PackageSources = @(
    $NuGetConfig.configuration.packageSources.add |
    ForEach-Object {
        [PSCustomObject] @{
            Key   = [string] $_.key
            Value = [string] $_.value
        }
    }
)

$DisabledPackageSources = @(
    $NuGetConfig.configuration.disabledPackageSources.add |
    ForEach-Object {
        [PSCustomObject] @{
            Key   = [string] $_.key
            Value = [string] $_.value
        }
    }
)

Write-Host 'NuGet configuration (nuget.config) file path:'
Write-Host "  $NuGetConfigFilePath"
Write-Host 'Local package source key prefix:'
Write-Host "  $LocalPackageSourceKeyPrefix"
Write-Host 'Local package source key prefix comparison:'
Write-Host "  $LocalPackageSourceKeyPrefixComparison"
Write-Host

$ValidatedInvalidPackageSources = @()
$ValidatedDisabledPackageSources = @()
$ValidatedSkippedNonLocalPackageSources = @()

$ResolvedLocalPackageSources = @()

foreach ($PackageSource in $PackageSources) {
    $PackageSourceKey = $PackageSource.Key
    $PackageSourceValue = $PackageSource.Value

    if (-not (Test-HasValue $PackageSourceKey)) {
        $ValidatedInvalidPackageSources += $PackageSource

        continue
    }

    if (Test-PackageSourceIsDisabled `
            -DisabledPackageSources $DisabledPackageSources `
            -PackageSourceKey $PackageSourceKey) {
        $ValidatedDisabledPackageSources += $PackageSource

        continue
    }

    if (-not (Test-HasValue $PackageSourceValue)) {
        $ValidatedInvalidPackageSources += $PackageSource

        continue
    }

    $PackageSourceFolderPath = `
        Resolve-LocalPackageSourceFolderPath `
            -NuGetConfigFolderPath $NuGetConfigFolderPath `
            -PackageSourceValue $PackageSourceValue

    if ($null -eq $PackageSourceFolderPath) {
        $ValidatedSkippedNonLocalPackageSources += $PackageSource

        continue
    }

    $ResolvedLocalPackageSources += [PSCustomObject] @{
        Key        = $PackageSourceKey
        Value      = $PackageSourceValue
        FolderPath = $PackageSourceFolderPath
    }
}

if ($ValidatedInvalidPackageSources.Count -gt 0) {
    Write-Host 'Invalid NuGet package source(s):'

    foreach ($PackageSource in $ValidatedInvalidPackageSources) {
        Write-Host "  $($PackageSource.Key)"
    }

    Write-Host
}

if ($ValidatedDisabledPackageSources.Count -gt 0) {
    Write-Host 'Disabled NuGet package source(s):'

    foreach ($PackageSource in $ValidatedDisabledPackageSources) {
        Write-Host "  $($PackageSource.Key) →"
        Write-Host "    $($PackageSource.Value)"
    }

    Write-Host
}

if ($ValidatedSkippedNonLocalPackageSources.Count -gt 0) {
    Write-Host 'Skipped non-local NuGet package source(s):'

    foreach ($PackageSource in $ValidatedSkippedNonLocalPackageSources) {
        Write-Host "  $($PackageSource.Key) →"
        Write-Host "    $($PackageSource.Value)"
    }

    Write-Host
}

$MatchingLocalPackageSources = @(
    $ResolvedLocalPackageSources |
    Where-Object {
        Test-PackageSourceKeyMatchesPrefix `
            -PackageSourceKey $_.Key `
            -PackageSourceKeyPrefix $LocalPackageSourceKeyPrefix `
            -PackageSourceKeyPrefixComparison $LocalPackageSourceKeyPrefixComparison
    }
)

if ($MatchingLocalPackageSources.Count -eq 0) {
    if ($ResolvedLocalPackageSources.Count -gt 0) {
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

Write-Host "Found $($MatchingLocalPackageSources.Count) enabled local NuGet package source(s) with such key prefix."
Write-Host

$PreparedLocalPackageSources = @()
$FailedToPrepareLocalPackageSources = @()

foreach ($LocalPackageSource in $MatchingLocalPackageSources) {
    $PackageSourceKey = $LocalPackageSource.Key
    $PackageSourceFolderPath = $LocalPackageSource.FolderPath

    try {
        if (Test-Path -LiteralPath $PackageSourceFolderPath -PathType Leaf) {
            throw "NuGet package source '$PackageSourceKey' resolves to an existing file: `"$PackageSourceFolderPath`"."
        }

        if (-not (Test-Path -LiteralPath $PackageSourceFolderPath -PathType Container)) {
            New-Item -ItemType Directory -Path $PackageSourceFolderPath -Force -ErrorAction Stop | Out-Null
        }

        $PreparedLocalPackageSources += $LocalPackageSource
    }
    catch {
        $FailedToPrepareLocalPackageSources += [PSCustomObject] @{
            Key          = $PackageSourceKey
            Value        = $LocalPackageSource.Value
            FolderPath   = $PackageSourceFolderPath
            ErrorMessage = $_.Exception.Message
        }
    }
}

Write-Host 'Prepared local NuGet package source(s):'

foreach ($LocalPackageSource in $PreparedLocalPackageSources) {
    Write-Host "  $($LocalPackageSource.Key) →"
    Write-Host "    $($LocalPackageSource.FolderPath)"
}

Write-Host

if ($FailedToPrepareLocalPackageSources.Count -gt 0) {
    Write-Host 'Failed to prepare local NuGet package source(s):'

    foreach ($LocalPackageSource in $FailedToPrepareLocalPackageSources) {
        Write-Host "  $($LocalPackageSource.Key) →"
        Write-Host "    $($LocalPackageSource.FolderPath)"
        Write-Host "    $($LocalPackageSource.ErrorMessage)"
    }

    Write-Host

    exit 4
}


exit 0
