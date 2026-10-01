"""Grava 3 s de cada entrada abrvível (Tarefa 1.1): pico e RMS com usuário falando.

Pula WDM-KS (PortAudio não abre). WASAPI na taxa nativa. FALE SEM PARAR.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/gravar_entradas.py
"""
from __future__ import annotations

import numpy as np
import sounddevice as sd


def principal() -> int:
    devs = sd.query_devices()
    apis = {i: a["name"] for i, a in enumerate(sd.query_hostapis())}
    alvos = [(i, d) for i, d in enumerate(devs)
             if d["max_input_channels"] > 0 and "WDM-KS" not in apis.get(d["hostapi"], "")]
    print(f"{len(alvos)} entradas testáveis. FALE ALTO SEM PARAR até acabar (~30 s)!")
    for i, d in alvos:
        api = apis.get(d["hostapi"], "?")
        taxa = int(d.get("default_samplerate") or 16000)
        try:
            rec = sd.rec(int(taxa * 3), samplerate=taxa, channels=1,
                         dtype="int16", device=i, blocking=True)
            a = rec.flatten().astype(np.float32) / 32768.0
            rms = float(np.sqrt(np.mean(a * a)))
            pico = float(np.max(np.abs(a)))
            marca = "  <-- VOZ" if rms >= 0.02 else ""
            print(f"idx {i:>2} [{api:<8} {taxa:>5}Hz] rms={rms:.4f} pico={pico:.3f} :: {d['name'][:48]}{marca}")
        except Exception as erro:  # noqa: BLE001
            print(f"idx {i:>2} [{api:<8}] ERRO {str(erro)[:70]} :: {d['name'][:40]}")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
