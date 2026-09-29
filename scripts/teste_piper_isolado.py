"""Teste isolado do Piper TTS binário + modelos + reprodução de áudio.

Gera um arquivo WAV com 'Olá, eu sou o Jarvis', verifica tamanho,
valida os arquivos .onnx/.json/espeak-ng-data e reproduz o som.
"""
from __future__ import annotations

import json
import os
import subprocess
import sys
import time
import wave
import winsound
from pathlib import Path

import numpy as np
import sounddevice as sd

RAIZ = Path(__file__).resolve().parents[1]

def testar_piper():
    print("=" * 70)
    print("TESTE ISOLADO — PIPER TTS (Binário + Voz + Áudio)")
    print("=" * 70)
    
    piper_exe = RAIZ / "models" / "piper" / "piper.exe"
    espeak_dir = RAIZ / "models" / "piper" / "espeak-ng-data"
    voz_onnx = RAIZ / "models" / "piper-voices" / "pt_BR-faber-medium.onnx"
    voz_json = RAIZ / "models" / "piper-voices" / "pt_BR-faber-medium.onnx.json"
    
    print(f"1. Checagem de Arquivos:")
    print(f"   - piper.exe: {'[EXISTE]' if piper_exe.is_file() else '[FALTA]'} ({piper_exe})")
    print(f"   - espeak-ng-data: {'[EXISTE]' if espeak_dir.is_dir() else '[FALTA]'} ({espeak_dir})")
    print(f"   - voz .onnx: {'[EXISTE]' if voz_onnx.is_file() else '[FALTA]'} ({voz_onnx})")
    print(f"   - voz .json: {'[EXISTE]' if voz_json.is_file() else '[FALTA]'} ({voz_json})")
    
    if not (piper_exe.is_file() and voz_onnx.is_file() and voz_json.is_file()):
        print("\n[ERRO FATAL] Arquivos necessários do Piper não foram encontrados.")
        return
        
    cfg_voz = json.loads(voz_json.read_text(encoding="utf-8"))
    sample_rate = int(cfg_voz.get("audio", {}).get("sample_rate", 22050))
    print(f"\n2. Metadados da Voz:")
    print(f"   - Taxa de amostragem nativa (sample_rate): {sample_rate} Hz")
    
    saida_wav = RAIZ / "workspace" / "teste_piper_isolado.wav"
    saida_wav.parent.mkdir(exist_ok=True)
    if saida_wav.exists():
        saida_wav.unlink()
        
    frase = "Olá, eu sou o Jarvis. Teste de voz e saída de áudio."
    print(f"\n3. Executando Piper binário para sintetizar:")
    print(f"   - Frase: '{frase}'")
    print(f"   - Arquivo de saída: {saida_wav}")
    
    t0 = time.perf_counter()
    cmd = [
        str(piper_exe),
        "--model", str(voz_onnx),
        "--output_file", str(saida_wav),
        "--espeak_data", str(espeak_dir),
        "-q"
    ]
    
    res = subprocess.run(cmd, input=frase.encode("utf-8"), capture_output=True)
    duracao_sintese = time.perf_counter() - t0
    
    print(f"   - Tempo de síntese: {duracao_sintese:.3f} s")
    print(f"   - Returncode: {res.returncode}")
    if res.stderr:
        print(f"   - Stderr: {res.stderr.decode('utf-8', 'replace').strip()}")
        
    if not saida_wav.is_file():
        print("[ERRO] Arquivo WAV não foi criado pelo Piper.")
        return
        
    tamanho_bytes = saida_wav.stat().st_size
    print(f"   - Tamanho do arquivo WAV: {tamanho_bytes} bytes ({tamanho_bytes / 1024:.1f} KB)")
    
    if tamanho_bytes < 1000:
        print("[AVISO] Arquivo WAV muito pequeno, pode estar vazio ou truncado.")
        return
        
    with wave.open(str(saida_wav), "rb") as w:
        n_canais = w.getnchannels()
        sampwidth = w.getsampwidth()
        framerate = w.getframerate()
        n_frames = w.getnframes()
        duracao_audio = n_frames / framerate
        print(f"   - WAV: {framerate} Hz, {n_canais} canal(is), {sampwidth * 8} bits, {duracao_audio:.2f} s")
        frames = w.readframes(n_frames)

    print("\n4. Reproduzindo áudio via winsound.PlaySound (Padrão Windows)...")
    try:
        winsound.PlaySound(str(saida_wav), winsound.SND_FILENAME)
        print("   [OK] winsound.PlaySound concluído com sucesso.")
    except Exception as e:
        print(f"   [FALHA] winsound.PlaySound falhou: {e}")
        
    print("\n5. Reproduzindo áudio via sounddevice (dispositivo padrão)...")
    try:
        audio_np = np.frombuffer(frames, dtype=np.int16).astype(np.float32) / 32768.0
        sd.play(audio_np, samplerate=framerate)
        sd.wait()
        print("   [OK] sounddevice.play concluído com sucesso.")
    except Exception as e:
        print(f"   [FALHA] sounddevice.play falhou: {e}")
        
    print("\n" + "=" * 70)
    print("Teste finalizado. Por favor confirme se ouviu a voz do Jarvis.")
    print("=" * 70)

if __name__ == "__main__":
    testar_piper()
