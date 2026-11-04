# Jarvis 1.9.0 — espelhamento, controle e conexão direta

Atualização de 21/10/2026. Android: versão 1.9.0, código 14.

## Implementado

- Removidas as confirmações adicionais do próprio Jarvis para transmitir a tela do PC e enviar toques do celular. HTTPS, token de pareamento, pinning e limites dos comandos permanecem.
- A autorização de captura do Android continua obrigatória a cada sessão. Mouse e teclado no celular usam um serviço de acessibilidade que o dono deve habilitar nas configurações do Android uma vez. Não foi implementado root, concessão silenciosa ou desativação de proteções do sistema.
- Controle do celular pelo PC: clique, segurar com botão direito, arrastar, roda para rolagem, digitação no campo focado, Backspace, Enter, setas, Tab, Escape e colar texto com Ctrl+V. Botões Voltar, Início e Recentes na visualização normal e ampliada. Senhas não são lidas/editadas pelo serviço.
- Entradas vinculadas ao aparelho/token e à sessão de espelhamento, fila limitada e confirmação do Android. Comandos expiram em cinco segundos; parar, girar ou iniciar outra sessão invalida os anteriores. O Android aceita controle somente durante a sessão ativa, com tela desbloqueada e geometria correspondente à imagem.
- Captura revisada: dimensões físicas completas, descarte do canal alfa ao gerar JPEG, cópia respeitando o preenchimento das linhas, pedido de tela inteira no Android 14 quando disponível e ausência temporária de visibilidade sem encerrar imediatamente o serviço. Android anterior ao 14 redimensiona a captura ao girar o aparelho. A última imagem fica sendo renovada quando o fabricante só produz novos quadros ao mudar o conteúdo.
- O erro real da captura aparece no celular, em vez de atribuir todos os problemas à rede. Compartilhamento para ao apagar/bloquear a tela e pode ser encerrado pela notificação ou PC.
- Removida a integração do projeto com o aplicativo Tailscale: descoberta do pacote, abertura de VPN, instruções de login e uso de seus endereços. O programa instalado no Windows não foi desinstalado.
- Conexão externa direta por HTTPS: anúncio de IPv6 público de interface física e endereço/domínio configurável. Endereços Tailscale antigos são filtrados, mantendo o pareamento e os endereços LAN disponíveis. Confirmação por PIN curto restrita à rede local; acesso externo usa o token duradouro já pareado e o certificado fixado.
- Regra Windows `Jarvis-Https-Direto`: somente TCP 5189, executável Release atual, origem IPv6 global `2000::/3`. HTTP de download e descoberta não foi aberto à internet.

## Como usar

1. Atualizar o Jarvis no Android por cima da versão instalada.
2. No celular: **Mais → Meu aparelho → Configurar mouse e teclado no Android**; ativar o serviço Jarvis nas configurações de acessibilidade.
3. Voltar ao Jarvis e escolher **Autorizar e mostrar tela no PC**. Selecionar **Tela inteira** se o Android oferecer essa opção.
4. No PC: **Dispositivos → Seu celular**; clicar na imagem ou em **Ampliar tela** e usar mouse/teclado. Fechar a janela ampliada também encerra o espelhamento.
5. Testar inicialmente na tela inicial do Android. Conteúdo que outro aplicativo marca como protegido não pode ser capturado pelo Jarvis.

## Wake-on-LAN e acesso externo: limites ainda não confirmados

- PC Dell OptiPlex 9020, placa 0XCR8D, BIOS A02, Ethernet Intel I217-LM. Magic Packet e capacidade de despertar foram confirmados no Windows; isso não confirma a configuração atual da BIOS.
- Os namespaces WMI da BIOS Dell não estão disponíveis. A revisão automática bloqueou a tentativa de baixar/executar a ferramenta oficial Dell para consultar a BIOS, sem detalhar o motivo. A ação não foi contornada.
- Verificação restante na BIOS, ao reiniciar por iniciativa do dono: **F2 → Power Management → Wake on LAN = LAN Only**, **Deep Sleep Control = Disabled**. Não foi reiniciado/desligado o PC para testar despertar.
- IPv6 público encontrado no Ethernet: `2804:14c:b385:81bb::1000`. Esse endereço pode mudar com a operadora. Sua presença e a regra Windows não provam acesso por dados móveis: o roteador também precisa permitir a conexão e a operadora móvel precisa alcançar IPv6.
- Não foram configuradas portas no roteador nem testado acesso por uma rede externa. Em conexão IPv4 atrás de CGNAT, comunicação recebida diretamente exige solução da operadora ou um retransmissor; não foi prometido funcionamento sem essa condição.
- PC desligado não executa o servidor Jarvis. Acordá-lo pela internet requer suporte do roteador ou outro equipamento ligado na rede que envie o Magic Packet. A conexão direta com o PC ligado não resolve essa limitação de energia.

