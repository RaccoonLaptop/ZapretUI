# Build ZapretUI-Setup.exe (self-contained, no .NET install required)
$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$ParentDir = Split-Path $ProjectDir -Parent
$StagingDir = Join-Path $ParentDir "ZapretUI-installer-staging"
$DistDir = Join-Path $ParentDir "ZapretUI-dist"
$Packaging = Join-Path $ProjectDir "packaging"
$IssFile = Join-Path $ProjectDir "installer\ZapretUI.iss"

[xml]$csproj = Get-Content (Join-Path $ProjectDir "ZapretUI.csproj")
$version = $csproj.Project.PropertyGroup.Version | Select-Object -First 1
if (-not $version) { $version = "1.0.0" }

Write-Host "Building Zapret UI v$version (self-contained installer)..." -ForegroundColor Cyan

if (Test-Path $StagingDir) { Remove-Item $StagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

Push-Location $ProjectDir
dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishReadyToRun=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $StagingDir
if ($LASTEXITCODE -ne 0) { Pop-Location; exit 1 }
Pop-Location

Get-ChildItem $Packaging -File | Where-Object { $_.Name -ne 'update.json' } | ForEach-Object {
    Copy-Item $_.FullName $StagingDir -Force
}

$isccCandidates = @(
    (Join-Path $ProjectDir "tools\innosetup\ISCC.exe"),
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host "Inno Setup 6 not found. Installing via winget..." -ForegroundColor Yellow
    winget install --id JRSoftware.InnoSetup -e --accept-package-agreements --accept-source-agreements
    $iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $iscc) {
    Write-Error "ISCC.exe not found. Install Inno Setup 6: https://jrsoftware.org/isinfo.php"
}

$stagingAbs = (Resolve-Path $StagingDir).Path
$distAbs = (Resolve-Path $DistDir).Path

& $iscc "/DAppVersion=$version" "/DSourceDir=$stagingAbs" "/DOutputDir=$distAbs" $IssFile
if ($LASTEXITCODE -ne 0) { exit 1 }

$setupExe = Join-Path $DistDir "ZapretUI-Setup.exe"

# Update zip for in-app auto-update (same self-contained build)
$UpdateDir = Join-Path $ParentDir "ZapretUI-update"
$ZipName = "ZapretUI-Program.zip"
New-Item -ItemType Directory -Path $UpdateDir -Force | Out-Null
if (Test-Path (Join-Path $UpdateDir $ZipName)) { Remove-Item (Join-Path $UpdateDir $ZipName) -Force }
Compress-Archive -Path "$StagingDir\*" -DestinationPath (Join-Path $UpdateDir $ZipName) -Force

$manifest = @{
    version = $version
    packageFile = $ZipName
    downloadUrl = "https://github.com/RaccoonLaptop/ZapretUI/releases/download/v$version/$ZipName"
    installerUrl = "https://github.com/RaccoonLaptop/ZapretUI/releases/download/v$version/ZapretUI-Setup.exe"
} | ConvertTo-Json -Depth 3
Set-Content -Path (Join-Path $UpdateDir "update.json") -Value $manifest -Encoding UTF8
Copy-Item (Join-Path $UpdateDir "update.json") (Join-Path $ProjectDir "update.json") -Force

Write-Host ""
Write-Host "Done: $setupExe" -ForegroundColor Green
Write-Host "Update zip: $UpdateDir\$ZipName" -ForegroundColor Green
Write-Host "Give users ZapretUI-Setup.exe - one file, no .NET install." -ForegroundColor Yellow
