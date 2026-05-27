# Build multi-size app.ico + source.png for Zapret UI (taskbar, tray, installer).
$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$SourcePng = Join-Path $ProjectDir "Assets\source.png"
$OutIco = Join-Path $ProjectDir "Assets\app.ico"

New-Item -ItemType Directory -Path (Split-Path $OutIco) -Force | Out-Null
Add-Type -AssemblyName System.Drawing

function New-ZapretIconPng {
    param([string]$Path)

    $size = 512
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $g.Clear([System.Drawing.Color]::Transparent)

        $outer = New-Object System.Drawing.Rectangle 8, 8, ($size - 16), ($size - 16)
        $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $outer,
            ([System.Drawing.Color]::FromArgb(12, 14, 22)),
            ([System.Drawing.Color]::FromArgb(32, 38, 58)),
            135
        $g.FillEllipse($grad, $outer)
        $grad.Dispose()

        $ring = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(107, 159, 255), 10)
        $g.DrawEllipse($ring, 24, 24, $size - 48, $size - 48)
        $ring.Dispose()

        $arcPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(143, 212, 96), 9)
        $arcPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $arcPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawArc($arcPen, 72, 72, 368, 368, 210, 95)
        $arcPen.Dispose()

        $font = New-Object System.Drawing.Font("Segoe UI", 248, [System.Drawing.FontStyle]::Bold)
        $textBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 238, 248))
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $textRect = New-Object System.Drawing.RectangleF 0, 18, $size, $size
        $g.DrawString("Z", $font, $textBrush, $textRect, $format)
        $font.Dispose()
        $textBrush.Dispose()
        $format.Dispose()
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
        $sizes = @(16, 24, 32, 48, 64, 128, 256)
        $stream = New-Object System.IO.MemoryStream
        $writer = New-Object System.IO.BinaryWriter $stream
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

New-ZapretIconPng -Path $SourcePng
$bytes = Convert-PngToIcoBytes -Path $SourcePng
[System.IO.File]::WriteAllBytes($OutIco, $bytes)
Write-Host "Icon created: $OutIco ($($bytes.Length) bytes)" -ForegroundColor Green
Write-Host "Source PNG: $SourcePng" -ForegroundColor Green
