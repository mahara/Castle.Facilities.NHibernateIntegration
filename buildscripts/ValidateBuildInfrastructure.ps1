#Requires -Version 7.0


# ================
# EXIT CODES
# ================
# 0  = Build infrastructure validated successfully.
# 1  = Missing required build infrastructure.
# 2  = Invalid command-line argument.
# 3  = Invalid build infrastructure configuration.
# 4  = Build infrastructure validation failed.



param(
    [string] $WorkspaceFolderPath = (Split-Path -Parent $PSScriptRoot),

    [string] $BuildConfigurationFolderName = 'build'
)

$ErrorActionPreference = 'Stop'



################################################################################
# Types
################################################################################


class BuildParameter {
    [string] $InputFileName

    [string[]] $Frameworks

    BuildParameter(
        [string] $InputFileName,

        [string[]] $Frameworks) {
        $this.InputFileName = $InputFileName
        $this.Frameworks = $Frameworks
    }

    [string] ToString() {
        return (
            '{0}|{1}' -f `
                ($this.InputFileName ?? ''),
                ($this.Frameworks -join ';')
        )
    }
}



################################################################################
# Workflow Operations
################################################################################


function Get-CmdEnvironmentVariables {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $BuildPropertiesFilePath
    )

    $cmdExecutableFilePath = if ([string]::IsNullOrWhiteSpace($env:ComSpec)) { 'cmd.exe' } else { $env:ComSpec }
    $cmdCommand = "call `"$BuildPropertiesFilePath`" >NUL && set"
    $cmdOutput = & $cmdExecutableFilePath /D /S /C $cmdCommand

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`"$BuildPropertiesFilePath`" failed with exit code $LASTEXITCODE."
        Write-Host

        exit 3
    }

    $variables = @{}

    foreach ($line in $cmdOutput) {
        $separatorIndex = $line.IndexOf('=')

        if ($separatorIndex -le 0) {
            continue
        }

        $name = $line.Substring(0, $separatorIndex)
        $value = $line.Substring($separatorIndex + 1)

        $variables[$name] = $value
    }

    return $variables
}

function Get-PowerShellVariableValue {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $Name
    )

    $variable = Get-Variable -Name $Name -Scope Script -ErrorAction SilentlyContinue

    if ($null -eq $variable) {
        return $null
    }

    if ($variable.Value -is [bool]) {
        return $variable.Value.ToString().ToLowerInvariant()
    }

    return $variable.Value
}

function Get-CmdVariableValue {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $Name
    )

    return $CmdVariables[$Name]
}

function Add-ComparisonDifference {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[string]] $Differences,

        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $Name,

        [AllowNull()]
        [string] $PowerShellValue,

        [AllowNull()]
        [string] $CmdValue
    )

    if ($PowerShellValue -cne $CmdValue) {
        $Differences.Add("$Name`n  .ps1 = '$PowerShellValue'`n  .cmd = '$CmdValue'")
    }
}

function Get-PowerShellBuildUnits {
    return @($BUILD_UNITS)
}

function Get-CmdBuildUnits {
    return ConvertTo-BuildParameterSpecifications (Get-CmdVariableValue 'BUILD_UNITS')
}

function Format-BuildParameters {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [BuildParameter[]] $BuildParameters
    )

    $buildParameterStrings = @(
        foreach ($buildParameter in $BuildParameters) {
            $buildParameter.ToString()
        }
    )

    return $buildParameterStrings -join '; '
}

function ConvertTo-BuildParameters {
    param(
        [AllowNull()]
        [object] $Value
    )

    return @(
        foreach ($buildParameterSpecification in (ConvertTo-BuildParameterSpecifications $Value)) {
            ConvertTo-BuildParameter $buildParameterSpecification
        }
    )
}

function ConvertTo-BuildParameter {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $BuildParameterSpecification
    )

    $parts = $BuildParameterSpecification -split '\|', 2

    return (
        [BuildParameter]::new(
            $(if ($parts.Count -gt 0) { $parts[0].Trim() } else { '' }),
            (ConvertTo-Frameworks $(if ($parts.Count -gt 1) { $parts[1] } else { $null }))
        )
    )
}

