param(
    [Parameter(Mandatory)][string]$Exclusions,
    [Parameter(Mandatory)][string]$Programs,
    [string]$LogFile = ""
)

$ErrorActionPreference = "Continue"

function Write-Log($Message) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    if ($LogFile) { Add-Content -LiteralPath $LogFile -Value $line -Encoding UTF8 }
}

$exclusionList = $Exclusions -split '\|' | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
$programList = $Programs -split '\|' | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

Write-Log "Security setup started"

# Windows Defender exclusions
try {
    $defender = Get-MpPreference -ErrorAction Stop
    Write-Log "Defender found"
    foreach ($path in $exclusionList) {
        try {
            if ($defender.ExclusionPath -contains $path) {
                Write-Log "Defender exclusion exists: $path"
            } else {
                Add-MpPreference -ExclusionPath $path -ErrorAction Stop
                Write-Log "Defender exclusion added: $path"
            }
        }
        catch {
            Write-Log "Defender exclusion failed ($path): $($_.Exception.Message)"
        }
    }
}
catch {
    Write-Log "Defender not available: $($_.Exception.Message)"
}

# Windows Firewall rules
foreach ($prog in $programList) {
    $base = [IO.Path]::GetFileNameWithoutExtension($prog)
    foreach ($dir in @("in", "out")) {
        $ruleName = "Zapret $base ($dir)"
        try {
            $existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
            if ($existing) {
                Write-Log "Firewall rule exists: $ruleName"
                continue
            }
            $direction = if ($dir -eq "in") { "Inbound" } else { "Outbound" }
            New-NetFirewallRule -DisplayName $ruleName -Direction $direction -Program $prog `
                -Action Allow -Profile Any -Enabled True -ErrorAction Stop | Out-Null
            Write-Log "Firewall rule added: $ruleName"
        }
        catch {
            Write-Log "NetFirewallRule failed, trying netsh: $($_.Exception.Message)"
            try {
                $action = if ($dir -eq "in") { "in" } else { "out" }
                netsh advfirewall firewall add rule name="$ruleName" dir=$action action=allow `
                    program="$prog" enable=yes profile=any | Out-Null
                Write-Log "netsh rule added: $ruleName"
            }
            catch {
                Write-Log "netsh failed: $($_.Exception.Message)"
            }
        }
    }
}

Write-Log "Security setup finished"
exit 0
