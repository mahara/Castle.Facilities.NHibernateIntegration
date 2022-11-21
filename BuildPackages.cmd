@ECHO OFF

REM ================
REM EXIT CODES
REM ================
REM 0  = Build completed successfully.
REM 1  = Missing required build infrastructure.
REM 2  = Invalid command-line argument.
REM >2 = Build operation failure propagated from inner BuildPackages.cmd.
REM 99 = Failed to ensure a clean build environment.



REM ============================================================================
REM Main
REM ============================================================================


@CALL "%~dp0Build.Environment.cmd"
IF ERRORLEVEL 1 (
    ECHO Failed to ensure a clean build environment before execution.
    EXIT /B 99
)


SETLOCAL


REM ================================================================================
REM Initialize Build Properties
REM
REM Build.Properties.cmd defines build infrastructure environment variables
REM required by the build execution workflow.
REM
REM CMD batch execution does not create an isolated environment scope by default.
REM Variables modified during batch execution remain in the current CMD environment
REM and can affect subsequent script executions.
REM
REM SETLOCAL establishes the environment lifetime boundary
REM before Build.Properties.cmd execution.
REM All environment changes performed after SETLOCAL
REM are scoped to this batch execution scope
REM and are automatically reverted when the local scope is ended.
REM
REM Build.Environment.cmd ensures that no owned build environment variables exist
REM before entering the local execution scope.
REM ================================================================================

@CALL "%~dp0Build.Properties.cmd"

IF NOT DEFINED BUILD_CONFIGURATION_FOLDER_PATH (
    ECHO Missing required build infrastructure.
    EXIT /B 1
)


@CALL "%BUILD_CONFIGURATION_FOLDER_PATH%\BuildPackages.cmd" %*
EXIT /B %ERRORLEVEL%


EXIT /B 0
