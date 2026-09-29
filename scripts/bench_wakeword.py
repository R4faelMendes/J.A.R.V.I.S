"""Benchmark da wake word (Fase 4) — OFFLINE (sem microfone).

Pontua: silêncio digital, ruído baixo e a voz Piper dizendo "Jarvis".
O teste AO VIVO (20 tentativas com voz real + falsos positivos) fica
pendente do microfone físico (mesmo bloqueador da Fase 3).
Uso:
    .\\.venv\\Scripts\\python.exe scripts/bench_wakeword.py
"""
from __future__ import annotations

import sys
import time
from pathlib import Path

import numpy as np

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from voice.stt import reamostrar  # noqa: E402
from voice.wakeword import JANELA_AMOSTRAS, WakeListener  # noqa: E402


def pontuar_amostra(w: WakeListener, audio16: np.ndarray) -> dict:
    pcm = (np.clip(audio16, -1.0, 1.0) * 32767).astype(np.int16)
    scores = []
    for i in range(0, len(pcm) - JANELA_AMOSTRAS + 1, JANELA_AMOSTRAS):
        scores.append(w.pontuar(pcm[i:i + JANELA_AMOSTRAS]))
    if not scores:
        return {"max": 0.0, "media": 0.0, "quadros": 0}
    return {"max": float(max(scores)), "media": float(sum(scores) / len(scores)), "quadros": len(scores)}


def principal() -> int:
    print("JARVIS 2.0 — bench_wakeword OFFLINE (Fase 4)")
    t0 = time.perf_counter()
    w = WakeListener(on_wake=lambda: None)
    w._carregar_modelo()
    print(f"Modelo carregado em {time.perf_counter() - t0:.1f} s | limiar={w.limiar}")

    from voice.tts import TTS

    tts = TTS()
    casos = {"silencio_1s": np.zeros(16000, dtype=np.float32)}
    for frase in ["Jarvis.", "Ei Jarvis, você está aí?", "Olá, tudo bem?"]:
        taxa, audio = tts.sintetizar(frase)
        casos[f"piper_{frase[:12]}"] = reamostrar(audio, taxa, 16000)

    for nome, amostra in casos.items():
        r = pontuar_amostra(w, amostra)
        marca = "DISPARA" if r["max"] >= w.limiar else "ignora"
        print(f"  {nome}: max={r['max']:.3f} media={r['media']:.4f} ({r['quadros']} quadros) -> {marca}")
    print("\nNota: voz sintética NÃO equivale à voz humana. O teste ao vivo (20x) será feito na Fase 5.")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
