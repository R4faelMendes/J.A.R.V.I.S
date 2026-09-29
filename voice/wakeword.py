"""Palavra de ativação do JARVIS 2.0 (Fase 4) — openWakeWord (onnx) + atalho global.

- Ouve sempre com CPU mínima: blocos de 80 ms no modelo hey_jarvis (onnx).
- Fluxo: "Jarvis" (score >= limiar) -> callback on_wake (o pipeline toca o bip).
- Modo conversa: após responder, follow-ups por ~8 s sem repetir a palavra.
- Reserva obrigatória: atalho global Ctrl+Alt+J (configurável), sempre ativo.
Uso:
    from voice.wakeword import WakeListener
    w = WakeListener(on_wake=lambda: print("Jarvis!"))
    w.iniciar()            # thread do mic + atalho; não bloqueia
    ...
    w.parar()
"""
from __future__ import annotations

import queue
import threading
import time
from pathlib import Path
from typing import Callable

import numpy as np
import sounddevice as sd
import yaml

RAIZ = Path(__file__).resolve().parents[1]
TAXA = 16000
JANELA_AMOSTRAS = 1280  # 80 ms a 16 kHz (quadro do openWakeWord)


class WakeListener:
    """Ouvinte de 'Jarvis' com atalho global de reserva."""

    def __init__(
        self,
        on_wake: Callable[[], None],
        config_path: Path | str | None = None,
    ) -> None:
        caminho = Path(config_path) if config_path else RAIZ / "config.yaml"
        try:
            cfg = yaml.safe_load(caminho.read_text(encoding="utf-8"))
        except Exception as erro:
            raise RuntimeError(f"Não li {caminho}: {erro}") from erro
        ww = cfg.get("wakeword", {})
        audio_cfg = cfg.get("audio", {})
        self.limiar: float = float(ww.get("limiar", 0.5))
        self.modelo_nome: str = str(ww.get("modelo", "hey_jarvis"))
        self.janela_conversa_s: float = float(ww.get("modo_conversa_seg", 8))
        self.atalho: str = str(ww.get("atalho_global", "ctrl+alt+j"))
        self.indice_mic = audio_cfg.get("microfone_indice")
        self.on_wake = on_wake
        self._rodando = threading.Event()
        self._thread: threading.Thread | None = None
        self._ultimo_disparo = 0.0
        self._conversa_ate = 0.0
        self._modelo = None
        pasta = RAIZ / "models" / "wakeword"
        candidatos = sorted(pasta.glob("hey_jarvis*.onnx"))
        if not candidatos:
            raise FileNotFoundError(
                "Modelo hey_jarvis não encontrado em models/wakeword. "
                "Baixe com: .\\.venv\\Scripts\\python.exe -c "
                "\"from openwakeword import utils; utils.download_models(['hey_jarvis'], 'models/wakeword')\""
            )
        self._caminho_modelo = str(candidatos[0])
        self._hotkey_id = None

    def _carregar_modelo(self) -> None:
        from openwakeword.model import Model

        self._modelo = Model(
            wakeword_models=[self._caminho_modelo], inference_framework="onnx"
        )

    def pontuar(self, bloco80ms: np.ndarray) -> float:
        """Pontua UM quadro de 80 ms (int16 mono 16 kHz). Retorna score 0..1."""
        if self._modelo is None:
            self._carregar_modelo()
        try:
            pred = self._modelo.predict(bloco80ms)
        except Exception:
            return 0.0
        if not pred:
            return 0.0
        return float(max(pred.values()))

    def em_conversa(self) -> bool:
        """True se estamos na janela de follow-up (sem precisar da palavra)."""
        return time.time() < self._conversa_ate

    def manter_conversa(self) -> None:
        """Abre/renova a janela de follow-up de ~8 s (chamar após cada resposta)."""
        self._conversa_ate = time.time() + self.janela_conversa_s

    def _disparar(self) -> None:
        agora = time.time()
        if agora - self._ultimo_disparo < 3.0:  # cooldown: evita disparo duplo
            return
        self._ultimo_disparo = agora
        try:
            self.on_wake()
        except Exception:
            pass

    def _laco_mic(self) -> None:
        fila: queue.Queue = queue.Queue()
        try:
            fluxo = sd.InputStream(
                samplerate=TAXA, channels=1, dtype="int16",
                blocksize=480, device=self.indice_mic,
                callback=lambda indata, f, t, s: fila.put(bytes(indata)),
            )
            fluxo.start()
        except Exception:
            return  # microfone indisponível: o atalho continua funcionando
        sobra = np.zeros(0, dtype=np.int16)
        while self._rodando.is_set():
            try:
                bloco = fila.get(timeout=0.2)
            except queue.Empty:
                continue
            vindo = np.frombuffer(bloco, dtype=np.int16)
            sobra = np.concatenate([sobra, vindo])
            while len(sobra) >= JANELA_AMOSTRAS and self._rodando.is_set():
                quadro, sobra = sobra[:JANELA_AMOSTRAS], sobra[JANELA_AMOSTRAS:]
                if self.pontuar(quadro) >= self.limiar:
                    self._disparar()
        try:
            fluxo.stop()
            fluxo.close()
        except Exception:
            pass

    def iniciar(self) -> None:
        """Liga o mic em thread + registra o atalho global. Não bloqueia."""
        self._carregar_modelo()
        self._rodando.set()
        self._thread = threading.Thread(target=self._laco_mic, daemon=True)
        self._thread.start()
        try:
            import keyboard

            self._hotkey_id = keyboard.add_hotkey(self.atalho, self._disparar)
        except Exception:
            self._hotkey_id = None  # atalho indisponível; mic continua

    def parar(self) -> None:
        """Desliga mic e atalho."""
        self._rodando.clear()
        if self._hotkey_id is not None:
            try:
                import keyboard

                keyboard.remove_hotkey(self._hotkey_id)
            except Exception:
                pass
            self._hotkey_id = None
        if self._thread is not None:
            self._thread.join(timeout=3)
            self._thread = None
