"""Skill apps (Fase 6): abrir/fechar programas e sites com LISTA DE PERMISSÃO.

Nada fora da lista abre sem confirmação. Fechar usa o nome da janela/processo.
"""
from __future__ import annotations

import os
import subprocess
import webbrowser

from skills.base import Resultado, Skill

# Nome falado -> comando. Somente estes abrem sem confirmação.
PERMITIDOS = {
    "chrome": r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    "edge": r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    "bloco de notas": "notepad.exe",
    "notepad": "notepad.exe",
    "calculadora": "calc.exe",
    "explorer": "explorer.exe",
    "explorador": "explorer.exe",
    "spotify": os.path.expandvars(r"%APPDATA%\Spotify\Spotify.exe"),
    "terminal": "wt.exe",
    "cmd": "cmd.exe",
}

# Processo p/ taskkill ao fechar.
PROCESSOS = {
    "chrome": "chrome.exe",
    "edge": "msedge.exe",
    "bloco de notas": "notepad.exe",
    "notepad": "notepad.exe",
    "calculadora": "CalculatorApp.exe",
    "spotify": "Spotify.exe",
}

SITES_PERMITIDOS = ("google.com", "youtube.com", "wikipedia.org", "github.com", "bing.com")


class AppsSkill(Skill):
    """Abre e fecha programas e sites (lista de permissão)."""

    nome = "apps"
    descricao = "Abrir ou fechar programas e sites permitidos. Args: operacao (abrir|fechar), alvo (nome)."
    parametros = {
        "operacao": {"type": "string", "description": "abrir ou fechar"},
        "alvo": {"type": "string", "description": "nome do programa ou site"},
    }

    def run(self, operacao: str = "abrir", alvo: str = "", **kwargs) -> Resultado:
        alvo_norm = (alvo or "").strip().lower()
        try:
            if operacao == "abrir":
                return self._abrir(alvo_norm)
            if operacao == "fechar":
                return self._fechar(alvo_norm)
            return Resultado(f"Operação desconhecida: {operacao}.", ok=False)
        except Exception as erro:  # noqa: BLE001
            return Resultado(f"Não consegui {operacao} {alvo}: {erro}.", ok=False)

    def _abrir(self, alvo: str) -> Resultado:
        if alvo in PERMITIDOS:
            caminho = PERMITIDOS[alvo]
            try:
                if caminho.endswith(".exe") and ":" not in caminho and "\\" not in caminho:
                    subprocess.Popen(caminho)
                elif os.path.isfile(caminho):
                    os.startfile(caminho)  # noqa: PTH110 - API do Windows p/ abrir apps
                else:
                    subprocess.Popen(caminho)
            except FileNotFoundError:
                return Resultado(f"O programa {alvo} não está instalado aqui.", ok=False)
            return Resultado(f"Abrindo {alvo}.")
        if "." in alvo:  # parece site
            if not alvo.startswith(("http://", "https://")):
                alvo = "https://" + alvo
            dominio = alvo.split("/")[2]
            if not any(dominio.endswith(s) for s in SITES_PERMITIDOS):
                return Resultado(f"Site fora da lista de permissão: {dominio}.", ok=False)
            webbrowser.open(alvo)
            return Resultado(f"Abrindo {dominio}.")
        return Resultado(f"Não tenho permissão para abrir {alvo}.", ok=False)

    def _fechar(self, alvo: str) -> Resultado:
        proc = PROCESSOS.get(alvo)
        if not proc:
            return Resultado(f"Não sei fechar {alvo} com segurança.", ok=False)
        r = subprocess.run(["taskkill", "/IM", proc, "/T"], capture_output=True, text=True)
        if r.returncode == 0:
            return Resultado(f"Fechei {alvo}.")
        return Resultado(f"O {alvo} já estava fechado.", ok=True)
