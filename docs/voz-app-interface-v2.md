# Jarvis — voz, interface e aplicativo nativo

Entrega local de 17/10/2026. Android 1.2, versão interna 4. Este documento substitui as instruções visuais e de instalação da entrega anterior em central-3d-e-mobile.md.

## Usar agora

O executável atualizado continua em `src/Jarvis.Desktop/bin/Release/net8.0-windows10.0.19041.0/Jarvis.Desktop.exe`, usado pelo atalho existente.

1. Com o Jarvis aberto, diga **“Jarvis, abra a calculadora”** ou **“Jarvis, coloque o volume em cinquenta por cento”**. Não precisa apertar o botão. Também pode dizer Jarvis, esperar a indicação de escuta e falar o comando.
2. Se necessário, escolha o dispositivo em Configurações e ajuste a sensibilidade. Na validação local o microfone aberto foi `Microphone (3- Wireless Stereo Headset)`, 48 kHz mono. A captura converte esse sinal para PCM16 mono de 16 kHz.
3. O X da interface recolhe o Jarvis para a bandeja, mantendo escuta e servidor. Para encerrar, use Sair na bandeja. A escuta pode ser desativada explicitamente nas configurações/bandeja.
4. Transfira `artifacts/entrega/Jarvis.apk` para o Android, por exemplo por USB, e instale. A página Dispositivos tem o botão Copiar instalador APK.
5. Abra o app Jarvis, toque em Fotografar QR code e fotografe o QR em Dispositivos no PC. Alternativa: Descobrir PC na minha rede e informar o PIN de seis dígitos.

PC e telefone devem compartilhar uma rede que permita comunicação entre os aparelhos. O QR usa `jarvis://parear` e inclui certificado, token de uso único e candidatos de IP. O APK precisa estar instalado primeiro. O fluxo não exige site ou navegador.

## Voz

- O fluxo normal agora usa Vosk para ativação rápida e Whisper local para transcrever o comando completo. Antes, o Whisper não participava adequadamente da escuta normal e o PCM era passado como se fosse WAV.
- Modelo `models/ggml-base.bin` disponível nesta máquina; tiny é a alternativa se base estiver ausente. Base foi obtido do repositório oficial whisper.cpp, `https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin` (147.951.465 bytes).
- Remoção de componente contínua, ganho limitado, limiar adaptativo de ruído, áudio anterior à ativação, encerramento por silêncio e tempo máximo de captura.
- Se Vosk perder a palavra de ativação, Whisper verifica o segmento localmente. Só prossegue se a ativação estiver no início; uma menção ambiente a Jarvis no meio de outra frase não autoriza comandos.
- Frases de cortesia e números falados em português são tratados. A aproximação de nomes é restrita a abertura de aplicativo não jogo, com correspondência única; fechamento conserva correspondência exata.
- O modelo é liberado após ociosidade. A própria síntese suspende comandos normais para reduzir autoativação. “Jarvis, pare” continua sendo a interrupção prevista.

É ativação por palavra, não identificação biométrica de quem está falando. A escuta sem botão desta entrega é a do desktop. No Android, a tela Falar usa a atividade de reconhecimento do sistema, acionada pelo usuário, e pode depender de internet conforme o serviço instalado. As condições acústicas reais ainda precisam ser conferidas com a voz do usuário.

## Conexão nativa

- Kestrel inicia antes da descoberta UDP. Falha na porta UDP não impede o servidor HTTPS nem QR/PIN.
- HTTP 5188 responde ao ping; HTTPS 5189 transporta os dados; UDP 5187 permite descoberta. A regra local de descoberta foi habilitada para rede privada e sub-rede local.
- QR com candidatos de interfaces, leitura com rotação/tentativas adicionais e alternativa de descoberta dentro do app.
- Validação após pareamento aguarda o teste em andamento; falha de telemetria não invalida uma identidade autenticada.
- Certificado fixado e token persistente continuam obrigatórios nas operações remotas. O celular envia somente comandos permitidos, sem execução de texto livre.
- Registro `UseSkiaSharp()` corrigido no MAUI. A presença no desktop usa atividade autenticada recente e bateria/rede informadas pelo app.

## Visual e assets

Desktop WPF e início MAUI receberam a composição da imagem fornecida: fundo azul escuro, ciano, reator central, cartões de controle e navegação nativa. Fontes locais IBM Plex Sans e Chakra Petch. CPU/RAM, música, e-mails e celular exibem dados disponíveis, sem valores inventados.

O reator principal usa uma ilustração de aparência 3D com transparência e animação nativa de movimento/escala; no desktop reage ao áudio. Não é uma malha 3D com anéis independentes. Animações param quando ocultas ou conforme o modo econômico. A referência orienta o desenho; não se afirma igualdade pixel a pixel em todos os tamanhos.

Assets criados com a skill imagegen e ferramenta nativa `image_gen.imagegen`, usando a referência do usuário:

- `src/Jarvis.Desktop/Assets/Artwork/reactor-cinematic.png`
- `src/Jarvis.Mobile/Resources/Images/reactor_cinematic.png`
- Referência preservada em `docs/design/reference-interface.png`.

Direção do prompt: reconstruir somente o reator da referência, esfera de plasma ciano, três anéis espessos inclinados em titânio/cromo usinado, detalhes âmbar, pedestal circular, sem interface, texto, ambiente ou chão; fundo transparente. O arquivo final preserva o canal alfa.

Prévia WPF: `artifacts/preview/CentralCompleta.png`. Os testes de renderização não iniciam os serviços nem a sessão interativa; campos vazios na prévia não representam telemetria durante execução.

## Wake-on-LAN

Ligar PC envia o pacote usando MAC/broadcast salvos no pareamento. BIOS/UEFI, placa de rede, driver e alimentação precisam permitir despertar. O envio não comprova que o PC ligou. Não houve alteração de BIOS ou teste de desligamento/despertar nesta entrega.

## Validação

- Solução Release: zero erros; permanecem 35 avisos, inclusive resolução de NAudio 2.1.4 para 2.2.0 e compatibilidade declarada de pacotes WPF.
- 90 testes aprovados. Incluem áudio sintetizado por Piper transcrito pelo Whisper base e roteado como abertura da calculadora; QR gerado e decodificado pelo ZXing usado no app; rejeição de ativações fora do início; renderização e controles remotos.
- Inicialização real com `--startup`, criação da janela e encerramento normal aprovados. O app atualizado foi reiniciado e abriu o microfone do headset.
- Servidor real respondeu ao ping e ao HTTPS de pareamento com validação do certificado persistido: esquema Jarvis, PIN de seis dígitos, PNG de QR e dois IPs candidatos.
- APK Release assinado: 28.059.712 bytes; SHA256 `D40A5DF5C2480452F073D9230A9091BFD5791D6FA9E6B1488B816DA5AB45A344`. Copiado também para a pasta mobile do executável.
- Não havia Android físico conectado. Instalação, pareamento entre aparelhos, voz humana no ambiente e despertar do PC ainda não foram validados fisicamente.
- Logs: `artifacts/solution-v2.log`, `artifacts/tests-v2.log`, `artifacts/startup-v2.log`, `artifacts/server-v2.json`; amostra de voz em `artifacts/voice`.

Para recompilar e empacotar: `dotnet build Jarvis.sln -c Release`, `dotnet test tests/Jarvis.Tests/Jarvis.Tests.csproj -c Release` e `./scripts/Empacotar-Jarvis.ps1`.
