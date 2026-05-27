param(
    [Parameter(Mandatory)][string]$Action,
    [string]$Extra = "",
    [string]$Root = ""
)

$ErrorActionPreference = "Continue"

if (-not $Root) {
    $Root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    if (-not (Test-Path (Join-Path $Root "service.bat"))) {
        $Root = Split-Path $PSScriptRoot -Parent
        while ($Root -and -not (Test-Path (Join-Path $Root "service.bat"))) {
            $Root = Split-Path $Root -Parent
        }
    }
}

$BinPath = Join-Path $Root "bin\"
$ListsPath = Join-Path $Root "lists\"

function Write-Color($Text, $Color = "White") {
    Write-Output "{COLOR:$Color}$Text"
}

function Get-GameFilterVars {
    $flag = Join-Path $Root "utils\game_filter.enabled"
    if (-not (Test-Path $flag)) {
        return @{ GameFilter = "12"; GameFilterTCP = "12"; GameFilterUDP = "12" }
    }
    $mode = (Get-Content $flag -Raw).Trim().ToLower()
    switch ($mode) {
        "all" { return @{ GameFilter = "1024-65535"; GameFilterTCP = "1024-65535"; GameFilterUDP = "1024-65535" } }
        "tcp" { return @{ GameFilter = "1024-65535"; GameFilterTCP = "1024-65535"; GameFilterUDP = "12" } }
        "udp" { return @{ GameFilter = "1024-65535"; GameFilterTCP = "12"; GameFilterUDP = "1024-65535" } }
        default { return @{ GameFilter = "12"; GameFilterTCP = "12"; GameFilterUDP = "12" } }
    }
}

function Parse-StrategyArgs {
    param([string]$BatFile)
    $gf = Get-GameFilterVars
    $argsWithValue = @("sni", "host", "altorder")
    $lines = Get-Content -LiteralPath $BatFile
    $capture = $false
    $mergeargs = 0
    $result = ""
    $quote = '"'

    foreach ($rawLine in $lines) {
        $line = $rawLine -replace '!', 'EXCL_MARK'
        if ($line -match [regex]::Escape('%BIN%winws.exe')) { $capture = $true; continue }
        if (-not $capture) { continue }

        if ($line -match 'winws\.exe') {
            $line = $line -replace '.*winws\.exe', ''
        }

        $tokens = $line -split '\s+' | Where-Object { $_ -and $_ -ne '^' }
        foreach ($arg in $tokens) {
            if ($arg.StartsWith("--") -and $mergeargs -ne 0) { $mergeargs = 0 }

            $a = $arg
            if ($a.StartsWith($quote) -and $a.EndsWith($quote)) {
                $a = $a.Substring(1, $a.Length - 2)
                if ($a -match ':') { $a = "$quote$a$quote" }
                elseif ($a.StartsWith('@')) { $a = "$quote$($Root)\$($a.Substring(1))$quote" }
                elseif ($a.StartsWith('%BIN%')) { $a = "$quote$BinPath$($a.Substring(5))$quote" }
                elseif ($a.StartsWith('%LISTS%')) { $a = "$quote$ListsPath$($a.Substring(7))$quote" }
                else { $a = "$quote$Root\$a$quote" }
            }
            elseif ($a.StartsWith('%GameFilterTCP%')) { $a = $gf.GameFilterTCP }
            elseif ($a.StartsWith('%GameFilterUDP%')) { $a = $gf.GameFilterUDP }
            elseif ($a.StartsWith('%GameFilter%')) { $a = $gf.GameFilter }

            if ($mergeargs -eq 1) { $result += ",$a" }
            elseif ($mergeargs -eq 3) { $result += "=$a"; $mergeargs = 1 }
            else { $result += " $a" }

            if ($a.StartsWith("--")) { $mergeargs = 2 }
            elseif ($mergeargs -ge 1) {
                if ($mergeargs -eq 2) { $mergeargs = 1 }
                foreach ($v in $argsWithValue) {
                    if ($arg -eq $v) { $mergeargs = 3; break }
                }
            }
        }
    }
    return ($result -replace 'EXCL_MARK', '!').Trim()
}

