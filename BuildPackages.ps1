#Requires -Version 7.0


# ================
# EXIT CODES
# ================
# 0  = Build completed successfully.
# 1  = Missing required build infrastructure.
# 2  = Invalid command-line argument.
# >2 = Build operation failure propagated from inner BuildPackages.cmd.


param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $RemainingArguments = @()
)

$ErrorActionPreference = 'Stop'



################################################################################
# Main
################################################################################


. (Join-Path -Path $PSScriptRoot -ChildPath 'Build.Properties.ps1')

if ([string]::IsNullOrWhiteSpace($BUILD_CONFIGURATION_FOLDER_PATH)) {
    exit 1
}


$buildPackagesFilePath = Join-Path -Path $BUILD_CONFIGURATION_FOLDER_PATH -ChildPath 'BuildPackages.ps1'

& $buildPackagesFilePath @RemainingArguments
exit $LASTEXITCODE
