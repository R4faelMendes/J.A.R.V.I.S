"""Testes da Fase 6 — skills (mocks nas partes perigosas: nada real é alterado)."""
from __future__ import annotations

from pathlib import Path
from unittest.mock import MagicMock, patch

import pytest

from skills.base import carregar_skills
from skills.executor import Executor

RAIZ = Path(__file__).resolve().parents[1]


def test_registro_carrega_todas():
    reg = carregar_skills()
    for nome in ["apps", "system", "files", "web", "code_runner",
                 "notes", "media", "smart_home", "screen_agent"]:
        assert nome in reg, f"skill ausente: {nome}"


def test_apps_abrir_permitido_com_mock():
    from skills.apps import AppsSkill

    with patch("skills.apps.subprocess.Popen") as popen:
        r = AppsSkill().run(operacao="abrir", alvo="calculadora")
        assert r.ok and popen.called
    r = AppsSkill().run(operacao="abrir", alvo="formatar C")
    assert not r.ok  # fora da lista


def test_apps_site_fora_da_lista():
    from skills.apps import AppsSkill

    r = AppsSkill().run(operacao="abrir", alvo="site-malicioso.xyz")
    assert not r.ok


def test_system_status_real():
    from skills.system import SystemSkill

    r = SystemSkill().run(metodo="status")
    assert r.ok and "por cento" in r.resposta


def test_system_volume_mockado():
    from skills import system as sysmod

    falso = MagicMock()
    falso.GetMasterVolumeLevelScalar.return_value = 0.5
    with patch.object(sysmod, "_sessao_audio", return_value=falso):
        r = sysmod.SystemSkill().run(metodo="volume", nivel=30)
        assert r.ok and "30" in r.resposta


def test_files_fluxo_completo_no_workspace(tmp_path):
    from skills import files as fmod
    from skills.files import FilesSkill

    fmod.WORKSPACE = tmp_path
    s = FilesSkill()
    assert s.run(operacao="criar", caminho="a.txt", conteudo="oi").ok
    r = s.run(operacao="listar", caminho=".")
    assert r.ok and "a.txt" in r.resposta
    assert s.run(operacao="mover", caminho="a.txt", destino="sub/b.txt").ok
    assert (tmp_path / "sub" / "b.txt").is_file()


def test_files_apagar_pede_confirmacao_e_usa_lixeira():
    from skills import files as fmod
    from skills.files import FilesSkill

    with patch.object(fmod, "WORKSPACE", RAIZ / "workspace"), \
         patch("skills.files.send2trash") as lixeira:
        s = FilesSkill()
        (RAIZ / "workspace").mkdir(exist_ok=True)
        alvo = RAIZ / "workspace" / "apagar_teste.txt"
        alvo.write_text("x", encoding="utf-8")
        r1 = s.run(operacao="apagar", caminho="apagar_teste.txt")
        assert not r1.ok and r1.dados.get("precisa_confirmar")
        assert alvo.is_file()  # nada apagado sem confirmação
        r2 = s.run(operacao="apagar", caminho="apagar_teste.txt", confirmar=True)
        assert lixeira.called
        alvo.unlink(missing_ok=True)


def test_web_mockada():
    from skills import web as wmod

    class FakeDDGS:
        def __enter__(self): return self
        def __exit__(self, *a): return False
        def text(self, q, region="", max_results=3):
            return [{"title": "T", "body": "Resposta curta. Resto."}]

    with patch.dict("sys.modules", {"ddgs": MagicMock(DDGS=FakeDDGS),
                                       "duckduckgo_search": MagicMock(DDGS=FakeDDGS)}):
        import importlib
        importlib.reload(wmod)
        r = wmod.WebSkill().run(consulta="teste")
        assert r.ok and "Resposta curta" in r.resposta


def test_code_runner_bloqueia_e_pede_confirmacao():
    from skills.code_runner import CodeRunnerSkill

    s = CodeRunnerSkill()
    r = s.run(linguagem="python", codigo="print('oi')")
    assert not r.ok and r.dados.get("precisa_confirmar")  # mostra antes
    r = s.run(linguagem="python", codigo="print('oi')", confirmar=True)
    assert r.ok and "oi" in r.resposta
    r = s.run(linguagem="powershell", codigo="format C: /y", confirmar=True)
    assert not r.ok and "Bloqueado" in r.resposta


def test_notes_e_timer():
    from skills import notes as nmod
    from skills.notes import NotesSkill

    nmod.ARQUIVO = RAIZ / "workspace" / "notas_teste.json"
    try:
        s = NotesSkill()
        assert s.run(operacao="anotar", texto="comprar pão").ok
        assert "pão" in s.run(operacao="ler").resposta
        disparou = []
        s.alarme = disparou.append
        assert s.run(operacao="timer", segundos=1, texto="teste").ok
        import time
        time.sleep(1.5)
        assert disparou and "teste" in disparou[0]
    finally:
        nmod.ARQUIVO.unlink(missing_ok=True)


def test_media_mockada():
    from skills.media import MediaSkill

    with patch("keyboard.send") as enviar:
        assert MediaSkill().run(metodo="pausar").ok and enviar.called
    assert not MediaSkill().run(metodo="dançar").ok


def test_desativadas_avisam():
    from skills.screen_agent import ScreenAgentSkill
    from skills.smart_home import SmartHomeSkill

    assert not ScreenAgentSkill().run().ok
    assert "VRAM" in ScreenAgentSkill().run().resposta
    assert not SmartHomeSkill().run(entidade="light.x").ok


def test_executor_respeita_config_e_loga():
    ex = Executor()
    assert set(ex.skills) >= {"apps", "system", "files"}
    r = ex.executar({"skill": "screen_agent", "metodo": "x", "args": {}})
    assert not r.ok and "desligada" in r.resposta
    assert (RAIZ / "logs" / "exec.log").is_file()


def test_pipeline_confirmo_executa_apagar():
    import asyncio

    from core.pipeline import JarvisPipeline

    async def vai():
        from skills import files as fmod
        with patch.object(fmod, "WORKSPACE", RAIZ / "workspace"), \
             patch("skills.files.send2trash"):
            (RAIZ / "workspace").mkdir(exist_ok=True)
            (RAIZ / "workspace" / "conf_teste.txt").write_text("x", encoding="utf-8")
            j = JarvisPipeline()
            # Força ação direta de apagar (bypass do roteador, que não tem "apagar")
            import time as _time
            r1 = await j._executar_acao(
                {"skill": "files", "metodo": "_direto",
                 "args": {"operacao": "apagar", "caminho": "conf_teste.txt"}},
                "apague conf_teste", _time.perf_counter(), falar=False)
            assert "confirmo" in r1["resposta"]
            r2 = await j.processar_texto("confirmo", falar=False)
            assert "lixeira" in r2["resposta"]
            (RAIZ / "workspace" / "conf_teste.txt").unlink(missing_ok=True)

    asyncio.run(vai())
