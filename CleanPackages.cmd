@ECHO OFF


SET PACKAGES_DIRECTORY=build

dotnet clean --configuration Debug
dotnet clean --configuration Release
IF EXIST "%PACKAGES_DIRECTORY%" RMDIR "%PACKAGES_DIRECTORY%" /S /Q
