"""Testes da Fase 1 — cérebro (sem exigir Ollama rodando)."""
from __future__ import annotations

from pathlib import Path

import yaml

RAIZ = Path(__file__).resolve().parents[1]


def test_brain_importa_e_config():
    from core.brain import Brain, SYSTEM_PROMPT

    cerebro = Brain()
    assert cerebro.model == "qwen2.5:3b"
    assert 2048 <= cerebro.num_ctx <= 4096
    assert "português do Brasil" in SYSTEM_PROMPT
    assert "markdown" in SYSTEM_PROMPT.lower()
    assert cerebro.max_trocas == 8


def test_system_prompt_falado():
    from core.brain import SYSTEM_PROMPT

    baixo = SYSTEM_PROMPT.lower()
    assert "sem markdown" in baixo
    assert "sem emojis" in baixo
    assert "voz alta" in baixo or "lidas em voz" in baixo or "faladas" in baixo


def test_bench_existe():
    assert (RAIZ / "scripts" / "bench_brain.py").is_file()
    assert (RAIZ / "core" / "brain.py").is_file()
