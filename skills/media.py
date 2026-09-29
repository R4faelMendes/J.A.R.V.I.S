"""Skill media (Fase 6): play/pause/próxima/anterior/volume por teclas de mídia."""
from __future__ import annotations

from skills.base import Resultado, Skill

TECLAS = {
    "tocar": "play/pause media",
    "pausar": "play/pause media",
    "proxima": "next track",
    "anterior": "previous track",
    "volume_mais": "volume up",
    "volume_menos": "volume down",
    "mudo": "volume mute",
}


class MediaSkill(Skill):
    """Controla o player atual via teclas de mídia do Windows."""

    nome = "media"
    descricao = "Mídia: tocar, pausar, proxima, anterior, volume_mais, volume_menos, mudo."
    parametros = {"metodo": {"type": "string"}}

    def run(self, metodo: str = "tocar", **kwargs) -> Resultado:
        tecla = TECLAS.get((metodo or "").strip())
        if not tecla:
            return Resultado(f"Comando de mídia desconhecido: {metodo}.", ok=False)
        try:
            import keyboard

            keyboard.send(tecla)
            return Resultado({"tocar": "Tocando.", "pausar": "Pausado.",
                              "proxima": "Próxima.", "anterior": "Anterior."}.get(metodo, "Feito."))
        except Exception as erro:  # noqa: BLE001
            return Resultado(f"Não consegui controlar a mídia: {erro}.", ok=False)
