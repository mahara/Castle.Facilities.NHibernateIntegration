@ECHO OFF

REM https://stackoverflow.com/questions/15420004/write-batch-file-with-hyphenated-parameters
REM https://superuser.com/questions/1505178/parsing-command-line-argument-in-batch-file
REM https://ss64.com/nt/for.html
REM https://ss64.com/nt/for_f.html
REM https://stackoverflow.com/questions/2591758/batch-script-loop
REM https://stackoverflow.com/questions/46576996/how-to-use-for-loop-to-get-set-variable-by-batch-file
REM https://stackoverflow.com/questions/3294599/do-batch-files-support-multiline-variables


IF NOT DEFINED ARTIFACTS_FOLDER_PATH EXIT /B 1


SETLOCAL EnableDelayedExpansion

SET BUILD_CONFIGURATION=Release
SET BUILD_VERSION=1.0.0
SET NO_TEST=


:PARSE_ARGS

IF {%1} == {} GOTO SET_FOLDERS

IF /I "%1" == "--configuration" (
    SET "BUILD_CONFIGURATION=%2" & SHIFT
)
IF /I "%1" == "--version" (
    SET "BUILD_VERSION=%2" & SHIFT
)
IF /I "%1" == "--no-test" (
    SET NO_TEST=true
)

SHIFT

GOTO PARSE_ARGS


:SET_FOLDERS

SET ARTIFACTS_OUTPUT_FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS_OUTPUT_FOLDER_NAME%
SET ARTIFACTS_OUTPUT_BUILD_CONFIGURATION_FOLDER_PATH=%ARTIFACTS_OUTPUT_FOLDER_PATH%\%BUILD_CONFIGURATION%

SET ARTIFACTS_PACKAGES_FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS_PACKAGES_FOLDER_NAME%\%BUILD_CONFIGURATION%

SET ARTIFACTS_TEST_RESULTS_FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS_TEST_RESULTS_FOLDER_NAME%\%BUILD_CONFIGURATION%

GOTO BUILD


:BUILD

ECHO ----------------------------------------------------
ECHO Building "%BUILD_CONFIGURATION%" packages with version "%BUILD_VERSION%"...
ECHO ----------------------------------------------------

dotnet build "Castle.Facilities.NHibernateIntegration.sln" --configuration %BUILD_CONFIGURATION% -property:PACKAGE_BUILD_VERSION=%BUILD_VERSION% || EXIT /B 4

dotnet build "tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln" --configuration Release
SET NEV_COMMAND="%ARTIFACTS_FOLDER_PATH%\tools\nev\nev.exe" "%ARTIFACTS_PACKAGES_FOLDER_PATH%\" "Castle."
ECHO %NEV_COMMAND%
%NEV_COMMAND%

IF DEFINED NO_TEST EXIT /B


:TEST

REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-vstest
REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56

SET PROJECT_NAMES=^
    Castle.Facilities.NHibernateIntegration.Tests

SET TARGET_FRAMEWORKS=net9.0;net8.0;net48

FOR %%G IN (%TARGET_FRAMEWORKS%) DO (
    SET TARGET_FRAMEWORK=%%G

    ECHO ------------------------------------
    ECHO Running .NET ^(!TARGET_FRAMEWORK!^) Unit Tests
    ECHO ------------------------------------

    FOR %%H IN (%PROJECT_NAMES%) DO (
        SET PROJECT_NAME=%%H

        dotnet test "src\!PROJECT_NAME!\!PROJECT_NAME!.csproj" --configuration %BUILD_CONFIGURATION% --framework !TARGET_FRAMEWORK! --no-build --no-restore --results-directory "%ARTIFACTS_TEST_RESULTS_FOLDER_PATH%" --logger "nunit;LogFileName=!PROJECT_NAME!_!TARGET_FRAMEWORK!_%BUILD_CONFIGURATION%_TestResults.xml;format=nunit3"
    )
)
