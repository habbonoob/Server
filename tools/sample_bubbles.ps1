Add-Type -AssemblyName System.Drawing
foreach ($name in @('generic_bubble','normal_bubble')) {
  $path = "$PSScriptRoot\$name.png"
  $img = [System.Drawing.Bitmap]::FromFile($path)
  $w = $img.Width; $h = $img.Height
  $samples = @(
    $img.GetPixel([int]($w/2),[int]($h/2)),
    $img.GetPixel([int]($w/2),[int]($h*0.15)),
    $img.GetPixel([int]($w*0.1),[int]($h*0.5)),
    $img.GetPixel([int]($w/2),[int]($h*0.85))
  )
  $colors = ($samples | ForEach-Object { $_.ToArgb().ToString('X8') }) -join ' '
  Write-Output "$name ${w}x${h} $colors"
  $img.Dispose()
}
