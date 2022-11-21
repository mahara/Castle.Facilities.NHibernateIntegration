@ECHO OFF


REM ================================
REM ARCHITECTURAL FOUNDATIONS
REM ================================
REM -   Design Principles
REM     Fundamental architectural rules.
REM
REM -   Common Behavior
REM     Shared workflow and semantics across implementations.
REM
REM -   Implementation Parity
REM     The .cmd and .ps1 implementations should exhibit the closest practical behavioral parity.
REM     Differences are permitted only when required by the underlying shell or platform capabilities.
REM
REM ================
REM TERMINOLOGY
REM ================
REM
REM     CALL :LABEL                         = batch subroutine call
REM     EXIT /B code                        = subroutine/script exit code; sets ERRORLEVEL
REM     IF ERRORLEVEL n                     = ERRORLEVEL threshold check
REM     second argument as variable name    = output parameter
REM     CALL SET %%dynamic_name%%           = indirect variable lookup
REM     FOR variable modifiers              = file-name parsing
REM     %%~nF                               = file name without extension
REM     %%~xF                               = file extension
REM     %~1                                 = argument dequoting
REM     !VAR!                               = delayed variable expansion inside blocks
REM
REM     %%U         = build unit item
REM     %%G         = generic parameter item
REM     %%H / %%I   = parsed items from parameter item
REM                   BUILD/TEST/PACKAGE: "input file name|frameworks"
REM     %%F         = parsed file-name items
REM     %%J         = selected framework item
REM
REM ================
REM EXIT CODES
REM ================
REM 0  = Build operations completed successfully.
REM 1  = Missing required build infrastructure.
REM 2  = Invalid command-line argument.
REM 3  = Build unit build parameters are not defined.
REM 4  = Build input file name is not defined, or file path resolution failed.
REM 5  = Build frameworks are not defined.
REM 6  = dotnet build failed.
REM 7  = Test input file name is not defined, or file path resolution failed.
REM 8  = Test frameworks are not defined.
REM 9  = dotnet test failed.
REM 10 = Package input file name is not defined, or file path resolution failed.
REM 11 = Package frameworks are not defined.
REM 12 = dotnet pack failed.
REM 13 = Explicit.NuGet.Versions build failed.
REM 14 = Explicit.NuGet.Versions execution failed.
REM
REM ================
REM REFERENCES
REM ================
REM https://superuser.com/questions/149951/does-in-batch-file-mean-all-command-line-arguments
REM https://stackoverflow.com/questions/15420004/write-batch-file-with-hyphenated-parameters
REM https://superuser.com/questions/1505178/parsing-command-line-argument-in-batch-file
REM https://ss64.com/nt/for.html
REM https://ss64.com/nt/for_f.html
REM https://stackoverflow.com/questions/2591758/batch-script-loop
REM https://stackoverflow.com/questions/46576996/how-to-use-for-loop-to-get-set-variable-by-batch-file
REM https://stackoverflow.com/questions/3294599/do-batch-files-support-multiline-variables
REM https://stackoverflow.com/questions/36228474/batch-file-if-string-starts-with
REM https://stackoverflow.com/questions/2952401/remove-trailing-slash-from-batch-file-input
REM https://stackoverflow.com/questions/1964192/removing-double-quotes-from-variables-in-batch-file-creates-problems-with-cmd-en



REM ================================================================================
REM Main
REM ================================================================================


IF NOT DEFINED ARTIFACTS_FOLDER_PATH EXIT /B 1


SETLOCAL EnableDelayedExpansion


:INITIALIZE_ARGUMENTS

IF "%~1" == "" GOTO INITIALIZE_PARAMETERS

IF /I "%~1" == "--no-build" (
    SET "ARGUMENT___RUN_BUILD=false"
    SHIFT
    GOTO INITIALIZE_ARGUMENTS
)

IF /I "%~1" == "--no-test" (
    SET "ARGUMENT___RUN_TEST=false"
    SHIFT
    GOTO INITIALIZE_ARGUMENTS
)

IF /I "%~1" == "--no-package" (
    SET "ARGUMENT___RUN_PACKAGE=false"
    SHIFT
    GOTO INITIALIZE_ARGUMENTS
)

IF /I "%~1" == "--version" (
    CALL :RESOLVE_ARGUMENT_VALUE "%~1" "%~2" ARGUMENT___VERSION
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
    SHIFT
    SHIFT
    GOTO INITIALIZE_ARGUMENTS
)

IF /I "%~1" == "--configuration" (
    CALL :RESOLVE_ARGUMENT_VALUE "%~1" "%~2" ARGUMENT___CONFIGURATION
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
    SHIFT
    SHIFT
    GOTO INITIALIZE_ARGUMENTS
)

