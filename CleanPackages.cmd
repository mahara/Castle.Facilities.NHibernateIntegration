@ECHO OFF


SET PACKAGES_DIRECTORY=build

dotnet clean "Castle.Facilities.NHibernateIntegration.sln" --configuration Debug
dotnet clean "Castle.Facilities.NHibernateIntegration.sln" --configuration Release

IF EXIST "%PACKAGES_DIRECTORY%" RMDIR "%PACKAGES_DIRECTORY%" /S /Q
