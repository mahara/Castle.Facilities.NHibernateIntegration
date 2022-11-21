#Requires -Version 7.0


# ================================
# ARCHITECTURAL FOUNDATIONS
# ================================
#
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
#
# ========================
# EXECUTION MODEL
# ========================
#
# This script builds, tests, and packages build units defined in build configuration.
#
# The main stages of execution are build configuration initialization,
# parameter resolution and validation,
# and execution of build, test, and package operations of all build units.
#
# Build configuration, or simply configuration, defines what operations are performed
# for all build units through their corresponding parameters.
#
# The Configuration Model describes this configuration using configuration-level vocabulary.
# The Configuration Execution Model represents the same configuration for execution
# using execution-level vocabulary.
#
# Resolution and validation establish valid execution inputs before execution begins.
#
#
# ========================
# CONFIGURATION MODEL
# ========================
#
# The Configuration Model describes the build configuration
# using configuration-level vocabulary.
#
# Configuration-level vocabulary consists of BUILD_* names,
# including BUILD_UNITS, BUILD_UNIT_PARAMETERS,
# BUILD_UNIT_PARAMETER, and *_PARAMETERS.
#
# The conceptual types describe the meaning of configuration elements
# and do not imply corresponding implementation types.
#
# The build configuration is organized around build units and their related parameters:
#
#     BUILD_UNITS
#
#     BUILD_UNIT_PARAMETERS
#         ├── <BUILD_UNIT>
#         │     └── <BUILD_UNIT_PARAMETER>
#         │           ├── BUILD_PARAMETERS
#         │           ├── TEST_PARAMETERS
#         │           ├── PACKAGE_PARAMETERS
#         │           └── PACKAGE_NEV_PARAMETERS
#         │
#         ├── <BUILD_UNIT>
#         │     └── <BUILD_UNIT_PARAMETER>
#         │           ├── BUILD_PARAMETERS
#         │           ├── TEST_PARAMETERS
#         │           ├── PACKAGE_PARAMETERS
#         │           └── PACKAGE_NEV_PARAMETERS
#         │
#         └── ...
#
#
# BUILD_UNITS defines the build units to process.
# BUILD_UNIT_PARAMETERS defines build unit parameters for all build units.
# Each BUILD_UNIT has exactly one BUILD_UNIT_PARAMETER.
#
# A BUILD_UNIT_PARAMETER defines the following parameter groups
# for the operations performed for that build unit:
# - BUILD_PARAMETERS
#   Parameters for building the build unit.
# - TEST_PARAMETERS
#   Parameters for testing the build unit.
# - PACKAGE_PARAMETERS
#   Parameters for packaging the build unit.
# - PACKAGE_NEV_PARAMETERS
#   Parameters for updating package dependency versions for the build unit.
#
#
# ================================
# CONFIGURATION EXECUTION MODEL
# ================================
#
# The Configuration Execution Model represents the same build configuration for execution.
# It is derived from the Configuration Model and uses execution-level vocabulary.
#
# Configuration Execution Model vocabulary consists of
# *ParameterSpecification and *Parameter types.
# Thus it represents parameters in build configuration in two forms:
# - *ParameterSpecification types represent the raw string-encoded form.
# - *Parameter types represent the strongly typed form.
#
# The Configuration Execution Model directly corresponds to the Configuration Model.
# A BuildUnit has exactly one BuildUnitParameterSpecification.
# A BuildUnitParameterSpecification represents the following parameter groups:
# BUILD_PARAMETERS, TEST_PARAMETERS, PACKAGE_PARAMETERS, and PACKAGE_NEV_PARAMETERS.
# BUILD_PARAMETERS, TEST_PARAMETERS, and PACKAGE_PARAMETERS are represented
# as arrays of BuildParameterSpecifications (BuildParameterSpecification[]).
# PACKAGE_NEV_PARAMETERS is represented as an array of strings (string[]).
#
# The following diagram illustrates the corresponding Configuration Execution Model representation
# of the Configuration Model defined in build configuration:
#
#     BUILD_UNITS                                     →   string[]
#
#     BUILD_UNIT_PARAMETERS                           →   BuildUnitParameterSpecification[]
#         └── <BUILD_UNIT>                            →   string
#               └── <BUILD_UNIT_PARAMETER>            →   BuildUnitParameterSpecification
#                     ├── BUILD_PARAMETERS            →   BuildParameterSpecification[]
#                     ├── TEST_PARAMETERS             →   BuildParameterSpecification[]
#                     ├── PACKAGE_PARAMETERS          →   BuildParameterSpecification[]
#                     └── PACKAGE_NEV_PARAMETERS      →   string[]
#
#
# ========================================
# CONFIGURATION MODEL DECOMPOSITION
# ========================================
#
# The Configuration Model is progressively decomposed
# into Configuration Execution Model representations for execution.
#
# The decomposition is also a staged derivation process:
# each Configuration Execution Model representation is derived
# from the preceding representation.
#
# Conversion is the process used to derive each subsequent representation
# when further decomposition is required.
#
# The Configuration Model is converted to the Configuration Execution Model first
# through the raw string-encoded form, and then through the strongly typed form as needed,
# before BuildUnit execution phases consume the resulting values.
#
# The decomposition and representation flow is:
#
#     Configuration Model
#             ↓
#     Configuration Execution Model (raw string-encoded form (*ParameterSpecification))
#             ↓
#     Configuration Execution Model (converted strongly typed form (*Parameter))
#             ↓
#     BuildUnit execution phases
#         ├── Invoke-Build
#         ├── Invoke-Test
#         └── Invoke-Package
#
#
# BUILD_UNIT_PARAMETERS is converted to BuildUnitParameterSpecification[]
# through Get-BuildUnitParameterSpecification for each BuildUnit.
#
# BuildUnitParameterSpecification (BUILD_UNIT_PARAMETER) parameter groups
# are first converted to their corresponding *ParameterSpecification representations.
# *ParameterSpecifications are then further converted to *Parameter representations as needed.
#
# Parameters containing InputFileName and Frameworks are represented
# as BuildParameterSpecification[] in the Configuration Execution Model,
# and are then converted to BuildParameter[] through ConvertTo-BuildParameters when needed,
# before being consumed by BuildUnit execution phases.
#
# Parameters containing only package ID prefix filters are represented
# as string[] in the Configuration Execution Model,
# and are then normalized through Normalize-StringCollection,
# before being consumed by BuildUnit execution phases.
#
# The following diagram illustrates the decomposition and representation flow
# of build configuration:
#
#     BUILD_UNIT_PARAMETERS                           →   BuildUnitParameterSpecification[]
#         │   Get-BuildUnitParameterSpecification
#         │
#         └── BuildUnitParameterSpecification
#               │
#               ├── BUILD_PARAMETERS                  →   BuildParameter[]
#               │         ConvertTo-BuildParameters
#               │
#               ├── TEST_PARAMETERS                   →   BuildParameter[]
#               │         ConvertTo-BuildParameters
#               │
#               ├── PACKAGE_PARAMETERS                →   BuildParameter[]
#               │         ConvertTo-BuildParameters
#               │
#               └── PACKAGE_NEV_PARAMETERS            →   string[]
#                         Normalize-StringCollection
#
#
# BuildUnit execution phases then consume the converted values above:
#
#     Invoke-Build
#         BuildParameter[]
#
#     Invoke-Test
#         BuildParameter[]
#
#     Invoke-Package
#         BuildParameter[]
#         string[]
#
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
#     [SolutionName].slnx|net10.0;net9.0;net8.0;net48
#     [MSBuildProjectName].csproj|net10.0;net9.0;net8.0;net48
#
# Conversion:
#     BuildParameterSpecification[]
#         ↓
#     ConvertTo-BuildParameters
#         ↓
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
#     [SolutionName].slnx|net10.0;net9.0;net8.0;net48
#     [MSBuildProjectName].dll|net10.0;net9.0;net8.0;net48
#     [MSBuildProjectName].csproj|net10.0;net9.0;net8.0;net48
#
# Conversion:
#     BuildParameterSpecification[]
#         ↓
#     ConvertTo-BuildParameters
#         ↓
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
#     [SolutionName].slnx|net10.0;net9.0;net8.0;net48
#     [MSBuildProjectName].csproj|net10.0;net9.0;net8.0;net48
#
# Conversion:
#     BuildParameterSpecification[]
#         ↓
#     ConvertTo-BuildParameters
#         ↓
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
#     No BuildParameter conversion is required.
#     Only normalization of string collection is performed through Normalize-StringCollection.
#
#
# ========================
# EXECUTION FLOW
# ========================
#
# The script resolves and validates its build configuration
# before executing operations for the defined BuildUnits.
#
# The execution flow is:
#
#     Build Packages
#         │
#         ├── Load build configuration
#         │
#         ├── Resolve command-line arguments
#         │
#         ├── Resolve parameters from the arguments
#         │
#         ├── Validate build-unit parameters from build configuration
#         │
#         └── Execute each BuildUnit
#               │
#               ├── Build
#               ├── Test
#               └── Package
#                     └── Update package dependency versions
#
#
# ================
# TERMINOLOGY
# ================
#
#     BUILD_UNITS (BuildUnits)
#     A collection of build units.
#     BuildUnits is both the Configuration Execution Model representation
#     and the collection name of BUILD_UNITS.
#
#     BUILD_UNIT (BuildUnit)
#     A single build unit derived from BUILD_UNITS/BuildUnits.
#     BuildUnit is the Configuration Execution Model representation of a BUILD_UNIT.
#
#     *ParameterSpecification
#     A Configuration Execution Model representation
#     of a *_PARAMETER from the Configuration Model in build configuration.
#
#     BUILD_UNIT_PARAMETERS (BuildUnitParameters/BuildUnitParameterSpecifications/BuildUnitParameterSpecification[])
#     A collection of BUILD_UNIT_PARAMETERs/BuildUnitParameterSpecifications of all build units.
#     BuildUnitParameters is the conceptual type of BUILD_UNIT_PARAMETERS.
#     BuildUnitParameterSpecifications is the collection name.
#     BuildUnitParameterSpecification[] is the implementation type.
#
#     BUILD_UNIT_PARAMETER (BuildUnitParameterSpecification)
#     A BUILD_UNIT_PARAMETER is a single build unit parameter
#     derived from BUILD_UNIT_PARAMETERS/BuildUnitParameters.
#     BuildUnitParameter is the conceptual type of BUILD_UNIT_PARAMETER.
#     A BuildUnitParameterSpecification is a Configuration Execution Model representation of a BUILD_UNIT_PARAMETER.
#     BUILD_UNIT_PARAMETER/BuildUnitParameterSpecification contains the following parameter groups:
#     BUILD_PARAMETERS, TEST_PARAMETERS, PACKAGE_PARAMETERS, and PACKAGE_NEV_PARAMETERS.
#
#     *_PARAMETERS
#     A collection of parameter groups belonging to a BUILD_UNIT_PARAMETER/BuildUnitParameterSpecification.
#
#     BuildParameterSpecification
#     A Configuration Execution Model representation of each element
#     of the following BUILD_UNIT_PARAMETER/BuildUnitParameterSpecification.*_PARAMETERS:
#     BUILD_PARAMETERS, TEST_PARAMETERS, and PACKAGE_PARAMETERS.
#     It defines an input file name and frameworks parameter specification (FrameworksParameterSpecification).
#
#     BuildParameter
#     A Configuration Execution Model representation of BuildParameterSpecification
#     containing an input file name and an array of FrameworkParameters/Frameworks (string[]).
#
#     FrameworksParameterSpecification
#     A Configuration Execution Model representation of a collection of target framework monikers (TFMs)
#     in a semicolon-separated string format (string).
#     For example: "net10.0;net9.0;net8.0;net48".
#
#     FrameworkParameters/Frameworks
#     A Configuration Execution Model representation of a collection of target framework monikers (TFMs)
#     derived from FrameworksParameterSpecification in a string array form (string[]).
#     FrameworkParameters is the conceptual type of FrameworksParameterSpecification.
#     Frameworks is the collection name.
#     For example: @("net10.0", "net9.0", "net8.0", "net48").
#
#     FrameworkParameter/Framework
#     A Configuration Execution Model representation of a single target framework moniker (TFM)
#     derived from FrameworksParameterSpecification in a string form (string).
#     For example: "net10.0".
#
#     exit code                         = script/subroutine result code.
#     $args                             = all original command-line arguments.
#     array splatting invocation        = safe external command argument passing.
#     Join-Path                         = file/folder path composition.
#     [System.IO.Path] helpers          = file-name parsing.
#
#
# ================
# EXIT CODES
# ================
# 0  = Build operations completed successfully.
# 1  = Missing required build infrastructure.
# 2  = Invalid command-line argument.
# 3  = Build units, build unit parameters, or build unit parameter's build parameters are not defined.
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
        return (
            '{0}|{1}' -f `
                $this.InputFileName,
                ($this.Frameworks -join ';')
        )
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
        Write-Host '  Supported arguments are --no-build, --no-test, --no-package, --version, --configuration, --framework, and --disable-source-link.'
        Write-Host
        exit 2
    }
}

function Resolve-ArgumentValue {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $ArgumentName,

        [AllowNull()]
        [string] $ArgumentValue
    )

    if ([string]::IsNullOrWhiteSpace($ArgumentValue)) {
        Write-Host "Missing value for $ArgumentName."
        Write-Host
        exit 2
    }

    if ($ArgumentValue.StartsWith('--', [System.StringComparison]::Ordinal)) {
        Write-Host "Missing value for $ArgumentName."
        Write-Host
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

function Validate-BuildUnitsParameters {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute(
        'PSUseApprovedVerbs',
        '',
        Justification = 'Validate accurately describes validating build units'' parameters from build configuration before execution; Test and Assert do not convey the same semantic intent.'
    )]

    param()

    if ($null -eq $BUILD_UNITS -or
        @($BUILD_UNITS).Count -eq 0) {

        Write-Host 'Build units must be defined.'
        Write-Host
        exit 3
    }

    foreach ($buildUnit in $BUILD_UNITS) {
        if ($null -eq $BUILD_UNIT_PARAMETERS -or
            -not $BUILD_UNIT_PARAMETERS.ContainsKey($buildUnit)) {

            Write-Host "Build unit `"$buildUnit`" is invalid because no build unit parameter is defined."
            Write-Host
            exit 3
        }

        $buildUnitParameterSpecification = Get-BuildUnitParameterSpecification $buildUnit

        if ($null -eq $buildUnitParameterSpecification.BUILD_PARAMETERS -or
            @($buildUnitParameterSpecification.BUILD_PARAMETERS).Count -eq 0) {

            Write-Host "Build unit `"$buildUnit`" is invalid because no build unit parameter's build parameters are defined."
            Write-Host
            exit 3
        }
    }
}

function Get-BuildUnitParameterSpecification {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $BuildUnit
    )

    return $BUILD_UNIT_PARAMETERS[$BuildUnit]
}

function Invoke-BuildUnit {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [object] $BuildUnitParameterSpecification
    )

    Invoke-Build `
        -BuildParameters @(
            ConvertTo-BuildParameters $BuildUnitParameterSpecification.BUILD_PARAMETERS
        )

    Write-Host

    Invoke-Test `
        -TestParameters @(
            ConvertTo-BuildParameters $BuildUnitParameterSpecification.TEST_PARAMETERS
        )

    Write-Host

    Invoke-Package `
        -PackageParameters @(
            ConvertTo-BuildParameters $BuildUnitParameterSpecification.PACKAGE_PARAMETERS
        ) `
        -PackageNevParameters @(
            Normalize-StringCollection $BuildUnitParameterSpecification.PACKAGE_NEV_PARAMETERS
        )

    Write-Host
}

function Invoke-Build {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
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
            Write-Host
            exit 4
        }

        if ($buildParameter_Frameworks.Count -eq 0) {
            Write-Host "Build input file `"$($buildParameter_InputFileName)`" does not define any frameworks."
            Write-Host
            exit 5
        }

        $buildParameter_InputFilePath = Resolve-BuildInputFilePath $buildParameter_InputFileName

        if (-not (Test-HasValue $buildParameter_InputFilePath)) {
            Write-Host "Build input file `"$($buildParameter_InputFileName)`" could not be resolved."
            Write-Host
            exit 4
        }

        if (Test-ParameterFrameworksDefault) {
            Write-Host '------------------------------------------------------------------------------------------------'
            Write-Host "Building `"$($buildParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | project-defined frameworks)..."
            Write-Host '------------------------------------------------------------------------------------------------'

            $dotnetCommandArguments = @(
                'build'
                $buildParameter_InputFilePath
                "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                '--configuration'
                $script:PARAMETER___CONFIGURATION
                "--property:ENABLE_SOURCE_LINK=$script:PARAMETER___ENABLE_SOURCE_LINK"
                '--property:GeneratePackageOnBuild=false'
            )

            Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetCommandArguments -FailureExitCode 6
        }
        else {
            $frameworks = @(ConvertTo-Frameworks $script:PARAMETER___FRAMEWORKS)

            $resolvedFrameworks = @(Resolve-Frameworks -AvailableFrameworks $buildParameter_Frameworks -RequestedFrameworks $frameworks)

            if ($resolvedFrameworks.Count -eq 0) {
                Write-Host "Skipping build operations for build input file `"$($buildParameter_InputFileName)`" because no defined frameworks match `"$script:PARAMETER___FRAMEWORKS`"."

                $buildParameter_Index++

                continue
            }

            foreach ($buildFramework in $resolvedFrameworks) {
                Write-Host '------------------------------------------------------------------------------------------------'
                Write-Host "Building `"$($buildParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | $buildFramework)..."
                Write-Host '------------------------------------------------------------------------------------------------'

                $dotnetCommandArguments = @(
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

                Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetCommandArguments -FailureExitCode 6
            }
        }

        $buildParameter_Index++
    }
}

