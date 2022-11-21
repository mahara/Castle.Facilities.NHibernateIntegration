#Requires -Version 7.0


# ================
# EXIT CODES
# ================
# 0  = Clean operations completed successfully.
# 1  = Missing required build infrastructure.
# 2  = Invalid command-line argument.
# 3  = Failed to delete artifacts folder.



param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $RemainingArguments = @()
)

$ErrorActionPreference = 'Stop'



################################################################################
# Main
################################################################################


$BuildPropertiesFileName = 'Build.Properties.ps1'

$BuildPropertiesFilePath = Join-Path -Path $PSScriptRoot -ChildPath $BuildPropertiesFileName


Write-Host


if (-not (Test-Path -LiteralPath $BuildPropertiesFilePath -PathType Leaf)) {
    Write-Host "Build properties file not found: `"$BuildPropertiesFilePath`"."
    Write-Host

    exit 1
}

. $BuildPropertiesFilePath

if ([string]::IsNullOrWhiteSpace($ARTIFACTS_FOLDER_PATH)) {
    Write-Host 'Artifacts folder path must be defined.'
    Write-Host

    exit 1
}


try {
    # dotnet clean $RemainingArguments[0] --configuration Debug
    # dotnet clean $RemainingArguments[0] --configuration Release

    if (Test-Path -LiteralPath $ARTIFACTS_FOLDER_PATH -PathType Container) {
        Write-Host "Deleting `"$ARTIFACTS_FOLDER_PATH`" folder..."

        Remove-Item -LiteralPath $ARTIFACTS_FOLDER_PATH -Recurse -Force

        Write-Host

        exit 0
    }
}
catch {
    Write-Host "Failed to delete `"$ARTIFACTS_FOLDER_PATH`" folder."
    Write-Host "  Error details: $($_.Exception.Message)"
    Write-Host

    exit 3
}


exit 0
