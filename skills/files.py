"""Skill files (Fase 6): arquivos e pastas com TRAVA DE SEGURANÇA.

- Tudo acontece dentro da pasta de trabalho (workspace/) por padrão.
- Apagar vai para a LIXEIRA (send2trash, nunca delete direto) e EXIGE confirmação.
- Caminhos fora do workspace exigem confirmação explícita.
"""
from __future__ import annotations

import shutil
from pathlib import Path

from send2trash import send2trash

from skills.base import Resultado, Skill

RAIZ = Path(__file__).resolve().parents[1]
WORKSPACE = RAIZ / "workspace"


class FilesSkill(Skill):
    """Listar, criar, mover e organizar arquivos (apagar só com confirmação)."""

    nome = "files"
    descricao = (
        "Arquivos: listar (caminho), criar (caminho, conteudo), mover (origem, destino), "
        "organizar (caminho), apagar (caminho, exige confirmar=True)."
    )
    parametros = {
        "operacao": {"type": "string"},
        "caminho": {"type": "string"},
        "destino": {"type": "string"},
        "conteudo": {"type": "string"},
    }
    destrutiva = True  # o executor pede confirmação p/ apagar

    def _resolver(self, caminho: str) -> tuple[Path, bool]:
        """Retorna (caminho absoluto, fora_do_workspace)."""
        p = Path(caminho or ".")
        if not p.is_absolute():
            p = WORKSPACE / p
        return p.resolve(), WORKSPACE.resolve() not in p.resolve().parents and p.resolve() != WORKSPACE.resolve()

    def run(self, operacao: str = "listar", caminho: str = ".", destino: str = "",
            conteudo: str = "", confirmar: bool = False, **kwargs) -> Resultado:
        try:
            alvo, fora = self._resolver(caminho)
            if operacao == "apagar" and not confirmar:
                return Resultado(
                    f"Para apagar {alvo.name} diga 'confirmo'. Vai para a lixeira.",
                    ok=False, dados={"precisa_confirmar": True},
                )
            if fora and operacao in {"apagar", "mover"} and not confirmar:
                return Resultado(
                    f"{alvo} está fora da área de trabalho. Diga 'confirmo'.",
                    ok=False, dados={"precisa_confirmar": True},
                )
            if operacao == "listar":
                if not alvo.is_dir():
                    return Resultado(f"Pasta não encontrada: {alvo}.", ok=False)
                itens = sorted(x.name for x in alvo.iterdir())
                if not itens:
                    return Resultado("Pasta vazia.")
                return Resultado(f"Encontrei {len(itens)} itens: {', '.join(itens[:15])}.",
                                 dados={"itens": itens})
            if operacao == "criar":
                WORKSPACE.mkdir(exist_ok=True)
                alvo.parent.mkdir(parents=True, exist_ok=True)
                alvo.write_text(conteudo or "", encoding="utf-8")
                return Resultado(f"Criei {alvo.name}.")
            if operacao == "mover":
                dest, _f = self._resolver(destino)
                if not alvo.exists():
                    return Resultado(f"Não achei {alvo.name}.", ok=False)
                dest.parent.mkdir(parents=True, exist_ok=True)
                shutil.move(str(alvo), str(dest))
                return Resultado(f"Movi {alvo.name} para {dest.parent.name}.")
            if operacao == "organizar":
                if not alvo.is_dir():
                    return Resultado(f"Pasta não encontrada: {alvo}.", ok=False)
                movidos = 0
                for arq in alvo.iterdir():
                    if arq.is_file() and arq.suffix:
                        pasta = alvo / arq.suffix.lstrip(".").lower()
                        pasta.mkdir(exist_ok=True)
                        shutil.move(str(arq), str(pasta / arq.name))
                        movidos += 1
                return Resultado(f"Organizei {movidos} arquivos por tipo.")
            if operacao == "apagar":
                if not alvo.exists():
                    return Resultado(f"Não achei {alvo.name}.", ok=False)
                send2trash(str(alvo))
                return Resultado(f"Enviei {alvo.name} para a lixeira.")
            return Resultado(f"Operação desconhecida: {operacao}.", ok=False)
        except Exception as erro:  # noqa: BLE001
            return Resultado(f"Falha em arquivos ({operacao}): {erro}.", ok=False)