function ConvertTo-Frameworks {
    param(
        [AllowNull()]
        [string] $FrameworksParameterSpecification,

        [ValidateNotNullOrEmpty()]
        [string] $Separator = ';'
    )

    if ($null -eq $FrameworksParameterSpecification) {
        return @()
    }

    return @(
        Normalize-StringCollection (
            $FrameworksParameterSpecification.Split(
                $Separator,
                [StringSplitOptions]::RemoveEmptyEntries
            )
        )
    )
}

function ConvertTo-BuildParameterSpecifications {
    param(
        [AllowNull()]
        [object] $Value
    )

    if ($null -eq $Value) {
        return @()
    }



    #
    # .NET/PS Collection Type
    #

    if ($Value -is [System.Array]) {
        return @(Normalize-StringCollection $Value)
    }



    #
    # CMD Raw String Type
    #

    return @(ConvertFrom-CmdRawBuildParameterSpecifications $Value)
}

function ConvertFrom-CmdRawBuildParameterSpecifications {
    param(
        [AllowNull()]
        [object] $Value
    )

    if ($null -eq $Value) {
        return @()
    }

    $regexMatches = `
        [regex]::Matches(
            [string] $Value,
            '"([^"]*)"'
        )

    return @(
        foreach ($match in $regexMatches) {
            $match.Groups[1].Value.Trim()
        }
    )
}

function Normalize-StringCollection {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute(
        'PSUseApprovedVerbs',
        '',
        Justification = 'Normalize describes canonicalizing a string collection and is clearer than available approved verbs.'
    )]

    param(
        [AllowNull()]
        [object] $Value
    )

    if ($null -eq $Value) {
        return @()
    }

    return @(
        foreach ($item in @($Value)) {
            $item = ([string] $item).Trim()

            if (-not [string]::IsNullOrWhiteSpace($item)) {
                $item
            }
        }
    )
}

function Test-BuildParametersEqual {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [BuildParameter[]] $Left,

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [BuildParameter[]] $Right
    )

    if ($Left.Count -ne $Right.Count) {
        return $false
    }

    for ($i = 0; $i -lt $Left.Count; $i++) {
        if (-not (
                Test-BuildParameterEqual `
                    -Left $Left[$i] `
                    -Right $Right[$i]
           )) {
            return $false
        }
    }

    return $true
}

function Test-BuildParameterEqual {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [BuildParameter] $Left,

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [BuildParameter] $Right
    )

    if ($Left.InputFileName -cne $Right.InputFileName) {
        return $false
    }

    return (
        Test-StringCollectionEqual `
            -Left $Left.Frameworks `
            -Right $Right.Frameworks
    )
}

function Test-StringCollectionEqual {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [string[]] $Left,

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [string[]] $Right
    )

    if ($Left.Count -ne $Right.Count) {
        return $false
    }

    for ($i = 0; $i -lt $Left.Count; $i++) {
        if ($Left[$i] -cne $Right[$i]) {
            return $false
        }
    }

    return $true
}

function Get-NormalizedFolderPath {
    param(
        [AllowNull()]
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }

    $fullPath = [System.IO.Path]::GetFullPath($Value)
    $rootPath = [System.IO.Path]::GetPathRoot($fullPath)

    if ($fullPath.Length -le $rootPath.Length) {
        return $fullPath
    }

    return (
        $fullPath.TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar
        )
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


if (-not $IsWindows) {
    Write-Host 'Skipping Windows-specific build infrastructure validation on non-Windows platforms.'
    Write-Host

    exit 0
}


if (-not (Test-HasValue $WorkspaceFolderPath)) {
    Write-Host 'Workspace folder path must not be empty.'
    Write-Host

    exit 2
}

if (-not (Test-Path -LiteralPath $WorkspaceFolderPath -PathType Container)) {
    Write-Host "Workspace folder not found: `"$WorkspaceFolderPath`"."
    Write-Host

    exit 1
}

if (-not (Test-HasValue $BuildConfigurationFolderName)) {
    Write-Host 'Build configuration folder name must not be empty.'
    Write-Host

    exit 2
}


$WorkspaceFolderPath = Get-NormalizedFolderPath $WorkspaceFolderPath

$BuildConfigurationFolderPath = Join-Path -Path $WorkspaceFolderPath -ChildPath $BuildConfigurationFolderName

