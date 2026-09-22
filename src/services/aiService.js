// J.A.R.V.I.S. Cognitive Core & Cloud AI Integration
// Provedores, nesta ordem de prioridade:
//   1. Nuvem gratuita     — SEM chave de API (pollinations.ai) — padrão
//   2. Cérebro local      — offline, respostas programadas
import { memoryService } from './memoryService';

const FREE_CLOUD_TIMEOUT_MS = 15000;

class AIService {
  constructor() {
    this.systemPrompt = `Você é o J.A.R.V.I.S. (Just A Rather Very Intelligent System), a inteligência artificial tática avançada de Rafael Mendes(o novo Tony Stark).
Suas respostas devem ser elegantes, altamente inteligentes, objetivas, em tom educado e prestativo com 'Senhor'.
Você tem acesso completo aos sistemas do computador, telemetria, satélites Verônica, reatores ARC e histórico de projetos.
Responda sempre em português do Brasil de forma concisa e direta.`;
  }

  async query(prompt) {
    const prefs = memoryService.getPreferences();
    memoryService.addShortTermLog('user', prompt);

    const providers = [];

    // Nuvem gratuita sem chave (padrão) — opções com token foram removidas
    if (prefs.aiProvider !== 'local') {
      providers.push({ nome: 'nuvem gratuita', run: () => this.queryFreeCloud(prompt) });
    }

    for (const provider of providers) {
      try {
        const answer = await provider.run();
        if (answer) {
          const clean = String(answer).trim();
          memoryService.addShortTermLog('jarvis', clean);
          return clean;
        }
      } catch (err) {
        console.warn(`[J.A.R.V.I.S.] ${provider.nome} falhou, tentando o próximo:`, err?.message || err);
      }
    }

    // Cérebro local (offline)
    const localAnswer = this.generateLocalResponse(prompt);
    memoryService.addShortTermLog('jarvis', localAnswer);
    return localAnswer;
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
