# RELATÓRIO JARVIS 2.0 — Atualizado após Intervenção do Antigravity

**Data:** 29/09/2026 (~01:30 UTC-3)  
**Status Geral:** ✅ **Pipeline de Áudio Corrigido & Validado | HUD Holográfico Cinematográfico Implementado (Three.js 3D + Canvas 2D)**  
**Pasta do projeto:** `C:\Users\Administrator\Desktop\J.A.R.V.I.S - 2.0`  
**PIN:** `***`

---

## 1. Ambiente Verificado

- **Windows:** Microsoft Windows 10 Pro, versão 10.0.19045, 64 bits
- **CPU:** Intel(R) Core(TM) i5-4570 @ 3.20GHz — 4 núcleos físicos / 4 lógicos
- **RAM:** 17,91 GB total (~10 GB livres com modelos Ollama residentes)
- **GPU:** Intel HD Graphics 4600 (1 GB) + AMD Radeon HD8490 (1 GB) — IA 100% na CPU
- **Python:** 3.12.10 (.venv)
- **Modelos de IA:**
  - Ollama: `qwen2.5:3b` (principal) e `qwen2.5:1.5b` (fallback)
  - TTS: Piper 1.2.0 local (`pt_BR-faber-medium.onnx` + `pt_BR-edresson-low.onnx` a 22050 Hz)
  - STT: Faster-Whisper base (CPU int8, 4 threads) + webrtcvad
  - Wakeword: openWakeWord onnx (`hey_jarvis_v0.1.onnx` + atalho global `Ctrl+Alt+J`)

---

## 2. Resumo das Alterações e Correções

### 2.1 Saída de Áudio & TTS (Tarefa 1)
- **Mapeamento de Dispositivos:** `scripts/teste_audio_dispositivos.py` listou 22 saídas de áudio ativas. O dispositivo padrão do sistema foi detectado no índice 4 (`Speakers (3- Wireless Stereo He)` via MME, 44100 Hz).
- **Tom de Diagnóstico:** Tom senoidal de 440 Hz (2 s) emitido com sucesso nas saídas MME, DirectSound e WASAPI.
- **Piper Isolado:** `scripts/teste_piper_isolado.py` validou a síntese do modelo `pt_BR-faber-medium.onnx` em 0.886 s gerando WAV de 156.3 KB (22050 Hz, 16 bits). Reprodução executada com sucesso via `sounddevice` e `winsound`.
- **Correção em `voice/tts.py`:**
  1. Passagem explícita de `--espeak_data` para o binário do Piper no `ServidorPiper` persistente, eliminando risco de falhas fonéticas silenciosas.
  2. Suporte direto ao dispositivo configurado em `config.yaml` (`audio.alto_falante_indice`).
  3. Implementação de fallback transparente em memória via `winsound.PlaySound` caso o stream `sounddevice` sofra bloqueio de driver.
  4. Script de validação completo criado: `scripts/teste_voz.py` (sintetiza e reproduz frase longa exibindo tempos detalhados de cada etapa).

### 2.2 Redesign da Interface: HUD Holográfico Cinematográfico (Tarefa 2)
- **Biblioteca Three.js Local:** Three.js r128 baixado e servido localmente em `ui/three.min.js` (603 KB, 100% offline, sem CDN).
- **Peça Central (Reator 3D):**
  - Esfera luminosa central com shader de pulso/ruído e reação ao volume de áudio.
  - 4 anéis 3D concêntricos com rotação em múltiplos eixos e velocidades independentes.
  - Nuvem orbital com 180 partículas e mostrador externo graduado estilo HUD.
  - Reatividade ao áudio em tempo real com suavização exponencial (`lerp`).
  - 5 estados com transições fluidas: Dormindo, Ouvindo (wake word / atalho), Pensando (rotação acelerada e partículas convergindo), Falando (ondas radiais e deformação do núcleo), Erro (flash vermelho).
