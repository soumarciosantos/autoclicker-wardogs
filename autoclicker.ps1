# Autoclicker Wardogs - interface em janela
#   F8 = liga / pausa (trava a posição atual do mouse como alvo)
#   F9 = para a sessão
# Para gerar o version.json de uma nova versão:
#   powershell -File autoclicker.ps1 -ExportarVersao version.json -UrlScript <link raw do autoclicker.ps1>
param([string]$ExportarVersao, [string]$UrlScript)

$VERSAO = '2.0.0'
# Endereço padrão do version.json (pode ser trocado na aba Atualizações)
$URL_PADRAO = 'https://raw.githubusercontent.com/soumarciosantos/autoclicker-wardogs/main/version.json'

$NOVIDADES = @(
    @{ versao = '2.0.0'; data = '2026-10-25'; itens = @(
        'Interface em janela com painel, gráfico de cliques/s e log',
        'Aba de ajustes, salvos em config.json',
        'Atualização automática pela internet com conferência sha256',
        'Cliques em thread própria: mais rápidos e estáveis') },
    @{ versao = '1.2.0'; data = '2026-10-25'; itens = @('Status ao vivo no terminal') },
    @{ versao = '1.1.0'; data = '2026-10-24'; itens = @('Rajadas de cliques sem pausa', 'Para sozinho ao entrar na facção') },
    @{ versao = '1.0.0'; data = '2026-10-24'; itens = @('Primeira versão: F8 liga/pausa, F9 sai') }
)

$SCRIPT_PATH = $PSCommandPath

function Get-Sha256([byte[]]$bytes) {
    -join ([Security.Cryptography.SHA256]::Create().ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') })
}

if ($ExportarVersao) {
    if (-not $UrlScript) { Write-Host 'Informe -UrlScript com o link direto (raw) do autoclicker.ps1'; exit 1 }
    $hash = Get-Sha256 ([IO.File]::ReadAllBytes($SCRIPT_PATH))
    $obj = [ordered]@{
        versao    = $VERSAO
        script    = $UrlScript
        sha256    = $hash
        novidades = @($NOVIDADES | ForEach-Object { [ordered]@{ versao = $_.versao; data = $_.data; itens = @($_.itens) } })
    }
    [IO.File]::WriteAllText($ExportarVersao, ($obj | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding $false))
    Write-Host "Gerado $ExportarVersao (v$VERSAO, sha256 $hash)"
    exit 0
}

Add-Type -AssemblyName System.Windows.Forms, System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Threading;
public static class Native {
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("winmm.dll")] public static extern uint timeBeginPeriod(uint p);
    [DllImport("winmm.dll")] public static extern uint timeEndPeriod(uint p);
}
// Clica numa thread separada para a janela não travar
public class Clicker {
    public volatile int DelayMs = 5;
    int x, y;
    long total;
    volatile bool running;
    public long Total { get { return Interlocked.Read(ref total); } }
    public bool Running { get { return running; } }
    public void Reset() { Interlocked.Exchange(ref total, 0); }
    public void Stop() { running = false; }
    public void Start(int px, int py) {
        if (running) return;
        x = px; y = py; running = true;
        Thread t = new Thread(Loop);
        t.IsBackground = true;
        t.Start();
    }
    void Loop() {
        Native.timeBeginPeriod(1); // Sleep com precisão de 1 ms
        while (running) {
            Native.SetCursorPos(x, y);
            Native.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            Native.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
            Interlocked.Increment(ref total);
            Thread.Sleep(DelayMs);
        }
        Native.timeEndPeriod(1);
    }
}
"@
[void][Native]::SetProcessDPIAware()

# ---------- configuração ----------
$CONFIG_PATH = Join-Path $PSScriptRoot 'config.json'
$cfg = [ordered]@{
    intervaloMs = 5; autoParar = $true; limiar = 60; checagens = 6
    sempreNoTopo = $true; bipe = $true; urlAtualizacao = $URL_PADRAO; verificarAoAbrir = $true
}
if (Test-Path $CONFIG_PATH) {
    try {
        $j = Get-Content $CONFIG_PATH -Raw | ConvertFrom-Json
        foreach ($k in @($cfg.Keys)) { if ($null -ne $j.$k) { $cfg[$k] = $j.$k } }
    } catch {}
}
function Save-Config { [IO.File]::WriteAllText($CONFIG_PATH, ($cfg | ConvertTo-Json), (New-Object Text.UTF8Encoding $false)) }

