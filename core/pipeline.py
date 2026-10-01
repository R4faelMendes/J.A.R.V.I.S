"""Pipeline de voz do JARVIS 2.0 (Fase 5) — o maestro responsivo.

- Modelos pré-carregados ao iniciar (Whisper, Piper, wake word, LLM aquecido).
- Roteador instantâneo ANTES do LLM (comandos comuns em <1 s, sem IA).
- Streaming LLM -> TTS por frase; respostas curtas por padrão.
- Barge-in: dizer "Jarvis" (ou Ctrl+Alt+J) durante a fala CORTA o áudio na hora.
- Filtro de eco: enquanto fala, a palavra precisa de score MAIOR (alto-falantes).
- Modo texto funciona sem microfone (útil para testes e celular).
Uso (texto):
    from core.pipeline import JarvisPipeline
    j = JarvisPipeline(); await j.aquecer_tudo()
    r = await j.processar_texto("que horas são?")
"""
from __future__ import annotations

import asyncio
import datetime
import re
import threading
import time
from pathlib import Path
from typing import Any

from core.brain import Brain
from core.router import rotear
from core.events import BUS
from core.states import ESTADOS, ERROR, IDLE, LISTENING, SPEAKING, THINKING
import uuid
from voice.stt import STT
from voice.tts import TTS
from voice.wakeword import WakeListener

try:
    from skills.executor import Executor
except Exception:  # pipeline funciona mesmo sem skills
    Executor = None  # type: ignore[assignment]

RAIZ = Path(__file__).resolve().parents[1]


def limpar_para_fala(texto: str) -> str:
    """Remove markdown/código/URLs para a fala sair natural."""
    texto = re.sub(r"```.*?```", " código omitido. ", texto, flags=re.DOTALL)
    texto = re.sub(r"`([^`]*)`", r"\1", texto)
    texto = re.sub(r"!\[.*?\]\(.*?\)", "", texto)
    texto = re.sub(r"\[([^\]]*)\]\(.*?\)", r"\1", texto)
    texto = re.sub(r"(?m)^#{1,6}\s*", "", texto)
    texto = re.sub(r"[*_~>|]", "", texto)
    texto = re.sub(r"https?://\S+", "link omitido", texto)
    return re.sub(r"\s+", " ", texto).strip()


