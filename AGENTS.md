# AGENTS.md — JARVIS (C#/.NET)

## Decisões
- .NET 8 LTS, WPF + MVVM (`CommunityToolkit.Mvvm`), SkiaSharp (reator) com fallback DrawingContext.
- UI só fala com interfaces de `Jarvis.Core` (IVoiceService, IAppController, ...). Sem Win32/Google/IA na UI.
- Fontes locais em `src/Jarvis.Desktop/Assets/Fonts` (Chakra Petch + IBM Plex Sans, licença livre).
- Tokens em `Jarvis.Core/Theme.cs` (protótipo vence divergências).
- Voz: NAudio 2.1.4 + Vosk 0.3.38 (`vosk-model-small-pt-0.3`, 31 MB) + Whisper.net 1.9.1 (`ggml-tiny.bin`, 77 MB) + Piper 1.2.0 local.
- Namespace de sistema: `Jarvis.Sistema` (nunca `Jarvis.System`: colide com `System.Windows` no XAML).
- Servidor: Kestrel+SignalR embutido (`HospedeJarvis.Criar`), porta 8766, lista fixa de comandos, token duradouro, pinning de impressão. Celular nunca executa texto livre.
- Mobile: MAUI Android+Windows; `Directory.Build.props` fixa AndroidSdkDirectory/JavaSdkDirectory.
- Segredos: DPAPI (`CofreDpapi`, `DpapiDataStore`); SQLite em `%LocalAppData%/Jarvis/jarvis.db`.
- Design e Interatividade: Interface reproduzindo 100% o protótipo web (rotas `/`, `/conversa`, `/apps`, `/desempenho`, `/agenda`, `/emails`, `/rotinas`, `/celular`, `/seguranca`, `/configuracoes`), com cartões clicáveis, navegação fluida, Google OAuth completo para Gmail/Agenda, sintetização de voz sob demanda, gerenciador de apps e janelas e seletor de cores do reator.

## Comandos
```powershell
cd "C:\Users\Administrator\Desktop\Jarvis"
dotnet build Jarvis.sln
dotnet test Jarvis.sln
```

## Estado (todas as fases, sem pausas)
- Fase 1: shell + 10 páginas completas com dados e layout idênticos ao protótipo web + reator Skia/WPF + modo econômico + FPS no dock.
- Fase 2: NAudio WASAPI + VAD + Vosk wake word + Whisper tiny sob demanda + Piper quente com cache + Ctrl+Espaço + sintetização de voz em botões "Ouvir de novo" e "Ler em voz alta".
- Fase 3: apps instalados/Steam/Menu Iniciar, gerenciamento de janelas + 4 layouts de tela, mídia/volume/bloqueio, confirmação para jogos.
- Fase 4: CPU/RAM/GPU/disco/temperatura em tempo real + tabela de processos mais pesados + CPU do próprio processo na página Desempenho.
- Fase 5: roteador local + Gemini/Groq/OpenRouter com fallback + cota diária (20 testes).
- Fase 6: OAuth desktop + DPAPI + Gmail/Agenda reais com sincronização, visualização em 2 colunas, resumos do Jarvis, criação de eventos com confirmação e resposta de e-mail por voz.
- Fase 7: abas Chrome/Edge via DevTools (porta 9222) e abertura de URLs.
- Fase 8: SQLite (rotinas com toggle e teste passo a passo, log de auditoria), permissões com confirmação de risco, bandeja e mini overlay.
- Fase 9: servidor embutido + SignalR + QR code + token + certificado autoassinado (41 testes aprovados).
- Fase 10: MAUI com 5 abas + Mais, pareamento, Wake-on-LAN, pinning.
- Fase 11: ocioso 0% CPU / 119 MB RAM (< 250 MB ✅); abertura < 12 s.
- Correcoes 03/10 (dono): sidebar com icones em largura fixa; Assets/jarvis.ico como icone do app e do atalho; MapaProcesso no AppController (calc->CalculatorApp) + filtro anti-lixo do registro + Take(60); E-mails/Agenda com estado vazio sem login (sem mocks); login Google por client_id/secret (DPAPI) + SincronizarAsync real; rascunho de resposta vazio; Celular com QR/codigo reais de /api/parear/iniciar.
- Correcoes 03/10 noite (dono): login de e-mail por senha de app (IMAP/SMTP MailKit, DPAPI) sem Cloud Console; sync real + rascunho vazio; TLS do servidor: sem EphemeralKeySet, sem extensoes, PFX persistido; loopback via MapToIPv4; QR/codigo reais; APK com scan por foto (ZXing camera view inexistente no .NET 8); APK Release sem AOT (mono AOT crasha no SkiaSharp nesta maquina).
- Correcoes finais (Google, Agenda, E-mails e Celular):
  * Google Login unificado com seletor de arquivo JSON (`client_secret.json`), credenciais manuais e Senha de App do Gmail.
  * E-mails e Agenda com estado vazio estrito quando desconectados (sem mocks, zero dados fictícios).
  * Ao conectar, visual idêntico 100% ao protótipo web (duas colunas, timeline de eventos com detalhes, e-mails com resumo do Jarvis, avatar circular com iniciais, tags, Meet e ações).
  * Correção de codificação UTF-8 em todos os ícones e botões.
  * Pareamento do celular corrigido: detecção real de IP local (`AparelhosIp`), emissão e validação do PIN de 6 dígitos no servidor (`/api/parear/confirmar-codigo`) e pareamento direto pelo app mobile.
- Refacao conexoes 03/10: servidor em 2 portas (HTTP 5188 so /ping + HTTPS 5189 dados); AparelhosIp.Candidatos (Ethernet com gateway primeiro, virtuais por ultimo); UDP discovery 5187; RedeService (firewall LocalSubnet/privado, perfil de rede, checklist WoL); Celular com checklist + QR/codigo reais + /health real; mobile com scan por foto, discovery, tela de diagnostico e portas novas; Google em 3 niveis (IGoogleGateway: OAuth/Simples/Offline) + assistente de 6 passos + ErroGoogle (tabela A4); SidebarControl fiel ao prototipo (trilho 72px, Lucide 19px, barra ativa 3x20, tooltip) + docs/sidebar-spec.md; APK rebuildado.

## Portes de Jarvis_test (10/10/2026)
- Novo projeto Jarvis.Info: WeatherService (Open-Meteo, sem chave) e NewsService (RSS Google News).
- Jarvis.Voice: TtsTextSanitizer (horas, datas, porcento, reais, links, emojis) aplicado em PiperTts.Falar; WakeWordMatcher tolerante (edit distance <= 1) no WakeWordListener.
- Nao portado (avaliar depois): Brain/MemoryExtractor, DigestService, AnalystEngine, CommandRouter, SelfTest.
- Atalho atualizado: Desktop Jarvis.lnk -> Release atual, com icone jarvis.ico.
