"""Entrada principal do JARVIS 2.0 (Fase 5+).

Modos:
    python scripts/jarvis.py --texto "que horas são"   # turno único em texto
    python scripts/jarvis.py                            # conversa em texto (sem mic)
    python scripts/jarvis.py --voz                      # loop de voz (precisa do mic)
    python scripts/jarvis.py --sem-voz --texto "..."    # sem tocar áudio
Uso no venv: .\\.venv\\Scripts\\python.exe scripts/jarvis.py
"""
from __future__ import annotations

import argparse
import asyncio
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from core.pipeline import JarvisPipeline  # noqa: E402


async def turno_unico(texto: str, falar: bool) -> int:
    print("Aquecendo modelos (uma vez só)...")
    j = JarvisPipeline()
    tempos = await j.aquecer_tudo()
    print("Pronto: " + ", ".join(f"{k}={v:.1f}s" for k, v in tempos.items()))
    r = await j.processar_texto(texto, falar=falar)
    print(f"[{r['origem']}] Jarvis: {r['resposta']}")
    if j._pendente is not None:
        print("(aguardando confirmação: responda 'confirmo' para executar)")
    print(f"Primeiro áudio em {r.get('primeiro_audio_s', r.get('primeiro_audio', 0)):.2f} s")
    return 0


async def conversa_texto(falar: bool) -> int:
    j = JarvisPipeline()
    print("Aquecendo modelos (uma vez só)...")
    await j.aquecer_tudo()
    print("Conversa em texto. Digite 'sair' para encerrar.")
    while True:
        try:
            texto = input("Você: ").strip()
        except (EOFError, KeyboardInterrupt):
            break
        if texto.lower() in {"sair", "exit", "quit"}:
            break
        if not texto:
            continue
        r = await j.processar_texto(texto, falar=falar)
        print(f"Jarvis: {r['resposta']}")
    return 0


def principal() -> int:
    ap = argparse.ArgumentParser(description="JARVIS 2.0 — assistente local")
    ap.add_argument("--texto", default=None, help="turno único em texto")
    ap.add_argument("--voz", action="store_true", help="loop de voz (precisa do mic)")
    ap.add_argument("--sem-voz", action="store_true", help="não toca áudio")
    args = ap.parse_args()
    falar = not args.sem_voz
    if args.voz:
        j = JarvisPipeline()
        asyncio.run(j.aquecer_tudo())
        j.rodar_voz()
        return 0
    if args.texto:
        return asyncio.run(turno_unico(args.texto, falar))
    return asyncio.run(conversa_texto(falar))


if __name__ == "__main__":
    raise SystemExit(principal())
