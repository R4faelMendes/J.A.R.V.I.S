"""Voz de saída do JARVIS 2.0 (Fase 2) — Piper TTS via binário oficial + sounddevice.

- Sintetiza por FRASE: cada frase é tocada assim que fica pronta,
  enquanto o LLM gera as próximas (streaming LLM -> TTS).
- Fila de áudio com pré-síntese da próxima frase (sobrepõe CPU e playback).
- Barge-in: parar() interrompe a fala imediatamente (thread-safe).
- 100% local: piper.exe + voz .onnx em models/. Nenhum pip piper.
Uso:
    from voice.tts import TTS
    tts = TTS()          # lê config.yaml
    tts.aquecer()        # pré-carrega (chamar ao iniciar o Jarvis)
    tts.falar("Olá! Como posso ajudar?")
    # com streaming do cérebro:
    # await tts.falar_tokens(cerebro.chat("conte uma piada"))
"""
from __future__ import annotations

import json
import queue
import re
import subprocess
import tempfile
import threading
import wave
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from typing import AsyncIterator

import numpy as np
import sounddevice as sd
import yaml

RAIZ = Path(__file__).resolve().parents[1]

# Divide texto em frases na pontuação final. Abreviações comuns ficam inteiras.
_PADRAO_FRASE = re.compile(r"(.+?[.!?…]+[\s\"”)]*|.+$)", re.DOTALL)


def dividir_frases(texto: str) -> list[str]:
    """Divide um texto em frases para síntese incremental."""
    texto = re.sub(r"\s+", " ", texto).strip()
    if not texto:
        return []
    frases = [m.group(1).strip() for m in _PADRAO_FRASE.finditer(texto)]
    # Junta fragmentos muito curtos (ex.: "Sr.") com a frase seguinte.
    unidas: list[str] = []
    for frase in frases:
        if unidas and len(unidas[-1]) < 12 and not re.search(r"[.!?…]\s*$", unidas[-1]):
            unidas[-1] = f"{unidas[-1]} {frase}"
        else:
            unidas.append(frase)
    return [f for f in unidas if f]


class ServidorPiper:
    """Processo Piper persistente: carrega a voz UMA vez, recebe frases no stdin.

    Protocolo: escreve "frase\\n" no stdin, lê PCM int16 mono no stdout até
    o silêncio final de cada sentença (--sentence-silence). Elimina ~0,4-1,0 s
    de carga do modelo a cada frase. Thread-safe via trava interna.
    """

    def __init__(self, piper_exe: Path, voz_onnx: Path, length_scale: float) -> None:
        try:
            cfg = json.loads((voz_onnx.parent / f"{voz_onnx.name}.json").read_text(encoding="utf-8"))
            # Formato real: {"audio": {"sample_rate": 22050}, ...}
            self.taxa: int = int(cfg.get("audio", {}).get("sample_rate", 22050))
        except Exception:
            self.taxa = 22050
        self._trava = threading.Lock()
        espeak_dir = piper_exe.parent / "espeak-ng-data"
        cmd = [
            str(piper_exe), "--model", str(voz_onnx),
            "--output_raw", "--sentence-silence", "0.3",
            "--length-scale", str(length_scale), "-q"
        ]
        if espeak_dir.is_dir():
            cmd.extend(["--espeak_data", str(espeak_dir)])
        try:
            self.proc = subprocess.Popen(
                cmd,
                stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
                cwd=str(piper_exe.parent), bufsize=0,
            )
        except Exception as erro:
            raise RuntimeError(f"Não iniciei o servidor Piper: {erro}") from erro

    def vivo(self) -> bool:
        """True se o processo continua rodando."""
        return self.proc is not None and self.proc.poll() is None

    def sintetizar(self, frase: str) -> tuple[int, np.ndarray]:
        """Sintetiza UMA frase pelo processo persistente (reinicia se morrer)."""
        frase = frase.strip()
        if not frase:
            return self.taxa, np.zeros(0, dtype=np.float32)
        with self._trava:
            if not self.vivo():
                raise RuntimeError("Servidor Piper morreu.")
            try:
                self.proc.stdin.write((frase + "\n").encode("utf-8"))
                self.proc.stdin.flush()
                return self.taxa, self._ler_sentenca()
            except Exception as erro:
                raise RuntimeError(f"Falha no servidor Piper: {erro}") from erro

    def _ler_sentenca(self, limite_s: float = 30.0) -> np.ndarray:
        """Lê PCM até voz + silêncio final de 0,3 s (com trava de segurança)."""
        quadro = 1024
        bytes_quadro = quadro * 2
        silencio_necessario = int(self.taxa * 0.35)  # margem sobre 0.3 s
        silencio = 0
        com_voz = False
        pedacos: list[bytes] = []
        total = 0
        limite = int(self.taxa * limite_s)
        while total < limite:
            bloco = self.proc.stdout.read(bytes_quadro)
            if not bloco:
                break
            pedacos.append(bloco)
            total += len(bloco) // 2
            pico = max(abs(int.from_bytes(bloco[i:i + 2], "little", signed=True))
                       for i in range(0, len(bloco) - 1, 2))
            if pico > 400:
                com_voz = True
                silencio = 0
            elif com_voz:
                silencio += len(bloco) // 2
                if silencio >= silencio_necessario:
                    break
        if not com_voz:
            raise RuntimeError("Piper não retornou áudio (frase vazia ou voz travada).")
        bruto = b"".join(pedacos)
        return (np.frombuffer(bruto, dtype=np.int16).astype(np.float32) / 32768.0).copy()

    def fechar(self) -> None:
        """Encerra o processo com delicadeza."""
        with self._trava:
            try:
                if self.proc is not None:
                    if self.proc.stdin:
                        self.proc.stdin.close()
                    self.proc.terminate()
                    self.proc.wait(timeout=5)
            except Exception:
                try:
                    if self.proc is not None:
                        self.proc.kill()
                except Exception:
                    pass
            finally:
                self.proc = None


