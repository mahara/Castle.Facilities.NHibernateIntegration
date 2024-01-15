@ECHO OFF


SET ARTIFACTS_FOLDER_NAME=artifacts

SET ARTIFACTS_FOLDER_PATH=%ARTIFACTS_FOLDER_NAME%

REM dotnet clean %1 --configuration Debug
REM dotnet clean %1 --configuration Release

IF EXIST "%ARTIFACTS_FOLDER_PATH%" (
    ECHO Deleting "%ARTIFACTS_FOLDER_PATH%" folder...

    RMDIR "%ARTIFACTS_FOLDER_PATH%\" /S /Q
)
