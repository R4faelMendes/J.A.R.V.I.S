// J.A.R.V.I.S. Cognitive Core & Cloud AI Integration (Gemini, OpenAI & Local Brain)
import { memoryService } from './memoryService';

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

    // 1. Try Gemini API if key is provided
    if (prefs.aiProvider === 'gemini' && prefs.geminiApiKey) {
      try {
        const response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=${prefs.geminiApiKey}`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            contents: [
              {
                role: 'user',
                parts: [{ text: `${this.systemPrompt}\n\nPergunta do Senhor: ${prompt}` }]
              }
            ],
            generationConfig: {
              maxOutputTokens: 250,
              temperature: 0.7
            }
          })
        });

        if (response.ok) {
          const data = await response.json();
          const answer = data.candidates?.[0]?.content?.parts?.[0]?.text;
          if (answer) {
            memoryService.addShortTermLog('jarvis', answer);
            return answer.trim();
          }
        }
      } catch (err) {
        console.warn('Gemini API call failed, falling back to local neural brain:', err);
      }
    }

    // 2. Try OpenAI API if key is provided
    if (prefs.aiProvider === 'openai' && prefs.openAiApiKey) {
      try {
        const response = await fetch('https://api.openai.com/v1/chat/completions', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${prefs.openAiApiKey}`
          },
          body: JSON.stringify({
            model: 'gpt-4o-mini',
            messages: [
              { role: 'system', content: this.systemPrompt },
              { role: 'user', content: prompt }
            ],
            max_tokens: 250
          })
        });

        if (response.ok) {
          const data = await response.json();
          const answer = data.choices?.[0]?.message?.content;
          if (answer) {
            memoryService.addShortTermLog('jarvis', answer);
            return answer.trim();
          }
        }
      } catch (err) {
        console.warn('OpenAI API call failed, falling back to local neural brain:', err);
      }
    }

    // 3. Smart Local Neural Engine
    const localAnswer = this.generateLocalResponse(prompt);
    memoryService.addShortTermLog('jarvis', localAnswer);
    return localAnswer;
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
    } catch {}

    return `Entendido, Senhor. Processei a instrução "${prompt}". Se desejar conectar a chave de API do Google Gemini ou OpenAI para raciocínio ilimitado de IA, acesse o painel de configurações no canto superior.`;
  }
}

export const aiService = new AIService();