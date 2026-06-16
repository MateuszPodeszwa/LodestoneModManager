# Regenerates the app icon from the source logo.
# Trims the surrounding white canvas (keeping the tile + soft shadow), squares the crop,
# then writes a multi-resolution .ico (PNG-compressed entries, 16-256 px).
#
# Usage:  pwsh tools/make-icon.ps1
param(
    [string]$Source = "$PSScriptRoot/../src/Lodestone.App/Assets/lodestone-source.png",
    [string]$Out    = "$PSScriptRoot/../src/Lodestone.App/Assets/lodestone.ico"
)

Add-Type -AssemblyName System.Drawing

$orig = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Source))

# --- find the content bounding box on a fast 256px thumbnail ---
$tw = 256
$thumb = New-Object System.Drawing.Bitmap $tw, $tw
$g = [System.Drawing.Graphics]::FromImage($thumb)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($orig, 0, 0, $tw, $tw)
$g.Dispose()

$minX = $tw; $minY = $tw; $maxX = 0; $maxY = 0
for ($y = 0; $y -lt $tw; $y++) {
    for ($x = 0; $x -lt $tw; $x++) {
        $p = $thumb.GetPixel($x, $y)
        $isBackground = ($p.A -lt 8) -or ($p.R -gt 244 -and $p.G -gt 244 -and $p.B -gt 244)
        if (-not $isBackground) {
            if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y }
        }
    }
}
$thumb.Dispose()
Write-Host "content bbox on 256px thumb: ($minX,$minY)-($maxX,$maxY)"

# --- map the bbox back to full resolution, square + center it, add a small margin ---
$scale = $orig.Width / $tw
$x0 = $minX * $scale; $y0 = $minY * $scale
$x1 = ($maxX + 1) * $scale; $y1 = ($maxY + 1) * $scale
$side = [Math]::Max($x1 - $x0, $y1 - $y0)
$side = $side * 1.06                       # ~3% breathing room each side
$cx = ($x0 + $x1) / 2; $cy = ($y0 + $y1) / 2
$left = [int][Math]::Round($cx - $side / 2)
$top  = [int][Math]::Round($cy - $side / 2)
$side = [int][Math]::Round($side)
if ($left -lt 0) { $left = 0 }
if ($top -lt 0) { $top = 0 }
if ($left + $side -gt $orig.Width)  { $side = $orig.Width  - $left }
if ($top  + $side -gt $orig.Height) { $side = $orig.Height - $top }

$cropRect = New-Object System.Drawing.Rectangle $left, $top, $side, $side
$cropped = New-Object System.Drawing.Bitmap $side, $side
$gc = [System.Drawing.Graphics]::FromImage($cropped)
$gc.DrawImage($orig, (New-Object System.Drawing.Rectangle 0, 0, $side, $side), $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
$gc.Dispose()
Write-Host "cropped square: ${side}px at ($left,$top)"

# --- render each icon size as a PNG ---
$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = @()
foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $gg = [System.Drawing.Graphics]::FromImage($bmp)
    $gg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gg.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $gg.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $gg.DrawImage($cropped, 0, 0, $s, $s)
    $gg.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , ($ms.ToArray())
    $bmp.Dispose(); $ms.Dispose()
}

# --- pack into the ICO container ---
$fs = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)   # ICONDIR
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $data = $pngs[$i]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([Byte]$dim); $bw.Write([Byte]$dim); $bw.Write([Byte]0); $bw.Write([Byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$data.Length); $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
foreach ($data in $pngs) { $bw.Write($data) }
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path (Split-Path -Parent $Out) (Split-Path -Leaf $Out)), $fs.ToArray())
$bw.Dispose(); $fs.Dispose(); $orig.Dispose(); $cropped.Dispose()

Write-Host "wrote $Out ($((Get-Item $Out).Length) bytes)"
