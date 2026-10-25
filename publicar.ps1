# Publica uma nova versão: compila, gera update.json, cria a release no GitHub e envia.
# Antes: aumente <Version> no .csproj e adicione as mudanças no topo de novidades.json.
# Uso: .\publicar.ps1
$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
$repo = 'soumarciosantos/autoclicker-wardogs'
$pastaProj = Join-Path $raiz 'src\AutoclickerWardogs'
$proj = Join-Path $pastaProj 'AutoclickerWardogs.csproj'

$versao = ([xml](Get-Content $proj -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$novidades = @(Get-Content (Join-Path $pastaProj 'novidades.json') -Raw -Encoding UTF8 | ConvertFrom-Json)
if ($novidades[0].versao -ne $versao) { throw "novidades.json começa com v$($novidades[0].versao), mas o .csproj está em v$versao" }
if (git -C $raiz tag -l "v$versao") { throw "A tag v$versao já existe. Aumente a versão no .csproj." }

Write-Host "Compilando v$versao..."
$dist = Join-Path $raiz 'dist'
dotnet build $proj -c Release -o $dist --nologo -v q
if ($LASTEXITCODE) { throw 'Falha na compilação' }
$exe = Join-Path $dist 'AutoclickerWardogs.exe'
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLower()

Write-Host 'Gerando o instalador...'
$iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup não encontrado. Instale com: winget install JRSoftware.InnoSetup' }
& $iscc /Q "/DAppVersion=$versao" (Join-Path $raiz 'instalador\AutoclickerWardogs.iss')
if ($LASTEXITCODE) { throw 'Falha ao gerar o instalador' }
$setup = Join-Path $dist 'AutoclickerWardogs-Setup.exe'

$info = [ordered]@{
    versao    = $versao
    exe       = "https://github.com/$repo/releases/download/v$versao/AutoclickerWardogs.exe"
    sha256    = $hash
    novidades = $novidades
}
[IO.File]::WriteAllText((Join-Path $raiz 'update.json'), ($info | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding $false))

git -C $raiz add -A
git -C $raiz commit -m "Versão $versao"
git -C $raiz tag "v$versao"
# a tag e a release vão primeiro: assim o .exe já existe quando o update.json novo chegar na main
git -C $raiz push origin "v$versao"
$notas = "**Instalar:** baixe o ``AutoclickerWardogs-Setup.exe``. Prefere sem instalar? Use o ``AutoclickerWardogs.exe``.`n`n" +
    (($novidades[0].itens | ForEach-Object { "- $_" }) -join "`n")
gh release create "v$versao" $setup $exe --repo $repo --title "v$versao" --notes $notas --verify-tag
if ($LASTEXITCODE) { throw 'Falha ao criar a release' }
git -C $raiz push origin main

Write-Host "Publicado: v$versao (sha256 $hash)"
