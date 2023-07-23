@ECHO OFF
REM ****************************************************************************
REM Copyright 2004-2022 Castle Project - https://www.castleproject.org/
REM Licensed under the Apache License, Version 2.0 (the "License");
REM you may not use this file except in compliance with the License.
REM You may obtain a copy of the License at
REM
REM     http://www.apache.org/licenses/LICENSE-2.0
REM
REM Unless required by applicable law or agreed to in writing, software
REM distributed under the License is distributed on an "AS IS" BASIS,
REM WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
REM See the License for the specific language governing permissions and
REM limitations under the License.
REM ****************************************************************************


ECHO.

SET ARTIFACTS_FOLDER_PATH=build
SET NEV_BIN_FOLDER_PATH=tools\Explicit.NuGet.Versions\bin
SET NEV_OBJ_FOLDER_PATH=tools\Explicit.NuGet.Versions\obj

dotnet clean %1 --configuration Debug
dotnet clean %1 --configuration Release

IF EXIST "%ARTIFACTS_FOLDER_PATH%" (
    ECHO Deleting "%ARTIFACTS_FOLDER_PATH%" folder...

    RMDIR "%ARTIFACTS_FOLDER_PATH%" /S /Q
)

IF EXIST "%NEV_BIN_FOLDER_PATH%" (
    ECHO Deleting "%NEV_BIN_FOLDER_PATH%" folder...

    RMDIR "%NEV_BIN_FOLDER_PATH%" /S /Q
)
IF EXIST "%NEV_OBJ_FOLDER_PATH%" (
    ECHO Deleting "%NEV_OBJ_FOLDER_PATH%" folder...

    RMDIR "%NEV_OBJ_FOLDER_PATH%" /S /Q
)

ECHO.
