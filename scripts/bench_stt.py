"""Benchmark da escuta (Fase 3): você fala UMA frase, eu transcrevo e meço o tempo.

Uso:
    .\\.venv\\Scripts\\python.exe scripts/bench_stt.py
Meta: texto pronto em até 1,5 s após você parar de falar.
"""
from __future__ import annotations

import datetime
import sys
import winsound
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from voice.stt import STT  # noqa: E402


def principal() -> int:
    print("JARVIS 2.0 — bench_stt (Fase 3)")
    stt = STT()
    print(f"Whisper '{stt.modelo_nome}' carregado. Fale após o BIP e pare de falar.")
    print("Frase sugerida: 'olá Jarvis, teste do microfone'")
    winsound.Beep(880, 300)
    try:
        resultado = stt.ouvir_uma_frase()
    except RuntimeError as erro:
        print(f"[ERRO] {erro}")
        return 1
    print(f"Você disse: {resultado['texto']!r}")
    print(f"Fala: {resultado['dur_fala_s']:.2f} s | Transcrição: {resultado['tempo_transcricao_s']:.2f} s (meta: < 1,5 s)")
    log = RAIZ / "logs" / "perf.log"
    linha = (
        f"{datetime.datetime.now():%Y-%m-%d %H:%M:%S} | fase3 | "
        f"dur_fala_s={resultado['dur_fala_s']:.2f} | transcricao_s={resultado['tempo_transcricao_s']:.2f} | "
        f"texto={resultado['texto'][:80]}\n"
    )
    log.write_text(log.read_text(encoding="utf-8") + linha if log.exists() else linha, encoding="utf-8")
    print(f"Registrado em {log}")
    if resultado["tempo_transcricao_s"] > 1.5:
        print("AVISO honesto: acima de 1,5 s. whisper 'base' pode estar pesado; reveja cpu_threads.")
    if not resultado["texto"]:
        print("AVISO honesto: transcrição vazia — fale mais alto/perto ou confira o microfone padrão.")
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
