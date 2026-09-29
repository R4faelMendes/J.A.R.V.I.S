"""Benchmark fim-a-fim (Fase 5): comando rápido (roteador) x conversa (LLM).

Sem microfone: entrada em texto, mede até o 1º áudio PRONTO + toca a resposta.
Metas: <1 s p/ comando rápido; ~2-3 s p/ LLM (fim da fala -> 1º áudio).
Uso:
    .\\.venv\\Scripts\\python.exe scripts/bench_pipeline.py
"""
from __future__ import annotations

import asyncio
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from core.pipeline import JarvisPipeline  # noqa: E402


async def principal() -> int:
    print("JARVIS 2.0 — bench_pipeline (Fase 5)")
    j = JarvisPipeline()
    print("Aquecendo ( Whisper + Piper + wake + LLM )...")
    tempos = await j.aquecer_tudo()
    print("Aquecimento: " + ", ".join(f"{k}={v:.1f}s" for k, v in tempos.items()))

    print("\n[1] Comando rápido (roteador, sem LLM): 'que horas são?' (2x: frio e aquecido)")
    r1 = await j.processar_texto("que horas são?", falar=False)
    print(f"    Resposta: {r1['resposta']}")
    print(f"    1º áudio pronto em {r1['primeiro_audio_s']:.2f} s (frio; meta aquecido: < 1 s)")
    r1b = await j.processar_texto("que horas são?", falar=False)
    print(f"    1º áudio pronto em {r1b['primeiro_audio_s']:.2f} s (aquecido; meta: < 1 s)")

    print("\n[2] Conversa (LLM + streaming): 'diga oi em uma frase curta'")
    r2 = await j.processar_texto("diga oi em uma frase curta", falar=False)
    print(f"    Resposta: {r2['resposta']}")
    print(f"    1º token em {r2.get('primeiro_token', 0):.2f} s | 1º áudio em {r2.get('primeiro_audio', 0):.2f} s (meta: 2-3 s)")

    ok1 = r1b["primeiro_audio_s"] < 1.0
    ok2 = (r2.get("primeiro_audio", 99)) < 4.0
    print(f"\nResultado: roteador {'OK' if ok1 else 'ACIMA'} (frio: {r1['primeiro_audio_s']:.2f}s) | LLM {'OK' if ok2 else 'ACIMA'}")
    return 0 if (ok1 and ok2) else 2


if __name__ == "__main__":
    raise SystemExit(asyncio.run(principal()))
