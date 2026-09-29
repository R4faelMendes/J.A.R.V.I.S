"""Voz de entrada do JARVIS 2.0 (Fase 3) — faster-whisper + VAD, tudo na CPU.

- Captura contínua do microfone em blocos de 30 ms (16 kHz, mono).
- webrtcvad detecta início/fim da fala (fim = ~600 ms de silêncio).
- Só o trecho falado vai para o Whisper (base, int8, beam_size=1).
- Modelo carregado UMA vez e reutilizado (rápido + econômico).
Uso:
    from voice.stt import STT
    stt = STT()                       # carrega Whisper + VAD
    texto = stt.ouvir_uma_frase()     # bloqueia até você falar e parar
    print(texto)
"""
from __future__ import annotations

import queue
import time
from pathlib import Path

import numpy as np
import sounddevice as sd
import webrtcvad
import yaml
from faster_whisper import WhisperModel

RAIZ = Path(__file__).resolve().parents[1]
TAXA = 16000
BLOCO_AMOSTRAS = 480  # 30 ms a 16 kHz (formato exigido pelo webrtcvad)


def reamostrar(audio: np.ndarray, taxa_orig: int, taxa_dest: int = TAXA) -> np.ndarray:
    """Reamostra áudio mono para a taxa de destino (interpolação linear)."""
    if taxa_orig == taxa_dest:
        return audio.astype(np.float32)
    dur = len(audio) / taxa_orig
    n_novo = int(dur * taxa_dest)
    velho = np.linspace(0.0, dur, len(audio))
    novo = np.linspace(0.0, dur, n_novo)
    return np.interp(novo, velho, audio).astype(np.float32)


class STT:
    """Transcritor com endpoint por VAD. Carregue uma vez e reutilize."""

    def __init__(self, config_path: Path | str | None = None) -> None:
        caminho = Path(config_path) if config_path else RAIZ / "config.yaml"
        try:
            cfg = yaml.safe_load(caminho.read_text(encoding="utf-8"))
        except Exception as erro:
            raise RuntimeError(f"Não li {caminho}: {erro}") from erro
        stt = cfg.get("stt", {})
        audio_cfg = cfg.get("audio", {})
        self.modelo_nome: str = stt.get("modelo", "base")
        self.pasta_modelo = RAIZ / str(stt.get("pasta_modelo", "models/whisper"))
        self.cpu_threads: int = int(stt.get("cpu_threads", 4))
        self.silencio_fim_ms: int = int(stt.get("fim_fala_silencio_ms", 600))
        self.indice_mic = audio_cfg.get("microfone_indice")
        # VAD leve sem PyTorch (webrtcvad); agressividade 2 = equilíbrio.
        self.vad = webrtcvad.Vad(2)
        try:
            self.modelo = WhisperModel(
                self.modelo_nome,
                device="cpu",
                compute_type="int8",
                cpu_threads=self.cpu_threads,
                download_root=str(self.pasta_modelo),
            )
        except Exception as erro:
            raise RuntimeError(f"Não carreguei o Whisper '{self.modelo_nome}': {erro}") from erro

    def tem_fala(self, bloco_int16: bytes) -> bool:
        """Diz se um bloco de 30 ms contém voz (True/False)."""
        if len(bloco_int16) != BLOCO_AMOSTRAS * 2:
            return False
        try:
            return self.vad.is_speech(bloco_int16, TAXA)
        except Exception:
            return False

    def transcrever(self, audio: np.ndarray) -> str:
        """Transcreve um trecho de áudio (float32 mono 16 kHz)."""
        if audio.size < TAXA // 2:  # menos de 0,5 s: ignora (ruído)
            return ""
        try:
            segmentos, _info = self.modelo.transcribe(
                audio, language="pt", beam_size=1, vad_filter=True
            )
            return "".join(s.text for s in segmentos).strip()
        except Exception as erro:
            raise RuntimeError(f"Falha na transcrição: {erro}") from erro

    def ouvir_uma_frase(self, espera_max_s: float = 30.0, gravacao_max_s: float = 15.0) -> dict:
        """Ouve o microfone até detectar UMA frase completa.

        Retorna {"texto": str, "dur_fala_s": float, "tempo_transcricao_s": float}.
        Levanta RuntimeError se o microfone falhar ou ninguém falar no prazo.
        """
        fila: queue.Queue = queue.Queue()

        def callback(indata, frames, tempo, status):
            fila.put(bytes(indata))

        try:
            fluxo = sd.InputStream(
                samplerate=TAXA, channels=1, dtype="int16",
                blocksize=BLOCO_AMOSTRAS, device=self.indice_mic, callback=callback,
            )
            fluxo.start()
        except Exception as erro:
            raise RuntimeError(
                "Não abri o microfone. Verifique: Configurações > Privacidade > "
                f"Microfone > permitir apps da área de trabalho. Detalhe: {erro}"
            ) from erro

        blocos_fala: list[bytes] = []
        falando = False
        silencio_ms = 0
        voz_ms = 0
        inicio_espera = time.perf_counter()
        try:
            while True:
                try:
                    bloco = fila.get(timeout=0.1)
                except queue.Empty:
                    bloco = None
                if time.perf_counter() - inicio_espera > espera_max_s and not falando:
                    raise RuntimeError("Ninguém falou no prazo. Tente de novo.")
                if bloco is None:
                    continue
                com_voz = self.tem_fala(bloco)
                if com_voz and not falando:
                    falando = True
                    blocos_fala = [bloco]
                    voz_ms = 30
                    silencio_ms = 0
                elif falando:
                    blocos_fala.append(bloco)
                    if com_voz:
                        voz_ms += 30
                        silencio_ms = 0
                    else:
                        silencio_ms += 30
                    total_ms = len(blocos_fala) * 30
                    if silencio_ms >= self.silencio_fim_ms and voz_ms >= 300:
                        break
                    if total_ms >= gravacao_max_s * 1000:
                        break
        finally:
            fluxo.stop()
            fluxo.close()

        cru = np.frombuffer(b"".join(blocos_fala), dtype=np.int16).astype(np.float32) / 32768.0
        dur_fala = len(cru) / TAXA
        t0 = time.perf_counter()
        texto = self.transcrever(cru)
        tempo_stt = time.perf_counter() - t0
        return {"texto": texto, "dur_fala_s": dur_fala, "tempo_transcricao_s": tempo_stt}

    def nivel(self, bloco_int16: bytes) -> float:
        """Nível RMS 0..1 de um bloco (para o medidor da interface, Fase 7)."""
        if not bloco_int16:
            return 0.0
        dados = np.frombuffer(bloco_int16, dtype=np.int16).astype(np.float32) / 32768.0
        return float(min(1.0, np.sqrt(np.mean(dados * dados)) * 3.0))