IF /I "%~1" == "--framework" (
    CALL :RESOLVE_ARGUMENT_VALUE "%~1" "%~2" ARGUMENT___FRAMEWORKS
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
    SHIFT
    SHIFT
    GOTO INITIALIZE_ARGUMENTS
)

IF /I "%~1" == "--disable-source-link" (
    SET "ARGUMENT___ENABLE_SOURCE_LINK=false"
    SHIFT
    GOTO INITIALIZE_ARGUMENTS
)

ECHO Unknown argument "%~1".
ECHO Supported arguments are --no-build, --no-test, --no-package, --version, --configuration, --framework, and --disable-source-link.
EXIT /B 2


:INITIALIZE_PARAMETERS

SET "PARAMETER___RUN_BUILD=%PARAMETER___RUN_BUILD___DEFAULT%"
IF DEFINED ARGUMENT___RUN_BUILD (
    SET "PARAMETER___RUN_BUILD=%ARGUMENT___RUN_BUILD%"
)

SET "PARAMETER___RUN_TEST=%PARAMETER___RUN_TEST___DEFAULT%"
IF DEFINED ARGUMENT___RUN_TEST (
    SET "PARAMETER___RUN_TEST=%ARGUMENT___RUN_TEST%"
)

SET "PARAMETER___RUN_PACKAGE=%PARAMETER___RUN_PACKAGE___DEFAULT%"
IF DEFINED ARGUMENT___RUN_PACKAGE (
    SET "PARAMETER___RUN_PACKAGE=%ARGUMENT___RUN_PACKAGE%"
)

SET "PARAMETER___VERSION=%PARAMETER___VERSION___DEFAULT%"
IF DEFINED ARGUMENT___VERSION (
    SET "PARAMETER___VERSION=%ARGUMENT___VERSION%"
)

SET "PARAMETER___CONFIGURATION=%PARAMETER___CONFIGURATION___DEFAULT%"
IF DEFINED ARGUMENT___CONFIGURATION (
    SET "PARAMETER___CONFIGURATION=%ARGUMENT___CONFIGURATION%"
)

SET "PARAMETER___FRAMEWORKS=%PARAMETER___FRAMEWORKS___DEFAULT%"
IF DEFINED ARGUMENT___FRAMEWORKS (
    SET "PARAMETER___FRAMEWORKS=%ARGUMENT___FRAMEWORKS%"
)

SET "PARAMETER___ENABLE_SOURCE_LINK=%PARAMETER___ENABLE_SOURCE_LINK___DEFAULT%"
IF DEFINED ARGUMENT___ENABLE_SOURCE_LINK (
    SET "PARAMETER___ENABLE_SOURCE_LINK=%ARGUMENT___ENABLE_SOURCE_LINK%"
)


:INVOKE_BUILD_UNITS

IF NOT DEFINED BUILD_UNITS EXIT /B 1

ECHO.

FOR %%U IN (%BUILD_UNITS%) DO (
    SET "BUILD_UNIT=%%~U"

    ECHO ================================================================================
    ECHO Build Unit "!BUILD_UNIT!"
    ECHO ================================================================================

    CALL :INITIALIZE_BUILD_UNIT_PARAMETERS "!BUILD_UNIT!"
    IF ERRORLEVEL 1 EXIT /B !ERRORLEVEL!

    CALL :INVOKE_BUILD_UNIT
    IF ERRORLEVEL 1 EXIT /B !ERRORLEVEL!

    CALL :UNINITIALIZE_BUILD_UNIT_PARAMETERS "!BUILD_UNIT!"
    IF ERRORLEVEL 1 EXIT /B !ERRORLEVEL!
)

EXIT /B 0



REM ================================================================================
REM Workflow Operations
REM ================================================================================


:RESOLVE_ARGUMENT_VALUE

IF "%~2" == "" (
    ECHO Missing value for %~1.
    EXIT /B 2
)

SET "ARGUMENT_VALUE=%~2"

IF "!ARGUMENT_VALUE:~0,2!" == "--" (
    ECHO Missing value for %~1.
    EXIT /B 2
)

SET "%~3=%~2"

EXIT /B 0


REM Build unit parameter dependency model:
REM
REM BUILD_UNIT
REM     requires BUILD_PARAMETERS
REM
REM     optional TEST_PARAMETERS
REM         depends on BUILD_PARAMETERS
REM
REM     optional PACKAGE_PARAMETERS
REM         depends on BUILD_PARAMETERS
REM
REM         optional PACKAGE_NEV_PARAMETERS
REM             depends on PACKAGE_PARAMETERS
REM
REM PACKAGE_NEV_PARAMETERS is package post-processing.
REM It must not run without PACKAGE_PARAMETERS because no package operation boundary exists.

:INITIALIZE_BUILD_UNIT_PARAMETERS

SET "BUILD_PARAMETERS="
SET "TEST_PARAMETERS="
SET "PACKAGE_PARAMETERS="
SET "PACKAGE_NEV_PARAMETERS="

