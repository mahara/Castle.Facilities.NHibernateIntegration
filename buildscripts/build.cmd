@ECHO OFF
REM ****************************************************************************
REM Copyright 2004-2024 Castle Project - https://www.castleproject.org/
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
GOTO build

:no_config
SET BUILD_CONFIGURATION=Release
GOTO build

:build
dotnet build "Castle.Facilities.NHibernateIntegration.sln" --configuration %BUILD_CONFIGURATION% || EXIT /B 1

dotnet build "tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln" --configuration Release
"tools\Explicit.NuGet.Versions\bin\nev.exe" "build" "Castle."
GOTO test

:test

REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
REM https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-vstest
REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56

ECHO ------------------------------------
ECHO Running .NET (net8.0) Unit Tests
ECHO ------------------------------------

dotnet test "src\Castle.Facilities.NHibernateIntegration.Tests\bin\%BUILD_CONFIGURATION%\net8.0\Castle.Facilities.NHibernateIntegration.Tests.dll" --results-directory "build\%BUILD_CONFIGURATION%" --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests_net8.0_TestResults.xml;format=nunit3"

ECHO ------------------------------------
ECHO Running .NET (net7.0) Unit Tests
ECHO ------------------------------------

dotnet test "src\Castle.Facilities.NHibernateIntegration.Tests\bin\%BUILD_CONFIGURATION%\net7.0\Castle.Facilities.NHibernateIntegration.Tests.dll" --results-directory "build\%BUILD_CONFIGURATION%" --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests_net7.0_TestResults.xml;format=nunit3"

ECHO ------------------------------------
ECHO Running .NET (net6.0) Unit Tests
ECHO ------------------------------------

dotnet test "src\Castle.Facilities.NHibernateIntegration.Tests\bin\%BUILD_CONFIGURATION%\net6.0\Castle.Facilities.NHibernateIntegration.Tests.dll" --results-directory "build\%BUILD_CONFIGURATION%" --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests_net6.0_TestResults.xml;format=nunit3"

ECHO --------------------------------------------
ECHO Running .NET Framework (net48) Unit Tests
ECHO --------------------------------------------

dotnet test "src\Castle.Facilities.NHibernateIntegration.Tests\bin\%BUILD_CONFIGURATION%\net48\Castle.Facilities.NHibernateIntegration.Tests.exe" --results-directory "build\%BUILD_CONFIGURATION%" --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests_net48_TestResults.xml;format=nunit3"