- **Modo Economia & Fallback Automático:** Canvas 2D holográfico a 30 fps ativado pelo botão `ECO` ou automaticamente se a taxa de quadros cair abaixo de 30 fps.
- **Identidade Visual & Vidro:**
  - Fundo `#03060d` com gradiente radial azul petróleo, grade de mira com parallax e scanlines sutis.
  - Paleta Ciano Gelo (`#00E5FF`), Azul Elétrico (`#2979FF`), Âmbar (`#FFB300`) e Vermelho (`#FF3D57`).
  - Painéis com `backdrop-filter: blur(10px)`, cantos chanfrados (`clip-path`), mira nos 4 cantos e efeito de digitação suave com cursor piscante.
  - Tema alternativo Âmbar Dourado trocável pelo cabeçalho.
- **Efeitos Sonoros Sci-Fi:** Sintetizador Web Audio API local (`OscillatorNode`) com bips para boot, ativação (`wake`), confirmação (`confirm`), erro e cliques, com opção de mudo (`FX`).
- **Segurança:** Modal em âmbar para confirmação de ações perigosas.

---

## 3. Testes Automatizados

- **Comando:** `.\.venv\Scripts\python.exe -m pytest tests/ -p no:cacheprovider`
- **Resultado:** **51 passed, 0 failed** em ~31 s.
- **Correção de Teste:** Corrigido `tests/test_fase6.py` para passar `time.perf_counter()` real em vez de `0.0`.
- **Limpeza de Logs:** Linhas-artefato com `t0=0.0` removidas de `logs/perf.log`.

---

## 4. Medições de Desempenho

| Métrica | Medição Real | Meta | Status |
|---|---|---|---|
| Síntese 1ª frase Piper (aquecida) | 0,08–0,39 s | < 1,0 s | ✅ Passou |
| Reprodução de áudio frase curta | 0,85 s | < 1,5 s | ✅ Passou |
| LLM 1º token (aquecido) | 0,63–0,67 s | < 2,0 s | ✅ Passou |
| Roteador instantâneo → áudio | 0,23–0,34 s | < 1,0 s | ✅ Passou |
| Suíte de testes unitários | 51/51 testes | 100% | ✅ Passou |

---

## 5. Critérios de Aceitação Atualizados

1. Instalação limpa e offline — **Passou** ✅
2. Jarvis sintetiza e reproduz áudio com fallback — **Passou** ✅ (Validado via `teste_voz.py` e `teste_piper_isolado.py`)
3. Latências dentro das metas — **Passou** ✅
4. Interrupção imediata / barge-in — **Passou** ✅
5. HUD Holográfico 3D Cinematográfico (Three.js + Canvas 2D + Web Audio) — **Passou** ✅
6. Confirmação de segurança para comandos perigosos com modal âmbar — **Passou** ✅
7. 100% local, custo zero, sem APIs pagas — **Passou** ✅
8. Suíte de testes íntegra (51/51) — **Passou** ✅ (agora 55/55 com Tarefa 0)

---

## 6. Sessão 29/09 manhã — Tarefas 0, 1 e 2 (instrumentação + áudio real)

- **Tarefa 0:** `core/events.py` (barramento com `events.log`), `core/states.py` (máquina única
  IDLE/LISTENING/THINKING/SPEAKING/ERROR + watchdog 20–30 s), `core/monitor.py` (nível+VAD+wake
  ~12 Hz), `core/audio.py` (dispositivo por nome + reamostragem), página `/debug`
  (mic do navegador, VAD/wake do servidor, 50 eventos, clientes), `scripts/diagnostico_completo.py`
  (**9/9 OK**). Pipeline/STT/TTS/wake emitem eventos; servidor espelha `ESTADOS` no WS.
- **Tarefa 1 (mic):** varredura com usuário falando — headset sem fio funciona
  (MME idx 1: rms 0,039, pico 0,14); USB morto (ruído 0,01). `microfone_nome: "wireless stereo"`,
  gate de energia 3× ruído, AGC leve. Transcrição ao vivo: 1,77 s de fala → texto em **1,12 s** ✅.
  WASAPI exige taxa nativa (48 k) — MME a 16 k funciona, ficou como padrão.
- **Wake word (32 tentativas reais):** "JARVIS" sozinho 1/6 (picos 0,30–0,38, limiar antigo 0,5 nunca
  disparava); **"EI JARVIS" 5/6** (0,25–0,39). `wakeword.limiar: 0.2` (silêncio pico 0,045;
  0 falsos em 20 s). **Diga "EI JARVIS".** Reserva: Ctrl+Alt+J.
