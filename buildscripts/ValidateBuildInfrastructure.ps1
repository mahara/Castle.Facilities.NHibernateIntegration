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
        return '{0}|{1}' -f `
            $this.InputFileName,
            ($this.Frameworks -join ';')
    }
}



################################################################################
# Workflow Operations
################################################################################


function Get-CmdEnvironmentVariables {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BuildPropertiesFilePath
    )

    $cmdExecutableFilePath = if ([string]::IsNullOrWhiteSpace($env:ComSpec)) { 'cmd.exe' } else { $env:ComSpec }
    $cmdCommand = "call `"$BuildPropertiesFilePath`" >NUL && set"
    $cmdOutput = & $cmdExecutableFilePath /D /S /C $cmdCommand

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build.Properties.cmd failed with exit code $LASTEXITCODE."
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
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    $variable = Get-Variable -Name $Name -Scope Script -ErrorAction SilentlyContinue

    if ($null -eq $variable) {
        return $null
    }

    return $variable.Value
}

function Get-CmdVariableValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    return $cmdVariables[$Name]
}

function Add-ComparisonDifference {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[string]] $Differences,

        [Parameter(Mandatory = $true)]
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
        [string] $BuildParameterSpecification
    )

    $parts = $BuildParameterSpecification -split '\|', 2

    return [BuildParameter]::new(
        $parts[0].Trim(),
        (ConvertTo-Frameworks $parts[1])
    )
}

function ConvertTo-Frameworks {
    param(
        [AllowNull()]
        [string] $FrameworksParameterSpecification
    )

    return @(ConvertTo-ParameterSpecifications $FrameworksParameterSpecification)
}

