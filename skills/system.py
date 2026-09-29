"""Skill system (Fase 6): volume, brilho, bloqueio, status, hora e data."""
from __future__ import annotations

import ctypes
import datetime

import psutil

from skills.base import Resultado, Skill


def _sessao_audio():
    """Retorna (interface IAudioEndpointVolume, cast lib). Erro claro se falhar."""
    from comtypes import CLSCTX_ALL
    from ctypes import POINTER, cast
    from pycaw.pycaw import AudioUtilities, IAudioEndpointVolume

    dispositivos = AudioUtilities.GetSpeakers()
    interface = dispositivos.Activate(IAudioEndpointVolume._iid_, CLSCTX_ALL, None)
    return cast(interface, POINTER(IAudioEndpointVolume))


class SystemSkill(Skill):
    """Controle do Windows: volume, brilho, bloqueio e diagnósticos."""

    nome = "system"
    descricao = (
        "Controle do sistema. metodos: volume (nivel 0-100), volume_mais, volume_menos, "
        "mudo, desmudo, brilho (nivel), bloquear, status, hora, data."
    )
    parametros = {
        "metodo": {"type": "string"},
        "nivel": {"type": "integer", "description": "0 a 100 p/ volume/brilho"},
    }

    def run(self, metodo: str = "status", nivel: int | None = None, **kwargs) -> Resultado:
        try:
            fn = getattr(self, f"_cmd_{metodo}", None)
            if fn is None:
                return Resultado(f"Comando de sistema desconhecido: {metodo}.", ok=False)
            return fn(nivel)
        except Exception as erro:  # noqa: BLE001
            return Resultado(f"Falha no sistema ({metodo}): {erro}.", ok=False)

    def _cmd_volume(self, nivel) -> Resultado:
        if nivel is None:
            return Resultado("Diga o nível de 0 a 100.", ok=False)
        nivel = max(0, min(100, int(nivel)))
        vol = _sessao_audio()
        vol.SetMasterVolumeLevelScalar(nivel / 100.0, None)
        return Resultado(f"Volume em {nivel} por cento.")

    def _cmd_volume_mais(self, _n) -> Resultado:
        vol = _sessao_audio()
        atual = int(vol.GetMasterVolumeLevelScalar() * 100)
        return self._cmd_volume(min(100, atual + 10))

    def _cmd_volume_menos(self, _n) -> Resultado:
        vol = _sessao_audio()
        atual = int(vol.GetMasterVolumeLevelScalar() * 100)
        return self._cmd_volume(max(0, atual - 10))

    def _cmd_mudo(self, _n) -> Resultado:
        _sessao_audio().SetMute(1, None)
        return Resultado("Mudo ativado.")

    def _cmd_desmudo(self, _n) -> Resultado:
        _sessao_audio().SetMute(0, None)
        return Resultado("Mudo desativado.")

    def _cmd_brilho(self, nivel) -> Resultado:
        if nivel is None:
            return Resultado("Diga o brilho de 0 a 100.", ok=False)
        try:
            import screen_brightness_control as sbc

            sbc.set_brightness(max(0, min(100, int(nivel))))
            return Resultado(f"Brilho em {nivel} por cento.")
        except Exception:
            return Resultado("Este monitor não permite ajustar brilho por software.", ok=False)

    def _cmd_bloquear(self, _n) -> Resultado:
        ctypes.windll.user32.LockWorkStation()
        return Resultado("Tela bloqueada.")

    def _cmd_status(self, _n) -> Resultado:
        cpu = psutil.cpu_percent(interval=0.5)
        ram = psutil.virtual_memory()
        try:
            bat = psutil.sensors_battery()
            txt_bat = f", bateria em {int(bat.percent)} por cento" if bat else ""
        except Exception:
            txt_bat = ""
        return Resultado(
            f"Processador em {cpu:.0f} por cento, memória em {ram.percent:.0f} por cento{txt_bat}."
        )

    def _cmd_hora(self, _n) -> Resultado:
        agora = datetime.datetime.now()
        return Resultado(f"São {agora.hour} horas e {agora.minute:02d} minutos.")

    def _cmd_data(self, _n) -> Resultado:
        from core.router import rotear

        return Resultado(rotear("que dia é hoje")["resposta"])
