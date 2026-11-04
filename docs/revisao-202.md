# Revisão 2.0.2 — entregue em 21/10/2026

Usuário confirmou: APK 2.0.1 instala, mas não abre. Botões e cartões do PC ainda ficam azuis.

Reproduzida exceção de inicialização no código real do tema MAUI: SolidColorBrush padrão possui Color nula. Map tentava acessar Red e lançava NullReferenceException. Criado projeto de regressão portátil com os controles MAUI 8.0.100 reais; o teste falhou antes da correção e passou depois. Não havia celular nem emulador conectado, portanto abertura no Infinix ainda depende de validação física.

Correção mobile: preservar brushes sem cor definida, proteger Color e FileImageSource.File nulos e observar somente propriedades de aparência/handler/source. Corrigida também a conversão implícita de Color para Brush na expressão condicional: propriedades de cor agora recebem Color, não um brush incompatível. O percurso inclui explicitamente conteúdo de cartões, páginas, layouts e Shell, com controle de elementos já visitados. Mantida a ausência de Acessibilidade no instalador.

Desktop: recursos compartilhados de cores e brushes atualizados a cada tema, estilos passam a consumir brushes por DynamicResource, removida exclusão de propriedades com binding (SetCurrentValue preserva a expressão), cores da própria paleta deixam de sofrer novo tingimento. Validação ampliada para botões/cartões ligados a dados e janela completa após evento real de troca de tema.

## Validação e entrega concluídas

- Android Release 2.0.2, código 17, e desktop Release compilados sem erros. Avisos anteriores de pacotes/APIs permanecem.
- 62 testes integrados de desktop/Core/servidor/voz aprovados, zero falhas: `tests/Jarvis.Tests/TestResults/revisao-202-final.trx`.
- 3 testes MAUI aprovados: controles novos e brushes nulos, Shell/imagem sem handler, aplicação e reaplicação de temas em botões/cartões/gradientes durante cinco trocas sucessivas. Projeto `tests/Jarvis.Mobile.Tests` adicionado à solução. Usa os controles MAUI 8.0.100 reais e o mesmo arquivo TemaMobile.cs, sem emular Android: `tests/Jarvis.Mobile.Tests/TestResults/mobile-202-final.trx`.
- Regressão do desktop verifica binding preservado em cartão, botão de estilo primário recebendo recurso do tema e janela completa após evento real de escolha de tema. Auditoria de brushes azuis primários restantes: zero (`artifacts/preview/AzuisRestantes202.txt`, vazio).
- Prévia de configurações carregadas no tema prata: `artifacts/preview/Configuracoes202-ao-vivo.png`.
- APK universal com ARM de 32 bits, bibliotecas Skia nativas em todas as arquiteturas, 22 recursos de ícone Jarvis, sem AOT/depuração. Manifesto real inspecionado com aapt: sem serviço de Acessibilidade/SMS/listener de notificações. Verificação: `artifacts/apk-202-validacao.json`, `artifacts/manifesto-202-apk.txt`.
- Assinatura mantida: certificado SHA-256 `d30df2c464b72377f4eddbbac3e692695d67ca55aee150940dc0b16951653957`. Atualiza a versão anterior sem exigir desinstalação.
- Arquivo: `artifacts/entrega/Jarvis-2.0.2.apk`, também entregue como Jarvis.apk e copiado para o servidor desktop. 43.434.400 bytes; SHA-256 `154768D8C4D3C10E1012458438959A28DE9AE6C641B45904330EB5955B8310EE`.
- Desktop reaberto e servidor confirmado na versão 2.0.2. Download real com hash/tamanho idênticos à entrega: HEAD 200 e retomada 206 (64 bytes). HTTPS com pinning LAN/IPv6 local: 200. API sem token: 401; API via HTTP: 403. Resultado: `artifacts/validacao-entrega-202.json`.
- Download na mesma rede: http://192.168.0.64:5188/mobile . Somente instalador universal, sem oferta separada ARM64.

## Confirmações que ainda dependem do aparelho

Não há celular conectado nem emulador disponível. A exceção foi reproduzida e eliminada no código real MAUI, mas isso não confirma todos os caminhos nativos no Infinix. O usuário precisa instalar 2.0.2 por cima da versão anterior e confirmar abertura, navegação e conexão no aparelho. Nenhuma proteção do Android foi desativada e o controle do celular pelo PC que dependia de Acessibilidade continua fora do instalador padrão. Acesso por rede externa não foi testado nesta revisão.


A execução integrada também expôs a transcrição local sintética “Pervis, abrir calculadora”. Acrescentada somente essa variante explícita ao nome do assistente, mantendo a exigência de comando fixo e as validações de risco existentes; adicionada regressão para a variante. Nenhum comando livre é executado.
