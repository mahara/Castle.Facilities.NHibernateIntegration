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
dotnet restore .\tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.csproj
dotnet restore .\src\Castle.Facilities.NHibernateIntegration\Castle.Facilities.NHibernateIntegration.csproj
dotnet restore .\src\Castle.Facilities.NHibernateIntegration.Tests\Castle.Facilities.NHibernateIntegration.Tests.csproj

GOTO BUILD


:BUILD

ECHO ----------------------------------------------------
ECHO Building "%CONFIGURATION%" packages with version "%BUILD_VERSION%"...
ECHO ----------------------------------------------------

dotnet build .\tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln --configuration "Release" --no-restore || exit /b 1
dotnet build .\Castle.Facilities.NHibernateIntegration.sln --configuration %CONFIGURATION% -property:APPVEYOR_BUILD_VERSION=%BUILD_VERSION% --no-restore || exit /b 1
.\tools\Explicit.NuGet.Versions\build\nev.exe ".\build" "Castle."

GOTO TEST


:TEST

REM https://github.com/Microsoft/vstest-docs/blob/main/docs/report.md
REM https://github.com/spekt/nunit.testlogger/issues/56

ECHO ----------------------------
ECHO Running .NET (net6.0) Tests
ECHO ----------------------------

dotnet test .\src\Castle.Facilities.NHibernateIntegration.Tests --configuration %CONFIGURATION% --framework net6.0 --no-build --output .\src\Castle.Facilities.NHibernateIntegration.Tests\bin\%CONFIGURATION%\net6.0 --results-directory .\src\Castle.Facilities.NHibernateIntegration.Tests\bin\%CONFIGURATION% --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests-Net-TestResults.xml;format=nunit3" || exit /b 1

ECHO ------------------------------------
ECHO Running .NET Framework (net48) Tests
ECHO ------------------------------------

dotnet test .\src\Castle.Facilities.NHibernateIntegration.Tests --configuration %CONFIGURATION% --framework net48 --no-build --output .\src\Castle.Facilities.NHibernateIntegration.Tests\bin\%CONFIGURATION%\net48 --results-directory .\src\Castle.Facilities.NHibernateIntegration.Tests\bin\%CONFIGURATION% --logger "nunit;LogFileName=Castle.Facilities.NHibernateIntegration.Tests-NetFramework-TestResults.xml;format=nunit3" || exit /b 1



