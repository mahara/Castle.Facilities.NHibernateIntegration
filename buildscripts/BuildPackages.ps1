#Requires -Version 7.0


# ================================
# ARCHITECTURAL FOUNDATIONS
# ================================
# -   Design Principles
#     Fundamental architectural rules.
#
# -   Common Behavior
#     Shared workflow and semantics across implementations.
#
# -   Implementation Parity
#     The .cmd and .ps1 implementations should exhibit the closest practical behavioral parity.
#     Differences are permitted only when required by the underlying shell or platform capabilities.
#
# ================
# TERMINOLOGY
# ================
#
#     exit code                         = script/subroutine result code.
#     $args                             = all original command-line arguments.
#     array splatting invocation        = safe external command argument passing.
#     Join-Path                         = file/folder path composition.
#     [System.IO.Path] helpers          = file-name parsing.
#
# ================
# EXIT CODES
# ================
# 0  = Build operations completed successfully.
# 1  = Missing required build infrastructure.
# 2  = Invalid command-line argument.
# 3  = Build unit build parameters are not defined.
# 4  = Build input file name is not defined, or file path resolution failed.
# 5  = Build frameworks are not defined.
# 6  = dotnet build failed.
# 7  = Test input file name is not defined, or file path resolution failed.
# 8  = Test frameworks are not defined.
# 9  = dotnet test failed.
# 10 = Package input file name is not defined, or file path resolution failed.
# 11 = Package frameworks are not defined.
# 12 = dotnet pack failed.
# 13 = Explicit.NuGet.Versions build failed.
# 14 = Explicit.NuGet.Versions execution failed.



param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $RemainingArguments = @()
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