# ---------- detecção de mudança de tela ----------
function Get-Amostra([int]$cx, [int]$cy) {
    $x0 = $cx - 240; $y0 = $cy - 150; $w = 480; $h = 260
    $bmp = New-Object Drawing.Bitmap $w, $h
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x0, $y0, 0, 0, $bmp.Size)
    $g.Dispose()
    $cores = foreach ($y in 0..9) { foreach ($x in 0..15) { $bmp.GetPixel([int]($x * ($w - 1) / 15), [int]($y * ($h - 1) / 9)) } }
    $bmp.Dispose()
    return , $cores
}

function Get-FracaoMudada($a, $b) {
    $mudou = 0
    for ($i = 0; $i -lt $a.Count; $i++) {
        $d = [math]::Abs($a[$i].R - $b[$i].R) + [math]::Abs($a[$i].G - $b[$i].G) + [math]::Abs($a[$i].B - $b[$i].B)
        if ($d -gt 45) { $mudou++ }
    }
    return $mudou / $a.Count
}

# ---------- estado ----------
$clicker = New-Object Clicker
$sessao = New-Object Diagnostics.Stopwatch
$S = @{
    alvo = $null; ref = $null; trocada = 0; frac = 0.0; cps = 0.0
    ultimoTotal = 0; relStatus = [Diagnostics.Stopwatch]::StartNew()
    historico = New-Object 'Collections.Generic.List[double]'
    prevF8 = $false; prevF9 = $false; contagem = 0; novaSessao = $true; remoto = $null
}

# ---------- visual ----------
$corFundo    = [Drawing.Color]::FromArgb(18, 18, 20)
$corPainel   = [Drawing.Color]::FromArgb(32, 32, 36)
$corTexto    = [Drawing.Color]::FromArgb(232, 232, 232)
$corSub      = [Drawing.Color]::FromArgb(150, 150, 156)
$corVerde    = [Drawing.Color]::FromArgb(56, 170, 96)
$corAmarelo  = [Drawing.Color]::FromArgb(235, 190, 60)
$corAzul     = [Drawing.Color]::FromArgb(52, 140, 210)
$corVermelho = [Drawing.Color]::FromArgb(200, 70, 62)

function New-Fonte($tam, [bool]$negrito = $false) {
    New-Object Drawing.Font('Segoe UI', $tam, $(if ($negrito) { [Drawing.FontStyle]::Bold } else { [Drawing.FontStyle]::Regular }))
}
function New-Label($texto, $x, $y, $w, $h, $tam = 9, $cor = $corTexto, [bool]$negrito = $false) {
    $l = New-Object Windows.Forms.Label
    $l.Text = $texto; $l.SetBounds($x, $y, $w, $h); $l.ForeColor = $cor
    $l.Font = New-Fonte $tam $negrito; $l.BackColor = [Drawing.Color]::Transparent
    $l
}
function New-Botao($texto, $x, $y, $w, $h, $cor) {
    $b = New-Object Windows.Forms.Button
    $b.Text = $texto; $b.SetBounds($x, $y, $w, $h); $b.FlatStyle = 'Flat'
    $b.FlatAppearance.BorderSize = 0; $b.BackColor = $cor; $b.ForeColor = [Drawing.Color]::White
    $b.Font = New-Fonte 10 $true; $b.Cursor = 'Hand'
    $b
}
function New-Card($pai, $titulo, $x, $y) {
    $c = New-Object Windows.Forms.Panel
    $c.SetBounds($x, $y, 108, 58); $c.BackColor = $corPainel
    $c.Controls.Add((New-Label $titulo 8 6 96 16 8 $corSub))
    $v = New-Label '0' 8 22 96 30 14 $corTexto $true
    $c.Controls.Add($v)
    $pai.Controls.Add($c)
    $v
}
function New-Numero($pai, $x, $y, $min, $max, $valor) {
    $n = New-Object Windows.Forms.NumericUpDown
    $n.SetBounds($x, $y, 90, 26); $n.Minimum = $min; $n.Maximum = $max; $n.Value = $valor
    $n.BackColor = $corPainel; $n.ForeColor = $corTexto; $n.BorderStyle = 'FixedSingle'; $n.Font = New-Fonte 10
    $pai.Controls.Add($n)
    $n
}
function New-Check($pai, $texto, $x, $y, $marcado) {
    $c = New-Object Windows.Forms.CheckBox
    $c.Text = $texto; $c.SetBounds($x, $y, 430, 26); $c.Checked = [bool]$marcado
    $c.ForeColor = $corTexto; $c.Font = New-Fonte 10
    $pai.Controls.Add($c)
    $c
}