REM Load build-unit-specific variables into generic working variables.
REM CALL SET performs a second expansion pass, allowing indirect variable lookup.
CALL SET BUILD_PARAMETERS=%%BUILD_UNIT___!BUILD_UNIT!___BUILD_PARAMETERS%%
CALL SET TEST_PARAMETERS=%%BUILD_UNIT___!BUILD_UNIT!___TEST_PARAMETERS%%
CALL SET PACKAGE_PARAMETERS=%%BUILD_UNIT___!BUILD_UNIT!___PACKAGE_PARAMETERS%%
CALL SET PACKAGE_NEV_PARAMETERS=%%BUILD_UNIT___!BUILD_UNIT!___PACKAGE_NEV_PARAMETERS%%

IF NOT DEFINED BUILD_PARAMETERS (
    ECHO Build unit "!BUILD_UNIT!" is invalid because no build parameters defined.
    EXIT /B 3
)

EXIT /B 0


:UNINITIALIZE_BUILD_UNIT_PARAMETERS

SET "BUILD_UNIT___!BUILD_UNIT!___BUILD_PARAMETERS="
SET "BUILD_UNIT___!BUILD_UNIT!___TEST_PARAMETERS="
SET "BUILD_UNIT___!BUILD_UNIT!___PACKAGE_PARAMETERS="
SET "BUILD_UNIT___!BUILD_UNIT!___PACKAGE_NEV_PARAMETERS="

SET "BUILD_PARAMETERS="
SET "TEST_PARAMETERS="
SET "PACKAGE_PARAMETERS="
SET "PACKAGE_NEV_PARAMETERS="

EXIT /B 0


:INVOKE_BUILD_UNIT

ECHO.

CALL :INVOKE_BUILD
IF ERRORLEVEL 1 EXIT /B !ERRORLEVEL!

ECHO.

CALL :INVOKE_TEST
IF ERRORLEVEL 1 EXIT /B !ERRORLEVEL!

ECHO.

CALL :INVOKE_PACKAGE
IF ERRORLEVEL 1 EXIT /B !ERRORLEVEL!

ECHO.

EXIT /B 0


:INVOKE_BUILD

IF /I "!PARAMETER___RUN_BUILD!" == "false" (
    ECHO Skipping build operations because --no-build was specified.
    EXIT /B 0
)

SET /A BUILD_PARAMETER___INDEX=0

FOR %%G IN (%BUILD_PARAMETERS%) DO (
    FOR /F "tokens=1,2 delims=|" %%H IN (%%G) DO (
        SET "BUILD_PARAMETER___INPUT_FILE_NAME=%%H"
        SET "BUILD_PARAMETER___FRAMEWORKS=%%I"

        IF NOT DEFINED BUILD_PARAMETER___INPUT_FILE_NAME (
            ECHO Build input file name is not defined for build unit "!BUILD_UNIT!" build parameter #!BUILD_PARAMETER___INDEX!.
            EXIT /B 4
        )

        IF NOT DEFINED BUILD_PARAMETER___FRAMEWORKS (
            ECHO Build input file "!BUILD_PARAMETER___INPUT_FILE_NAME!" does not define any frameworks.
            EXIT /B 5
        )

        REM Resolve the input file name into a full file path.
        REM The second argument is the name of the variable that receives the resolved path.
        CALL :RESOLVE_BUILD_INPUT_FILE_PATH "!BUILD_PARAMETER___INPUT_FILE_NAME!" BUILD_PARAMETER___INPUT_FILE_PATH
        IF ERRORLEVEL 1 (
            ECHO Build input file "!BUILD_PARAMETER___INPUT_FILE_NAME!" could not be resolved.
            EXIT /B 4
        )

        IF /I "!PARAMETER___FRAMEWORKS!" == "!PARAMETER___FRAMEWORKS___DEFAULT!" (
            ECHO --------------------------------------------------------------------------------
            ECHO Building "!BUILD_PARAMETER___INPUT_FILE_NAME!" ^(!PARAMETER___VERSION! ^| !PARAMETER___CONFIGURATION! ^| project-defined frameworks^)^.^.^.
            ECHO --------------------------------------------------------------------------------

            SET BUILD_COMMAND=dotnet build "!BUILD_PARAMETER___INPUT_FILE_PATH!" --property:BUILD_VERSION=!PARAMETER___VERSION! --configuration !PARAMETER___CONFIGURATION! --property:ENABLE_SOURCE_LINK=!PARAMETER___ENABLE_SOURCE_LINK! --property:GeneratePackageOnBuild=false

            REM ECHO !BUILD_COMMAND!
            !BUILD_COMMAND! || EXIT /B 6
        ) ELSE (
            SET "BUILD_PARAMETER___SKIP_BUILD_OPERATION=false"

            CALL :RESOLVE_FRAMEWORKS "!BUILD_PARAMETER___FRAMEWORKS!" "!PARAMETER___FRAMEWORKS!" BUILD_PARAMETER___FRAMEWORKS
            IF ERRORLEVEL 1 (
                ECHO Skipping build operations for build input file "!BUILD_PARAMETER___INPUT_FILE_NAME!" because it does not define any frameworks that match the requested frameworks: "!PARAMETER___FRAMEWORKS!".
                SET "BUILD_PARAMETER___SKIP_BUILD_OPERATION=true"
            )

            IF /I "!BUILD_PARAMETER___SKIP_BUILD_OPERATION!" == "false" (
                FOR %%J IN (!BUILD_PARAMETER___FRAMEWORKS!) DO (
                    SET "BUILD_PARAMETER___FRAMEWORK=%%J"

                    ECHO --------------------------------------------------------------------------------
                    ECHO Building "!BUILD_PARAMETER___INPUT_FILE_NAME!" ^(!PARAMETER___VERSION! ^| !PARAMETER___CONFIGURATION! ^| !BUILD_PARAMETER___FRAMEWORK!^)^.^.^.
                    ECHO --------------------------------------------------------------------------------

                    SET BUILD_COMMAND=dotnet build "!BUILD_PARAMETER___INPUT_FILE_PATH!" --property:BUILD_VERSION=!PARAMETER___VERSION! --configuration !PARAMETER___CONFIGURATION! --framework !BUILD_PARAMETER___FRAMEWORK! --property:ENABLE_SOURCE_LINK=!PARAMETER___ENABLE_SOURCE_LINK! --property:GeneratePackageOnBuild=false

                    REM ECHO !BUILD_COMMAND!
                    !BUILD_COMMAND! || EXIT /B 6
                )
            )
        )
    )

    SET /A BUILD_PARAMETER___INDEX+=1
)