class JarvisPipeline:
    """Orquestra STT -> roteador/LLM -> TTS com interrupção e métricas."""

    def __init__(self) -> None:
        self.brain = Brain()
        self.tts = TTS()
        self.stt: STT | None = None          # pesado: carrega em aquecer_tudo()
        self.wake: WakeListener | None = None
        self.falando = False                 # True durante playback (filtro de eco)
        self._limiar_base = 0.5
        self._ouvindo = threading.Event()    # True enquanto grava comando
        self._pendente: dict | None = None   # ação aguardando "confirmo"
        self._auto_consecutivas: int = 0     # detector de loop de auto-respostas (Tarefa 3)
        self.executor = Executor() if Executor is not None else None

    async def aquecer_tudo(self) -> dict[str, float]:
        """Pré-carrega todos os modelos e mede cada etapa. Registra no log."""
        tempos: dict[str, float] = {}
        t0 = time.perf_counter()
        self.stt = STT()
        tempos["whisper_s"] = time.perf_counter() - t0
        t0 = time.perf_counter()
        self.tts.aquecer()
        tempos["piper_s"] = time.perf_counter() - t0
        t0 = time.perf_counter()
        self.wake = WakeListener(on_wake=self._ao_acordar)
        self._limiar_base = self.wake.limiar
        self.wake._carregar_modelo()
        tempos["wake_s"] = time.perf_counter() - t0
        t0 = time.perf_counter()
        medida = await self.brain.aquecer()
        tempos["llm_s"] = time.perf_counter() - t0
        tempos["llm_primeiro_token_s"] = medida["primeiro_token_s"]
        linha = (
            f"{datetime.datetime.now():%Y-%m-%d %H:%M:%S} | fase5 aquecer | "
            + " | ".join(f"{k}={v:.2f}" for k, v in tempos.items()) + "\n"
        )
        (RAIZ / "logs" / "perf.log").write_text(
            ((RAIZ / "logs" / "perf.log").read_text(encoding="utf-8") if (RAIZ / "logs" / "perf.log").exists() else "")
            + linha, encoding="utf-8")
        return tempos

    # ---------------- barge-in / eco ----------------
    def _ao_acordar(self, score: float = 0.0) -> None:
        """Callback da wake word / atalho: se estiver falando, CORTA na hora."""
        if self.falando:
            self.tts.parar()

    def _eco_strito(self, ligado: bool) -> None:
        """Enquanto fala pelos alto-falantes, exige score maior p/ interromper."""
        if self.wake is not None:
            self.wake.limiar = self._limiar_base + (0.2 if ligado else 0.0)

    # ---------------- turnos ----------------
    async def processar_texto(self, texto: str, falar: bool = True,
                              origem_humana: bool = True,
                              msg_id: str | None = None) -> dict[str, Any]:
        """Processa UM comando em texto. Retorna tempos, resposta e ação.

        falar=False: não toca áudio (testes silenciosos), mas sintetiza a
        1ª frase para medir a latência real até o áudio ficar pronto.
        """
        # Detecção de loop: mais de 3 respostas automáticas seguidas sem entrada humana
        if origem_humana:
            self._auto_consecutivas = 0
        else:
            self._auto_consecutivas += 1
            if self._auto_consecutivas > 3:
                BUS.emitir("LOOP_DETECTADO", {"consecutivas": self._auto_consecutivas})
                ESTADOS.definir(ERROR, "loop detectado: >3 respostas automáticas sem entrada humana")
                msg_erro = "Operação interrompida: loop de respostas automáticas detectado (> 3 sem entrada humana)."
                mid = msg_id or f"resp_{int(time.time() * 1000)}_{uuid.uuid4().hex[:6]}"
                BUS.emitir("RESPONSE_START", {"id": mid, "seq": 0, "origem": "erro"})
                BUS.emitir("RESPONSE_DELTA", {"id": mid, "seq": 1, "delta": msg_erro})
                BUS.emitir("RESPONSE_END", {"id": mid, "seq": 2, "texto_final": msg_erro})
                return {"id": mid, "entrada": texto, "origem": "erro", "resposta": msg_erro,
                        "acao": None, "primeiro_audio_s": 0.0}

        if not msg_id:
            msg_id = f"resp_{int(time.time() * 1000)}_{uuid.uuid4().hex[:6]}"

        t0 = time.perf_counter()
        resultado: dict[str, Any] = {"id": msg_id, "entrada": texto, "acao": None}
        # Confirmação pendente: "confirmo" executa; outra coisa cancela.
        if self._pendente is not None:
            if re.match(r"^(confirmo|confirmar|sim,? pode|pode executar)\.?$", texto.strip().lower()):
                acao = self._pendente
                self._pendente = None
                return await self._executar_acao(acao, texto, t0, falar, confirmada=True, msg_id=msg_id)
            self._pendente = None
        rota = rotear(texto)
        if rota is not None:
            resposta = rota["resposta"]
            resultado.update({"origem": "roteador", "resposta": resposta, "acao": rota["acao"]})
            # Emite eventos de protocolo do roteador rápido
            BUS.emitir("RESPONSE_START", {"id": msg_id, "seq": 0, "origem": "roteador"})
            BUS.emitir("RESPONSE_DELTA", {"id": msg_id, "seq": 1, "delta": resposta})
            BUS.emitir("RESPONSE_END", {"id": msg_id, "seq": 2, "texto_final": resposta})

            if rota["acao"] and rota["acao"].get("metodo") == "parar_tudo":
                self.tts.parar()
            elif rota["acao"] and self.executor is not None:
                return await self._executar_acao(rota["acao"], texto, t0, falar, msg_id=msg_id)
            t_sint = time.perf_counter()
            if falar:
                self._falar_bloqueante(resposta)
            else:
                from voice.tts import dividir_frases
                frases = dividir_frases(resposta)
                if frases:
                    self.tts.sintetizar(frases[0])
            resultado["primeiro_audio_s"] = time.perf_counter() - t0
            resultado["sintese_s"] = time.perf_counter() - t_sint
            self._log_turno(resultado, time.perf_counter() - t0)
            return resultado

        # Caminho LLM com streaming e timestamps.
        marcas: dict[str, float] = {}
        brain = self.brain
        ESTADOS.definir(THINKING, "llm")
        BUS.emitir("LLM_START", {"entrada": texto[:80]})
        BUS.emitir("RESPONSE_START", {"id": msg_id, "seq": 0, "origem": "llm"})

        seq = 0

        async def observado():
            nonlocal seq
            async for ev in brain.chat(texto, tools=self._tools_llm()):
                if "token" in ev:
                    # Tira marcas de markdown na hora: nunca pertencem à fala.
                    token_limpo = re.sub(r"[*_#`~>|]", "", ev["token"])
                    if token_limpo:
                        seq += 1
                        BUS.emitir("RESPONSE_DELTA", {"id": msg_id, "seq": seq, "delta": token_limpo})
                    ev = {"token": token_limpo}
                    if "primeiro_token" not in marcas:
                        marcas["primeiro_token"] = time.perf_counter() - t0
                        BUS.emitir("LLM_FIRST_TOKEN",
                                   {"s": round(marcas["primeiro_token"], 2)})
                elif "tool_calls" in ev:
                    marcas["tools"] = ev["tool_calls"]
                yield ev

        resultado["origem"] = "llm"
        if falar:
            tarefa = asyncio.create_task(self._falar_streaming_monitorado(observado()))
            # Primeiro áudio = quando o TTS começa a tocar de fato.
            while not tarefa.done():
                if self.tts.falando and "primeiro_audio" not in marcas:
                    marcas["primeiro_audio"] = time.perf_counter() - t0
                await asyncio.sleep(0.05)
            resposta = limpar_para_fala(await tarefa)
            if "primeiro_audio" not in marcas:  # resposta vazia/curta demais
                marcas["primeiro_audio"] = time.perf_counter() - t0
        else:
            partes = []
            erro_cerebro = ""
            async for ev in observado():
                if "token" in ev:
                    partes.append(ev["token"])
                elif "erro" in ev and not erro_cerebro:
                    # Nunca engolir erro em silêncio (causava resposta vazia).
                    erro_cerebro = str(ev["erro"])[:200]
                    BUS.emitir("LLM_ERRO", {"erro": erro_cerebro})
            resposta = limpar_para_fala("".join(partes))
            if not resposta and erro_cerebro:
                resposta = f"Desculpe, tive um problema: {erro_cerebro}"
            from voice.tts import dividir_frases
            frases = dividir_frases(resposta)
            if frases:
                self.tts.sintetizar(frases[0])
            marcas["primeiro_audio"] = time.perf_counter() - t0
        resultado["resposta"] = resposta
        resultado.update({k: v for k, v in marcas.items() if k != "tools"})
        BUS.emitir("LLM_END", {"origem": resultado.get("origem"),
                               "primeiro_audio_s": round(resultado.get("primeiro_audio", 0), 2)})
        if marcas.get("tools") and self.executor is not None:
            extras = []
            for chamada in marcas["tools"]:
                fn = (chamada.get("function") or {})
                nome = str(fn.get("name", "")).removeprefix("skill_")
                try:
                    import json as _json
                    args = fn.get("arguments", {})
                    if isinstance(args, str):
                        args = _json.loads(args)
                except Exception:
                    args = {}
                r = self.executor.executar({"skill": nome, "metodo": "_direto", "args": args})
                extras.append(r.resposta)
            if extras:
                resumo = " ".join(extras)[:300]
                resultado["resposta"] = f"{resposta} {resumo}".strip()
                seq += 1
                BUS.emitir("RESPONSE_DELTA", {"id": msg_id, "seq": seq, "delta": f" {resumo}"})
                if falar:
                    self._falar_bloqueante(resumo)
        seq += 1
        BUS.emitir("RESPONSE_END", {"id": msg_id, "seq": seq, "texto_final": resultado["resposta"]})
        self._log_turno(resultado, time.perf_counter() - t0)
        return resultado

    async def _executar_acao(self, acao: dict, texto: str, t0: float,
                             falar: bool, confirmada: bool = False,
                             msg_id: str | None = None) -> dict[str, Any]:
        """Roda a skill; se pedir confirmação, guarda pendente e pergunta."""
        mid = msg_id or f"resp_{int(time.time() * 1000)}_{uuid.uuid4().hex[:6]}"
        r = self.executor.executar(acao, confirmar=confirmada)
        if r.dados.get("precisa_confirmar") and not confirmada:
            self._pendente = acao
            BUS.emitir("CONFIRMACAO_PENDENTE", {"acao": acao})
        resposta = r.resposta
        resultado = {"id": mid, "entrada": texto, "origem": "skill", "resposta": resposta, "acao": acao}
        BUS.emitir("RESPONSE_START", {"id": mid, "seq": 0, "origem": "skill"})
        BUS.emitir("RESPONSE_DELTA", {"id": mid, "seq": 1, "delta": resposta})
        BUS.emitir("RESPONSE_END", {"id": mid, "seq": 2, "texto_final": resposta})
        if falar:
            self._falar_bloqueante(resposta)
        else:
            from voice.tts import dividir_frases
            frases = dividir_frases(resposta)
            if frases:
                self.tts.sintetizar(frases[0])
        resultado["primeiro_audio_s"] = time.perf_counter() - t0
        self._log_turno(resultado, time.perf_counter() - t0)
        return resultado

    def _falar_bloqueante(self, texto: str) -> None:
        self.falando = True
        self._eco_strito(True)
        ESTADOS.definir(SPEAKING, "fala direta")
        try:
            self.tts.falar(texto)
        finally:
            self.falando = False
            self._eco_strito(False)
            ESTADOS.definir(IDLE, "fim da fala")

    async def _falar_streaming_monitorado(self, tokens) -> str:
        self.falando = True
        self._eco_strito(True)
        ESTADOS.definir(SPEAKING, "fala streaming")
        try:
            return await self.tts.falar_tokens(tokens)
        finally:
            self.falando = False
            self._eco_strito(False)
            ESTADOS.definir(IDLE, "fim do streaming")

    def _tools_llm(self) -> list | None:
        """Esquemas das skills ativadas p/ tool calling (None se sem executor)."""
        try:
            ferramentas = self.executor.ferramentas_llm() if self.executor else []
            return ferramentas or None
        except Exception:
            return None

    def _log_turno(self, resultado: dict, total: float) -> None:
        linha = (
            f"{datetime.datetime.now():%Y-%m-%d %H:%M:%S} | fase5 turno | "
            f"origem={resultado.get('origem')} | "
            f"primeiro_audio_s={resultado.get('primeiro_audio_s', resultado.get('primeiro_audio', 0)):.2f} | "
            f"total_s={total:.2f} | entrada={resultado.get('entrada', '')[:60]}\n"
        )
        log = RAIZ / "logs" / "perf.log"
        log.write_text((log.read_text(encoding="utf-8") if log.exists() else "") + linha, encoding="utf-8")

    # ---------------- modo voz (precisa do microfone) ----------------
    def rodar_voz(self) -> None:
        """Loop de voz: espera 'Jarvis' -> bip -> comando -> resposta. Ctrl+C sai."""
        import winsound

        if self.stt is None or self.wake is None:
            raise RuntimeError("Chame await aquecer_tudo() antes do modo voz.")
        self.wake.iniciar()
        print("Diga 'Jarvis' ou pressione Ctrl+Alt+J. Ctrl+C para sair.")
        try:
            while True:
                evento = self._aguardar_ativacao()
                if evento == "sair":
                    break
                winsound.Beep(660, 150)  # bip suave: estou ouvindo
                ESTADOS.definir(LISTENING, "gravando comando")
                try:
                    ouvido = self.stt.ouvir_uma_frase()
                except RuntimeError as erro:
                    ESTADOS.definir(IDLE, "mic falhou")
                    print(f"[mic] {erro}")
                    continue
                ESTADOS.definir(THINKING, "comando ouvido")
                if not ouvido["texto"]:
                    continue
                print(f"Você: {ouvido['texto']}")
                resp = asyncio.run(self.processar_texto(ouvido["texto"]))
                print(f"Jarvis: {resp['resposta']}")
                self.wake.manter_conversa()  # follow-ups por ~8 s
        except KeyboardInterrupt:
            print("\nEncerrando.")
        finally:
            if self.wake:
                self.wake.parar()

    def _aguardar_ativacao(self) -> str:
        """Bloqueia até wake word, atalho ou follow-up no modo conversa."""
        gatilho: threading.Event = threading.Event()

        orig = self.wake.on_wake

        def marcado() -> None:
            orig()
            gatilho.set()

        self.wake.on_wake = marcado  # type: ignore[assignment]
        try:
            while not gatilho.is_set():
                if self.wake.em_conversa():
                    # No modo conversa, qualquer fala conta: checagem curta.
                    return "conversa"
                time.sleep(0.1)
            return "wake"
        finally:
            self.wake.on_wake = orig
