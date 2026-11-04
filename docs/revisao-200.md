# Jarvis 2.0.0 — reator comum, voz local e estabilidade Android

Pedido de 21/10/2026. Android: versão 2.0.0, código 15.

## Alterações implementadas

- **Um reator para os dois apps:** `src/Jarvis.Rendering.Shared/ReatorRealista.cs` é compilado diretamente pelo desktop e pelo mobile. Mesma câmera, proporções, faces, anéis, núcleo e trajetórias de animação. Hospedeiros WPF e MAUI apenas enviam tamanho/tempo/nível de áudio. O reator Skia usado nas outras páginas do PC também delega ao mesmo desenho.
- Materiais com reflexos metálicos, bordas chanfradas, frisos, parafusos, recortes, iluminação quente/fria, sombra de contato e pedestal. Anéis giram em sentidos alternados, com precessão; núcleo e meridianos giram independentemente. Energia acompanha o tema. Mobile limita a atualização a 30 quadros/s para reduzir carga no aparelho; isso não altera geometria nem movimentos. Animação para quando a página/app fica oculto.
- **Espelhamento Android:** cada `Image` agora chama `Close()` explicitamente, além de liberar o wrapper .NET. Sem isso, os buffers do `ImageReader` ficam adquiridos e a captura para depois de poucos quadros. Buffer/plane também são liberados; encerramento tolera o Android já ter encerrado a projeção. Imagens transparentes continuam convertidas para opacas antes de JPEG.
- Captura alternativa pela API de acessibilidade do Android 11+, somente durante a sessão de compartilhamento autorizada e com tela desbloqueada. É tentada quando quadros permanecem pretos/ausentes e o serviço já está ativo. Também há chave **Modo compatível** em Meu aparelho. Essa modalidade atualiza mais devagar (aproximadamente duas capturas/s); não contorna conteúdo protegido por outros apps. Divulgação do serviço Android atualizada para explicar captura e controle.
- **Voz do celular:** gravação PCM16 mono/16 kHz dentro do Jarvis, com permissão de microfone, limite de 12 segundos e conclusão por pausa ou botão. Áudio vai pelo HTTPS pareado ao Whisper local do PC, sem Google Speech, provedor externo ou chave de IA. Reconhecimento não executa texto livre: a transcrição volta ao celular e passa pela lista de comandos fixos e seleção de apps por identificador. Ações de risco continuam com confirmação. Resposta pode ser lida pela voz do Android.
- API de áudio exige token/TLS, aceita somente PCM, limita corpo a 480 KB e aplica limite de requisições. Transcrição isolada não dispara comandos. Modelo local `models/ggml-base.bin`, carregado sob demanda e descarregado por ociosidade.
- Nos testes reais, Whisper retornou variações como **“Garvis, abriu calculadora.”**, **“Carvis, abrir calculadora.”** e artigos extras no nome do app. Agora o tradutor tolera uma letra inserida/removida/substituída em Jarvis e resolve artigos extras antes do nome do app, mantendo os comandos permitidos e confirmações. Nomes ambíguos e frases negadas não executam ações.
- **Adicionar apps no PC:** seletor de arquivo `.exe` ou `.lnk`, nome editável, resolução do destino/argumentos de atalhos na camada Sistema, seleção automática e persistência. Repetir o mesmo app não duplica o catálogo. Erros aparecem na interface; não são exceções soltas no clique do botão. Voz do desktop, interface e controle remoto usam a mesma instância do catálogo; novos itens podem ser reconhecidos sem reiniciar o Jarvis. Aparecem também no catálogo consultado pelo Android.
- **Layout PC:** F11 implementado com entrada/saída de tela cheia no monitor atual, Escape para sair, restauração de posição/tamanho e limites de maximização pela área de trabalho. Barra de voz ocupa a largura disponível e trunca frases longas. Página inicial tem rolagem quando falta altura; três cartões inferiores com espaçamento simétrico e e-mails centralizados no conjunto.
- **Temas nos dois apps:** Ciano, Âmbar, Violeta, Esmeralda e Rubi, com interface e energia do reator mudando juntas. PC: Configurações. Android: Mais → Temas. Estado persistido no PC; celular conectado pode escolher e recebe o estado autoritativo do PC na verificação de conexão. Desconectado mantém o último tema recebido. Não há exigência de conta externa.
- Diagnóstico Android permite compartilhar um arquivo com versão, estado da conexão/captura e última pilha de fechamento inesperado. Não grava áudio, transcrições, imagens ou ações recentes no arquivo. Compartilhamento depende do botão e da escolha do dono no Android; não é enviado automaticamente.

## Verificação

