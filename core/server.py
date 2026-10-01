"""Servidor e interface do JARVIS 2.0 (Fase 7) — FastAPI + WebSocket.

- Serve a interface HUD (ui/) e expõe a API local do Jarvis.
- WebSocket /ws: estado, nível de áudio, transcrição, resposta, ferramenta,
  logs e métricas (CPU/RAM) em tempo real.
- POST /api/comando {texto}: executa um turno e devolve texto + áudio (wav
  base64) para o navegador tocar. POST /api/parar, /api/confirmar.
- Segurança: fora do localhost exige o PIN do config (Fase 8 detalha o celular).
Uso:
    .\\.venv\\Scripts\\python.exe -m uvicorn core.server:app --host 127.0.0.1 --port 8765
"""
from __future__ import annotations

import asyncio
import base64
import io
import time
import wave
from pathlib import Path
from typing import Any

import numpy as np
import psutil
import yaml
from fastapi import FastAPI, WebSocket, WebSocketDisconnect, Request
from fastapi.responses import FileResponse, JSONResponse
from fastapi.staticfiles import StaticFiles

from core.events import BUS
from core.states import ESTADOS, ERROR, IDLE, LISTENING, SPEAKING, THINKING

# Máquina de estados (servidor) -> HUD (interface).
MAPA_ESTADO = {IDLE: "dormindo", LISTENING: "ouvindo", THINKING: "pensando",
               SPEAKING: "falando", ERROR: "erro"}

RAIZ = Path(__file__).resolve().parents[1]
UI = RAIZ / "ui"


def _cfg() -> dict:
    try:
        return yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8")) or {}
    except Exception:
        return {}


def pin_para_host(host: str, pin_dado: str) -> bool:
    """True se o host pode chamar a API (localhost livre; resto exige PIN)."""
    if host in {"127.0.0.1", "::1", "localhost", "testclient", "testserver"}:
        return True
    cfg = _cfg()
    return pin_dado == str(cfg.get("servidor", {}).get("pin", "0000"))