## Validação e entrega

- 43 testes aprovados, zero falhas: `tests/Jarvis.Tests/TestResults/controle-190-final.trx`. Incluem API HTTPS real, token, isolamento de aparelhos, confirmação/expiração das entradas, rotação, sessões antigas, limite da fila, rede direta, agenda, descoberta, aparelho, toque remoto e construção/renderização das telas WPF.
- Modelo informado pelo dono: **Infinix Smart 8 Pro**. Versão Android não informada. Nenhum aparelho/emulador estava conectado para testar captura, gestos ou teclado. A correção da tela preta e as ações físicas precisam ser confirmadas no telefone atualizado; não foram declaradas como verificadas no hardware.
- Desktop Release compilado sem erros; permanecem avisos de dependências anteriores desta base (NAudio e compatibilidade de pacotes WPF). APK universal e ARM64 Release compilados sem erros ou avisos na última compilação.
- `Test-ApkAndroid.ps1`, `aapt` e `apksigner` confirmaram versão 1.9.0/código 14, pacote `com.companyname.jarvis.mobile`, runtime/Skia completos nas arquiteturas, 22 recursos de ícone e assinatura de atualização preservada. Certificado Android SHA-256: `d30df2c464b72377f4eddbbac3e692695d67ca55aee150940dc0b16951653957`.
- Universal: **43.385.410 bytes (41,38 MiB)**; SHA-256 `CB111A8DDCEF0161FC6D5092C3041918135E7E25A16783A83ED4C73AD61C6359`. Inclui ARM de 32 e 64 bits, x86 e x86_64; usar esta versão no Infinix se a compatibilidade ARM64 do sistema não estiver confirmada.
- ARM64: **22.292.880 bytes (21,26 MiB)**; SHA-256 `FC83D583B7AB023CC97445FC73AD5D3F0E29C55D7021FACF924CE0BD4F8F7707`. Exige Android com suporte a aplicativos de 64 bits, não apenas processador de 64 bits.
- Arquivos em `artifacts/entrega/Jarvis-1.9.0.apk` e `Jarvis-1.9.0-arm64.apk`, também copiados como `Jarvis.apk`/`Jarvis-arm64.apk` na entrega e na pasta `mobile` do desktop. SHA-256 das cópias servidas corresponde aos APKs finais.
- Jarvis reaberto com a versão atual. Validação no processo real em `artifacts/validacao-entrega-190.json`: página/manifesto 1.9.0, downloads completos dos dois APKs com tamanho e SHA-256 corretos, HEAD 200 e retomada If-Range 206/64 bytes. HTTPS com pinning retornou 200 na LAN e no IPv6 público acessado pelo próprio PC; API recusou ausência de token com 401 e HTTP com 403. Esse teste local do IPv6 não confirma passagem pelo roteador a partir da internet.
- Para instalar sem USB, no celular conectado ao Wi-Fi: `http://192.168.0.64:5188/mobile`. O navegador serve apenas para baixar o instalador; o uso permanece no aplicativo Android instalado. O QR de instalação em Dispositivos aponta para essa página.
- Pendências externas ao teste local: confirmar imagem/gestos/teclado no Infinix atualizado, testar HTTPS em dados móveis e conferir BIOS/roteador. Não é necessário escrever código adicional para clicar/digitar na imagem; o teste físico ainda não foi realizado.

## Referências

- [Android — MediaProjection, dimensões e autorização de captura](https://developer.android.com/media/grow/media-projection)
- [Android — criação de serviço de acessibilidade](https://developer.android.com/guide/topics/ui/accessibility/service)
- [Dell — opções da BIOS OptiPlex 9020](https://www.dell.com/support/manuals/en-us/optiplex-9020-desktop/opt9020sffom-v2/system-setup-options?guid=guid-2cff7608-3f85-4cc2-8614-e84a203c395c&lang=en-us)
