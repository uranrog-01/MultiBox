@echo off
setlocal
title MultiBox - Authenticode Sign

:: zer0ne Code Signing leaf, issued by "zer0ne Root CA" (private key in
:: CurrentUser\My store). To renew or find the thumbprint:
::   certutil -user -store My | findstr /i zer0ne
:: Regenerate the whole chain with: powershell -File certs\gen-cert-chain.ps1
set "THUMB=37EE71C2620ADD585A0DC150ECDAE675297AA3AC"
set "TIMESTAMP=http://timestamp.digicert.com"

:: Locate the newest signtool.exe from the Windows SDK (x64)
set "SIGNTOOL="
for /f "usebackq delims=" %%s in (`powershell -NoProfile -Command "(Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match '\\x64\\' } | Sort-Object FullName -Descending | Select-Object -First 1).FullName"`) do set "SIGNTOOL=%%s"
if not defined SIGNTOOL (
    echo ERROR: signtool.exe not found. Install the Windows SDK ^(includes signtool^).
    exit /b 1
)
echo Using signtool: %SIGNTOOL%
echo.

echo [1/2] Signing publish\MultiBox.exe (release artifact)...
"%SIGNTOOL%" sign /sha1 %THUMB% /fd SHA256 /tr %TIMESTAMP% /td SHA256 "%~dp0publish\MultiBox.exe"
if errorlevel 1 goto :err

echo.
echo [2/2] Signing root MultiBox.exe...
"%SIGNTOOL%" sign /sha1 %THUMB% /fd SHA256 /tr %TIMESTAMP% /td SHA256 "%~dp0MultiBox.exe"
if errorlevel 1 echo   ^(Skipped - close the running MultiBox.exe and re-run this script.^)

echo.
echo Done. Verify with:
echo   "%SIGNTOOL%" verify /pa /v "%~dp0publish\MultiBox.exe"
pause
exit /b 0

:err
echo.
echo Signing failed. Check that the certificate with thumbprint %THUMB%
echo still exists in your personal store: certutil -user -store My
exit /b 1
