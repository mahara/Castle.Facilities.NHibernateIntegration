@ECHO OFF

REM
REM https://stackoverflow.com/questions/15420004/write-batch-file-with-hyphenated-parameters
REM https://superuser.com/questions/1505178/parsing-command-line-argument-in-batch-file
REM


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

SET ARTIFACTS_FOLDER_NAME=artifacts
SET ARTIFACTS_OUTPUT_FOLDER_NAME=bin
SET ARTIFACTS_PACKAGES_FOLDER_NAME=packages
SET ARTIFACTS_TEST_RESULTS_FOLDER_NAME=testresults

SET ARTIFACTS_FOLDER_PATH=%ARTIFACTS_FOLDER_NAME%

SET ARTIFACTS_OUTPUT_FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS_OUTPUT_FOLDER_NAME%
SET ARTIFACTS_OUTPUT_BUILD_CONFIGURATION_FOLDER_PATH=%ARTIFACTS_OUTPUT_FOLDER_PATH%\%BUILD_CONFIGURATION%

SET ARTIFACTS_PACKAGES_FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS_PACKAGES_FOLDER_NAME%\%BUILD_CONFIGURATION%

SET ARTIFACTS_TEST_RESULTS_FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS_TEST_RESULTS_FOLDER_NAME%\%BUILD_CONFIGURATION%

GOTO BUILD


:BUILD

ECHO ----------------------------------------------------
ECHO Building "%BUILD_CONFIGURATION%" packages with version "%BUILD_VERSION%"...
ECHO ----------------------------------------------------

dotnet build "Castle.Facilities.NHibernateIntegration.sln" --configuration %BUILD_CONFIGURATION% -property:APPVEYOR_BUILD_VERSION=%BUILD_VERSION% || EXIT /B 1

dotnet build "tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln" --configuration Release
SET NEV_COMMAND="%ARTIFACTS_FOLDER_PATH%\tools\nev\nev.exe" "%ARTIFACTS_PACKAGES_FOLDER_PATH%\" "Castle."
ECHO %NEV_COMMAND%
%NEV_COMMAND%

IF DEFINED NO_TEST GOTO EXIT


:TEST

REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-vstest
REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56

ECHO ------------------------------------
ECHO Running .NET (net9.0) Unit Tests
ECHO ------------------------------------

dotnet test "%ARTIFACTS_OUTPUT_BUILD_CONFIGURATION_FOLDER_PATH%\net9.0\Castle.Facilities.NHibernateIntegration.Tests\Castle.Facilities.NHibernateIntegration.Tests.dll" --results-directory "%ARTIFACTS_TEST_RESULTS_FOLDER_PATH%" --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests_net9.0_TestResults.xml;format=nunit3"

ECHO ------------------------------------
ECHO Running .NET (net8.0) Unit Tests
ECHO ------------------------------------

dotnet test "%ARTIFACTS_OUTPUT_BUILD_CONFIGURATION_FOLDER_PATH%\net8.0\Castle.Facilities.NHibernateIntegration.Tests\Castle.Facilities.NHibernateIntegration.Tests.dll" --results-directory "%ARTIFACTS_TEST_RESULTS_FOLDER_PATH%" --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests_net8.0_TestResults.xml;format=nunit3"

ECHO --------------------------------------------
ECHO Running .NET Framework (net48) Unit Tests
ECHO --------------------------------------------

dotnet test "%ARTIFACTS_OUTPUT_BUILD_CONFIGURATION_FOLDER_PATH%\net48\Castle.Facilities.NHibernateIntegration.Tests\Castle.Facilities.NHibernateIntegration.Tests.exe" --results-directory "%ARTIFACTS_TEST_RESULTS_FOLDER_PATH%" --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests_net48_TestResults.xml;format=nunit3"


:EXIT
