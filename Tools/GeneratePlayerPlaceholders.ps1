Add-Type -AssemblyName System.Drawing

$root = Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets\material\Player\Placeholder'
$actions = @('Idle','Walk','Run','Dash','Jump','Attack')
$dirs = @(
    @{ name = 'down'; r = 60; g = 120; b = 220 },
    @{ name = 'up'; r = 60; g = 200; b = 90 },
    @{ name = 'left'; r = 240; g = 140; b = 50 },
    @{ name = 'right'; r = 170; g = 80; b = 220 },
    @{ name = 'left_up'; r = 40; g = 210; b = 210 },
    @{ name = 'right_up'; r = 240; g = 220; b = 60 }
)

$frameW = 48
$frameH = 64
$frameCount = 6

foreach ($action in $actions) {
    $dirPath = Join-Path $root $action
    New-Item -ItemType Directory -Force -Path $dirPath | Out-Null

    foreach ($d in $dirs) {
        $bmp = New-Object System.Drawing.Bitmap ($frameW * $frameCount), $frameH
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))

        for ($i = 0; $i -lt $frameCount; $i++) {
            $shade = [Math]::Min(80, $i * 12)
            $c = [System.Drawing.Color]::FromArgb(
                255,
                [Math]::Min(255, $d.r + $shade),
                [Math]::Min(255, $d.g + $shade),
                [Math]::Min(255, $d.b + $shade))
            $brush = New-Object System.Drawing.SolidBrush $c
            $g.FillRectangle($brush, ($i * $frameW) + 4, 8, $frameW - 8, $frameH - 16)
            $brush.Dispose()
        }

        $g.Dispose()
        $prefix = $action.ToLower()
        $path = Join-Path $dirPath ("{0}_{1}.png" -f $prefix, $d.name)
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }
}

Write-Output "Done: $root"
