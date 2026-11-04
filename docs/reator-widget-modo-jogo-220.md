# Jarvis 2.2 — reator frontal, temas, widget e modo de jogo

## Reator

A referência aprovada é `artifacts/design-mark-iv/proposta-frontal/reator-frontal-v3-animado.mp4`.
Os 400 quadros, a 25 fps e 16 segundos por volta, foram extraídos em WebP de 480 × 480 e empacotados em `src/Jarvis.Rendering.Shared/Assets/reactor-frontal-v3.zip`.
O mesmo renderizador é incorporado ao desktop e ao Android. Ele mantém no máximo quatro quadros decodificados, recolore a energia e remove o fundo externo para integrar a peça ao tema.

A montagem inicial dura cerca de 1,8 segundo. No desktop, o botão **Desmontar reator / Montar reator** separa e reúne setores de três camadas e o núcleo. No celular, toque no reator para alternar. O núcleo contrarrotaciona em relação à estrutura. Em economia ou com animações reduzidas, o desktop exibe um quadro estático e aplica a montagem instantaneamente.

## Interface

Os cartões preservam as cores-base dos recursos para acompanhar trocas sucessivas, mesmo quando são criados após uma primeira troca de tema. Fundos e bordas azuis de contêineres são associados aos papéis de painel e linha. O mobile também normaliza cores já recebidas na paleta atual.

O cartão de e-mails do início usa um cabeçalho de duas colunas, com um único contorno externo. As colunas laterais são proporcionais. Abaixo de 940 px de conteúdo, os blocos se reorganizam verticalmente.

A página de chat, a entrada de comando por texto do topo e o botão de digitar comando no mobile foram removidos. O identificador antigo de navegação permanece reservado e leva ao início, preservando IDs existentes. Microfone, transcrição de voz e controles continuam disponíveis.

## Modo de jogo

Acesso pelo dock ou pelo início. O usuário escolhe explicitamente os processos dispensáveis, entre apps conhecidos. A lista é revalidada ao ativar; processos de outras sessões, do sistema e jogos ficam fora dela.

O Windows recebe o plano de alto desempenho, ou o overlay de desempenho em máquinas Modern Standby compatíveis. Se o Windows recusar ambas as opções, o modo não é ativado e nenhum aplicativo é fechado. Não há overclock nem alteração de drivers.

Janelas recebem fechamento normal; se houver um aviso de trabalho não salvo, permanecem abertas. Processos sem janela só são encerrados quando foram selecionados e constam da lista permitida. O Jarvis ativa economia, para a animação principal e reduz a frequência de consulta da telemetria. Ao desativar ou encerrar normalmente o Jarvis, restaura o plano e a preferência de economia anteriores. Apps fechados não são reabertos.

Voz: **ativar modo de jogo** e **desativar modo de jogo**. Ativação por voz não fecha aplicativos; use a seleção da página para autorizar fechamentos.

## Widget Android

APK 2.2.0, código 19. Widget nativo `AppWidgetProvider` com atalhos de voz, controles e agenda, sem gravação ou comandos em segundo plano. Em **Mais → Adicionar widget à tela inicial**, o aplicativo solicita a fixação ao launcher; quando não há suporte, fornece o caminho manual em Widgets. A confirmação do launcher é feita pelo usuário.

Referências de plataforma: [widgets MAUI/Android](https://devblogs.microsoft.com/dotnet/how-to-build-android-widgets-with-dotnet-maui/) e [gerenciamento de planos do Windows](https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/Power/managing-power-schemes.md).

## Validação

Testes de renderização conferem quadros distintos, montagem/desmontagem, transparência externa e recoloração do núcleo. O teste de interface cobre o modo de jogo, ausência do chat, trocas sucessivas de tema e o cabeçalho de e-mails em largura reduzida. O serviço de jogo é testado com um plano de energia simulado, sem alterar o Windows nem fechar processos durante os testes.

A instalação e a fixação do widget em aparelho físico dependem de instalar o APK e aceitar a solicitação do launcher.
