"""Testes da Fase 8 — celular (cert autoassinado + PWA + PIN)."""
from __future__ import annotations

from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]


def test_cert_gerado_com_san():
    from scripts.gerar_cert import ip_local, principal

    assert principal() == 0
    cert = (RAIZ / "models" / "cert.pem").read_bytes()
    assert b"BEGIN CERTIFICATE" in cert
    chave = (RAIZ / "models" / "key.pem").read_bytes()
    assert b"PRIVATE KEY" in chave
    assert ip_local().count(".") == 3


def test_pwa_arquivos():
    assert (RAIZ / "ui" / "manifest.json").is_file()
    assert (RAIZ / "ui" / "sw.js").is_file()
    assert (RAIZ / "ui" / "icone-192.png").is_file()
    assert (RAIZ / "ui" / "icone-512.png").is_file()
    html = (RAIZ / "ui" / "index.html").read_text(encoding="utf-8")
    assert "manifest" in html and "btn-mic" in html


def test_pin_bloqueia_fora_do_localhost():
    import yaml

    from core.server import pin_para_host

    cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
    pin = str(cfg.get("servidor", {}).get("pin", "0000"))
    assert pin_para_host("127.0.0.1", "") is True
    assert pin_para_host("192.168.1.50", "") is False
    assert pin_para_host("192.168.1.50", "errado") is False
    assert pin_para_host("192.168.1.50", pin) is True