function Initialize-Arguments {
    param(
        [string[]] $Arguments = @()
    )

    $argumentIndex = 0

    while ($argumentIndex -lt $Arguments.Count) {
        $argument = [string] $Arguments[$argumentIndex]

        if ($argument -ieq '--no-build') {
            $script:ARGUMENT___RUN_BUILD = $false
            $argumentIndex++
            continue
        }

        if ($argument -ieq '--no-test') {
            $script:ARGUMENT___RUN_TEST = $false
            $argumentIndex++
            continue
        }

        if ($argument -ieq '--no-package') {
            $script:ARGUMENT___RUN_PACKAGE = $false
            $argumentIndex++
            continue
        }

        if ($argument -ieq '--version') {
            $script:ARGUMENT___VERSION = Resolve-ArgumentValue -ArgumentName $argument -ArgumentValue $Arguments[$argumentIndex + 1]
            $argumentIndex += 2
            continue
        }

        if ($argument -ieq '--configuration') {
            $script:ARGUMENT___CONFIGURATION = Resolve-ArgumentValue -ArgumentName $argument -ArgumentValue $Arguments[$argumentIndex + 1]
            $argumentIndex += 2
            continue
        }

        if ($argument -ieq '--framework') {
            $script:ARGUMENT___FRAMEWORKS = Resolve-ArgumentValue -ArgumentName $argument -ArgumentValue $Arguments[$argumentIndex + 1]
            $argumentIndex += 2
            continue
        }

        if ($argument -ieq '--disable-source-link') {
            $script:ARGUMENT___ENABLE_SOURCE_LINK = $false
            $argumentIndex++
            continue
        }

        Write-Host "Unknown argument `"$argument`"."
        Write-Host 'Supported arguments are --no-build, --no-test, --no-package, --version, --configuration, --framework, and --disable-source-link.'
        exit 2
    }
}

function Resolve-ArgumentValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ArgumentName,

        [AllowNull()]
        [string] $ArgumentValue
    )

    if ([string]::IsNullOrWhiteSpace($ArgumentValue)) {
        Write-Host "Missing value for $ArgumentName."
        exit 2
    }

    if ($ArgumentValue.StartsWith('--', [System.StringComparison]::Ordinal)) {
        Write-Host "Missing value for $ArgumentName."
        exit 2
    }

    return $ArgumentValue
}

function Initialize-Parameters {
    $script:PARAMETER___RUN_BUILD = $PARAMETER___RUN_BUILD___DEFAULT
    if (Test-HasValue $script:ARGUMENT___RUN_BUILD) {
        $script:PARAMETER___RUN_BUILD = $script:ARGUMENT___RUN_BUILD
    }

    $script:PARAMETER___RUN_TEST = $PARAMETER___RUN_TEST___DEFAULT
    if (Test-HasValue $script:ARGUMENT___RUN_TEST) {
        $script:PARAMETER___RUN_TEST = $script:ARGUMENT___RUN_TEST
    }

    $script:PARAMETER___RUN_PACKAGE = $PARAMETER___RUN_PACKAGE___DEFAULT
    if (Test-HasValue $script:ARGUMENT___RUN_PACKAGE) {
        $script:PARAMETER___RUN_PACKAGE = $script:ARGUMENT___RUN_PACKAGE
    }

    $script:PARAMETER___VERSION = $PARAMETER___VERSION___DEFAULT
    if (Test-HasValue $script:ARGUMENT___VERSION) {
        $script:PARAMETER___VERSION = $script:ARGUMENT___VERSION
    }

    $script:PARAMETER___CONFIGURATION = $PARAMETER___CONFIGURATION___DEFAULT
    if (Test-HasValue $script:ARGUMENT___CONFIGURATION) {
        $script:PARAMETER___CONFIGURATION = $script:ARGUMENT___CONFIGURATION
    }

    $script:PARAMETER___FRAMEWORKS = $PARAMETER___FRAMEWORKS___DEFAULT
    if (Test-HasValue $script:ARGUMENT___FRAMEWORKS) {
        $script:PARAMETER___FRAMEWORKS = $script:ARGUMENT___FRAMEWORKS
    }

    $script:PARAMETER___ENABLE_SOURCE_LINK = $PARAMETER___ENABLE_SOURCE_LINK___DEFAULT
    if (Test-HasValue $script:ARGUMENT___ENABLE_SOURCE_LINK) {
        $script:PARAMETER___ENABLE_SOURCE_LINK = $script:ARGUMENT___ENABLE_SOURCE_LINK
    }
}

function Get-BuildUnitParameterSpecification {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BuildUnit
    )

    if ($null -eq $BUILD_UNIT_PARAMETERS -or
        -not $BUILD_UNIT_PARAMETERS.ContainsKey($BuildUnit)) {
        Write-Host "Build unit `"$BuildUnit`" is invalid because no build unit parameter defined."
        exit 3
    }

    $buildUnitParameterSpecification = $BUILD_UNIT_PARAMETERS[$BuildUnit]

    if ($null -eq $buildUnitParameterSpecification.BUILD_PARAMETERS -or
        @($buildUnitParameterSpecification.BUILD_PARAMETERS).Count -eq 0) {
        Write-Host "Build unit `"$BuildUnit`" is invalid because no build unit parameter defined."
        exit 3
    }

    return $buildUnitParameterSpecification
}

# Build Unit and Build Unit Parameter Specification Data Model
# ============================================================
#
# A BuildUnit has a BuildUnitParameterSpecification.
# A BuildUnitParameterSpecification contains multiple parameter groups.
# The parameter groups are BUILD_PARAMETERS, TEST_PARAMETERS, PACKAGE_PARAMETERS, and PACKAGE_NEV_PARAMETERS.
# Those parameter groups are represented as arrays of BuildParameterSpecification objects,
# except for the PACKAGE_NEV_PARAMETERS group, which is represented as an array of strings.
#
# BuildUnitParameterSpecification
# -------------------------------
#
#     BuildUnitParameterSpecification
#         |
#         +-- BUILD_PARAMETERS
#         |       |
#         |       +-- BuildParameterSpecification[]
#         |
#         +-- TEST_PARAMETERS
#         |       |
#         |       +-- BuildParameterSpecification[]
#         |
#         +-- PACKAGE_PARAMETERS
#         |       |
#         |       +-- BuildParameterSpecification[]
#         |
#         +-- PACKAGE_NEV_PARAMETERS
#                 |
#                 +-- string[]
#
# BUILD_PARAMETERS
# ----------------
# Representation:
#     BuildParameterSpecification[]
#
# Format:
#     InputFileName|FrameworksParameterSpecification
#
# Examples:
#     [MSBuildProjectName].csproj|net10.0;net9.0;net8.0;net48
#
# Conversion:
#     BuildParameterSpecification[]
#         |
#         v
#     ConvertTo-BuildParameters
#         |
#         v
#     BuildParameter[]
#
#
# TEST_PARAMETERS
# ---------------
# Representation:
#     BuildParameterSpecification[]
#
# Format:
#     InputFileName|FrameworksParameterSpecification
#
# Examples:
#     [MSBuildProjectName].dll|net10.0;net9.0;net8.0;net48
#     [MSBuildProjectName].csproj|net10.0;net9.0;net8.0;net48
#
# Conversion:
#     BuildParameterSpecification[]
#         |
#         v
#     ConvertTo-BuildParameters
#         |
#         v
#     BuildParameter[]
#
#
# PACKAGE_PARAMETERS
# ------------------
# Representation:
#     BuildParameterSpecification[]
#
# Format:
#     InputFileName|FrameworksParameterSpecification
#
# Examples:
#     [MSBuildProjectName].csproj|net10.0;net9.0;net8.0;net48
#
# Conversion:
#     BuildParameterSpecification[]
#         |
#         v
#     ConvertTo-BuildParameters
#         |
#         v
#     BuildParameter[]
#
#
# PACKAGE_NEV_PARAMETERS
# ----------------------
# Representation:
#     string[]
#
# Format:
#     Package ID prefix filters
#
# Examples:
#     [MSBuildProjectName]
#
# Conversion:
#     No conversion required.
#
# The value is already a normalized string collection and can be passed
# directly to package dependency version update operations.
#
#
# BuildParameterSpecification and BuildParameter
# ----------------------------------------------
# * Parameters containing InputFileName and Frameworks
#   use the BuildParameterSpecification format and require conversion to BuildParameter.
# * Parameters containing only package identifiers are already represented as
#   string collections and must not be converted through BuildParameter logic.
#
# The conversion boundary is Invoke-BuildUnit:
#     Build unit parameter specifications (BuildUnitParameterSpecification[])
#             |
#             +-- BUILD_PARAMETERS
#             |       ConvertTo-BuildParameters
#             |
#             +-- TEST_PARAMETERS
#             |       ConvertTo-BuildParameters
#             |
#             +-- PACKAGE_PARAMETERS
#             |       ConvertTo-BuildParameters
#             |
#             +-- PACKAGE_NEV_PARAMETERS
#                     Pass through
#
# Execution phases consume converted values only:
#     Invoke-Build
#         BuildParameter[]
#
#     Invoke-Test
#         BuildParameter[]
#
#     Invoke-Package
#         BuildParameter[]
#         string[]
function Invoke-BuildUnit {
    param(
        [Parameter(Mandatory = $true)]
        [object] $BuildUnitParameterSpecification
    )

    Invoke-Build `
        -BuildParameters (
            ConvertTo-BuildParameters $BuildUnitParameterSpecification.BUILD_PARAMETERS
        )

    Write-Host

    Invoke-Test `
        -TestParameters (
            ConvertTo-BuildParameters $BuildUnitParameterSpecification.TEST_PARAMETERS
        )

    Write-Host

    Invoke-Package `
        -PackageParameters (
            ConvertTo-BuildParameters $BuildUnitParameterSpecification.PACKAGE_PARAMETERS
        ) `
        -PackageNevParameters (
            $BuildUnitParameterSpecification.PACKAGE_NEV_PARAMETERS
        )

    Write-Host
}

function Invoke-Build {
    param(
        [Parameter(Mandatory = $true)]
        [BuildParameter[]] $BuildParameters
    )

    if (-not $script:PARAMETER___RUN_BUILD) {
        Write-Host 'Skipping build operations because --no-build was specified.'
        return
    }

    $buildParameter_Index = 0

    foreach ($buildParameter in $BuildParameters) {
        $buildParameter_InputFileName = $buildParameter.InputFileName
        $buildParameter_Frameworks = $buildParameter.Frameworks

        if (-not (Test-HasValue $buildParameter_InputFileName)) {
            Write-Host "Build input file name is not defined for build unit `"$script:BUILD_UNIT`" build parameter #$buildParameter_Index."
            exit 4
        }

        if ($buildParameter_Frameworks.Count -eq 0) {
            Write-Host "Build input file `"$($buildParameter_InputFileName)`" does not define any frameworks."
            exit 5
        }

        $buildParameter_InputFilePath = Resolve-BuildInputFilePath $buildParameter_InputFileName

        if (-not (Test-HasValue $buildParameter_InputFilePath)) {
            Write-Host "Build input file `"$($buildParameter_InputFileName)`" could not be resolved."
            exit 4
        }

        if (Test-UsingDefaultFrameworks) {
            Write-Host '--------------------------------------------------------------------------------'
            Write-Host "Building `"$($buildParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | project-defined frameworks)..."
            Write-Host '--------------------------------------------------------------------------------'

            $dotnetArguments = @(
                'build'
                $buildParameter_InputFilePath
                "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                '--configuration'
                $script:PARAMETER___CONFIGURATION
                "--property:ENABLE_SOURCE_LINK=$script:PARAMETER___ENABLE_SOURCE_LINK"
                '--property:GeneratePackageOnBuild=false'
            )

            Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetArguments -FailureExitCode 6
        }
        else {
            $frameworks = ConvertTo-Frameworks $script:PARAMETER___FRAMEWORKS

            $resolvedFrameworks = Resolve-Frameworks -AvailableFrameworks $buildParameter_Frameworks -RequestedFrameworks $frameworks

            if ($resolvedFrameworks.Count -eq 0) {
                Write-Host "Skipping build operations for build input file `"$($buildParameter_InputFileName)`" because it does not define any frameworks that match the requested frameworks: `"$script:PARAMETER___FRAMEWORKS`"."

                $buildParameter_Index++

                continue
            }

            foreach ($buildFramework in $resolvedFrameworks) {
                Write-Host '--------------------------------------------------------------------------------'
                Write-Host "Building `"$($buildParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | $buildFramework)..."
                Write-Host '--------------------------------------------------------------------------------'

                $dotnetArguments = @(
                    'build'
                    $buildParameter_InputFilePath
                    "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                    '--configuration'
                    $script:PARAMETER___CONFIGURATION
                    '--framework'
                    $buildFramework
                    "--property:ENABLE_SOURCE_LINK=$script:PARAMETER___ENABLE_SOURCE_LINK"
                    '--property:GeneratePackageOnBuild=false'
                )

                Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetArguments -FailureExitCode 6
            }
        }

        $buildParameter_Index++
    }
}

