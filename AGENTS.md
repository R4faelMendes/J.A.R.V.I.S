# JARVIS — assistente pessoal para Windows + celular

App nativo em C#/.NET 8: WPF no desktop, .NET MAUI no celular.
Design system em `src/design/` (tokens + fontes embutidas, sem CDN).

## Primeira execução neste computador

Duplo clique em **`install/Jarvis-Setup.cmd`**. Ele prepara o .NET 8
(só para o usuário, sem administrador), baixa os modelos de voz locais
(Piper pt-BR + Vosk pt-BR) para `%LOCALAPPDATA%\Jarvis\modelos`,
verifica o certificado HTTPS e abre o app.

Detalhes e opções (`-SemApp`, `-SemModelos`, `-Forcar`): `install/bootstrap.ps1`.

## Comandos

O `dotnet` usado aqui é o portátil de `%USERPROFILE%\.dotnet`:

```powershell
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1

dotnet build Jarvis.sln -c Release   # compila tudo
dotnet test Jarvis.sln               # testes (xUnit)
dotnet run --project src/Jarvis.Desktop -c Release
```

## Estrutura

```
install/               Jarvis-Setup.cmd + bootstrap.ps1 (primeira execução)
src/design/            tokens.css, fontes, reator/componentes/telas (referência visual)
src/Jarvis.Core        abstrações (IServicoVoz, IControladorApps, …) + tokens C#
src/Jarvis.Voice       captação NAudio, VAD, palavra de ativação, STT, TTS Piper
src/Jarvis.Ai          provedores gratuitos + roteador de intenções
src/Jarvis.Google      Gmail + Agenda (OAuth)
src/Jarvis.System      apps, janelas Win32, processos, desempenho, navegador
src/Jarvis.Server      API local + SignalR, pareamento QR, Wake-on-LAN
src/Jarvis.Desktop     WPF: shell, reator, bandeja, overlay (fase 1 pronta)
src/Jarvis.Mobile      MAUI Android: telas da seção 7 do plano
tests/Jarvis.Tests     xUnit, foco no interpretador de comandos
```

## Decisões

- UI nunca fala com Win32/Google/IA direto: chama `Jarvis.Core.Abstracoes`.
- WPF com `VisaoReator` vetorial próprio (sem Viewport3D); máx. 30 fps;
  pausa com a janela oculta; Modo econômico vira estático.
- Vidro sem blur real (cor translúcida + sombra interna ciano 10%).
- Segredos em DPAPI; celular só usa lista fixa de comandos + HTTPS com pinning.
- Nenhum e-mail/evento sem confirmação; limite diário de IA visível.

## Plano de fases

1. Base visual ✅ (shell, tokens, reator, solução compila com 0 erros, testes passam;
   Mobile fora da sln até a fase 10 — falta o SDK Android API 34 neste PC)
2. Voz (parcial: CaptadorMicrofone, DetectorPalavraAtivacao, SintetizadorPiper
   como estrutura; faltam VAD, integração Vosk/Whisper/Piper, calibração —
   sem teste de microfone a pedido do dono)
2. Voz · 3. Controle do PC · 4. Desempenho · 5. IA · 6. Google · 7. Navegador
8. Rotinas/Segurança/Config · 9. Servidor/pareamento · 10. Mobile · 11. Polimento
