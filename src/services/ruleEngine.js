// J.A.R.V.I.S. SYNAPSE — Motor de reflexos (regras/gatilhos proativos).
// Observa eventos do sistema (hora do dia, processos em execução) e dispara
// ações via commandExecutor automaticamente. Regras persistem em localStorage.

const STORAGE_KEY = 'jarvis_synapse_rules';

const DEFAULT_RULES = [
  {
    id: 'rule_morning',
    name: 'Bom dia operacional',
    enabled: true,
    trigger: { kind: 'time', at: '08:00' },
    action: { kind: 'speak', text: 'Bom dia, Senhor. Organismo sincronizado e pronto para as diretivas do dia.' },
  },
  {
    id: 'rule_focus',
    name: 'Vigília de foco',
    enabled: false,
    trigger: { kind: 'process', match: 'Code.exe', mode: 'running' },
    action: { kind: 'speak', text: 'Detectei o ambiente de desenvolvimento ativo, Senhor. Modo de foco sugerido.' },
  },
];

function load() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) return JSON.parse(raw);
  } catch { /* ignora */ }
  return DEFAULT_RULES;
}

function save(rules) {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(rules)); } catch { /* ignora */ }
}

function todayAt(hhmm) {
  const [h, m] = String(hhmm || '08:00').split(':').map(Number);
  const d = new Date();
  d.setHours(h || 0, m || 0, 0, 0);
  return d;
}

class RuleEngine {
  constructor() {
    this.rules = load();
    this.timers = [];
    this.firedToday = new Set();
    this.onFire = null; // (rule, result) => void — conectado pelo App
    this.pollTimer = null;
    this.lastProcessSnapshot = [];
  }

  getRules() { return this.rules; }

  setRules(rules) {
    this.rules = Array.isArray(rules) ? rules : [];
    save(this.rules);
    this.restart();
  }

  addRule(rule) {
    const full = { id: `rule_${Date.now()}`, enabled: true, ...rule };
    this.setRules([full, ...this.rules]);
    return full;
  }

  removeRule(id) { this.setRules(this.rules.filter((r) => r.id !== id)); }

  toggleRule(id) {
    this.setRules(this.rules.map((r) => (r.id === id ? { ...r, enabled: !r.enabled } : r)));
  }

  start(onFire) {
    this.onFire = onFire;
    this.restart();
  }

  stop() {
    this.timers.forEach(clearTimeout);
    this.timers = [];
    if (this.pollTimer) clearInterval(this.pollTimer);
    this.pollTimer = null;
  }

  restart() {
    this.stop();
    // Gatilhos de horário: agenda o próximo disparo de hoje
    for (const rule of this.rules) {
      if (!rule.enabled || rule.trigger?.kind !== 'time') continue;
      const at = todayAt(rule.trigger.at);
      const ms = at.getTime() - Date.now();
      if (ms <= 0) continue; // horário já passou hoje
      const t = setTimeout(() => this.fire(rule, { type: 'time', at: rule.trigger.at }), ms);
      this.timers.push(t);
    }
    // Gatilhos de processo: polling a cada 20s
    if (this.rules.some((r) => r.enabled && r.trigger?.kind === 'process')) {
      this.pollTimer = setInterval(() => this.checkProcesses(), 20000);
      this.checkProcesses();
    }
  }

  async checkProcesses() {
    let list = [];
    try {
      if (window.electronAPI?.getRunningProcesses) list = (await window.electronAPI.getRunningProcesses()) || [];
    } catch { /* ignora */ }
    const names = list.map((p) => String(p.process || p.name || '').toLowerCase());
    for (const rule of this.rules) {
      if (!rule.enabled || rule.trigger?.kind !== 'process') continue;
      const match = String(rule.trigger.match || '').toLowerCase();
      if (!match) continue;
      const running = names.some((n) => n.includes(match));
      const key = `${rule.id}:${running ? 'on' : 'off'}`;
      if (rule.trigger.mode === 'running' && running && !this.firedToday.has(rule.id)) {
        this.firedToday.add(rule.id);
        this.fire(rule, { type: 'process', running: true, match });
      }
      if (rule.trigger.mode === 'closed' && !running && this.lastProcessSnapshot.some((n) => n.includes(match))) {
        this.fire(rule, { type: 'process', running: false, match });
      }
      void key;
    }
    this.lastProcessSnapshot = names;
  }

  fire(rule, context) {
    try { this.onFire?.(rule, context); } catch (e) { console.warn('[reflexos] falha ao disparar regra:', e); }
  }
}

export const ruleEngine = new RuleEngine();
export const RULE_ACTION_KINDS = ['speak', 'command'];
export const RULE_TRIGGER_KINDS = ['time', 'process'];
