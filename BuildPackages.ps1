#Requires -Version 7.0


# ================
# EXIT CODES
# ================
# 0  = Build operations completed successfully.
# 1  = Missing required build infrastructure.
# 2  = Invalid command-line argument.
# >2 = Build operation failure propagated from inner BuildPackages.ps1.



param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $RemainingArguments = @()
)

$ErrorActionPreference = 'Stop'



################################################################################
# Main
################################################################################


$BuildPropertiesFileName = 'Build.Properties.ps1'
$BuildPackagesFileName = 'BuildPackages.ps1'

$BuildPropertiesFilePath = Join-Path -Path $PSScriptRoot -ChildPath $BuildPropertiesFileName


Write-Host


if (-not (Test-Path -LiteralPath $BuildPropertiesFilePath -PathType Leaf)) {
    Write-Host "Build properties file not found: `"$BuildPropertiesFilePath`"."
    Write-Host

    exit 1
}

. $BuildPropertiesFilePath

if ([string]::IsNullOrWhiteSpace($BUILD_CONFIGURATION_FOLDER_PATH)) {
    Write-Host 'Build configuration folder path must be defined.'
    Write-Host

    exit 1
}

if (-not (Test-Path -LiteralPath $BUILD_CONFIGURATION_FOLDER_PATH -PathType Container)) {
    Write-Host "Build configuration folder not found: `"$BUILD_CONFIGURATION_FOLDER_PATH`"."
    Write-Host

    exit 1
}

if ([string]::IsNullOrWhiteSpace($ARTIFACTS_FOLDER_PATH)) {
    Write-Host 'Artifacts folder path must be defined.'
    Write-Host

    exit 1
}


$BuildPackagesFilePath = Join-Path -Path $BUILD_CONFIGURATION_FOLDER_PATH -ChildPath $BuildPackagesFileName

if (-not (Test-Path -LiteralPath $BuildPackagesFilePath -PathType Leaf)) {
    Write-Host "Build infrastructure file not found: `"$BuildPackagesFilePath`"."
    Write-Host

    exit 1
}

& $BuildPackagesFilePath @RemainingArguments

exit $LASTEXITCODE


exit 0
