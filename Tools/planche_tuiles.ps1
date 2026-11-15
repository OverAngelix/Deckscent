# Dessine la planche des tuiles (docs/images/tuiles.png) d'apres l'export texte de
# Deckscent > Planche des tuiles (TilePlate.cs), qui appelle ce script. Ne pas lancer seul.
param([string]$In, [string]$Out)
Add-Type -AssemblyName System.Drawing
$txt = [IO.File]::ReadAllText($In) -replace "`r", ""
$tiles = @()
$cur = $null
foreach ($line in $txt -split "`n") {
    if ($line.StartsWith('@')) {
        $p = $line.Substring(1).Split('|')
        $cur = [pscustomobject]@{ Id = $p[0]; Name = $p[1]; Kind = $p[2]; Slots = [int]$p[3]; Rows = @(); Walls = @() }
        $tiles += $cur
    } elseif ($line.StartsWith('W')) {
        foreach ($w in ($line.Substring(1).Trim() -split ' ')) { if ($w) { $cur.Walls += , ($w.Split(',') | ForEach-Object { [int]$_ }) } }
    } elseif ($cur -and $line.Length -gt 0) { $cur.Rows += $line }
}

$cell = 20; $pad = 28; $titleH = 34; $maxW = 1500
$ink = [Drawing.Color]::FromArgb(20, 20, 22); $paper = [Drawing.Color]::FromArgb(246, 245, 240); $rock = [Drawing.Color]::FromArgb(70, 70, 74)
$fontT = New-Object Drawing.Font('Georgia', 13, [Drawing.FontStyle]::Bold)
$fontS = New-Object Drawing.Font('Georgia', 10, [Drawing.FontStyle]::Italic)
$fontH = New-Object Drawing.Font('Georgia', 22, [Drawing.FontStyle]::Bold)

# Mise en page : une section par sorte, tuiles en ligne avec retour à la ligne.
$layout = @(); $y = 20
foreach ($kind in 'couloir', 'salle') {
    $count = @($tiles | Where-Object Kind -eq $kind).Count
    $label = if ($kind -eq 'couloir') { "Couloirs ($count)" } else { "Salles ($count)" }
    $layout += [pscustomobject]@{ Header = $label; X = 20; Y = $y }
    $y += 44; $x = 20; $rowH = 0
    foreach ($t in $tiles | Where-Object Kind -eq $kind) {
        $w = $t.Rows[0].Length * $cell; $h = $t.Rows.Count * $cell
        $bw = [Math]::Max($w, 150) + $pad
        if ($x + $bw -gt $maxW) { $x = 20; $y += $rowH + $titleH + $pad; $rowH = 0 }
        $layout += [pscustomobject]@{ Tile = $t; X = $x; Y = $y }
        $x += $bw; $rowH = [Math]::Max($rowH, $h)
    }
    $y += $rowH + $titleH + $pad + 10
}
$bmp = New-Object Drawing.Bitmap $maxW, ($y + 10)
$g = [Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([Drawing.Color]::FromArgb(30, 30, 33))
$inkB = New-Object Drawing.SolidBrush $ink; $paperB = New-Object Drawing.SolidBrush $paper; $rockB = New-Object Drawing.SolidBrush $rock
$white = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(235, 233, 225))
$wallPen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(200, 40, 30)), 4
$gridPen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(215, 213, 205)), 1
foreach ($e in $layout) {
    if ($e.Header) { $g.DrawString($e.Header, $fontH, $white, $e.X, $e.Y); continue }
    $t = $e.Tile; $ox = $e.X; $oy = $e.Y + $titleH
    $g.DrawString($t.Name, $fontT, $white, $ox, $e.Y + 2)
    for ($r = 0; $r -lt $t.Rows.Count; $r++) {
        for ($c = 0; $c -lt $t.Rows[$r].Length; $c++) {
            $ch = $t.Rows[$r][$c]
            if ($ch -eq '_') { continue }
            $rect = New-Object Drawing.Rectangle ($ox + $c * $cell), ($oy + $r * $cell), $cell, $cell
            if ($ch -eq '#') { $g.FillRectangle($rockB, $rect) } else {
                $g.FillRectangle($paperB, $rect); $g.DrawRectangle($gridPen, $rect)
                if ($ch -eq 'T') { $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(230, 160, 40))), $rect.X + 5, $rect.Y + 5, 10, 10) }
            }
        }
    }
    # Murs fins (pourtour des salles) en rouge, troués aux portes.
    foreach ($w in $t.Walls) {
        $x0 = $ox + $w[0] * $cell; $y0 = $oy + $w[1] * $cell
        switch ($w[2]) {
            0 { $g.DrawLine($wallPen, $x0, $y0, $x0 + $cell, $y0) }
            1 { $g.DrawLine($wallPen, $x0 + $cell, $y0, $x0 + $cell, $y0 + $cell) }
            2 { $g.DrawLine($wallPen, $x0, $y0 + $cell, $x0 + $cell, $y0 + $cell) }
            3 { $g.DrawLine($wallPen, $x0, $y0, $x0, $y0 + $cell) }
        }
    }
    $sub = if ($t.Kind -eq 'salle') { "$($t.Slots) emplacements" } else { '1 emplacement' }
    $g.DrawString($sub, $fontS, $white, $ox, $oy + $t.Rows.Count * $cell + 4)
}
$bmp.Save($Out, [Drawing.Imaging.ImageFormat]::Png)
"ok $($tiles.Count) tuiles"