EXIT /B 0


:INVOKE_TEST

REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-vstest
REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56

IF NOT DEFINED TEST_PARAMETERS (
    ECHO Skipping test operations because no test parameters defined for build unit "!BUILD_UNIT!".
    EXIT /B 0
)

IF /I "!PARAMETER___RUN_TEST!" == "false" (
    ECHO Skipping test operations because --no-test was specified.
    EXIT /B 0
)

SET "ARTIFACTS___OUTPUT___CONFIGURATION___FOLDER_PATH=!ARTIFACTS___OUTPUT___FOLDER_PATH!\!PARAMETER___CONFIGURATION!"
SET "ARTIFACTS___TEST_RESULTS___CONFIGURATION___FOLDER_PATH=!ARTIFACTS___TEST_RESULTS___FOLDER_PATH!\!PARAMETER___CONFIGURATION!"

SET /A TEST_PARAMETER___INDEX=0

FOR %%G IN (%TEST_PARAMETERS%) DO (
    FOR /F "tokens=1,2 delims=|" %%H IN (%%G) DO (
        SET "TEST_PARAMETER___INPUT_FILE_NAME=%%H"
        SET "TEST_PARAMETER___FRAMEWORKS=%%I"

        IF NOT DEFINED TEST_PARAMETER___INPUT_FILE_NAME (
            ECHO Test input file name is not defined for build unit "!BUILD_UNIT!" test parameter #!TEST_PARAMETER___INDEX!.
            EXIT /B 7
        )

        IF NOT DEFINED TEST_PARAMETER___FRAMEWORKS (
            ECHO Test input file "!TEST_PARAMETER___INPUT_FILE_NAME!" does not define any frameworks.
            EXIT /B 8
        )

        REM Use FOR variable modifiers to split the file name into:
        REM - %%~xF = extension
        REM - %%~nF = file name without extension
        FOR %%F IN ("!TEST_PARAMETER___INPUT_FILE_NAME!") DO (
            SET "TEST_PARAMETER___INPUT_FILE_EXTENSION=%%~xF"
            SET "TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION=%%~nF"
        )

        SET "TEST_PARAMETER___SKIP_TEST_OPERATION=false"

        IF /I NOT "!PARAMETER___FRAMEWORKS!" == "!PARAMETER___FRAMEWORKS___DEFAULT!" (
            CALL :RESOLVE_FRAMEWORKS "!TEST_PARAMETER___FRAMEWORKS!" "!PARAMETER___FRAMEWORKS!" TEST_PARAMETER___FRAMEWORKS
            IF ERRORLEVEL 1 (
                ECHO Skipping test operations for test input file "!TEST_PARAMETER___INPUT_FILE_NAME!" because it does not define any frameworks that match the requested frameworks: "!PARAMETER___FRAMEWORKS!".
                SET "TEST_PARAMETER___SKIP_TEST_OPERATION=true"
            )
        )

        IF /I "!TEST_PARAMETER___SKIP_TEST_OPERATION!" == "false" (
            FOR %%J IN (!TEST_PARAMETER___FRAMEWORKS!) DO (
                SET "TEST_PARAMETER___FRAMEWORK=%%J"

                REM Resolve the input file name into a full file path.
                REM The second argument is the name of the variable that receives the resolved path.
                CALL :RESOLVE_TEST_INPUT_FILE_PATH "!TEST_PARAMETER___INPUT_FILE_NAME!" "!TEST_PARAMETER___FRAMEWORK!" TEST_PARAMETER___INPUT_FILE_PATH
                IF ERRORLEVEL 1 (
                    ECHO Test input file "!TEST_PARAMETER___INPUT_FILE_NAME!" could not be resolved.
                    EXIT /B 7
                )

                ECHO --------------------------------------------------------------------------------
                ECHO Testing "!TEST_PARAMETER___INPUT_FILE_NAME!" ^(!PARAMETER___VERSION! ^| !PARAMETER___CONFIGURATION! ^| !TEST_PARAMETER___FRAMEWORK!^)^.^.^.
                ECHO --------------------------------------------------------------------------------

                IF /I "!TEST_PARAMETER___INPUT_FILE_EXTENSION!" == ".dll" (
                    SET TEST_COMMAND=dotnet test "!TEST_PARAMETER___INPUT_FILE_PATH!" --framework !TEST_PARAMETER___FRAMEWORK! --results-directory "!ARTIFACTS___TEST_RESULTS___CONFIGURATION___FOLDER_PATH!" --logger "nunit;LogFileName=!TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!_!PARAMETER___VERSION!_!PARAMETER___CONFIGURATION!_!TEST_PARAMETER___FRAMEWORK!_TestResults-NUnit.xml;format=nunit3" --logger "liquid.md;LogFileName=!TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!_!PARAMETER___VERSION!_!PARAMETER___CONFIGURATION!_!TEST_PARAMETER___FRAMEWORK!_TestResults-Liquid.md"
                ) ELSE (
                    SET TEST_COMMAND=dotnet test "!TEST_PARAMETER___INPUT_FILE_PATH!" --property:BUILD_VERSION=!PARAMETER___VERSION! --configuration !PARAMETER___CONFIGURATION! --framework !TEST_PARAMETER___FRAMEWORK! --property:ENABLE_SOURCE_LINK=!PARAMETER___ENABLE_SOURCE_LINK! --property:GeneratePackageOnBuild=false --results-directory "!ARTIFACTS___TEST_RESULTS___CONFIGURATION___FOLDER_PATH!" --logger "nunit;LogFileName=!TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!_!PARAMETER___VERSION!_!PARAMETER___CONFIGURATION!_!TEST_PARAMETER___FRAMEWORK!_TestResults-NUnit.xml;format=nunit3" --logger "liquid.md;LogFileName=!TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!_!PARAMETER___VERSION!_!PARAMETER___CONFIGURATION!_!TEST_PARAMETER___FRAMEWORK!_TestResults-Liquid.md"
                )

                REM ECHO !TEST_COMMAND!
                !TEST_COMMAND! || EXIT /B 9
            )
        )
    )

    SET /A TEST_PARAMETER___INDEX+=1
)

