$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot

Add-Type -AssemblyName System.Drawing

function New-WizardBitmap {
    param(
        [int]$Width,
        [int]$Height,
        [string]$OutPath
    )

    $bmp = New-Object System.Drawing.Bitmap $Width, $Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $rect = New-Object System.Drawing.Rectangle 0, 0, $Width, $Height

        $bg1 = [System.Drawing.Color]::FromArgb(16, 18, 26)
        $bg2 = [System.Drawing.Color]::FromArgb(8, 9, 13)
        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $bg1, $bg2, 90
        $g.FillRectangle($brush, $rect)
        $brush.Dispose()

        $accent = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(107, 159, 255))
        $muted = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(143, 212, 96))
        $warn = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(232, 184, 106))
        $linePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(45, 51, 72), 2)

        $g.FillEllipse($accent, 38, [Math]::Max(24, $Height / 2 - 28), 56, 56)
        $g.FillEllipse($muted, [Math]::Max(104, $Width / 2 - 16), [Math]::Max(12, $Height / 2 - 20), 40, 40)
        $g.FillEllipse($warn, [Math]::Max(160, $Width - 76), [Math]::Max(24, $Height / 2 - 18), 36, 36)
        $g.DrawLine($linePen, 60, $Height / 2, $Width - 44, $Height / 2)

        $titleBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(232, 235, 244))
        $font = New-Object System.Drawing.Font("Segoe UI Semibold", 14, [System.Drawing.FontStyle]::Bold)
        $subFont = New-Object System.Drawing.Font("Segoe UI", 9)
        $g.DrawString("Zapret UI", $font, $titleBrush, 22, 14)
        $g.DrawString("Niko", $subFont, $titleBrush, 24, 38)

        $linePen.Dispose()
        $accent.Dispose()
        $muted.Dispose()
        $warn.Dispose()
        $titleBrush.Dispose()
        $font.Dispose()
        $subFont.Dispose()
    }
    finally {
        $g.Dispose()
    }

    $bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $bmp.Dispose()
}

$wizard = Join-Path $ProjectDir "Assets\installer-wizard.bmp"
$small = Join-Path $ProjectDir "Assets\installer-small.bmp"
New-Item -ItemType Directory -Path (Join-Path $ProjectDir "Assets") -Force | Out-Null

New-WizardBitmap -Width 164 -Height 314 -OutPath $wizard
New-WizardBitmap -Width 55 -Height 55 -OutPath $small

Write-Host "Installer artwork created:" -ForegroundColor Green
Write-Host " - $wizard"
Write-Host " - $small"
