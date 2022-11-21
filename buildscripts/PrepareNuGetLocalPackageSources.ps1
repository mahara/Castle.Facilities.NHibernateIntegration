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
        [Parameter(Mandatory = $true)]
        [xml] $NuGetConfig,

        [Parameter(Mandatory = $true)]
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
        [Parameter(Mandatory = $true)]
        [string] $NuGetConfigFolderPath,

        [Parameter(Mandatory = $true)]
        [string] $PackageSourceValue
    )

    $packageSourceUri = $null
    $isAbsoluteUri = [System.Uri]::TryCreate(
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
        [Parameter(Mandatory = $true)]
        [string] $PackageSourceKey,

        [Parameter(Mandatory = $true)]
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
        [string] $Value
    )

    return -not [string]::IsNullOrWhiteSpace($Value)
}



################################################################################
# Main
################################################################################


if (-not (Test-HasValue $NuGetConfigFilePath)) {
    Write-Host "NuGet config file path must not be empty."
    exit 2
}

if (-not (Test-HasValue $LocalPackageSourceKeyPrefix)) {
    Write-Host "Local package source key prefix must not be empty."
    exit 2
}

if (-not (Test-Path -LiteralPath $NuGetConfigFilePath -PathType Leaf)) {
    Write-Host "Missing nuget.config file '$NuGetConfigFilePath'."
    exit 1
}

$nuGetConfigFolderPath = Split-Path -Parent $NuGetConfigFilePath

[xml] $nuGetConfig = Get-Content -LiteralPath $NuGetConfigFilePath

$packageSources = @($nuGetConfig.configuration.packageSources.add)

$localPackageSources = @(
    foreach ($packageSource in $packageSources) {
        $packageSourceKey = [string] $packageSource.key
        $packageSourceValue = [string] $packageSource.value

        if (Test-PackageSourceIsDisabled `
                -NuGetConfig $nuGetConfig `
                -PackageSourceKey $packageSourceKey) {
            continue
        }

        if (-not (Test-HasValue $packageSourceValue)) {
            Write-Host "NuGet package source '$packageSourceKey' has an empty value in '$NuGetConfigFilePath'."

            continue
        }

        if ($null -ne (
                Resolve-LocalPackageSourceFolderPath `
                    -NuGetConfigFolderPath $nuGetConfigFolderPath `
                    -PackageSourceValue $packageSourceValue
            )) {
            $packageSource
        }
        else {
            Write-Host "Skipping non-local NuGet package source '$packageSourceKey': $packageSourceValue"
        }
    }
)

$matchingLocalPackageSources = @(
    $localPackageSources |
    Where-Object {
        Test-PackageSourceKeyMatchesPrefix `
            -PackageSourceKey ([string] $_.key) `
            -PackageSourceKeyPrefix $LocalPackageSourceKeyPrefix
    }
)

if ($matchingLocalPackageSources.Count -eq 0) {
    if ($localPackageSources.Count -gt 0) {
        Write-Host "No enabled local NuGet package sources with key prefix '$LocalPackageSourceKeyPrefix' exist in '$NuGetConfigFilePath'."
        exit 3
    }
    else {
        Write-Host "No enabled local NuGet package sources found in '$NuGetConfigFilePath'."
        Write-Host 'Nothing to prepare.'
        exit 0
    }
}
else {
    Write-Host "Found $($matchingLocalPackageSources.Count) enabled local NuGet package source(s) with key prefix '$LocalPackageSourceKeyPrefix' in '$NuGetConfigFilePath'."
    Write-Host 'Preparing local NuGet package source(s)...'
}

foreach ($packageSource in $matchingLocalPackageSources) {
    $packageSourceKey = [string] $packageSource.key
    $packageSourceValue = [string] $packageSource.value

    $packageSourceFolderPath = Resolve-LocalPackageSourceFolderPath `
        -NuGetConfigFolderPath $nuGetConfigFolderPath `
        -PackageSourceValue $packageSourceValue

    if (Test-Path -LiteralPath $packageSourceFolderPath -PathType Leaf) {
        Write-Host "NuGet package source '$packageSourceKey' resolves to an existing file: $packageSourceFolderPath"
        exit 4
    }

    if (-not (Test-Path -LiteralPath $packageSourceFolderPath -PathType Container)) {
        New-Item -ItemType Directory -Path $packageSourceFolderPath -Force | Out-Null
    }

    Write-Host "  $packageSourceKey`:"
    Write-Host "    $packageSourceFolderPath"
}