EXIT /B 0


:INVOKE_PACKAGE

IF NOT DEFINED PACKAGE_PARAMETERS (
    ECHO Skipping package operations because no package parameters defined for build unit "!BUILD_UNIT!".
    EXIT /B 0
)

IF /I "!PARAMETER___RUN_PACKAGE!" == "false" (
    ECHO Skipping package operations because --no-package was specified.
    EXIT /B 0
)

SET "ARTIFACTS___PACKAGE_OUTPUT___CONFIGURATION___FOLDER_PATH=!ARTIFACTS___PACKAGE_OUTPUT___FOLDER_PATH!\!PARAMETER___CONFIGURATION!"

SET /A PACKAGE_PARAMETER___INDEX=0

SET "PACKAGE_PARAMETER___PACKAGE_OPERATIONS_PERFORMED=false"

FOR %%G IN (%PACKAGE_PARAMETERS%) DO (
    FOR /F "tokens=1,2 delims=|" %%H IN (%%G) DO (
        SET "PACKAGE_PARAMETER___INPUT_FILE_NAME=%%H"
        SET "PACKAGE_PARAMETER___FRAMEWORK=%%I"

        IF NOT DEFINED PACKAGE_PARAMETER___INPUT_FILE_NAME (
            ECHO Package input file name is not defined for build unit "!BUILD_UNIT!" package parameter #!PACKAGE_PARAMETER___INDEX!.
            EXIT /B 10
        )

        IF NOT DEFINED PACKAGE_PARAMETER___FRAMEWORK (
            ECHO Package input file "!PACKAGE_PARAMETER___INPUT_FILE_NAME!" does not define any frameworks.
            EXIT /B 11
        )

        CALL :RESOLVE_PACKAGE_INPUT_FILE_PATH "!PACKAGE_PARAMETER___INPUT_FILE_NAME!" PACKAGE_PARAMETER___INPUT_FILE_PATH
        IF ERRORLEVEL 1 (
            ECHO Package input file "!PACKAGE_PARAMETER___INPUT_FILE_NAME!" could not be resolved.
            EXIT /B 10
        )

        IF /I "!PARAMETER___FRAMEWORKS!" == "!PARAMETER___FRAMEWORKS___DEFAULT!" (
            ECHO --------------------------------------------------------------------------------
            ECHO Packaging "!PACKAGE_PARAMETER___INPUT_FILE_NAME!" ^(!PARAMETER___VERSION! ^| !PARAMETER___CONFIGURATION! ^| project-defined frameworks^)^.^.^.
            ECHO --------------------------------------------------------------------------------

            IF /I "!PARAMETER___RUN_BUILD!" == "true" (
                SET PACKAGE_COMMAND=dotnet pack "!PACKAGE_PARAMETER___INPUT_FILE_PATH!" --property:BUILD_VERSION=!PARAMETER___VERSION! --configuration !PARAMETER___CONFIGURATION! --no-build
            ) ELSE (
                SET PACKAGE_COMMAND=dotnet pack "!PACKAGE_PARAMETER___INPUT_FILE_PATH!" --property:BUILD_VERSION=!PARAMETER___VERSION! --configuration !PARAMETER___CONFIGURATION! --property:ENABLE_SOURCE_LINK=!PARAMETER___ENABLE_SOURCE_LINK! --property:GeneratePackageOnBuild=false
            )

            REM ECHO !PACKAGE_COMMAND!
            !PACKAGE_COMMAND! || EXIT /B 12

            SET "PACKAGE_PARAMETER___PACKAGE_OPERATIONS_PERFORMED=true"
        ) ELSE (
            SET "PACKAGE_PARAMETER___SKIP_PACKAGE_OPERATION=false"

            CALL :RESOLVE_FRAMEWORKS "!PACKAGE_PARAMETER___FRAMEWORK!" "!PARAMETER___FRAMEWORKS!" PACKAGE_PARAMETER___FRAMEWORKS
            IF ERRORLEVEL 1 (
                ECHO Skipping package operations for package input file "!PACKAGE_PARAMETER___INPUT_FILE_NAME!" because it does not define any frameworks that match the requested frameworks: "!PARAMETER___FRAMEWORKS!".
                SET "PACKAGE_PARAMETER___SKIP_PACKAGE_OPERATION=true"
            )

            IF /I "!PACKAGE_PARAMETER___SKIP_PACKAGE_OPERATION!" == "false" (
                ECHO --------------------------------------------------------------------------------
                ECHO Packaging "!PACKAGE_PARAMETER___INPUT_FILE_NAME!" ^(!PARAMETER___VERSION! ^| !PARAMETER___CONFIGURATION! ^| project-defined frameworks^)^.^.^.
                ECHO --------------------------------------------------------------------------------

                SET PACKAGE_COMMAND=dotnet pack "!PACKAGE_PARAMETER___INPUT_FILE_PATH!" --property:BUILD_VERSION=!PARAMETER___VERSION! --configuration !PARAMETER___CONFIGURATION! --property:ENABLE_SOURCE_LINK=!PARAMETER___ENABLE_SOURCE_LINK! --property:GeneratePackageOnBuild=false

                REM ECHO !PACKAGE_COMMAND!
                !PACKAGE_COMMAND! || EXIT /B 12

                SET "PACKAGE_PARAMETER___PACKAGE_OPERATIONS_PERFORMED=true"
            )
        )
    )

    SET /A PACKAGE_PARAMETER___INDEX+=1
)

