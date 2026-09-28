# J.A.R.V.I.S. — Cérebro n8n ("JARVIS Agent")

O n8n é o backend que **age de verdade**: e-mail, agenda, contatos e pesquisa
web. O Electron continua sendo o corpo (voz + organismo SYNAPSE) e fala com
ele por HTTP. Sem n8n configurado, nada muda — a fila cai para nuvem gratuita
→ Ollama → tático local.

> **Passo zero obrigatório:** assista ao vídeo *"Como construi o JARVIS do
> Homem de Ferro no N8N (TEMPLATE GRATUITO)"*, canal Julio | Lucrar com IA
> (https://www.youtube.com/watch?v=vTIq4pUR7o0), e baixe o template gratuito
> da descrição, se houver. Se o template dele importar pronto, **use o dele**
> e pule para a seção "Ligando o Electron" — este workflow aqui
> (`jarvis-agent.workflow.json`) é o rascunho de estrutura + contrato HTTP.

## 1. Subindo o n8n

- Caminho rápido: n8n Cloud (comece por aqui).
- Caminho coerente com o projeto (tudo no seu PC): self-host via Docker:
  `docker run -d --name n8n -p 5678:5678 -v n8n_data:/home/node/.n8n n8nio/n8n`
- Crie o workflow **"JARVIS Agent"**: importe `jarvis-agent.workflow.json`
  (⋯ → Import from File) **ou** monte seguindo o vídeo — o que o vídeo mostrar
  vence este arquivo (nomes de nós, ordem, modelo, system prompt).

## 2. Contrato HTTP (o que o Electron espera)

- Webhook `POST /webhook/jarvis-agent`, Header Auth `X-Jarvis-Key`.
- Entrada:
  ```json
  { "userId": "tony-stark", "message": "texto da fala", "sessionId": "uuid-da-sessao" }
  ```
- Saída (nó Respond to Webhook, sempre):
  ```json
  { "reply": "texto que o JARVIS vai falar" }
  ```
- `sessionId`: 1 UUID por execução do app (gerado em `memoryService.getSessionId()`).
  Fechar e reabrir o app = sessão nova. É ele que alimenta a memória do agente.
- Timeout do lado Electron: 90s (agente com ferramentas pensa um pouco).

## 3. Credenciais (escopo mínimo)

| Onde | O quê |
|---|---|
| Webhook | Header Auth `X-Jarvis-Key` = mesmo token colado nos Ajustes do JARVIS |
| Agente | OpenAI (GPT-4o) ou Anthropic (Claude) — o que o vídeo usar |
| Agenda | OAuth2 Google, escopo Calendar |
| Gmail | OAuth2 Google, escopos `gmail.send` + `gmail.readonly` (nada de admin) |
| Airtable | API key + base com tabela **Contatos** |
| Tavily | API key (ou nó nativo, se sua versão do n8n tiver) |

## 4. Ligando o Electron

Nos **Ajustes → Cérebro n8n** do JARVIS:
1. Cole a URL do webhook (modo produção: `.../webhook/jarvis-agent`).
2. Cole a chave (mesmo token do Header Auth) e o `userId`.
3. Ative o toggle e use **Testar conexão**.

Ordem de provedores com o toggle ativo: **n8n → nuvem gratuita → Ollama →
tático local**. Com o toggle desligado (ou `aiProvider = local`), o n8n é
ignorado. Falha em qualquer etapa cai para a próxima sem você perceber —
só o indicador da sidebar muda.

## 5. Ordem de montagem (não plugue as 5 ferramentas de uma vez)

1. Webhook + Agente respondendo só texto → confirme que o Electron fala.
2. Pesquisa web (Tavily) → valor imediato, a mais simples.
3. Google Calendar → a mais usada no dia a dia.
4. Gmail → teste leitura antes de envio.
5. Airtable (contatos) por último, com a base pronta.

Teste cada ferramenta isolada no n8n (**Execute Node**) antes de plugar no
agente — debugar ferramenta por ferramenta é muito mais fácil.

## 6. Segurança (não pule)

- **Nunca** deixe o webhook público sem o Header Auth.
- Guarda de ritmo em **duas camadas**: nó `Guarda de ritmo` no workflow
  (bloqueia intervalos < 2s, responde 429) + serialização no `aiService.js`
  (1 chamada por vez, intervalo mínimo de 2s).
- Ações destrutivas continuam protegidas no Electron pela trava biométrica
  (`biometricService.js`) e, no agente, pela regra 1 do system prompt
  (confirmar antes de modificar algo real).

## 7. Problemas comuns

| Sintoma no teste | Causa provável |
|---|---|
| `401/403` | Token diferente entre n8n e Ajustes |
| `HTTP 404` | Workflow inativo ou URL de teste vs. produção trocada (use `/webhook/`, não `/webhook-test/` para produção) |
| `429` | Guarda de ritmo atuando — aguarde e repita |
| Resposta vazia | Nó Respond não devolve `{ reply }` — confira o `responseBody` |
| Timeout | Modelo lento ou ferramenta travada — 90s é o limite do Electron |

## Pendente (configuração externa, sua parte)

- [ ] Assistir ao vídeo e ajustar nós/modelo/prompt ao template original.
- [ ] Credenciais OAuth2 Google, Tavily e Airtable criadas por você.
- [ ] Webhook em produção ativo (workflow com toggle **Active** ligado).
