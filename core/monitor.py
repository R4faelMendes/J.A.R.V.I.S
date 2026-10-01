"""Monitor de áudio do servidor (Tarefa 0): nível + VAD + wake score ao vivo.

Thread leve que abre o microfone e, a cada 80 ms, emite:
MIC_FRAME(nível), VAD(prob 0/1) e WAKE(score). Só observa; quem age é o
pipeline (via on_wake). Se o mic falhar, registra e desliga sem derrubar nada.
"""
from __future__ import annotations

import queue
import threading
import time
from typing import Callable

import numpy as np

from core.events import BUS


class MonitorAudio:
    """Observador contínuo do microfone para o /debug. Leve (~12 Hz)."""

    def __init__(self, on_wake: Callable[[float], None] | None = None,
                 indice_mic=None, limiar_wake: float = 0.5) -> None:
        self.on_wake = on_wake
        self.indice_mic = indice_mic
        self.limiar_wake = limiar_wake
        self.nivel = 0.0
        self.vad = 0
        self.wake = 0.0
        self.rodando = False
        self.erro = ""
        self._thread: threading.Thread | None = None

    def iniciar(self) -> bool:
        """Abre o mic em thread. Retorna False se indisponível (sem exceção)."""
        if self.rodando:
            return True
        if self.indice_mic is None:
            from core.audio import resolver_entrada

            self.indice_mic = resolver_entrada()
        self.rodando = True
        self._thread = threading.Thread(target=self._laco, daemon=True)
        self._thread.start()
        return True

    def parar(self) -> None:
        """Encerra o monitor."""
        self.rodando = False
        if self._thread:
            self._thread.join(timeout=3)
            self._thread = None

    def _laco(self) -> None:
        import sounddevice as sd
        import webrtcvad

        try:
            from voice.wakeword import JANELA_AMOSTRAS, WakeListener
            wake = WakeListener(on_wake=lambda: None)
            wake._carregar_modelo()
        except Exception as erro:  # noqa: BLE001 - monitor nunca pode quebrar
            self.erro = f"wake indisponível: {erro}"
            wake = None
        vad = webrtcvad.Vad(2)
        fila: queue.Queue = queue.Queue()
        try:
            fluxo = sd.InputStream(samplerate=16000, channels=1, dtype="int16",
                                   blocksize=480, device=self.indice_mic,
                                   callback=lambda i, f, t, s: fila.put(bytes(i)))
            fluxo.start()
        except Exception as erro:  # noqa: BLE001
            self.erro = f"mic indisponível: {erro}"
            BUS.emitir("MIC_ERRO", {"erro": str(erro)[:200]})
            self.rodando = False
            return
        sobra = np.zeros(0, dtype=np.int16)
        ultimo_envio = 0.0
        while self.rodando:
            try:
                bloco = fila.get(timeout=0.2)
            except queue.Empty:
                continue
            vindo = np.frombuffer(bloco, dtype=np.int16)
            a = vindo.astype(np.float32) / 32768.0
            self.nivel = float(min(1.0, np.sqrt(np.mean(a * a)) * 3.0))
            sobra = np.concatenate([sobra, vindo])
            while len(sobra) >= JANELA_AMOSTRAS:
                quadro, sobra = sobra[:JANELA_AMOSTRAS], sobra[JANELA_AMOSTRAS:]
                try:
                    self.vad = 1 if vad.is_speech(quadro.tobytes(), 16000) else 0
                except Exception:
                    self.vad = 0
                if wake is not None:
                    try:
                        self.wake = wake.pontuar(quadro)
                    except Exception:
                        self.wake = 0.0
                    if self.wake >= self.limiar_wake and self.on_wake:
                        try:
                            self.on_wake(self.wake)
                        except Exception:
                            pass
            agora = time.time()
            if agora - ultimo_envio >= 0.08:  # ~12 Hz: enxuto p/ o WS
                ultimo_envio = agora
                BUS.emitir("MIC_FRAME", {"nivel": round(self.nivel, 3)}, nivel="DEBUG")
                BUS.emitir("VAD", {"prob": self.vad}, nivel="DEBUG")
                BUS.emitir("WAKE", {"score": round(self.wake, 3)}, nivel="DEBUG")
        try:
            fluxo.stop()
            fluxo.close()
        except Exception:
            pass
