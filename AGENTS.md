# Instruções para agentes — autoclicker-wardogs

Este repositório integra o workspace DNSLINK no Plane. Siga as instruções locais adicionais quando existirem.

## Plane: card obrigatório e atualização do Kanban

Estas regras valem para toda demanda, correção, melhoria, documentação, configuração ou alteração de código deste projeto, inclusive trabalho feito por agentes de IA.

1. Antes de iniciar uma mudança, localizar ou criar **um card no projeto correspondente do Plane**, no workspace DNSLINK: https://plane.dnslink.com.br/dnslink/. Não começar trabalho sem card. Se uma demanda já tiver card, atualizá-lo; não criar duplicata.
2. Registrar no card objetivo, escopo, critérios de aceite, responsável quando definido e links para issue, branch e pull request do GitHub. Não colocar segredos, tokens ou dados pessoais no card.
3. Manter **o mesmo card** atualizado ao longo do trabalho, com decisões, bloqueios, testes, resultado da revisão e evidências. Ao terminar uma etapa, mover o card no Kanban imediatamente.
4. Usar os estados do Plane conforme a situação real:
   - **Ideias**: demanda capturada, ainda sem compromisso de execução.
   - **Planejado**: escopo e prioridade definidos, aguardando início.
   - **Em desenvolvimento**: implementação iniciada na branch da mudança.
   - **Revisão de código**: pull request aberto ou revisão técnica em andamento.
   - **Pronto para homologação**: revisão aprovada, aguardando teste no ambiente isolado.
   - **Em homologação**: testes e validação em homologação em andamento.
   - **Aprovado para main**: homologação aprovada, aguardando promoção.
   - **Concluído**: mudança mesclada na main e critérios de aceite confirmados. Registrar link da entrega e resultado. Não presumir deploy em produção a partir do merge.
   - **Cancelado**: demanda encerrada sem entrega, com motivo registrado.
5. Se a implementação terminou mas revisão, homologação ou promoção ainda faltam, manter o card na etapa correspondente; **não** marcar Concluído antecipadamente.
6. Antes de encerrar qualquer trabalho, verificar que o card do Plane reflete a situação atual. Se não houver acesso ao Plane, informar a pendência e fornecer o conteúdo exato a registrar; não declarar o card atualizado sem confirmação.
7. Respeitar o fluxo de branches e aprovações específico deste repositório. A atualização do Plane não autoriza merge, deploy ou alteração em produção.

