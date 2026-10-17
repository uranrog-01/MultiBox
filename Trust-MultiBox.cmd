@echo off
setlocal
title MultiBox - Trust Publisher & Unblock Files

:: Self-elevate: an ADMIN machine-store install is silent (no per-cert
:: confirmation dialog). The only prompt is the standard UAC elevation.
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting administrator rights for a silent certificate install...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo ======================================================
echo   MultiBox - Trusted Publisher Setup (by zer0ne)
echo ======================================================
echo.
echo Running elevated: certificate installs below are silent
echo and apply to every user on this PC.
echo.

:: 1. Unblock downloaded files (removes Mark-of-the-Web)
echo [1/2] Unblocking downloaded files...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%~dp0*' -Recurse | Unblock-File" 2>nul
echo       Files unblocked successfully.
echo.

:: 2. Install the certificate chain into the MACHINE stores (silent when
::    elevated):
::    - zer0ne-root.cer -> LocalMachine\ROOT (trust anchor)
::    - zer0ne.cer (Code Signing leaf) -> LocalMachine\TrustedPublisher
echo [2/2] Installing zer0ne certificates...
if exist "%~dp0zer0ne-root.cer" (
    certutil -addstore -f "ROOT" "%~dp0zer0ne-root.cer"
) else (
    echo Note: zer0ne-root.cer not found in script directory.
)
if exist "%~dp0zer0ne.cer" (
    certutil -addstore -f "TrustedPublisher" "%~dp0zer0ne.cer"
) else (
    echo Note: zer0ne.cer not found in script directory.
)
echo.
echo ======================================================
echo   Setup Complete! MultiBox is now a verified publisher.
echo ======================================================
echo.
pause
