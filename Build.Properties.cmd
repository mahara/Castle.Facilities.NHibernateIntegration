@ECHO OFF


REM ================
REM CONVENTIONS
REM ================
REM -   All "*FOLDER_PATH" variables must not have a trailing backslash (a folder/directory separator).
REM     This is different from the convention used for MSBuild folder/directory properties.
REM     -   %~dp0 has a trailing backslash, so it needs to be removed by using this code:
REM             SET WORKSPACE_FOLDER_PATH=%WORKSPACE_FOLDER_PATH:~0,-1%
REM     -   When building folder/directory paths by combining path segments,
REM         a backslash must be added between them, for example:
REM             SET BUILD_CONFIGURATION_FOLDER_PATH=%WORKSPACE_FOLDER_PATH%\%BUILD_CONFIGURATION_FOLDER_NAME%
REM     -   When passing a double-quoted folder/directory path as an argument to an external process,
REM         the trailing backslash must be removed, or not explicitly specified, because it would create issues
REM         with legacy Windows command-line argument parsing behavior, for example:
REM             SET PACKAGE_NEV_COMMAND="!ARTIFACTS_FOLDER_PATH!\tools\nev\nev.exe" "!ARTIFACTS___PACKAGE_OUTPUT___CONFIGURATION___FOLDER_PATH!" "!PACKAGE_NEV_PARAMETER___DEPENDENCY_PACKAGE_ID_PREFIX_FILTER!"
REM         Adding a backslash at the end of "!ARTIFACTS___PACKAGE_OUTPUT___CONFIGURATION___FOLDER_PATH!"
REM         would create a trailing \" sequence and cause the external process to receive
REM         one merged argument like 'C:\...\Release" Iesi.', instead of two separate arguments.
REM -   All "*FOLDER_PATH" variables defined below are base/root folder/directory paths,
REM     even though their variable names don't use the word "BASE".



SET "BUILD_CONFIGURATION_FOLDER_NAME=buildscripts"
SET "SOURCE_CODE_FOLDER_NAME=src"
SET "ARTIFACTS_FOLDER_NAME=artifacts"
SET "ARTIFACTS___OUTPUT___FOLDER_NAME=bin"
SET "ARTIFACTS___PACKAGE_OUTPUT___FOLDER_NAME=packages"
SET "ARTIFACTS___TEST_RESULTS___FOLDER_NAME=testresults"

SET "WORKSPACE_FOLDER_PATH=%~dp0"
SET "WORKSPACE_FOLDER_PATH=%WORKSPACE_FOLDER_PATH:~0,-1%"
SET "BUILD_CONFIGURATION_FOLDER_PATH=%WORKSPACE_FOLDER_PATH%\%BUILD_CONFIGURATION_FOLDER_NAME%"
SET "SOURCE_CODE_FOLDER_PATH=%WORKSPACE_FOLDER_PATH%\%SOURCE_CODE_FOLDER_NAME%"
SET "ARTIFACTS_FOLDER_PATH=%WORKSPACE_FOLDER_PATH%\%ARTIFACTS_FOLDER_NAME%"
SET "ARTIFACTS___OUTPUT___FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS___OUTPUT___FOLDER_NAME%"
SET "ARTIFACTS___PACKAGE_OUTPUT___FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS___PACKAGE_OUTPUT___FOLDER_NAME%"
SET "ARTIFACTS___TEST_RESULTS___FOLDER_PATH=%ARTIFACTS_FOLDER_PATH%\%ARTIFACTS___TEST_RESULTS___FOLDER_NAME%"


SET "PARAMETER___RUN_BUILD___DEFAULT=true"
SET "PARAMETER___RUN_TEST___DEFAULT=true"
SET "PARAMETER___RUN_PACKAGE___DEFAULT=true"
SET "PARAMETER___VERSION___DEFAULT=5.6.0"
SET "PARAMETER___CONFIGURATION___DEFAULT=Release"
SET "PARAMETER___FRAMEWORKS___DEFAULT=net9.0;net8.0;net48"
SET "PARAMETER___ENABLE_SOURCE_LINK___DEFAULT=true"


REM ================================================================================
REM BUILD UNITS
REM ================================================================================
REM Build units are executed in the order defined below.
REM Each build unit completes build -> test -> package before the next build unit starts.

SET BUILD_UNITS=^
    "Castle.Facilities.NHibernateIntegration"


REM ================================================================================
REM BUILD UNIT: Castle.Facilities.NHibernateIntegration
REM ================================================================================

SET BUILD_UNIT___Castle.Facilities.NHibernateIntegration___BUILD_PARAMETERS=^
    "Castle.Facilities.NHibernateIntegration.slnx|%PARAMETER___FRAMEWORKS___DEFAULT%"

SET BUILD_UNIT___Castle.Facilities.NHibernateIntegration___TEST_PARAMETERS=^
    "Castle.Facilities.NHibernateIntegration.Tests.dll|%PARAMETER___FRAMEWORKS___DEFAULT%"

SET BUILD_UNIT___Castle.Facilities.NHibernateIntegration___PACKAGE_PARAMETERS=^
    "Castle.Facilities.NHibernateIntegration.csproj|%PARAMETER___FRAMEWORKS___DEFAULT%"

SET BUILD_UNIT___Castle.Facilities.NHibernateIntegration___PACKAGE_NEV_PARAMETERS=^
    "Castle."
