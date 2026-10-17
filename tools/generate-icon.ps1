<#
  generate-icon.ps1 — สร้าง CapsLockKing.ico จากโค้ด (ไม่ต้องใช้ไฟล์ภาพภายนอก)

  ดีไซน์: พื้นหลังสี่เหลี่ยมมนสีเข้มไล่เฉด + มงกุฎทอง (CapsLock King)
  ขนาดที่ฝังในไฟล์: 16 / 32 / 48 / 64 / 128 / 256 px (PNG-compressed ICO)

  วิธีใช้:
    powershell -ExecutionPolicy Bypass -File tools\generate-icon.ps1
    powershell -ExecutionPolicy Bypass -File tools\generate-icon.ps1 -PreviewPath preview.png
#>
param(
  [string]$OutFile,
  [string]$PreviewPath
)

Add-Type -AssemblyName System.Drawing

$root = Split-Path $PSScriptRoot -Parent
if (-not $OutFile) { $OutFile = Join-Path $root 'CapsLockKing.ico' }

$sizes = 16, 32, 48, 64, 128, 256

function New-CrownBitmap {
  param([int]$S)

  $bmp = New-Object System.Drawing.Bitmap $S, $S, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

  try {
    # --- พื้นหลัง: สี่เหลี่ยมมน ไล่สีเข้มลงล่าง ---
    $r = [Math]::Max(2, [int]($S * 0.22))
    $d = $r * 2
    $bgPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bgPath.AddArc(0, 0, $d, $d, 180, 90)
    $bgPath.AddArc($S - $d, 0, $d, $d, 270, 90)
    $bgPath.AddArc($S - $d, $S - $d, $d, $d, 0, 90)
    $bgPath.AddArc(0, $S - $d, $d, $d, 90, 90)
    $bgPath.CloseFigure()

    $bgTop = [System.Drawing.Color]::FromArgb(255, 38, 36, 58)
    $bgBottom = [System.Drawing.Color]::FromArgb(255, 20, 19, 28)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (
      [System.Drawing.PointF]::new(0, 0), [System.Drawing.PointF]::new(0, $S), $bgTop, $bgBottom)
    $g.FillPath($bgBrush, $bgPath)

    # --- มงกุฎทอง: 3 ยอด 2 ท้อง ---
    $x = $S * 0.16; $y = $S * 0.20; $w = $S * 0.68; $h = $S * 0.56
    $norm = @(
      @(0.00, 0.88), @(0.00, 0.22), @(0.25, 0.50), @(0.50, 0.12),
      @(0.75, 0.50), @(1.00, 0.22), @(1.00, 0.88)
    )
    $pts = foreach ($n in $norm) {
      [System.Drawing.PointF]::new([float]($x + $n[0] * $w), [float]($y + $n[1] * $h))
    }
    $crown = New-Object System.Drawing.Drawing2D.GraphicsPath
    $crown.AddPolygon([System.Drawing.PointF[]]$pts)

    $goldTop = [System.Drawing.Color]::FromArgb(255, 255, 222, 89)
    $goldBottom = [System.Drawing.Color]::FromArgb(255, 214, 158, 0)
    $goldBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (
      [System.Drawing.PointF]::new(0, $y), [System.Drawing.PointF]::new(0, ($y + $h)), $goldTop, $goldBottom)
    $g.FillPath($goldBrush, $crown)

    # --- ลูกแก้วปลายยอดทั้งสาม ---
    $jewelBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 240, 180))
    $jewels = @(@(0.00, 0.22), @(0.50, 0.12), @(1.00, 0.22))
    $jr = [Math]::Max(0.75, $w * 0.075)
    foreach ($j in $jewels) {
      $cx = $x + $j[0] * $w; $cy = $y + $j[1] * $h
      $g.FillEllipse($jewelBrush, [float]($cx - $jr), [float]($cy - $jr), [float]($jr * 2), [float]($jr * 2))
    }

    # --- เส้นขอบมงกุฎ (เฉพาะขนาดใหญ่ เพื่อไม่ให้เล็กเกินไปเบลอ) ---
    if ($S -ge 48) {
      $penColor = [System.Drawing.Color]::FromArgb(255, 160, 110, 0)
      $penWidth = [float]([Math]::Max(1.0, $S * 0.02))
      $pen = New-Object System.Drawing.Pen -ArgumentList $penColor, $penWidth
      $g.DrawPath($pen, $crown)
      $pen.Dispose()
    }
  }
  finally {
    $g.Dispose()
  }

  return $bmp
}

# --- เรนเดอร์ทุกขนาดเป็น PNG bytes ---
$images = @()
foreach ($s in $sizes) {
  $bmp = New-CrownBitmap -S $s
  try {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += @{ Size = $s; Bytes = $ms.ToArray() }
    if ($PreviewPath -and $s -eq 256) {
      $bmp.Save($PreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $ms.Dispose()
  }
  finally {
    $bmp.Dispose()
  }
}

# --- ประกอบไฟล์ ICO (ICONDIR + ICONDIRENTRY[] + PNG payloads) ---
$fs = [System.IO.File]::Create($OutFile)
$bw = New-Object System.IO.BinaryWriter($fs)
try {
  $bw.Write([uint16]0)                       # reserved
  $bw.Write([uint16]1)                       # type: icon
  $bw.Write([uint16]$images.Count)           # image count

  $offset = 6 + (16 * $images.Count)
  foreach ($img in $images) {
    $dim = 0
    if ($img.Size -lt 256) { $dim = $img.Size }
    $bw.Write([byte]$dim)                    # width (0 = 256)
    $bw.Write([byte]$dim)                    # height (0 = 256)
    $bw.Write([byte]0)                       # palette count
    $bw.Write([byte]0)                       # reserved
    $bw.Write([uint16]1)                     # color planes
    $bw.Write([uint16]32)                    # bits per pixel
    $bw.Write([uint32]$img.Bytes.Length)     # payload size
    $bw.Write([uint32]$offset)               # payload offset
    $offset += $img.Bytes.Length
  }
  foreach ($img in $images) {
    $bw.Write($img.Bytes)
  }
}
finally {
  $bw.Dispose()
  $fs.Dispose()
}

Write-Host ("Wrote {0} ({1} sizes: {2})" -f $OutFile, $images.Count, ($sizes -join ', '))
