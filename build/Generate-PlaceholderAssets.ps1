# Generates placeholder PNG visual assets for the MSIX package so CI can
# build without real artwork checked in. Replace with real icons before
# shipping.
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\GHelperXboxBar.Package\Images')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$OutputDir = (Resolve-Path -LiteralPath $OutputDir).Path
Write-Host "Writing placeholder assets to $OutputDir"

$assets = @(
    @{ Name = 'StoreLogo.png';          W =  50; H =  50 },
    @{ Name = 'Square44x44Logo.png';    W =  44; H =  44 },
    @{ Name = 'Square150x150Logo.png';  W = 150; H = 150 },
    @{ Name = 'SmallTile.png';          W =  71; H =  71 },
    @{ Name = 'Wide310x150Logo.png';    W = 310; H = 150 },
    @{ Name = 'LargeTile.png';          W = 310; H = 310 },
    @{ Name = 'SplashScreen.png';       W = 620; H = 300 }
)

$bg       = [System.Drawing.Color]::FromArgb(31, 31, 31)
$accent   = [System.Drawing.Color]::FromArgb(76, 180, 234)
$fontName = 'Segoe UI'

foreach ($a in $assets) {
    $bmp = New-Object System.Drawing.Bitmap($a.W, $a.H)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode    = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    $g.Clear($bg)

    $brush = New-Object System.Drawing.SolidBrush($accent)
    $size  = [Math]::Max(8, [int]([Math]::Min($a.W, $a.H) * 0.5))
    $font  = New-Object System.Drawing.Font($fontName, $size, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt   = New-Object System.Drawing.StringFormat
    $fmt.Alignment     = [System.Drawing.StringAlignment]::Center
    $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center

    $rect = New-Object System.Drawing.RectangleF(0, 0, $a.W, $a.H)
    $g.DrawString('G', $font, $brush, $rect, $fmt)

    $path = Join-Path $OutputDir $a.Name
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)

    $font.Dispose(); $brush.Dispose(); $g.Dispose(); $bmp.Dispose()
    Write-Host "  wrote $($a.Name) ($($a.W)x$($a.H))"
}
