$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resourceRoots = @(
    'IDDSCommunity.Agents.FileMaker\res',
    'IDDSCommunity.Agents.FtpServer\Resources',
    'IDDSCommunity.Agents.MySql\Resources',
    'IDDSCommunity.Agents.Smtp\Resources',
    'IDDSCommunity.Agents.SqlServer\Resources',
    'IDDSCommunity.Agents.TerminalServer\Resources',
    'IDDSCommunity.Agents.WebSecurity\Resources',
    'IDDSCommunity.IntrusionDetection.Admin\Resources',
    'IDDSCommunity.IntrusionDetection.Base\Resources',
    'IDDSCommunity.IntrusionDetection.Shared\Resources'
)

$navy = [System.Drawing.Color]::FromArgb(14, 42, 68)
$blue = [System.Drawing.Color]::FromArgb(31, 111, 139)
$cyan = [System.Drawing.Color]::FromArgb(58, 191, 192)
$green = [System.Drawing.Color]::FromArgb(45, 166, 113)
$amber = [System.Drawing.Color]::FromArgb(236, 167, 44)
$red = [System.Drawing.Color]::FromArgb(220, 74, 74)
$muted = [System.Drawing.Color]::FromArgb(130, 148, 162)

function New-IconPen([System.Drawing.Color]$color, [float]$width) {
    $pen = [System.Drawing.Pen]::new($color, $width)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    return $pen
}

function Get-Glyph([string]$name) {
    switch -Regex ($name.ToLowerInvariant()) {
        'hardlock|lock' { 'lock'; break }
        'unlock' { 'unlock'; break }
        'warning|loginattempt' { 'warning'; break }
        'mail' { 'mail'; break }
        'sql' { 'database'; break }
        'ftp|download' { 'download'; break }
        'rdp|monitor' { 'monitor'; break }
        'web|network|protection' { 'network'; break }
        'filemaker|layout' { 'grid'; break }
        'settings|configuration' { 'gear'; break }
        'filter' { 'filter'; break }
        'enable|start' { 'play'; break }
        'disable|stop' { 'stop'; break }
        'new|add' { 'add'; break }
        'delete|close|error' { 'delete'; break }
        'edit' { 'edit'; break }
        'save' { 'save'; break }
        'refresh' { 'refresh'; break }
        'search' { 'search'; break }
        'help' { 'help'; break }
        'info|systemmessage' { 'info'; break }
        'grip' { 'grip'; break }
        'minimize' { 'minimize'; break }
        'maximize' { 'maximize'; break }
        'scale' { 'scale'; break }
        default { 'shield' }
    }
}

function Get-GlyphColor([string]$name, [string]$glyph) {
    $lower = $name.ToLowerInvariant()
    if ($lower.Contains('white')) { return [System.Drawing.Color]::White }
    if ($lower.Contains('deactivated') -or $lower.Contains('disabled')) { return $muted }
    if ($glyph -in 'delete', 'stop', 'warning') { return $red }
    if ($glyph -eq 'play' -or $lower.Contains('enabled')) { return $green }
    if ($glyph -eq 'lock') { return $amber }
    if ($glyph -eq 'unlock') { return $cyan }
    return $blue
}

