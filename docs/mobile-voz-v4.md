# Jarvis 1.4 — mobile, ícone, voz local e monitores

## Entrega

- Início Android redesenhado com cabeçalho próprio, cartão do reator, conexão real, atalhos e acesso direto a Suas telas ao vivo.
- Ícone vetorial Jarvis original: núcleo ciano, órbita e letra J. Substitui os assets .NET do launcher, foreground adaptativo e splash. Arquivos em `src/Jarvis.Mobile/Resources/AppIcon` e `Resources/Splash`.
- Versão Android 1.4, código 6. Instale o APK atualizado sobre a versão anterior, mantendo a assinatura e o pareamento.
- Tela ao vivo lista os monitores reais. Escolha Monitor 1 ou Monitor 2 antes de iniciar, ou altere a seleção durante a transmissão. Atualizar busca telas conectadas/removidas.
- O cache da captura inclui o monitor. Uma troca não reutiliza o quadro da outra tela; ID inválido ou monitor desconectado gera mensagem, sem selecionar outra tela silenciosamente.

## Voz sem chave de IA

Desktop: Vosk, Whisper e Piper rodam na máquina com os modelos locais. Os comandos básicos não dependem de serviço de IA externo, chave ou instalação de uma API de modelos em localhost. A conexão HTTPS embutida entre celular e PC continua sendo necessária para o controle remoto; ela não é uma API de IA.

Correções:

1. Detectar Jarvis não reinicia mais o Vosk no meio da frase. A transcrição local da frase completa é conservada até o fim do segmento.
2. O segmento final sempre segue para avaliação, inclusive quando o Vosk só reconhece a ativação no resultado final. Antes, esse caminho podia deixar de despachar o áudio ou iniciar uma captura tardia.
3. Se o Whisper falhar, retornar vazio ou não entender um comando que o Vosk reconheceu, há alternativa local. A verificação de ativação continua obrigatória no reconhecimento ambiente; permissões, ambiguidades e confirmações são preservadas.
4. Um vigia finaliza a fala quando um headset deixa de entregar pacotes durante o silêncio. Não depende exclusivamente de receber bytes silenciosos do dispositivo.
5. Comandos aceitos e erros do reconhecimento têm diagnóstico em `%LocalAppData%/Jarvis/voz.log`. Fala ambiente rejeitada não é gravada nesse log.
6. No Android, frases como “abre a calculadora por favor” e números por extenso passam pelo tradutor local de comandos. O reconhecimento do Android solicita preferência offline, mas depende do serviço e dos modelos de idioma instalados no aparelho; essa preferência não garante operação offline em todos os celulares.

Exemplos no PC: “Jarvis, abra a calculadora”, “Jarvis, coloque o volume em cinquenta por cento”, “Jarvis, que horas são”. Fechamento de aplicativos e outras ações de risco conservam confirmação. Jogos e programas ambíguos não são escolhidos automaticamente.

Não é reconhecimento biométrico e não promete executar qualquer frase livre. As ações suportadas continuam limitadas ao roteador e ao catálogo. Gmail e sites, quando usados, precisam de internet própria dessas integrações.

## Reator

Os dois apps têm órbitas luminosas, filamentos, precessão, oscilação e pulsação. No desktop, a energia reage ao áudio. O modo econômico agora reduz a taxa de animação para aproximadamente 8 FPS em vez de congelar tudo. Animações reduzidas continuam respeitadas. Ocultar/minimizar a tela ou deixar o Android em segundo plano interrompe a animação.

O reator conserva uma imagem detalhada com elementos desenhados em tempo real; a imagem não contém malhas independentes para cada anel metálico. O ícone do launcher é estático, conforme o formato normal dos ícones Android.

## Usar a seleção de telas

1. Instale `artifacts/entrega/Jarvis.apk`.
2. No Jarvis do PC, abra Dispositivos e ative **Compartilhar minhas telas com o app pareado**.
3. No telefone, toque em **Suas telas, ao vivo**, selecione o monitor e toque em Iniciar.

Mantidos HTTPS, token, certificado fixado, captura apenas sob demanda, ausência de gravação e pausa na tela de bloqueio. A visualização continua sendo JPEG até 1280 × 720 e alvo de 2,5 quadros/s, sem áudio ou entrada de mouse/teclado.

## Verificação e pendências

O teste completo gera uma frase com Piper e passa por Vosk, segmentação real, Whisper, roteador e execução em um controlador de aplicativos de teste. Nenhum programa do usuário é aberto nesse teste. Também são testados o fallback sem Whisper, rejeição de ativação ambiente, números falados e escolha do monitor pelo endpoint autenticado.

Validação final: solução Release compilada sem erros, 25 avisos de pacotes/APIs; 107 testes aprovados, sem falhas ou ignorados. O ícone PNG gerado pelo empacotador foi inspecionado. O microfone padrão estava ativo, sem mudo e com volume de entrada em 100%; nenhuma dessas opções foi alterada.

A máquina apresentou dois monitores reais de 1600 × 900. O teste nativo capturou os monitores enumerados, conferiu o cache por monitor e rejeitou ID inexistente. A inicialização com `--startup` e o encerramento normal passaram; o executável atualizado foi reaberto e confirmou o microfone ativo.

APK 1.4, código 6, 28.527.072 bytes; SHA256 `9DE6AC3EE400A4EC3DCFF18514D43D5B985002836D17159ACA4E92758E54E16F`. As cópias de entrega e da pasta mobile do desktop são idênticas.

Foi encontrado `ReduzirAnimacoes=true` nas preferências locais, o que congelava o reator do desktop. Essa preferência foi desativada conforme o pedido de animação, preservando os demais campos.

Logs desta versão: `artifacts/build-v4.log`, `artifacts/testes-v4-entrega.log`, `artifacts/startup-v4.log`; metadados e hash do APK em `artifacts/apk-v4.json`; diagnóstico sem áudio em `artifacts/microfone-v4.json`.

Pendências físicas: conferir a voz humana e o ambiente do headset, instalar o APK em Android, verificar o ícone no launcher, conferir a disposição dos cartões e alternar entre as duas telas no telefone. A validação com áudio sintetizado não substitui o teste com a voz do usuário.
