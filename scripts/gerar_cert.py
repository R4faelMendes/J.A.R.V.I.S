"""Gera certificado autoassinado local (Fase 8): HTTPS só p/ rede local.

O navegador do celular só libera o microfone em HTTPS ou localhost.
Gera models/cert.pem + models/key.pem com o IP local no SAN.
No celular, aceite o aviso de certificado na primeira vez.
NUNCA exponha à internet; sem redirecionamento de portas no roteador.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/gerar_cert.py
"""
from __future__ import annotations

import datetime
import ipaddress
import socket
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]


def ip_local() -> str:
    """Descobre o IP da rede local (sem enviar nada para fora)."""
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(("192.168.1.1", 1))
        return s.getsockname()[0]
    except Exception:
        return "127.0.0.1"
    finally:
        s.close()


def principal() -> int:
    from cryptography import x509
    from cryptography.hazmat.primitives import hashes, serialization
    from cryptography.hazmat.primitives.asymmetric import rsa
    from cryptography.x509.oid import NameOID

    ip = ip_local()
    chave = rsa.generate_private_key(public_exponent=65537, key_size=2048)
    assunto = emissor = x509.Name([x509.NameAttribute(NameOID.COMMON_NAME, f"JARVIS-local-{ip}")])
    agora = datetime.datetime.now(datetime.timezone.utc)
    cert = (
        x509.CertificateBuilder()
        .subject_name(assunto).issuer_name(emissor)
        .public_key(chave.public_key()).serial_number(x509.random_serial_number())
        .not_valid_before(agora).not_valid_after(agora + datetime.timedelta(days=825))
        .add_extension(x509.SubjectAlternativeName(
            [x509.DNSName("localhost"), x509.IPAddress(ipaddress.ip_address(ip)),
             x509.IPAddress(ipaddress.ip_address("127.0.0.1"))]), critical=False)
        .sign(chave, hashes.SHA256())
    )
    (RAIZ / "models").mkdir(exist_ok=True)
    (RAIZ / "models" / "key.pem").write_bytes(chave.private_bytes(
        serialization.Encoding.PEM, serialization.PrivateFormat.TraditionalOpenSSL,
        serialization.NoEncryption()))
    (RAIZ / "models" / "cert.pem").write_bytes(cert.public_bytes(serialization.Encoding.PEM))
    print(f"Certificado gerado para IP {ip} em models/cert.pem + models/key.pem")
    print("No celular: abra https://<este-IP>:8765 e aceite o aviso de certificado.")
    return 0


if __name__ == "__main__":
    raise SystemExit(principal())