function Draw-Glyph([System.Drawing.Graphics]$graphics, [string]$glyph, [int]$size, [System.Drawing.Color]$color) {
    $stroke = [Math]::Max(1.2, $size * 0.09)
    $pen = New-IconPen $color $stroke
    $thin = New-IconPen $color ([Math]::Max(1, $size * 0.06))
    $brush = [System.Drawing.SolidBrush]::new($color)
    switch ($glyph) {
        'add' { $graphics.DrawLine($pen,$size*.5,$size*.2,$size*.5,$size*.8); $graphics.DrawLine($pen,$size*.2,$size*.5,$size*.8,$size*.5) }
        'delete' { $graphics.DrawLine($pen,$size*.25,$size*.25,$size*.75,$size*.75); $graphics.DrawLine($pen,$size*.75,$size*.25,$size*.25,$size*.75) }
        'edit' { $graphics.DrawLine($pen,$size*.22,$size*.76,$size*.72,$size*.26); $graphics.DrawLine($thin,$size*.2,$size*.8,$size*.42,$size*.75) }
        'play' { $graphics.FillPolygon($brush,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($size*.3,$size*.18),[System.Drawing.PointF]::new($size*.8,$size*.5),[System.Drawing.PointF]::new($size*.3,$size*.82))) }
        'stop' { $graphics.FillRectangle($brush,$size*.24,$size*.24,$size*.52,$size*.52) }
        'download' { $graphics.DrawLine($pen,$size*.5,$size*.15,$size*.5,$size*.65); $graphics.DrawLine($pen,$size*.3,$size*.47,$size*.5,$size*.67); $graphics.DrawLine($pen,$size*.7,$size*.47,$size*.5,$size*.67); $graphics.DrawLine($thin,$size*.2,$size*.82,$size*.8,$size*.82) }
        'refresh' { $graphics.DrawArc($pen,$size*.18,$size*.18,$size*.64,$size*.64,35,285); $graphics.FillPolygon($brush,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($size*.73,$size*.12),[System.Drawing.PointF]::new($size*.84,$size*.4),[System.Drawing.PointF]::new($size*.56,$size*.34))) }
        'search' { $graphics.DrawEllipse($pen,$size*.17,$size*.17,$size*.45,$size*.45); $graphics.DrawLine($pen,$size*.58,$size*.58,$size*.82,$size*.82) }
        'mail' { $graphics.DrawRectangle($thin,$size*.12,$size*.24,$size*.76,$size*.52); $graphics.DrawLine($thin,$size*.13,$size*.27,$size*.5,$size*.55); $graphics.DrawLine($thin,$size*.87,$size*.27,$size*.5,$size*.55) }
        'database' { $graphics.FillEllipse($brush,$size*.19,$size*.14,$size*.62,$size*.25); $graphics.FillRectangle($brush,$size*.19,$size*.26,$size*.62,$size*.46); $graphics.FillEllipse($brush,$size*.19,$size*.59,$size*.62,$size*.25) }
        'monitor' { $graphics.DrawRectangle($thin,$size*.12,$size*.17,$size*.76,$size*.52); $graphics.DrawLine($thin,$size*.5,$size*.69,$size*.5,$size*.81); $graphics.DrawLine($thin,$size*.3,$size*.82,$size*.7,$size*.82) }
        'network' { $graphics.FillEllipse($brush,$size*.43,$size*.1,$size*.14,$size*.14); $graphics.FillEllipse($brush,$size*.13,$size*.68,$size*.14,$size*.14); $graphics.FillEllipse($brush,$size*.73,$size*.68,$size*.14,$size*.14); $graphics.DrawLine($thin,$size*.49,$size*.24,$size*.22,$size*.69); $graphics.DrawLine($thin,$size*.51,$size*.24,$size*.78,$size*.69); $graphics.DrawLine($thin,$size*.27,$size*.75,$size*.73,$size*.75) }
        'grid' { $graphics.DrawRectangle($thin,$size*.16,$size*.16,$size*.68,$size*.68); $graphics.DrawLine($thin,$size*.39,$size*.16,$size*.39,$size*.84); $graphics.DrawLine($thin,$size*.61,$size*.16,$size*.61,$size*.84); $graphics.DrawLine($thin,$size*.16,$size*.39,$size*.84,$size*.39); $graphics.DrawLine($thin,$size*.16,$size*.61,$size*.84,$size*.61) }
        'gear' { $graphics.DrawEllipse($pen,$size*.25,$size*.25,$size*.5,$size*.5); $graphics.DrawEllipse($thin,$size*.42,$size*.42,$size*.16,$size*.16); foreach ($a in 0,45,90,135) { $r=$a*[Math]::PI/180; $x=[Math]::Cos($r)*$size*.28; $y=[Math]::Sin($r)*$size*.28; $graphics.DrawLine($pen,$size*.5-$x,$size*.5-$y,$size*.5+$x,$size*.5+$y) } }
        'filter' { $graphics.FillPolygon($brush,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($size*.16,$size*.2),[System.Drawing.PointF]::new($size*.84,$size*.2),[System.Drawing.PointF]::new($size*.6,$size*.5),[System.Drawing.PointF]::new($size*.6,$size*.76),[System.Drawing.PointF]::new($size*.4,$size*.84),[System.Drawing.PointF]::new($size*.4,$size*.5))) }
        'lock' { $graphics.DrawArc($pen,$size*.28,$size*.12,$size*.44,$size*.46,180,180); $graphics.FillRectangle($brush,$size*.19,$size*.42,$size*.62,$size*.43) }
        'unlock' { $graphics.DrawArc($pen,$size*.43,$size*.12,$size*.38,$size*.46,185,225); $graphics.FillRectangle($brush,$size*.19,$size*.42,$size*.62,$size*.43) }
        'warning' { $graphics.FillPolygon($brush,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($size*.5,$size*.1),[System.Drawing.PointF]::new($size*.9,$size*.84),[System.Drawing.PointF]::new($size*.1,$size*.84))) }
        'info' { $graphics.DrawEllipse($thin,$size*.16,$size*.16,$size*.68,$size*.68); $graphics.FillEllipse($brush,$size*.46,$size*.28,$size*.08,$size*.08); $graphics.FillRectangle($brush,$size*.46,$size*.43,$size*.08,$size*.3) }
        'help' { $graphics.DrawEllipse($thin,$size*.16,$size*.16,$size*.68,$size*.68); $graphics.DrawArc($pen,$size*.34,$size*.28,$size*.32,$size*.3,200,250); $graphics.FillEllipse($brush,$size*.46,$size*.7,$size*.08,$size*.08) }
        'save' { $graphics.DrawRectangle($thin,$size*.2,$size*.16,$size*.6,$size*.68); $graphics.FillRectangle($brush,$size*.32,$size*.18,$size*.3,$size*.22); $graphics.DrawRectangle($thin,$size*.33,$size*.57,$size*.34,$size*.25) }
        'grip' { for($x=3;$x -lt $size;$x+=4){for($y=3;$y -lt $size;$y+=4){$graphics.FillEllipse($brush,$x-1,$y-1,2,2)}} }
        'minimize' { $graphics.DrawLine($pen,$size*.22,$size*.68,$size*.78,$size*.68) }
        'maximize' { $graphics.DrawRectangle($thin,$size*.2,$size*.2,$size*.6,$size*.6) }
        'scale' { $graphics.DrawRectangle($thin,$size*.16,$size*.3,$size*.52,$size*.52); $graphics.DrawRectangle($thin,$size*.32,$size*.16,$size*.52,$size*.52) }
        default { $graphics.FillPolygon($brush,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($size*.5,$size*.08),[System.Drawing.PointF]::new($size*.84,$size*.2),[System.Drawing.PointF]::new($size*.77,$size*.67),[System.Drawing.PointF]::new($size*.5,$size*.92),[System.Drawing.PointF]::new($size*.23,$size*.67),[System.Drawing.PointF]::new($size*.16,$size*.2))) }
    }
    $brush.Dispose(); $thin.Dispose(); $pen.Dispose()
}

