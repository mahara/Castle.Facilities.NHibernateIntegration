@ECHO OFF


@CALL "Build.Properties.cmd"

IF NOT DEFINED ARTIFACTS_FOLDER_PATH EXIT /B 1


REM dotnet clean --configuration Debug
REM dotnet clean --configuration Release

IF EXIST "%ARTIFACTS_FOLDER_PATH%" (
    ECHO Deleting "%ARTIFACTS_FOLDER_PATH%" folder...

    RMDIR "%ARTIFACTS_FOLDER_PATH%\" /S /Q
)
