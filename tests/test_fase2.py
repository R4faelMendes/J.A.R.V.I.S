"""Testes da Fase 2 — voz de saída (síntese; sem playback no pytest)."""
from __future__ import annotations

from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]


def test_arquivos_piper_e_vozes():
    assert (RAIZ / "models" / "piper" / "piper.exe").is_file()
    for nome in ["pt_BR-faber-medium", "pt_BR-edresson-low"]:
        assert (RAIZ / "models" / "piper-voices" / f"{nome}.onnx").is_file()
        assert (RAIZ / "models" / "piper-voices" / f"{nome}.onnx.json").is_file()


def test_dividir_frases():
    from voice.tts import dividir_frases

    assert dividir_frases("") == []
    frases = dividir_frases("Olá! Como vai você? Tudo bem.")
    assert len(frases) == 3
    assert frases[0] == "Olá!"


def test_sintetizar_gera_audio():
    from voice.tts import TTS

    tts = TTS()
    taxa, audio = tts.sintetizar("Olá, teste do Jarvis.")
    assert taxa >= 16000
    assert len(audio) > taxa // 2  # pelo menos meio segundo de áudio
    assert max(abs(audio)) > 0.01  # não é silêncio


def test_bench_existe():
    assert (RAIZ / "scripts" / "bench_tts.py").is_file()
    assert (RAIZ / "voice" / "tts.py").is_file()


def test_servidor_persistente_rapido():
    import time

    from voice.tts import TTS

    tts = TTS()
    tts.iniciar_servidor()
    try:
        assert tts.servidor.vivo()
        t0 = time.perf_counter()
        taxa, audio = tts.sintetizar("Servidor persistente em teste.")
        dt = time.perf_counter() - t0
        assert len(audio) > taxa // 2
        assert dt < 2.0, f"síntese persistente lenta: {dt:.2f}s"
    finally:
        tts.fechar()
    assert tts.servidor is None
