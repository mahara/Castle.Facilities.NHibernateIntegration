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

SET BUILD_PARAMETER___ENABLE_SOURCE_LINK=true
SET RUN_TEST=true


:PARSE_ARGS

IF {%1} == {} GOTO BUILD

IF /I "%1" == "--version" (
    SET "BUILD_ARG___VERSION=%2" & SHIFT
)

IF /I "%1" == "--configuration" (
    SET "BUILD_ARG___CONFIGURATION=%2" & SHIFT
)

IF /I "%1" == "--disable-source-link" (
    SET BUILD_ARG___ENABLE_SOURCE_LINK=false
)

IF /I "%1" == "--no-test" (
    SET RUN_TEST=false
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

IF DEFINED BUILD_ARG___ENABLE_SOURCE_LINK (
    SET BUILD_PARAMETER___ENABLE_SOURCE_LINK=%BUILD_ARG___ENABLE_SOURCE_LINK%
)

FOR %%G IN (%BUILD_PARAMETERS%) DO (
    FOR /F "tokens=1,2,3 delims=|" %%H IN (%%G) DO (
        SET BUILD_PARAMETER___PROJECT_FILE_PATH=%%H

        IF NOT DEFINED BUILD_ARG___VERSION (
            SET BUILD_PARAMETER___VERSION=%%I
        )

        IF NOT DEFINED BUILD_ARG___CONFIGURATION (
            SET BUILD_PARAMETER___CONFIGURATION=%%J
        )

        ECHO ----------------------------------------------------------------
        ECHO Building "!BUILD_PARAMETER___PROJECT_FILE_PATH!" ^(!BUILD_PARAMETER___VERSION! ^| !BUILD_PARAMETER___CONFIGURATION!^)^.^.^.
        ECHO ----------------------------------------------------------------

        dotnet build "!BUILD_PARAMETER___PROJECT_FILE_PATH!" --property:PACKAGE_VERSION=!BUILD_PARAMETER___VERSION! --configuration !BUILD_PARAMETER___CONFIGURATION! --property:ENABLE_SOURCE_LINK=%BUILD_PARAMETER___ENABLE_SOURCE_LINK% || EXIT /B 4
    )
)


SET ARTIFACTS_PACKAGES___CONFIGURATION___FOLDER_PATH=%ARTIFACTS_PACKAGES_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%

dotnet build "tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.slnx" --configuration Release

FOR %%G IN (%PACKAGE_PARAMETERS%) DO (
    SET PACKAGE_PARAMETER___PACKAGE_ID_PREFIX_FILTER=%%G

    SET NEV_COMMAND="%ARTIFACTS_FOLDER_PATH%\tools\nev\nev.exe" "%ARTIFACTS_PACKAGES___CONFIGURATION___FOLDER_PATH%\" !PACKAGE_PARAMETER___PACKAGE_ID_PREFIX_FILTER!
    REM ECHO !NEV_COMMAND!
    !NEV_COMMAND!
)


IF /I "%RUN_TEST%" == "false" EXIT /B


:TEST

REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-vstest
REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56

IF NOT DEFINED TEST_PARAMETERS EXIT /B 1


SET ARTIFACTS_OUTPUT___CONFIGURATION___FOLDER_PATH=%ARTIFACTS_OUTPUT_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%
SET ARTIFACTS_TEST_RESULTS___CONFIGURATION___FOLDER_PATH=%ARTIFACTS_TEST_RESULTS_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%

FOR %%G IN (%TEST_PARAMETERS%) DO (
    FOR /F "tokens=1,2 delims=|" %%H IN (%%G) DO (
        SET TEST_PARAMETER___PROJECT_NAME=%%H

        FOR %%J IN (%%I) DO (
            SET TEST_PARAMETER___TARGET_FRAMEWORK=%%J

            ECHO ----------------------------------------------------------------
            ECHO Testing "!TEST_PARAMETER___PROJECT_NAME!" ^(%BUILD_PARAMETER___VERSION% ^| %BUILD_PARAMETER___CONFIGURATION% ^| !TEST_PARAMETER___TARGET_FRAMEWORK!^)^.^.^.
            ECHO ----------------------------------------------------------------

            REM dotnet test "%SOURCE_CODE_FOLDER_PATH%\!TEST_PARAMETER___PROJECT_NAME!\!TEST_PARAMETER___PROJECT_NAME!.csproj" --configuration %BUILD_PARAMETER___CONFIGURATION% --framework !TEST_PARAMETER___TARGET_FRAMEWORK! --no-build --no-restore --results-directory "%ARTIFACTS_TEST_RESULTS___CONFIGURATION___FOLDER_PATH%" --logger "nunit;LogFileName=!TEST_PARAMETER___PROJECT_NAME!_%BUILD_PARAMETER___VERSION%_%BUILD_PARAMETER___CONFIGURATION%_!TEST_PARAMETER___TARGET_FRAMEWORK!_TestResults.xml;format=nunit3"
            dotnet test "%ARTIFACTS_OUTPUT___CONFIGURATION___FOLDER_PATH%\!TEST_PARAMETER___TARGET_FRAMEWORK!\!TEST_PARAMETER___PROJECT_NAME!\!TEST_PARAMETER___PROJECT_NAME!.dll" --framework !TEST_PARAMETER___TARGET_FRAMEWORK! --results-directory "%ARTIFACTS_TEST_RESULTS___CONFIGURATION___FOLDER_PATH%" --logger "nunit;LogFileName=!TEST_PARAMETER___PROJECT_NAME!_%BUILD_PARAMETER___VERSION%_%BUILD_PARAMETER___CONFIGURATION%_!TEST_PARAMETER___TARGET_FRAMEWORK!_TestResults.xml;format=nunit3"
        )
    )
)
