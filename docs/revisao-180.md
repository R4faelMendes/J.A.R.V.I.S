# Jarvis 1.8.0 — revisão solicitada em 20/10/2026

## Alterações

- Preferências removidas da navegação e da fábrica de páginas do PC; integração dessa memória nos comandos de voz removida.
- Registro de ações recentes removido de Segurança. Comandos do assistente e do controle remoto deixam de alimentar esse registro.
- Agenda do PC com aproximadamente 70% da largura para o calendário e células que aproveitam a altura disponível. Dias anteriores a hoje ficam desabilitados, assim como a navegação para meses anteriores. Seletores de data do PC e Android têm hoje como limite mínimo.
- Regra de datas aplicada também no armazenamento local do PC, no cache editável do Android e na API. Compromissos antigos continuam legíveis e podem ser excluídos; criar/alterar para dia passado é recusado. Alteração pendente que ficou no passado informa que precisa ser editada.
- Bateria Android lida pelo broadcast de sistema `ACTION_BATTERY_CHANGED`, sem depender de `BATTERY_STATS`. Antes, a biblioteca MAUI exigia uma permissão ausente e a exceção era tratada como leitura indisponível. Agora painel e cabeçalho de presença usam a mesma leitura; pulso atualiza também os cartões antigos da página inicial.
- Painéis do aparelho no PC e celular refeitos com cartões, ícones, percentual e barra de bateria, armazenamento e rede. Estados sem leitura não inventam dados.
- Descoberta do PC inicia automaticamente na tela de pareamento do Android, com novas tentativas enquanto a página está visível. Utiliza broadcast geral e broadcasts das sub-redes IPv4, em três anúncios, preferindo o endereço que realmente respondeu em vez de interfaces VPN/virtuais anunciadas. Se houver vários PCs, o usuário escolhe.
- Primeiro vínculo exige PIN ou QR. Estar no mesmo Wi-Fi não autoriza qualquer telefone a controlar o PC; reconexões posteriores mantêm token e pinning.
- Download passa a oferecer APK ARM64 menor e universal como alternativa. Android ARM64 precisa de sistema compatível com aplicativos de 64 bits. Ambos mantêm o mesmo identificador, código de versão e certificado para atualizar sobre a instalação existente.
- Download com HEAD, ETag, If-Range e intervalos de bytes para retomada. Página mostra tamanho e SHA-256 dos arquivos servidos. Dados e comandos continuam exclusivamente em HTTPS autenticado; HTTP serve apenas instalação e descoberta/diagnóstico públicos.
- Permissões legadas de acesso ao armazenamento limitadas a Android 9 ou anterior, em vez de Android 12. Entrega sem depuração; notificações e autorização de captura continuam sob controle do Android.

## Wake-on-LAN neste PC

Computador Dell OptiPlex 9020, placa-mãe Dell 0XCR8D, BIOS A02; Ethernet Intel I217-LM, MAC `74:86:7A:FC:88:E4`. Link Ethernet negociado a 100 Mbps. O hardware tem suporte a Wake-on-LAN.

Foram habilitados Magic Packet e gerenciamento de energia da placa. O adaptador Ethernet foi reiniciado para aplicar as opções, sem desligar/suspender o computador. Windows confirmou:

- `WakeOnMagicPacket = Enabled`;
- `WakeOnPattern = Disabled`;
- `AllowComputerToTurnOffDevice = Enabled`;
- `MSPower_DeviceWakeEnable.Enable = True` para esta placa;
- Intel I217-LM presente em `powercfg /devicequery wake_armed`.

Backup do estado anterior em `artifacts/wol-antes-180.json`; leitura posterior em `artifacts/wol-depois-180.json`.

A configuração atual da BIOS não pôde ser lida pela interface WMI disponível. Quando puder reiniciar o PC, entrar com F2 e conferir **Power Management → Wake on LAN = LAN Only** e **Deep Sleep Control = Disabled**. Manter cabo de rede e alimentação conectados. O teste de acordar de suspensão/desligamento não foi executado para preservar o trabalho aberto.

