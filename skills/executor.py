"""Executor das skills (Fase 6): liga roteador -> skills com segurança.

- Respeita config.yaml -> skills (ligadas/desligadas).
- Ações destrutivas sem confirmar=True voltam como pedido de confirmação.
- Tudo que executa é registrado em logs/exec.log (regra 5 da missão).
- ferramentas_llm(): esquemas p/ o Brain usar as skills como tools.
"""
from __future__ import annotations

import datetime
from pathlib import Path
from typing import Any

import yaml

from skills.base import Resultado, Skill, carregar_skills, esquema_ollama

RAIZ = Path(__file__).resolve().parents[1]

# Router (metodo) -> skill real.
MAPA_METODOS = {
    "abrir": ("apps", "abrir"), "fechar": ("apps", "fechar"),
    "volume": ("system", "volume"), "volume_mais": ("system", "volume_mais"),
    "volume_menos": ("system", "volume_menos"), "mudo": ("system", "mudo"),
    "desmudo": ("system", "desmudo"), "bloquear": ("system", "bloquear"),
    "status": ("system", "status"),
    "tocar": ("media", "tocar"), "pausar": ("media", "pausar"),
    "proxima": ("media", "proxima"), "anterior": ("media", "anterior"),
    "timer": ("notes", "timer"),
}


class Executor:
    """Carrega skills, verifica permissão e executa com log."""

    def __init__(self, config_path: Path | str | None = None) -> None:
        caminho = Path(config_path) if config_path else RAIZ / "config.yaml"
        try:
            cfg = yaml.safe_load(caminho.read_text(encoding="utf-8"))
        except Exception:
            cfg = {}
        self.flags: dict[str, bool] = cfg.get("skills", {})
        self.skills: dict[str, Skill] = carregar_skills()

    def ativada(self, nome: str) -> bool:
        """Config manda; padrão é o da skill."""
        skill = self.skills.get(nome)
        padrao = skill.ativada_por_padrao if skill else True
        return bool(self.flags.get(nome, padrao))

    def executar(self, acao: dict[str, Any], confirmar: bool = False) -> Resultado:
        """Executa {'skill','metodo','args'}. Sem confirmar, destrutiva pede."""
        nome = acao.get("skill", "")
        metodo = acao.get("metodo", "")
        args = dict(acao.get("args", {}))
        skill = self.skills.get(nome)
        if skill is None:
            r = Resultado(f"Skill desconhecida: {nome}.", ok=False)
            self._log(nome, metodo, args, r)
            return r
        if not self.ativada(nome):
            r = Resultado(f"A skill {nome} está desligada no config.", ok=False)
            self._log(nome, metodo, args, r)
            return r
        # Mapeia operações do roteador para os run() reais.
        if nome in {"apps", "system", "media", "notes"} and metodo != "_direto":
            chave = MAPA_METODOS.get(metodo, (nome, metodo))
            _sn, metodo_real = chave
            args = self._args_para_skill(nome, metodo, args)
            return self._chamar(skill, metodo_real, args, confirmar)
        return self._chamar(skill, metodo, args, confirmar, direto=True)

    def _args_para_skill(self, nome: str, metodo: str, args: dict) -> dict:
        if nome == "apps":
            return {"operacao": metodo, "alvo": args.get("alvo", "")}
        if nome == "system":
            return {"metodo": metodo, "nivel": args.get("nivel")}
        if nome == "media":
            return {"metodo": metodo}
        if nome == "notes":
            return {"operacao": "timer", "segundos": args.get("segundos", 0)}
        return args

    def _chamar(self, skill: Skill, metodo: str, args: dict,
                confirmar: bool, direto: bool = False) -> Resultado:
        if skill.destrutiva and not confirmar:
            args["confirmar"] = False
            previo = self._rodar(skill, metodo, args, direto)
            if previo.dados.get("precisa_confirmar"):
                return previo  # pede "confirmo" sem executar nada perigoso
        args["confirmar"] = True
        r = self._rodar(skill, metodo, args, direto)
        self._log(skill.nome, metodo, args, r)
        return r

    def _rodar(self, skill: Skill, metodo: str, args: dict, direto: bool) -> Resultado:
        try:
            if direto and metodo and hasattr(skill, "run"):
                return skill.run(**args)
            return skill.run(**args)
        except TypeError:
            try:  # compat: skills que só aceitam operacao/metodo
                return skill.run(**args)
            except Exception as erro:  # noqa: BLE001
                return Resultado(f"Falha na skill {skill.nome}: {erro}.", ok=False)
        except Exception as erro:  # noqa: BLE001
            return Resultado(f"Falha na skill {skill.nome}: {erro}.", ok=False)

    def _log(self, skill: str, metodo: str, args: dict, r: Resultado) -> None:
        linha = (
            f"{datetime.datetime.now():%Y-%m-%d %H:%M:%S} | skill={skill} metodo={metodo} "
            f"args={str(args)[:200]} ok={r.ok} resp={r.resposta[:120]}\n"
        )
        log = RAIZ / "logs" / "exec.log"
        log.write_text((log.read_text(encoding="utf-8") if log.exists() else "") + linha,
                       encoding="utf-8")

    def ferramentas_llm(self) -> list[dict[str, Any]]:
        """Esquemas das skills ATIVADAS para o Brain (tool calling)."""
        return [esquema_ollama(s) for n, s in self.skills.items() if self.ativada(n)]