function Invoke-Test {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [BuildParameter[]] $TestParameters
    )

    if (-not $script:PARAMETER___RUN_TEST) {
        Write-Host 'Skipping test operations because --no-test was specified.'
        return
    }

    if ($TestParameters.Count -eq 0) {
        Write-Host "Skipping test operations because no test parameters are defined for build unit `"$script:BUILD_UNIT`"."
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
            Write-Host
            exit 7
        }

        if ($testParameter_Frameworks.Count -eq 0) {
            Write-Host "Test input file `"$($testParameter_InputFileName)`" does not define any frameworks."
            Write-Host
            exit 8
        }

        $testParameter_InputFileExtension = Get-FileExtension $testParameter_InputFileName
        $testParameter_InputFileNameWithoutExtension = Get-FileNameWithoutExtension $testParameter_InputFileName

        if (Test-ParameterFrameworksDefault) {
            $resolvedFrameworks = $testParameter_Frameworks
        }
        else {
            $frameworks = @(ConvertTo-Frameworks $script:PARAMETER___FRAMEWORKS)

            $resolvedFrameworks = @(Resolve-Frameworks -AvailableFrameworks $testParameter_Frameworks -RequestedFrameworks $frameworks)

            if ($resolvedFrameworks.Count -eq 0) {
                Write-Host "Skipping test operations for test input file `"$($testParameter_InputFileName)`" because no defined frameworks match `"$script:PARAMETER___FRAMEWORKS`"."

                $testParameter_Index++

                continue
            }
        }

        foreach ($testFramework in $resolvedFrameworks) {
            $testInputFilePath = Resolve-TestInputFilePath $testParameter_InputFileName $testFramework

            if (-not (Test-HasValue $testInputFilePath)) {
                Write-Host "Test input file `"$($testParameter_InputFileName)`" could not be resolved."
                Write-Host
                exit 7
            }

            Write-Host '------------------------------------------------------------------------------------------------'
            Write-Host "Testing `"$($testParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | $testFramework)..."
            Write-Host '------------------------------------------------------------------------------------------------'

            $nunitLogger = "nunit;LogFileName=$($testParameter_InputFileNameWithoutExtension)_$($script:PARAMETER___VERSION)_$($script:PARAMETER___CONFIGURATION)_$($testFramework)_TestResults-NUnit.xml;format=nunit3"
            $liquidLogger = "liquid.md;LogFileName=$($testParameter_InputFileNameWithoutExtension)_$($script:PARAMETER___VERSION)_$($script:PARAMETER___CONFIGURATION)_$($testFramework)_TestResults-Liquid.md"

            if ($testParameter_InputFileExtension -ieq '.dll') {
                $dotnetCommandArguments = @(
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
                $dotnetCommandArguments = @(
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

            Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetCommandArguments -FailureExitCode 9
        }

        $testParameter_Index++
    }
}

function Invoke-Package {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [BuildParameter[]] $PackageParameters,

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [string[]] $PackageNevParameters
    )

    if (-not $script:PARAMETER___RUN_PACKAGE) {
        Write-Host 'Skipping package operations because --no-package was specified.'
        return
    }

    if ($PackageParameters.Count -eq 0) {
        Write-Host "Skipping package operations because no package parameters are defined for build unit `"$script:BUILD_UNIT`"."
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
            Write-Host
            exit 10
        }

        if ($packageParameter_Frameworks.Count -eq 0) {
            Write-Host "Package input file `"$($packageParameter_InputFileName)`" does not define any frameworks."
            Write-Host
            exit 11
        }

        $packageParameter_InputFilePath = Resolve-PackageInputFilePath $packageParameter_InputFileName

        if (-not (Test-HasValue $packageParameter_InputFilePath)) {
            Write-Host "Package input file `"$($packageParameter_InputFileName)`" could not be resolved."
            Write-Host
            exit 10
        }

        if (Test-ParameterFrameworksDefault) {
            Write-Host '------------------------------------------------------------------------------------------------'
            Write-Host "Packaging `"$($packageParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | project-defined frameworks)..."
            Write-Host '------------------------------------------------------------------------------------------------'

            if ($script:PARAMETER___RUN_BUILD) {

                $dotnetCommandArguments = @(
                    'pack'
                    $packageParameter_InputFilePath
                    "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                    '--configuration'
                    $script:PARAMETER___CONFIGURATION
                    '--no-build'
                )

                Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetCommandArguments -FailureExitCode 12
            }
            else {
                $dotnetCommandArguments = @(
                    'pack'
                    $packageParameter_InputFilePath
                    "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                    '--configuration'
                    $script:PARAMETER___CONFIGURATION
                    "--property:ENABLE_SOURCE_LINK=$script:PARAMETER___ENABLE_SOURCE_LINK"
                    '--property:GeneratePackageOnBuild=false'
                )

                Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetCommandArguments -FailureExitCode 12
            }

            $packageParameter_PackageOperationsPerformed = $true
        }
        else {
            $frameworks = @(ConvertTo-Frameworks $script:PARAMETER___FRAMEWORKS)

            $resolvedFrameworks = @(Resolve-Frameworks -AvailableFrameworks $packageParameter_Frameworks -RequestedFrameworks $frameworks)

            if ($resolvedFrameworks.Count -eq 0) {
                Write-Host "Skipping package operations for package input file `"$($packageParameter_InputFileName)`" because no defined frameworks match `"$script:PARAMETER___FRAMEWORKS`"."

                $packageParameter_Index++

                continue
            }

            Write-Host '------------------------------------------------------------------------------------------------'
            Write-Host "Packaging `"$($packageParameter_InputFileName)`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION | project-defined frameworks)..."
            Write-Host '------------------------------------------------------------------------------------------------'

            $dotnetCommandArguments = @(
                'pack'
                $packageParameter_InputFilePath
                "--property:BUILD_VERSION=$script:PARAMETER___VERSION"
                '--configuration'
                $script:PARAMETER___CONFIGURATION
                "--property:ENABLE_SOURCE_LINK=$script:PARAMETER___ENABLE_SOURCE_LINK"
                '--property:GeneratePackageOnBuild=false'
            )

            Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetCommandArguments -FailureExitCode 12

            $packageParameter_PackageOperationsPerformed = $true
        }

        $packageParameter_Index++
    }

    Write-Host

    if (-not $packageParameter_PackageOperationsPerformed) {
        Write-Host "  Skipping package dependency version update operations because no package operations were performed for build unit `"$script:BUILD_UNIT`"."
        return
    }

    if ($PackageNevParameters.Count -eq 0) {
        Write-Host "  Skipping package dependency version update operations because no such package parameters are defined for build unit `"$script:BUILD_UNIT`"."
        return
    }

    $packageNevSolutionFilePathSegments = @(
        'tools'
        'Explicit.NuGet.Versions'
        'Explicit.NuGet.Versions.slnx'
    )

    $packageNevSolutionFilePath = `
        Join-FolderPath `
            -BaseFolderPath $WORKSPACE_FOLDER_PATH `
            -ChildPath $packageNevSolutionFilePathSegments

    $dotnetCommandArguments = @(
        'build'
        $packageNevSolutionFilePath
        '--configuration'
        'Release'
    )

    Invoke-ExternalCommand -FilePath 'dotnet' -ArgumentList $dotnetCommandArguments -FailureExitCode 13 -SuppressStandardOutput

    foreach ($packageNevParameter in $PackageNevParameters) {
        $packageNevParameter_DependencyPackageIdPrefixFilter = $packageNevParameter.Trim().Trim('"')

        Write-Host '  ----------------------------------------------------------------------------------------------'
        Write-Host "  Updating package dependency versions for packages with package ID prefix `"$packageNevParameter_DependencyPackageIdPrefixFilter`" ($script:PARAMETER___VERSION | $script:PARAMETER___CONFIGURATION)..."
        Write-Host '  ----------------------------------------------------------------------------------------------'

        $packageNevExecutableFileName =
            if ($IsWindows) {
                'nev.exe'
            }
            else {
                'nev'
            }

        $packageNevCommandPathSegments = @(
            'tools'
            'nev'
            $packageNevExecutableFileName
        )

        $packageNevCommandPath = `
            Join-FolderPath `
                -BaseFolderPath $ARTIFACTS_FOLDER_PATH `
                -ChildPath $packageNevCommandPathSegments

        $packageNevCommandArguments = @(
            $script:ARTIFACTS___PACKAGE_OUTPUT___CONFIGURATION___FOLDER_PATH
            $packageNevParameter_DependencyPackageIdPrefixFilter
        )

        Invoke-ExternalCommand -FilePath $packageNevCommandPath -ArgumentList $packageNevCommandArguments -FailureExitCode 14 -SuppressStandardOutput
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
        [ValidateNotNullOrWhiteSpace()]
        [string] $BuildParameterSpecification
    )

    $parts = $BuildParameterSpecification -split '\|', 2

    return (
        [BuildParameter]::new(
            $parts[0].Trim(),
            (ConvertTo-Frameworks $parts[1])
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
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $InputFileName
    )

    $inputFileExtension = Get-FileExtension $InputFileName
    $inputFileNameWithoutExtension = Get-FileNameWithoutExtension $InputFileName

    if ($inputFileExtension -iin @('.slnx', '.sln')) {
        return Join-Path -Path $WORKSPACE_FOLDER_PATH -ChildPath $InputFileName
    }

    if ($inputFileExtension -iin @('.csproj', '.fsproj', '.vbproj')) {
        $inputFilePathSegments = @(
            $inputFileNameWithoutExtension
            $InputFileName
        )

        return (
            Join-FolderPath `
                -BaseFolderPath $SOURCE_CODE_FOLDER_PATH `
                -ChildPath $inputFilePathSegments
        )
    }

    Write-Host "  Unsupported build input file name `"$InputFileName`"."
    Write-Host '    Supported build input file extensions are .slnx, .sln, .csproj, .fsproj, and .vbproj.'
    return $null
}

function Resolve-TestInputFilePath {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $InputFileName,

        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $Framework
    )

    $inputFileExtension = Get-FileExtension $InputFileName
    $inputFileNameWithoutExtension = Get-FileNameWithoutExtension $InputFileName

    if ($inputFileExtension -ieq '.dll') {
        $inputFilePathSegments = @(
            $Framework
            $inputFileNameWithoutExtension
            $InputFileName
        )

        return (
            Join-FolderPath `
                -BaseFolderPath $script:ARTIFACTS___OUTPUT___CONFIGURATION___FOLDER_PATH `
                -ChildPath $inputFilePathSegments
        )
    }

    if ($inputFileExtension -iin @('.slnx', '.sln')) {
        return Join-Path -Path $WORKSPACE_FOLDER_PATH -ChildPath $InputFileName
    }

    if ($inputFileExtension -iin @('.csproj', '.fsproj', '.vbproj')) {
        $inputFilePathSegments = @(
            $inputFileNameWithoutExtension
            $InputFileName
        )

        return (
            Join-FolderPath `
                -BaseFolderPath $SOURCE_CODE_FOLDER_PATH `
                -ChildPath $inputFilePathSegments
        )
    }

    Write-Host "  Unsupported test input file name `"$InputFileName`"."
    Write-Host '    Supported test input file extensions are .dll, .slnx, .sln, .csproj, .fsproj, and .vbproj.'
    return $null
}

function Resolve-PackageInputFilePath {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $InputFileName
    )

    $inputFileExtension = Get-FileExtension $InputFileName
    $inputFileNameWithoutExtension = Get-FileNameWithoutExtension $InputFileName

    if ($inputFileExtension -iin @('.slnx', '.sln')) {
        return Join-Path -Path $WORKSPACE_FOLDER_PATH -ChildPath $InputFileName
    }

    if ($inputFileExtension -iin @('.csproj', '.fsproj', '.vbproj')) {
        $inputFilePathSegments = @(
            $inputFileNameWithoutExtension
            $InputFileName
        )

        return (
            Join-FolderPath `
                -BaseFolderPath $SOURCE_CODE_FOLDER_PATH `
                -ChildPath $inputFilePathSegments
        )
    }

    Write-Host "  Unsupported package input file name `"$InputFileName`"."
    Write-Host '    Supported package input file extensions are .slnx, .sln, .csproj, .fsproj, and .vbproj.'
    return $null
}

function Resolve-Frameworks {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [string[]] $AvailableFrameworks,

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
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
    }

    return $resolvedFrameworks.ToArray()
}

function Test-ParameterFrameworksDefault {
    return $script:PARAMETER___FRAMEWORKS -ieq $PARAMETER___FRAMEWORKS___DEFAULT
}

function Invoke-ExternalCommand {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $FilePath,

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
        [string[]] $ArgumentList,

        [Parameter(Mandatory)]
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
        Write-Host "External command `"$FilePath`" failed with exit code $LASTEXITCODE."
        Write-Host
        exit $FailureExitCode
    }
}

function Join-FolderPath {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $BaseFolderPath,

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [AllowEmptyCollection()]
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
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
        [string] $FileName
    )

    return [System.IO.Path]::GetFileNameWithoutExtension($FileName)
}

function Get-FileExtension {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrWhiteSpace()]
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
    Write-Host 'Artifacts folder path must be defined.'
    Write-Host
    exit 1
}


Initialize-Arguments $RemainingArguments

Initialize-Parameters

Validate-BuildUnitsParameters

foreach ($script:BUILD_UNIT in $BUILD_UNITS) {
    Write-Host '================================================================================================'
    Write-Host "Build Unit `"$script:BUILD_UNIT`""
    Write-Host '================================================================================================'
    Write-Host

    Invoke-BuildUnit (Get-BuildUnitParameterSpecification $script:BUILD_UNIT)
}


exit 0