function Invoke-Test {
    param(
        [AllowNull()]
        [BuildParameter[]] $TestParameters
    )

    if ($TestParameters.Count -eq 0) {
        Write-Host "Skipping test operations because no test parameters defined for build unit `"$script:BUILD_UNIT`"."
        return
    }

    if (-not $script:PARAMETER___RUN_TEST) {
        Write-Host 'Skipping test operations because --no-test was specified.'
        return
    }

    $script:ARTIFACTS___OUTPUT___CONFIGURATION___FOLDER_PATH = Join-Path -Path $ARTIFACTS___OUTPUT___FOLDER_PATH -ChildPath $script:PARAMETER___CONFIGURATION
    $script:ARTIFACTS___TEST_RESULTS___CONFIGURATION___FOLDER_PATH = Join-Path -Path $ARTIFACTS___TEST_RESULTS___FOLDER_PATH -ChildPath $script:PARAMETER___CONFIGURATION

    $testParameter_Index = 0

    foreach ($testParameter in $TestParameters) {
        $testParameter_InputFileName = $testParameter.InputFileName
        $testParameter_Frameworks = $testParameter.Frameworks

        if (-not (Test-HasValue $testParameter_InputFileName)) {
            Write-Host "Test input file name is not defined for build unit `"$script:BUILD_UNIT`" test parameter #$testParameter_Index."
            exit 7
        }

        if ($testParameter_Frameworks.Count -eq 0) {
            Write-Host "Test input file `"$($testParameter_InputFileName)`" does not define any frameworks."
            exit 8
        }

        $testParameter_InputFileExtension = Get-FileExtension $testParameter_InputFileName
        $testParameter_InputFileNameWithoutExtension = Get-FileNameWithoutExtension $testParameter_InputFileName

        if (Test-UsingDefaultFrameworks) {
            $resolvedFrameworks = $testParameter_Frameworks
        }
        else {
            $frameworks = ConvertTo-Frameworks $script:PARAMETER___FRAMEWORKS

            $resolvedFrameworks = Resolve-Frameworks -AvailableFrameworks $testParameter_Frameworks -RequestedFrameworks $frameworks

            if ($resolvedFrameworks.Count -eq 0) {
                Write-Host "Skipping test operations for test input file `"$($testParameter_InputFileName)`" because it does not define any frameworks that match the requested frameworks: `"$script:PARAMETER___FRAMEWORKS`"."

                $testParameter_Index++

                continue
            }
        }

        foreach ($testFramework in $resolvedFrameworks) {
            $testInputFilePath = Resolve-TestInputFilePath $testParameter_InputFileName $testFramework

            if (-not (Test-HasValue $testInputFilePath)) {
                Write-Host "Test input file `"$($testParameter_InputFileName)`" could not be resolved."
                exit 7
            }

            Write-Host '--------------------------------------------------------------------------------'
            Write-Host "Testing `"$($testParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | $testFramework)..."
            Write-Host '--------------------------------------------------------------------------------'

            $nunitLogger = "nunit;LogFileName=$($testParameter_InputFileNameWithoutExtension)_$($script:PARAMETER___VERSION)_$($script:PARAMETER___CONFIGURATION)_$($testFramework)_TestResults-NUnit.xml;format=nunit3"
            $liquidLogger = "liquid.md;LogFileName=$($testParameter_InputFileNameWithoutExtension)_$($script:PARAMETER___VERSION)_$($script:PARAMETER___CONFIGURATION)_$($testFramework)_TestResults-Liquid.md"

            if ($testParameter_InputFileExtension -ieq '.dll') {
                $dotnetArguments = @(
                    'test'
                    $testInputFilePath
                    '--framework'
                    $testFramework
                    '--results-directory'
                    $script:ARTIFACTS___TEST_RESULTS___CONFIGURATION___FOLDER_PATH
                    '--logger'
                    $nunitLogger
                    '--logger'
                    $liquidLogger
                )
            }
            else {
                $dotnetArguments = @(
                    'test'
                    $testInputFilePath
                    "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                    '--configuration'
                    $script:PARAMETER___CONFIGURATION
                    '--framework'
                    $testFramework
                    "--property:ENABLE_SOURCE_LINK=$script:PARAMETER___ENABLE_SOURCE_LINK"
                    '--property:GeneratePackageOnBuild=false'
                    '--results-directory'
                    $script:ARTIFACTS___TEST_RESULTS___CONFIGURATION___FOLDER_PATH
                    '--logger'
                    $nunitLogger
                    '--logger'
                    $liquidLogger
                )
            }

            Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetArguments -FailureExitCode 9
        }

        $testParameter_Index++
    }
}

