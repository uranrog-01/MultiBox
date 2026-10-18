# Regenerates docs/screenshots/*.png from the real UI with sample data (design/screenshots/capture.html).
# Needs Playwright's headless Chromium in %LOCALAPPDATA%\ms-playwright.
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$chrome = (Get-ChildItem "$env:LOCALAPPDATA\ms-playwright\chromium_headless_shell-*\chrome-headless-shell-win64\chrome-headless-shell.exe" | Sort-Object FullName | Select-Object -Last 1).FullName
$page = 'file:///' + ((Join-Path $PSScriptRoot 'capture.html') -replace '\\', '/')
$views = @{ dashboard = 'dashboard'; vault = 'savebox'; tasks = 'tasks'; settings = 'settings' }
foreach ($view in $views.Keys) {
  $out = Join-Path $root "docs\screenshots\$($views[$view]).png"
  & $chrome --headless --disable-gpu --hide-scrollbars --force-device-scale-factor=1 --allow-file-access-from-files `
    --virtual-time-budget=6000 --window-size=620,960 "--screenshot=$out" "${page}?view=$view" 2>$null | Out-Null
  Write-Host "Wrote $out"
}