if (-not (Test-Path -LiteralPath $BuildConfigurationFolderPath -PathType Container)) {
    Write-Host "Build configuration folder not found: `"$BuildConfigurationFolderPath`"."
    Write-Host

    exit 1
}

$RequiredFilePaths = @(
    (Join-Path -Path $WorkspaceFolderPath -ChildPath 'Build.Environment.cmd')
    (Join-Path -Path $WorkspaceFolderPath -ChildPath 'Build.Properties.cmd')
    (Join-Path -Path $WorkspaceFolderPath -ChildPath 'Build.Properties.ps1')
    (Join-Path -Path $WorkspaceFolderPath -ChildPath 'BuildPackages.cmd')
    (Join-Path -Path $WorkspaceFolderPath -ChildPath 'BuildPackages.ps1')
    (Join-Path -Path $WorkspaceFolderPath -ChildPath 'CleanPackages.cmd')
    (Join-Path -Path $WorkspaceFolderPath -ChildPath 'CleanPackages.ps1')
    (Join-Path -Path $BuildConfigurationFolderPath -ChildPath 'BuildPackages.cmd')
    (Join-Path -Path $BuildConfigurationFolderPath -ChildPath 'BuildPackages.ps1')
    (Join-Path -Path $BuildConfigurationFolderPath -ChildPath 'PrepareNuGetLocalPackageSources.ps1')
    (Join-Path -Path $BuildConfigurationFolderPath -ChildPath 'ValidateBuildInfrastructure.ps1')
)

foreach ($FilePath in $RequiredFilePaths) {
    if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
        Write-Host "Build infrastructure file not found: `"$FilePath`"."
        Write-Host

        exit 1
    }
}


#
# Load
#

$BuildPropertiesPowerShellFilePath = $RequiredFilePaths[2]

$BuildPropertiesCmdFilePath = $RequiredFilePaths[1]


. $BuildPropertiesPowerShellFilePath

$CmdVariables = Get-CmdEnvironmentVariables $BuildPropertiesCmdFilePath


$PowerShellBuildUnits = @(Get-PowerShellBuildUnits)

$CmdBuildUnits = @(Get-CmdBuildUnits)


$Differences = [System.Collections.Generic.List[string]]::new()


if (-not (
        Test-StringCollectionEqual `
            -Left $PowerShellBuildUnits `
            -Right $CmdBuildUnits
    )) {
    Add-ComparisonDifference `
        -Differences $Differences `
        -Name 'BUILD_UNITS' `
        -PowerShellValue ($PowerShellBuildUnits -join '; ') `
        -CmdValue ($CmdBuildUnits -join '; ')
}

$VariableNames = @(
    'BUILD_CONFIGURATION_FOLDER_NAME'
    'SOURCE_CODE_FOLDER_NAME'
    'ARTIFACTS_FOLDER_NAME'
    'ARTIFACTS___OUTPUT___FOLDER_NAME'
    'ARTIFACTS___TEST_RESULTS___FOLDER_NAME'
    'ARTIFACTS___PACKAGE_OUTPUT___FOLDER_NAME'
    'PARAMETER___RUN_BUILD___DEFAULT'
    'PARAMETER___RUN_TEST___DEFAULT'
    'PARAMETER___RUN_PACKAGE___DEFAULT'
    'PARAMETER___VERSION___DEFAULT'
    'PARAMETER___CONFIGURATION___DEFAULT'
    'PARAMETER___FRAMEWORKS___DEFAULT'
    'PARAMETER___ENABLE_SOURCE_LINK___DEFAULT'
)

foreach ($VariableName in $VariableNames) {
    Add-ComparisonDifference `
        -Differences $Differences `
        -Name $VariableName `
        -PowerShellValue (Get-PowerShellVariableValue $VariableName) `
        -CmdValue (Get-CmdVariableValue $VariableName)
}

$FolderPathVariableNames = @(
    'WORKSPACE_FOLDER_PATH'
    'BUILD_CONFIGURATION_FOLDER_PATH'
    'SOURCE_CODE_FOLDER_PATH'
    'ARTIFACTS_FOLDER_PATH'
    'ARTIFACTS___OUTPUT___FOLDER_PATH'
    'ARTIFACTS___TEST_RESULTS___FOLDER_PATH'
    'ARTIFACTS___PACKAGE_OUTPUT___FOLDER_PATH'
)

