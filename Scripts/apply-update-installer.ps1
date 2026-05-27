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

try {
    Write-Log "Installer update started"
    Write-Log "Installer: $InstallerPath"
    Write-Log "Target: $TargetDir"

    if (-not (Test-Path -LiteralPath $InstallerPath)) {
        throw "Installer not found: $InstallerPath"
    }

    if ($ProcessId -gt 0) {
        Write-Log "Waiting for process $ProcessId..."
        $waited = 0
        while ($waited -lt 120) {
            if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) { break }
            Start-Sleep -Milliseconds 500
            $waited++
        }
        Start-Sleep -Seconds 2
    }

    $args = @(
        "/VERYSILENT",
        "/SUPPRESSMSGBOXES",
        "/NORESTART",
        "/CLOSEAPPLICATIONS",
        "/DIR=`"$TargetDir`""
    )
    Write-Log "Running: $InstallerPath $($args -join ' ')"
    $proc = Start-Process -FilePath $InstallerPath -ArgumentList $args -Wait -PassThru
    if ($proc.ExitCode -ne 0) {
        throw "Installer exited with code $($proc.ExitCode)"
    }

    if ($StagingDir -and (Test-Path -LiteralPath $StagingDir)) {
        Remove-Item -LiteralPath $StagingDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $launch = $ExePath
    if (-not (Test-Path -LiteralPath $launch)) {
        $launch = Join-Path $TargetDir "ZapretUI.exe"
    }

    if (Test-Path -LiteralPath $launch) {
        Write-Log "Starting: $launch"
        Start-Process -FilePath $launch -WorkingDirectory $TargetDir -Verb RunAs
    }

    Write-Log "Installer update completed"
    exit 0
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    try {
        Add-Type -AssemblyName System.Windows.Forms -ErrorAction SilentlyContinue
        [System.Windows.Forms.MessageBox]::Show(
            "Не удалось обновить Zapret UI.`n$($_.Exception.Message)",
            "Zapret UI",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
    } catch { }
    exit 1
}
