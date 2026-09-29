"""Skill smart_home (Fase 6): Home Assistant via API REST local. DESLIGADA por padrão.

Para ativar: config.yaml -> skills.smart_home: true + preencher [smart_home]
url/token abaixo via variáveis de ambiente HASS_URL / HASS_TOKEN.
Sem isso, responde o aviso e não faz nada (seguro).
"""
from __future__ import annotations

import os

from skills.base import Resultado, Skill


class SmartHomeSkill(Skill):
    """Integração opcional com Home Assistant local (desligada por padrão)."""

    nome = "smart_home"
    descricao = "Casa inteligente (requer Home Assistant configurado). Args: entidade, acao."
    parametros = {"entidade": {"type": "string"}, "acao": {"type": "string"}}
    ativada_por_padrao = False

    def run(self, entidade: str = "", acao: str = "", **kwargs) -> Resultado:
        url = os.environ.get("HASS_URL", "")
        token = os.environ.get("HASS_TOKEN", "")
        if not url or not token:
            return Resultado(
                "Casa inteligente desligada. Configure HASS_URL e HASS_TOKEN para ativar.",
                ok=False)
        try:
            import httpx

            dominio = (entidade or "").split(".")[0]
            r = httpx.post(
                f"{url}/api/services/{dominio}/{acao or 'toggle'}",
                headers={"Authorization": f"Bearer {token}"},
                json={"entity_id": entidade}, timeout=10)
            if r.status_code in (200, 201):
                return Resultado(f"Comando enviado para {entidade}.")
            return Resultado(f"O Home Assistant recusou ({r.status_code}).", ok=False)
        except Exception as erro:  # noqa: BLE001
            return Resultado(f"Falha na casa inteligente: {erro}.", ok=False)
