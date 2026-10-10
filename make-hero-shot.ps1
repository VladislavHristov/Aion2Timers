# Composes the three overlay screenshots into one stacked hero image.
Add-Type -AssemblyName System.Drawing

$dir = 'I:\Aion2RiftOverlay\docs\screenshots'
$daily = [System.Drawing.Image]::FromFile((Join-Path $dir 'daily-reset.png'))
$weekly = [System.Drawing.Image]::FromFile((Join-Path $dir 'weekly-reset.png'))
$rift = [System.Drawing.Image]::FromFile((Join-Path $dir 'spacerift.png'))

$gap = 8
$width = [Math]::Max($rift.Width, $daily.Width + $weekly.Width + $gap)
# Layout: daily top-left, weekly directly below, space rift below them (like in game).
$height = $daily.Height + $gap + $weekly.Height + $gap + $rift.Height

$canvas = New-Object System.Drawing.Bitmap $width, $height
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.Clear([System.Drawing.Color]::Transparent)
$g.DrawImage($daily, 0, 0, $daily.Width, $daily.Height)
$g.DrawImage($weekly, 0, $daily.Height + $gap, $weekly.Width, $weekly.Height)
$g.DrawImage($rift, 0, $daily.Height + $gap + $weekly.Height + $gap, $rift.Width, $rift.Height)
$g.Dispose()

$out = Join-Path $dir 'overlays.png'
$canvas.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
$daily.Dispose(); $weekly.Dispose(); $rift.Dispose()
Write-Output "saved $out ($width x $height)"