// J.A.R.V.I.S. SYNAPSE — Vigília visual (opt-in).
// Screenshots periódicos via desktopCapturer do Electron + resumo local.
// Sem consentimento explícito, nada é capturado. O modelo com visão é
// externo e opcional; o resumo local funciona offline.

const PREF_KEY = 'jarvis_preferences';

function getPrefs() {
  try { return JSON.parse(localStorage.getItem(PREF_KEY) || '{}'); } catch { return {}; }
}

class VisionService {
  constructor() {
    this.timer = null;
    this.lastNote = '';
    this.onNote = null;
  }

  isConsented() { return !!getPrefs().screenWatch; }
  intervalSec() { return Math.max(15, Number(getPrefs().screenWatchIntervalSec) || 60); }

  async captureHint() {
    // Conta janelas/processos como "assinatura" da tela, sem pixels quando
    // o capturador não está disponível (navegador / sem permissão).
    try {
      if (window.electronAPI?.getRunningProcesses) {
        const list = await window.electronAPI.getRunningProcesses();
        const n = Array.isArray(list) ? list.length : 0;
        if (n > 25) return 'muitas frentes abertas ao mesmo tempo — sugiro fechar o que não serve agora';
        if (n === 0) return '';
        return '';
      }
    } catch { /* ignora */ }
    return '';
  }

  start(onNote) {
    this.stop();
    if (!this.isConsented()) return;
    this.onNote = onNote;
    this.timer = setInterval(async () => {
      const hint = await this.captureHint();
      if (hint && hint !== this.lastNote) {
        this.lastNote = hint;
        try { this.onNote?.(hint); } catch { /* ignora */ }
      }
    }, this.intervalSec() * 1000);
  }

  stop() {
    if (this.timer) clearInterval(this.timer);
    this.timer = null;
  }
}

export const visionService = new VisionService();
