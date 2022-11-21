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
REM https://stackoverflow.com/questions/2952401/remove-trailing-slash-from-batch-file-input
REM https://stackoverflow.com/questions/1964192/removing-double-quotes-from-variables-in-batch-file-creates-problems-with-cmd-en


IF NOT DEFINED ARTIFACTS_FOLDER_PATH EXIT /B 1


SETLOCAL EnableDelayedExpansion


:PARSE_ARGUMENTS

IF {%1} == {} GOTO INITIALIZE_PARAMETERS

IF /I "%1" == "--no-build" (
    SET ARGUMENT___RUN_BUILD=false
)

IF /I "%1" == "--no-test" (
    SET ARGUMENT___RUN_TEST=false
)

IF /I "%1" == "--version" (
    SET "BUILD_ARGUMENT___VERSION=%2" & SHIFT
)

IF /I "%1" == "--configuration" (
    SET "BUILD_ARGUMENT___CONFIGURATION=%2" & SHIFT
)

IF /I "%1" == "--disable-source-link" (
    SET BUILD_ARGUMENT___ENABLE_SOURCE_LINK=false
)

SHIFT

GOTO PARSE_ARGUMENTS


:INITIALIZE_PARAMETERS

SET PARAMETER___RUN_BUILD=%PARAMETER___RUN_BUILD___DEFAULT%
IF DEFINED ARGUMENT___RUN_BUILD (
    SET PARAMETER___RUN_BUILD=%ARGUMENT___RUN_BUILD%
)

SET PARAMETER___RUN_TEST=%PARAMETER___RUN_TEST___DEFAULT%
IF DEFINED ARGUMENT___RUN_TEST (
    SET PARAMETER___RUN_TEST=%ARGUMENT___RUN_TEST%
)

SET BUILD_PARAMETER___VERSION=%BUILD_PARAMETER___VERSION___DEFAULT%
IF DEFINED BUILD_ARGUMENT___VERSION (
    SET BUILD_PARAMETER___VERSION=%BUILD_ARGUMENT___VERSION%
)

SET BUILD_PARAMETER___CONFIGURATION=%BUILD_PARAMETER___CONFIGURATION___DEFAULT%
IF DEFINED BUILD_ARGUMENT___CONFIGURATION (
    SET BUILD_PARAMETER___CONFIGURATION=%BUILD_ARGUMENT___CONFIGURATION%
)

SET BUILD_PARAMETER___ENABLE_SOURCE_LINK=%BUILD_PARAMETER___ENABLE_SOURCE_LINK___DEFAULT%
IF DEFINED BUILD_ARGUMENT___ENABLE_SOURCE_LINK (
    SET BUILD_PARAMETER___ENABLE_SOURCE_LINK=%BUILD_ARGUMENT___ENABLE_SOURCE_LINK%
)


:BUILD

IF NOT DEFINED BUILD_PARAMETERS EXIT /B 1

IF /I "%PARAMETER___RUN_BUILD%" == "false" GOTO TEST


FOR %%G IN (%BUILD_PARAMETERS%) DO (
    SET BUILD_PARAMETER___PROJECT_FILE_NAME=%%G
    SET BUILD_PARAMETER___PROJECT_FILE_NAME=!BUILD_PARAMETER___PROJECT_FILE_NAME:"=!

    ECHO --------------------------------------------------------------------------------
    ECHO Building "!BUILD_PARAMETER___PROJECT_FILE_NAME!" ^(!BUILD_PARAMETER___VERSION! ^| !BUILD_PARAMETER___CONFIGURATION!^)^.^.^.
    ECHO --------------------------------------------------------------------------------

    SET BUILD_COMMAND=dotnet build "!WORKSPACE_FOLDER_PATH!\!BUILD_PARAMETER___PROJECT_FILE_NAME!" --property:PACKAGE_VERSION=!BUILD_PARAMETER___VERSION! --configuration !BUILD_PARAMETER___CONFIGURATION! --property:ENABLE_SOURCE_LINK=%BUILD_PARAMETER___ENABLE_SOURCE_LINK% || EXIT /B 4
    REM ECHO !BUILD_COMMAND!
    !BUILD_COMMAND!
)