No mesmo Wi-Fi, o aplicativo já envia o pacote ao MAC e broadcast registrados no pareamento. Para acordar de fora de casa, o próprio PC desligado não consegue executar Tailscale nem retransmitir o pacote. É necessário roteador com Wake-on-LAN ou outro aparelho sempre ligado na rede local, acessível pela VPN, que faça essa retransmissão. Não foi inventado um serviço externo ou aberto acesso público ao roteador.

## Confiança da instalação Android

Play Protect pode analisar cada versão instalada fora da loja; o aplicativo não pode garantir a remoção desse aviso. A proteção deve permanecer ativa. Foi preservado o certificado existente para não quebrar atualizações e pareamento. O certificado atual tem assunto `CN=Android Debug`; é uma assinatura de desenvolvimento, não uma certificação da Google. O APK Release não fica depurável por esse motivo.

Uma distribuição pela Google Play exige conta de desenvolvedor, publicação/revisão e planejamento da chave de produção/migração das instalações existentes. Nada foi publicado em conta externa, e não foi substituída silenciosamente a assinatura do APK instalado.

## Validação

- 36 testes aprovados, zero falhas: `tests/Jarvis.Tests/TestResults/revisao-180-final.trx`.
- Testes incluem retomada de download, manifestos dos dois pacotes, isolamento da API HTTPS, descoberta e validação das mensagens, bateria zero/escala inválida, bloqueio de dias passados, agenda, pareamento, protocolo do aparelho, toque remoto e criação/renderização das páginas.
- Prévias verificadas: `artifacts/preview/Agenda.png` e `artifacts/preview/Celular.png`.
- Desktop, APK universal e APK ARM64 Release compilados sem erros. Arquivos de entrega e cópias servidas pelo desktop conferidos por SHA-256. Mesma assinatura da versão anterior, identificador `com.companyname.jarvis.mobile`, versão 1.8.0 e código 13 nos dois APKs.
- Universal: 43.356.576 bytes (41,35 MiB), SHA-256 `8F3D982ED2B3A3379A50301EBB48371878467B88CAB3BBFD43E664BBB9729FF0`.
- ARM64: 22.272.238 bytes (21,24 MiB), SHA-256 `BCDB91287E040CC934F73BB0CA3B6F59EDBF4486B3469A49CE87EB38D4D91443`. Redução de **48,6%** no volume baixado. Não foi medida a taxa de download no telefone; o tempo também depende do Wi-Fi/internet/VPN.
- `Test-ApkAndroid.ps1` confirmou runtime e Skia nativo em todas as arquiteturas do pacote, 22 recursos de ícone e os serviços/permissões. Assinaturas verificadas por `apksigner`; manifesto/ABI e código de versão conferidos pelo `aapt` diretamente nos APKs.
- Jarvis 1.8.0 reaberto e respondendo. Página de instalação e manifesto acessíveis, download ARM64 completo com hash correto pelo endereço Wi-Fi. HEAD HTTP 200 e retomada If-Range HTTP 206 pelo endereço Tailscale, com o intervalo solicitado. Conferência executada no próprio PC, não em um telefone fora de casa.
- Resposta real de descoberta UDP recebida de `192.168.0.64` na rede local, com nome do PC e porta HTTPS 5189.
- Leitura real de bateria e descoberta executando no Android ainda precisam ser confirmadas no telefone atualizado; não há aparelho/emulador conectado para esse teste.

## Referências

- [Dell OptiPlex 9020 — BIOS e energia](https://www.dell.com/support/manuals/en-us/optiplex-9020-desktop/opt9020sffom-v2/system-setup-options?guid=guid-2cff7608-3f85-4cc2-8614-e84a203c395c&lang=en-us)
- [Intel — habilitar Wake-on-LAN](https://www.intel.com/content/www/us/en/support/articles/000059062.html)
- [Google — funcionamento do Play Protect](https://support.google.com/googleplay/answer/2812853?hl=pt-BR)
