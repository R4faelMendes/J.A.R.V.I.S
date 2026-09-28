// J.A.R.V.I.S. Cognitive Core & Cloud AI Integration
// Provedores, nesta ordem de prioridade:
//   0. Cérebro n8n       — agente com ferramentas (e-mail, agenda, contatos,
//                          pesquisa) via webhook — quando configurado e ativo
//   1. Nuvem gratuita     — SEM chave de API (pollinations.ai) — padrão
//   2. Cérebro local      — Ollama (http://localhost:11434) — fallback automático
//   3. Cérebro local tático — offline, respostas programadas
import { memoryService } from './memoryService';

const FREE_CLOUD_TIMEOUT_MS = 15000;
const OLLAMA_URL = 'http://localhost:11434';
const OLLAMA_TIMEOUT_MS = 25000;
const N8N_TIMEOUT_MS = 90000; // agente com ferramentas pode pensar por um tempo
const N8N_MIN_INTERVAL_MS = 2000; // guarda de ritmo local (anti-loop de e-mails)

class AIService {
  constructor() {
    this.systemPrompt = `Você é o J.A.R.V.I.S. (Just A Rather Very Intelligent System), a inteligência artificial tática avançada de Rafael Mendes(o novo Tony Stark).
Suas respostas devem ser elegantes, altamente inteligentes, objetivas, em tom educado e prestativo com 'Senhor'.
Você tem acesso completo aos sistemas do computador, telemetria, satélites Verônica, reatores ARC e histórico de projetos.
Responda sempre em português do Brasil de forma concisa e direta.`;
    this.activeProvider = 'nuvem gratuita'; // último provedor que respondeu
    this._n8nLastCall = 0;
    this._n8nChain = Promise.resolve(); // 1 chamada ao cérebro por vez
  }

  getActiveProvider() { return this.activeProvider; }
  isLocalMode() { return this.activeProvider === 'ollama local' || this.activeProvider === 'tático local'; }
  isBrainMode() { return this.activeProvider === 'cérebro n8n'; }

  /** Cérebro n8n pronto para uso? (toggle ativo + URL configurada) */
  n8nConfigured() {
    const prefs = memoryService.getPreferences();
    return !!(prefs.n8nEnabled && String(prefs.n8nWebhookUrl || '').trim().startsWith('http'));
  }

  async query(prompt) {
    const prefs = memoryService.getPreferences();
    memoryService.addShortTermLog('user', prompt);

    const providers = [];

    // Cérebro n8n primeiro (quando ativo) — é o único que realmente AGE
    // (e-mail, agenda, contatos, pesquisa). Se falhar, a fila cai para os
    // cérebros menores sem o operador perceber a troca.
    if (this.n8nConfigured() && prefs.aiProvider !== 'local') {
      providers.push({ nome: 'cérebro n8n', run: () => this.queryN8n(prompt) });
    }

    // Nuvem gratuita sem chave (padrão) — opções com token foram removidas
    if (prefs.aiProvider !== 'local') {
      providers.push({ nome: 'nuvem gratuita', run: () => this.queryFreeCloud(prompt) });
    }

    // Fallback de IA local (Ollama) — automático se a nuvem falhar.
    // Fica depois da nuvem na fila, então a troca é invisível; o indicador
    // visual (glow violeta) reflete o provedor ativo via isLocalMode().
    providers.push({ nome: 'ollama local', run: () => this.queryOllama(prompt) });

    for (const provider of providers) {
      try {
        const answer = await provider.run();
        if (answer) {
          const clean = String(answer).trim();
          this.activeProvider = provider.nome;
          memoryService.addShortTermLog('jarvis', clean);
          return clean;
        }
      } catch (err) {
        console.warn(`[J.A.R.V.I.S.] ${provider.nome} falhou, tentando o próximo:`, err?.message || err);
      }
    }

    // Cérebro local tático (offline, programado)
    this.activeProvider = 'tático local';
    const localAnswer = this.generateLocalResponse(prompt);
    memoryService.addShortTermLog('jarvis', localAnswer);
    return localAnswer;
  }

