# Builds a clean release package: dist\MultiBox-v<version>-win-x64.zip plus its SHA-256.
# Usage (from the repository root):  pwsh -File build-release.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$version = ([xml](Get-Content MultiBox.csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$name = "MultiBox-v$version-win-x64"
$stage = Join-Path $PSScriptRoot "dist\$name"
$zip = Join-Path $PSScriptRoot "dist\$name.zip"

# Always start from an empty folder so files from older builds never ship.
if (Test-Path $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }

dotnet publish MultiBox.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -o $stage
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Package-reference documentation and debug files are for developers, not users.
Get-ChildItem $stage -Recurse -Include *.xml, *.pdb | Remove-Item -Force

Compress-Archive -Path "$stage\*" -DestinationPath $zip
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash
Set-Content -Path "$zip.sha256" -Value "$hash  $name.zip" -Encoding ascii

Write-Host "Package: $zip"
Write-Host "SHA-256: $hash"