IF NOT DEFINED PACKAGE_NEV_PARAMETERS (
    ECHO Skipping package dependency version update operations because no package dependency version update parameters defined for build unit "!BUILD_UNIT!".
    EXIT /B 0
)

IF /I "!PACKAGE_PARAMETER___PACKAGE_OPERATIONS_PERFORMED!" == "false" (
    ECHO Skipping package dependency version update operations because no package operations were performed for build unit "!BUILD_UNIT!".
    EXIT /B 0
)

SET PACKAGE_NEV_BUILD_COMMAND=dotnet build "!WORKSPACE_FOLDER_PATH!\tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.slnx" --configuration Release >NUL

REM ECHO !PACKAGE_NEV_BUILD_COMMAND!
!PACKAGE_NEV_BUILD_COMMAND! || EXIT /B 13

FOR %%G IN (%PACKAGE_NEV_PARAMETERS%) DO (
    SET "PACKAGE_NEV_PARAMETER___DEPENDENCY_PACKAGE_ID_PREFIX_FILTER=%%~G"

    ECHO --------------------------------------------------------------------------------
    ECHO Updating package dependency versions for "!PACKAGE_NEV_PARAMETER___DEPENDENCY_PACKAGE_ID_PREFIX_FILTER!" ^(!PARAMETER___VERSION! ^| !PARAMETER___CONFIGURATION!^)^.^.^.
    ECHO --------------------------------------------------------------------------------

    SET PACKAGE_NEV_COMMAND="!ARTIFACTS_FOLDER_PATH!\tools\nev\nev.exe" "!ARTIFACTS___PACKAGE_OUTPUT___CONFIGURATION___FOLDER_PATH!" "!PACKAGE_NEV_PARAMETER___DEPENDENCY_PACKAGE_ID_PREFIX_FILTER!"

    REM ECHO !PACKAGE_NEV_COMMAND!
    !PACKAGE_NEV_COMMAND! || EXIT /B 14
)