  // -------------------------------------------------------------------------
  // Cérebro n8n — workflow "JARVIS Agent" (ver pasta n8n/ do projeto).
  // POST { userId, message, sessionId } com Header Auth X-Jarvis-Key.
  // Resposta: { reply }. Sessão = 1 UUID por execução do app (memória de
  // conversa contínua); fechar e reabrir começa sessão nova.
  // Guarda de ritmo: 1 chamada por vez + intervalo mínimo entre chamadas,
  // para que um bug em loop nunca dispare centenas de ações reais.
  // -------------------------------------------------------------------------
  async queryN8n(prompt) {
    const prefs = memoryService.getPreferences();
    const url = String(prefs.n8nWebhookUrl || '').trim().replace(/\/$/, '');
    const key = String(prefs.n8nKey || '');
    const userId = String(prefs.n8nUserId || 'tony-stark').trim() || 'tony-stark';
    if (!url) throw new Error('webhook n8n não configurado');

    // Serializa: uma chamada ao cérebro por vez
    const run = this._n8nChain.then(async () => {
      const wait = N8N_MIN_INTERVAL_MS - (Date.now() - this._n8nLastCall);
      if (wait > 0) await new Promise((r) => setTimeout(r, wait));
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), N8N_TIMEOUT_MS);
      try {
        const response = await fetch(url, {
          method: 'POST',
          signal: controller.signal,
          headers: {
            'Content-Type': 'application/json',
            ...(key ? { 'X-Jarvis-Key': key } : {}),
          },
          body: JSON.stringify({
            userId,
            message: prompt,
            sessionId: memoryService.getSessionId(),
          }),
        });
        if (response.status === 429) throw new Error('cérebro pediu calma (rate limit)');
        if (!response.ok) throw new Error(`n8n respondeu ${response.status}`);
        const data = await response.json();
        const text = String(data?.reply ?? data?.output ?? data?.text ?? '').trim();
        if (!text) throw new Error('cérebro respondeu vazio');
        return text;
      } finally {
        clearTimeout(timer);
        this._n8nLastCall = Date.now();
      }
    });
    // Mantém a cadeia viva mesmo se esta chamada falhar
    this._n8nChain = run.catch(() => {});
    return run;
  }

  /** Teste de conexão para o botão nos Ajustes. Não consome sessão real. */
  async testN8n() {
    const prefs = memoryService.getPreferences();
    const url = String(prefs.n8nWebhookUrl || '').trim().replace(/\/$/, '');
    const key = String(prefs.n8nKey || '');
    if (!url) return { ok: false, message: 'Informe a URL do webhook primeiro.' };
    try {
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), 15000);
      const response = await fetch(url, {
        method: 'POST',
        signal: controller.signal,
        headers: {
          'Content-Type': 'application/json',
          ...(key ? { 'X-Jarvis-Key': key } : {}),
        },
        body: JSON.stringify({
          userId: String(prefs.n8nUserId || 'tony-stark'),
          message: 'Teste de conexão. Responda apenas: "cérebro online, Senhor."',
          sessionId: `test_${Date.now()}`,
        }),
      });
      clearTimeout(timer);
      if (response.status === 401 || response.status === 403) {
        return { ok: false, message: 'Chave rejeitada (401/403). Confira o Header Auth no n8n.' };
      }
      if (response.status === 429) return { ok: false, message: 'Cérebro pediu calma (rate limit). Tente de novo em instantes.' };
      if (!response.ok) return { ok: false, message: `n8n respondeu HTTP ${response.status}. Workflow ativo?` };
      const data = await response.json();
      const reply = String(data?.reply ?? data?.output ?? '').trim();
      return { ok: true, message: reply ? `Online. Resposta: "${reply.slice(0, 120)}"` : 'Online (resposta vazia — confira o nó Respond).' };
    } catch (err) {
      return { ok: false, message: `Sem alcançar o n8n: ${err?.message || err}. URL correta? n8n rodando?` };
    }
  }

  // -------------------------------------------------------------------------
  // Cérebro local via Ollama — http://localhost:11434 (modelo configurável
  // em preferências: ollamaModel, padrão 'llama3.1'). Requer `ollama serve`
  // com o modelo baixado (`ollama pull llama3.1`).
  // -------------------------------------------------------------------------
  async queryOllama(prompt) {
    const prefs = memoryService.getPreferences();
    const model = prefs.ollamaModel || 'llama3.1';
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), OLLAMA_TIMEOUT_MS);
    try {
      const response = await fetch(`${OLLAMA_URL}/api/generate`, {
        method: 'POST',
        signal: controller.signal,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          model,
          prompt: `${this.systemPrompt}\n\nPergunta do Senhor: ${prompt}\nResposta:`,
          stream: false,
          options: { temperature: 0.7 },
        }),
      });
      if (!response.ok) throw new Error(`Ollama HTTP ${response.status}`);
      const data = await response.json();
      const text = String(data?.response || '').trim();
      if (!text) throw new Error('resposta vazia do Ollama');
      return text;
    } finally {
      clearTimeout(timer);
    }
  }

  async ollamaAvailable() {
    try {
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), 4000);
      const res = await fetch(`${OLLAMA_URL}/api/tags`, { signal: controller.signal });
      clearTimeout(timer);
      return res.ok;
    } catch {
      return false;
    }
  }
  // -------------------------------------------------------------------------
  // Nuvem gratuita — não exige chave nem cadastro
  // -------------------------------------------------------------------------
  async queryFreeCloud(prompt) {
    const input = `${this.systemPrompt}\n\nPergunta do Senhor: ${prompt}\nResposta:`;
    const url = `https://text.pollinations.ai/${encodeURIComponent(input)}`;
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), FREE_CLOUD_TIMEOUT_MS);
    try {
      const response = await fetch(url, {
        signal: controller.signal,
        headers: { Accept: 'text/plain' }
      });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const text = (await response.text()).trim();
      if (!text || text.replace(/\s/g, '').length < 2) throw new Error('resposta vazia');
      return text;
    } finally {
      clearTimeout(timer);
    }
  }

  generateLocalResponse(prompt) {
    const raw = prompt.toLowerCase();

    if (raw.includes('quem é você') || raw.includes('quem e voce') || raw.includes('o que você faz')) {
      return 'Sou o J.A.R.V.I.S., seu assistente de inteligência tática e automação de sistemas. Administro telemetria, agenda, projetos de desenvolvimento e controle completo da sua estação de trabalho.';
    }

    if (raw.includes('horas') || raw.includes('que horas são') || raw.includes('horario')) {
      const now = new Date();
      return `Agora são exatamente ${now.toLocaleTimeString('pt-BR')} no horário local, Senhor.`;
    }

    if (raw.includes('data') || raw.includes('que dia é hoje') || raw.includes('qual é a data')) {
      const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
      const now = new Date().toLocaleDateString('pt-BR', options);
      return `Hoje é ${now}, Senhor. Todos os cronogramas estão sincronizados.`;
    }

    if (raw.includes('como você está') || raw.includes('como esta') || raw.includes('tudo bem')) {
      return 'Operando com 100% de capacidade nos núcleos quânticos, Senhor. Às suas ordens para qualquer diretiva.';
    }

    if (raw.includes('temperatura') || raw.includes('clima') || raw.includes('tempo')) {
      return 'Senhor, a telemetria climática local indica temperatura ambiente de 24.5°C com umidade relativa de 58%. Céu com pouca nebulosidade.';
    }

    if (raw.includes('piada') || raw.includes('conte uma piada')) {
      return 'Senhor, por que o programador não gosta da praia? Porque há muitos bugs na areia e o Wi-Fi tem perda de pacotes.';
    }

    if (raw.includes('obrigado') || raw.includes('valeu') || raw.includes('agradeço')) {
      return 'Sempre ao seu dispor, Senhor. É um prazer auxiliá-lo.';
    }

    // Math calculation handling
    try {
      const mathMatch = prompt.match(/(\d+[\s\+\-\*\/\^]+\d+)/);
      if (mathMatch) {
        // Safe evaluation
        const sanitized = mathMatch[0].replace(/[^0-9\+\-\*\/\.]/g, '');
        // eslint-disable-next-line no-eval
        const result = Function(`'use strict'; return (${sanitized})`)();
        return `O cálculo de ${sanitized} resulta em ${result}, Senhor.`;
      }
    } catch { /* ignora */ }

    return `Entendido, Senhor. Processei a instrução "${prompt}" nos módulos locais. A nuvem gratuita de IA está indisponível no momento — posso tentar novamente quando a conexão retornar.`;
  }
}

export const aiService = new AIService();
