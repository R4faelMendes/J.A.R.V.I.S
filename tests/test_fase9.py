"""Testes da Fase 9 — acabamento (inicializadores, config completa, freeze)."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import yaml

RAIZ = Path(__file__).resolve().parents[1]


def test_inicializadores():
    assert (RAIZ / "start.bat").is_file()
    assert (RAIZ / "start_silencioso.vbs").is_file()
    bat = (RAIZ / "start.bat").read_text(encoding="utf-8")
    assert "uvicorn core.server:app" in bat and "msedge" in bat


def test_startup_script():
    r = subprocess.run([sys.executable, str(RAIZ / "scripts" / "startup.py")],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    assert r.returncode == 0 and "OK" in (r.stdout or "")


def test_config_completo():
    cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
    for secao in ["geral", "cerebro", "audio", "stt", "vad", "wakeword", "tts",
                  "roteador_rapido", "seguranca", "skills", "servidor",
                  "interface", "logs", "startup"]:
        assert secao in cfg, f"seção ausente: {secao}"
    assert cfg["skills"]["screen_agent"] is False
    assert cfg["servidor"]["porta"] == 8765


def test_requirements_congelado():
    linhas = (RAIZ / "requirements.txt").read_text(encoding="utf-8-sig").splitlines()
    fixadas = [l for l in linhas if "==" in l and not l.strip().startswith("#")]
    assert len(fixadas) >= 25, f"poucas versões fixadas: {len(fixadas)}"
