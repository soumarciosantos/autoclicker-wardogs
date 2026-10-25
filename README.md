# Autoclicker Wardogs

Autoclicker com janela para a tela **"Selecione a facção"**: clica sem parar no card do time e para sozinho quando a tela some.

## Como usar

1. Baixe `autoclicker.ps1` e `iniciar-autoclicker.bat` para a mesma pasta.
2. Dê dois cliques em `iniciar-autoclicker.bat`.
3. No jogo, deixe o mouse sobre o card do time e aperte **F8**.

| Tecla | Ação |
|---|---|
| F8 | Liga / pausa (usa a posição atual do mouse) |
| F9 | Para a sessão |

Dica: use o jogo em modo **janela sem borda**; em tela cheia exclusiva os cliques simulados podem ser ignorados.

## Atualizações

O programa confere `version.json` deste repositório ao abrir. Se houver versão nova, a aba **Atualizações** mostra as novidades e o botão **Instalar atualização** baixa o novo `autoclicker.ps1`, confere o sha256 e reabre.

## Publicar uma nova versão

1. Em `autoclicker.ps1`, aumente `$VERSAO` e adicione as mudanças no topo de `$NOVIDADES`.
2. Gere o `version.json`:

   ```
   powershell -File autoclicker.ps1 -ExportarVersao version.json -UrlScript https://raw.githubusercontent.com/soumarciosantos/autoclicker-wardogs/main/autoclicker.ps1
   ```

3. Faça commit de `autoclicker.ps1` e `version.json` juntos e envie para `main`.

Não edite `autoclicker.ps1` depois de gerar o `version.json` — o sha256 deixaria de bater e a atualização seria recusada.
