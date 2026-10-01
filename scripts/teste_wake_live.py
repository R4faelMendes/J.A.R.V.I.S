"""Teste ao vivo da wake word (Tarefa 1.6): 20 tentativas + 20 s de silêncio.

Diga "Jarvis" (voz normal, como chamaria a pessoa) a cada número.
Depois, 20 s em silêncio para medir falsos positivos.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/teste_wake_live.py
"""
from __future__ import annotations

import sys
import time
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

import numpy as np

from core.audio import resolver_entrada
from voice.wakeword import JANELA_AMOSTRAS, WakeListener

TENTATIVAS = int(sys.argv[1]) if len(sys.argv) > 1 else 20
FRASE = sys.argv[2] if len(sys.argv) > 2 else "JARVIS"


def principal() -> int:
    import sounddevice as sd

    idx = resolver_entrada()
    print(f"Microfone resolvido: índice {idx}. Diga {FRASE} a cada número, voz normal.")
    w = WakeListener(on_wake=lambda: None)
    w._carregar_modelo()
    print(f"Modelo ok, limiar={w.limiar}. Começando em 3 s...")
    time.sleep(3)
    acertos = 0
    for n in range(1, TENTATIVAS + 1):
        print(f"[{n}/{TENTATIVAS}] Diga {FRASE} agora (2 s)...", flush=True)
        rec = sd.rec(int(16000 * 2.0), samplerate=16000, channels=1,
                     dtype="int16", device=idx, blocking=True)
        pcm = rec.flatten()
        melhor = 0.0
        for i in range(0, len(pcm) - JANELA_AMOSTRAS + 1, JANELA_AMOSTRAS):
            melhor = max(melhor, w.pontuar(pcm[i:i + JANELA_AMOSTRAS]))
        ok = melhor >= w.limiar
        acertos += ok
        print(f"    score máx={melhor:.3f} -> {'ACERTO' if ok else 'falhou'}")
    print(f"\nAcertos: {acertos}/{TENTATIVAS}")
    print("Agora 20 s em SILÊNCIO (falsos positivos)...")
    rec = sd.rec(int(16000 * 20), samplerate=16000, channels=1,
                 dtype="int16", device=idx, blocking=True)
    pcm = rec.flatten()
    falsos = 0
    pico = 0.0
    for i in range(0, len(pcm) - JANELA_AMOSTRAS + 1, JANELA_AMOSTRAS):
        s = w.pontuar(pcm[i:i + JANELA_AMOSTRAS])
        pico = max(pico, s)
        falsos += s >= w.limiar
    print(f"Falsos positivos: {falsos} quadros | pico no silêncio={pico:.3f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
