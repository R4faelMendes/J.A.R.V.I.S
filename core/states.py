"""Máquina de estados única do JARVIS (Tarefa 0/3): fonte da verdade no servidor.

Estados: IDLE → LISTENING → THINKING → SPEAKING → IDLE (ou ERROR → IDLE).
Transições inválidas são recusadas e registradas. Watchdog: se um estado
passar do tempo máximo, volta a IDLE e reabre o microfone (cura a "porta
trancada": preso em SPEAKING/THINKING para sempre).
"""
from __future__ import annotations

import threading
import time
from typing import Callable

from core.events import BUS

IDLE, LISTENING, THINKING, SPEAKING, ERROR = "IDLE", "LISTENING", "THINKING", "SPEAKING", "ERROR"

VALIDAS = {
    IDLE: {LISTENING, SPEAKING, ERROR},
    LISTENING: {THINKING, IDLE, ERROR, SPEAKING},
    THINKING: {SPEAKING, IDLE, ERROR},
    SPEAKING: {IDLE, LISTENING, ERROR},
    ERROR: {IDLE},
}

# Tempo máximo por estado (s). Estourou → watchdog volta a IDLE.
MAX_TEMPO = {LISTENING: 30.0, THINKING: 20.0, SPEAKING: 30.0, ERROR: 10.0}


class MaquinaEstados:
    """Estado global com watchdog. Thread-safe."""

    def __init__(self, ao_mudar: Callable[[str, str, str], None] | None = None) -> None:
        self._estado = IDLE
        self._desde = time.time()
        self._motivo = "inicio"
        self._trava = threading.Lock()
        self._ao_mudar = ao_mudar
        self._watch = threading.Thread(target=self._vigiar, daemon=True)
        self._watch.start()

    @property
    def estado(self) -> str:
        """Estado atual (leitura thread-safe)."""
        with self._trava:
            return self._estado

    def definir(self, novo: str, motivo: str = "") -> bool:
        """Tenta transição; retorna False se inválida (e registra)."""
        with self._trava:
            velho = self._estado
            if novo not in VALIDAS.get(velho, set()):
                BUS.emitir("STATE_REJEITADA",
                           {"de": velho, "para": novo, "motivo": motivo}, nivel="DEBUG")
                return False
            self._estado = novo
            self._desde = time.time()
            self._motivo = motivo
        BUS.emitir("STATE_CHANGE", {"de": velho, "para": novo, "motivo": motivo})
        if self._ao_mudar:
            try:
                self._ao_mudar(velho, novo, motivo)
            except Exception:
                pass
        return True

    def forcar_idle(self, motivo: str) -> None:
        """Volta a IDLE por qualquer caminho (usado pelo watchdog)."""
        with self._trava:
            velho = self._estado
            self._estado = IDLE
            self._desde = time.time()
            self._motivo = motivo
        BUS.emitir("STATE_CHANGE", {"de": velho, "para": IDLE, "motivo": motivo})

    def _vigiar(self) -> None:
        while True:
            time.sleep(1.0)
            with self._trava:
                est, desde = self._estado, self._desde
            limite = MAX_TEMPO.get(est)
            if limite and (time.time() - desde) > limite:
                self.forcar_idle(f"watchdog: {est} há {limite:.0f}s")
                BUS.emitir("WATCHDOG", {"estado": est, "limite_s": limite})


# Instância global (servidor e pipeline compartilham).
ESTADOS = MaquinaEstados()
