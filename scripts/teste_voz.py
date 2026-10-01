"""Script de teste de voz completo do JARVIS 2.0.

Sintetiza e reproduz uma frase longa mostrando detalhadamente cada etapa,
tamanho dos buffers gerados, taxas de amostragem e latências de síntese e reprodução.
Permite testar tanto via sounddevice quanto via winsound (WAV em memória).
"""
from __future__ import annotations

import io
import json
import sys
import time
import wave
import winsound
from pathlib import Path

import numpy as np
import sounddevice as sd
import yaml

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from voice.tts import TTS, dividir_frases


def tocar_wav_memoria(audio_np: np.ndarray, taxa: int) -> None:
    """Converte array numpy float32 para WAV em memória e toca via winsound."""
    pcm = (np.clip(audio_np, -1.0, 1.0) * 32767).astype(np.int16)
    buf = io.BytesIO()
    with wave.open(buf, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(taxa)
        w.writeframes(pcm.tobytes())
    winsound.PlaySound(buf.getvalue(), winsound.SND_MEMORY)


def main():
    print("=" * 75)
    print("  JARVIS 2.0 — TESTE DE VOZ E SAÍDA DE ÁUDIO")
    print("=" * 75)

    try:
        cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8")) or {}
    except Exception:
        cfg = {}

    voz_nome = cfg.get("tts", {}).get("voz", "pt_BR-faber-medium")
    dev_saida = cfg.get("audio", {}).get("alto_falante_indice")
    if dev_saida is None:
        from core.audio import resolver_saida

        dev_saida = resolver_saida()  # nome do config > índice > padrão
    
    print(f"Configuração carregada:")
    print(f"  - Voz configurada: {voz_nome}")
    print(f"  - Dispositivo de saída em config.yaml: {dev_saida} (None = padrão do Windows)")
    padrao_out = sd.default.device[1]
    info_padrao = sd.query_devices(padrao_out)
    print(f"  - Dispositivo padrão do sistema: [{padrao_out}] {info_padrao['name']} ({int(info_padrao['default_samplerate'])} Hz)")
    print("-" * 75)

    frase_longa = (
        "Olá! Eu sou o Jarvis, seu assistente pessoal de inteligência artificial. "
        "Estou operando de forma cem por cento local e gratuita no seu computador. "
        "Todos os meus módulos de síntese de voz, reconhecimento e raciocínio estão ativos e prontos."
    )

    print("\n[Etapa 1] Inicializando o motor de voz (TTS)...")
    t0 = time.perf_counter()
    tts = TTS()
    tts.iniciar_servidor()
    t_init = time.perf_counter() - t0
    print(f"  -> Motor inicializado e processo do Piper ativo em {t_init:.3f} s.")

    print("\n[Etapa 2] Aquecimento do modelo com frase curta...")
    t0 = time.perf_counter()
    taxa, audio_aq = tts.sintetizar("Pronto.")
    t_aq = time.perf_counter() - t0
    print(f"  -> Frase de aquecimento sintetizada em {t_aq:.3f} s (Taxa: {taxa} Hz, Amostras: {len(audio_aq)}).")

    print("\n[Etapa 3] Divisão do texto em frases:")
    frases = dividir_frases(frase_longa)
    for idx, f in enumerate(frases, 1):
        print(f"   {idx}. \"{f}\"")

    print("\n[Etapa 4] Síntese e reprodução frase a frase (sounddevice):")
    for idx, f in enumerate(frases, 1):
        t0_sint = time.perf_counter()
        taxa, audio = tts.sintetizar(f)
        dt_sint = time.perf_counter() - t0_sint
        dur_audio = len(audio) / taxa if taxa > 0 else 0
        print(f"  [{idx}/{len(frases)}] Sintetizada em {dt_sint:.3f} s ({len(audio)} amostras, {dur_audio:.2f} s de áudio) -> Tocando no sounddevice...")
        t0_play = time.perf_counter()
        sd.play(audio, taxa, device=dev_saida)
        sd.wait()
        dt_play = time.perf_counter() - t0_play
        print(f"         Concluído em {dt_play:.2f} s.")

    print("\n[Etapa 5] Teste de reprodução pelo fallback winsound (WAV em memória):")
    print("  -> Sintetizando frase de encerramento...")
    frase_fim = "Este é o teste do sistema de áudio alternativo em memória."
    t0 = time.perf_counter()
    taxa_fim, audio_fim = tts.sintetizar(frase_fim)
    t_sint_fim = time.perf_counter() - t0
    print(f"  -> Sintetizada em {t_sint_fim:.3f} s. Tocando via winsound...")
    tocar_wav_memoria(audio_fim, taxa_fim)
    print("  -> Reprodução via winsound concluída.")

    tts.fechar()
    print("\n" + "=" * 75)
    print("  TESTE DE VOZ CONCLUÍDO COM SUCESSO")
    print("=" * 75)


if __name__ == "__main__":
    main()
