@echo off
set "msbuild=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild"
for /r "%~dp0" %%a in (*.csproj) do call :build "%%~nxa"
pause
exit

:build
echo.Building %1
"%msbuild%" %1