# Build valid multi-size app.ico from PNG (Windows 10+ PNG-in-ICO format)
$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$PngCandidates = @(
    (Join-Path $ProjectDir "Assets\source.png"),
    "C:\Users\Aero\.cursor\projects\c-Users-Aero-Desktop-zapret-discord-youtube-1-9-8c\assets\zapret-ui-icon.png"
)
$PngPath = $PngCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $PngPath) { throw "Source PNG not found for icon generation." }

$OutIco = Join-Path $ProjectDir "Assets\app.ico"
New-Item -ItemType Directory -Path (Split-Path $OutIco) -Force | Out-Null
$SourceCopy = Join-Path $ProjectDir "Assets\source.png"
if ($PngPath -ne $SourceCopy) { Copy-Item $PngPath $SourceCopy -Force }

Add-Type -AssemblyName System.Drawing

function Convert-PngToIcoBytes {
    param([string]$Path)
    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        $sizes = @(16, 32, 48, 256)
        $stream = New-Object System.IO.MemoryStream
        $writer = New-Object System.IO.BinaryWriter($stream)
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$sizes.Count)
        $offset = 6 + ($sizes.Count * 16)
        $chunks = New-Object System.Collections.Generic.List[byte[]]
        foreach ($size in $sizes) {
            $bmp = New-Object System.Drawing.Bitmap $image, $size, $size
            try {
                $pngStream = New-Object System.IO.MemoryStream
                $bmp.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
                $bytes = $pngStream.ToArray()
                $chunks.Add($bytes)
                $writer.Write([byte][Math]::Min($size, 255))
                $writer.Write([byte][Math]::Min($size, 255))
                $writer.Write([byte]0)
                $writer.Write([byte]0)
                $writer.Write([uint16]1)
                $writer.Write([uint16]32)
                $writer.Write([uint32]$bytes.Length)
                $writer.Write([uint32]$offset)
                $offset += $bytes.Length
            }
            finally { $bmp.Dispose() }
        }
        foreach ($chunk in $chunks) { $writer.Write($chunk) }
        $writer.Flush()
        return $stream.ToArray()
    }
    finally { $image.Dispose() }
}

$bytes = Convert-PngToIcoBytes -Path $PngPath
[System.IO.File]::WriteAllBytes($OutIco, $bytes)
Write-Host "Icon created: $OutIco ($($bytes.Length) bytes)" -ForegroundColor Green
