"""Skill code_runner (Fase 6): gera contexto p/ código e executa com SEGURANÇA.

- Execução isolada na pasta workspace/, com timeout de 30 s.
- SEMPRE pede confirmação antes de executar (mostra o código primeiro).
- Bloqueia comandos da lista de proibidos (config.yaml -> seguranca).
"""
from __future__ import annotations

import subprocess
import tempfile
from pathlib import Path

import yaml

from skills.base import Resultado, Skill

RAIZ = Path(__file__).resolve().parents[1]


def _proibidos() -> list[str]:
    try:
        cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
        return [str(x).lower() for x in cfg.get("seguranca", {}).get("comandos_proibidos", [])]
    except Exception:
        return []


class CodeRunnerSkill(Skill):
    """Executa Python/PowerShell isolado (sempre com confirmação prévia)."""

    nome = "code_runner"
    descricao = (
        "Executa código Python ou PowerShell na pasta isolada. "
        "Args: linguagem (python|powershell), codigo. Exige confirmar=True."
    )
    parametros = {
        "linguagem": {"type": "string"},
        "codigo": {"type": "string"},
    }
    destrutiva = True

    def run(self, linguagem: str = "python", codigo: str = "",
            confirmar: bool = False, **kwargs) -> Resultado:
        codigo = (codigo or "").strip()
        if not codigo:
            return Resultado("Nenhum código para executar.", ok=False)
        for proibido in _proibidos():
            if proibido and proibido in codigo.lower():
                return Resultado(
                    f"Bloqueado por segurança: o código contém '{proibido}'.", ok=False)
        if not confirmar:
            previa = codigo[:300] + ("..." if len(codigo) > 300 else "")
            return Resultado(
                f"Vou executar este código em {linguagem}: {previa} Diga 'confirmo' para rodar.",
                ok=False, dados={"precisa_confirmar": True, "codigo": codigo, "linguagem": linguagem},
            )
        return self._executar(linguagem, codigo)

    def _executar(self, linguagem: str, codigo: str) -> Resultado:
        ws = RAIZ / "workspace"
        ws.mkdir(exist_ok=True)
        try:
            if linguagem == "powershell":
                proc = subprocess.run(
                    ["powershell", "-NoProfile", "-Command", codigo],
                    capture_output=True, text=True, encoding="utf-8",
                    timeout=30, cwd=str(ws))
            else:
                with tempfile.NamedTemporaryFile("w", suffix=".py", delete=False,
                                                 dir=str(ws), encoding="utf-8") as f:
                    f.write(codigo)
                    tmp = f.name
                proc = subprocess.run(
                    [str(RAIZ / ".venv" / "Scripts" / "python.exe"), tmp],
                    capture_output=True, text=True, encoding="utf-8",
                    timeout=30, cwd=str(ws))
                Path(tmp).unlink(missing_ok=True)
        except subprocess.TimeoutExpired:
            return Resultado("O código passou de 30 segundos e foi interrompido.", ok=False)
        except Exception as erro:  # noqa: BLE001
            return Resultado(f"Falha ao executar: {erro}.", ok=False)
        saida = (proc.stdout or "")[-800:] or "(sem saída)"
        if proc.returncode != 0:
            erro_txt = (proc.stderr or "")[-500:]
            return Resultado(f"Erro na execução: {erro_txt or saida}", ok=False)
        return Resultado(f"Resultado: {saida}", dados={"saida": saida})