$form = New-Object Windows.Forms.Form
$form.Text = "Autoclicker Wardogs v$VERSAO"
$form.ClientSize = New-Object Drawing.Size(480, 600)
$form.FormBorderStyle = 'FixedSingle'; $form.MaximizeBox = $false
$form.StartPosition = 'CenterScreen'; $form.BackColor = $corFundo; $form.ForeColor = $corTexto
$form.TopMost = [bool]$cfg.sempreNoTopo
$form.KeyPreview = $true

# abas feitas com botões (o TabControl padrão não aceita tema escuro)
$paginas = [ordered]@{}; $abas = @{}
$i = 0
foreach ($n in 'Painel', 'Ajustes', 'Atualizações') {
    $b = New-Botao $n (12 + $i * 152) 12 148 34 $corPainel
    $b.Tag = $n
    $b.Add_Click({ Show-Pagina $this.Tag })
    $form.Controls.Add($b); $abas[$n] = $b
    $p = New-Object Windows.Forms.Panel
    $p.SetBounds(12, 56, 456, 532); $p.BackColor = $corFundo; $p.Visible = $false
    $form.Controls.Add($p); $paginas[$n] = $p
    $i++
}
function Show-Pagina($nome) {
    foreach ($k in $paginas.Keys) {
        $paginas[$k].Visible = ($k -eq $nome)
        $abas[$k].BackColor = $(if ($k -eq $nome) { $corAzul } else { $corPainel })
    }
}

# ---------- página Painel ----------
$pp = $paginas['Painel']
$lblStatus = New-Label 'PARADO' 0 0 456 34 18 $corSub $true
$lblDica = New-Label 'Coloque o mouse no card do Wardogs e aperte F8.' 0 34 456 20 9 $corSub
$lblTotal = New-Label '0' 0 58 456 62 36 $corTexto $true
$pp.Controls.AddRange(@($lblStatus, $lblDica, $lblTotal, (New-Label 'cliques nesta sessão' 4 118 300 18 9 $corSub)))
$cardAgora = New-Card $pp 'AGORA /s' 0 144
$cardMedia = New-Card $pp 'MÉDIA /s' 116 144
$cardTempo = New-Card $pp 'TEMPO' 232 144
$cardTela  = New-Card $pp 'TELA MUDOU' 348 144
$lblAlvo = New-Label 'Alvo: nenhum' 0 208 456 20 9 $corSub
$pp.Controls.Add($lblAlvo)

$grafico = New-Object Windows.Forms.Panel
$grafico.SetBounds(0, 232, 456, 120)
[Windows.Forms.Panel].GetProperty('DoubleBuffered', [Reflection.BindingFlags]'NonPublic,Instance').SetValue($grafico, $true, $null)
$grafico.Add_Paint({
    param($origem, $ev)   # não usar $s: o PowerShell confundiria com $S
    $g = $ev.Graphics; $g.SmoothingMode = 'AntiAlias'
    $w = $origem.Width; $h = $origem.Height
    $g.Clear($corPainel)
    $dados = $S.historico
    $max = 10.0
    foreach ($v in $dados) { if ($v -gt $max) { $max = $v } }
    $penGrade = New-Object Drawing.Pen([Drawing.Color]::FromArgb(48, 48, 54))
    foreach ($f in 0.25, 0.5, 0.75) { $yy = [int](22 + ($h - 26) * $f); $g.DrawLine($penGrade, 0, $yy, $w, $yy) }
    $g.DrawString('cliques/s - últimos 60 s', (New-Fonte 8), (New-Object Drawing.SolidBrush $corSub), 6, 4)
    $g.DrawString(('máx {0:N0}' -f $max), (New-Fonte 8), (New-Object Drawing.SolidBrush $corSub), ($w - 70), 4)
    if ($dados.Count -ge 2) {
        $pts = New-Object 'Collections.Generic.List[Drawing.PointF]'
        for ($k = 0; $k -lt $dados.Count; $k++) {
            $x = $w - 1 - ($dados.Count - 1 - $k) * ($w - 2) / 59.0
            $y = $h - 4 - ($dados[$k] / $max) * ($h - 30)
            $pts.Add((New-Object Drawing.PointF([single]$x, [single]$y)))
        }
        $g.DrawLines((New-Object Drawing.Pen($corVerde, 2)), $pts.ToArray())
    }
})
$pp.Controls.Add($grafico)

