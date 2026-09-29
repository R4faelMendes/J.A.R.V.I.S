"""Testes da Fase 0 — estrutura, config e hardware."""
from __future__ import annotations

import sys
from pathlib import Path

import yaml

RAIZ = Path(__file__).resolve().parents[1]


def test_python_versao():
    assert sys.version_info[:2] in [(3, 11), (3, 12)], f"Use Python 3.11/3.12, atual: {sys.version}"


def test_estrutura_pastas():
    for pasta in ["core", "skills", "voice", "ui", "models", "logs", "tests", "scripts"]:
        assert (RAIZ / pasta).is_dir(), f"pasta ausente: {pasta}"


def test_arquivos_base():
    for arq in ["requirements.txt", "config.yaml", "README.md", "scripts/check_hardware.py"]:
        assert (RAIZ / arq).is_file(), f"arquivo ausente: {arq}"


def test_config_yaml_valido():
    cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
    assert cfg["cerebro"]["modelo_principal"] == "qwen2.5:3b"
    assert cfg["skills"]["screen_agent"] is False  # desativada por falta de VRAM
    assert cfg["wakeword"]["framework"] == "onnx"