function Invoke-Package {
    param(
        [AllowNull()]
        [BuildParameter[]] $PackageParameters,

        [AllowNull()]
        [string[]] $PackageNevParameters
    )

    if ($PackageParameters.Count -eq 0) {
        Write-Host "Skipping package operations because no package parameters defined for build unit `"$script:BUILD_UNIT`"."
        return
    }

    if (-not $script:PARAMETER___RUN_PACKAGE) {
        Write-Host 'Skipping package operations because --no-package was specified.'
        return
    }

    $script:ARTIFACTS___PACKAGE_OUTPUT___CONFIGURATION___FOLDER_PATH = Join-Path -Path $ARTIFACTS___PACKAGE_OUTPUT___FOLDER_PATH -ChildPath $script:PARAMETER___CONFIGURATION

    $packageParameter_Index = 0

    $packageParameter_PackageOperationsPerformed = $false

    foreach ($packageParameter in $PackageParameters) {
        $packageParameter_InputFileName = $packageParameter.InputFileName
        $packageParameter_Frameworks = $packageParameter.Frameworks

        if (-not (Test-HasValue $packageParameter_InputFileName)) {
            Write-Host "Package input file name is not defined for build unit `"$script:BUILD_UNIT`" package parameter #$packageParameter_Index."
            exit 10
        }

        if ($packageParameter_Frameworks.Count -eq 0) {
            Write-Host "Package input file `"$($packageParameter_InputFileName)`" does not define any frameworks."
            exit 11
        }

        $packageParameter_InputFilePath = Resolve-PackageInputFilePath $packageParameter_InputFileName

        if (-not (Test-HasValue $packageParameter_InputFilePath)) {
            Write-Host "Package input file `"$($packageParameter_InputFileName)`" could not be resolved."
            exit 10
        }

        if (Test-UsingDefaultFrameworks) {
            Write-Host '--------------------------------------------------------------------------------'
            Write-Host "Packaging `"$($packageParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | project-defined frameworks)..."
            Write-Host '--------------------------------------------------------------------------------'

            if ($script:PARAMETER___RUN_BUILD) {

                $dotnetArguments = @(
                    'pack'
                    $packageParameter_InputFilePath
                    "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                    '--configuration'
                    $script:PARAMETER___CONFIGURATION
                    '--no-build'
                )

                Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetArguments -FailureExitCode 12
            }
            else {
                $dotnetArguments = @(
                    'pack'
                    $packageParameter_InputFilePath
                    "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                    '--configuration'
                    $script:PARAMETER___CONFIGURATION
                    "--property:ENABLE_SOURCE_LINK=$script:PARAMETER___ENABLE_SOURCE_LINK"
                    '--property:GeneratePackageOnBuild=false'
                )

                Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetArguments -FailureExitCode 12
            }

            $packageParameter_PackageOperationsPerformed = $true
        }
        else {
            $frameworks = ConvertTo-Frameworks $script:PARAMETER___FRAMEWORKS

            $resolvedFrameworks = Resolve-Frameworks -AvailableFrameworks $packageParameter_Frameworks -RequestedFrameworks $frameworks

            if ($resolvedFrameworks.Count -eq 0) {
                Write-Host "Skipping package operations for package input file `"$($packageParameter_InputFileName)`" because it does not define any frameworks that match the requested frameworks: `"$script:PARAMETER___FRAMEWORKS`"."

                $packageParameter_Index++

                continue
            }

            Write-Host '--------------------------------------------------------------------------------'
            Write-Host "Packaging `"$($packageParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | project-defined frameworks)..."
            Write-Host '--------------------------------------------------------------------------------'

            $dotnetArguments = @(
                'pack'
                $packageParameter_InputFilePath
                "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                '--configuration'
                $script:PARAMETER___CONFIGURATION
                "--property:ENABLE_SOURCE_LINK=$script:PARAMETER___ENABLE_SOURCE_LINK"
                '--property:GeneratePackageOnBuild=false'
            )

            Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetArguments -FailureExitCode 12

            $packageParameter_PackageOperationsPerformed = $true
        }

        $packageParameter_Index++
    }

    if ($null -eq $PackageNevParameters -or
        $PackageNevParameters.Count -eq 0) {
        Write-Host "Skipping package dependency version update operations because no package dependency version update parameters defined for build unit `"$script:BUILD_UNIT`"."
        return
    }

    if (-not $packageParameter_PackageOperationsPerformed) {
        Write-Host "Skipping package dependency version update operations because no package operations were performed for build unit `"$script:BUILD_UNIT`"."
        return
    }

    $nevSolutionFilePathSegments = @(
        'tools',
        'Explicit.NuGet.Versions',
        'Explicit.NuGet.Versions.slnx'
    )

    $nevSolutionFilePath = Join-FolderPath -BaseFolderPath $WORKSPACE_FOLDER_PATH -ChildPath $nevSolutionFilePathSegments

    $dotnetArguments = @(
        'build'
        $nevSolutionFilePath
        '--configuration'
        'Release'
    )

    Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetArguments -FailureExitCode 13 -SuppressStandardOutput

    foreach ($packageNevParameter in $PackageNevParameters) {
        $packageNevParameter_DependencyPackageIdPrefixFilter = $packageNevParameter.Trim().Trim('"')

        Write-Host '--------------------------------------------------------------------------------'
        Write-Host "Updating package dependency versions for `"$packageNevParameter_DependencyPackageIdPrefixFilter`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION)..."
        Write-Host '--------------------------------------------------------------------------------'

        $packageNevExecutableFileName =
            if ($IsWindows) {
                'nev.exe'
            }
            else {
                'nev'
            }

        $packageNevCommandPath = Join-FolderPath -BaseFolderPath $ARTIFACTS_FOLDER_PATH -ChildPath @('tools', 'nev', $packageNevExecutableFileName)

        $packageNevArguments = @(
            $script:ARTIFACTS___PACKAGE_OUTPUT___CONFIGURATION___FOLDER_PATH
            $packageNevParameter_DependencyPackageIdPrefixFilter
        )

        Invoke-ExternalCommand -FilePath $packageNevCommandPath -ArgumentList $packageNevArguments -FailureExitCode 14
    }
}

function ConvertTo-BuildParameters {
    param(
        [AllowNull()]
        [string[]] $BuildParameterSpecifications
    )

    return @(
        foreach ($buildParameterSpecification in $BuildParameterSpecifications) {
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

function Resolve-BuildInputFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $InputFileName
    )

    $fileExtension = Get-FileExtension $InputFileName
    $fileNameWithoutExtension = Get-FileNameWithoutExtension $InputFileName

    if ($fileExtension -iin @('.slnx', '.sln')) {
        return Join-Path -Path $WORKSPACE_FOLDER_PATH -ChildPath $InputFileName
    }

    if ($fileExtension -iin @('.csproj', '.fsproj', '.vbproj')) {
        return Join-FolderPath -BaseFolderPath $SOURCE_CODE_FOLDER_PATH -ChildPath @($fileNameWithoutExtension, $InputFileName)
    }

    Write-Host "Unsupported build input file name `"$InputFileName`"."
    Write-Host 'Supported build input file extensions are .slnx, .sln, .csproj, .fsproj, and .vbproj.'
    return $null
}

