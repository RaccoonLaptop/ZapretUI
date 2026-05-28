param(
    [Parameter(Mandatory = $true)]
    [string]$TargetDir
)

$ErrorActionPreference = 'Stop'

function Test-ZapretRoot {
    param([string]$Dir)
    return (Test-Path (Join-Path $Dir 'service.bat')) -and (Test-Path (Join-Path $Dir 'bin'))
}

if (Test-ZapretRoot $TargetDir) {
    Write-Host "Zapret already installed at $TargetDir"
    exit 0
}

$headers = @{ 'User-Agent' = 'ZapretUI-Installer' }
Write-Host 'Fetching latest Flowseal release...'
$release = Invoke-RestMethod -Uri 'https://api.github.com/repos/Flowseal/zapret-discord-youtube/releases/latest' -Headers $headers
$asset = $release.assets | Where-Object { $_.name -like '*.zip' } | Select-Object -First 1
if (-not $asset) { throw 'Zip asset not found in latest release.' }

$tmp = Join-Path $env:TEMP ("zapret-bootstrap-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp -Force | Out-Null
try {
    $zipPath = Join-Path $tmp 'zapret.zip'
    Write-Host "Downloading $($asset.name)..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -Headers $headers

    $extractDir = Join-Path $tmp 'extract'
    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
    Copy-Item -Path (Join-Path $extractDir '*') -Destination $TargetDir -Recurse -Force

    if (-not (Test-ZapretRoot $TargetDir)) {
        throw 'Downloaded package is invalid (service.bat or bin missing).'
    }

    $utilsDir = Join-Path $TargetDir 'utils'
    $targetsFile = Join-Path $utilsDir 'targets.txt'
    $bundledTargets = Join-Path $PSScriptRoot 'targets.txt'
    if (-not (Test-Path $targetsFile) -and (Test-Path $bundledTargets)) {
        New-Item -ItemType Directory -Path $utilsDir -Force | Out-Null
        Copy-Item -Path $bundledTargets -Destination $targetsFile -Force
        Write-Host "Installed default targets.txt for strategy tests."
    }

    Write-Host "Zapret installed to $TargetDir (version $($release.tag_name))"
}
finally {
    if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue }
}
