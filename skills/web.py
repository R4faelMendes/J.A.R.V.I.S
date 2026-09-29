"""Skill web (Fase 6): pesquisa DuckDuckGo (sem chave) + resumo falado curto."""
from __future__ import annotations

from skills.base import Resultado, Skill


class WebSkill(Skill):
    """Pesquisa na web e resume em voz curta (3 resultados, 1 frase cada)."""

    nome = "web"
    descricao = "Pesquisar na web. Args: consulta (texto)."
    parametros = {"consulta": {"type": "string"}}

    def run(self, consulta: str = "", **kwargs) -> Resultado:
        consulta = (consulta or "").strip()
        if not consulta:
            return Resultado("Diga o que pesquisar.", ok=False)
        try:
            try:
                from ddgs import DDGS  # nome atual (duckduckgo-search foi renomeado)
            except ImportError:
                from duckduckgo_search import DDGS  # reserva legada
        except Exception as erro:
            return Resultado(f"Pesquisa indisponível (pacote ddgs: {erro}).", ok=False)
        try:
            with DDGS() as buscador:
                achados = list(buscador.text(consulta, region="br-pt", max_results=3))
        except Exception as erro:  # noqa: BLE001 - API pode mudar; mensagem clara
            return Resultado(f"Não consegui pesquisar agora: {erro}.", ok=False)
        if not achados:
            return Resultado(f"Nada encontrado sobre {consulta}.", ok=True)
        partes = []
        for a in achados:
            titulo = (a.get("title") or "")[:60]
            corpo = (a.get("body") or "").split(".")[0][:140]
            partes.append(f"{titulo}: {corpo}".strip())
        resumo = " ".join(partes)
        return Resultado(f"Sobre {consulta}: {resumo}", dados={"resultados": achados})
