"""Testes da Fase 4 — wake word (offline; teste ao vivo de 20x pendente do mic)."""
from __future__ import annotations

import time

import numpy as np

from voice.wakeword import JANELA_AMOSTRAS, WakeListener


def test_modelo_baixado():
    from pathlib import Path

    assert list((Path("models") / "wakeword").glob("hey_jarvis*.onnx")), "hey_jarvis.onnx ausente"


def test_silencio_nao_dispara():
    assert JANELA_AMOSTRAS == 1280
    w = WakeListener(on_wake=lambda: None)
    silencio = np.zeros(JANELA_AMOSTRAS, dtype=np.int16)
    assert w.pontuar(silencio) < w.limiar


def test_cooldown_e_conversa():
    toques: list = []
    w = WakeListener(on_wake=lambda: toques.append(1))
    w._disparar()
    w._disparar()  # dentro do cooldown: ignorado
    assert len(toques) == 1
    assert not w.em_conversa()
    w.manter_conversa()
    assert w.em_conversa()


def test_iniciar_parar_nao_quebram():
    w = WakeListener(on_wake=lambda: None)
    w.iniciar()
    time.sleep(1.0)
    w.parar()
    assert w._thread is None
