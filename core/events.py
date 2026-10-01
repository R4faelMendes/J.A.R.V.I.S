"""Barramento de eventos do JARVIS (Tarefa 0): log estruturado + histórico em memória.

Cada etapa do pipeline emite eventos com timestamp:
MIC_FRAME, VAD, WAKE, STT_START/END, LLM_START/FIRST_TOKEN/END,
TTS_SYNTH, AUDIO_PLAY_START/END, STATE_CHANGE, WS_SEND.
O servidor transmite ao /debug via WebSocket; scripts leem logs/events.log.
Nível INFO por padrão (DEBUG só em diagnóstico). Sem I/O no caminho quente:
escrita em thread própria com fila.
"""
from __future__ import annotations

import collections
import json
import queue
import threading
import time
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
ARQUIVO = RAIZ / "logs" / "events.log"
MAX_MEMORIA = 500  # últimos eventos acessíveis ao /debug


class Barramento:
    """Emissor thread-safe com buffer circular e log em arquivo (assíncrono)."""

    def __init__(self, arquivo: Path = ARQUIVO, debug: bool = False) -> None:
        self._memoria: collections.deque = collections.deque(maxlen=MAX_MEMORIA)
        self._fila: queue.Queue = queue.Queue()
        self._arquivo = arquivo
        self._debug = debug
        self._seq = 0
        self._trava = threading.Lock()
        self._ouvintes: list = []
        self._thread = threading.Thread(target=self._escrever, daemon=True)
        self._thread.start()

    def set_debug(self, ligado: bool) -> None:
        """Liga/desliga o nível DEBUG (modo diagnóstico)."""
        self._debug = ligado

    def emitir(self, tipo: str, dados: dict | None = None, nivel: str = "INFO") -> dict:
        """Registra um evento e devolve o dicionário (com id e timestamp)."""
        if nivel == "DEBUG" and not self._debug:
            return {}
        with self._trava:
            self._seq += 1
            evento = {"id": self._seq, "ts": round(time.time(), 3),
                      "tipo": tipo, "dados": dados or {}}
        self._memoria.append(evento)
        for ouvinte in list(self._ouvintes):
            try:
                ouvinte(evento)
            except Exception:
                pass
        try:
            self._fila.put_nowait(evento)
        except queue.Full:
            pass
        return evento

    def ultimos(self, n: int = 50) -> list[dict]:
        """Últimos n eventos (para o /debug)."""
        return list(self._memoria)[-n:]

    def assinar(self, fn) -> None:
        """Registra um ouvinte chamado a cada evento (ex.: broadcast WS)."""
        self._ouvintes.append(fn)

    def _escrever(self) -> None:
        try:
            self._arquivo.parent.mkdir(parents=True, exist_ok=True)
        except Exception:
            return
        while True:
            evento = self._fila.get()
            try:
                with self._arquivo.open("a", encoding="utf-8") as f:
                    f.write(json.dumps(evento, ensure_ascii=False) + "\n")
            except Exception:
                pass


# Instância global (importada por pipeline, voz e servidor).
BUS = Barramento()
