"""Testes da Fase 3 — escuta (offline: transcreve áudio gerado pelo Piper)."""
from __future__ import annotations

import numpy as np

from voice.stt import STT, TAXA, reamostrar


def test_vad_silencio_e_ruido():
    stt = STT.__new__(STT)  # sem carregar o Whisper
    import webrtcvad

    stt.vad = webrtcvad.Vad(2)
    silencio = bytes(480 * 2)  # 30 ms de zeros
    assert stt.tem_fala(silencio) is False
    assert stt.tem_fala(b"curto") is False  # tamanho inválido


def test_reamostrar():
    audio = np.ones(22050, dtype=np.float32)
    saida = reamostrar(audio, 22050, 16000)
    assert len(saida) == 16000


def test_transcrever_voz_piper():
    from voice.tts import TTS

    tts = TTS()
    taxa, audio = tts.sintetizar("olá Jarvis, teste do microfone")
    audio16 = reamostrar(audio, taxa, TAXA)
    stt = STT()
    texto = stt.transcrever(audio16).lower()
    print(f"\n[transcrito] {texto!r}")
    assert len(texto) > 0, "transcrição vazia da voz Piper"
    assert any(p in texto for p in ["jarvis", "microfone", "teste", "olá", "ola"]), f"fora do esperado: {texto!r}"