$images = foreach ($resourceRoot in $resourceRoots) {
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot $resourceRoot) -File |
        Where-Object { $_.Extension -in '.png', '.gif' }
}

foreach ($imageFile in $images) {
    $source = [System.Drawing.Image]::FromFile($imageFile.FullName)
    $width = $source.Width; $height = $source.Height
    $source.Dispose()
    $bitmap = [System.Drawing.Bitmap]::new($width,$height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $name = [System.IO.Path]::GetFileNameWithoutExtension($imageFile.Name)
    if ($name -like 'border-*') {
        $graphics.Clear([System.Drawing.Color]::FromArgb(226,234,239))
    } elseif ($name -like 'corner-*') {
        $cornerPen = New-IconPen $blue 2
        $graphics.DrawArc($cornerPen,1,1,[Math]::Max(1,$width-3),[Math]::Max(1,$height-3),180,90)
        $cornerPen.Dispose()
    } elseif ($name -eq 'loading2') {
        for($i=0;$i -lt 8;$i++){ $b=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(55+$i*25,$cyan)); $a=$i*45*[Math]::PI/180; $graphics.FillEllipse($b,[float]($width/2+[Math]::Cos($a)*$width*.31-2),[float]($height/2+[Math]::Sin($a)*$height*.31-2),4,4); $b.Dispose() }
    } else {
        $glyph = Get-Glyph $name
        Draw-Glyph $graphics $glyph ([Math]::Min($width,$height)) (Get-GlyphColor $name $glyph)
    }
    $graphics.Dispose()
    if ($imageFile.Extension -ieq '.gif') { $bitmap.Save($imageFile.FullName,[System.Drawing.Imaging.ImageFormat]::Gif) }
    else { $bitmap.Save($imageFile.FullName,[System.Drawing.Imaging.ImageFormat]::Png) }
    $bitmap.Dispose()
}

Write-Host "Generated $($images.Count) original IDDS Community resource images."