- **53 testes aprovados, zero falhas:** `tests/Jarvis.Tests/TestResults/revisao-200-final.trx`. Inclui API real com TLS/token, isolamento de aparelhos, fila/expiração/rotação do controle, limites da gravação, transcrição sem executar comandos, tolerância ao nome/negações, temas, novos apps/atalhos, descoberta, agenda, aparelho, toques e layout WPF em F11.
- **Piper e Whisper reais testados:** áudio “Jarvis, abrir calculadora” foi sintetizado sem reprodução no alto-falante, convertido de WAV para PCM16/16 kHz, transcrito pelo serviço usado pelo mobile e reconhecido pelo tradutor como `abrir_app`. Não abriu a calculadora durante o teste. Resultado em `artifacts/voice/transcricao-mobile-200.txt`.
- Prévia do reator e do layout inicial inspecionada em `artifacts/preview`. O teste WPF passa a renderizar o reator comum na prévia e verificar que a barra de voz cabe em F11.
- Desktop Release compilado sem erros; permanecem avisos das dependências WPF/NAudio existentes. APKs Android universal e ARM64 Release compilados sem erros ou avisos na última compilação. Conferência extra da interface após compartilhar o catálogo com a voz também passou: `tests/Jarvis.Tests/TestResults/interface-200-final.trx`.
- `Test-ApkAndroid.ps1`, `aapt` e `apksigner` confirmaram versão 2.0.0/código 15, pacote `com.companyname.jarvis.mobile`, microfone, serviço de acessibilidade protegido, captura/gestos no recurso Android por versão, ícones e bibliotecas nativas. O certificado de atualização foi preservado: SHA-256 `d30df2c464b72377f4eddbbac3e692695d67ca55aee150940dc0b16951653957`.

## O que depende do aparelho

Não há Infinix/emulador conectado ao ADB. Portanto, liberar os buffers e acrescentar captura alternativa são correções implementadas, não uma confirmação de que o defeito específico do telefone foi eliminado. Falta testar no Infinix Smart 8 Pro: abrir app, compartilhar a tela inicial, girar, usar o modo compatível e controle, gravar comando pelo microfone e receber um tema.

A autorização nativa de projeção e a ativação de acessibilidade continuam sob controle do Android. Se acessibilidade não estiver ativa, o espelhamento normal funciona sem o recurso alternativo. Se o fabricante impedir a captura normal, o modo compatível precisa do serviço ativado pelo dono.

## Entrega

- Jarvis reaberto e respondendo como 2.0.0. Downloads disponíveis em **http://192.168.0.64:5188/mobile**, pelo Wi-Fi. Atualizar sobre a instalação existente para manter pareamento e dados. No Infinix, preferir a versão universal se o suporte do Android a apps ARM64 não estiver confirmado.
- Universal: **43.446.850 bytes (41,43 MiB)**; SHA-256 `9426CFFAD1B7487B0A26ADD382089143855929C54DCA93EA416234F6E9FCC41A`. Entrega: `artifacts/entrega/Jarvis-2.0.0.apk`.
- ARM64: **22.333.840 bytes (21,30 MiB)**; SHA-256 `B70587EC70921DA6D2712CDEC486F5CD77A531BFAB448076B6E103663B525116`. Entrega: `artifacts/entrega/Jarvis-2.0.0-arm64.apk`.
- As cópias na entrega e na pasta `mobile` do desktop têm os mesmos hashes. Download completo dos dois arquivos, manifesto/página 2.0.0, HEAD 200 e retomada If-Range 206/64 bytes conferidos no processo real. HTTPS com pinning retornou 200 na LAN e no IPv6 acessado pelo próprio PC; ausência de token retornou 401 e API por HTTP retornou 403. Resultado: `artifacts/validacao-entrega-200.json`.
- Nenhum teste externo por dados móveis foi realizado nesta revisão. Não foi publicado APK em loja nem alterada a configuração do roteador.
- Prévias: `artifacts/preview/Reator200-final.png`, `Reator200-ambar.png` e `Nucleo.png`. São renderizações do código real; dados de desempenho não foram inventados nas prévias sem conexão.

## Teste final no Infinix

1. Atualizar o APK para 2.0.0 e abrir Jarvis.
2. Em **Mais → Meu aparelho**, ativar o serviço Jarvis em Acessibilidade se ainda não estiver ativo. Autorizar o compartilhamento e testar primeiro a tela inicial do Android.
3. Se continuar preta, ativar **Modo compatível**, mantendo a sessão de compartilhamento autorizada. No PC abrir **Dispositivos → Ampliar tela**.
4. Em **Mais → Falar com Jarvis**, permitir o microfone e dizer **“abrir calculadora”**. Pausa ou **Concluir fala** finaliza a gravação. Aguardar a transcrição e confirmar a abertura. O PC precisa estar conectado.
5. Em **Mais → Temas**, escolher uma cor e conferir PC e celular. Pelo PC também pode mudar em Configurações. A sincronização usa a verificação de conexão (normalmente cerca de três segundos quando online).
6. Se o app fechar novamente ou continuar sem imagem, usar **Mais → Diagnóstico → Compartilhar diagnóstico de tela preta ou fechamento**. A captura física e a gravação do microfone do Infinix continuam como a única validação pendente deste pedido que não pôde ser feita neste ambiente.

## Referências técnicas

- [Android ImageReader — imagens devem ser fechadas para devolver buffers](https://developer.android.com/reference/android/media/ImageReader)
- [Android AccessibilityService — captura compatível e gestos](https://developer.android.com/reference/android/accessibilityservice/AccessibilityService)
- [Android MediaProjection — autorização de cada sessão](https://developer.android.com/media/grow/media-projection)
