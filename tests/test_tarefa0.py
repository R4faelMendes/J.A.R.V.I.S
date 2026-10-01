"""Testes da Tarefa 0 — instrumentação (eventos, estados, /debug)."""
from __future__ import annotations

from fastapi.testclient import TestClient

from core.events import Barramento
from core.server import criar_app
from core.states import IDLE, LISTENING, SPEAKING, THINKING, MaquinaEstados


def test_barramento_emite_e_guarda():
    b = Barramento()
    e = b.emitir("TESTE", {"x": 1})
    assert e["id"] >= 1 and e["tipo"] == "TESTE"
    assert b.ultimos(5)[-1]["tipo"] == "TESTE"


def test_estados_transicoes_validas_e_invalidas():
    m = MaquinaEstados()
    assert m.estado == IDLE
    assert m.definir(LISTENING, "t") is True
    assert m.definir(SPEAKING, "t") is True  # LISTENING->SPEAKING válida
    assert m.definir(THINKING, "t") is False  # SPEAKING->THINKING inválida
    m.forcar_idle("t")
    assert m.estado == IDLE


def test_debug_rotas():
    c = TestClient(criar_app())
    assert c.get("/debug").status_code == 200
    assert "eventos" in c.get("/api/eventos?n=5").json()
    assert "conectados" in c.get("/api/clientes").json()


def test_monitor_objeto():
    from core.monitor import MonitorAudio

    mon = MonitorAudio()
    assert mon.nivel == 0.0 and mon.rodando is False