function Resolve-TestInputFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $InputFileName,

        [Parameter(Mandatory = $true)]
        [string] $Framework
    )

    $fileExtension = Get-FileExtension $InputFileName
    $fileNameWithoutExtension = Get-FileNameWithoutExtension $InputFileName

    if ($fileExtension -ieq '.dll') {
        return Join-FolderPath -BaseFolderPath $script:ARTIFACTS___OUTPUT___CONFIGURATION___FOLDER_PATH -ChildPath @($Framework, $fileNameWithoutExtension, $InputFileName)
    }

    if ($fileExtension -iin @('.slnx', '.sln')) {
        return Join-Path -Path $WORKSPACE_FOLDER_PATH -ChildPath $InputFileName
    }

    if ($fileExtension -iin @('.csproj', '.fsproj', '.vbproj')) {
        return Join-FolderPath -BaseFolderPath $SOURCE_CODE_FOLDER_PATH -ChildPath @($fileNameWithoutExtension, $InputFileName)
    }

    Write-Host "Unsupported test input file name `"$InputFileName`"."
    Write-Host 'Supported test input file extensions are .dll, .slnx, .sln, .csproj, .fsproj, and .vbproj.'
    return $null
}

function Resolve-PackageInputFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $InputFileName
    )

    $fileExtension = Get-FileExtension $InputFileName
    $fileNameWithoutExtension = Get-FileNameWithoutExtension $InputFileName

    if ($fileExtension -iin @('.slnx', '.sln')) {
        return Join-Path -Path $WORKSPACE_FOLDER_PATH -ChildPath $InputFileName
    }

    if ($fileExtension -iin @('.csproj', '.fsproj', '.vbproj')) {
        return Join-FolderPath -BaseFolderPath $SOURCE_CODE_FOLDER_PATH -ChildPath @($fileNameWithoutExtension, $InputFileName)
    }

    Write-Host "Unsupported package input file name `"$InputFileName`"."
    Write-Host 'Supported package input file extensions are .slnx, .sln, .csproj, .fsproj, and .vbproj.'
    return $null
}

