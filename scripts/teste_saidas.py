"""Teste de saídas (Tarefa 2.1): toca 440 Hz por 2 s em CADA saída.

Anote o NÚMERO de cada som que você ouvir e me diga no final.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/teste_saidas.py
"""
from __future__ import annotations

import numpy as np
import sounddevice as sd


def principal() -> int:
    devs = sd.query_devices()
    apis = {i: a["name"] for i, a in enumerate(sd.query_hostapis())}
    saidas = [(i, d) for i, d in enumerate(devs)
              if d["max_output_channels"] > 0 and "KS" not in d["name"]]
    print(f"{len(saidas)} saídas. Vai tocar um BIP de 2 s em cada uma, na ordem.")
    sr = 44100
    t = np.arange(sr * 2) / sr
    tom = (0.4 * np.sin(2 * np.pi * 440 * t)).astype(np.float32)
    for i, d in saidas:
        api = apis.get(d["hostapi"], "?")
        print(f"\n>>> Tocando na saída {i} [{api}] {d['name'][:50]} ...")
        try:
            sd.play(tom, sr, device=i, blocking=True)
            print("    (se ouviu, anote o número)")
        except Exception as erro:  # noqa: BLE001
            print(f"    ERRO: {str(erro)[:100]}")
    print("\nFim. Me diga QUAIS números você ouviu (ou 'nenhum').")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
