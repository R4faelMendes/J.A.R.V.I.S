"""Teste focado de fone (Tarefa 2): fala 'Teste N' nas saídas informadas.

Uso:
    .\\.venv\\Scripts\\python.exe scripts/teste_fone.py 4 11 16
"""
from __future__ import annotations

import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

import sounddevice as sd

from voice.tts import TTS


def principal() -> int:
    indices = [int(a) for a in sys.argv[1:]] or [4, 11, 16]
    tts = TTS()
    tts.aquecer()
    for n, i in enumerate(indices, 1):
        print(f"TESTE {n} na saída {i}", flush=True)
        try:
            taxa, audio = tts.sintetizar(f"Teste {n}. Esquerda. Direita.")
            tts._tocar(taxa, audio)
        except Exception as erro:  # noqa: BLE001
            print(f"  ERRO na saída {i}: {str(erro)[:120]}")
    tts.fechar()
    print("FIM")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
