# Atualização 1.5.0 — reator, RAM, aplicativos e tela ao vivo

## Implementado

- Desktop: geometria 3D nativa no núcleo. Três anéis giram independentemente e em sentidos alternados ao redor do globo. O núcleo, sua grade e estrutura interna também giram. Atualização de animação até 60 Hz; respeita redução de movimento, modo econômico e minimização.
- Android: reator desenhado no Skia, com projeção 3D, ocultação das partes traseiras, peças orbitais e meridianos giratórios; timer de 60 Hz, pausado fora do app.
- Removido o seletor de cores do reator que não tinha efeito na arte. Paleta fixa ciano/metal; configuração legada preservada apenas para compatibilidade.
- RAM: corrigida MEMORYSTATUSEX de 56 para 64 bytes, incluindo ullAvailExtendedVirtual. Windows agora fornece percentual, memória usada e total; celular mostra percentual e GB reais.
- Aplicativos: lista de seleção pessoal compartilhada entre PC e celular. Somente launchers cujo executável existe; atalhos são resolvidos, utilitários de manutenção são filtrados, jogos Steam precisam estar instalados. Apps abertos e aplicativos comuns instalados formam a seleção inicial. Botão “Escolher meus apps” nos dois clientes.
- Abertura utiliza executável validado e argumentos do atalho local, sem tentar executar nomes ou texto livre recebido do celular. Novos aplicativos podem ser adicionados por executável existente.
- Tela: conexão WSS contínua com certificado fixado e token no cabeçalho. Configuração 15/30/60 FPS, larguras 640/960/1280/1920, escolha de monitor, contador de FPS recebidos e tráfego. Padrão 60 FPS/960 px para fluidez.
- Sessão de captura reutiliza bitmaps, Graphics, encoder e buffer. Até dois quadros pendentes e duas sessões; frames antigos não se acumulam. Android apresenta diretamente no ImageView nativo.
- Compartilhamento continua dependendo da autorização no PC, pausa na tela de segurança/bloqueio e encerra ao revogar a permissão. Nenhuma imagem é gravada em disco.

## Como usar

1. Atualizar o app Android com o APK 1.5.0, por cima da instalação existente.
2. Apps → Escolher meus apps → marcar seus programas → Salvar. A seleção é usada no celular e no PC.
3. Dispositivos no PC → Compartilhar tela. No celular, Tela ao vivo → escolher monitor, FPS e resolução → Iniciar.
4. Começar com 60 FPS/960. Se o contador estiver baixo, experimentar 640; se houver capacidade disponível, 1280 ou 1920.

60 FPS é o limite solicitado ao transmissor. A taxa efetiva depende de captura, codificação, Wi-Fi, resolução e decodificação do telefone. O contador mostra quadros entregues ao visualizador, não uma medição de apresentação física do painel. Não confundir timer de 60 Hz do reator com FPS garantido da transmissão.

## Validação

Validação concluída: 112 testes aprovados em execuções isoladas conforme detalhado abaixo. Captura medida em `artifacts/verificacao-fluidez.json`, logs em `artifacts/tests-150.log` e `artifacts/mobile-150-build.log`.

Sem celular conectado por ADB e sem emulador Android disponível nesta máquina. Instalação, abertura, animação e fluidez no aparelho físico precisam ser verificadas no celular do dono. Isso impede afirmar que a rede/aparelho sustentará 60 FPS.

## Próximas adições relevantes

- Mouse, teclado e touchpad remoto com autorização visível no PC.
- Perfis de trabalho/jogo que abrem seus apps e restauram posições de janelas nos dois monitores.
- Painel de voz mostrando o que foi ouvido, comando identificado e motivo de uma ação não ter ocorrido; editor de apelidos pessoais.
- Alertas de RAM alta com identificação do processo responsável e ação sugerida, sem fechar programas automaticamente.
- Transmissão com encoder H.264 por hardware e adaptação de bitrate para reduzir tráfego e melhorar fluidez em resoluções maiores.

### Resultado final da validação

- Build Release da solução: aprovado, sem erros (avisos de dependências/compatibilidade já existentes).
- Android Release 1.5.0, código 8: aprovado com zero avisos e zero erros.
- APK verificado: SkiaSharp nativo em arm64-v8a, armeabi-v7a, x86 e x86_64; 22 recursos de ícone; manifesto com o ícone Jarvis.
- SHA-256 APK: `974543B6EEC1ECCFEEB2057A98AFDE43122F6CF78E004C9A1B270204EA907F9D`.
- Suíte final: 111 testes aprovados na suíte geral + 1 teste de janelas reais aprovado em execução separada (112 ao todo). A separação evita a interferência do Application.Shutdown do teste de interface WPF sobre as janelas de outro teste.
- Testes específicos de transmissão/janelas/RAM: 14 aprovados, incluindo token obrigatório, HTTPS obrigatório, limitação de quadros pendentes, revogação de captura, seleção de monitor e validação de catálogo.
- Captura local final (somente captura/escala/JPEG, sem rede nem celular): aproximadamente 29,5 FPS a 960 px e 28,8 FPS a 1280 px. Os valores variam com o conteúdo/carga do PC. O caminho Direct3D não encontrou uma saída DXGI utilizável neste ambiente e o fallback GDI foi utilizado.

### O que falta para comprovar 60 FPS sustentados

O controle de 60 FPS e a transmissão contínua estão implementados, mas a captura disponível neste ambiente ainda ficou próxima de 30 FPS. Não houve teste ponta a ponta em telefone físico. É necessário medir na sessão física do Windows com saída Direct3D disponível e no Wi-Fi/telefone reais; se o hardware continuar limitando, evoluir para captura/escala e codificação de vídeo por hardware. O contador do app permite observar a taxa entregue sem apresentar 60 FPS fictícios.

Captura Direct3D usa Vortice.Direct3D11 3.6.2 (MIT), com fallback automático GDI em caso de incompatibilidade ou mudança de dispositivo. A escolha da tela é validada por identificador e área física; rotação de monitor não suportada pelo caminho GPU usa GDI.

