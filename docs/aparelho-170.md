# Jarvis 1.7.0 — painel, encontrar celular e tela do Android no PC

## Funcionalidades

- PC → Dispositivos: painel do aparelho com modelo, sistema, bateria, estado de carregamento, armazenamento livre/total, conexão, VPN e presença. Dados reais enviados pelo Android; sem aparelho conectado o painel permanece vazio.
- Encontrar celular: pedido autenticado para tocar por até 20 segundos. PC diferencia pedido enviado, confirmação recebida, ausência de confirmação e conclusão. Parar som disponível no PC e na notificação do Android. Respeita o volume e as regras de som do aparelho.
- Tela do celular no PC: iniciar no Android em Mais → Meu aparelho → Autorizar e mostrar tela no PC. Android solicita consentimento por sessão. PC mostra a imagem em Dispositivos e permite ampliar ou parar. A notificação do Android também encerra a captura.
- Compartilhamento somente de imagem; sem áudio, gravação ou controle de outros aplicativos do Android. Superfícies que o Android protege contra captura continuam protegidas.
- Serviço de aparelho em primeiro plano mantém painel e localizar disponíveis em segundo plano após abrir o app pareado. Notificação visível permite desconectar. Serviço de projeção é separado e só inicia após a autorização do sistema.

## Como usar

1. Instalar o APK 1.7.0 por cima da versão anterior, pelo link/QR em Dispositivos no PC. Mantém a assinatura e o pareamento existente.
2. Abrir Jarvis no Android. Em Mais → Meu aparelho, ativar painel e localizar caso estejam desativados.
3. No PC → Dispositivos, selecionar o aparelho. Encontrar celular solicita o som e aguarda a confirmação enviada por ele.
4. Para visualizar a tela, tocar em Autorizar e mostrar tela no PC no Android e aceitar o diálogo do sistema. O PC não inicia a captura silenciosamente.
5. Parar pelo PC, pelo aplicativo Android ou pela notificação. Bloquear/desligar a tela do celular também encerra o compartilhamento. Cada retomada exige nova autorização.

Conexão por Wi-Fi ou pela rede privada Tailscale existente. PC e telefone precisam estar alcançáveis. Sem serviço ativo, sem rede ou com o aplicativo forçado a parar pelo Android, encontrar celular não pode acordar o aparelho remotamente. Pedidos expiram em 20 segundos para evitar toque inesperado muito depois da solicitação.

## Implementação

- Contratos em `Jarvis.Core/AparelhoCompanheiro.cs`; interface do desktop acessa somente `ICentralAparelhos`.
- Central volátil por token pareado em `Jarvis.Server/CentralAparelhos.cs`. Identificador público é derivado do hash do token; comandos de um aparelho nunca são entregues a outro.
- Protocolo de pulso/ordens/confirmação e envio de quadros em `/api/celular/*`, protegido pelo middleware HTTPS/token existente. Pinning do cliente preservado.
- Somente ordens fixas: localizar, silenciar e parar espelho. Sem execução de texto remoto.
- Sessões de projeção independentes, com validação do token e identificador da sessão. Quadros atrasados de uma sessão encerrada são recusados, inclusive após uma nova sessão começar.
- Apenas o último JPEG fica em memória, até 2 MB, com resolução máxima de 1920 por dimensão e até 3 milhões de pixels. Upload limitado a 12 pedidos/s por token; cliente envia aproximadamente 6 quadros/s com lado maior de até 1280 px e qualidade 65.
- Quadro sem atualização por 3 segundos é removido da visualização; sessão sem quadros por 10 segundos expira. Parar apaga imediatamente o quadro mantido no servidor. Até oito aparelhos no estado em memória.
- Android: `MediaProjection`, `ImageReader`, ajuste de superfície em mudanças de tamanho, tratamento de padding das linhas, captura/compactação fora da thread da interface e encerramento ao bloquear/desligar a tela. Buffer temporário devolvido ao pool com limpeza.
- Tipos de serviço Android: `connectedDevice` e `mediaProjection`, com permissões específicas no manifesto. Projeção nunca é restaurada após reinício do processo; não é armazenado o consentimento da captura.
- PAINEL/LOCALIZAR pode permanecer ativo ao navegar no app ou colocá-lo em segundo plano. O usuário pode desativá-lo. Não depende do serviço de Spotify.

## Validação

- 35 testes aprovados, zero falhas (`tests/Jarvis.Tests/TestResults/aparelho-170-final.trx`): isolamento entre aparelhos, dados inválidos, presença, confirmação e expiração do som, sessões antigas, interrupção do espelho, cabeçalho/resolução/tamanho JPEG, autenticação/HTTPS, pareamento, interface, agenda, grade e controle remoto.
- Desktop e Android Release compilados sem erros. Android mantém somente o aviso já existente de `ConnectivityManager.GetAllNetworks` obsoleto.
- APK 1.7.0, código 12; quatro arquiteturas, Skia nativo, 22 recursos de ícone e os dois novos serviços Android com seus tipos e permissões validados pelo script `Test-ApkAndroid.ps1`.
- Assinatura igual à versão anterior: atualização mantém os dados do app e o pareamento.
- APK de entrega: `artifacts/entrega/Jarvis-1.7.0.apk`.
- SHA-256: `4BB24D0BB63F8E10BD16E7D88C1D4BA09F5D29347D993A82ED7DA4B925745614`.
- Prévia da interface com estado vazio real em `artifacts/preview/Celular.png`.
- Revisão em 20/10/2026: Jarvis Release reaberto, `/ping` informa 1.7.0 e as páginas de instalação retornam HTTP 200 nos endereços Wi-Fi e Tailscale do PC. Download completo via Wi-Fi com SHA-256 idêntico ao APK de entrega; download parcial pelo endereço Tailscale retorna HTTP 206. Verificação feita no PC, sem simular um telefone conectado de outra rede.

Ainda sem telefone conectado por ADB. Reprodução do som, consentimento `MediaProjection`, rotação e captura física precisam ser confirmados no aparelho real. Compilação e testes de protocolo não substituem essa verificação.

## Referências

- https://developer.android.com/develop/background-work/services/fgs/service-types
- https://developer.android.com/media/grow/media-projection
- https://learn.microsoft.com/en-us/dotnet/api/android.media.projection.mediaprojectionmanager
