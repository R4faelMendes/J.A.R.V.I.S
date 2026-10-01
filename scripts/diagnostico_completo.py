"""Diagnóstico completo do JARVIS (Tarefa 0): testa cada etapa ISOLADA.

Não toca áudio nem exige sua voz — só diz OK/FALHA por etapa para
sabermos onde quebrar a cabeça. Testes com ouvido/boca vêm depois.
Uso:
    .\\.venv\\Scripts\\python.exe scripts/diagnostico_completo.py
"""
from __future__ import annotations

import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

RESULTADOS: list[tuple[str, bool, str]] = []


def relatar(etapa: str, ok: bool, detalhe: str = "") -> None:
    """Registra e imprime o resultado de uma etapa."""
    RESULTADOS.append((etapa, ok, detalhe))
    print(f"[{'OK' if ok else 'FALHA'}] {etapa}" + (f" — {detalhe}" if detalhe else ""))


def t_config() -> None:
    try:
        import yaml

        cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
        assert cfg["cerebro"]["modelo_principal"] and cfg["servidor"]["porta"]
        relatar("config.yaml válido", True)
    except Exception as erro:  # noqa: BLE001
        relatar("config.yaml válido", False, str(erro)[:150])


def t_ollama() -> None:
    try:
        import httpx
        import yaml

        cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8"))
        host = cfg["cerebro"]["host"]
        r = httpx.get(f"{host}/api/tags", timeout=10)
        modelos = [m["name"] for m in r.json().get("models", [])]
        alvo = cfg["cerebro"]["modelo_principal"]
        relatar("Ollama responde + modelo existe", alvo in modelos,
                f"host={host} modelos={modelos} esperado={alvo}")
    except Exception as erro:  # noqa: BLE001
        relatar("Ollama responde + modelo existe", False, str(erro)[:150])


def t_whisper() -> None:
    try:
        from voice.stt import STT

        s = STT()
        relatar("Whisper carrega", True, f"modelo={s.modelo_nome}")
    except Exception as erro:  # noqa: BLE001
        relatar("Whisper carrega", False, str(erro)[:150])


def t_piper() -> None:
    try:
        import json

        from voice.tts import TTS

        t = TTS()
        anexos = [t.voz_onnx.is_file(),
                  (t.voz_onnx.parent / f"{t.voz_onnx.name}.json").is_file(),
                  (t.piper_exe.parent / "espeak-ng-data").is_dir()]
        taxa, audio = t._sintetizar_oneshot("Teste do diagnóstico.")
        j = json.loads((t.voz_onnx.parent / f"{t.voz_onnx.name}.json").read_text(encoding="utf-8"))
        taxa_json = j.get("audio", {}).get("sample_rate")
        ok = all(anexos) and len(audio) > 0 and taxa == taxa_json
        relatar("Piper gera WAV", ok,
                f"bytes={audio.nbytes} taxa={taxa} (json={taxa_json}) anexos={anexos}")
        t.fechar()
    except Exception as erro:  # noqa: BLE001
        relatar("Piper gera WAV", False, str(erro)[:150])


def t_audio() -> None:
    try:
        import numpy as np
        import sounddevice as sd

        devs = sd.query_devices()
        ins = [d for d in devs if d["max_input_channels"] > 0]
        outs = [d for d in devs if d["max_output_channels"] > 0]
        rec = sd.rec(int(16000 * 0.5), samplerate=16000, channels=1, dtype="int16", blocking=True)
        rms = float(np.sqrt(np.mean((rec.astype(np.float32) / 32768.0) ** 2)))
        relatar("sounddevice in/out", bool(ins) and bool(outs),
                f"entradas={len(ins)} saídas={len(outs)} rms_0.5s={rms:.4f} (0 = silêncio)")
    except Exception as erro:  # noqa: BLE001
        relatar("sounddevice in/out", False, str(erro)[:150])


def t_wake() -> None:
    try:
        import numpy as np

        from voice.wakeword import JANELA_AMOSTRAS, WakeListener

        w = WakeListener(on_wake=lambda: None)
        score = w.pontuar(np.zeros(JANELA_AMOSTRAS, dtype=np.int16))
        relatar("wake word carrega", score < w.limiar,
                f"score_silencio={score:.3f} limiar={w.limiar}")
    except Exception as erro:  # noqa: BLE001
        relatar("wake word carrega", False, str(erro)[:150])


def t_server() -> None:
    try:
        from fastapi.testclient import TestClient

        from core.server import criar_app

        c = TestClient(criar_app())
        rotas = {r.path for r in c.app.routes}
        ok = {"/", "/debug", "/api/eventos", "/api/clientes", "/api/estado"} <= rotas
        e = c.get("/api/eventos?n=5").json()
        relatar("servidor rotas+eventos", ok and "eventos" in e, f"rotas={len(rotas)}")
    except Exception as erro:  # noqa: BLE001
        relatar("servidor rotas+eventos", False, str(erro)[:150])


def t_porta() -> None:
    import socket

    s = socket.socket()
    try:
        s.bind(("127.0.0.1", 8765))
        relatar("porta 8765 livre", True)
    except OSError:
        relatar("porta 8765 livre", False, "ocupada (servidor já rodando?)")
    finally:
        s.close()


def t_estados() -> None:
    try:
        from core.states import ESTADOS, IDLE, LISTENING

        ok = ESTADOS.definir(LISTENING, "diagnóstico") and ESTADOS.definir(IDLE, "diagnóstico")
        relatar("máquina de estados", ok and ESTADOS.estado == IDLE, f"estado={ESTADOS.estado}")
    except Exception as erro:  # noqa: BLE001
        relatar("máquina de estados", False, str(erro)[:150])


def principal() -> int:
    print("JARVIS — diagnóstico completo (etapas isoladas)")
    for fn in [t_config, t_ollama, t_whisper, t_piper, t_audio,
               t_wake, t_server, t_porta, t_estados]:
        fn()
    oks = sum(1 for _, ok, _ in RESULTADOS if ok)
    print(f"\n{oks}/{len(RESULTADOS)} etapas OK")
    return 0 if oks == len(RESULTADOS) else 1


if __name__ == "__main__":
    raise SystemExit(principal())