class TTS:
    """Sintetizador Piper com playback em fila e interrupção imediata."""

    def __init__(self, config_path: Path | str | None = None, voz: str | None = None) -> None:
        caminho = Path(config_path) if config_path else RAIZ / "config.yaml"
        try:
            cfg = yaml.safe_load(caminho.read_text(encoding="utf-8"))
        except Exception as erro:
            raise RuntimeError(f"Não li {caminho}: {erro}") from erro
        tts = cfg.get("tts", {})
        self.piper_exe = RAIZ / str(tts.get("binario_piper", "models/piper/piper.exe"))
        self.voz_nome = voz or str(tts.get("voz", "pt_BR-faber-medium"))
        pasta_vozes = RAIZ / str(tts.get("pasta_vozes", "models/piper-voices"))
        self.voz_onnx = pasta_vozes / f"{self.voz_nome}.onnx"
        velocidade = float(tts.get("velocidade", 1.0))
        # Piper usa length-scale (inverso): <1 = mais rápido.
        self.length_scale = max(0.5, min(1.5, 1.0 / velocidade if velocidade > 0 else 1.0))
        self.dispositivo_saida = cfg.get("audio", {}).get("alto_falante_indice")
        self._parar = threading.Event()
        self._falando = threading.Event()
        self.servidor: ServidorPiper | None = None
        if not self.piper_exe.is_file():
            raise FileNotFoundError(f"Binário Piper não encontrado: {self.piper_exe}")
        if not self.voz_onnx.is_file():
            raise FileNotFoundError(f"Voz não encontrada: {self.voz_onnx} (+ .json ao lado)")

    def iniciar_servidor(self) -> None:
        """Sobe o processo persistente (voz carregada uma vez). Chamar no boot."""
        if self.servidor is None or not self.servidor.vivo():
            self.servidor = ServidorPiper(self.piper_exe, self.voz_onnx, self.length_scale)

    def fechar(self) -> None:
        """Encerra o servidor persistente (ao desligar o Jarvis)."""
        if self.servidor is not None:
            self.servidor.fechar()
            self.servidor = None

    @property
    def falando(self) -> bool:
        """True enquanto há áudio sendo tocado."""
        return self._falando.is_set()

    def aquecer(self) -> None:
        """Pré-sintetiza uma frase curta (sobe o servidor + traz voz p/ o cache)."""
        self.iniciar_servidor()
        self.sintetizar("Pronto.")

    def sintetizar(self, frase: str) -> tuple[int, np.ndarray]:
        """Sintetiza UMA frase: via servidor persistente, com fallback one-shot."""
        if self.servidor is not None and self.servidor.vivo():
            try:
                return self.servidor.sintetizar(frase)
            except Exception:
                pass  # cai para o one-shot abaixo
        return self._sintetizar_oneshot(frase)

    def _sintetizar_oneshot(self, frase: str) -> tuple[int, np.ndarray]:
        """Sintetiza UMA frase num processo novo (lento; reserva)."""
        frase = frase.strip()
        if not frase:
            return 22050, np.zeros(0, dtype=np.float32)
        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
            saida = tmp.name
        try:
            espeak_dir = self.piper_exe.parent / "espeak-ng-data"
            cmd = [
                str(self.piper_exe),
                "--model", str(self.voz_onnx),
                "--output_file", saida,
                "--length-scale", str(self.length_scale),
                "-q",
            ]
            if espeak_dir.is_dir():
                cmd.extend(["--espeak_data", str(espeak_dir)])
            proc = subprocess.run(
                cmd,
                input=frase.encode("utf-8"),
                capture_output=True,
                timeout=60,
            )
            if proc.returncode != 0 or not Path(saida).is_file():
                detalhe = (proc.stderr or b"")[-500:].decode("utf-8", "replace")
                raise RuntimeError(f"Piper falhou (código {proc.returncode}): {detalhe}")
            with wave.open(saida, "rb") as wav:
                taxa = wav.getframerate()
                n = wav.getnframes()
                bruto = wav.readframes(n)
                largura = wav.getsampwidth()
        finally:
            Path(saida).unlink(missing_ok=True)
        if largura == 2:
            audio = np.frombuffer(bruto, dtype=np.int16).astype(np.float32) / 32768.0
        elif largura == 4:
            audio = np.frombuffer(bruto, dtype=np.int32).astype(np.float32) / 2147483648.0
        else:
            raise RuntimeError(f"Formato WAV inesperado do Piper: {largura * 8} bits")
        return taxa, audio

    def _tocar_winsound_memoria(self, taxa: int, audio: np.ndarray) -> None:
        """Fallback de áudio: toca via winsound do Windows usando buffer em memória."""
        import io
        import winsound
        pcm = (np.clip(audio, -1.0, 1.0) * 32767).astype(np.int16)
        buf = io.BytesIO()
        with wave.open(buf, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(taxa)
            w.writeframes(pcm.tobytes())
        winsound.PlaySound(buf.getvalue(), winsound.SND_MEMORY)

    def _tocar(self, taxa: int, audio: np.ndarray) -> bool:
        """Toca um trecho; retorna False se foi interrompido (barge-in)."""
        if self._parar.is_set() or audio.size == 0:
            return False
        self._falando.set()
        try:
            try:
                sd.play(audio, taxa, device=self.dispositivo_saida)
                # Espera em fatias curtas para reagir ao parar() em ~50 ms.
                total_ms = int(len(audio) / taxa * 1000)
                esperado = 0
                while esperado < total_ms:
                    if self._parar.is_set():
                        sd.stop()
                        return False
                    sd.sleep(50)
                    esperado += 50
                sd.wait()
            except Exception:
                # Fallback de reserva se o sounddevice falhar no dispositivo
                if not self._parar.is_set():
                    self._tocar_winsound_memoria(taxa, audio)
            return not self._parar.is_set()
        finally:
            if not self._parar.is_set():
                self._falando.clear()

    def parar(self) -> None:
        """Interrompe a fala IMEDIATAMENTE (barge-in). Thread-safe."""
        self._parar.set()
        try:
            sd.stop()
        except Exception:
            pass
        self._falando.clear()

    def falar(self, texto: str) -> None:
        """Fala um texto completo, frase a frase, com pré-síntese sobreposta."""
        self._parar.clear()
        frases = dividir_frases(texto)
        if not frases:
            return
        with ThreadPoolExecutor(max_workers=1) as executor:
            futuro = executor.submit(self.sintetizar, frases[1]) if len(frases) > 1 else None
            taxa, audio = self.sintetizar(frases[0])
            for i in range(len(frases)):
                if not self._tocar(taxa, audio):
                    break
                if futuro is not None:
                    taxa, audio = futuro.result()
                    seguinte = i + 2
                    futuro = executor.submit(self.sintetizar, frases[seguinte]) if seguinte < len(frases) else None
        self._falando.clear()

    async def falar_tokens(self, tokens: AsyncIterator[dict]) -> str:
        """Fala o streaming do cérebro: sintetiza cada frase assim que fecha.

        Recebe o gerador de eventos do Brain.chat e retorna o texto completo.
        """
        self._parar.clear()
        buffer = ""
        completo: list[str] = []
        fila: queue.Queue = queue.Queue()
        fim = object()

        def produtor_sintese(frase: str) -> None:
            try:
                fila.put(self.sintetizar(frase))
            except Exception as erro:  # noqa: BLE001 - não derruba a fala
                fila.put(erro)

        def entregador() -> None:
            while True:
                item = fila.get()
                if item is fim or self._parar.is_set():
                    return
                if isinstance(item, Exception):
                    continue
                taxa, audio = item
                self._tocar(taxa, audio)

        voz_thread = threading.Thread(target=entregador, daemon=True)
        voz_thread.start()
        try:
            async for evento in tokens:
                if self._parar.is_set():
                    break
                if "erro" in evento:
                    texto_erro = "Desculpe, tive um problema no cérebro."
                    produtor_sintese(texto_erro)
                    completo.append(texto_erro)
                    break
                if "token" in evento:
                    buffer += evento["token"]
                    frases = dividir_frases(buffer)
                    # Só envia frases FECHADAS (com pontuação final); guarda o resto.
                    while len(frases) > 1 or (
                        len(frases) == 1 and re.search(r"[.!?…][\s\"”)]*$", frases[0].strip())
                    ):
                        pronta = frases.pop(0).strip()
                        if pronta:
                            completo.append(pronta)
                            produtor_sintese(pronta)
                        buffer = " ".join(frases)
                        frases = dividir_frases(buffer) if buffer else []
                        if not buffer:
                            break
            resto = buffer.strip()
            if resto and not self._parar.is_set():
                completo.append(resto)
                produtor_sintese(resto)
        finally:
            # Aguarda a fila esvaziar antes de encerrar o entregador.
            while not fila.empty():
                sd.sleep(50)
            fila.put(fim)
            voz_thread.join(timeout=30)
            self._falando.clear()
        return " ".join(completo)
