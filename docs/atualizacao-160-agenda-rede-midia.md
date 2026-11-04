# Atualização 1.6.0 — agenda, janelas, acesso externo e Spotify

## Entregue no código

- Posições de janela independentes da ordem da lista. Selecione esquerda/direita ou qualquer canto da grade 2×2, inclusive com uma única janela. Posições repetidas são rejeitadas antes de mover janelas. Disponível no PC e Android.
- Rotinas removidas da navegação e da execução automática por nome. Os registros antigos são preservados no banco, sem executar ou excluir seus dados.
- Agenda local com calendário mensal no PC e celular: criar, editar, excluir, notas, início/fim e lembrete. Funciona sem Google ou chave de API.
- Agenda do PC em SQLite; cópia e edições pendentes no armazenamento privado do Android, separadas por computador pareado. Sincronização ao abrir a agenda e a cada minuto enquanto o app está ativo e conectado. Conflitos de edição exigem escolha entre usar a versão do PC ou preservar a edição como cópia.
- AlarmManager agenda lembretes no próprio Android. Alertas já sincronizados independem do PC e da internet. Reagendamento após reiniciar/atualizar o app ou mudar hora/fuso. Exclusões e mudanças sincronizadas cancelam/reagendam os alertas. Permissão de notificação exigida no Android 13+; alarmes exatos dependem da permissão especial no Android 12+.
- Spotify: capa, título e artista no início e em Controles. Ação “Controle na notificação” inicia serviço de mídia remota com anterior/tocar/pausar/próxima e capa, inclusive fora da tela do app. Há botão para encerrar o controle. Isso controla a sessão do Spotify no PC; não transfere o áudio ao telefone.
- Tela cheia na transmissão: botão Tela cheia, orientação horizontal com sensor, barras ocultas, tela mantida ligada. Sair da tela cheia ou Voltar restaura a orientação anterior. Entrar/sair não reinicia a transmissão.
- Endereço HTTPS da rede privada salvo separadamente do endereço local, para reconectar ao mesmo PC pela VPN. Certificado fixado e token do pareamento continuam obrigatórios.

## Preparação do Tailscale

Tailscale 1.104.1 instalado no PC pelo pacote oficial, com hash verificado pelo winget. Estado atual: NeedsLogin. O dono informou que ainda não concluiu o login nos dois aparelhos; a conectividade fora de casa não foi validada.

Regra `Jarvis-Tailscale-5189`: TCP 5189 somente para Jarvis.Desktop.exe, origem e destino em 100.64.0.0/10. Nenhuma porta foi encaminhada no roteador. Script reproduzível: `scripts/Preparar-Tailscale.ps1`.

1. Entrar no Tailscale no PC e no Android com a mesma conta, ativando a VPN no celular.
2. Parear Jarvis localmente uma vez.
3. No PC, Dispositivos → Copiar endereço externo seguro.
4. No celular, Mais → Conectar por rede privada / VPN → colar endereço e verificar.
5. Para testar, desligar o Wi-Fi do celular e tentar pelos dados móveis com Tailscale ativo. PC precisa permanecer ligado e com Jarvis aberto.

Referência oficial: https://tailscale.com/docs/how-to/connect-to-devices

## Lembretes offline e limitações

- O compromisso precisa existir no celular para avisar offline. Uma alteração feita só no PC quando não há conexão chega após a próxima sincronização.
- Ativar notificações e alarmes na Agenda do Android. Sem permissão de horário preciso, o Android pode atrasar o alerta. Economia de bateria do fabricante e “Forçar parada” podem impedir execução; o app não promete contornar restrições do sistema.
- Nada depende de agendamento nesta conversa ou de uma automação do Codex.
- Ainda sem telefone físico conectado por ADB. Teste de alertas com tela apagada, reinício, widget de mídia e rotação/tela cheia deve ser realizado no aparelho do dono.
- Wake-on-LAN pela internet requer outro dispositivo ligado na rede de casa; instalar Tailscale no PC desligado não o transforma em receptor de wake remoto.

## Validação

- Desktop Release: aprovado, sem erros; 12 avisos de dependências preexistentes.
- Android Release 1.6.0, código 9: aprovado, zero erros e zero avisos na compilação final.
- APK final: `artifacts/entrega/Jarvis-1.6.0.apk`; cópias `Jarvis.apk` e `mobile/Jarvis.apk` do Desktop idênticas.
- SHA-256: `659F975C712A6F01E7893ADC05E1EF1B60FF8DE70702292FD6FD727A7D742F97`.
- Validador do APK aprovado: SkiaSharp nativo nas quatro arquiteturas, 22 recursos de ícone, permissões de notificações/alarmes, receivers da agenda e serviço de mídia não exportado com tipo mediaPlayback.
- Testes direcionados finais: 19 aprovados (agenda, persistência, conflitos, posições de grade, navegação e voz).
- Suíte geral: 117 aprovados de 118, com uma falha intermitente do teste real de voz: o Whisper ouviu “Service” em vez de “Jarvis”. Esse teste e a movimentação real de janelas passaram na execução isolada (2 aprovados). A suíte não deve ser descrita como totalmente estável.
- Outra variação real da transcrição (“abri-la, calculadora”) recebeu tratamento no roteador e teste de regressão.
- Prévia do calendário renderizada e inspecionada em `artifacts/preview/Agenda.png`.
- O serviço de mídia mantém reconexão e sincronização enquanto seu controle está ativo na notificação; ao encerrá-lo com o app em segundo plano, o monitor de conexão também é parado.

## Pendências que exigem o aparelho ou a conta do dono

1. Concluir o login na mesma conta Tailscale no PC e Android, ativar a VPN no celular e salvar o endereço privado no Jarvis. Estado observado no PC: NeedsLogin. Sem essa etapa não foi possível comprovar controle fora do Wi-Fi.
2. Instalar o APK final no telefone; conceder notificações e a permissão de alarmes precisos na Agenda. Testar um compromisso com tela apagada e sem rede; depois testar reinício.
3. Ativar “Controle na notificação” em Início ou Controles, com Spotify tocando no PC; verificar capa e anterior/pausa/próxima.
4. Abrir Tela ao vivo, iniciar transmissão e tocar em Tela cheia; verificar giro horizontal, botão Voltar e retorno à orientação anterior.
5. Instabilidade residual da palavra de ativação no teste de voz exige uma avaliação dedicada com áudio real do dono. Não foi introduzida uma correspondência ampla para “Service”, pois isso poderia causar ativações indevidas.


Desktop reaberto ao concluir: janela respondendo e servidor /ping ativo na porta 5188. Tailscale continua aguardando login (NeedsLogin).