function Resolve-Frameworks {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $AvailableFrameworks,

        [Parameter(Mandatory = $true)]
        [string[]] $RequestedFrameworks
    )

    $resolvedFrameworks = New-Object System.Collections.Generic.List[string]

    foreach ($requestedFramework in $requestedFrameworks) {
        $exactMatchFound = $false

        foreach ($availableFramework in $availableFrameworks) {
            if ([string]::Equals($availableFramework, $requestedFramework, [System.StringComparison]::OrdinalIgnoreCase)) {
                $resolvedFrameworks.Add($availableFramework)
                $exactMatchFound = $true
            }
        }

        if (-not $exactMatchFound) {
            foreach ($availableFramework in $availableFrameworks) {
                if ($availableFramework.StartsWith($requestedFramework, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $resolvedFrameworks.Add($availableFramework)
                }
            }
        }
    }

    if ($resolvedFrameworks.Count -eq 0) {
        Write-Host '  No matching frameworks found.'
        Write-Host "    Available frameworks: `"$($AvailableFrameworks -join ';')`""
        Write-Host "    Requested frameworks: `"$($RequestedFrameworks -join ';')`""
        return $null
    }

    return $resolvedFrameworks.ToArray()
}

function Test-UsingDefaultFrameworks {
    return $script:PARAMETER___FRAMEWORKS -ieq $PARAMETER___FRAMEWORKS___DEFAULT
}

