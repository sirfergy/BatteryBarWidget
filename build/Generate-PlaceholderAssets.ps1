# Generates the MSIX package's visual assets at build time: a stylized
# battery silhouette with a lightning-bolt charge overlay, rendered via
# System.Drawing so there are no binary blobs in the repo.
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\BatteryBarWidget.Package\Images')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$OutputDir = (Resolve-Path -LiteralPath $OutputDir).Path
Write-Host "Writing visual assets to $OutputDir"

$assets = @(
    @{ Name = 'StoreLogo.png';          W =  50; H =  50;  Wide = $false },
    @{ Name = 'Square44x44Logo.png';    W =  44; H =  44;  Wide = $false },
    @{ Name = 'Square150x150Logo.png';  W = 150; H = 150;  Wide = $false },
    @{ Name = 'SmallTile.png';          W =  71; H =  71;  Wide = $false },
    @{ Name = 'Wide310x150Logo.png';    W = 310; H = 150;  Wide = $true  },
    @{ Name = 'LargeTile.png';          W = 310; H = 310;  Wide = $false },
    @{ Name = 'SplashScreen.png';       W = 620; H = 300;  Wide = $true  }
)

# Palette: deep slate background, electric green charge, amber bolt.
$bgTop     = [System.Drawing.Color]::FromArgb(15, 23, 42)    # slate-900
$bgBot     = [System.Drawing.Color]::FromArgb(31, 31, 31)    # matches manifest
$shellCol  = [System.Drawing.Color]::FromArgb(226, 232, 240) # battery outline
$fillCol   = [System.Drawing.Color]::FromArgb(74, 222, 128)  # green charge
$fillGlow  = [System.Drawing.Color]::FromArgb(34, 197, 94)
$boltCol   = [System.Drawing.Color]::FromArgb(251, 191, 36)  # amber
$boltEdge  = [System.Drawing.Color]::FromArgb(253, 224, 71)
$textCol   = [System.Drawing.Color]::FromArgb(241, 245, 249)

function New-RoundedRectPath {
    param([float]$X,[float]$Y,[float]$W,[float]$H,[float]$R)
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $R * 2
    $p.AddArc($X,          $Y,          $d, $d, 180, 90) | Out-Null
    $p.AddArc($X + $W - $d,$Y,          $d, $d, 270, 90) | Out-Null
    $p.AddArc($X + $W - $d,$Y + $H - $d,$d, $d,   0, 90) | Out-Null
    $p.AddArc($X,          $Y + $H - $d,$d, $d,  90, 90) | Out-Null
    $p.CloseFigure()
    return $p
}