foreach ($VariableName in $FolderPathVariableNames) {
    Add-ComparisonDifference `
        -Differences $Differences `
        -Name $VariableName `
        -PowerShellValue (Get-NormalizedFolderPath (Get-PowerShellVariableValue $VariableName)) `
        -CmdValue (Get-NormalizedFolderPath (Get-CmdVariableValue $VariableName))
}


$BuildUnits = $PowerShellBuildUnits

$BuildUnitParameterNames = @(
    'BUILD_PARAMETERS'
    'TEST_PARAMETERS'
    'PACKAGE_PARAMETERS'
    'PACKAGE_NEV_PARAMETERS'
)

foreach ($BuildUnit in $BuildUnits) {
    foreach ($BuildUnitParameterName in $BuildUnitParameterNames) {
        #
        # Raw values
        #

        $VariableName = "BUILD_UNIT___$BuildUnit`___$BuildUnitParameterName"

        $PowerShellVariableRawValue = $null

        if ($BUILD_UNIT_PARAMETERS.ContainsKey($BuildUnit) -and
            $BUILD_UNIT_PARAMETERS[$BuildUnit].ContainsKey($BuildUnitParameterName)) {
            $PowerShellVariableRawValue = $BUILD_UNIT_PARAMETERS[$BuildUnit][$BuildUnitParameterName]
        }

        $CmdVariableRawValue = Get-CmdVariableValue $VariableName

        switch ($BuildUnitParameterName) {
            {
                $_ -in @(
                    'BUILD_PARAMETERS'
                    'TEST_PARAMETERS'
                    'PACKAGE_PARAMETERS'
                )
            }
            {
                #
                # Parse
                #

                $PowerShellBuildParameters = @(ConvertTo-BuildParameters $PowerShellVariableRawValue)

                $CmdBuildParameters = @(ConvertTo-BuildParameters $CmdVariableRawValue)


                #
                # Compare
                #

                if (-not (
                        Test-BuildParametersEqual `
                            -Left $PowerShellBuildParameters `
                            -Right $CmdBuildParameters
                    )) {
                    Add-ComparisonDifference `
                        -Differences $Differences `
                        -Name $VariableName `
                        -PowerShellValue (
                            Format-BuildParameters `
                                $PowerShellBuildParameters
                        ) `
                        -CmdValue (
                            Format-BuildParameters `
                                $CmdBuildParameters
                        )
                }
            }

            {
                $_ -in @(
                    'PACKAGE_NEV_PARAMETERS'
                )
            }
            {
                #
                # Parse
                #

                $PowerShellPackageNevParameters = @(ConvertTo-BuildParameterSpecifications $PowerShellVariableRawValue)

                $CmdPackageNevParameters = @(ConvertTo-BuildParameterSpecifications $CmdVariableRawValue)


                #
                # Compare
                #

                if (-not (
                        Test-StringCollectionEqual `
                            -Left $PowerShellPackageNevParameters `
                            -Right $CmdPackageNevParameters
                    )) {
                    Add-ComparisonDifference `
                        -Differences $Differences `
                        -Name $VariableName `
                        -PowerShellValue ($PowerShellPackageNevParameters -join '; ') `
                        -CmdValue ($CmdPackageNevParameters -join '; ')
                }
            }

            default {
                Write-Host "Unknown build unit parameter name '$BuildUnitParameterName'."
                Write-Host

                exit 3
            }
        }
    }
}

if ($Differences.Count -gt 0) {
    Write-Host 'Build infrastructure validation failed.'
    Write-Host

    foreach ($Difference in $Differences) {
        Write-Host $Difference
        Write-Host
    }

    exit 4
}

Write-Host 'Build infrastructure validation succeeded.'
Write-Host

Write-Host "Workspace folder path:"
Write-Host "  $WorkspaceFolderPath"
Write-Host "Build configuration folder path:"
Write-Host "  $BuildConfigurationFolderPath"
Write-Host

Write-Host 'Build units:'

foreach ($BuildUnit in $BuildUnits) {
    Write-Host "  $BuildUnit"
}

Write-Host


exit 0
