#Requires -Version 7.0
[CmdletBinding()]
param()

Add-Type -AssemblyName System.Drawing.Common
$workspace = Split-Path -Parent $PSScriptRoot
$extensions = @('.png', '.gif')
$files = Get-ChildItem -Path $workspace -Recurse -File | Where-Object {
    $extensions -contains $_.Extension.ToLowerInvariant() -and
    $_.FullName -notmatch '\\(bin|obj|\.git)\\'
}

function Get-AssetKind([string]$name) {
    $key = $name.ToLowerInvariant()
    if ($key -match 'border|corner') { return 'border' }
    if ($key -match 'close|delete') { return 'cross' }
    if ($key -match 'add|new') { return 'plus' }
    if ($key -match 'edit') { return 'pencil' }
    if ($key -match 'save|download|ftp') { return 'down' }
    if ($key -match 'start|enable') { return 'play' }
    if ($key -match 'stop|disable') { return 'stop' }
    if ($key -match 'refresh') { return 'refresh' }
    if ($key -match 'search') { return 'search' }
    if ($key -match 'unlock') { return 'unlock' }
    if ($key -match 'lock|protection|security|warning|error') { return 'shield' }
    if ($key -match 'mail|smtp|imap|pop3') { return 'mail' }
    if ($key -match 'sql|filemaker') { return 'database' }
    if ($key -match 'rdp|web|dns|network') { return 'network' }
    if ($key -match 'help|info') { return 'info' }
    if ($key -match 'minimize') { return 'minus' }
    if ($key -match 'maximize|scale') { return 'square' }
    if ($key -match 'grip') { return 'grip' }
    return 'shield'
}

foreach ($file in $files) {
    $source = [System.Drawing.Image]::FromFile($file.FullName)
    $width = $source.Width
    $height = $source.Height
    $source.Dispose()
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $selected = $file.BaseName -match 'white|selected'
    $color = if ($selected) { [System.Drawing.Color]::White } else { [System.Drawing.Color]::FromArgb(255, 31, 132, 151) }
    $accent = [System.Drawing.Color]::FromArgb(255, 19, 184, 166)
    $penWidth = [Math]::Max(1.4, [Math]::Min($width, $height) / 8)
    $pen = [System.Drawing.Pen]::new($color, $penWidth)
    $pen.StartCap = $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $brush = [System.Drawing.SolidBrush]::new($color)
    $accentBrush = [System.Drawing.SolidBrush]::new($accent)
    $m = [Math]::Max(2, [Math]::Min($width, $height) * 0.18)
    $right = $width - $m
    $bottom = $height - $m
    switch (Get-AssetKind $file.BaseName) {
        'border' { $graphics.Clear([System.Drawing.Color]::FromArgb(255, 214, 222, 226)) }
        'cross' { $graphics.DrawLine($pen,$m,$m,$right,$bottom); $graphics.DrawLine($pen,$right,$m,$m,$bottom) }
        'plus' { $graphics.DrawLine($pen,$width/2,$m,$width/2,$bottom); $graphics.DrawLine($pen,$m,$height/2,$right,$height/2) }
        'minus' { $graphics.DrawLine($pen,$m,$height/2,$right,$height/2) }
        'square' { $graphics.DrawRectangle($pen,$m,$m,$right-$m,$bottom-$m) }
        'play' { $graphics.FillPolygon($brush,@([Drawing.PointF]::new($m,$m),[Drawing.PointF]::new($right,$height/2),[Drawing.PointF]::new($m,$bottom))) }
        'stop' { $graphics.FillRectangle($brush,$m,$m,$right-$m,$bottom-$m) }
        'down' { $graphics.DrawLine($pen,$width/2,$m,$width/2,$bottom); $graphics.DrawLine($pen,$width/2,$bottom,$m,$height*0.58); $graphics.DrawLine($pen,$width/2,$bottom,$right,$height*0.58) }
        'search' { $graphics.DrawEllipse($pen,$m,$m,$width*0.5,$height*0.5); $graphics.DrawLine($pen,$width*0.58,$height*0.58,$right,$bottom) }
        'mail' { $graphics.DrawRectangle($pen,$m,$height*0.28,$right-$m,$height*0.5); $graphics.DrawLine($pen,$m,$height*0.3,$width/2,$height*0.56); $graphics.DrawLine($pen,$right,$height*0.3,$width/2,$height*0.56) }
        'database' { $graphics.FillEllipse($brush,$m,$m,$right-$m,$height*0.28); $graphics.FillRectangle($brush,$m,$height*0.28,$right-$m,$height*0.48); $graphics.FillEllipse($brush,$m,$height*0.62,$right-$m,$height*0.22) }
        'network' { $graphics.FillEllipse($brush,$width*0.42,$m,$width*0.16,$height*0.16); $graphics.FillEllipse($brush,$m,$bottom-$height*0.16,$width*0.16,$height*0.16); $graphics.FillEllipse($brush,$right-$width*0.16,$bottom-$height*0.16,$width*0.16,$height*0.16); $graphics.DrawLine($pen,$width/2,$height*0.28,$width*0.25,$height*0.72); $graphics.DrawLine($pen,$width/2,$height*0.28,$width*0.75,$height*0.72) }
        'info' { $graphics.DrawEllipse($pen,$m,$m,$right-$m,$bottom-$m); $graphics.DrawLine($pen,$width/2,$height*0.43,$width/2,$bottom); $graphics.FillEllipse($accentBrush,$width*0.44,$height*0.25,$width*0.12,$height*0.12) }
        'grip' { for($x=$m;$x -le $right;$x+=4){for($y=$m;$y -le $bottom;$y+=4){$graphics.FillEllipse($brush,$x,$y,2,2)}} }
        default { $points=@([Drawing.PointF]::new($width/2,$m),[Drawing.PointF]::new($right,$height*0.3),[Drawing.PointF]::new($width*0.78,$bottom),[Drawing.PointF]::new($width/2,$height-$m/2),[Drawing.PointF]::new($width*0.22,$bottom),[Drawing.PointF]::new($m,$height*0.3)); $graphics.FillPolygon($brush,$points); $graphics.FillEllipse($accentBrush,$width*0.42,$height*0.38,$width*0.16,$height*0.16) }
    }
    $graphics.Dispose(); $pen.Dispose(); $brush.Dispose(); $accentBrush.Dispose()
    $format = if ($file.Extension -ieq '.gif') { [System.Drawing.Imaging.ImageFormat]::Gif } else { [System.Drawing.Imaging.ImageFormat]::Png }
    $temporary = $file.FullName + '.generated'
    $bitmap.Save($temporary, $format)
    $bitmap.Dispose()
    Move-Item -LiteralPath $temporary -Destination $file.FullName -Force
}

Write-Output "Generated $($files.Count) original project assets."
