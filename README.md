# Autoclicker Wardogs

Autoclicker com janela para a tela **"Selecione a facção"**: clica sem parar no card do time e para sozinho quando a tela some.

![Autoclicker Wardogs: atacando, pausado, ajustes e atualizações](telas/v3.4.0-visual-de-guerra.png)

## Como usar

1. Baixe o **`AutoclickerWardogs-Setup.exe`** da [última versão](https://github.com/soumarciosantos/autoclicker-wardogs/releases/latest) e instale. Não pede administrador; cria atalhos no Menu Iniciar e (opcional) na área de trabalho.
   - Prefere sem instalar? Baixe o `AutoclickerWardogs.exe` da mesma página e abra direto.
   - Para remover: **Configurações → Aplicativos → Aplicativos instalados → Autoclicker Wardogs → Desinstalar**.
2. No jogo, deixe o mouse sobre o card do time e aperte **F8**.

Requer Windows 10 ou 11 (usa o .NET Framework 4.8, que já vem no sistema).

| Tecla | Ação |
|---|---|
| F8 | Liga / pausa (usa a posição atual do mouse) |
| F9 | Para a sessão |

Dicas:
- Use o jogo em modo **janela sem borda**; em tela cheia exclusiva os cliques simulados podem ser ignorados.
- Na primeira vez, o Windows pode mostrar "O Windows protegeu o computador" porque o programa não é assinado. Clique em **Mais informações → Executar assim mesmo**.

## Atualizações

Ao abrir, o programa confere o `update.json` deste repositório. Se houver versão nova, a aba **Atualizações** mostra as novidades e o botão **Instalar atualização** baixa o novo `.exe`, confere o sha256, troca o arquivo e reabre.

Quem ainda usa a versão antiga em PowerShell (2.0.0) recebe automaticamente uma atualização que baixa o `.exe`.

## Desenvolvimento

Código em `src/AutoclickerWardogs` (C#, WinForms, .NET Framework 4.8). Para compilar:

```
dotnet build src/AutoclickerWardogs -c Release -o dist
```

### Publicar uma nova versão

1. Aumente `<Version>` em `src/AutoclickerWardogs/AutoclickerWardogs.csproj`.
2. Adicione as mudanças no topo de `src/AutoclickerWardogs/novidades.json`.
3. Rode `.\publicar.ps1`. Ele compila, gera o instalador (`instalador/AutoclickerWardogs.nsi`), gera o `update.json`, cria a release com o instalador e o `.exe` no GitHub e envia tudo.

Para gerar o instalador é preciso o NSIS 3: `winget install NSIS.NSIS`.

### Ícone

O ícone é gerado a partir do desenho do mascote (`Mascote.Desenhar`). Se mudar o mascote, compile, rode `powershell -File ferramentas/gerar-icone.ps1` e compile de novo.
