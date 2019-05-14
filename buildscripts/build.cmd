@ECHO OFF
REM ****************************************************************************
REM Copyright 2004-2019 Castle Project - http://www.castleproject.org/
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

if "%1" == "" goto no_config
if "%1" NEQ "" goto set_config

:set_config
SET Configuration=%1
GOTO restore_packages

:no_config
SET Configuration=Release
GOTO restore_packages

:restore_packages
dotnet restore ./tools/Explicit.NuGet.Versions/Explicit.NuGet.Versions.csproj
dotnet restore ./buildscripts/BuildScripts.csproj
dotnet restore ./src/Castle.Facilities.NHibernateIntegration/Castle.Facilities.NHibernateIntegration.csproj
dotnet restore ./src/Castle.Facilities.NHibernateIntegration.Tests/Castle.Facilities.NHibernateIntegration.Tests.csproj

GOTO build

:build
dotnet build ./tools/Explicit.NuGet.Versions/Explicit.NuGet.Versions.sln
dotnet build Castle.Facilities.NHibernateIntegration.sln -c %Configuration%
GOTO test

:test

echo -------------
echo Running Tests
echo -------------

dotnet test src\Castle.Facilities.NHibernateIntegration.Tests || exit /b 1

GOTO nuget_explicit_versions

:nuget_explicit_versions

.\tools\Explicit.NuGet.Versions\build\nev.exe ".\build" "Castle.Facilities.NHibernateIntegration"



