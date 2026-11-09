Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile('F:\MBC\GT-DREAMRIFT\Assets\UI\UI TUTORIAL\tutorial panel.png')
$minX = 9999; $maxX = 0; $minY = 9999; $maxY = 0
for ($y = 0; $y -lt $bmp.Height; $y += 2) {
    for ($x = 0; $x -lt $bmp.Width; $x += 2) {
        $p = $bmp.GetPixel($x, $y)
        if ($p.A -gt 20) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}
$bmp.Dispose()
"minX=$minX maxX=$maxX minY=$minY maxY=$maxY W=$($maxX - $minX) H=$($maxY - $minY)" | Out-File 'F:\MBC\GT-DREAMRIFT\Assets\Script\Tutorial\bounds.txt'

