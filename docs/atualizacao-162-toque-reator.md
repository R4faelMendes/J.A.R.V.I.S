# Jarvis 1.6.2 — janela, reator e controle por toque

## O que foi corrigido

- A janela sem moldura tinha `WindowChrome.CaptionHeight=0` e dependia de um evento sobre um cabeçalho sem fundo. Partes vazias não funcionavam como área de arraste e o tratamento manual também não restaurava a janela maximizada durante o arraste.
- Agora os 81 px superiores usam a área de título nativa do Windows. Arraste, duplo clique, restauração e encaixe ficam a cargo do sistema; busca e botões continuam interativos. Verificado por `WM_NCHITTEST`: título retorna `HTCAPTION`, busca retorna `HTCLIENT`.
- Jarvis deixou de ser excluído da lista de janelas. Pode ser selecionado explicitamente na grade pelo PC ou celular. Layouts automáticos por voz continuam selecionando os demais aplicativos. O tamanho mínimo da interface ainda pode impedir encaixe em células pequenas; a organização informa esse limite.

## Reator

- Desktop: três anéis com espessura, chanfros, placas segmentadas, parafusos, ranhuras, iluminação direcional, detalhes de titânio, base e núcleo com textura de plasma. Cada anel gira em torno do próprio eixo; globo e estrutura interna têm rotação independente.
- Android: projeção com profundidade e oclusão, faces metálicas, parafusos, placas e núcleo de plasma detalhado. A textura é calculada uma vez e reutilizada, sem gerar um bitmap por quadro.
- Paleta fixa. Animação do desktop respeita modo econômico, redução de animações e janela minimizada; mobile pausa ao sair da página ou colocar o app em segundo plano.
- Prévias verificadas em `artifacts/preview/Nucleo.png`, `ReatorMobile162.png` e `ReatorMobile162-giro.png`.

## Como controlar pelo celular

1. Atualizar para o APK 1.6.2 pelo link/QR em Dispositivos. Instalar sobre a versão anterior para manter o pareamento.
2. No PC → Dispositivos, ativar **Compartilhar minhas telas** e **Permitir controle por toque do celular pareado**.
3. No Android → Tela ao vivo, escolher o monitor, iniciar e tocar em **Ativar toque**. Também funciona em tela cheia.
4. Toque = clique; duplo toque = duplo clique; segurar = botão direito; deslizar com um dedo = arrastar; deslizar com dois dedos = rolar. Há botões de rolagem e Enter.

O arraste é enviado após soltar o dedo, como uma operação curta e completa. Assim, uma queda da conexão não deixa o mouse pressionado entre solicitações. Falhas durante a operação também soltam o botão no bloco de limpeza. Não é um gesto de arraste espelhado continuamente enquanto o dedo está encostado.

As coordenadas consideram a proporção real do quadro, faixas pretas, resolução da captura, origem negativa de outro monitor e área virtual do Windows. O toque nas faixas pretas é ignorado.

O controle começa desativado a cada inicialização do PC. Exige HTTPS, token e certificado do pareamento, permissão no PC e transmissão recente. Desmarcar o compartilhamento desativa o controle. Não há execução remota de texto livre: o protocolo permite apenas gestos definidos e uma pequena lista de teclas. Entrada bloqueada nas telas de bloqueio e de segurança; janelas elevadas podem ser recusadas pelo Windows.

## Validação e pacote

- Compilação Release desktop e Android concluída sem erros. Android mantém somente o aviso já existente de `ConnectivityManager.GetAllNetworks` obsoleto.
- 30 testes de conexão, autorização, coordenadas, gestos inválidos, agenda, grade, transmissão e interface aprovados em `toque-162-entrega.trx`.
- Teste de janelas reais/captura aprovado em processo separado (`toque-162-janelas-nativas.trx`). O teste de interface encerra o `Application` WPF global; por isso esse teste nativo não compartilha seu processo com ele. Na execução conjunta inicial houve falha do ambiente de teste após esse encerramento, sem defeito reproduzido na execução isolada.
- Verificação adicional da entrada nativa contra uma janela criada exclusivamente para o teste: clique, botão direito, rolagem e arraste recebidos. Resultado final: dois eventos de botão esquerdo (clique + arraste), um direito, uma rolagem e nove movimentos. Nenhum aplicativo do usuário foi usado como alvo. Log: `artifacts/entrada-nativa-162.log`.
- APK 1.6.2, código 11, quatro arquiteturas, bibliotecas Skia nativas e 22 recursos de ícone confirmados. Mesma assinatura da versão anterior.
- APK: `artifacts/entrega/Jarvis-1.6.2.apk`.
- SHA-256: `F7D41E8FA5DBE0F8D820D60915BF06CB7EBDCC10C5A30EFAD60EEF62CC4DBE41`.
- Desktop Release reiniciado e respondendo; `/ping` informa 1.6.2. Página de download retorna 200 e a transferência parcial 206, com nome `Jarvis-1.6.2.apk`.
- Conferência autenticada do servidor em execução: dois monitores disponíveis, Jarvis incluído na seleção de grade e controle por toque inicialmente desativado. Download completo pelo endereço VPN teve SHA-256 idêntico ao APK de entrega.
- Download no mesmo Wi-Fi: `http://192.168.0.64:5188/mobile`; pela VPN: `http://100.110.47.61:5188/mobile`.
- Tailscale do PC em Running, endereço 100.110.47.61. Na conferência desta entrega, nenhum Android aparecia online na VPN; download externo depende de o telefone se conectar a ela. O QR de Wi-Fi permanece disponível no PC para baixar pela rede local.

Sem telefone conectado por ADB: o reconhecimento dos gestos físicos no Android e a fluidez em Wi-Fi/dados móveis ainda precisam ser confirmados no aparelho. Os testes nativos do Windows e do protocolo não substituem essa etapa.

## Ideias para mostrar informações do celular no PC

Propostas, ainda não implementadas nesta atualização:

1. **Painel do celular:** ampliar bateria e conexão que já existem com estado de carregamento, armazenamento livre e alerta de bateria baixa.
2. **Notificações escolhidas pelo usuário:** mostrar no Jarvis apenas os aplicativos autorizados, ocultando conteúdo sensível e permitindo pausar o espelhamento. Android exige conceder acesso às notificações; conteúdos protegidos pelo sistema podem não ser enviados.
3. **Caixa de transferência de arquivos:** compartilhar uma foto, PDF ou link pelo menu Compartilhar do Android e recebê-lo no PC, com fila e progresso. Seleção explícita de arquivos, sem varrer o armazenamento do telefone.
4. **Encontrar celular:** botão no PC para pedir um toque sonoro ao app, respeitando as permissões e restrições de som do Android.
5. **Tela do celular no PC:** visualização autorizada por sessão com `MediaProjection`, indicador visível e botão de parar. Visualização é viável; controlar outros aplicativos do Android exigiria permissões adicionais e não está incluído nesta proposta.
6. **Agenda em dois sentidos:** exibir no desktop alterações criadas no celular e estado dos próximos lembretes; a agenda local/sincronização existente pode ser a base.

Prioridade sugerida: notificações filtradas + envio de arquivos; em seguida painel do aparelho e localizar celular.

## Referências técnicas

- https://learn.microsoft.com/en-us/dotnet/api/system.windows.shell.windowchrome
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
- https://developer.android.com/reference/android/service/notification/NotificationListenerService
- https://developer.android.com/training/sharing
- https://developer.android.com/media/grow/media-projection
