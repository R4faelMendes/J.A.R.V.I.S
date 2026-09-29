"""Skill notes_reminders (Fase 6): anotações, lembretes e timers com alarme."""
from __future__ import annotations

import json
import threading
from pathlib import Path

from skills.base import Resultado, Skill

ARQUIVO = Path(__file__).resolve().parents[1] / "workspace" / "notas.json"


def _ler() -> dict:
    try:
        return json.loads(ARQUIVO.read_text(encoding="utf-8"))
    except Exception:
        return {"notas": [], "lembretes": []}


def _salvar(dados: dict) -> None:
    ARQUIVO.parent.mkdir(parents=True, exist_ok=True)
    ARQUIVO.write_text(json.dumps(dados, ensure_ascii=False, indent=2), encoding="utf-8")


class NotesSkill(Skill):
    """Guarda notas e agenda timers (o alarme chama o callback registrado)."""

    nome = "notes"
    descricao = "Notas e timers. Args: operacao (anotar|ler|timer), texto, segundos."
    parametros = {
        "operacao": {"type": "string"},
        "texto": {"type": "string"},
        "segundos": {"type": "integer"},
    }
    alarme: object = None  # função chamada no disparo (registrada pelo pipeline)

    def run(self, operacao: str = "anotar", texto: str = "",
            segundos: int = 0, **kwargs) -> Resultado:
        try:
            dados = _ler()
            if operacao == "anotar":
                if not texto:
                    return Resultado("Diga o que anotar.", ok=False)
                dados["notas"].append(texto)
                _salvar(dados)
                return Resultado("Anotei.")
            if operacao == "ler":
                if not dados["notas"]:
                    return Resultado("Nenhuma anotação guardada.")
                ultimas = dados["notas"][-3:]
                return Resultado("Últimas anotações: " + " ".join(ultimas) + ".")
            if operacao == "timer":
                seg = max(1, int(segundos or 0))
                msg = texto or "Tempo esgotado"
                threading.Timer(seg, self._disparar, args=(msg,)).start()
                return Resultado(f"Certo, aviso em {seg} segundos.")
            return Resultado(f"Operação desconhecida: {operacao}.", ok=False)
        except Exception as erro:  # noqa: BLE001
            return Resultado(f"Falha nas notas: {erro}.", ok=False)

    def _disparar(self, msg: str) -> None:
        try:
            if callable(self.alarme):
                self.alarme(f"Alarme: {msg}.")
        except Exception:
            pass
