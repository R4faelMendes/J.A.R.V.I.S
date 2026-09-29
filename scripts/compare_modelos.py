"""Comparador de modelos (Fase 1): qwen2.5:3b vs qwen2.5:1.5b.

Aquece cada modelo e mede primeiro token + tok/s com a MESMA pergunta em pt-BR.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/compare_modelos.py
"""
from __future__ import annotations

import asyncio
import sys
import time
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from core.brain import Brain  # noqa: E402

PERGUNTA = "Explique em duas frases curtas o que é um buraco negro."

# Sem acentos no print (console Windows cp850 quebra UTF-8).
PERGUNTA_LOG = "Explique em duas frases curtas o que e um buraco negro."


async def medir(modelo: str) -> dict:
    cerebro = Brain()
    cerebro.model = modelo
    # Aquecimento (carrega na RAM, descarta)
    async for _ in cerebro.chat("Responda so com: pronto."):
        pass
    # Medicao real
    inicio = time.perf_counter()
    primeiro = 0.0
    n = 0
    texto = ""
    async for evento in cerebro.chat(PERGUNTA):
        if "token" in evento:
            n += 1
            texto += evento["token"]
            if primeiro == 0.0:
                primeiro = time.perf_counter() - inicio
    total = time.perf_counter() - inicio
    return {
        "modelo": modelo,
        "primeiro_token_s": primeiro,
        "total_s": total,
        "eventos": n,
        "tok_s": (n / total) if total else 0.0,
        "resposta": texto,
    }


async def principal() -> int:
    print("Comparando modelos (pergunta PT, com aquecimento):")
    print(f"Pergunta: {PERGUNTA_LOG}\n")
    resultados = []
    for modelo in ["qwen2.5:3b", "qwen2.5:1.5b"]:
        print(f"--- {modelo} ---")
        try:
            r = await medir(modelo)
        except Exception as erro:  # noqa: BLE001
            print(f"[ERRO] {erro}")
            continue
        resultados.append(r)
        print(f"Primeiro token: {r['primeiro_token_s']:.2f} s | Total: {r['total_s']:.2f} s | {r['tok_s']:.1f} tok/s")
        print(f"Resposta: {r['resposta']}\n")
    if len(resultados) == 2:
        rapido = min(resultados, key=lambda r: r["primeiro_token_s"])
        print(f"Mais rapido no primeiro token: {rapido['modelo']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(principal()))
