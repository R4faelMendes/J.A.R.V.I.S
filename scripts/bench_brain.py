"""Benchmark do cérebro (Fase 1): mede primeiro token e tokens/s.

Uso:
    .\\.venv\\Scripts\\python.exe scripts/bench_brain.py
Registra também em logs/perf.log no formato: data | fase1 | primeiro_token_s | total_s | tok/s
"""
from __future__ import annotations

import asyncio
import datetime
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from core.brain import Brain  # noqa: E402


async def principal() -> int:
    print("JARVIS 2.0 — bench_brain (Fase 1)")
    cerebro = Brain()
    print(f"Modelo: {cerebro.model} | host: {cerebro.host}")
    print("Aquecendo e medindo (pergunta de teste)...")
    try:
        medida = await cerebro.aquecer()
    except Exception as erro:  # noqa: BLE001
        print(f"[ERRO] {erro}")
        return 1
    print(f"Primeiro token: {medida['primeiro_token_s']:.2f} s (meta: < 2 s com modelo carregado)")
    print(f"Total: {medida['total_s']:.2f} s | Ritmo: {medida['tokens_por_s']:.1f} tok/s")
    # Teste de streaming visível
    print("\nResposta de teste (streaming): ", end="", flush=True)
    async for evento in cerebro.chat("Diga olá em uma frase curta."):
        if "token" in evento:
            print(evento["token"], end="", flush=True)
        elif "erro" in evento:
            print(f"\n[ERRO] {evento['erro']}")
            return 1
    print()
    # Registra no perf.log
    log = RAIZ / "logs" / "perf.log"
    linha = (
        f"{datetime.datetime.now():%Y-%m-%d %H:%M:%S} | fase1 | "
        f"primeiro_token_s={medida['primeiro_token_s']:.2f} | "
        f"total_s={medida['total_s']:.2f} | toks_por_s={medida['tokens_por_s']:.1f}\n"
    )
    log.parent.mkdir(parents=True, exist_ok=True)
    log.write_text(log.read_text(encoding="utf-8") + linha if log.exists() else linha, encoding="utf-8")
    print(f"Registrado em {log}")
    if medida["primeiro_token_s"] > 2.0:
        print("AVISO honesto: acima da meta de 2 s. Tente qwen2.5:1.5b (Fase de desempenho).")
    return 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(principal()))
