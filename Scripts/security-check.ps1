param(
    [Parameter(Mandatory)][string]$Exclusions,
    [Parameter(Mandatory)][string]$Programs
)

$result = [ordered]@{
    defenderAvailable = $false
    defenderAllExcluded = $false
    missingExclusions = @()
    firewallAllConfigured = $false
    missingFirewallPrograms = @()
}

try {
    $pref = Get-MpPreference -ErrorAction Stop
    $result.defenderAvailable = $true
    $existing = @($pref.ExclusionPath | ForEach-Object { $_.TrimEnd('\') })
    foreach ($raw in ($Exclusions -split '\|')) {
        $p = $raw.Trim().TrimEnd('\')
        if (-not $p) { continue }
        $found = $false
        foreach ($e in $existing) {
            if ($e -eq $p -or $p.StartsWith($e, [StringComparison]::OrdinalIgnoreCase)) {
                $found = $true
                break
            }
        }
        if (-not $found) { $result.missingExclusions += $p }
    }
    $result.defenderAllExcluded = ($result.missingExclusions.Count -eq 0)
}
catch {
    $result.defenderAvailable = $false
    $result.defenderAllExcluded = $false
}

foreach ($prog in ($Programs -split '\|')) {
    $prog = $prog.Trim()
    if (-not $prog -or -not (Test-Path -LiteralPath $prog)) { continue }
    $base = [IO.Path]::GetFileNameWithoutExtension($prog)
    $inName = "Zapret $base (in)"
    $outName = "Zapret $base (out)"
    $inOk = $null -ne (Get-NetFirewallRule -DisplayName $inName -ErrorAction SilentlyContinue | Where-Object { $_.Enabled -eq 'True' } | Select-Object -First 1)
    $outOk = $null -ne (Get-NetFirewallRule -DisplayName $outName -ErrorAction SilentlyContinue | Where-Object { $_.Enabled -eq 'True' } | Select-Object -First 1)
    if (-not $inOk -or -not $outOk) {
        $result.missingFirewallPrograms += $prog
    }
}

$result.firewallAllConfigured = ($result.missingFirewallPrograms.Count -eq 0)

$result | ConvertTo-Json -Compress -Depth 4
