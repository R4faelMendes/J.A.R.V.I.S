"""Medidor de nível do microfone (diagnóstico): mostra barras ao vivo por 10 s.

Uso:
    .\\.venv\\Scripts\\python.exe scripts/check_mic.py [indice]
Fale normalmente: as barras devem mexer. Se ficarem zeradas, o problema
é microfone mudo, ganho baixo ou dispositivo errado — não é o Jarvis.
"""
from __future__ import annotations

import sys

import numpy as np
import sounddevice as sd


def principal() -> int:
    indice = int(sys.argv[1]) if len(sys.argv) > 1 else None
    print(f"Medindo microfone {indice if indice is not None else '(padrão do Windows)'} por 10 s. FALE AGORA.")
    try:
        with sd.InputStream(samplerate=16000, channels=1, dtype="int16",
                            blocksize=480, device=indice) as fluxo:
            for _ in range(int(16000 / 480 * 10)):
                bloco, _overflow = fluxo.read(480)
                a = bloco.flatten().astype(np.float32) / 32768.0
                rms = float(np.sqrt(np.mean(a * a)))
                barras = int(min(1.0, rms * 5.0) * 40)
                print(f"\r|{'#' * barras:<40}| {rms:.4f}", end="", flush=True)
    except Exception as erro:
        print(f"\n[ERRO] Não abri o microfone: {erro}")
        print("Vá em Configurações > Privacidade > Microfone > permitir apps da área de trabalho.")
        return 1
    print("\nFim. Se as barras NÃO mexeram: microfone mudo/ganho zero/dispositivo errado.")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