switch ($Action) {
    "CheckStatus" {
        $svc = Get-Service -Name "zapret" -ErrorAction SilentlyContinue
        if ($svc) {
            $strat = (Get-ItemProperty "HKLM:\System\CurrentControlSet\Services\zapret" -Name "zapret-discord-youtube" -ErrorAction SilentlyContinue).'zapret-discord-youtube'
            if ($strat) { Write-Color "Service strategy: $strat" Cyan }
            Write-Color "zapret service: $($svc.Status)" $(if ($svc.Status -eq 'Running') { 'Green' } else { 'Yellow' })
        } else {
            Write-Color "zapret service: NOT installed" Yellow
        }

        foreach ($name in @("WinDivert")) {
            $s = Get-Service -Name $name -ErrorAction SilentlyContinue
            if ($s) { Write-Color "$name service: $($s.Status)" $(if ($s.Status -eq 'Running') { 'Green' } else { 'Yellow' }) }
            else { Write-Color "$name service: NOT installed" Yellow }
        }

        $sys = Get-ChildItem $BinPath -Filter "*.sys" -ErrorAction SilentlyContinue
        if (-not $sys) { Write-Color "WinDivert64.sys NOT found!" Red }

        $winws = Get-Process winws -ErrorAction SilentlyContinue
        if ($winws) { Write-Color "Bypass (winws.exe) is RUNNING" Green }
        else { Write-Color "Bypass (winws.exe) is NOT running" Red }
    }

    "InstallService" {
        if (-not $Extra) { Write-Color "Strategy file not specified" Red; exit 1 }
        $bat = Join-Path $Root $Extra
        if (-not (Test-Path $bat)) { Write-Color "File not found: $bat" Red; exit 1 }

        $parsed = Parse-StrategyArgs -BatFile $bat
        Write-Color "Installing service with strategy: $Extra" Cyan
        Write-Color "Args: $parsed" DarkGray

        netsh interface tcp set global timestamps=enabled | Out-Null
        sc.exe stop zapret 2>$null | Out-Null
        sc.exe delete zapret 2>$null | Out-Null

        $binPath = Join-Path $BinPath "winws.exe"
        sc.exe create zapret binPath= "`"$binPath`" $parsed" DisplayName= "zapret" start= auto
        sc.exe description zapret "Zapret DPI bypass software"
        sc.exe start zapret

        $name = [System.IO.Path]::GetFileNameWithoutExtension($Extra)
        Set-ItemProperty "HKLM:\System\CurrentControlSet\Services\zapret" -Name "zapret-discord-youtube" -Value $name -Type String -Force
        Write-Color "Service installed successfully" Green
    }

    "RemoveServices" {
        $svc = Get-Service zapret -ErrorAction SilentlyContinue
        if ($svc) {
            Stop-Service zapret -Force -ErrorAction SilentlyContinue
            sc.exe delete zapret | Out-Null
            Write-Color "zapret service removed" Green
        } else { Write-Color "zapret service not installed" Yellow }

        Get-Process winws -ErrorAction SilentlyContinue | Stop-Process -Force
        Write-Color "winws.exe stopped" Green

        foreach ($n in @("WinDivert", "WinDivert14")) {
            sc.exe stop $n 2>$null | Out-Null
            sc.exe delete $n 2>$null | Out-Null
        }
        Write-Color "WinDivert services cleaned" Green
    }

    "RunDiagnostics" {
        $bfe = Get-Service BFE -ErrorAction SilentlyContinue
        if ($bfe -and $bfe.Status -eq 'Running') { Write-Color "[OK] Base Filtering Engine" Green }
        else { Write-Color "[X] Base Filtering Engine not running" Red }

        $proxy = Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings" -ErrorAction SilentlyContinue
        if ($proxy.ProxyEnable -eq 1) { Write-Color "[?] System proxy enabled: $($proxy.ProxyServer)" Yellow }
        else { Write-Color "[OK] Proxy check passed" Green }

        $ts = netsh interface tcp show global 2>$null
        if ($ts -match "enabled") { Write-Color "[OK] TCP timestamps enabled" Green }
        else {
            Write-Color "[?] Enabling TCP timestamps..." Yellow
            netsh interface tcp set global timestamps=enabled | Out-Null
        }

        if (Get-Process AdguardSvc -ErrorAction SilentlyContinue) { Write-Color "[X] Adguard found - may cause Discord issues" Red }
        else { Write-Color "[OK] Adguard check passed" Green }

        $killers = Get-Service | Where-Object { $_.Name -like "*Killer*" }
        if ($killers) { Write-Color "[X] Killer services conflict with zapret" Red }
        else { Write-Color "[OK] Killer check passed" Green }

        $sys = Get-ChildItem $BinPath -Filter "*.sys" -ErrorAction SilentlyContinue
        if (-not $sys) { Write-Color "[X] WinDivert64.sys NOT found" Red }
        else { Write-Color "[OK] WinDivert driver present" Green }

        $conflicts = @("GoodbyeDPI", "discordfix_zapret", "winws1", "winws2") | Where-Object {
            Get-Service $_ -ErrorAction SilentlyContinue
        }
        if ($conflicts) {
            Write-Color "[X] Conflicting services: $($conflicts -join ', ')" Red
            foreach ($c in $conflicts) {
                Stop-Service $c -Force -ErrorAction SilentlyContinue
                sc.exe delete $c | Out-Null
                Write-Color "  Removed $c" Yellow
            }
        } else { Write-Color "[OK] No conflicting bypass services" Green }

        Write-Color "Diagnostics complete" Cyan
    }

    "UpdateHosts" {
        $hostsUrl = "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/refs/heads/main/.service/hosts"
        $tempFile = Join-Path $env:TEMP "zapret_hosts.txt"
        $hostsFile = "$env:SystemRoot\System32\drivers\etc\hosts"
        Invoke-WebRequest -Uri $hostsUrl -OutFile $tempFile -UseBasicParsing -TimeoutSec 15
        $lines = Get-Content $tempFile
        $first = $lines[0]; $last = $lines[-1]
        $hostsContent = Get-Content $hostsFile -ErrorAction SilentlyContinue
        $needs = ($hostsContent -notcontains $first) -or ($hostsContent -notcontains $last)
        if ($needs) {
            Write-Color "Hosts file needs update. Opening files..." Yellow
            Start-Process notepad $tempFile
            Start-Process explorer "/select,$hostsFile"
        } else {
            Write-Color "Hosts file is up to date" Green
            Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
        }
    }

    "RunTests" {
        $testScript = Join-Path $Root "utils\test zapret.ps1"
        if (-not (Test-Path $testScript)) { Write-Color "Test script not found" Red; exit 1 }
        & $testScript
    }

    default {
        Write-Color "Unknown action: $Action" Red
        exit 1
    }
}
