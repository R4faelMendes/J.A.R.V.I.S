# Atualização do Jarvis — 15/10/2026

## Abrir a versão corrigida

Use o atalho Jarvis da área de trabalho. O executável agora é `src/Jarvis.Desktop/bin/Release/net8.0-windows10.0.19041.0/Jarvis.Desktop.exe`.

## Google e e-mails

O fluxo escolhido pelo dono é OAuth oficial no navegador, com Gmail API. O cliente desktop antigo, quando encontrado nos arquivos DPAPI do próprio Jarvis, é reaproveitado. Abra **E-mails → Entrar com Google** e autorize a consulta. O aplicativo pede somente leitura; não envia nem apaga e-mails. O cadastro da aplicação é feito uma vez pelo desenvolvedor, não a cada login.

Se o cliente antigo tiver sido revogado ou desativado, use **Configuração do desenvolvedor** para importar um novo JSON do tipo Aplicativo para computador. Habilite Gmail API e adicione sua conta como usuário de teste na tela de consentimento. Em modo de teste a autorização pode expirar após sete dias. A publicação/verificação é independente desta implementação.

Ao iniciar, o Jarvis consulta a caixa de entrada e anuncia a quantidade total, as não lidas e os cinco remetentes/assuntos mais recentes. A página exibe até vinte mensagens com prévias. A opção pode ser desligada na tela E-mails. Mensagens e áudios de e-mail não são enviados a provedores de IA nem armazenados no cache de fala.

## Voz e música

- “Jarvis, pausar a música”, “continuar a música”, “próxima música”, “música anterior”.
- “Jarvis, que música está tocando?”
- “Jarvis, leia meus e-mails”.
- “Jarvis, abrir calculadora”.
- Ctrl + Espaço cancela a fala e abre uma janela de escuta de sete segundos.
- Fechar aplicativos respeita as permissões e pede confirmação quando necessário. “Jarvis, confirmar” vale por trinta segundos; “Jarvis, cancelar” descarta o pedido.

O Spotify/Lite precisa publicar uma sessão de mídia no Windows; normalmente basta iniciar uma música uma vez. Não é necessário Spotify Web API. O Jarvis não controla outro player quando não encontra a sessão do Spotify.

A voz original é **Faber medium** (63.201.294 bytes). A alternativa **Cadu medium** (62.950.044 bytes) está instalada. Ambas usam Piper local e são selecionadas em Configurações. A fluidez percebida é subjetiva; compare com “Testar voz”. O motor gera áudio sob demanda e não mantém dois modelos carregados. A velocidade usa o parâmetro real `--length_scale`, com formato numérico independente do idioma do Windows.

## Correções e validação

A falha de pilha vinha de `NucleoPage.AtualizarTelemetria` chamando a si mesma dentro da própria atualização. A tela inicial foi substituída por um fluxo assíncrono sem recursão. A inicialização de voz ocorre depois da janela e tolera dispositivos de áudio ainda indisponíveis no logon. Uma segunda instância não inicializa outro conjunto de serviços.

Os testes cobrem comandos, OAuth com PKCE/state, renovação, DPAPI, renderização de telas e síntese Piper real. A autorização na conta Google precisa ser concluída pelo usuário. O teste de sessão local confirmou Spotify disponível. A inspeção automatizada por controle de janelas ficou indisponível por erro 1058 no serviço Windows do ambiente de ferramentas; a renderização foi conferida em PNG.

Fontes oficiais:
- https://developers.google.com/identity/protocols/oauth2/native-app
- https://developers.google.com/workspace/gmail/api/reference/rest/v1/users.messages/list
- https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager
- https://huggingface.co/rhasspy/piper-voices/tree/main/pt/pt_BR/cadu/medium

O cartão/licença da voz está em `docs/voices/cadu-MODEL_CARD.txt`. SHA256: `765F0809A6EA9035D4A6D0D008DBF8876E68B2DD32029312672FA8F405BDB535`.

## Validação final — 16/10/2026

- Compilação Release da solução concluída sem erros. A configuração Android sem AOT, já exigida no AGENTS.md, agora está persistida no projeto.
- 57 testes aprovados na versão Release.
- Executável testado com `--startup`: abriu a janela, ativou o microfone e encerrou normalmente. Não foi feito reboot do Windows.
- Corrigida também a espera síncrona do servidor local no encerramento, que prendia a thread da interface.
- Atalho da área de trabalho e entrada `JarvisAssistente` do Windows atualizados para o novo executável. Cópias anteriores em `artifacts/backup-inicializacao`.
- Cadastro OAuth desktop anterior migrado diretamente entre arquivos protegidos por DPAPI. A autorização da conta continua dependendo do login do usuário.
- A memória observada em uma abertura foi de aproximadamente 287 MB (conjunto de trabalho). A meta antiga de menos de 250 MB não está comprovada nesta versão.
- Para repetir a validação de abertura/fechamento, feche o Jarvis e execute `scripts/Test-DesktopStartup.ps1`.
