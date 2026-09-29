"""Skill screen_agent (Fase 6): controle de tela estilo UI-TARS.

DESATIVADA neste PC: 1 GB de VRAM não comporta modelo de visão decente.
Mantida como estrutura para hardware futuro (GPU >= 6 GB).
Toda chamada responde o aviso — nunca executa nada.
"""
from __future__ import annotations

from skills.base import Resultado, Skill

AVISO = (
    "Controle de tela desativado: este PC tem só 1 GB de VRAM, "
    "insuficiente para um modelo de visão. Use comandos de voz e skills."
)


class ScreenAgentSkill(Skill):
    """Agente visual de tela (DESATIVADO por falta de VRAM)."""

    nome = "screen_agent"
    descricao = "Controle visual da tela (desativado neste hardware)."
    ativada_por_padrao = False

    def run(self, **kwargs) -> Resultado:
        return Resultado(AVISO, ok=False)
