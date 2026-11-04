# Central 3D e controle remoto — 17/10/2026

## Desktop

- Central de comando com reator de geometria 3D nativa WPF, materiais, iluminação, órbitas e reação aos estados de voz.
- O reator pausa minimizado, fora da tela, em modo econômico e com animações reduzidas. O dock conserva o renderizador Skia/WPF existente.
- Barra de comando por texto: digite e pressione Enter. O botão Interromper cancela a fala e a rotina de voz atual.
- Navegação com transição breve; novos acessos a Diagnóstico e Minhas preferências.
- Telemetria compartilhada entre a central e o celular. Valores indisponíveis permanecem ausentes; a página não inventa leituras.

## Instalar e parear Android

1. Abra o Jarvis no PC e entre em Celular.
2. Na primeira instalação, abra no telefone o endereço da seção Primeira instalação no Android e instale o APK. O navegador é usado somente para baixar o instalador.
3. Com o app instalado, o QR principal contém `jarvis://parear` e abre o aplicativo nativo. Também é possível fotografar esse QR dentro do app ou digitar IP e PIN.
4. O PIN/token de uso único expira em cinco minutos. O app guarda o token duradouro e a impressão do certificado no SecureStorage.
5. Refaça o pareamento após instalar esta versão se o app antigo não tiver salvo o MAC e o broadcast do PC.

O QR fixa a impressão já fornecida pelo PC. O pareamento manual por PIN estabelece confiança no primeiro contato: a impressão recebida precisa coincidir com o certificado observado nessa conexão. Prefira o QR na primeira conexão. As rotas de pareamento têm limite de tentativas.

## Controles e conexão

- Apps: abrir/fechar com catálogo do PC e confirmação.
- Controles: Spotify, volume, quatro layouts de janelas, focar/minimizar/maximizar, bloquear, suspender e reiniciar. O reinício é solicitado com atraso de 30 segundos e sem fechamento forçado.
- Desempenho: atualização automática aproximadamente a cada três segundos enquanto o app está ativo. Sem conexão, a busca usa intervalo maior.
- Reconexão: descoberta UDP; um novo IP só é aceito depois de verificar o certificado pareado e o token.
- O indicador do desktop representa atividade autenticada recente, com expiração em 20 segundos; não confunde aparelho cadastrado com aparelho online.
- A aba Mais aceita um endereço HTTPS de rede privada/VPN, mantendo o certificado pareado. A VPN precisa estar previamente instalada/configurada nos dois aparelhos; esta entrega não instala infraestrutura de acesso externo nem abre portas no roteador. O firewall da interface VPN também precisa permitir a conexão.
- O monitor de conexão e as animações do celular pausam quando o app vai para segundo plano.

## Ligar o PC

O pareamento salva o MAC e o broadcast do adaptador usado na conexão. Ligar PC envia o pacote Wake-on-LAN ao broadcast dessa rede e ao broadcast geral, com três envios para cada destino. O app acompanha o retorno da conexão; envio não significa que o PC já ligou.

A placa, o driver, a BIOS/UEFI, a alimentação e o estado de energia precisam permitir a ativação. Não é possível ligar um computador sem alimentação. Broadcast pela internet/VPN não é garantido: despertar de fora de casa pode precisar de um equipamento sempre ligado na rede local que encaminhe o pacote. Esse equipamento não foi presumido ou provisionado nesta entrega.

## Rotinas

O construtor salva etapas executáveis no SQLite: abrir um aplicativo, volume, música, layout ou pausa. Há sugestões para Modo trabalho, Modo jogo e Encerrar o dia. Elas precisam ser salvas antes de usar o nome por voz.

O executor valida todas as etapas antes de iniciar, verifica permissões, mostra progresso e permite cancelar as próximas etapas. Não desfaz ações já realizadas. Jogos e permissões que exigem confirmação são executados individualmente, fora das rotinas. Descrições antigas não são tratadas como comandos: crie uma sequência nova no construtor. Gatilhos por horário não foram acrescentados nesta entrega.

## Voz e preferências

- Desktop: “Jarvis, pare” interrompe a síntese. Durante a fala, só a frase de interrupção é aceita pelo reconhecedor; outros comandos não são executados a partir do áudio da própria síntese. O funcionamento acústico depende de microfone, alto-falantes e ambiente.
- Após abrir um app, “agora minimize” ou “agora maximize” usa um contexto de até um minuto. Se não houver uma janela identificável sem ambiguidade, o Jarvis pede seleção manual.
- A memória fica desligada por padrão. Salve explicitamente `aplicativo favorito`, `layout favorito` e `modo favorito`; consulte, corrija ou apague na tela Minhas preferências. Não há extração automática de conversas ou e-mails.
- Exemplos: “abra meu aplicativo favorito”, “meu layout favorito”, “ativar meu modo favorito”, “o que você lembra de mim”.
- Android: reconhecimento pela atividade de voz do sistema. A transcrição é convertida no celular para uma lista fixa de comandos; texto livre não é executado no PC. Há alternativa por digitação.

O reconhecimento do Android pode utilizar serviços externos e internet, conforme o serviço configurado no aparelho: [Android RecognizerIntent](https://developer.android.com/reference/android/speech/RecognizerIntent). A captura de foto segue a configuração de permissões e descoberta de atividade do [MediaPicker do .NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device-media/picker?tabs=android).

## Compilar e entregar

```powershell
dotnet build Jarvis.sln -c Release
dotnet test tests/Jarvis.Tests/Jarvis.Tests.csproj -c Release
./scripts/Empacotar-Jarvis.ps1
```

O empacotamento compila o Android primeiro e depois o desktop. O APK assinado é copiado para `artifacts/entrega/Jarvis.apk` e para a pasta `mobile` do executável, servida em `/jarvis.apk`. O desktop continua no caminho Release usado pelo atalho existente. Não é necessário criar um novo atalho.

As prévias renderizadas das telas ficam em `artifacts/preview`. A suíte verifica navegação, renderização, autenticação HTTPS, telemetria protegida, comandos, formato do pacote Wake-on-LAN, preferências e validação/cancelamento de rotinas. A validação física em telefone, as condições acústicas de interrupção por voz, o despertar do PC e uma conexão externa por VPN dependem dos aparelhos e da rede; não devem ser inferidos apenas da compilação ou dos testes.

## Validação desta entrega

- Solução Release: compilada sem erros. Permanecem avisos de pacotes já usados no projeto, inclusive a resolução de NAudio 2.1.4 para 2.2.0 e a compatibilidade declarada do SkiaSharp WPF.
- Suíte: 77 testes aprovados; após o último ajuste de texto da barra de comando, o teste de renderização foi repetido e aprovado.
- Abertura com `--startup`, criação da janela e fechamento normal: aprovados pelo script de inicialização.
- APK Android Release assinado: 25.890.387 bytes. SHA256: `33846FF47E22A6B770B6BB4AA2C44E13151375798D3F2B25CE72560DC0E06E00`.
- Nenhum dispositivo Android estava conectado ao ADB; não houve teste físico de instalação, pareamento, microfone móvel ou despertar do PC. Não foram acionados bloqueio, suspensão ou reinício do computador durante a validação.
- Logs: `artifacts/solution-build.log`, `artifacts/tests-central.log`, `artifacts/interface-final.log`, `artifacts/startup-final.log`.
