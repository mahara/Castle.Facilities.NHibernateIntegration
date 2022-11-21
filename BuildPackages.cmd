@ECHO OFF


@CALL "Build.Properties.cmd"

IF NOT DEFINED BUILD_CONFIGURATION_FOLDER_PATH EXIT /B 1


@CALL "%BUILD_CONFIGURATION_FOLDER_PATH%\BuildPackages.cmd" --configuration Release --version 5.4.0 %1
