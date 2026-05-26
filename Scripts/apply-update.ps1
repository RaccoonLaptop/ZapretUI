param(
    [Parameter(Mandatory)][string]$SourceDir,
    [Parameter(Mandatory)][string]$TargetDir,
    [Parameter(Mandatory)][int]$ProcessId,
    [Parameter(Mandatory)][string]$ExePath,
    [string]$LogFile = ""
)

$ErrorActionPreference = "Stop"

function Write-Log($Message) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    if ($LogFile) { Add-Content -LiteralPath $LogFile -Value $line -Encoding UTF8 }
}

try {
    Write-Log "Updater started"
    Write-Log "Source: $SourceDir"
    Write-Log "Target: $TargetDir"

    if ($ProcessId -gt 0) {
        Write-Log "Waiting for process $ProcessId..."
        $waited = 0
        while ($waited -lt 60) {
            if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) { break }
            Start-Sleep -Milliseconds 500
            $waited++
        }
        Start-Sleep -Seconds 1
    }

    if (-not (Test-Path -LiteralPath $SourceDir)) {
        throw "Source folder not found: $SourceDir"
    }

    $backupDir = Join-Path $env:TEMP ("ZapretUI-backup-" + [guid]::NewGuid().ToString("N"))
    Write-Log "Backup: $backupDir"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

    if (Test-Path -LiteralPath $TargetDir) {
        Copy-Item -LiteralPath $TargetDir -Destination $backupDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

    $copied = $false
    for ($i = 1; $i -le 15; $i++) {
        try {
            Write-Log "Copy attempt $i..."
            Get-ChildItem -LiteralPath $SourceDir -Force | ForEach-Object {
                $dest = Join-Path $TargetDir $_.Name
                Copy-Item -LiteralPath $_.FullName -Destination $dest -Recurse -Force
            }
            $copied = $true
            break
        }
        catch {
            Write-Log "Copy failed: $($_.Exception.Message)"
            Start-Sleep -Seconds 1
        }
    }

    if (-not $copied) {
        Write-Log "Restore from backup..."
        if (Test-Path -LiteralPath $backupDir) {
            Get-ChildItem -LiteralPath $TargetDir -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            Get-ChildItem -LiteralPath $backupDir | ForEach-Object {
                Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $TargetDir $_.Name) -Recurse -Force
            }
        }
        throw "Failed to copy update files after multiple attempts"
    }

    Write-Log "Update applied successfully"
    Remove-Item -LiteralPath $backupDir -Recurse -Force -ErrorAction SilentlyContinue

    $launch = $ExePath
    if (-not (Test-Path -LiteralPath $launch)) {
        $launch = Join-Path $TargetDir "ZapretUI.exe"
    }

    if (Test-Path -LiteralPath $launch) {
        Write-Log "Starting: $launch"
        Start-Process -FilePath $launch -WorkingDirectory $TargetDir
    }

    exit 0
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    try {
        Add-Type -AssemblyName System.Windows.Forms -ErrorAction SilentlyContinue
        [System.Windows.Forms.MessageBox]::Show(
            "Ne udalos obnovit Zapret UI.`n$($_.Exception.Message)",
            "Zapret UI",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
    } catch { }
    exit 1
}