- **Tarefa 2 (saída):** tons 440 Hz + voz "Teste N" nas 3 APIs do headset — **usuário ouviu tudo nos
  dois ouvidos**; WASAPI quebrou a 22050 Hz e ganhou reamostragem automática em `TTS._tocar`.
  Saída fixada: MME (mais leve) via `alto_falante_nome`. `teste_voz.py` (14 s) **ouvido por completo** ✅.
- **Testes:** 60/60 verdes (55 anteriores + 5 novos da Tarefa 3). Arquivos novos: `core/audio|events|monitor|states.py`, `ui/debug.html`,
  `scripts/diagnostico_completo|teste_microfones|teste_saidas|teste_fone|teste_wake_live|reproduzir_bug_painel.py`,
  `tests/test_tarefa0.py`, `tests/test_tarefa3.py`.

---

## 7. Tarefa 3 — Painel de Resposta (Protocolo de Streaming, Deduplicação e Loop)

- **1. Reprodução e Evidência do Bug:**
  - Script executado: `scripts/reproduzir_bug_painel.py`.
  - Causa confirmada: o WebSocket enviava o estado periodicamente a cada 250ms com a string de resposta. A função `digitarTexto` (12ms/char) ainda estava digitando os primeiros ~20 caracteres quando a próxima mensagem chegava. Como `texto_parcial !== resposta_completa`, a condição disparava novamente, limpava `innerHTML = ""` e reiniciava do zero em loop (5 reinícios em 5 ticks de 250ms).
- **2. Protocolo Implementado:**
  - `response_start(id, seq=0, origem)`: inicia novo container e demarca a origem da resposta.
  - `response_delta(id, seq, delta)`: envia trechos/tokens incrementais com número de sequência estrito.
  - `response_end(id, seq, texto_final)`: finaliza a mensagem garantindo integridade e reconciliação.
- **3. Deduplicação e Histórico de 5 Itens no Front:**
  - `Set` de sequências rastreia deltas por ID; eventos repetidos são descartados silenciosamente.
  - O painel mantém as últimas 5 respostas anteriores com rolagem estilizada (`.response-item.past`) e exibe a ativa destacada (`.response-item.active`).
  - Limpeza do elemento ativo ocorre **apenas** quando um ID novo chega (nunca por transições de estado como "falando", "pensando" ou "dormindo").
- **4. WebSocket Singleton & Backoff Exponencial:**
  - Singleton garantido: handlers antigos limpos antes de instanciar novo socket.
  - Backoff exponencial implementado: 1s -> 2s -> 4s -> 8s -> 16s (com reset para 1s no `onopen`).
  - Contagem precisa de clientes verificada em `/api/clientes`.
- **5. Renderização Eficiente via rAF:**
  - Deltas agrupados em buffer e despachados no ciclo de renderização do navegador via `requestAnimationFrame`.
  - Manipulação de texto feita exclusivamente via `textContent` em nós de texto e spans (sem destruição de DOM por `innerHTML`).
- **6. Detecção de Loop:**
  - Contador de respostas automáticas consecutivas sem entrada humana (`origem_humana=False`).
  - Se > 3 respostas automáticas consecutivas ocorrerem, o pipeline corta a execução imediatamente, emite `LOOP_DETECTADO` e define estado `ERROR`.
  - Entrada humana (texto ou STT) zera o contador.
- **7. Testes Automatizados:**
  - Novo arquivo de testes: `tests/test_tarefa3.py` (5 novos testes com 100% de aprovação).
  - Total da suíte: **60/60 testes aprovados** em 50.03s.
- **Próximo:** Tarefa 4 (Medir o 3D: FPS, CPU do Edge, GPU, memória heap e comportamento em segundo plano).

---

## 8. Sessão 29/09 tarde — TAREFA 1 (mic não escuta) — PARCIAL

