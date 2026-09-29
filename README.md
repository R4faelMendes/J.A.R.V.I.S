# JARVIS 2.0 — Assistente pessoal 100% local e gratuito (Windows 10, CPU)

> Pasta real: `C:\Users\Administrator\Desktop\J.A.R.V.I.S - 2.0` (Desktop clássico, **sem** OneDrive — verificado).
> Custo zero, tudo local (áudio, tela e arquivos nunca saem do PC). Internet só para baixar ferramentas e pesquisar.

## Hardware registrado (verificado via PowerShell/WMI)
- **SO:** Windows 10 Pro 10.0.19045, 64 bits
- **CPU:** Intel i5-4570 @ 3.20 GHz — **4 núcleos / 4 threads** (`cpu_threads=4`)
- **RAM:** ~17,9 GiB | **GPU:** 2x 1 GB (inúteis p/ IA → tudo na CPU)
- **Disco C:** ~267 GB livres | **Áudio:** USB + Realtek + headset (39 dispositivos via sounddevice)
- **Rede local (Fase 8):** IP `192.168.0.64`, porta `8765`, PIN em `config.yaml → servidor.pin`

## Instalação limpa (PowerShell)
```powershell
cd "C:\Users\Administrator\Desktop\J.A.R.V.I.S - 2.0"
winget install Python.Python.3.12 Git.Git Ollama.Ollama   # UAC pode pedir "Sim"
py -3.12 -m venv .venv
.\.venv\Scripts\pip.exe install -r requirements.txt       # versões congeladas
ollama pull qwen2.5:3b
.\.venv\Scripts\python.exe scripts/check_hardware.py
.\.venv\Scripts\python.exe -m pytest tests/               # 50 testes
```
Se o PowerShell bloquear o `Activate.ps1`: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` — ou chame `.\.venv\Scripts\python.exe` direto.

## Como iniciar
- **Duplo clique em `start.bat`**: garante Ollama, sobe o servidor (`0.0.0.0:8765`, HTTPS se `models/cert.pem` existir) e abre o HUD no Edge sem bordas. No Firewall, permita **só "Redes privadas"**.
- `start_silencioso.vbs`: igual, sem janela. Início com Windows: `iniciar_com_windows: true` no config + `scripts/startup.py`.
- **Texto:** `scripts/jarvis.py --texto "que horas são"` | conversa: `scripts/jarvis.py` | voz: `scripts/jarvis.py --voz`.
- **Celular (mesmo Wi-Fi):** `scripts/mostrar_qr.py` → escaneie `models/qr-celular.png`, digite o PIN, aceite o certificado.

## Comandos de exemplo
"Jarvis, que horas são?" · "que dia é hoje?" · "abrir a calculadora" · "volume para 30" · "pausar a música" · "me lembra em 5 minutos" · "uso de cpu" · "pesquise filme Duna" · "pare" · "confirmo" (confirma ação pendente) · atalho **Ctrl+Alt+J**.

## Medições reais (i5-4570, CPU)
| Etapa | Número | Meta |
|---|---|---|
| LLM 1º token (aquecido) | 0,6–0,9 s | < 2 s ✅ |
| Roteador → 1º áudio | 0,26 s | < 1 s ✅ |
| LLM → 1º áudio | 1,30 s | 2–3 s ✅ |
| STT (15 s em 1,23 s) | 1,23 s | < 1,5 s ✅ |
| TTS 1ª frase (faber-medium) | 0,80 s | < 1 s ✅ |
| qwen2.5:3b 5,8 tok/s vs 1.5b 10,1 tok/s | principal 3b, fallback 1.5b | — |
| Servidor fim-a-fim (HTTP+voz) | 0,48 s | — |
| Wake: silêncio 0,000 (sem falso +); voz sintética 0,105 | teste 20x ao vivo pendente | limiar 0,5 |

## Fases — todas concluídas
- [x] **F0** Preparação (estrutura + hardware) · [x] **F1** Cérebro Ollama qwen2.5:3b · [x] **F2** Piper faber-medium + servidor persistente
- [x] **F3** Whisper base + webrtcvad (sem torch) · [x] **F4** hey_jarvis onnx + Ctrl+Alt+J · [x] **F5** Pipeline (roteador, barge-in, eco)
- [x] **F6** 9 skills + executor com confirmação e `logs/exec.log`
- [x] **F7** FastAPI+WS + HUD Holográfico Cinematográfico (Three.js 3D + Canvas 2D + Web Audio FX + fontes locais)
- [x] **F8** PWA + PIN/QR + HTTPS autoassinado (só rede local) · [x] **F9** start.bat/VBS, startup, freeze

## Pendências de hardware (não de código)
1. **Microfone capta silêncio** (rms ~0,01): confira mute físico, USB, Som → Entrada (barra azul deve mexer), ganho 80–100, Privacidade → Microfone. Diagnóstico: `scripts/check_mic.py`. Validação ao vivo (frase + 20x wake) fica para depois do ajuste.
2. **Ocioso:** medir CPU em repouso quando o mic funcionar (meta < 5%).

## Solução de problemas
| Sintoma | Causa e correção |
|---|---|
| `python não foi encontrado` | Instale Python 3.12 via winget (não 3.13+) |
| PowerShell bloqueia `Activate.ps1` | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| Microfone não abre | Privacidade → Microfone → permitir apps da área de trabalho |
| Mic abre mas silêncio | Mute/ganho/USB (ver acima) |
| Porta 8765 ocupada | `Get-Process python \| Stop-Process` ou troque `servidor.porta` |
| Ollama parado | `ollama serve` (o start.bat já tenta sozinho) |
| Voz Piper não encontrada | `models/piper-voices/*.onnx` (+ `.json`); `tts.voz` no config |
| `duckduckgo-search` renomeado | Migrado para `ddgs` 9.16.0 (import com fallback) |
| Acentos `?` no terminal | Normal no cp850; logs/arquivos são UTF-8 (`Get-Content -Encoding utf8`) |
| Firewall | Permitir **só redes privadas**; nunca expor à internet nem abrir portas no roteador |
| `DeprecationWarning` sounddevice/numpy | Irrelevante (dentro da lib); avaliado no próximo freeze |
| `on_event` FastAPI deprecated | Irrelevante (aviso da lib; migrar p/ lifespan no futuro) |

## Segurança
Confirmação por voz (`confirmo`) + botão ✓ na UI para apagar, executar código e similares; lista de proibidos em `config.yaml → seguranca`; tudo logado em `logs/exec.log`; lixeira em vez de delete; "pare" interrompe tudo.

## Melhorias futuras (gratuitas)
Processo Piper já persistente; próximos: Whisper "small" se sobrar CPU; silero-vad onnx sem torch; tool calling mais agressivo no roteador; ditado pelo mic do navegador via WebSocket; lifespan no FastAPI; wake com modelo pt-BR próprio; backup do workspace.
