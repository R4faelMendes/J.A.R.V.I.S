"""Benchmark da voz (Fase 2): velocidade de síntese por voz + latência até o 1º áudio.

Compara faber-medium x edresson-low e TOCA uma frase longa na voz configurada.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/bench_tts.py
Meta: áudio da 1ª frase começando em menos de 1 s.
"""
from __future__ import annotations

import datetime
import sys
import time
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from voice.tts import TTS, dividir_frases  # noqa: E402

FRASE_LONGA = (
    "Olá! Eu sou o Jarvis, seu assistente pessoal, rodando cem por cento no seu computador. "
    "Esta é uma frase longa para testar se o áudio começa em menos de um segundo, como manda a meta da fase dois."
)


def medir_voz(nome_voz: str) -> dict:
    tts = TTS(voz=nome_voz)
    frases = dividir_frases(FRASE_LONGA)
    inicio = time.perf_counter()
    taxa, audio = tts.sintetizar(frases[0])
    primeira = time.perf_counter() - inicio
    dur_audio = len(audio) / taxa if taxa else 0.0
    return {"voz": nome_voz, "primeira_frase_s": primeira, "audio_s": dur_audio, "rtf": primeira / max(dur_audio, 0.01)}


def principal() -> int:
    print("JARVIS 2.0 — bench_tts (Fase 2)")
    print("Comparando sintese (1a frase, sem playback):")
    resultados = []
    for voz in ["pt_BR-faber-medium", "pt_BR-edresson-low"]:
        try:
            r = medir_voz(voz)
        except Exception as erro:  # noqa: BLE001
            print(f"  {voz}: [ERRO] {erro}")
            continue
        resultados.append(r)
        print(f"  {voz}: sintese={r['primeira_frase_s']:.2f}s | audio={r['audio_s']:.2f}s | RTF={r['rtf']:.2f}")
    print("\nTocando frase longa na voz do config (aumente o volume!):")
    tts = TTS()
    print(f"Voz ativa: {tts.voz_nome}")
    inicio = time.perf_counter()
    # Mede até o 1º áudio PRONTO (sintese da 1ª frase) + toca tudo.
    frases = dividir_frases(FRASE_LONGA)
    taxa, audio = tts.sintetizar(frases[0])
    pronto = time.perf_counter() - inicio
    print(f"1a frase pronta em {pronto:.2f} s (meta: < 1 s)")
    tts._tocar(taxa, audio)
    for frase in frases[1:]:
        taxa2, audio2 = tts.sintetizar(frase)
        tts._tocar(taxa2, audio2)
    log = RAIZ / "logs" / "perf.log"
    for r in resultados:
        linha = (
            f"{datetime.datetime.now():%Y-%m-%d %H:%M:%S} | fase2 | voz={r['voz']} | "
            f"sintese_1a_frase_s={r['primeira_frase_s']:.2f} | audio_s={r['audio_s']:.2f}\n"
        )
        log.write_text(log.read_text(encoding="utf-8") + linha if log.exists() else linha, encoding="utf-8")
    print(f"Registrado em {log}")
    if pronto > 1.0:
        print("AVISO honesto: 1a frase acima de 1 s. Considere a voz low ou frases mais curtas.")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
