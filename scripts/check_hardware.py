"""Verificação de hardware do JARVIS 2.0 (Fase 0).

Imprime CPU, RAM, GPU, disco, microfones e alto-falantes,
e confirma o perfil "CPU leve" (sem GPU útil para IA).
Uso:
    .\\.venv\\Scripts\\python.exe scripts/check_hardware.py
Comentários em português, pathlib, utf-8.
"""
from __future__ import annotations

import platform
import shutil
import subprocess
import sys
from pathlib import Path


def executar_ps(comando: str) -> str:
    """Executa um comando PowerShell e retorna a saída como texto."""
    try:
        resultado = subprocess.run(
            ["powershell", "-NoProfile", "-Command", comando],
            capture_output=True,
            text=True,
            encoding="utf-8",
            timeout=30,
        )
        return (resultado.stdout or "").strip()
    except Exception as erro:  # noqa: BLE001 - mensagem clara ao usuário
        return f"[erro ao consultar hardware: {erro}]"


def secao(titulo: str) -> None:
    print(f"\n=== {titulo} ===")


def main() -> int:
    print("JARVIS 2.0 — check_hardware (Fase 0)")
    print(f"Python: {sys.version.split()[0]} | {sys.executable}")
    print(f"Sistema: {platform.system()} {platform.release()} ({platform.machine()})")
    print(f"Pasta do projeto: {Path.cwd()}")

    secao("CPU")
    cpu = executar_ps(
        "Get-CimInstance Win32_Processor | "
        "Select-Object Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed | "
        "Format-List | Out-String"
    )
    print(cpu or "[CPU não detectada]")

    secao("RAM")
    ram = executar_ps(
        "$m=(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory; "
        "\"Total: {0:N2} GB ({1} bytes)\" -f ($m/1GB), $m"
    )
    print(ram or "[RAM não detectada]")

    secao("GPU")
    gpu = executar_ps(
        "Get-CimInstance Win32_VideoController | "
        "Select-Object Name,AdapterRAM | Format-List | Out-String"
    )
    print(gpu or "[GPU não detectada]")
    print("-> Perfil esperado: GPU com 1 GB de VRAM = INÚTIL para IA. Tudo na CPU.")

    secao("Disco C:")
    disco = executar_ps("wmic logicaldisk where \"DeviceID='C:'\" get Size,FreeSpace")
    print(disco or "[disco não detectado]")

    secao("Dispositivos de som (WMI)")
    som = executar_ps("wmic sounddev get Name,Status")
    print(som or "[áudio não detectado]")

    secao("Áudio via sounddevice (se instalado)")
    try:
        import sounddevice as sd

        print(sd.query_devices())
    except ImportError:
        print("[sounddevice ainda não instalado — será instalado na Fase 3]")
        print("Se o microfone não aparecer depois, vá em:")
        print("Configurações > Privacidade > Microfone > permitir apps da área de trabalho.")
    except Exception as erro:  # noqa: BLE001
        print(f"[erro no sounddevice: {erro}]")

    secao("Espaço do projeto")
    total, usado, livre = shutil.disk_usage(Path.cwd())
    print(f"Total: {total/1e9:.1f} GB | Livre: {livre/1e9:.1f} GB")

    secao("Veredito")
    print("Perfil 'CPU leve' CONFIRMADO se: Windows 10, ~18 GB RAM, CPU 4 núcleos, GPU <= 1 GB.")
    print("Fase 0 OK: microfone + alto-falante precisam aparecer acima (WMI ou sounddevice).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
