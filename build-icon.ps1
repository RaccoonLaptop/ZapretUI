# Build valid multi-size app.ico from generated round PNG.
$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$SourcePng = Join-Path $ProjectDir "Assets\source.png"
$OutIco = Join-Path $ProjectDir "Assets\app.ico"

New-Item -ItemType Directory -Path (Split-Path $OutIco) -Force | Out-Null

Add-Type -AssemblyName System.Drawing

function New-RoundPng {
    param([string]$Path)

    $size = 512
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.Clear([System.Drawing.Color]::Transparent)

        $rect = New-Object System.Drawing.Rectangle 16, 16, ($size - 32), ($size - 32)
        $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect,
            ([System.Drawing.Color]::FromArgb(16, 18, 26)),
            ([System.Drawing.Color]::FromArgb(34, 38, 51)),
            45
        $g.FillEllipse($grad, $rect)
        $grad.Dispose()

        $ringPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(107, 159, 255), 18)
        $g.DrawEllipse($ringPen, 48, 48, $size - 96, $size - 96)
        $ringPen.Dispose()

        $green = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(143, 212, 96))
        $warn = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(232, 184, 106))
        $accent = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(107, 159, 255))

        $g.FillEllipse($green, 118, 244, 62, 62)
        $g.FillEllipse($warn, 226, 198, 62, 62)
        $g.FillEllipse($accent, 334, 152, 62, 62)

        $line = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(240, 112, 136), 11)
        $g.DrawLine($line, 149, 274, 257, 229)
        $g.DrawLine($line, 257, 229, 365, 183)

        $line.Dispose()
        $green.Dispose()
        $warn.Dispose()
        $accent.Dispose()
    }
    finally {
        $g.Dispose()
    }

    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

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

New-RoundPng -Path $SourcePng
$bytes = Convert-PngToIcoBytes -Path $SourcePng
[System.IO.File]::WriteAllBytes($OutIco, $bytes)
Write-Host "Icon created: $OutIco ($($bytes.Length) bytes)" -ForegroundColor Green
