@ECHO OFF


:INITIALIZE_ARGUMENTS
SET %1
REM ECHO arg1 = %1
SET %2
REM ECHO arg2 = %2


:INITIALIZE_VARIABLES
SET CONFIGURATION=Release
SET BUILD_VERSION=1.0.0


:SET_CONFIGURATION
IF "%config%"=="" GOTO SET_BUILD_VERSION
SET CONFIGURATION=%config%


:SET_BUILD_VERSION
IF "%version%"=="" GOTO RESTORE_PACKAGES
SET BUILD_VERSION=%version%

ECHO ---------------------------------------------------
REM ECHO Building "%config%" packages with version "%version%"...
ECHO Building "%CONFIGURATION%" packages with version "%BUILD_VERSION%"...
ECHO ---------------------------------------------------


:RESTORE_PACKAGES
dotnet restore "src\Castle.Facilities.NHibernateIntegration\Castle.Facilities.NHibernateIntegration.csproj"
dotnet restore "src\Castle.Facilities.NHibernateIntegration.Tests\Castle.Facilities.NHibernateIntegration.Tests.csproj"
dotnet restore "tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.csproj"


:BUILD
dotnet build "Castle.Facilities.NHibernateIntegration.sln" -p:PACKAGE_VERSION=%BUILD_VERSION% -c %CONFIGURATION% --no-restore || EXIT /B 4
dotnet build "tools\Explicit.NuGet.Versions\Explicit.NuGet.Versions.sln" --no-restore


:NUGET_EXPLICIT_VERSIONS

"tools\Explicit.NuGet.Versions\build\nev.exe" "build" "Castle."


:TEST

ECHO ----------------
ECHO Running Tests...
ECHO ----------------

dotnet test "src\Castle.Facilities.NHibernateIntegration.Tests" --no-restore || EXIT /B 8
