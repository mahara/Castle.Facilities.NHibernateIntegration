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


SET PACKAGES_FOLDER=build
SET NEV_BIN_FOLDER=".\tools\Explicit.NuGet.Versions\bin"
SET NEV_OBJ_FOLDER=".\tools\Explicit.NuGet.Versions\obj"

dotnet clean --configuration Debug
dotnet clean --configuration Release
IF EXIST %PACKAGES_FOLDER% RMDIR %PACKAGES_FOLDER% /S /Q

IF EXIST %NEV_BIN_FOLDER% RMDIR %NEV_BIN_FOLDER% /S /Q
IF EXIST %NEV_OBJ_FOLDER% RMDIR %NEV_OBJ_FOLDER% /S /Q



