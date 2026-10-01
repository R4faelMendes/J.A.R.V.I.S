"""Áudio utilitário (Tarefa 1.1/2.3): dispositivos, ruído e reamostragem.

Índices mudam quando despluga USB; nome (substring) é estável.
WASAPI exige a taxa NATIVA do dispositivo: capte/toque nela e reamostre.
"""
from __future__ import annotations

import numpy as np
import sounddevice as sd
import yaml
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
TAXA_PADRAO = 16000


def _cfg_audio() -> dict:
    try:
        cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
        return cfg.get("audio", {})
    except Exception:
        return {}


def resolver_entrada() -> int | None:
    """Devolve o índice do microfone: nome > índice salvo > padrão (None)."""
    cfg = _cfg_audio()
    nome = str(cfg.get("microfone_nome") or "").strip().lower()
    try:
        devs = sd.query_devices()
    except Exception:
        return cfg.get("microfone_indice")
    if nome:
        for i, d in enumerate(devs):
            if d["max_input_channels"] > 0 and nome in d["name"].lower():
                return i
    ind = cfg.get("microfone_indice")
    if isinstance(ind, int):
        try:
            if devs[ind]["max_input_channels"] > 0:
                return ind
        except Exception:
            pass
    return None


def resolver_saida(preferir_api: str = "MME") -> int | None:
    """Devolve o índice do alto-falante: nome > índice salvo > padrão.

    Entre homônimos (ex.: headset em MME/DirectSound/WASAPI), prefere a API
    indicada — MME é a mais leve (sem reamostragem na maioria dos casos).
    """
    cfg = _cfg_audio()
    nome = str(cfg.get("alto_falante_nome") or "").strip().lower()
    try:
        devs = sd.query_devices()
        apis = {i: a["name"] for i, a in enumerate(sd.query_hostapis())}
    except Exception:
        return cfg.get("alto_falante_indice")
    if nome:
        candidatos = [(i, d) for i, d in enumerate(devs)
                      if d["max_output_channels"] > 0 and nome in d["name"].lower()]
        for i, d in candidatos:
            if preferir_api.lower() in apis.get(d["hostapi"], "").lower():
                return i
        if candidatos:
            return candidatos[0][0]
    ind = cfg.get("alto_falante_indice")
    if isinstance(ind, int):
        try:
            if devs[ind]["max_output_channels"] > 0:
                return ind
        except Exception:
            pass
    return None


def medir_ruido_fundo(indice=None, segundos: float = 2.0, taxa: int = 16000) -> float:
    """Grava N segundos e devolve o RMS do ruído de fundo. Fique em silêncio!"""
    n = int(taxa * segundos)
    rec = sd.rec(n, samplerate=taxa, channels=1, dtype="int16",
                 device=indice, blocking=True)
    a = rec.flatten().astype(np.float32) / 32768.0
    return float(np.sqrt(np.mean(a * a)))


def reamostrar(audio: np.ndarray, taxa_orig: int, taxa_dest: int = TAXA_PADRAO) -> np.ndarray:
    """Reamostra áudio mono (interpolação linear). Mesma taxa = cópia."""
    audio = np.asarray(audio, dtype=np.float32)
    if taxa_orig == taxa_dest or len(audio) == 0:
        return audio.astype(np.float32)
    dur = len(audio) / taxa_orig
    n_novo = max(1, int(dur * taxa_dest))
    velho = np.linspace(0.0, dur, len(audio))
    novo = np.linspace(0.0, dur, n_novo)
    return np.interp(novo, velho, audio).astype(np.float32)


def taxa_nativa_saida(device=None) -> int:
    """Taxa nativa do dispositivo de saída (para o TTS reamostrar)."""
    try:
        return int(sd.query_devices(device, "output")["default_samplerate"] or 0) or 44100
    except Exception:
        return 44100
