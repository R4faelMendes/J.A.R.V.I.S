"""Script de pré-voo do JARVIS 2.0 (Fase 9 & Tarefa 6).

Garante que o ambiente esteja 100% pronto antes de subir o servidor:
1. Libera a porta 8765 automaticamente se estiver ocupada por instância anterior.
2. Garante que o Ollama esteja respondendo em http://127.0.0.1:11434/api/version.
3. Se o Ollama estiver fechado, localiza e inicia em segundo plano com as variáveis otimizadas.
4. Verifica se o modelo configurado (ex: qwen2.5:3b) está disponível.
"""
from __future__ import annotations

import os
import shutil
import subprocess
import sys
import time
import urllib.request
import json
import psutil


def liberar_porta(porta: int = 8765) -> bool:
    """Detecta se a porta está em uso e encerra o processo proprietário automaticamente."""
    processos_encerrados = 0
    for conn in psutil.net_connections(kind="tcp"):
        if conn.laddr.port == porta and conn.status == psutil.CONN_LISTEN:
            pid = conn.pid
            if pid and pid != os.getpid():
                try:
                    p = psutil.Process(pid)
                    nome = p.name()
                    print(f"[INFO] Porta {porta} em uso por {nome} (PID {pid}). Encerrando automaticamente...")
                    p.terminate()
                    p.wait(timeout=3)
                    processos_encerrados += 1
                except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.TimeoutExpired):
                    try:
                        p.kill()
                        processos_encerrados += 1
                    except Exception:
                        pass
    if processos_encerrados > 0:
        time.sleep(0.5)
        print(f"[INFO] Porta {porta} liberada com sucesso.")
    return True


def ollama_online(url: str = "http://127.0.0.1:11434/api/version", timeout: float = 2.0) -> bool:
    """Verifica se o Ollama está respondendo à API."""
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "JARVIS-Preflight"})
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return resp.status == 200
    except Exception:
        return False


def garantir_ollama() -> bool:
    """Garante que o Ollama esteja rodando; se não, inicia em segundo plano."""
    from pathlib import Path as _P

    flag = _P(__file__).resolve().parents[1] / "logs" / "ollama_iniciado_pelo_jarvis.flag"
    if ollama_online():
        print("[INFO] Ollama online e respondendo.")
        if flag.is_file():
            print("[INFO] (Ollama foi iniciado por este script antes; segue rodando.)")
        else:
            print("[AVISO] Ollama já estava rodando com as variáveis dele; "
                  "este script NÃO vai reiniciá-lo nem matá-lo ao sair.")
        return True

    print("[INFO] Ollama não detectado. Procurando executável...")
    caminho_ollama = shutil.which("ollama")
    if not caminho_ollama:
        local_app = os.environ.get("LOCALAPPDATA", "")
        candidato = os.path.join(local_app, "Programs", "Ollama", "ollama.exe")
        if os.path.isfile(candidato):
            caminho_ollama = candidato

    if not caminho_ollama:
        print("[ERRO] Executável do Ollama não foi encontrado no PATH nem no LocalAppData.")
        print("       Instale o Ollama em https://ollama.com ou execute 'winget install Ollama.Ollama'")
        return False

    print(f"[INFO] Iniciando Ollama em segundo plano: {caminho_ollama} serve")
    env = os.environ.copy()
    env["OLLAMA_KEEP_ALIVE"] = "-1"
    env["OLLAMA_MAX_LOADED_MODELS"] = "1"
    env["OLLAMA_NUM_PARALLEL"] = "1"

    # Inicia sem janela visível
    creation_flags = 0x08000000 if sys.platform == "win32" else 0  # CREATE_NO_WINDOW
    try:
        subprocess.Popen(
            [caminho_ollama, "serve"],
            env=env,
            creationflags=creation_flags,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
    except Exception as e:
        print(f"[ERRO] Falha ao iniciar Ollama: {e}")
        return False

    print("[INFO] Aguardando inicialização do Ollama (até 30s)...", end="", flush=True)
    t0 = time.time()
    while time.time() - t0 < 30:
        if ollama_online(timeout=1.0):
            print(" [OK]")
            try:
                from pathlib import Path as _P2

                (_P2(__file__).resolve().parents[1] / "logs").mkdir(exist_ok=True)
                (_P2(__file__).resolve().parents[1] / "logs"
                 / "ollama_iniciado_pelo_jarvis.flag").write_text("pid-responsavel: preflight.py",
                                                                  encoding="utf-8")
            except Exception:
                pass
            return True
        print(".", end="", flush=True)
        time.sleep(1.0)

    print("\n[ERRO] Tempo limite esgotado aguardando o Ollama responder.")
    return False


def verificar_modelo(modelo: str = "qwen2.5:3b") -> bool:
    """Verifica se o modelo está baixado no Ollama; se não, faz o pull."""
    try:
        req = urllib.request.Request("http://127.0.0.1:11434/api/tags")
        with urllib.request.urlopen(req, timeout=3.0) as resp:
            dados = json.loads(resp.read().decode("utf-8"))
            modelos = [m.get("name", "") for m in dados.get("models", [])]
            # Confere se modelo ou modelo:latest está presente
            if any(m == modelo or m.startswith(f"{modelo}:") or m.startswith(modelo) for m in modelos):
                print(f"[INFO] Modelo '{modelo}' pronto no Ollama.")
                return True
    except Exception as e:
        print(f"[AVISO] Não foi possível listar modelos via API: {e}")

    print(f"[AVISO] Modelo '{modelo}' não encontrado. Baixando via 'ollama pull {modelo}'...")
    print("       (Aviso: este download pode levar alguns minutos na primeira vez)")
    caminho_ollama = shutil.which("ollama") or "ollama"
    res = subprocess.run([caminho_ollama, "pull", modelo])
    return res.returncode == 0


def main() -> int:
    print("=== JARVIS 2.0 // VERIFICAÇÃO DE PRÉ-VOO ===")
    liberar_porta(8765)
    if not garantir_ollama():
        return 1
    verificar_modelo("qwen2.5:3b")
    print("=== AMBIENTE PRONTO // INICIANDO SERVIDOR ===")
    return 0


if __name__ == "__main__":
    sys.exit(main())
