@ECHO OFF

REM https://superuser.com/questions/149951/does-in-batch-file-mean-all-command-line-arguments
REM https://stackoverflow.com/questions/15420004/write-batch-file-with-hyphenated-parameters
REM https://superuser.com/questions/1505178/parsing-command-line-argument-in-batch-file
REM https://ss64.com/nt/for.html
REM https://ss64.com/nt/for_f.html
REM https://stackoverflow.com/questions/2591758/batch-script-loop
REM https://stackoverflow.com/questions/46576996/how-to-use-for-loop-to-get-set-variable-by-batch-file
REM https://stackoverflow.com/questions/3294599/do-batch-files-support-multiline-variables
REM https://stackoverflow.com/questions/36228474/batch-file-if-string-starts-with


IF NOT DEFINED ARTIFACTS_FOLDER_PATH EXIT /B 1


SETLOCAL EnableDelayedExpansion

SET NO_TEST=


:PARSE_ARGS

IF {%1} == {} GOTO BUILD

IF /I "%1" == "--version" (
    SET "BUILD_ARG___VERSION=%2" & SHIFT
)

IF /I "%1" == "--configuration" (
    SET "BUILD_ARG___CONFIGURATION=%2" & SHIFT
)

IF /I "%1" == "--no-test" (
    SET NO_TEST=true
)

SHIFT

GOTO PARSE_ARGS


:BUILD

IF NOT DEFINED BUILD_PARAMETERS EXIT /B 1


IF DEFINED BUILD_ARG___VERSION (
    SET BUILD_PARAMETER___VERSION=%BUILD_ARG___VERSION%
)

IF DEFINED BUILD_ARG___CONFIGURATION (
    SET BUILD_PARAMETER___CONFIGURATION=%BUILD_ARG___CONFIGURATION%
)

FOR %%G IN (%BUILD_PARAMETERS%) DO (
    FOR /F "tokens=1,2,3 delims=|" %%H IN (%%G) DO (
        SET BUILD_PROJECT_FILE_PATH=%%H

        IF NOT DEFINED BUILD_ARG___VERSION (
            SET BUILD_PARAMETER___VERSION=%%I
        )

        IF NOT DEFINED BUILD_ARG___CONFIGURATION (
            SET BUILD_PARAMETER___CONFIGURATION=%%J
        )

        ECHO ----------------------------------------------------------------
        ECHO Building "!BUILD_PROJECT_FILE_PATH!" ^(!BUILD_PARAMETER___VERSION! ^| !BUILD_PARAMETER___CONFIGURATION!^)^.^.^.
        ECHO ----------------------------------------------------------------

        dotnet build "!BUILD_PROJECT_FILE_PATH!" -property:PACKAGE_BUILD_VERSION=!BUILD_PARAMETER___VERSION! --configuration !BUILD_PARAMETER___CONFIGURATION! || EXIT /B 4
    )
)

SET ARTIFACTS_PACKAGES_BUILD_CONFIGURATION_FOLDER_PATH=%ARTIFACTS_PACKAGES_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%

dotnet build "tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln" --configuration Release
SET NEV_COMMAND="%ARTIFACTS_FOLDER_PATH%\tools\nev\nev.exe" "%ARTIFACTS_PACKAGES_BUILD_CONFIGURATION_FOLDER_PATH%\" "Framework."
REM ECHO %NEV_COMMAND%
%NEV_COMMAND%

IF DEFINED NO_TEST EXIT /B


:TEST

REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-vstest
REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56

IF NOT DEFINED TEST_PARAMETERS EXIT /B 1


SET ARTIFACTS_OUTPUT_BUILD_CONFIGURATION_FOLDER_PATH=%ARTIFACTS_OUTPUT_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%
SET ARTIFACTS_TEST_RESULTS_BUILD_CONFIGURATION_FOLDER_PATH=%ARTIFACTS_TEST_RESULTS_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%

FOR %%G IN (%TEST_PARAMETERS%) DO (
    FOR /F "tokens=1,2 delims=|" %%H IN (%%G) DO (
        SET TEST_PROJECT_NAME=%%H

        FOR %%J IN (%%I) DO (
            SET TEST_TARGET_FRAMEWORK=%%J

            ECHO ----------------------------------------------------------------
            ECHO Testing "!TEST_PROJECT_NAME!" ^(%BUILD_PARAMETER___VERSION% ^| %BUILD_PARAMETER___CONFIGURATION% ^| !TEST_TARGET_FRAMEWORK!^)^.^.^.
            ECHO ----------------------------------------------------------------

            REM dotnet test "%SOURCE_CODE_FOLDER_PATH%\!TEST_PROJECT_NAME!\!TEST_PROJECT_NAME!.csproj" --configuration %BUILD_PARAMETER___CONFIGURATION% --framework !TEST_TARGET_FRAMEWORK! --no-build --no-restore --results-directory "%ARTIFACTS_TEST_RESULTS_BUILD_CONFIGURATION_FOLDER_PATH%" --logger "nunit;LogFileName=!TEST_PROJECT_NAME!_%BUILD_PARAMETER___VERSION%_%BUILD_PARAMETER___CONFIGURATION%_!TEST_TARGET_FRAMEWORK!_TestResults.xml;format=nunit3"
            dotnet test "%ARTIFACTS_OUTPUT_BUILD_CONFIGURATION_FOLDER_PATH%\!TEST_TARGET_FRAMEWORK!\!TEST_PROJECT_NAME!\!TEST_PROJECT_NAME!.dll" --framework !TEST_TARGET_FRAMEWORK! --results-directory "%ARTIFACTS_TEST_RESULTS_BUILD_CONFIGURATION_FOLDER_PATH%" --logger "nunit;LogFileName=!TEST_PROJECT_NAME!_%BUILD_PARAMETER___VERSION%_%BUILD_PARAMETER___CONFIGURATION%_!TEST_TARGET_FRAMEWORK!_TestResults.xml;format=nunit3"
        )
    )
)
