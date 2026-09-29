"""Testes da Fase 5 — roteador + pipeline (modo texto, sem playback)."""
from __future__ import annotations

import asyncio

from core.pipeline import JarvisPipeline, limpar_para_fala
from core.router import CONFIRMACOES, rotear


def test_hora_data_pare_sem_llm():
    r = rotear("que horas são?")
    assert r is not None and "horas" in r["resposta"]
    r = rotear("que dia é hoje?")
    assert r is not None and "hoje" in r["resposta"].lower()
    r = rotear("PARE agora!")
    assert r is not None and r["acao"]["metodo"] == "parar_tudo"


def test_acoes_controle_viram_payload():
    r = rotear("abrir o bloco de notas")
    assert r["acao"] == {"skill": "apps", "metodo": "abrir", "args": {"alvo": "bloco de notas"}}
    r = rotear("volume para 30")
    assert r["acao"]["args"] == {"nivel": 30}
    r = rotear("aumentar o volume")
    assert r["acao"]["metodo"] == "volume_mais"
    r = rotear("me lembra em 5 minutos")
    assert r["acao"]["metodo"] == "timer" and r["acao"]["args"]["segundos"] == 300


def test_fora_do_roteador_vai_pao_llm():
    assert rotear("explique o que é um buraco negro") is None
    assert rotear("conte uma piada") is None


def test_limpar_para_fala():
    assert limpar_para_fala("**Olá**, veja [isso](http://x) `cod`!") == "Olá, veja isso cod!"
    assert "http" not in limpar_para_fala("abra https://exemplo.com agora")


def test_confirmacoes_variadas():
    assert len(set(CONFIRMACOES)) >= 5


def test_pipeline_turno_roteador_silencioso():
    async def vai():
        j = JarvisPipeline()
        r = await j.processar_texto("que horas são?", falar=False)
        assert r["origem"] == "roteador"
        assert r["primeiro_audio_s"] < 2.0
        return r

    asyncio.run(vai())


def test_pipeline_turno_llm_silencioso():
    async def vai():
        j = JarvisPipeline()
        await j.brain.aquecer()
        r = await j.processar_texto("responda com exatamente: tudo bem", falar=False)
        assert r["origem"] == "llm"
        assert len(r["resposta"]) > 0
        return r

    asyncio.run(vai())
