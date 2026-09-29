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
    app.state.estado: dict[str, Any] = {
        "estado": "dormindo", "transcricao": "", "resposta": "",
        "ferramenta": "", "nivel": 0.0, "cpu": 0.0, "ram": 0.0,
    }

    def pin_ok(req: Request) -> bool:
        host = req.client.host if req.client else ""
        dado = req.headers.get("x-jarvis-pin", "") or req.query_params.get("pin", "")
        return pin_para_host(host, dado)

    @app.on_event("startup")
    async def _startup() -> None:
        if app.state.pipeline is None:
            from core.pipeline import JarvisPipeline

            j = JarvisPipeline()
            await j.aquecer_tudo()
            app.state.pipeline = j

    @app.get("/")
    async def _index():
        return FileResponse(UI / "index.html")

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
        r = await j.processar_texto(texto, falar=bool(corpo.get("falar_pc", False)))
        if r.get("acao") and r["acao"].get("skill") not in (None, "_core"):
            app.state.estado["ferramenta"] = f"{r['acao']['skill']}.{r['acao']['metodo']}"
        else:
            app.state.estado["ferramenta"] = r.get("origem", "")
        app.state.estado.update({"estado": "falando", "resposta": r["resposta"]})
        audio_b64 = _sintetizar_resposta(j, r["resposta"])
        app.state.estado["estado"] = "dormindo" if not getattr(j, "wake", None) else "ouvindo"
        return {"resposta": r["resposta"], "origem": r.get("origem"),
                "audio_wav_b64": audio_b64, "tempos_s": round(time.perf_counter() - t0, 2),
                "acao": r.get("acao"), "precisa_confirmar": j._pendente is not None}

    @app.post("/api/parar")
    async def _parar(req: Request):
        if not pin_ok(req):
            return JSONResponse({"erro": "PIN inválido."}, status_code=401)
        app.state.pipeline.tts.parar()
        app.state.estado["estado"] = "dormindo"
        return {"ok": True}

    @app.post("/api/confirmar")
    async def _confirmar(req: Request):
        if not pin_ok(req):
            return JSONResponse({"erro": "PIN inválido."}, status_code=401)
        r = await app.state.pipeline.processar_texto("confirmo", falar=False)
        return {"resposta": r["resposta"]}

    @app.get("/api/estado")
    async def _estado():
        e = dict(app.state.estado)
        e["cpu"] = psutil.cpu_percent(interval=None)
        e["ram"] = psutil.virtual_memory().percent
        return e

    @app.websocket("/ws")
    async def _ws(ws: WebSocket):
        await ws.accept()
        try:
            while True:
                e = dict(app.state.estado)
                e["cpu"] = psutil.cpu_percent(interval=None)
                e["ram"] = psutil.virtual_memory().percent
                await ws.send_json(e)
                await asyncio.sleep(0.25)
        except WebSocketDisconnect:
            pass

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
