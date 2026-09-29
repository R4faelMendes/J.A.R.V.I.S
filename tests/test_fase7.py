"""Testes da Fase 7 — servidor + HUD (pipeline falso; sem IA)."""
from __future__ import annotations

from types import SimpleNamespace

from fastapi.testclient import TestClient

from core.server import criar_app


def _app_falso():
    app = criar_app()

    class PipeFalso:
        _pendente = None
        tts = SimpleNamespace(parar=lambda: None, sintetizar=lambda f: (22050, __import__("numpy").zeros(22050)),
                              falando=False)

        async def processar_texto(self, texto, falar=False):
            return {"resposta": f"eco: {texto}", "origem": "teste",
                    "acao": None, "primeiro_audio_s": 0.01}

    app.state.pipeline = PipeFalso()
    return app


def test_index_e_estaticos():
    c = TestClient(_app_falso())
    assert c.get("/").status_code == 200
    assert c.get("/static/app.js").status_code == 200
    assert c.get("/static/style.css").status_code == 200
    e = c.get("/api/estado").json()
    assert "cpu" in e and "ram" in e


def test_comando_local_sem_pin():
    c = TestClient(_app_falso())
    r = c.post("/api/comando", json={"texto": "oi"})
    assert r.status_code == 200
    corpo = r.json()
    assert corpo["resposta"] == "eco: oi"
    assert corpo["audio_wav_b64"] != ""


def test_parar_e_confirmar():
    c = TestClient(_app_falso())
    assert c.post("/api/parar").json() == {"ok": True}
    assert "resposta" in c.post("/api/confirmar").json()


def test_websocket_estado():
    c = TestClient(_app_falso())
    with c.websocket_connect("/ws") as ws:
        d = ws.receive_json()
        assert "estado" in d and "cpu" in d
