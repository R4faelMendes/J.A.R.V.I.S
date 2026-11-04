# MARK IV — revisão visual 2.1.0 entregue em 22/10/2026

Referências locais fornecidas pelo usuário: JARVIS-Mark-IV.html e JARVIS-Mobile-Mark-IV.html (cópias preservadas nesta pasta). As simulações desses arquivos não são código de produção: somente estilo e disposição visual foram usados.

Escopo: apresentação nativa WPF/MAUI, nenhuma remoção de funções, permissões ou rotas. Shell desktop com título nativo arrastável, dock inferior, painéis de comunicação/reator/telemetria; widgets reais de e-mail, Spotify e conexão preservados. Mobile mantém todos os handlers, abas e entradas do menu, com cartões e tipografia no padrão do HTML. Reator extraído da referência pela habilidade imagegen, usado pelos dois aplicativos, recoloração seletiva da energia e flutuação/respiração de baixa amplitude. Metal e cobre preservados. Movimento reduzido/econômico respeitados no desktop.

## Entrega e validação

- Desktop WPF e Android Release compilados sem erros. Android 2.1.0, código 18, mesma identidade e assinatura de atualização. Não exige desinstalação nem redefinição do pareamento.
- 62 testes de regressão passaram (`tests/Jarvis.Tests/TestResults/markiv-final.trx`): comandos, voz, transmissão/toques, agenda, conexão, APIs autenticadas, temas, aplicativo/janelas e interface. Os quatro testes do projeto MAUI também passaram (`tests/Jarvis.Mobile.Tests/TestResults/markiv-mobile.trx`), incluindo o renderizador no Skia 2.88.9 efetivamente utilizado pelo mobile.
- O teste de interface confere as dez entradas do dock com conteúdo visível, recursos/bindings de temas, arraste da janela e barra de voz dentro da janela em F11. A captura é da árvore nativa WPF; na captura, o quadro do Skia foi convertido para Image para o RenderTargetBitmap capturar a mesma arte. Não é um mock HTML.
- Prévia: `C:/Users/Administrator/Desktop/Jarvis/artifacts/preview/Mark-IV-PC.png`. Prévia do tema prata: `artifacts/preview/Configuracoes202-ao-vivo.png`. Telas de apps, agenda, e-mails, configurações, aparelho e segurança também renderizadas pelo smoke test.
- APK: `C:/Users/Administrator/Desktop/Jarvis/artifacts/entrega/Jarvis-2.1.0.apk`, também copiado como Jarvis.apk para a entrega e servidor local. Tamanho 45.531.552 bytes; SHA-256 `946A8EA383021DCFB8487ADEC680AACF5F7F28EBB32AA3C52C3013C14C26ED1F`.
- Pacote universal com ARM 32 bits e demais ABIs, Skia nativo em todas elas e ícone Jarvis (22 recursos). Verificados manifesto real (2.1.0/código 18), assinatura preservada e arte do reator no manifesto de recursos das quatro montagens Android após trimming.
- Chave de assinatura SHA-256: `d30df2c464b72377f4eddbbac3e692695d67ca55aee150940dc0b16951653957`. Permissões Android preservadas; não foi adicionado serviço de Acessibilidade.
- Download real comparado ao arquivo entregue: hash e tamanho idênticos, HEAD 200, retomada 206. HTTPS LAN e IPv6 no próprio PC com pinning: 200; API sem token: 401; API por HTTP: 403. Resultado: `artifacts/validacao-entrega-210.json`.
- Desktop reaberto. Tema inicial desta apresentação: Mark IV/ciano, como nos HTMLs; todos os outros temas permanecem selecionáveis e sincronizados. A seleção anterior foi preservada em `artifacts/design-mark-iv/tema-anterior.json`.
- Download na mesma rede: http://192.168.0.64:5188/mobile .

## Funções preservadas

Navegação completa; voz local e atalhos; conta de e-mail e configurações; agenda/calendário/lembretes e sincronização; aplicativos e seleção/adição; janelas e grades; CPU/RAM e gráficos; mídia/volume/capa do Spotify/notificação; pareamento e diagnósticos; Wake-on-LAN; seleção de monitor, transmissão, tela cheia e toque remoto; painel/bateria/localizar aparelho; espelhamento normal do celular; temas sincronizados e opções econômicas existentes. A função de controle do celular por mouse/teclado que já estava ausente do instalador 2.0.2 continua no mesmo estado, sem alterações de permissões nesta revisão. Os HTMLs não introduziram rotinas fictícias, telemetria de demonstração ou ações simuladas na aplicação.

## Arte e movimento

A habilidade imagegen e a ferramenta integrada de imagem foram usadas para extrair o reator da referência. Não foi utilizada API externa com chave do usuário. Arte salva no projeto em `C:/Users/Administrator/Desktop/Jarvis/src/Jarvis.Rendering.Shared/Assets/reactor-mark-iv.png` (PNG transparente, 1280 × 1280), incorporada nos dois apps. Prompt exato e origem em `C:/Users/Administrator/Desktop/Jarvis/docs/design/mark-iv/reator-prompt.txt`.

O renderizador nativo comum aplica deslocamento vertical de baixa amplitude, oscilação de até 0,45 grau, respiração discreta e leve reação ao áudio. Recolore somente os trechos azuis/cianos ao escolher outros temas, preservando cobre, aço e luz branca. A arte recolorida é armazenada em cache; conversão de cores em código gerenciado evita chamadas nativas por pixel no Android. No ciano mantém as cores da referência. Desktop até 30 quadros/s, reduzido em modo econômico e suspenso quando oculto/minimizado; o streaming do PC não foi alterado.

## Limites de verificação

Não há aparelho conectado ou emulador disponível: a instalação, abertura e aparência final no Infinix ainda precisam de confirmação física. Os testes MAUI são executados com os controles reais sem handlers Android, e o renderizador foi exercitado no Skia 2.88.9. Conexão por rede móvel externa não foi testada nesta revisão. Nada foi publicado para contas/lojas externas.