EXIT /B 0


:RESOLVE_BUILD_INPUT_FILE_PATH

SET "BUILD_PARAMETER___INPUT_FILE_NAME=%~1"

SET "BUILD_PARAMETER___INPUT_FILE_EXTENSION="
SET "BUILD_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION="

REM Use FOR variable modifiers to split the file name into:
REM - %%~xF = extension
REM - %%~nF = file name without extension
FOR %%F IN ("!BUILD_PARAMETER___INPUT_FILE_NAME!") DO (
    SET "BUILD_PARAMETER___INPUT_FILE_EXTENSION=%%~xF"
    SET "BUILD_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION=%%~nF"
)

IF /I "!BUILD_PARAMETER___INPUT_FILE_EXTENSION!" == ".slnx" (
    SET "%~2=!WORKSPACE_FOLDER_PATH!\!BUILD_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!BUILD_PARAMETER___INPUT_FILE_EXTENSION!" == ".sln" (
    SET "%~2=!WORKSPACE_FOLDER_PATH!\!BUILD_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!BUILD_PARAMETER___INPUT_FILE_EXTENSION!" == ".csproj" (
    SET "%~2=!SOURCE_CODE_FOLDER_PATH!\!BUILD_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!BUILD_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!BUILD_PARAMETER___INPUT_FILE_EXTENSION!" == ".fsproj" (
    SET "%~2=!SOURCE_CODE_FOLDER_PATH!\!BUILD_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!BUILD_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!BUILD_PARAMETER___INPUT_FILE_EXTENSION!" == ".vbproj" (
    SET "%~2=!SOURCE_CODE_FOLDER_PATH!\!BUILD_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!BUILD_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

ECHO Unsupported build input file name "!BUILD_PARAMETER___INPUT_FILE_NAME!".
ECHO Supported build input file extensions are .slnx, .sln, .csproj, .fsproj, and .vbproj.
EXIT /B 1


:RESOLVE_TEST_INPUT_FILE_PATH

SET "TEST_PARAMETER___INPUT_FILE_NAME=%~1"
SET "TEST_PARAMETER___FRAMEWORK=%~2"

SET "TEST_PARAMETER___INPUT_FILE_EXTENSION="
SET "TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION="

FOR %%F IN ("!TEST_PARAMETER___INPUT_FILE_NAME!") DO (
    SET "TEST_PARAMETER___INPUT_FILE_EXTENSION=%%~xF"
    SET "TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION=%%~nF"
)

IF /I "!TEST_PARAMETER___INPUT_FILE_EXTENSION!" == ".dll" (
    SET "%~3=!ARTIFACTS___OUTPUT___CONFIGURATION___FOLDER_PATH!\!TEST_PARAMETER___FRAMEWORK!\!TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!TEST_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!TEST_PARAMETER___INPUT_FILE_EXTENSION!" == ".slnx" (
    SET "%~3=!WORKSPACE_FOLDER_PATH!\!TEST_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!TEST_PARAMETER___INPUT_FILE_EXTENSION!" == ".sln" (
    SET "%~3=!WORKSPACE_FOLDER_PATH!\!TEST_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!TEST_PARAMETER___INPUT_FILE_EXTENSION!" == ".csproj" (
    SET "%~3=!SOURCE_CODE_FOLDER_PATH!\!TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!TEST_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!TEST_PARAMETER___INPUT_FILE_EXTENSION!" == ".fsproj" (
    SET "%~3=!SOURCE_CODE_FOLDER_PATH!\!TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!TEST_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!TEST_PARAMETER___INPUT_FILE_EXTENSION!" == ".vbproj" (
    SET "%~3=!SOURCE_CODE_FOLDER_PATH!\!TEST_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!TEST_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

ECHO Unsupported test input file name "!TEST_PARAMETER___INPUT_FILE_NAME!".
ECHO Supported test input file extensions are .dll, .slnx, .sln, .csproj, .fsproj, and .vbproj.
EXIT /B 1


:RESOLVE_PACKAGE_INPUT_FILE_PATH

SET "PACKAGE_PARAMETER___INPUT_FILE_NAME=%~1"

SET "PACKAGE_PARAMETER___INPUT_FILE_EXTENSION="
SET "PACKAGE_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION="

FOR %%F IN ("!PACKAGE_PARAMETER___INPUT_FILE_NAME!") DO (
    SET "PACKAGE_PARAMETER___INPUT_FILE_EXTENSION=%%~xF"
    SET "PACKAGE_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION=%%~nF"
)

IF /I "!PACKAGE_PARAMETER___INPUT_FILE_EXTENSION!" == ".slnx" (
    SET "%~2=!WORKSPACE_FOLDER_PATH!\!PACKAGE_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!PACKAGE_PARAMETER___INPUT_FILE_EXTENSION!" == ".sln" (
    SET "%~2=!WORKSPACE_FOLDER_PATH!\!PACKAGE_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!PACKAGE_PARAMETER___INPUT_FILE_EXTENSION!" == ".csproj" (
    SET "%~2=!SOURCE_CODE_FOLDER_PATH!\!PACKAGE_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!PACKAGE_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!PACKAGE_PARAMETER___INPUT_FILE_EXTENSION!" == ".fsproj" (
    SET "%~2=!SOURCE_CODE_FOLDER_PATH!\!PACKAGE_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!PACKAGE_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

IF /I "!PACKAGE_PARAMETER___INPUT_FILE_EXTENSION!" == ".vbproj" (
    SET "%~2=!SOURCE_CODE_FOLDER_PATH!\!PACKAGE_PARAMETER___INPUT_FILE_NAME_WITHOUT_EXTENSION!\!PACKAGE_PARAMETER___INPUT_FILE_NAME!"
    EXIT /B 0
)

ECHO Unsupported package input file name "!PACKAGE_PARAMETER___INPUT_FILE_NAME!".
ECHO Supported package input file extensions are .slnx, .sln, .csproj, .fsproj, and .vbproj.
EXIT /B 1


:RESOLVE_FRAMEWORKS

SET "RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORKS=%~1"
SET "RESOLVE_FRAMEWORKS_PARAMETER___REQUESTED_FRAMEWORKS=%~2"
SET "RESOLVE_FRAMEWORKS_PARAMETER___RESOLVED_FRAMEWORKS="

FOR %%R IN (%RESOLVE_FRAMEWORKS_PARAMETER___REQUESTED_FRAMEWORKS%) DO (
    SET "RESOLVE_FRAMEWORKS_PARAMETER___REQUESTED_FRAMEWORK=%%~R"

    SET "RESOLVE_FRAMEWORKS_PARAMETER___EXACT_MATCH_FOUND=false"

    FOR %%A IN (%RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORKS%) DO (
        SET "RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORK=%%~A"

        IF /I "!RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORK!" == "!RESOLVE_FRAMEWORKS_PARAMETER___REQUESTED_FRAMEWORK!" (
            CALL :APPEND_RESOLVED_FRAMEWORK "!RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORK!" RESOLVE_FRAMEWORKS_PARAMETER___RESOLVED_FRAMEWORKS
            SET "RESOLVE_FRAMEWORKS_PARAMETER___EXACT_MATCH_FOUND=true"
        )
    )

    IF /I "!RESOLVE_FRAMEWORKS_PARAMETER___EXACT_MATCH_FOUND!" == "false" (
        FOR %%A IN (%RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORKS%) DO (
            SET "RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORK=%%~A"

            ECHO(!RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORK! | FINDSTR /I /L /B /C:"!RESOLVE_FRAMEWORKS_PARAMETER___REQUESTED_FRAMEWORK!" >NUL 2>NUL
            IF NOT ERRORLEVEL 1 (
                CALL :APPEND_RESOLVED_FRAMEWORK "!RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORK!" RESOLVE_FRAMEWORKS_PARAMETER___RESOLVED_FRAMEWORKS
            )
        )
    )
)

IF NOT DEFINED RESOLVE_FRAMEWORKS_PARAMETER___RESOLVED_FRAMEWORKS (
    ECHO   No matching frameworks found.
    ECHO     Available frameworks: "%RESOLVE_FRAMEWORKS_PARAMETER___AVAILABLE_FRAMEWORKS%"
    ECHO     Requested frameworks: "%RESOLVE_FRAMEWORKS_PARAMETER___REQUESTED_FRAMEWORKS%"
    EXIT /B 1
)

SET "%~3=%RESOLVE_FRAMEWORKS_PARAMETER___RESOLVED_FRAMEWORKS%"

EXIT /B 0


:APPEND_RESOLVED_FRAMEWORK

IF NOT DEFINED %~2 (
    SET "%~2=%~1"
) ELSE (
    SET "%~2=!%~2!;%~1"
)

EXIT /B 0
