"""Cérebro do JARVIS 2.0 (Fase 1) — cliente assíncrono do Ollama com streaming.

- Histórico curto (últimas ~8 trocas).
- System prompt em pt-BR: educado, direto, levemente espirituoso,
  respostas CURTAS e FALADAS (sem markdown, sem emojis, sem listas).
- Tool calling no formato da API /api/chat do Ollama.
- Config CPU: keep_alive longo, num_ctx 2048-4096, num_predict limitado.
Uso:
    from core.brain import Brain
    brain = Brain()
    async for token in brain.chat("que horas são?"):
        print(token, end="", flush=True)
"""
from __future__ import annotations

import json
import time
from pathlib import Path
from typing import Any, AsyncIterator

import httpx
import yaml

RAIZ = Path(__file__).resolve().parents[1]

SYSTEM_PROMPT = (
    "Você é o Jarvis, assistente pessoal do usuário, falando português do Brasil. "
    "Seja educado, direto e levemente espirituoso. "
    "Suas respostas serão LIDAS EM VOZ ALTA, então: frases curtas e coloquiais, "
    "sem markdown, sem emojis, sem listas, sem código a menos que peçam. "
    "No máximo 2 a 3 frases por resposta, a menos que o usuário peça detalhes. "
    "Se não souber, diga que não sabe e ofereça ajuda. "
    "Nunca invente comandos, pacotes ou URLs."
)


class Brain:
    """Cliente assíncrono do Ollama com streaming e histórico curto."""

    def __init__(self, config_path: Path | str | None = None) -> None:
        caminho = Path(config_path) if config_path else RAIZ / "config.yaml"
        try:
            cfg = yaml.safe_load(caminho.read_text(encoding="utf-8"))
        except Exception as erro:
            raise RuntimeError(f"Não li {caminho}: {erro}") from erro
        cerebro = cfg.get("cerebro", {})
        self.host: str = cerebro.get("host", "http://localhost:11434")
        self.model: str = cerebro.get("modelo_principal", "qwen2.5:3b")
        self.num_ctx: int = int(cerebro.get("num_ctx", 4096))
        self.num_predict: int = int(cerebro.get("num_predict", 256))
        self.keep_alive: str = str(cerebro.get("keep_alive", "24h"))
        self.max_trocas: int = int(cerebro.get("historico_trocas", 8))
        self._historico: list[dict[str, str]] = []

    def limpar_historico(self) -> None:
        """Apaga o histórico de conversa."""
        self._historico.clear()

    def _mensagens(self, texto: str) -> list[dict[str, str]]:
        return [{"role": "system", "content": SYSTEM_PROMPT}, *self._historico, {"role": "user", "content": texto}]

    async def chat(
        self,
        texto: str,
        tools: list[dict[str, Any]] | None = None,
    ) -> AsyncIterator[dict[str, Any]]:
        """Gera eventos de streaming: {"token": str} ou {"tool_calls": [...]}.

        Faz uma única requisição POST /api/chat com stream=True e repassa
        cada pedaço assim que chega. Em erro de conexão, gera {"erro": msg}.
        """
        corpo: dict[str, Any] = {
            "model": self.model,
            "messages": self._mensagens(texto),
            "stream": True,
            "keep_alive": self.keep_alive,
            "options": {"num_ctx": self.num_ctx, "num_predict": self.num_predict},
        }
        if tools:
            corpo["tools"] = tools
        try:
            async with httpx.AsyncClient(timeout=120.0) as cliente:
                async with cliente.stream("POST", f"{self.host}/api/chat", json=corpo) as resp:
                    if resp.status_code != 200:
                        detalhe = await resp.aread()
                        yield {"erro": f"Ollama HTTP {resp.status_code}: {detalhe[:300]!r}"}
                        return
                    resposta_completa = ""
                    chamadas: list[dict[str, Any]] = []
                    async for linha in resp.aiter_lines():
                        if not linha.strip():
                            continue
                        try:
                            evento = json.loads(linha)
                        except json.JSONDecodeError:
                            continue
                        msg = evento.get("message", {})
                        pedaco = msg.get("content", "")
                        if pedaco:
                            resposta_completa += pedaco
                            yield {"token": pedaco}
                        if msg.get("tool_calls"):
                            chamadas.extend(msg["tool_calls"])
                        if evento.get("done"):
                            if chamadas:
                                yield {"tool_calls": chamadas}
                            break
                    # Atualiza histórico só com texto (sem tool_calls nesta versão simples).
                    self._historico.append({"role": "user", "content": texto})
                    self._historico.append({"role": "assistant", "content": resposta_completa})
                    # Mantém só as últimas N trocas (cada troca = 2 mensagens).
                    self._historico = self._historico[-(self.max_trocas * 2):]
        except httpx.ConnectError as erro:
            yield {"erro": f"Não conectei ao Ollama em {self.host}. Ele está rodando? ({erro})"}
        except Exception as erro:  # noqa: BLE001 - mensagem clara
            yield {"erro": f"Falha no cérebro: {erro}"}

    async def responder(self, texto: str, tools: list[dict[str, Any]] | None = None) -> str:
        """Versão sem streaming: junta todos os tokens e retorna o texto."""
        partes: list[str] = []
        async for evento in self.chat(texto, tools=tools):
            if "token" in evento:
                partes.append(evento["token"])
            elif "erro" in evento:
                return f"Desculpe, tive um problema: {evento['erro']}"
        return "".join(partes).strip()

    async def aquecer(self) -> dict[str, float]:
        """Aquece o modelo (deixa carregado na RAM) e mede latência.

        Retorna {"primeiro_token_s": x, "total_s": y, "tokens_por_s": z}.
        """
        inicio = time.perf_counter()
        primeiro = 0.0
        n_tokens = 0
        async for evento in self.chat("Responda só com: pronto."):
            if "token" in evento:
                n_tokens += 1
                if primeiro == 0.0:
                    primeiro = time.perf_counter() - inicio
        total = time.perf_counter() - inicio
        tps = (n_tokens / total) if total > 0 else 0.0
        return {"primeiro_token_s": primeiro, "total_s": total, "tokens_por_s": tps}