SET ARTIFACTS_PACKAGES___CONFIGURATION___FOLDER_PATH=%ARTIFACTS_PACKAGES_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%

dotnet build "%WORKSPACE_FOLDER_PATH%\tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln" --configuration Release

FOR %%G IN (%PACKAGE_PARAMETERS%) DO (
    SET PACKAGE_PARAMETER___PACKAGE_ID_PREFIX_FILTER=%%G
    SET PACKAGE_PARAMETER___PACKAGE_ID_PREFIX_FILTER=!PACKAGE_PARAMETER___PACKAGE_ID_PREFIX_FILTER:"=!

    SET PACKAGE_NEV_COMMAND="%ARTIFACTS_FOLDER_PATH%\tools\nev\nev.exe" "%ARTIFACTS_PACKAGES___CONFIGURATION___FOLDER_PATH%\" "!PACKAGE_PARAMETER___PACKAGE_ID_PREFIX_FILTER!"
    REM ECHO !PACKAGE_NEV_COMMAND!
    !PACKAGE_NEV_COMMAND!
)


:TEST

REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-vstest
REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56


IF NOT DEFINED TEST_PARAMETERS EXIT /B 1

IF /I "%PARAMETER___RUN_TEST%" == "false" EXIT /B


SET ARTIFACTS_OUTPUT___CONFIGURATION___FOLDER_PATH=%ARTIFACTS_OUTPUT_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%
SET ARTIFACTS_TEST_RESULTS___CONFIGURATION___FOLDER_PATH=%ARTIFACTS_TEST_RESULTS_FOLDER_PATH%\%BUILD_PARAMETER___CONFIGURATION%

FOR %%G IN (%TEST_PARAMETERS%) DO (
    FOR /F "tokens=1,2 delims=|" %%H IN (%%G) DO (
        SET TEST_PARAMETER___PROJECT_NAME=%%H

        FOR %%J IN (%%I) DO (
            SET TEST_PARAMETER___TARGET_FRAMEWORK=%%J

            ECHO --------------------------------------------------------------------------------
            ECHO Testing "!TEST_PARAMETER___PROJECT_NAME!" ^(%BUILD_PARAMETER___VERSION% ^| %BUILD_PARAMETER___CONFIGURATION% ^| !TEST_PARAMETER___TARGET_FRAMEWORK!^)^.^.^.
            ECHO --------------------------------------------------------------------------------

            REM SET TEST_COMMAND=dotnet test "%SOURCE_CODE_FOLDER_PATH%\!TEST_PARAMETER___PROJECT_NAME!\!TEST_PARAMETER___PROJECT_NAME!.csproj" --configuration %BUILD_PARAMETER___CONFIGURATION% --framework !TEST_PARAMETER___TARGET_FRAMEWORK! --no-build --no-restore --results-directory "%ARTIFACTS_TEST_RESULTS___CONFIGURATION___FOLDER_PATH%" --logger "nunit;LogFileName=!TEST_PARAMETER___PROJECT_NAME!_%BUILD_PARAMETER___VERSION%_%BUILD_PARAMETER___CONFIGURATION%_!TEST_PARAMETER___TARGET_FRAMEWORK!_TestResults.xml;format=nunit3"
            SET TEST_COMMAND=dotnet test "%ARTIFACTS_OUTPUT___CONFIGURATION___FOLDER_PATH%\!TEST_PARAMETER___TARGET_FRAMEWORK!\!TEST_PARAMETER___PROJECT_NAME!\!TEST_PARAMETER___PROJECT_NAME!.dll" --framework !TEST_PARAMETER___TARGET_FRAMEWORK! --results-directory "%ARTIFACTS_TEST_RESULTS___CONFIGURATION___FOLDER_PATH%" --logger "nunit;LogFileName=!TEST_PARAMETER___PROJECT_NAME!_%BUILD_PARAMETER___VERSION%_%BUILD_PARAMETER___CONFIGURATION%_!TEST_PARAMETER___TARGET_FRAMEWORK!_TestResults.xml;format=nunit3"
            REM ECHO !TEST_COMMAND!
            !TEST_COMMAND!
        )
    )
)