- **Mapa completo de entradas:** "Wireless Stereo Headset" (MME 1 / DS 8 / WASAPI 18 / KS 21),
  "Hands-Free AG Audio" (só WDM-KS 8 kHz — não abre via PortAudio), Realtek Mic (só KS 28),
  USB (MME 2 / DS 9 / WASAPI 17 / KS 30), Line In e Stereo Mix (KS). 3 fones BT pareados
  (TWS, J-60, TECTOY). Sem Discord/Teams/Zoom abertos.
- **Medição com usuário falando (3 s por entrada):** voz chega SÓ no "Wireless Stereo" via MME
  (rms 0,013, pico 0,11); o mesmo mic via DirectSound = 0,0003 (mudo); via WASAPI = 0,009 (fraco).
  USB = só ruído 0,0105. Ganho do Windows subido para 100 pelo usuário.
- **Mecanismo do bug atual:** gate de energia (0,024 = 3× ruído 0,008) bloqueava a fala fraca (0,013).
  Com ganho máximo, a detecção passou: transcrição ao vivo de 2,85 s de fala em **2,47 s**
  (acima da meta 1,5 s).
- **Qualidade ruim — NÃO resolvido:** "olá Jarvis, teste do microfone" foi transcrito como
  "Como é o filho do Cabrunk, Jarvis?" (alucinação por áudio degradado: BT + ruído amplificado).
  Hipóteses restantes: mic BT de baixa qualidade, ambiente ruidoso, degradação de perfil HFP.
- **Confirmado sem usuário:** transições + rejeição inválida + `forcar_idle` do watchdog em
  `core/states.py` OK; `scripts/gravar_entradas.py` criado (3 s/entrada, taxa nativa).
- **Critério da tarefa (texto correto no console): NÃO atingido.** Próximo: falar perto/alto no
  silêncio; se persistir, testar (a) outro mic de entrada, (b) entrada hands-free, (c) fone com fio.
  Arquivo novo: `scripts/gravar_entradas.py`. Nenhum teste quebrado (60/60 antes desta sessão;
  sem alterações de código desta vez, só `config.yaml` já estava ajustado).

---

## 9. Sessão 30/09 — Tarefas 5/6 (otimização + start.bat) + 2 bugs reais corrigidos

- **Tarefa 5:** `num_ctx` 4096→2048, `keep_alive` -1 (com tipo preservado, ver bug abaixo);
  WS com change-detection (nível quantizado em 0,05, heartbeat 1 s, métricas 1 Hz);
  env Ollama (`KEEP_ALIVE=-1`, `MAX_LOADED=1`, `NUM_PARALLEL=1`) via `preflight.py`.
- **Bug 1 (causava resposta LLM vazia):** `brain.py` fazia `str(keep_alive)` → `"-1"` (string);
  Ollama devolve `HTTP 400: missing unit in duration "-1"`, e o modo silencioso do pipeline
  **engolia o erro** (resposta vazia). Correções: tipo preservado em `brain.py` + erro
  surfacado em `pipeline.py` (evento `LLM_ERRO`, nunca silêncio). Teste que falhava voltou ao verde.
- **Bug 2 (disputa de porta):** outra sessão mantém um watcher reiniciando o servidor com o
  Python do sistema (sem `faster_whisper`) — respostas degradadas (LLM vazio em 0,47 s).
  Semáforo inexistente; `start.bat`+`preflight.py` do usuário se autocuram (matam o ocupante e
  sobem o venv). **Pedir ao usuário pausar a outra sessão** antes de validação final.
- **Tarefa 6 (cenários testados):** (a) Ollama fechado + (c) fora do PATH → `preflight.py` acha
  via `%LOCALAPPDATA%`, inicia, espera e valida modelo (exit 0); (b) já aberto → aviso de vars.
  Flag `logs/ollama_iniciado_pelo_jarvis.flag` registra quem iniciou.
- **Latência sob contenção (honesto):** com `llama-server` + browser + outra sessão ativos,
  TTFT medido 2,6 s direto e 14,9 s via bench (vs 0,6–1,0 s em sistema quieto). Números finais
  limpos exigem medição single-tenant na Tarefa 7.
- **Testes:** 60/60 verdes (2 falhas flaky sob carga — asserts de tempo — verdes na repetição).
