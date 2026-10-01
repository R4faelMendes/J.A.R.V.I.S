"""Varredura de microfones (Tarefa 1.1): grava 1,5 s de CADA entrada e mede nível.

O usuário deve FALAR ALTO SEM PARAR durante toda a varredura.
Mostra: índice, nome, host API (MME/WASAPI), taxa padrão, RMS e pico.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/teste_microfones.py
"""
from __future__ import annotations

import numpy as np
import sounddevice as sd


def principal() -> int:
    devs = sd.query_devices()
    apis = {i: a["name"] for i, a in enumerate(sd.query_hostapis())}
    entradas = [(i, d) for i, d in enumerate(devs)
                if d["max_input_channels"] > 0 and "KS" not in d["name"]]
    print(f"{len(entradas)} entradas (sem WDM-KS). FALE ALTO SEM PARAR até acabar!")
    print(f"{'idx':>4} {'api':<8} {'taxa':>6} {'rms':>7} {'pico':>6}  nome")
    melhor = (None, 0.0)
    for i, d in entradas:
        api = apis.get(d["hostapi"], "?")
        taxa = int(d.get("default_samplerate", 0) or 0)
        try:
            rec = sd.rec(int(16000 * 1.5), samplerate=16000, channels=1,
                         dtype="int16", device=i, blocking=True)
            a = rec.flatten().astype(np.float32) / 32768.0
            rms = float(np.sqrt(np.mean(a * a)))
            pico = float(np.max(np.abs(a)))
        except Exception as erro:  # noqa: BLE001 - informa e segue
            print(f"{i:>4} {api:<8} {taxa:>6}   ERRO  {str(erro)[:60]}  {d['name'][:45]}")
            continue
        print(f"{i:>4} {api:<8} {taxa:>6} {rms:>7.4f} {pico:>6.3f}  {d['name'][:45]}")
        if rms > melhor[1]:
            melhor = (i, rms)
    print(f"\nMaior nível: índice {melhor[0]} (rms={melhor[1]:.4f})")
    if melhor[1] < 0.02:
        print("NENHUM mic ouviu voz (tudo < 0.02 = silêncio/ruído). Confira mute, ganho e USB.")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
