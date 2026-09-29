"""Roteador de intenções do JARVIS 2.0 (Fase 5) — respostas instantâneas sem LLM.

Comandos comuns (hora, data, pare, ajuda...) respondem em <1 s.
Ações de controle (abrir app, volume...) viram "ações" que as skills
executam na Fase 6; o pipeline confirma na hora com frase curta.
Tudo com regex/palavras-chave: zero IA, zero espera.
"""
from __future__ import annotations

import datetime
import random
import re

CONFIRMACOES = [
    "Certo.",
    "Feito.",
    "Pronto.",
    "Já estou cuidando disso.",
    "Entendido.",
    "Pode deixar.",
]

AJUDA = (
    "Posso informar hora e data, abrir programas, ajustar volume, "
    "pesquisar na web e conversar. Diga pare para me interromper."
)


def _hora() -> str:
    agora = datetime.datetime.now()
    return f"São {agora.hour} horas e {agora.minute:02d} minutos."


def _data() -> str:
    dias = ["segunda-feira", "terça-feira", "quarta-feira", "quinta-feira",
            "sexta-feira", "sábado", "domingo"]
    meses = ["janeiro", "fevereiro", "março", "abril", "maio", "junho",
             "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"]
    hoje = datetime.date.today()
    return f"Hoje é {dias[hoje.weekday()]}, {hoje.day} de {meses[hoje.month - 1]} de {hoje.year}."


def rotear(texto: str) -> dict | None:
    """Roteia um comando. Retorna None se precisar do LLM.

    Formato: {"resposta": str, "acao": dict|None, "instantaneo": True}
    Ações: {"skill": "apps"|"system"|"media"|"notes", "metodo": str, "args": dict}
    """
    t = texto.strip().lower()
    if not t:
        return {"resposta": "Não ouvi nada. Pode repetir?", "acao": None, "instantaneo": True}

    if re.search(r"\bpare\b|\bpara tudo\b|\bcancele?\b|\bchega\b", t):
        return {"resposta": random.choice(["Certo, parei.", "Parei.", "Ok, interrompido."]),
                "acao": {"skill": "_core", "metodo": "parar_tudo", "args": {}},
                "instantaneo": True}

    if re.search(r"que horas|qual.*hora|me diz.*hora|hora atual|horas são", t):
        return {"resposta": _hora(), "acao": None, "instantaneo": True}

    if re.search(r"que dia|qual.*data|data de hoje|dia de hoje|que dia é hoje", t):
        return {"resposta": _data(), "acao": None, "instantaneo": True}

    if re.search(r"\bo (que )?(você|voce) (é|e)|quem é você|se apresente|sobre você", t):
        return {"resposta": "Eu sou o Jarvis, seu assistente pessoal. Rodo aqui no seu computador.",
                "acao": None, "instantaneo": True}

    if re.search(r"\bajuda\b|o que (você|voce) (sabe|faz|pode)|comandos", t):
        return {"resposta": AJUDA, "acao": None, "instantaneo": True}

    m = re.search(r"\babr[ai]r?\b\s+(?:o\s+|a\s+|os\s+|as\s+)?(.+)", t)
    if m:
        alvo = m.group(1).strip()
        return {"resposta": f"Abrindo {alvo}.",
                "acao": {"skill": "apps", "metodo": "abrir", "args": {"alvo": alvo}},
                "instantaneo": True}

    m = re.search(r"\bfech[ae]r?\b\s+(?:o\s+|a\s+|os\s+|as\s+)?(.+)", t)
    if m:
        alvo = m.group(1).strip()
        return {"resposta": f"Fechando {alvo}.",
                "acao": {"skill": "apps", "metodo": "fechar", "args": {"alvo": alvo}},
                "instantaneo": True}

    m = re.search(r"volume\s+(?:para\s+)?(\d{1,3})", t)
    if m:
        nivel = max(0, min(100, int(m.group(1))))
        return {"resposta": f"Volume em {nivel} por cento.",
                "acao": {"skill": "system", "metodo": "volume", "args": {"nivel": nivel}},
                "instantaneo": True}
    if re.search(r"aumenta.*volume|volume.*mais|aumentar o volume", t):
        return {"resposta": "Aumentando o volume.",
                "acao": {"skill": "system", "metodo": "volume_mais", "args": {}},
                "instantaneo": True}
    if re.search(r"(abaixa|diminui|baixa).*(volume|som)|volume.*menos|menos volume", t):
        return {"resposta": "Baixando o volume.",
                "acao": {"skill": "system", "metodo": "volume_menos", "args": {}},
                "instantaneo": True}
    if re.search(r"\b(mudo|mutar|sem som|silenciar)\b", t):
        return {"resposta": "Ativando o mudo.",
                "acao": {"skill": "system", "metodo": "mudo", "args": {}},
                "instantaneo": True}
    if re.search(r"\b(desmutar|tirar.*mudo|com som)\b", t):
        return {"resposta": "Tirando o mudo.",
                "acao": {"skill": "system", "metodo": "desmudo", "args": {}},
                "instantaneo": True}

    if re.search(r"pausar|pause|pausa a m[úu]sica", t):
        return {"resposta": "Pausando.",
                "acao": {"skill": "media", "metodo": "pausar", "args": {}},
                "instantaneo": True}
    if re.search(r"(continua|retoma|toca?)( a m[úu]sica)?$", t.strip()) or re.search(r"dar (o )?play", t):
        return {"resposta": "Tocando.",
                "acao": {"skill": "media", "metodo": "tocar", "args": {}},
                "instantaneo": True}
    if re.search(r"pr[óo]xima( m[úu]sica| faixa)?", t):
        return {"resposta": "Próxima.",
                "acao": {"skill": "media", "metodo": "proxima", "args": {}},
                "instantaneo": True}
    if re.search(r"(m[úu]sica |faixa )?anterior", t):
        return {"resposta": "Voltando.",
                "acao": {"skill": "media", "metodo": "anterior", "args": {}},
                "instantaneo": True}

    m = re.search(r"(lembrete|lembra-me|me lembra|timer|temporizador|despertador).*?(\d+)\s*(segundo|minuto|hora)", t)
    if m:
        qtd = int(m.group(2))
        un = m.group(3)
        mult = {"segundo": 1, "minuto": 60, "hora": 3600}[un[:7] if un.startswith("segundo") else un]
        seg = qtd * mult
        return {"resposta": f"Certo, aviso em {qtd} {un}s.",
                "acao": {"skill": "notes", "metodo": "timer", "args": {"segundos": seg}},
                "instantaneo": True}

    if re.search(r"bloquear( a tela)?|bloqueia", t):
        return {"resposta": "Bloqueando a tela.",
                "acao": {"skill": "system", "metodo": "bloquear", "args": {}},
                "instantaneo": True}

    if re.search(r"(uso de )?(cpu|processador|mem[óo]ria|ram|bateria)", t):
        return {"resposta": "Verificando o sistema.",
                "acao": {"skill": "system", "metodo": "status", "args": {}},
                "instantaneo": True}

    return None  # precisa do LLM