function ConvertTo-ParameterSpecifications {
    param(
        [AllowNull()]
        [object] $Value,

        [string] $Separator = ';'
    )

    if ($null -eq $Value) {
        return @()
    }

    if ($Value -is [System.Array]) {
        return @(Normalize-StringCollection $Value)
    }

    return @(
        Normalize-StringCollection (
            ([string] $Value).Split(
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

    $regexMatches = [regex]::Matches(
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
        Justification = 'Normalize describes canonicalizing parameter specification values and is clearer than available approved verbs.'
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
        [BuildParameter[]] $Left,

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
        [BuildParameter] $Left,

        [BuildParameter] $Right
    )

    if ($Left.InputFileName -cne $Right.InputFileName) {
        return $false
    }

    return Test-StringCollectionEqual `
        -Left $Left.Frameworks `
        -Right $Right.Frameworks
}

function Test-StringCollectionEqual {
    param(
        [string[]] $Left,

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

function Format-BuildParameters {
    param(
        [BuildParameter[]] $Value
    )

    return @(
        foreach ($item in $Value) {
            $item.ToString()
        }
    ) -join '; '
}

function Get-NormalizedFolderPath {
    param(
        [AllowNull()]
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }

    return [System.IO.Path]::GetFullPath($Value).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
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


if (-not $IsWindows) {
    Write-Host 'Skipping Windows-specific build infrastructure validation on non-Windows platforms.'
    exit 0
}

if (-not (Test-HasValue $WorkspaceFolderPath)) {
    Write-Host "Workspace folder path must not be empty."
    exit 2
}

if (-not (Test-HasValue $BuildConfigurationFolderName)) {
    Write-Host "Build configuration folder name must not be empty."
    exit 2
}

$WorkspaceFolderPath = Get-NormalizedFolderPath $WorkspaceFolderPath

$BuildConfigurationFolderPath = Join-Path -Path $WorkspaceFolderPath -ChildPath $BuildConfigurationFolderName

if (-not (Test-Path -LiteralPath $BuildConfigurationFolderPath -PathType Container)) {
    Write-Host "Missing build configuration folder '$BuildConfigurationFolderPath'."
    exit 1
}

$requiredFilePaths = @(
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
)

foreach ($requiredFilePath in $requiredFilePaths) {
    if (-not (Test-Path -LiteralPath $requiredFilePath -PathType Leaf)) {
        Write-Host "Missing build configuration file '$requiredFilePath'."
        exit 1
    }
}


#
# Load
#

$buildPropertiesPowerShellFilePath = Join-Path -Path $WorkspaceFolderPath -ChildPath 'Build.Properties.ps1'

$buildPropertiesCmdFilePath = Join-Path -Path $WorkspaceFolderPath -ChildPath 'Build.Properties.cmd'


. $buildPropertiesPowerShellFilePath

$cmdVariables = Get-CmdEnvironmentVariables $buildPropertiesCmdFilePath


$differences = [System.Collections.Generic.List[string]]::new()

$powerShellBuildUnits = Get-PowerShellBuildUnits
$cmdBuildUnits = Get-CmdBuildUnits

if (-not (
        Test-StringCollectionEqual `
            -Left $powerShellBuildUnits `
            -Right $cmdBuildUnits
    )) {
    Add-ComparisonDifference `
        -Differences $differences `
        -Name 'BUILD_UNITS' `
        -PowerShellValue ($powerShellBuildUnits -join '; ') `
        -CmdValue ($cmdBuildUnits -join '; ')
}

$variableNames = @(
    'BUILD_CONFIGURATION_FOLDER_NAME'
    'SOURCE_CODE_FOLDER_NAME'
    'ARTIFACTS_FOLDER_NAME'
    'ARTIFACTS___OUTPUT___FOLDER_NAME'
    'ARTIFACTS___PACKAGE_OUTPUT___FOLDER_NAME'
    'ARTIFACTS___TEST_RESULTS___FOLDER_NAME'
    'PARAMETER___RUN_BUILD___DEFAULT'
    'PARAMETER___RUN_TEST___DEFAULT'
    'PARAMETER___RUN_PACKAGE___DEFAULT'
    'PARAMETER___VERSION___DEFAULT'
    'PARAMETER___CONFIGURATION___DEFAULT'
    'PARAMETER___FRAMEWORKS___DEFAULT'
    'PARAMETER___ENABLE_SOURCE_LINK___DEFAULT'
)

foreach ($variableName in $variableNames) {
    Add-ComparisonDifference `
        -Differences $differences `
        -Name $variableName `
        -PowerShellValue (Get-PowerShellVariableValue $variableName) `
        -CmdValue (Get-CmdVariableValue $variableName)
}

$folderPathVariableNames = @(
    'WORKSPACE_FOLDER_PATH'
    'BUILD_CONFIGURATION_FOLDER_PATH'
    'SOURCE_CODE_FOLDER_PATH'
    'ARTIFACTS_FOLDER_PATH'
    'ARTIFACTS___OUTPUT___FOLDER_PATH'
    'ARTIFACTS___PACKAGE_OUTPUT___FOLDER_PATH'
    'ARTIFACTS___TEST_RESULTS___FOLDER_PATH'
)

foreach ($variableName in $folderPathVariableNames) {
    Add-ComparisonDifference `
        -Differences $differences `
        -Name $variableName `
        -PowerShellValue (Get-NormalizedFolderPath (Get-PowerShellVariableValue $variableName)) `
        -CmdValue (Get-NormalizedFolderPath (Get-CmdVariableValue $variableName))
}

$buildUnits = $powerShellBuildUnits

$buildUnitParameterNames = @(
    'BUILD_PARAMETERS'
    'TEST_PARAMETERS'
    'PACKAGE_PARAMETERS'
    'PACKAGE_NEV_PARAMETERS'
)

foreach ($buildUnit in $buildUnits) {
    foreach ($buildUnitParameterName in $buildUnitParameterNames) {
        #
        # Raw values
        #

        $variableName = "BUILD_UNIT___$buildUnit`___$buildUnitParameterName"

        $powerShellVariableRawValue = $null

        if ($BUILD_UNIT_PARAMETERS.ContainsKey($buildUnit) -and
            $BUILD_UNIT_PARAMETERS[$buildUnit].ContainsKey($buildUnitParameterName)) {
            $powerShellVariableRawValue = $BUILD_UNIT_PARAMETERS[$buildUnit][$buildUnitParameterName]
        }

        $cmdVariableRawValue = Get-CmdVariableValue $variableName

        switch ($buildUnitParameterName) {
            {
                $_ -in @(
                    'BUILD_PARAMETERS',
                    'TEST_PARAMETERS',
                    'PACKAGE_PARAMETERS'
                )
            }
            {
                #
                # Parse
                #

                $powerShellBuildParameters = ConvertTo-BuildParameters $powerShellVariableRawValue

                $cmdBuildParameters = ConvertTo-BuildParameters $cmdVariableRawValue


                #
                # Compare
                #

                if (-not (
                        Test-BuildParametersEqual `
                            -Left $powerShellBuildParameters `
                            -Right $cmdBuildParameters
                    )) {
                    Add-ComparisonDifference `
                        -Differences $differences `
                        -Name $variableName `
                        -PowerShellValue (
                            Format-BuildParameters `
                                $powerShellBuildParameters
                        ) `
                        -CmdValue (
                            Format-BuildParameters `
                                $cmdBuildParameters
                        )
                }
            }

            { $_ -eq 'PACKAGE_NEV_PARAMETERS' }
            {
                #
                # Parse
                #

                $powerShellPackageNevParameters = ConvertTo-BuildParameterSpecifications $powerShellVariableRawValue

                $cmdPackageNevParameters = ConvertTo-BuildParameterSpecifications $cmdVariableRawValue


                #
                # Compare
                #

                if (-not (
                        Test-StringCollectionEqual `
                            -Left $powerShellPackageNevParameters `
                            -Right $cmdPackageNevParameters
                    )) {
                    Add-ComparisonDifference `
                        -Differences $differences `
                        -Name $variableName `
                        -PowerShellValue ($powerShellPackageNevParameters -join '; ') `
                        -CmdValue ($cmdPackageNevParameters -join '; ')
                }
            }

            default {
                Write-Host "Unknown build unit parameter name '$buildUnitParameterName'."
                exit 3
            }
        }
    }
}

if ($differences.Count -gt 0) {
    Write-Host 'Build infrastructure validation failed.'
    Write-Host ''

    foreach ($difference in $differences) {
        Write-Host $difference
        Write-Host ''
    }

    exit 4
}

Write-Host 'Build infrastructure validation succeeded.'
Write-Host "Workspace folder path:"
Write-Host "  $WorkspaceFolderPath"
Write-Host "Build configuration folder path:"
Write-Host "  $BuildConfigurationFolderPath"
Write-Host 'Build units:'

foreach ($buildUnit in $buildUnits) {
    Write-Host "  $buildUnit"
}
