param(
    [Parameter(Mandatory)][string]$InstallerPath,
    [Parameter(Mandatory)][string]$TargetDir,
    [Parameter(Mandatory)][int]$ProcessId,
    [Parameter(Mandatory)][string]$ExePath,
    [string]$LogFile = "",
    [string]$StagingDir = ""
)

$ErrorActionPreference = "Stop"

function Write-Log($Message) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    if ($LogFile) { Add-Content -LiteralPath $LogFile -Value $line -Encoding UTF8 }
}

function Get-InstallerErrorMessage([int]$ExitCode) {
    switch ($ExitCode) {
        5 { return "UPDATE_ACCESS_DENIED" }
        default { return "Installer exited with code $ExitCode" }
    }
}

. (Join-Path $PSScriptRoot 'stop-bypass.ps1')

try {
    Write-Log "Installer update started"
    Write-Log "Installer: $InstallerPath"
    Write-Log "Target: $TargetDir"

    if (-not (Test-Path -LiteralPath $InstallerPath)) {
        throw "Installer not found: $InstallerPath"
    }

    if ($ProcessId -gt 0) {
        Write-Log "Waiting for application to close (PID $ProcessId)..."
        $waited = 0
        while ($waited -lt 120) {
            if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) { break }
            Start-Sleep -Milliseconds 500
            $waited++
        }
        Start-Sleep -Seconds 2
    }

    Stop-BypassForUpdate -LogAction { param($Message) Write-Log $Message }

    $args = @(
        "/VERYSILENT",
        "/SUPPRESSMSGBOXES",
        "/NORESTART",
        "/CLOSEAPPLICATIONS",
        "/DIR=`"$TargetDir`""
    )
    Write-Log "Installing update..."
    Write-Log "Running: $InstallerPath $($args -join ' ')"
    $proc = Start-Process -FilePath $InstallerPath -ArgumentList $args -Wait -PassThru
    if ($proc.ExitCode -ne 0) {
        throw (Get-InstallerErrorMessage $proc.ExitCode)
    }

    if ($StagingDir -and (Test-Path -LiteralPath $StagingDir)) {
        Remove-Item -LiteralPath $StagingDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $launch = $ExePath
    if (-not (Test-Path -LiteralPath $launch)) {
        $launch = Join-Path $TargetDir "ZapretUI.exe"
    }

    if (Test-Path -LiteralPath $launch) {
        Write-Log "Starting Zapret UI..."
        Write-Log "Starting: $launch"
        Start-Process -FilePath $launch -WorkingDirectory $TargetDir -Verb RunAs
    }

    Write-Log "Installer update completed"
    exit 0
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    exit 1
}
