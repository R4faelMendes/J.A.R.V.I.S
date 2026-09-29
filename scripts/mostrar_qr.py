"""Mostra o QR de acesso do celular (Fase 8): URL https + PIN.

Uso:
    .\\.venv\\Scripts\\python.exe scripts/mostrar_qr.py
"""
from __future__ import annotations

import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from scripts.gerar_cert import ip_local  # noqa: E402


def principal() -> int:
    import yaml

    import qrcode

    cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
    srv = cfg.get("servidor", {})
    url = f"https://{ip_local()}:{srv.get('porta', 8765)}?pin={srv.get('pin', '0000')}"
    print(f"URL do celular: {url}")
    print(f"PIN: {srv.get('pin', '0000')}")
    qr = qrcode.QRCode(border=1)
    qr.add_data(url)
    try:
        qr.print_ascii(invert=True)
    except UnicodeEncodeError:
        print("(QR no terminal indisponível neste console; veja models/qr-celular.png)")
    qr.make_image().save(RAIZ / "models" / "qr-celular.png")
    print("QR salvo em models/qr-celular.png — abra a imagem e escaneie (mesmo Wi-Fi).")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
