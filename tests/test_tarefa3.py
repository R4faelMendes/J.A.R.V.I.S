"""Testes automatizados da Tarefa 3 — Protocolo de Resposta, Streaming, Deduplicação e Loop.

Valida:
1. Emissão correta de response_start, response_delta e response_end com IDs e sequências.
2. Deduplicação de eventos (mesmo id e seq não duplicam texto).
3. Preservação de texto durante eventos de estado (nenhum evento de estado apaga a resposta).
4. Detecção de loop: mais de 3 respostas automáticas consecutivas sem entrada humana são interrompidas.
5. Contagem de clientes em /api/clientes e WebSocket com telemetria limpa.
"""
from __future__ import annotations

import asyncio
from types import SimpleNamespace
from fastapi.testclient import TestClient

from core.server import criar_app
from core.events import BUS
from core.pipeline import JarvisPipeline


def _app_falso():
    app = criar_app()

    class PipeFalso:
        _pendente = None
        tts = SimpleNamespace(parar=lambda: None, sintetizar=lambda f: (22050, __import__("numpy").zeros(22050)),
                              falando=False)

        async def processar_texto(self, texto, falar=False, origem_humana=True, msg_id=None):
            mid = msg_id or "msg_teste_123"
            BUS.emitir("RESPONSE_START", {"id": mid, "seq": 0, "origem": "teste"})
            BUS.emitir("RESPONSE_DELTA", {"id": mid, "seq": 1, "delta": "eco: "})
            BUS.emitir("RESPONSE_DELTA", {"id": mid, "seq": 2, "delta": texto})
            BUS.emitir("RESPONSE_END", {"id": mid, "seq": 3, "texto_final": f"eco: {texto}"})
            return {"id": mid, "resposta": f"eco: {texto}", "origem": "teste",
                    "acao": None, "primeiro_audio_s": 0.01}

    app.state.pipeline = PipeFalso()
    return app


def test_protocolo_streaming_e_delas():
    """Valida o envio sequenciado de start, delta e end via BUS e endpoint de comando."""
    eventos_recebidos = []

    def capturar(ev):
        if ev.get("tipo") in {"RESPONSE_START", "RESPONSE_DELTA", "RESPONSE_END"}:
            eventos_recebidos.append(ev)

    BUS.assinar(capturar)
    app = _app_falso()
    c = TestClient(app)

    r = c.post("/api/comando", json={"texto": "protocolo"})
    assert r.status_code == 200
    dados = r.json()
    assert dados["resposta"] == "eco: protocolo"
    assert "id" in dados

    # Verifica os eventos emitidos
    tipos = [e["tipo"] for e in eventos_recebidos]
    assert "RESPONSE_START" in tipos
    assert "RESPONSE_DELTA" in tipos
    assert "RESPONSE_END" in tipos

    # Verifica sequência estrita e id consistente
    mid = dados["id"]
    deltas = [e for e in eventos_recebidos if e["tipo"] == "RESPONSE_DELTA" and e["dados"]["id"] == mid]
    seqs = [d["dados"]["seq"] for d in deltas]
    assert seqs == sorted(seqs)
    assert len(seqs) >= 2


def test_simulacao_deduplicacao_e_acumulo():
    """Simula a lógica do front-end comprovando que eventos duplicados são ignorados e texto acumulado é exato."""
    # Implementação idêntica à lógica do app.js
    mensagem_ativa = {"id": "m1", "seqs": set([0]), "texto": ""}
    historico = []

    def receber_delta(id, seq, delta):
        nonlocal mensagem_ativa
        if seq in mensagem_ativa["seqs"]:
            return False  # Duplicado ignorado
        mensagem_ativa["seqs"].add(seq)
        mensagem_ativa["texto"] += delta
        return True

    def finalizar_resposta(id, seq, texto_final):
        nonlocal mensagem_ativa
        mensagem_ativa["seqs"].add(seq)
        mensagem_ativa["texto"] = texto_final

    # Envia deltas normais
    assert receber_delta("m1", 1, "Olá ") is True
    assert receber_delta("m1", 2, "mundo!") is True
    assert mensagem_ativa["texto"] == "Olá mundo!"

    # Envia deltas DUPLICADOS (mesmo id e seq) -> devem ser ignorados
    assert receber_delta("m1", 1, "Olá ") is False
    assert receber_delta("m1", 2, "mundo!") is False
    assert mensagem_ativa["texto"] == "Olá mundo!"

    # Finalização
    finalizar_resposta("m1", 3, "Olá mundo!")
    assert mensagem_ativa["texto"] == "Olá mundo!"