function Draw-BatteryIcon {
    param(
        [System.Drawing.Graphics]$g,
        [float]$CX,[float]$CY,  # center of the icon
        [float]$Size            # target bounding size (square)
    )

    # Battery body dimensions (horizontal orientation).
    $bodyW = $Size * 0.78
    $bodyH = $Size * 0.50
    $capW  = $Size * 0.07
    $capH  = $bodyH * 0.45
    $total = $bodyW + $capW
    $bx    = $CX - $total / 2.0
    $by    = $CY - $bodyH / 2.0
    $strokeW = [Math]::Max(1.5, $Size * 0.045)
    $radius  = [Math]::Max(2.0, $Size * 0.08)

    # Shell outline.
    $shellPath = New-RoundedRectPath -X $bx -Y $by -W $bodyW -H $bodyH -R $radius
    $pen = New-Object System.Drawing.Pen($shellCol, $strokeW)
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($pen, $shellPath)

    # Cap (rounded on right edge).
    $capPath = New-RoundedRectPath -X ($bx + $bodyW) -Y ($CY - $capH / 2.0) -W $capW -H $capH -R ([Math]::Max(1.0, $Size * 0.025))
    $capBrush = New-Object System.Drawing.SolidBrush($shellCol)
    $g.FillPath($capBrush, $capPath)

    # Charge fill (inset, ~75%) with vertical gradient.
    $inset = $strokeW + $Size * 0.025
    $fx = $bx + $inset
    $fy = $by + $inset
    $fw = ($bodyW - $inset * 2) * 0.75
    $fh = $bodyH - $inset * 2
    if ($fw -gt 1 -and $fh -gt 1) {
        $fillRadius = [Math]::Max(1.0, $radius - $inset * 0.6)
        $fillPath = New-RoundedRectPath -X $fx -Y $fy -W $fw -H $fh -R $fillRadius
        $fillRect = New-Object System.Drawing.RectangleF($fx, $fy, $fw, $fh)
        $fillBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            $fillRect, $fillCol, $fillGlow,
            [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
        $g.FillPath($fillBrush, $fillPath)
        $fillBrush.Dispose(); $fillPath.Dispose()
    }

    # Lightning bolt overlay centered on the battery body.
    $boltH = $bodyH * 1.20
    $boltW = $boltH * 0.55
    $bxC = $CX - $total / 2.0 + $bodyW / 2.0
    $byC = $CY
    # Normalized bolt silhouette (points in [0,1] x [0,1], origin top-left).
    $norm = @(
        @(0.55, 0.00),
        @(0.10, 0.55),
        @(0.42, 0.55),
        @(0.25, 1.00),
        @(0.90, 0.42),
        @(0.55, 0.42),
        @(0.78, 0.00)
    )
    $pts = New-Object System.Drawing.PointF[] $norm.Length
    for ($i = 0; $i -lt $norm.Length; $i++) {
        $px = $bxC - $boltW / 2.0 + $norm[$i][0] * $boltW
        $py = $byC - $boltH / 2.0 + $norm[$i][1] * $boltH
        $pts[$i] = New-Object System.Drawing.PointF($px, $py)
    }
    $boltPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $boltPath.AddPolygon($pts)
    $boltPath.CloseFigure()

    $boltBrush = New-Object System.Drawing.SolidBrush($boltCol)
    $g.FillPath($boltBrush, $boltPath)
    $boltPen = New-Object System.Drawing.Pen($boltEdge, [Math]::Max(1.0, $Size * 0.015))
    $boltPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($boltPen, $boltPath)

    $boltBrush.Dispose(); $boltPen.Dispose(); $boltPath.Dispose()
    $pen.Dispose(); $capBrush.Dispose(); $shellPath.Dispose(); $capPath.Dispose()
}

foreach ($a in $assets) {
    $bmp = New-Object System.Drawing.Bitmap($a.W, $a.H)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    # Vertical gradient background.
    $bgRect  = New-Object System.Drawing.RectangleF(0, 0, $a.W, $a.H)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $bgRect, $bgTop, $bgBot,
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
    $g.FillRectangle($bgBrush, $bgRect)
    $bgBrush.Dispose()

    if ($a.Wide) {
        # Icon on the left third, "Battery Bar" wordmark on the right.
        $iconSize = [Math]::Min($a.H * 0.80, $a.W * 0.30)
        $iconCX   = $a.W * 0.20
        $iconCY   = $a.H * 0.50
        Draw-BatteryIcon -g $g -CX $iconCX -CY $iconCY -Size $iconSize

        $fontSize = [int]($a.H * 0.28)
        $font = New-Object System.Drawing.Font('Segoe UI Semibold', $fontSize,
            [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $textBrush = New-Object System.Drawing.SolidBrush($textCol)
        $fmt = New-Object System.Drawing.StringFormat
        $fmt.Alignment     = [System.Drawing.StringAlignment]::Near
        $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
        $textRect = New-Object System.Drawing.RectangleF(
            ($a.W * 0.38), 0, ($a.W * 0.60), $a.H)
        $g.DrawString('Battery Bar', $font, $textBrush, $textRect, $fmt)
        $font.Dispose(); $textBrush.Dispose()
    } else {
        $iconSize = [Math]::Min($a.W, $a.H) * 0.88
        Draw-BatteryIcon -g $g -CX ($a.W / 2.0) -CY ($a.H / 2.0) -Size $iconSize
    }

    $path = Join-Path $OutputDir $a.Name
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)

    $g.Dispose(); $bmp.Dispose()
    Write-Host "  wrote $($a.Name) ($($a.W)x$($a.H))"
}
