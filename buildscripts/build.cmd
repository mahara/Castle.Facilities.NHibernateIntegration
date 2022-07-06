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


IF "%1" == "" GOTO no_config
IF "%1" NEQ "" GOTO set_config

:set_config
SET BUILD_CONFIGURATION=%1
GOTO restore_packages

:no_config
SET BUILD_CONFIGURATION=Release
GOTO restore_packages

:restore_packages
dotnet restore .\src\Castle.Facilities.NHibernateIntegration\Castle.Facilities.NHibernateIntegration.csproj || EXIT /B 1
dotnet restore .\src\Castle.Facilities.NHibernateIntegration.Tests\Castle.Facilities.NHibernateIntegration.Tests.csproj || EXIT /B 1

dotnet restore .\tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.csproj || EXIT /B 1
GOTO build

:build
dotnet build Castle.Facilities.NHibernateIntegration.sln --configuration %BUILD_CONFIGURATION% --no-restore || EXIT /B 1

dotnet build .\tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln --configuration "Release" --no-restore || EXIT /B 1
.\tools\Explicit.NuGet.Versions\bin\nev.exe ".\build" "Castle." || EXIT /B 1
GOTO test

:test

REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56

ECHO ----------------------------
ECHO Running .NET (net6.0) Tests
ECHO ----------------------------

dotnet test .\src\Castle.Facilities.NHibernateIntegration.Tests --configuration %BUILD_CONFIGURATION% --framework net6.0 --no-build --output .\src\Castle.Facilities.NHibernateIntegration.Tests\bin\%BUILD_CONFIGURATION%\net6.0 --results-directory .\src\Castle.Facilities.NHibernateIntegration.Tests\bin\%BUILD_CONFIGURATION% --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests-Net-TestResults.xml;format=nunit3" || EXIT /B 1

ECHO ------------------------------------
ECHO Running .NET Framework (net48) Tests
ECHO ------------------------------------

dotnet test .\src\Castle.Facilities.NHibernateIntegration.Tests --configuration %BUILD_CONFIGURATION% --framework net48 --no-build --output .\src\Castle.Facilities.NHibernateIntegration.Tests\bin\%BUILD_CONFIGURATION%\net48 --results-directory .\src\Castle.Facilities.NHibernateIntegration.Tests\bin\%BUILD_CONFIGURATION% --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests-NetFramework-TestResults.xml;format=nunit3" || EXIT /B 1