$btnIniciar = New-Botao 'Iniciar (F8)' 0 362 222 42 $corVerde
$btnParar = New-Botao 'Parar (F9)' 234 362 222 42 $corVermelho
$pp.Controls.AddRange(@($btnIniciar, $btnParar))

$log = New-Object Windows.Forms.ListBox
$log.SetBounds(0, 414, 456, 118); $log.BackColor = $corPainel; $log.ForeColor = $corTexto
$log.BorderStyle = 'None'; $log.Font = New-Object Drawing.Font('Consolas', 9)
$pp.Controls.Add($log)

function Add-Log($msg) {
    $log.Items.Insert(0, ('[{0:HH:mm:ss}] {1}' -f (Get-Date), $msg))
    while ($log.Items.Count -gt 200) { $log.Items.RemoveAt(200) }
}
function Set-Status($texto, $cor, $dica) {
    $lblStatus.Text = $texto; $lblStatus.ForeColor = $cor
    if ($dica) { $lblDica.Text = $dica }
}

function Start-Cliques {
    $p = New-Object Native+POINT
    [void][Native]::GetCursorPos([ref]$p)
    if ($form.Bounds.Contains($p.X, $p.Y)) {
        Set-Status 'PARADO' $corSub 'O mouse está em cima desta janela. Leve até o card do time e aperte F8.'
        return
    }
    if ($S.novaSessao) {
        $clicker.Reset(); $sessao.Reset(); $S.historico.Clear()
        $S.ultimoTotal = 0; $S.novaSessao = $false
    }
    $S.alvo = $p; $S.ref = Get-Amostra $p.X $p.Y; $S.trocada = 0; $S.frac = 0.0
    $clicker.DelayMs = [int]$cfg.intervaloMs
    $clicker.Start($p.X, $p.Y); $sessao.Start()
    $lblAlvo.Text = "Alvo: ($($p.X), $($p.Y))  |  intervalo $($cfg.intervaloMs) ms"
    $btnIniciar.Text = 'Pausar (F8)'; $btnIniciar.BackColor = $corAmarelo
    Set-Status 'CLICANDO' $corVerde 'F8 pausa  |  F9 para'
    Add-Log "Clicando em ($($p.X), $($p.Y))"
}
function Suspend-Cliques {
    $clicker.Stop(); $sessao.Stop()
    $btnIniciar.Text = 'Continuar (F8)'; $btnIniciar.BackColor = $corVerde
    Set-Status 'PAUSADO' $corAmarelo 'F8 continua no mesmo ponto do mouse  |  F9 encerra a sessão'
    Add-Log ('Pausado - {0:N0} cliques' -f $clicker.Total)
}
function Stop-Cliques([bool]$entrou = $false) {
    if ($S.contagem -gt 0) { $tContagem.Stop(); $S.contagem = 0 }
    $estava = $clicker.Running -or $sessao.Elapsed.Ticks -gt 0
    $clicker.Stop(); $sessao.Stop(); $S.novaSessao = $true
    $btnIniciar.Text = 'Iniciar (F8)'; $btnIniciar.BackColor = $corVerde
    if ($entrou) {
        Set-Status 'ENTROU NO TIME!' $corAzul 'A tela de facção sumiu. Aperte F8 para uma nova sessão.'
        if ($cfg.bipe) { [Console]::Beep(1000, 250); [Console]::Beep(1400, 250) }
        $form.Activate()
    } else {
        Set-Status 'PARADO' $corSub 'Coloque o mouse no card do Wardogs e aperte F8.'
    }
    if ($estava) { Add-Log ('Sessão encerrada - {0:N0} cliques em {1:hh\:mm\:ss}' -f $clicker.Total, $sessao.Elapsed) }
}
function Switch-Cliques {
    if ($S.contagem -gt 0) { $tContagem.Stop(); $S.contagem = 0 }
    if ($clicker.Running) { Suspend-Cliques } else { Start-Cliques }
}

