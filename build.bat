@echo off
setlocal EnableExtensions
cd /d "%~dp0"
title MultiBox Build

rem ================================================================
rem  MultiBox one-click build
rem
rem    build.bat           Build the setup installer. The version is
rem                        read from MultiBox.csproj automatically.
rem    build.bat quick     Rebuild only the setup UI, reusing the
rem                        existing app payload (much faster).
rem    build.bat zip       Also build the portable zip.
rem
rem  Output lands in  dist\MultiBox-Setup-v<version>-win-x64.exe
rem ================================================================

set "SKIP="
set "ALSO_ZIP="
for %%a in (%*) do (
    if /i "%%~a"=="quick" set "SKIP=-SkipApp"
    if /i "%%~a"=="zip"   set "ALSO_ZIP=1"
    if /i "%%~a"=="all"   set "ALSO_ZIP=1"
)

if not exist "MultiBox.csproj" (
    echo [ERROR] MultiBox.csproj not found. Keep build.bat in the repository root.
    echo.
    pause
    exit /b 1
)
if not exist "build-setup.ps1" (
    echo [ERROR] build-setup.ps1 not found next to build.bat.
    echo.
    pause
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] The dotnet CLI was not found on PATH.
    echo         Install the .NET 10 SDK: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

rem Read the version straight out of the project file.
set "VERSION="
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([xml](Get-Content 'MultiBox.csproj')).Project.PropertyGroup.Version"`) do set "VERSION=%%v"
if not defined VERSION set "VERSION=?"

cls
echo.
echo   ============================================================
echo     MultiBox  -  Build Setup Installer
echo     Version:  v%VERSION%
echo   ============================================================
echo.
echo   [1/2] Publishing app, packing payload, building setup...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "build-setup.ps1" %SKIP%
set "RC=%errorlevel%"
if not "%RC%"=="0" goto :failed

if defined ALSO_ZIP (
    echo.
    echo   [2/2] Building the portable zip...
    echo.
    powershell -NoProfile -ExecutionPolicy Bypass -File "build-release.ps1"
    set "RC=%errorlevel%"
    if not "%RC%"=="0" goto :failed
)

echo.
echo   ============================================================
echo     BUILD OK
echo.
echo     Output:  %CD%\dist\MultiBox-Setup-v%VERSION%-win-x64.exe
echo   ============================================================
echo.
start "" "%~dp0dist"
echo   Press any key to close this window...
pause >nul
exit /b 0

:failed
echo.
echo   ============================================================
echo     BUILD FAILED   (exit code %RC%)
echo     Read the messages above for the cause.
echo     Setup logs: %LocalAppData%\MultiBox\Setup\Logs
echo   ============================================================
echo.
pause
exit /b 1
