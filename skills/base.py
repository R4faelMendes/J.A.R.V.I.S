"""Base das skills do JARVIS 2.0 (Fase 6) — estilo Leon/OVOS.

Cada skill: arquivo próprio em skills/, classe Skill com nome/descrição/
parâmetros/run(). O registrador carrega tudo automaticamente e expõe
as ferramentas ao LLM (formato Ollama) e ao roteador rápido.
Ações destrutivas exigem confirmação (ver skills/executor.py).
"""
from __future__ import annotations

import importlib
import pkgutil
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


@dataclass
class Resultado:
    """Retorno padrão de uma skill: texto falado + sucesso + dados extras."""

    resposta: str
    ok: bool = True
    dados: dict[str, Any] = field(default_factory=dict)


class Skill:
    """Contrato de toda skill. Herdar e implementar run()."""

    nome: str = "base"
    descricao: str = "Skill base (não usar diretamente)."
    # Parâmetros no formato JSON Schema simplificado p/ o LLM (somente leitura).
    parametros: dict[str, Any] = {}
    # True = pede confirmação (voz + botão) antes de executar.
    destrutiva: bool = False
    # Se False, a skill responde que está desligada (ex.: screen_agent).
    ativada_por_padrao: bool = True

    def run(self, **kwargs) -> Resultado:
        """Executa a ação. Sempre com try/except e mensagem clara."""
        raise NotImplementedError


def carregar_skills(pacote: str = "skills") -> dict[str, Skill]:
    """Importa todos os módulos de skills/ e instancia as classes Skill."""
    from skills import executor as _ex  # noqa: F401 - garante pacote válido

    registro: dict[str, Skill] = {}
    nome_pacote = pacote
    pkg = importlib.import_module(nome_pacote)
    for info in pkgutil.iter_modules(pkg.__path__):
        if info.name in {"base", "executor"}:
            continue
        try:
            modulo = importlib.import_module(f"{nome_pacote}.{info.name}")
        except Exception:
            continue
        for atributo in vars(modulo).values():
            if (
                isinstance(atributo, type)
                and issubclass(atributo, Skill)
                and atributo is not Skill
            ):
                try:
                    inst = atributo()
                    registro[inst.nome] = inst
                except Exception:
                    continue
    return registro


def esquema_ollama(skill: Skill) -> dict[str, Any]:
    """Converte uma skill no formato 'tools' da API /api/chat do Ollama."""
    return {
        "type": "function",
        "function": {
            "name": f"skill_{skill.nome}",
            "description": skill.descricao,
            "parameters": {
                "type": "object",
                "properties": skill.parametros or {},
            },
        },
    }