# botão Iniciar: contagem de 3 s para dar tempo de levar o mouse até o card
$tContagem = New-Object Windows.Forms.Timer
$tContagem.Interval = 1000
$tContagem.Add_Tick({
    $S.contagem--
    if ($S.contagem -le 0) { $tContagem.Stop(); Start-Cliques }
    else { Set-Status "INICIANDO EM $($S.contagem)..." $corAmarelo 'Leve o mouse até o card do time.' }
})
$btnIniciar.Add_Click({
    if ($clicker.Running) { Suspend-Cliques; return }
    $S.contagem = 3
    Set-Status 'INICIANDO EM 3...' $corAmarelo 'Leve o mouse até o card do time.'
    $tContagem.Start()
})
$btnParar.Add_Click({ Stop-Cliques })

# ---------- página Ajustes ----------
$pa = $paginas['Ajustes']
$pa.Controls.Add((New-Label 'Intervalo entre cliques (ms)' 0 4 300 22 10))
$numIntervalo = New-Numero $pa 340 2 0 1000 $cfg.intervaloMs
$pa.Controls.Add((New-Label '1 ms ≈ 1000/s, 5 ms ≈ 200/s, 20 ms ≈ 50/s. Se o jogo não registrar, aumente.' 0 30 456 34 8 $corSub))
$chkAuto = New-Check $pa 'Parar sozinho quando entrar no time' 0 76 $cfg.autoParar
$pa.Controls.Add((New-Label 'Quanto da tela precisa mudar (%)' 0 112 300 22 10))
$numLimiar = New-Numero $pa 340 110 20 95 $cfg.limiar
$pa.Controls.Add((New-Label 'Conferências seguidas antes de parar (1 por s)' 0 148 330 22 10))
$numChecagens = New-Numero $pa 340 146 1 30 $cfg.checagens
$chkTopo = New-Check $pa 'Janela sempre por cima do jogo' 0 190 $cfg.sempreNoTopo
$chkBipe = New-Check $pa 'Bipe ao entrar no time' 0 220 $cfg.bipe
$btnSalvar = New-Botao 'Salvar ajustes' 0 266 456 40 $corAzul
$lblSalvo = New-Label '' 0 312 456 20 9 $corVerde
$pa.Controls.AddRange(@($btnSalvar, $lblSalvo))
$btnSalvar.Add_Click({
    $cfg.intervaloMs = [int]$numIntervalo.Value
    $cfg.autoParar = $chkAuto.Checked
    $cfg.limiar = [int]$numLimiar.Value
    $cfg.checagens = [int]$numChecagens.Value
    $cfg.sempreNoTopo = $chkTopo.Checked
    $cfg.bipe = $chkBipe.Checked
    $clicker.DelayMs = $cfg.intervaloMs
    $form.TopMost = $cfg.sempreNoTopo
    try { Save-Config; $lblSalvo.ForeColor = $corVerde; $lblSalvo.Text = 'Ajustes salvos.' }
    catch { $lblSalvo.ForeColor = $corVermelho; $lblSalvo.Text = "Erro ao salvar: $($_.Exception.Message)" }
    Add-Log 'Ajustes salvos'
})

