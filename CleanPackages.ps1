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


. (Join-Path -Path $PSScriptRoot -ChildPath 'Build.Properties.ps1')

if ([string]::IsNullOrWhiteSpace($ARTIFACTS_FOLDER_PATH)) {
    exit 1
}


try {
    # dotnet clean $RemainingArguments[0] --configuration Debug
    # dotnet clean $RemainingArguments[0] --configuration Release

    if (Test-Path -LiteralPath $ARTIFACTS_FOLDER_PATH) {
        Write-Host "Deleting `"$ARTIFACTS_FOLDER_PATH`" folder..."

        Remove-Item -LiteralPath $ARTIFACTS_FOLDER_PATH -Recurse -Force
    }

    exit 0
}
catch {
    Write-Error "Failed to delete `"$ARTIFACTS_FOLDER_PATH`" folder."
    Write-Error "  Error details: $($_.Exception.Message)"
    exit 3
}
