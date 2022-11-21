@ECHO OFF

REM ================
REM EXIT CODES
REM ================
REM 0  = Build environment reset verified successfully.
REM 1  = Build environment reset verification failed.



REM ================================================================================
REM Main
REM ================================================================================


REM ================================================================================
REM Ensure Build Environment
REM
REM Ensures that the build environment is clean
REM by removing all owned variables
REM and verifying that no owned variables remain assigned.
REM
REM Returns:
REM     0 - Build environment reset verified successfully.
REM     1 - Build environment reset verification failed.
REM ================================================================================

CALL :RESET_BUILD_ENVIRONMENT

CALL :VERIFY_BUILD_ENVIRONMENT_RESET
IF ERRORLEVEL 1 (
    EXIT /B 1
)

EXIT /B 0



REM ================================================================================
REM Workflow Operations
REM ================================================================================


REM ================================================================================
REM Reset Build Environment
REM
REM Removes all build environment variables owned by the build infrastructure.
REM This establishes a deterministic clean environment boundary before and after execution.
REM
REM Returns:
REM     0 - Build environment reset successfully.
REM ================================================================================
:RESET_BUILD_ENVIRONMENT

REM ================================================================================
REM Folder names
REM ================================================================================

SET "BUILD_CONFIGURATION_FOLDER_NAME="
SET "SOURCE_CODE_FOLDER_NAME="
SET "ARTIFACTS_FOLDER_NAME="
SET "ARTIFACTS___OUTPUT___FOLDER_NAME="
SET "ARTIFACTS___PACKAGE_OUTPUT___FOLDER_NAME="
SET "ARTIFACTS___TEST_RESULTS___FOLDER_NAME="


REM ================================================================================
REM Folder paths
REM ================================================================================

SET "WORKSPACE_FOLDER_PATH="
SET "BUILD_CONFIGURATION_FOLDER_PATH="
SET "SOURCE_CODE_FOLDER_PATH="
SET "ARTIFACTS_FOLDER_PATH="
SET "ARTIFACTS___OUTPUT___FOLDER_PATH="
SET "ARTIFACTS___PACKAGE_OUTPUT___FOLDER_PATH="
SET "ARTIFACTS___TEST_RESULTS___FOLDER_PATH="


REM ================================================================================
REM Arguments
REM ================================================================================

FOR /F "tokens=1 delims==" %%V IN ('SET ARGUMENT___ 2^>NUL') DO (
    SET "%%V="
)


REM ================================================================================
REM Parameters
REM ================================================================================

FOR /F "tokens=1 delims==" %%V IN ('SET PARAMETER___ 2^>NUL') DO (
    SET "%%V="
)


REM ================================================================================
REM Build units
REM ================================================================================

SET "BUILD_UNITS="


REM ================================================================================
REM Build unit parameters
REM ================================================================================

FOR /F "tokens=1 delims==" %%V IN ('SET BUILD_UNIT___ 2^>NUL') DO (
    SET "%%V="
)


REM ================================================================================
REM Working variables
REM ================================================================================

SET "BUILD_UNIT="

SET "BUILD_PARAMETERS="
SET "TEST_PARAMETERS="
SET "PACKAGE_PARAMETERS="
SET "PACKAGE_NEV_PARAMETERS="


EXIT /B 0


REM ================================================================================
REM Verify Build Environment Reset
REM
REM Verifies that no build environment variables owned by this build infrastructure
REM remain assigned.
REM
REM Returns:
REM     0 - Build environment reset verified successfully.
REM     1 - Build environment reset verification failed.
REM ================================================================================
:VERIFY_BUILD_ENVIRONMENT_RESET

REM ================================================================================
REM Folder names
REM ================================================================================

IF DEFINED BUILD_CONFIGURATION_FOLDER_NAME EXIT /B 1
IF DEFINED SOURCE_CODE_FOLDER_NAME EXIT /B 1
IF DEFINED ARTIFACTS_FOLDER_NAME EXIT /B 1
IF DEFINED ARTIFACTS___OUTPUT___FOLDER_NAME EXIT /B 1
IF DEFINED ARTIFACTS___PACKAGE_OUTPUT___FOLDER_NAME EXIT /B 1
IF DEFINED ARTIFACTS___TEST_RESULTS___FOLDER_NAME EXIT /B 1


REM ================================================================================
REM Folder paths
REM ================================================================================

IF DEFINED WORKSPACE_FOLDER_PATH EXIT /B 1
IF DEFINED BUILD_CONFIGURATION_FOLDER_PATH EXIT /B 1
IF DEFINED SOURCE_CODE_FOLDER_PATH EXIT /B 1
IF DEFINED ARTIFACTS_FOLDER_PATH EXIT /B 1
IF DEFINED ARTIFACTS___OUTPUT___FOLDER_PATH EXIT /B 1
IF DEFINED ARTIFACTS___PACKAGE_OUTPUT___FOLDER_PATH EXIT /B 1
IF DEFINED ARTIFACTS___TEST_RESULTS___FOLDER_PATH EXIT /B 1


REM ================================================================================
REM Arguments
REM ================================================================================

SET ARGUMENT___ >NUL 2>&1
IF NOT ERRORLEVEL 1 EXIT /B 1


REM ================================================================================
REM Parameters
REM ================================================================================

SET PARAMETER___ >NUL 2>&1
IF NOT ERRORLEVEL 1 EXIT /B 1


REM ================================================================================
REM Build units
REM ================================================================================

IF DEFINED BUILD_UNITS EXIT /B 1


REM ================================================================================
REM Build unit parameters
REM ================================================================================

SET BUILD_UNIT___ >NUL 2>&1
IF NOT ERRORLEVEL 1 EXIT /B 1


REM ================================================================================
REM Working variables
REM ================================================================================

IF DEFINED BUILD_UNIT EXIT /B 1
IF DEFINED BUILD_PARAMETERS EXIT /B 1
IF DEFINED TEST_PARAMETERS EXIT /B 1
IF DEFINED PACKAGE_PARAMETERS EXIT /B 1
IF DEFINED PACKAGE_NEV_PARAMETERS EXIT /B 1


EXIT /B 0