# ---------- página Atualizações ----------
$pu = $paginas['Atualizações']
$pu.Controls.Add((New-Label "Versão instalada: v$VERSAO" 0 0 456 26 12 $corTexto $true))
$pu.Controls.Add((New-Label 'Endereço do version.json' 0 34 456 20 9 $corSub))
$txtUrl = New-Object Windows.Forms.TextBox
$txtUrl.SetBounds(0, 56, 456, 26); $txtUrl.Text = $cfg.urlAtualizacao
$txtUrl.BackColor = $corPainel; $txtUrl.ForeColor = $corTexto; $txtUrl.BorderStyle = 'FixedSingle'; $txtUrl.Font = New-Fonte 9
$pu.Controls.Add($txtUrl)
$chkVerificar = New-Check $pu 'Verificar atualizações ao abrir' 0 88 $cfg.verificarAoAbrir
$btnVerificar = New-Botao 'Verificar agora' 0 122 222 40 $corAzul
$btnInstalar = New-Botao 'Instalar atualização' 234 122 222 40 $corVerde
$btnInstalar.Enabled = $false
$lblAtual = New-Label 'Ainda não verificado.' 0 170 456 36 9 $corSub
$pu.Controls.AddRange(@($btnVerificar, $btnInstalar, $lblAtual, (New-Label 'Novidades' 0 210 456 22 11 $corTexto $true)))
$rtb = New-Object Windows.Forms.RichTextBox
$rtb.SetBounds(0, 236, 456, 296); $rtb.ReadOnly = $true; $rtb.BorderStyle = 'None'
$rtb.BackColor = $corPainel; $rtb.ForeColor = $corTexto; $rtb.Font = New-Fonte 10
$pu.Controls.Add($rtb)

function Show-Novidades($lista) {
    $rtb.Clear()
    foreach ($n in $lista) {
        $rtb.SelectionFont = New-Fonte 10 $true
        $rtb.SelectionColor = $(if ([version]$n.versao -gt [version]$VERSAO) { $corVerde } else { $corTexto })
        $rtb.AppendText("v$($n.versao)   $($n.data)`n")
        $rtb.SelectionFont = New-Fonte 10
        $rtb.SelectionColor = $corSub
        foreach ($it in $n.itens) { $rtb.AppendText("   •  $it`n") }
        $rtb.AppendText("`n")
    }
}

function Save-Url {
    $cfg.urlAtualizacao = $txtUrl.Text.Trim()
    $cfg.verificarAoAbrir = $chkVerificar.Checked
    try { Save-Config } catch {}
}

function Test-Atualizacao([bool]$aoAbrir = $false) {
    $url = $txtUrl.Text.Trim()
    if (-not $url) {
        $lblAtual.ForeColor = $corSub
        $lblAtual.Text = 'Cole o link direto (raw) do version.json para ativar as atualizações.'
        return
    }
    Save-Url
    $lblAtual.ForeColor = $corSub; $lblAtual.Text = 'Verificando...'; $form.Refresh()
    try {
        $wc = New-Object Net.WebClient
        $wc.Encoding = [Text.Encoding]::UTF8
        $wc.Headers['Cache-Control'] = 'no-cache'
        # parâmetro extra só em http(s), para fugir do cache do GitHub
        if ($url -match '^https?://') {
            $sep = $(if ($url.Contains('?')) { '&' } else { '?' })
            $url = "$url$($sep)t=$([DateTime]::UtcNow.Ticks)"
        }
        $json = $wc.DownloadString($url).TrimStart([char]0xFEFF)
        $j = $json | ConvertFrom-Json
        $S.remoto = $j
        if ($j.novidades) { Show-Novidades $j.novidades }
        if ([version]$j.versao -gt [version]$VERSAO) {
            $lblAtual.ForeColor = $corVerde
            $lblAtual.Text = "Nova versão disponível: v$($j.versao). Clique em Instalar atualização."
            $btnInstalar.Enabled = $true
            $abas['Atualizações'].Text = 'Atualizações  ●'
            Add-Log "Atualização v$($j.versao) disponível - veja a aba Atualizações"
        } else {
            $lblAtual.ForeColor = $corSub
            $lblAtual.Text = "Você está na versão mais recente (v$VERSAO). Verificado às $((Get-Date).ToString('HH:mm'))."
            $btnInstalar.Enabled = $false
        }
    } catch {
        $lblAtual.ForeColor = $corVermelho
        $lblAtual.Text = "Não foi possível verificar: $($_.Exception.Message)"
        if ($aoAbrir) { Add-Log 'Falha ao verificar atualizações' }
    }
}

