# Melhorias implementadas — Jarvis 2.4.0

As propostas da revisão de 12/11/2026 foram implementadas. Tema único: preto, branco e verde `#00D800`.

| Recurso | Uso e limites |
| --- | --- |
| Preparação da voz | Primeiro uso e Configurações: baixar/reparar Vosk, Whisper tiny, Piper Windows e Faber, progresso/cancelamento, validação e teste de síntese/transcrição/microfone. Primeira instalação pede reabrir a escuta. Voz Faber fixada em `v1.0.0`, compatível com Piper Windows; cartão da licença preservado. |
| Central de comandos | Texto, histórico persistido de voz/texto, editar transcrições, repetir, cancelar e limpar. Até 200 registros locais. |
| Rotinas | Navegação própria, criação/edição/exclusão, prévia de ações, execução por etapas, cancelamento e gatilho por nome. Perfis editáveis para trabalho, estudar, programar e reunião. |
| Agenda de rotinas | Horário e dias persistidos em SQLite, bloqueio atômico de repetição no mesmo minuto, estado ativo e histórico. PC ligado e app aberto; sem recuperar horários perdidos. As permissões são verificadas antes da execução. |
| Atualizador | Release oficial, notas, cancelamento do download, SHA-256 antes de instalar e novamente no helper, confirmação de reinício, cópia anterior e tentativa de restauração se a substituição falhar. Não foi acionado sobre a sessão aberta do usuário. |
| Resumo do dia | Consulta explícita de compromissos locais pendentes de hoje e até cinco assuntos não lidos, fonte/horário e estado desconectado. Sem envio à IA. |
| IA opcional | Consentimento desativado por padrão, chaves DPAPI, modelos configuráveis, identificação do provedor, fallback e 20 tentativas/dia. Saída textual não executa ações. Depende de chave, rede e limites do provedor. |
| Modo discreto | Prévia; economia, animações reduzidas, silêncio de voz/toasts durante o jogo; restaura os ajustes anteriores ao sair normalmente. |

## Simplificações aplicadas

- Removido o botão que apenas afirmava otimizar o sistema e as sugestões estáticas. As sugestões agora dependem das amostras reais de CPU, memória e temperatura.
- Conversa e Diagnóstico têm telas próprias; não duplicam o início. Rotinas ganhou uma rota própria.
- Removidos FPS e atualização periódica do renderizador do dock; diagnóstico mostra o mecanismo e a configuração de animações.
- Mobile: opções de janelas, tela ao vivo e conexão avançada agrupadas em Avançado no menu Mais.
- Seletores e persistência de paletas já removidos. A API antiga de tema continua retornando o tema único para compatibilidade com clientes anteriores; não é um seletor ativo.

## Validação

Testes automatizados cobrem opt-in/fallback/cota de IA, histórico SQL, horários inválidos, execução única de agendamentos, origem oficial e SHA-256 adulterado, restauração do modo discreto e renderização das novas páginas. Um teste real sintetizou uma frase com Piper e a transcreveu com Whisper, sem capturar áudio pessoal. A instalação automática do atualizador exige confirmação no aplicativo; não foi executada sobre a instância em uso.

## Fontes dos recursos de voz

- [Vosk português](https://alphacephei.com/vosk/models)
- [Piper Windows oficial](https://github.com/rhasspy/piper/releases/tag/2023.11.14-2)
- [Faber na revisão compatível](https://huggingface.co/rhasspy/piper-voices/tree/v1.0.0/pt/pt_BR/faber/medium)
- [Whisper tiny](https://huggingface.co/ggerganov/whisper.cpp)

Os identificadores padrão de IA podem ser alterados conforme a disponibilidade dos provedores: [Gemini](https://ai.google.dev/gemini-api/docs/models), [Groq](https://console.groq.com/docs/deprecations), [OpenRouter](https://openrouter.ai/collections/free-models).