def criar_app() -> FastAPI:
    """Fábrica do app (testes injetam pipeline falso em app.state.pipeline)."""
    app = FastAPI(title="JARVIS 2.0")
    app.state.pipeline = None  # JarvisPipeline real (preguiçoso, no startup)
    app.state.loop = None      # asyncio event loop para broadcast de threads
    app.state.estado: dict[str, Any] = {
        "estado": "dormindo", "transcricao": "", "resposta": "",
        "ferramenta": "", "nivel": 0.0, "cpu": 0.0, "ram": 0.0,
        "vad": 0, "wake": 0.0,
    }
    app.state.clientes: set[WebSocket] = set()  # WebSockets conectados (contagem no /debug)
    app.state.monitor = None  # MonitorAudio (Tarefa 0: nível+VAD+wake ao vivo)

    def pin_ok(req: Request) -> bool:
        host = req.client.host if req.client else ""
        dado = req.headers.get("x-jarvis-pin", "") or req.query_params.get("pin", "")
        return pin_para_host(host, dado)

    async def broadcast_ws(msg: dict) -> None:
        """Envia mensagem JSON para todos os clientes WebSocket conectados."""
        for ws in list(app.state.clientes):
            try:
                await ws.send_json(msg)
            except Exception:
                pass

    def _ao_evento_bus(evento: dict) -> None:
        """Encaminha eventos do barramento para os WebSockets conectados."""
        tipo = evento.get("tipo")
        dados = evento.get("dados", {})
        msg = None
        if tipo == "RESPONSE_START":
            msg = {"tipo": "response_start", "id": dados.get("id"),
                   "seq": dados.get("seq", 0), "origem": dados.get("origem", "")}
        elif tipo == "RESPONSE_DELTA":
            msg = {"tipo": "response_delta", "id": dados.get("id"),
                   "seq": dados.get("seq", 1), "delta": dados.get("delta", "")}
        elif tipo == "RESPONSE_END":
            msg = {"tipo": "response_end", "id": dados.get("id"),
                   "seq": dados.get("seq", 1), "texto_final": dados.get("texto_final", "")}
        elif tipo == "TRANSCRICAO":
            msg = {"tipo": "transcricao", "texto": dados.get("texto", "")}
        elif tipo == "LOOP_DETECTADO":
            msg = {"tipo": "loop_detectado", "consecutivas": dados.get("consecutivas", 0)}
        elif tipo == "CONFIRMACAO_PENDENTE":
            msg = {"tipo": "confirmacao", "acao": dados.get("acao")}

        if msg and app.state.loop is not None and app.state.loop.is_running():
            asyncio.run_coroutine_threadsafe(broadcast_ws(msg), app.state.loop)

    BUS.assinar(_ao_evento_bus)

    @app.on_event("startup")
    async def _startup() -> None:
        app.state.loop = asyncio.get_running_loop()
        if app.state.pipeline is None:
            from core.pipeline import JarvisPipeline

            j = JarvisPipeline()
            await j.aquecer_tudo()
            app.state.pipeline = j
        # Monitor de áudio p/ o /debug (nunca derruba o servidor se falhar).
        try:
            from core.monitor import MonitorAudio

            cfg = _cfg()
            mon = MonitorAudio(
                on_wake=(app.state.pipeline._ao_acordar
                         if app.state.pipeline else None),
                indice_mic=cfg.get("audio", {}).get("microfone_indice"),
                limiar_wake=float(cfg.get("wakeword", {}).get("limiar", 0.5)),
            )
            mon.iniciar()
            app.state.monitor = mon
        except Exception as erro:  # noqa: BLE001
            BUS.emitir("MIC_ERRO", {"erro": f"monitor: {erro}"[:200]})

    @app.get("/")
    async def _index():
        return FileResponse(UI / "index.html")

    @app.get("/debug")
    async def _debug():
        return FileResponse(UI / "debug.html")

    @app.get("/api/eventos")
    async def _eventos(n: int = 50):
        return {"eventos": BUS.ultimos(max(1, min(n, 200)))}

    @app.get("/api/clientes")
    async def _clientes():
        return {"conectados": len(app.state.clientes)}

    @app.post("/api/debug")
    async def _debug_nivel(req: Request):
        if not pin_ok(req):
            return JSONResponse({"erro": "PIN inválido."}, status_code=401)
        corpo = await req.json()
        BUS.set_debug(bool(corpo.get("ligado", False)))
        return {"debug": BUS._debug}

    @app.post("/api/comando")
    async def _comando(req: Request):
        if not pin_ok(req):
            return JSONResponse({"erro": "PIN inválido."}, status_code=401)
        corpo_raw = await req.body()
        try:
            import json as _json
            corpo = _json.loads(corpo_raw.decode("utf-8-sig"))
        except UnicodeDecodeError:
            # Tolerância: PowerShell 5.1 envia latin-1 por padrão.
            corpo = _json.loads(corpo_raw.decode("latin-1"))
        texto = str(corpo.get("texto", "")).strip()
        if not texto:
            return {"erro": "Texto vazio."}
        j = app.state.pipeline
        app.state.estado.update({"estado": "pensando", "transcricao": texto})
        t0 = time.perf_counter()
        try:
            r = await j.processar_texto(texto, falar=bool(corpo.get("falar_pc", False)), origem_humana=True)
        except TypeError:
            r = await j.processar_texto(texto, falar=bool(corpo.get("falar_pc", False)))
        if r.get("acao") and r["acao"].get("skill") not in (None, "_core"):
            app.state.estado["ferramenta"] = f"{r['acao']['skill']}.{r['acao']['metodo']}"
        else:
            app.state.estado["ferramenta"] = r.get("origem", "")
        app.state.estado.update({"estado": "falando", "resposta": r["resposta"]})
        audio_b64 = _sintetizar_resposta(j, r["resposta"])
        app.state.estado["estado"] = "dormindo" if not getattr(j, "wake", None) else "ouvindo"
        return {"id": r.get("id"), "resposta": r["resposta"], "origem": r.get("origem"),
                "audio_wav_b64": audio_b64, "tempos_s": round(time.perf_counter() - t0, 2),
                "acao": r.get("acao"), "precisa_confirmar": getattr(j, "_pendente", None) is not None}

    @app.post("/api/parar")
    async def _parar(req: Request):
        if not pin_ok(req):
            return JSONResponse({"erro": "PIN inválido."}, status_code=401)
        if hasattr(app.state.pipeline, "tts"):
            app.state.pipeline.tts.parar()
        app.state.estado["estado"] = "dormindo"
        return {"ok": True}

    @app.post("/api/confirmar")
    async def _confirmar(req: Request):
        if not pin_ok(req):
            return JSONResponse({"erro": "PIN inválido."}, status_code=401)
        try:
            r = await app.state.pipeline.processar_texto("confirmo", falar=False, origem_humana=True)
        except TypeError:
            r = await app.state.pipeline.processar_texto("confirmo", falar=False)
        return {"id": r.get("id"), "resposta": r["resposta"]}

    @app.get("/api/estado")
    async def _estado():
        e = dict(app.state.estado)
        e["estado"] = MAPA_ESTADO.get(ESTADOS.estado, e.get("estado", "dormindo"))
        mon = app.state.monitor
        if mon is not None:
            e["nivel"] = round(mon.nivel, 3)
            e["vad"] = mon.vad
            e["wake"] = round(mon.wake, 3)
        e["cpu"] = round(psutil.cpu_percent(interval=None), 1)
        e["ram"] = round(psutil.virtual_memory().percent, 1)
        e["clientes"] = len(app.state.clientes)
        return e

    @app.websocket("/ws")
    async def _ws(ws: WebSocket):
        if app.state.loop is None:
            try:
                app.state.loop = asyncio.get_running_loop()
            except Exception:
                pass
        await ws.accept()
        app.state.clientes.add(ws)
        BUS.emitir("WS_SEND", {"tipo": "conectado", "clientes": len(app.state.clientes)})
        ultima_cpu = 0.0
        ultima_medicao_cpu = round(psutil.cpu_percent(interval=None), 1)
        ultima_medicao_ram = round(psutil.virtual_memory().percent, 1)
        ultimo_envio: dict = {}
        ultimo_heartbeat = 0.0
        try:
            while True:
                mon = app.state.monitor
                agora = time.time()
                if agora - ultima_cpu >= 1.0:  # métricas de sistema a 1 Hz (Tarefa 5)
                    ultima_cpu = agora
                    ultima_medicao_cpu = round(psutil.cpu_percent(interval=None), 1)
                    ultima_medicao_ram = round(psutil.virtual_memory().percent, 1)

                # Telemetria periódica (sem campo resposta para não resetar digitação)
                nivel_q = round((round(mon.nivel, 3) if mon else 0.0) * 20) / 20  # passo 0,05
                e = {
                    "tipo": "telemetria",
                    "estado": MAPA_ESTADO.get(ESTADOS.estado, "dormindo"),
                    "ferramenta": app.state.estado.get("ferramenta", ""),
                    "nivel": nivel_q,
                    "vad": mon.vad if mon else 0,
                    "wake": round(mon.wake, 3) if mon else 0.0,
                    "cpu": ultima_medicao_cpu,
                    "ram": ultima_medicao_ram,
                    "clientes": len(app.state.clientes),
                }
                # Tarefa 5: não reenvia o que não mudou (heartbeat de 1 s garante vida).
                if e != ultimo_envio or agora - ultimo_heartbeat >= 1.0:
                    ultimo_envio = e
                    ultimo_heartbeat = agora
                    await ws.send_json(e)
                await asyncio.sleep(0.25)
        except WebSocketDisconnect:
            pass
        except Exception:
            pass
        finally:
            app.state.clientes.discard(ws)
            BUS.emitir("WS_SEND", {"tipo": "desconectado", "clientes": len(app.state.clientes)})

    if UI.is_dir():
        app.mount("/static", StaticFiles(directory=str(UI)), name="static")
    return app


def _sintetizar_resposta(pipeline, texto: str) -> str:
    """Sintetiza a resposta completa e devolve wav em base64 (p/ o navegador)."""
    from voice.tts import dividir_frases

    frases = dividir_frases(texto)
    if not frases:
        return ""
    taxa = 22050
    trechos: list[np.ndarray] = []
    for frase in frases[:8]:  # limite de segurança
        try:
            t, a = pipeline.tts.sintetizar(frase)
            taxa = t
            trechos.append(a)
        except Exception:
            continue
    if not trechos:
        return ""
    pcm = (np.clip(np.concatenate(trechos), -1.0, 1.0) * 32767).astype(np.int16)
    buf = io.BytesIO()
    with wave.open(buf, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(taxa)
        w.writeframes(pcm.tobytes())
    return base64.b64encode(buf.getvalue()).decode("ascii")


app = criar_app()
