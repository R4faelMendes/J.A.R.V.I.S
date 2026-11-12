# Próximos passos do Jarvis

Revisão do código de desktop, voz, rotinas e navegação em 12/11/2026, após a adoção do tema único preto/branco/verde `#00D800`. As propostas abaixo não foram implementadas nesta alteração.

## Implementar primeiro

| Prioridade | Proposta | Base existente e resultado esperado |
| --- | --- | --- |
| 1 | Assistente de primeiro uso e reparo da voz | `DiagnosticoService` verifica microfone e arquivos; `ModelDownloader` já baixa Vosk. Reunir download de Vosk/Whisper/Piper, progresso, validação e teste de voz numa sequência única. O usuário deve conseguir sair de uma instalação sem modelos para um primeiro comando funcionando. |
| 2 | Central de comandos digitados e histórico | `NucleoPage` prioriza o microfone; a rota Conversa abre a mesma página. Acrescentar entrada de texto, sugestões de comandos reais, transcrição corrigível, resultado da ação e botão para repetir. Isso permite usar o Jarvis sem áudio e entender falhas de reconhecimento. |
| 3 | Rotinas acessíveis, depois gatilhos | `RotinasCentralPage` já contém editor, execução por etapas e cancelamento; `RotinaService` valida permissões. A fábrica atual não encaminha uma página para esse editor e a sidebar não o apresenta. Conectar a navegação, conferir a ativação por voz e depois acrescentar horários/dias e prévia das ações antes de executar. |
| 4 | Atualização dentro do aplicativo | A distribuição já usa GitHub Releases. Exibir versão atual, versão disponível, notas e download verificado por SHA-256, com instalação/reinício solicitado pelo usuário. Evita baixar manualmente cada release e facilita manter PC e celular compatíveis. |
| 5 | Resumo útil do dia | Agenda local, e-mails e integrações já existem. Montar uma ação explícita “resumo do dia” com compromissos próximos, mensagens selecionadas e pendências, indicando fonte e horário de atualização. Contas desconectadas devem produzir estados vazios. |

## Segunda etapa

- **IA opcional para perguntas:** `Jarvis.Ai` já contém provedores e fallback, mas `MotorVoz.ExecutarComandoAsync` encaminha comandos não reconhecidos a `AssistenteLocal.Responder`. Integrar um modo de conversa com consentimento, limites e indicação do provedor. Manter execução de ações na lista validada de comandos; texto do modelo não deve virar comando de sistema livre.
- **Áreas de trabalho por contexto:** aproveitar layouts e rotinas para “estudar”, “programar” e “reunião”, com seleção de aplicativos e uma prévia. Há contexto de janela de um minuto no `MotorVoz`; expandi-lo requer distinguir o aplicativo escolhido de janelas parecidas.
- **Modo discreto durante jogos:** concentrar modo econômico, redução de animações, silêncio e notificações num estado claro, mostrando quais mudanças serão feitas e permitindo restaurá-las. O projeto já tem modo jogo e instrumentação; a melhoria seria tornar o comportamento previsível.

## Remover ou simplificar

1. **Botão “Otimizar sistema” atual.** Em `Pages.cs`, o clique apenas mostra “Otimização concluída: memória liberada.” Não há operação de otimização nesse handler. Remover o botão ou substituí-lo por diagnóstico com dados reais e ações explícitas. As sugestões fixas “Nenhum processo em sobrecarga” e “Temperaturas estáveis” também devem depender da telemetria.
2. **Rotas que duplicam a tela inicial.** `FabricaPaginas.Criar` retorna `NucleoPage` para Núcleo, Conversa e Diagnóstico. Dar propósito real às duas últimas, ou retirar seus caminhos até existirem. Preservar os serviços de diagnóstico, que já fazem verificações úteis.
3. **FPS e nome do renderizador no uso normal.** `MainWindow.xaml.cs` atualiza o texto do dock com renderizador/FPS. Colocar essa informação em diagnóstico avançado; no dock, priorizar microfone, conexão e estado da ação.
4. **Opções técnicas no primeiro acesso do mobile.** O menu Mais mistura pareamento, conexão por internet, janelas e tela ao vivo. Manter pareamento e comandos principais em destaque; reunir espelhamento e rede avançada em uma área própria. São recursos de maior custo de manutenção, não necessariamente recursos sem utilidade.
5. **Infraestrutura de paletas múltiplas.** Os seletores, página de temas, persistência de escolhas e consulta periódica do tema pelo celular foram removidos nesta alteração. A API antiga aceita IDs conhecidos e retorna sempre `jarvis`, para compatibilidade. Quando clientes antigos deixarem de ser suportados, retirar também os endpoints e os eventos de troca mantidos como compatibilidade.

Eu priorizaria primeiro uso da voz, comandos digitados e acesso às rotinas. Essas mudanças aproveitam serviços presentes e melhoram o uso diário antes de ampliar a quantidade de integrações.
