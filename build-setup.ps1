# Builds the modern MultiBox setup: dist\MultiBox-Setup-v<version>-win-x64.exe plus its SHA-256.
#
#   pwsh -File build-setup.ps1              full build (publish app, pack payload, build setup exe)
#   pwsh -File build-setup.ps1 -SkipApp     reuse the existing payload (setup UI work only)
#
# The setup exe is a single self-contained file with the whole MultiBox app embedded inside it.
param(
    [switch]$SkipApp
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$version = (([xml](Get-Content MultiBox.csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)
if (-not $version) { throw "Could not read <Version> from MultiBox.csproj" }

# Catch the easy mistake of bumping <Version> but leaving the file-metadata versions behind.
$fv = (([xml](Get-Content MultiBox.csproj)).Project.PropertyGroup.FileVersion | Where-Object { $_ } | Select-Object -First 1)
if ($fv -and $fv -ne $version -and -not $fv.StartsWith("$version."))
{
    Write-Warning "FileVersion in MultiBox.csproj is $fv but Version is $version. Update AssemblyVersion/FileVersion too so the exe metadata matches the setup."
}

$name = "MultiBox-Setup-v$version-win-x64"
$appStage = Join-Path $PSScriptRoot "dist\app-payload"
$setupOut = Join-Path $PSScriptRoot "dist\$name"
$setupExe = Join-Path $setupOut "MultiBox.Setup.exe"
$finalExe = Join-Path $PSScriptRoot "dist\$name.exe"
$finalHash = "$finalExe.sha256"

# Explorer, search indexing or antivirus can hold a brief handle on files in dist\;
# retry the delete instead of failing the whole build.
function Remove-Stale([string]$Path, [switch]$Recurse)
{
    for ($attempt = 1; $attempt -le 10; $attempt++)
    {
        if (-not (Test-Path -LiteralPath $Path)) { return }
        try
        {
            if ($Recurse) { Remove-Item -LiteralPath $Path -Recurse -Force }
            else { Remove-Item -LiteralPath $Path -Force }
            return
        }
        catch { Start-Sleep -Milliseconds 400 }
    }
    throw "Could not delete '$Path' - it is locked by another program. Close any window using it and run the build again."
}

Write-Host ""
Write-Host "  MultiBox Setup builder" -ForegroundColor Green
Write-Host "  Version $version" -ForegroundColor DarkGray
Write-Host ""

# ---------------------------------------------------------------- 1. app payload
if (-not $SkipApp) {
    Write-Host "[1/4] Publishing MultiBox app..." -ForegroundColor Cyan
    Remove-Stale -Path $appStage -Recurse

    dotnet publish MultiBox.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $appStage
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish of MultiBox failed" }

    Get-ChildItem $appStage -Recurse -Include *.xml, *.pdb | Remove-Item -Force
}
elseif (-not (Test-Path (Join-Path $PSScriptRoot "setup\payload.zip"))) {
    throw "-SkipApp was set but setup\payload.zip does not exist yet. Run a full build once first."
}
else {
    Write-Host "[1/4] Reusing existing payload (app publish skipped)." -ForegroundColor DarkYellow
}

# ---------------------------------------------------------------- 2. payload archive
$payloadZip = Join-Path $PSScriptRoot "setup\payload.zip"
$payloadJson = Join-Path $PSScriptRoot "setup\payload.json"

if (-not $SkipApp) {
    Write-Host "[2/4] Packing payload..." -ForegroundColor Cyan
    Remove-Stale -Path $payloadZip

    # Deterministic-ish archive of the staged app.
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $appStage, $payloadZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    $stageRoot = $appStage.TrimEnd('\') + '\'
    $files = @(Get-ChildItem $appStage -Recurse -File | ForEach-Object {
        [pscustomobject]@{
            path  = $_.FullName.Substring($stageRoot.Length).Replace('\', '/')
            bytes = $_.Length
        }
    })
    $manifest = [pscustomobject]@{
        version    = $version
        generated  = (Get-Date).ToUniversalTime().ToString("o")
        totalBytes = ($files | Measure-Object -Property bytes -Sum).Sum
        files      = $files
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $payloadJson -Encoding utf8

    $sizeMb = [math]::Round((Get-Item $payloadZip).Length / 1MB, 1)
    Write-Host "      payload.zip: $sizeMb MB, $($files.Count) files" -ForegroundColor DarkGray
}
else {
    Write-Host "[2/4] Payload archive already present." -ForegroundColor DarkYellow
}

# ---------------------------------------------------------------- 3. setup executable
Write-Host "[3/4] Building setup executable..." -ForegroundColor Cyan
Remove-Stale -Path $setupOut -Recurse
Remove-Stale -Path $finalExe
Remove-Stale -Path $finalHash

dotnet publish setup\MultiBox.Setup.csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false `
    -p:EnableCompressionInSingleFile=true -p:EmbedPayload=true -o $setupOut
if ($LASTEXITCODE -ne 0) { throw "dotnet publish of MultiBox.Setup failed" }

Get-ChildItem $setupOut -Recurse -Include *.xml, *.pdb | Remove-Item -Force

# ---------------------------------------------------------------- 4. ship it
Write-Host "[4/4] Finalizing..." -ForegroundColor Cyan
Move-Item -LiteralPath $setupExe -Destination $finalExe -Force
$hash = (Get-FileHash $finalExe -Algorithm SHA256).Hash
Set-Content -LiteralPath $finalHash -Value "$hash  $name.exe" -Encoding ascii

$setupMb = [math]::Round((Get-Item $finalExe).Length / 1MB, 1)
Write-Host ""
Write-Host "  Setup:   $finalExe" -ForegroundColor Green
Write-Host "  Size:    $setupMb MB" -ForegroundColor DarkGray
Write-Host "  SHA-256: $hash" -ForegroundColor DarkGray
Write-Host ""
