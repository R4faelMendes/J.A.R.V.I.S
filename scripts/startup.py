"""Liga/desliga o início com o Windows (Fase 9): atalho em shell:startup.

Lê config.yaml -> startup.iniciar_com_windows. Aponta para start_silencioso.vbs.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/startup.py
"""
from __future__ import annotations

import subprocess
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]


def principal() -> int:
    import yaml

    cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
    ligar = bool(cfg.get("startup", {}).get("iniciar_com_windows", False))
    ps = (
        "$s=(New-Object -ComObject WScript.Shell).SpecialFolders('Startup');"
        f"$lnk=Join-Path $s 'JARVIS 2.0.lnk';"
    )
    if ligar:
        alvo = str(RAIZ / "start_silencioso.vbs")
        ps += (f"$w=New-Object -ComObject WScript.Shell;$a=$w.CreateShortcut($lnk);"
               f"$a.TargetPath='{alvo}';$a.WorkingDirectory='{RAIZ}';$a.Save();'OK: atalho criado'")
    else:
        ps += "if(Test-Path $lnk){Remove-Item $lnk -Force;'OK: atalho removido'}else{'OK: inicio automatico desligado'}"
    r = subprocess.run(["powershell", "-NoProfile", "-Command", ps],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    print((r.stdout or r.stderr or "").strip())
    return 0 if r.returncode == 0 else 1


if __name__ == "__main__":
    raise SystemExit(principal())