function Invoke-ExternalCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FilePath,

        [Parameter(Mandatory = $true)]
        [string[]] $ArgumentList,

        [Parameter(Mandatory = $true)]
        [int] $FailureExitCode,

        [switch] $SuppressStandardOutput
    )

    if ($SuppressStandardOutput) {
        & $FilePath @ArgumentList 1>$null
    }
    else {
        & $FilePath @ArgumentList
    }

    if ($LASTEXITCODE -ne 0) {
        exit $FailureExitCode
    }
}

function Join-FolderPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BaseFolderPath,

        [Parameter(Mandatory = $true)]
        [string[]] $ChildPath
    )

    $folderPath = $BaseFolderPath

    foreach ($pathSegment in $ChildPath) {
        $folderPath = Join-Path -Path $folderPath -ChildPath $pathSegment
    }

    return $folderPath
}

function Get-FileNameWithoutExtension {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FileName
    )

    return [System.IO.Path]::GetFileNameWithoutExtension($FileName)
}

function Get-FileExtension {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FileName
    )

    return [System.IO.Path]::GetExtension($FileName)
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


if ([string]::IsNullOrWhiteSpace($ARTIFACTS_FOLDER_PATH)) {
    exit 1
}


Initialize-Arguments $RemainingArguments

Initialize-Parameters

if ($null -eq $BUILD_UNITS -or
    @($BUILD_UNITS).Count -eq 0) {
    exit 1
}

Write-Host

foreach ($script:BUILD_UNIT in $BUILD_UNITS) {
    Write-Host '================================================================================'
    Write-Host "Build Unit `"$script:BUILD_UNIT`""
    Write-Host '================================================================================'

    $buildUnitParameterSpecification = Get-BuildUnitParameterSpecification $script:BUILD_UNIT
    Invoke-BuildUnit $buildUnitParameterSpecification
}

exit 0
