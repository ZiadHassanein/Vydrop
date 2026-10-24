$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskOutput=Join-Path $taskRoot 'assets/installer'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
foreach($taskTheme in @('light','dark')) {
    $taskBackground=if($taskTheme -eq 'dark'){[Drawing.Color]::FromArgb(28,33,41)}else{[Drawing.Color]::FromArgb(246,247,251)}
    $taskLogo=[Drawing.Image]::FromFile((Join-Path $taskRoot "assets/brand-logo-$taskTheme.png"))
    try {
        $taskBitmap=[Drawing.Bitmap]::new(328,628)
        $taskGraphics=[Drawing.Graphics]::FromImage($taskBitmap)
        try {
            $taskGraphics.Clear($taskBackground)
            $taskGraphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $taskHeight=[int](248*$taskLogo.Height/$taskLogo.Width)
            $taskGraphics.DrawImage($taskLogo,40,245,248,$taskHeight)
            $taskBitmap.Save((Join-Path $taskOutput "panel-$taskTheme.bmp"),[Drawing.Imaging.ImageFormat]::Bmp)
        } finally {$taskGraphics.Dispose();$taskBitmap.Dispose()}
    } finally {$taskLogo.Dispose()}
}
