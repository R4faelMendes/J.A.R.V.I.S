"""Diagnóstico de dispositivos de saída de áudio do JARVIS 2.0.

Lista todos os dispositivos de saída (índice, nome, host API, taxa de amostragem padrão)
e toca um tom senoidal de 440 Hz (2 segundos) para cada dispositivo compatível.
"""
from __future__ import annotations

import sys
import time
import numpy as np
import sounddevice as sd
import yaml
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]

def listar_e_testar_dispositivos():
    try:
        cfg = yaml.safe_load((RAIZ / "config.yaml").read_text(encoding="utf-8")) or {}
    except Exception:
        cfg = {}
    
    dev_config = cfg.get("audio", {}).get("alto_falante_indice")
    print("=" * 70)
    print("DIAGNÓSTICO DE ÁUDIO — DISPOSITIVOS DE SAÍDA (sounddevice)")
    print("=" * 70)
    print(f"Configuração atual em config.yaml (audio.alto_falante_indice): {dev_config}")
    
    dispositivos = sd.query_devices()
    hostapis = sd.query_hostapis()
    padrao_out = sd.default.device[1]  # índice padrão de saída
    
    print(f"Dispositivo padrão de saída do sistema: índice {padrao_out}\n")
    print("Índice | Host API           | Canais Out | Taxa Padrão | Nome")
    print("-" * 70)
    
    saidas_validas = []
    for idx, d in enumerate(dispositivos):
        max_out = d.get("max_output_channels", 0)
        if max_out > 0:
            host_nome = hostapis[d["hostapi"]]["name"] if d["hostapi"] < len(hostapis) else "Desconhecido"
            marcador = " [PADRÃO]" if idx == padrao_out else ""
            print(f"{idx:6d} | {host_nome:18s} | {max_out:10d} | {int(d['default_samplerate']):11d} | {d['name']}{marcador}")
            saidas_validas.append((idx, d['name'], host_nome, int(d['default_samplerate'])))
    
    print("-" * 70)
    print(f"Total de dispositivos de saída detectados: {len(saidas_validas)}\n")
    
    # Gerar tom senoidal 440 Hz de 2 segundos
    duracao_s = 2.0
    frequencia = 440.0
    
    print("Iniciando teste de tom (440 Hz, 2 segundos) em cada dispositivo de saída:\n")
    for idx, nome, host, taxa in saidas_validas:
        print(f"--> Testando dispositivo [{idx}] {nome} ({host}) na taxa {taxa} Hz...")
        t = np.linspace(0, duracao_s, int(taxa * duracao_s), endpoint=False)
        # Envelope suave para evitar estalo no início e fim
        envelope = np.ones_like(t)
        fade = int(taxa * 0.05)
        envelope[:fade] = np.linspace(0, 1, fade)
        envelope[-fade:] = np.linspace(1, 0, fade)
        tom = (0.5 * np.sin(2 * np.pi * frequencia * t) * envelope).astype(np.float32)
        
        try:
            sd.play(tom, samplerate=taxa, device=idx)
            sd.wait()
            print(f"    [OK] Tom reproduzido com sucesso no índice {idx}.")
        except Exception as erro:
            print(f"    [FALHA] Não foi possível reproduzir no índice {idx}: {erro}")
        time.sleep(0.5)
        
    print("\nTeste concluído.")

if __name__ == "__main__":
    listar_e_testar_dispositivos()
