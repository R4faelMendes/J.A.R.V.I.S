"""Script de reprodução do bug do painel de resposta (Tarefa 3 - Item 1).

Demonstra a causa do bug:
1. Quando o servidor envia periodicamente o estado via WebSocket (a cada 250ms),
   o front-end recebia o JSON completo com `d.resposta`.
2. O front-end comparava `elResposta.textContent !== d.resposta`.
3. Como a animação de digitação (digitarTexto) ainda estava em andamento (ex: texto parcial de 20 chars),
   a condição `elResposta.textContent !== d.resposta` era SEMPRE VERDADEIRA a cada tick do WebSocket.
4. Isso chamava `digitarTexto` novamente, que executava `elemento.innerHTML = ""` e reiniciava do zero,
   apagando e repetindo a digitação infinitamente em loop.
"""
import time

def simular_comportamento_antigo():
    resposta_completa = "Olá! Eu sou o Jarvis, seu assistente pessoal de inteligência artificial 100% local."
    tam_total = len(resposta_completa)
    
    # Simulação do front-end com intervalo de digitação (12ms por caractere)
    # e mensagens de WebSocket chegando a cada 250ms
    velocidade_ms = 12
    intervalo_ws_ms = 250
    
    chars_por_tick_ws = intervalo_ws_ms // velocidade_ms  # ~20 caracteres
    
    evidencias = []
    tempo_simulado = 0
    posicao_digitada = 0
    reinicios = 0
    
    for tick in range(1, 6):
        tempo_simulado += intervalo_ws_ms
        posicao_digitada = min(tam_total, posicao_digitada + chars_por_tick_ws)
        texto_no_dom = resposta_completa[:posicao_digitada]
        
        # Condição do app.js antigo: if (d.resposta && elResposta.textContent !== d.resposta)
        dispara_bug = (texto_no_dom != resposta_completa)
        
        evidencias.append({
            "tick_ws": tick,
            "tempo_ms": tempo_simulado,
            "texto_parcial_no_dom": texto_no_dom,
            "tamanho_no_dom": len(texto_no_dom),
            "tamanho_esperado": tam_total,
            "disparou_reinicio_e_apagou": dispara_bug
        })
        
        if dispara_bug:
            reinicios += 1
            # O código antigo limpava: elemento.innerHTML = "" e começava do 0
            posicao_digitada = 0
            
    return evidencias, reinicios

if __name__ == "__main__":
    evidencias, reinicios = simular_comportamento_antigo()
    print("=== EVIDÊNCIA DE REPRODUÇÃO DO BUG DO PAINEL DE RESPOSTA ===")
    for ev in evidencias:
        print(f"[Tick WS {ev['tick_ws']} @ {ev['tempo_ms']}ms]")
        print(f"  Texto no DOM: '{ev['texto_parcial_no_dom'][:30]}...' ({ev['tamanho_no_dom']}/{ev['tamanho_esperado']} chars)")
        print(f"  Disparou reinício (apagou painel): {ev['disparou_reinicio_e_apagou']}")
    print(f"\nTotal de reinícios forçados em 5 ticks de WS: {reinicios} vezes.")
    print("Conclusão: O painel nunca conseguia terminar a digitação de respostas com mais de 20 caracteres.")
