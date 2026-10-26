# Gera src\AutoclickerWardogs\icone.ico a partir do desenho do mascote (Mascote.Desenhar).
# Usa o .exe já compilado; depois de rodar, compile de novo para o ícone entrar no programa.
# Uso: powershell -File ferramentas\gerar-icone.ps1 [-Exe dist\AutoclickerWardogs.exe]
param([string]$Exe = "$PSScriptRoot\..\dist\AutoclickerWardogs.exe")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$asm = [Reflection.Assembly]::LoadFrom((Resolve-Path $Exe))
$desenhar = $asm.GetType('AutoclickerWardogs.Mascote').GetMethod('Desenhar')
$parado = [Enum]::Parse($asm.GetType('AutoclickerWardogs.Humor'), 'Parado')

function Renderizar([int]$tam) {
    $bmp = New-Object Drawing.Bitmap $tam, $tam, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.Clear([Drawing.Color]::Transparent)
    $g.ScaleTransform($tam / 100.0, $tam / 100.0)
    [void]$desenhar.Invoke($null, @($g, $parado, [single]0))
    $g.Dispose()
    $bmp
}

# imagem no formato BMP de ícone (32 bits com alfa + máscara vazia)
function Dib($bmp) {
    $s = $bmp.Width; $ms = New-Object IO.MemoryStream; $w = New-Object IO.BinaryWriter $ms
    $w.Write([uint32]40); $w.Write([int32]$s); $w.Write([int32]($s * 2)); $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]0); $w.Write([uint32]($s * $s * 4)); $w.Write([int32]0); $w.Write([int32]0); $w.Write([uint32]0); $w.Write([uint32]0)
    for ($y = $s - 1; $y -ge 0; $y--) { for ($x = 0; $x -lt $s; $x++) { $c = $bmp.GetPixel($x, $y); $w.Write([byte]$c.B); $w.Write([byte]$c.G); $w.Write([byte]$c.R); $w.Write([byte]$c.A) } }
    $linha = [int]([math]::Ceiling($s / 32.0)) * 4
    for ($y = 0; $y -lt $s; $y++) { $w.Write((New-Object byte[] $linha)) }
    $w.Flush(); $ms.ToArray()
}

$tamanhos = 16, 24, 32, 48, 64, 128, 256
$imagens = foreach ($t in $tamanhos) {
    $b = Renderizar $t
    if ($t -le 48) { , (Dib $b) } else { $m = New-Object IO.MemoryStream; $b.Save($m, [Drawing.Imaging.ImageFormat]::Png); , $m.ToArray() }
    $b.Dispose()
}

$out = New-Object IO.MemoryStream; $w = New-Object IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$tamanhos.Count)
$offset = 6 + 16 * $tamanhos.Count
for ($i = 0; $i -lt $tamanhos.Count; $i++) {
    $t = $tamanhos[$i]; $d = $imagens[$i]; $lado = if ($t -eq 256) { 0 } else { $t }
    $w.Write([byte]$lado); $w.Write([byte]$lado); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$d.Length); $w.Write([uint32]$offset)
    $offset += $d.Length
}
foreach ($d in $imagens) { $w.Write([byte[]]$d) }
$w.Flush()
$destino = Join-Path $PSScriptRoot '..\src\AutoclickerWardogs\icone.ico'
[IO.File]::WriteAllBytes($destino, $out.ToArray())
Write-Host "Ícone gerado: $((Resolve-Path $destino).Path) ($($out.Length) bytes)"
