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


:INITIALIZE_VARIABLES
SET %1
REM ECHO arg1 = %1
SET %2
REM ECHO arg2 = %2

SET CONFIGURATION="Release"
SET BUILD_VERSION="1.0.0"

GOTO SET_CONFIGURATION


:SET_CONFIGURATION
IF "%config%"=="" GOTO SET_BUILD_VERSION
SET CONFIGURATION=%config%

GOTO SET_BUILD_VERSION


:SET_BUILD_VERSION
IF "%version%"=="" GOTO RESTORE_PACKAGES
SET BUILD_VERSION=%version%

GOTO RESTORE_PACKAGES


:RESTORE_PACKAGES
dotnet restore .\tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln
dotnet restore .\src\Castle.Facilities.NHibernateIntegration.sln

GOTO BUILD


:BUILD

ECHO ---------------------------------------------------
REM ECHO Building "%config%" packages with version "%version%"...
ECHO Building "%CONFIGURATION%" packages with version "%BUILD_VERSION%"...
ECHO ---------------------------------------------------

dotnet build .\tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln --no-restore
dotnet build Castle.Facilities.NHibernateIntegration.sln --configuration %CONFIGURATION% -property:APPVEYOR_BUILD_VERSION=%BUILD_VERSION% --no-restore

GOTO TEST


:TEST

ECHO ----------------
ECHO Running Tests...
ECHO ----------------

dotnet test .\src\Castle.Facilities.NHibernateIntegration.Tests --no-restore || exit /b 1

GOTO NUGET_EXPLICIT_VERSIONS


:NUGET_EXPLICIT_VERSIONS

.\tools\Explicit.NuGet.Versions\build\nev.exe ".\build" "Castle."