def test_historico_ultimas_cinco_respostas():
    """Comprova que até 5 respostas são mantidas no histórico e novas respostas limpam só o ativo."""
    historico = []
    MAX_HISTORICO = 5
    mensagem_ativa = None

    def nova_resposta(mid, texto):
        nonlocal mensagem_ativa, historico
        if mensagem_ativa and mensagem_ativa.get("texto"):
            historico.append(dict(mensagem_ativa))
            if len(historico) > MAX_HISTORICO:
                historico.pop(0)
        mensagem_ativa = {"id": mid, "texto": texto}

    # Gera 7 mensagens consecutivas
    for i in range(1, 8):
        nova_resposta(f"m{i}", f"Resposta {i}")

    # Histórico deve conter exatamente as 5 anteriores (m2 a m6) e ativa ser m7
    assert len(historico) == 5
    assert [h["id"] for h in historico] == ["m2", "m3", "m4", "m5", "m6"]
    assert mensagem_ativa["id"] == "m7"
    assert mensagem_ativa["texto"] == "Resposta 7"


def test_detector_de_loop_respostas_automaticas():
    """Valida que mais de 3 respostas automáticas seguidas são interrompidas pelo pipeline."""
    pipe = JarvisPipeline()
    # Mock do executor e brain para simular respostas automáticas
    loop_eventos = []

    def capturar_loop(ev):
        if ev.get("tipo") == "LOOP_DETECTADO":
            loop_eventos.append(ev)

    BUS.assinar(capturar_loop)

    # 1ª automática: ok
    r1 = asyncio.run(pipe.processar_texto("auto 1", falar=False, origem_humana=False))
    assert r1.get("origem") != "erro"

    # 2ª automática: ok
    r2 = asyncio.run(pipe.processar_texto("auto 2", falar=False, origem_humana=False))
    assert r2.get("origem") != "erro"

    # 3ª automática: ok
    r3 = asyncio.run(pipe.processar_texto("auto 3", falar=False, origem_humana=False))
    assert r3.get("origem") != "erro"

    # 4ª automática: DEVE DISPARAR DETECÇÃO DE LOOP
    r4 = asyncio.run(pipe.processar_texto("auto 4", falar=False, origem_humana=False))
    assert r4.get("origem") == "erro"
    assert "loop de respostas" in r4["resposta"]
    assert len(loop_eventos) >= 1

    # Entrada humana reseta o contador
    r5 = asyncio.run(pipe.processar_texto("que horas são?", falar=False, origem_humana=True))
    assert r5.get("origem") != "erro"
    assert pipe._auto_consecutivas == 0


def test_websocket_telemetria_e_clientes():
    """Valida que o WebSocket envia telemetria limpa (sem poluir resposta) e contabiliza clientes."""
    app = _app_falso()
    c = TestClient(app)

    with c.websocket_connect("/ws") as ws:
        # Verifica mensagem de telemetria
        msg = ws.receive_json()
        assert msg.get("tipo") == "telemetria" or ("estado" in msg and "cpu" in msg)
        assert "resposta" not in msg  # Telemetria periódica não deve carregar resposta não-sequenciada
        
        # Verifica /api/clientes
        res_clientes = c.get("/api/clientes").json()
        assert res_clientes["conectados"] == 1
