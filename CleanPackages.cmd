@ECHO OFF

REM ================
REM EXIT CODES
REM ================
REM 0  = Artifacts cleaned up successfully.
REM 1  = Missing required build infrastructure.
REM 2  = Invalid command-line argument.
REM 3  = Failed to delete artifacts folder.
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

IF NOT DEFINED ARTIFACTS_FOLDER_PATH (
    ECHO Missing required build infrastructure.
    EXIT /B 1
)


REM dotnet clean "%1" --configuration Debug
REM dotnet clean "%1" --configuration Release

IF EXIST "%ARTIFACTS_FOLDER_PATH%" (
    ECHO Deleting "%ARTIFACTS_FOLDER_PATH%" folder...

    RMDIR "%ARTIFACTS_FOLDER_PATH%" /S /Q
    IF ERRORLEVEL 1 (
        ECHO Failed to delete "%ARTIFACTS_FOLDER_PATH%" folder.
        EXIT /B 3
    )
)


EXIT /B 0
