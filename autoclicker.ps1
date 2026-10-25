# Autoclicker Wardogs - ponte para a versão em .exe
# Quem ainda usa a versão em PowerShell recebe este arquivo como atualização.
# Ele baixa o AutoclickerWardogs.exe para a mesma pasta (conferindo o sha256) e abre o programa novo.
$VERSAO = '2.1.0'
$URL_INFO = 'https://raw.githubusercontent.com/soumarciosantos/autoclicker-wardogs/main/update.json'

Add-Type -AssemblyName System.Windows.Forms
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$exe = Join-Path $PSScriptRoot 'AutoclickerWardogs.exe'

if (-not (Test-Path $exe)) {
    try {
        $wc = New-Object Net.WebClient
        $wc.Encoding = [Text.Encoding]::UTF8
        $info = $wc.DownloadString("$($URL_INFO)?t=$([DateTime]::UtcNow.Ticks)").TrimStart([char]0xFEFF) | ConvertFrom-Json
        $bytes = $wc.DownloadData($info.exe)
        $hash = -join ([Security.Cryptography.SHA256]::Create().ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') })
        if ($hash -ne $info.sha256) { throw 'o arquivo baixado não confere com o sha256' }
        [IO.File]::WriteAllBytes($exe, $bytes)
    } catch {
        [void][Windows.Forms.MessageBox]::Show(
            "Não foi possível baixar a nova versão: $($_.Exception.Message)`n`nBaixe manualmente em https://github.com/soumarciosantos/autoclicker-wardogs/releases/latest",
            'Autoclicker Wardogs')
        exit 1
    }
}

Start-Process $exe
