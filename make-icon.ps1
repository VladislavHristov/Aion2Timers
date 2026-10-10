# Generates src/Aion2RiftOverlay/app.ico (cyan ring on dark disc).
# Requires only Windows PowerShell 5.1 (System.Drawing is built in).
Add-Type -AssemblyName System.Drawing

$size = 64
$bmp = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 12, 20, 30))
$g.FillEllipse($dark, 2, 2, 60, 60)

$accent = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 79, 225, 255)), 6
$g.DrawEllipse($accent, 8, 8, 48, 48)
$g.Dispose()

$hIcon = $bmp.GetHIcon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$outPath = Join-Path $PSScriptRoot 'src\Aion2RiftOverlay\app.ico'
$stream = [System.IO.File]::Create($outPath)
$icon.Save($stream)
$stream.Dispose()
$icon.Dispose()
$bmp.Dispose()
Write-Output "Icon written to $outPath"