function Install-Atualizacao {
    $j = $S.remoto
    if (-not $j) { return }
    if (-not $j.sha256 -or -not $j.script) {
        $lblAtual.ForeColor = $corVermelho
        $lblAtual.Text = 'O version.json não tem "script" e "sha256". Atualização cancelada.'
        return
    }
    Stop-Cliques
    try {
        $lblAtual.ForeColor = $corSub; $lblAtual.Text = 'Baixando...'; $form.Refresh()
        $bytes = (New-Object Net.WebClient).DownloadData($j.script)
        if ((Get-Sha256 $bytes) -ne $j.sha256.ToLower()) { throw 'o arquivo baixado não confere com o sha256 do version.json' }
        Copy-Item $SCRIPT_PATH "$SCRIPT_PATH.bak" -Force
        [IO.File]::WriteAllBytes($SCRIPT_PATH, $bytes)
        [void][Windows.Forms.MessageBox]::Show("Atualizado para v$($j.versao). O programa vai reabrir.", 'Autoclicker Wardogs')
        Start-Process powershell -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', "`"$SCRIPT_PATH`""
        $form.Close()
    } catch {
        $lblAtual.ForeColor = $corVermelho
        $lblAtual.Text = "Atualização cancelada: $($_.Exception.Message)"
    }
}

$btnVerificar.Add_Click({ Test-Atualizacao })
$btnInstalar.Add_Click({ Install-Atualizacao })
$chkVerificar.Add_CheckedChanged({ Save-Url })
Show-Novidades $NOVIDADES

# ---------- timers ----------
# teclas F8/F9 (funcionam com o jogo em foco)
$tTeclas = New-Object Windows.Forms.Timer
$tTeclas.Interval = 40
$tTeclas.Add_Tick({
    $f8 = ([Native]::GetAsyncKeyState(0x77) -band 0x8000) -ne 0
    $f9 = ([Native]::GetAsyncKeyState(0x78) -band 0x8000) -ne 0
    if ($f8 -and -not $S.prevF8) { Switch-Cliques }
    if ($f9 -and -not $S.prevF9) { Stop-Cliques }
    $S.prevF8 = $f8; $S.prevF9 = $f9
})

# números do painel
$tStatus = New-Object Windows.Forms.Timer
$tStatus.Interval = 250
$tStatus.Add_Tick({
    $total = $clicker.Total
    $dt = [math]::Max($S.relStatus.Elapsed.TotalSeconds, 0.001)
    $S.cps = $(if ($clicker.Running) { ($total - $S.ultimoTotal) / $dt } else { 0 })
    $S.ultimoTotal = $total; $S.relStatus.Restart()
    $seg = [math]::Max($sessao.Elapsed.TotalSeconds, 0.001)
    $lblTotal.Text = '{0:N0}' -f $total
    $cardAgora.Text = '{0:N0}' -f $S.cps
    $cardMedia.Text = '{0:N0}' -f $(if ($total -gt 0) { $total / $seg } else { 0 })
    $cardTempo.Text = '{0:hh\:mm\:ss}' -f $sessao.Elapsed
    $cardTela.Text = '{0:P0}' -f $S.frac
})

# gráfico e detecção de "entrou no time", 1x por segundo
$tSegundo = New-Object Windows.Forms.Timer
$tSegundo.Interval = 1000
$tSegundo.Add_Tick({
    if (-not $clicker.Running) { return }
    $S.historico.Add($S.cps)
    while ($S.historico.Count -gt 60) { $S.historico.RemoveAt(0) }
    $grafico.Invalidate()
    if ($cfg.autoParar) {
        $S.frac = Get-FracaoMudada $S.ref (Get-Amostra $S.alvo.X $S.alvo.Y)
        if ($S.frac -ge $cfg.limiar / 100) { $S.trocada++ } else { $S.trocada = 0 }
        if ($S.trocada -ge $cfg.checagens) { Stop-Cliques $true }
    }
})

$form.Add_Shown({
    Show-Pagina 'Painel'
    $tTeclas.Start(); $tStatus.Start(); $tSegundo.Start()
    Add-Log "Autoclicker v$VERSAO aberto"
    if ($cfg.verificarAoAbrir -and $txtUrl.Text.Trim()) { Test-Atualizacao $true }
})
$form.Add_FormClosing({
    $clicker.Stop()
    $tTeclas.Stop(); $tStatus.Stop(); $tSegundo.Stop(); $tContagem.Stop()
})

[void]$form.ShowDialog()
