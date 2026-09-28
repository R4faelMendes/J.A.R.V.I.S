// J.A.R.V.I.S. Core Neural Memory Matrix (Short-Term & Long-Term Persistence)

const STORAGE_KEYS = {
  LONG_TERM_PROJECTS: 'jarvis_longterm_projects',
  SHORT_TERM_CONTEXT: 'jarvis_shortterm_context',
  COMMAND_HISTORY: 'jarvis_command_history',
  PREFERENCES: 'jarvis_preferences',
  AGENDA_TASKS: 'jarvis_agenda_tasks',
  AGENDA_ALARMS: 'jarvis_agenda_alarms',
  BOX_SIZES: 'jarvis_box_sizes',
  GRAPH_ENTITIES: 'jarvis_graph_entities'
};

// Chaves gravadas também em arquivo no disco (memória permanente do J.A.R.V.I.S)
const PERSISTENT_KEYS = [
  STORAGE_KEYS.LONG_TERM_PROJECTS,
  STORAGE_KEYS.COMMAND_HISTORY,
  STORAGE_KEYS.PREFERENCES,
  STORAGE_KEYS.AGENDA_TASKS,
  STORAGE_KEYS.BOX_SIZES,
  STORAGE_KEYS.GRAPH_ENTITIES
];

const fileAPI = typeof window !== 'undefined' ? window.electronAPI : null;

function buildDiskBundle() {
  const bundle = {};
  for (const key of PERSISTENT_KEYS) {
    try {
      const value = localStorage.getItem(key);
      if (value != null) bundle[key] = value;
    } catch { /* ignora */ }
  }
  return bundle;
}

let persistTimer = null;
function persistToDisk(immediate = false) {
  if (!fileAPI || (!fileAPI.memorySave && !fileAPI.memorySaveSync)) return;
  if (persistTimer) clearTimeout(persistTimer);
  const write = () => {
    persistTimer = null;
    try {
      const bundle = buildDiskBundle();
      if (immediate && fileAPI.memorySaveSync) fileAPI.memorySaveSync(bundle);
      else if (fileAPI.memorySave) fileAPI.memorySave(bundle);
    } catch { /* ignora */ }
  };
  if (immediate) write();
  else persistTimer = setTimeout(write, 300);
}

const DEFAULT_PROJECTS = [
  {
    id: 'proj_temp_01',
    name: 'temperatura-mobile',
    description: 'Sistema IoT mobile para monitoramento e telemetria térmica em tempo real.',
    techStack: ['React Native', 'Expo', 'Node.js', 'MQTT', 'Tailwind CSS'],
    importantFiles: ['App.js', 'src/screens/Dashboard.js', 'src/services/sensorApi.js', 'server/mqttBroker.js'],
    lastErrors: [
      'MQTT Connection Timeout no broker local (Port 1883)',
      'Sensor calibration offset de +1.4°C em leituras analógicas'
    ],
    lastModified: '2026-09-15 17:40',
    commandsUsed: ['npm start', 'npx expo start --android', 'node server/mqttBroker.js'],
    gitBranch: 'feature/thermal-sensors-v2',
    status: 'Em Desenvolvimento'
  },
  {
    id: 'proj_jarvis_02',
    name: 'jarvis-tactical-os',
    description: 'Painel holográfico tático com controle por voz, IA integrada e automação Windows.',
    techStack: ['React', 'JSX', 'Tailwind CSS', 'Web Speech API', 'Web Audio API', 'IA Nuvem Gratuita'],
    importantFiles: ['src/App.jsx', 'src/services/speechService.js', 'src/services/aiService.js'],
    lastErrors: [],
    lastModified: '2026-09-17 14:00',
    commandsUsed: ['npm run dev', 'npm run build'],
    gitBranch: 'main',
    status: 'Ativo'
  }
];

class MemoryMatrix {
  constructor() {
    this.shortTermSession = [];
    this.initStorage();
  }

  initStorage() {
    if (typeof window === 'undefined') return;

    // 1) Memória permanente: arquivo no disco (perfil do usuário) vence o localStorage
    try {
      const disk = fileAPI && fileAPI.memoryLoadSync ? fileAPI.memoryLoadSync() : null;
      if (disk && typeof disk === 'object') {
        for (const key of PERSISTENT_KEYS) {
          if (typeof disk[key] === 'string' && disk[key]) localStorage.setItem(key, disk[key]);
        }
      }
    } catch { /* ignora */ }

    // Grava imediatamente ao fechar/recarregar/minimizar para não perder nada
    try {
      window.addEventListener('pagehide', () => persistToDisk(true));
      window.addEventListener('beforeunload', () => persistToDisk(true));
      window.addEventListener('unload', () => persistToDisk(true));
      document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') persistToDisk(true);
      });
    } catch { /* ignora */ }

    if (!localStorage.getItem(STORAGE_KEYS.LONG_TERM_PROJECTS)) {
      localStorage.setItem(STORAGE_KEYS.LONG_TERM_PROJECTS, JSON.stringify(DEFAULT_PROJECTS));
    }

    if (!localStorage.getItem(STORAGE_KEYS.PREFERENCES)) {
      const defaultPrefs = {
        userName: 'Tony Stark',
        accessLevel: 'NÍVEL ALPHA',
        voiceSpeed: 1.05,          // tom J.A.R.V.I.S. (leitura um pouco mais ágil)
        voicePitch: 0.85,          // voz mais profunda, estilo Jarvis do filme
        voiceEngine: 'neural', // neural (Microsoft Edge, fluída) | local (vozes do Windows)
        voiceId: 'pt-BR-AntonioNeural',
        jarvisVoicePreset: 'classico',
        localVoiceName: '',
        sttModel: 'base', // tiny | base | small (Whisper local offline)
        micDeviceId: '', // '' = microfone padrão do Windows
        theme: 'cyan', // cyan, amber, crimson, emerald, purple
        soundEnabled: true,
        aiProvider: 'free', // free (nuvem gratuita, sem chave) | local (offline)
        autoListen: true,
        gameModeActive: false,
        standbyMode: false,
        musicVolume: 75,
        musicDock: false,
        musicMode: 'ytmusic',
        lastMusicQuery: 'jarvis cinematic soundtrack',
        musicQueue: [
          { id: 'jarvis-suite', title: 'Stark Suite', query: 'jarvis cinematic soundtrack' },
          { id: 'focus', title: 'Focus Mode', query: 'ambient electronic focus music' },
          { id: 'travel', title: 'Launch Sequence', query: 'epic sci fi instrumental' }
        ]
      };
      localStorage.setItem(STORAGE_KEYS.PREFERENCES, JSON.stringify(defaultPrefs));
    } else {
      // Migração: preferências gravadas antes da reformulação do motor de voz
      try {
        const parsed = JSON.parse(localStorage.getItem(STORAGE_KEYS.PREFERENCES) || '{}');
        let changed = false;
        if (parsed && typeof parsed === 'object') {
          if (!('voiceEngine' in parsed)) {
            parsed.voiceEngine = 'neural';
            parsed.voiceId = 'pt-BR-AntonioNeural';
            parsed.localVoiceName = '';
            parsed.sttModel = 'base';
            parsed.autoListen = true; // nunca havia opção de ativar antes
            changed = true;
          }

          // IA: opções com chave (token) foram removidas — tudo migra para a nuvem gratuita
          if (!parsed.aiProvider || parsed.aiProvider === 'gemini' || parsed.aiProvider === 'openai') {
            parsed.aiProvider = 'free';
            changed = true;
          }
          if ('geminiApiKey' in parsed || 'openAiApiKey' in parsed) {
            delete parsed.geminiApiKey;
            delete parsed.openAiApiKey;
            changed = true;
          }

          // Voz: aplica o preset J.A.R.V.I.S. (voz mais profunda) se ainda estiver no padrão antigo
          if (!('jarvisVoicePreset' in parsed)) {
            parsed.jarvisVoicePreset = 'classico';
            if (parsed.voicePitch == null || parsed.voicePitch === 1.0) parsed.voicePitch = 0.85;
            if (parsed.voiceSpeed == null) parsed.voiceSpeed = 1.05;
            changed = true;
          }

          // Player do YouTube Music: o padrão antigo era a janela externa do Brave
          // ('brave'); a partir desta versão o padrão é o Brave EMULADO dentro do
          // box. Limpa a escolha legada uma única vez — trocas feitas depois
          // (inclusive voltar para BRAVE) continuam persistindo normalmente.
          if (parsed.ytPlayerModeMigrated == null) {
            if (parsed.ytPlayerMode === 'brave') delete parsed.ytPlayerMode;
            parsed.ytPlayerModeMigrated = true;
            changed = true;
          }

          if (changed) localStorage.setItem(STORAGE_KEYS.PREFERENCES, JSON.stringify(parsed));
        }
      } catch { /* ignora */ }
    }

    if (!localStorage.getItem(STORAGE_KEYS.COMMAND_HISTORY)) {
      localStorage.setItem(STORAGE_KEYS.COMMAND_HISTORY, JSON.stringify([]));
    }

    // 2) Sincroniza a memória em disco com o estado atual (primeira execução / migrações)
    persistToDisk();
  }

  // ---- Long Term Projects Memory ----
  getProjects() {
    try {
      const raw = localStorage.getItem(STORAGE_KEYS.LONG_TERM_PROJECTS);
      return raw ? JSON.parse(raw) : DEFAULT_PROJECTS;
    } catch {
      return DEFAULT_PROJECTS;
    }
  }

  saveProject(project) {
    const projects = this.getProjects();
    const index = projects.findIndex(p => p.name.toLowerCase() === project.name.toLowerCase() || p.id === project.id);
    if (index >= 0) {
      projects[index] = { ...projects[index], ...project, lastModified: new Date().toLocaleString('pt-BR') };
    } else {
      projects.unshift({
        id: `proj_${Date.now()}`,
        name: project.name,
        description: project.description || 'Novo projeto registrado pelo J.A.R.V.I.S.',
        techStack: project.techStack || ['JavaScript'],
        importantFiles: project.importantFiles || ['index.js'],
        lastErrors: project.lastErrors || [],
        lastModified: new Date().toLocaleString('pt-BR'),
        commandsUsed: project.commandsUsed || [],
        gitBranch: project.gitBranch || 'main',
        status: project.status || 'Ativo'
      });
    }
    localStorage.setItem(STORAGE_KEYS.LONG_TERM_PROJECTS, JSON.stringify(projects));
    persistToDisk();
  }

  findProjectByName(query) {
    const projects = this.getProjects();
    const clean = query.toLowerCase();
    return projects.find(p => p.name.toLowerCase().includes(clean) || (p.description && p.description.toLowerCase().includes(clean)));
  }

  // ---- Short-Term Context ----
  addShortTermLog(role, message) {
    const entry = { role, message, timestamp: new Date().toISOString() };
    this.shortTermSession.push(entry);
    if (this.shortTermSession.length > 50) {
      this.shortTermSession.shift();
    }
    try {
      sessionStorage.setItem(STORAGE_KEYS.SHORT_TERM_CONTEXT, JSON.stringify(this.shortTermSession));
    } catch {}
  }

  getShortTermContext() {
    return this.shortTermSession;
  }

  // ---- Command History ----
  logCommand(command) {
    try {
      const history = this.getCommandHistory();
      history.unshift({ command, timestamp: new Date().toLocaleString('pt-BR') });
      if (history.length > 100) history.pop();
      localStorage.setItem(STORAGE_KEYS.COMMAND_HISTORY, JSON.stringify(history));
      persistToDisk();
    } catch {}
  }

  getCommandHistory() {
    try {
      const raw = localStorage.getItem(STORAGE_KEYS.COMMAND_HISTORY);
      return raw ? JSON.parse(raw) : [];
    } catch {
      return [];
    }
  }

  // ---- Preferences & System State ----
  getPreferences() {
    try {
      const raw = localStorage.getItem(STORAGE_KEYS.PREFERENCES);
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  }

  updatePreferences(patch) {
    const current = this.getPreferences();
    const updated = { ...current, ...patch };
    localStorage.setItem(STORAGE_KEYS.PREFERENCES, JSON.stringify(updated));
    // Gravação SÍNRONA no disco: a configuração vale imediatamente e não se
    // perde nem se o app for fechado no mesmo instante
    persistToDisk(true);
    return updated;
  }

  // ---- Sessão do cérebro (n8n) — um UUID por execução do app ----
  // Vive em sessionStorage: mesma aba/janela compartilha a sessão (memória
  // de conversa contínua); fechar e reabrir começa uma sessão nova.
  getSessionId() {
    try {
      const KEY = 'jarvis_brain_session';
      let id = sessionStorage.getItem(KEY);
      if (!id) {
        id = (crypto?.randomUUID && crypto.randomUUID()) || `sess_${Date.now()}_${Math.floor(Math.random() * 1e6)}`;
        sessionStorage.setItem(KEY, id);
      }
      return id;
    } catch {
      if (!this._memSession) this._memSession = `sess_${Date.now()}`;
      return this._memSession;
    }
  }

  // ---- Tamanho dos boxes (arraste com o mouse, estilo janelas do Windows) ----
  getBoxSizes() {
    try {
      const raw = localStorage.getItem(STORAGE_KEYS.BOX_SIZES);
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  }

  setBoxSize(id, size) {
    try {
      const sizes = this.getBoxSizes();
      if (size && size.width > 0 && size.height > 0) sizes[id] = size;
      else delete sizes[id];
      localStorage.setItem(STORAGE_KEYS.BOX_SIZES, JSON.stringify(sizes));
      persistToDisk();
    } catch { /* ignora */ }
  }

  // ---- Agenda Tasks & Alarms ----
  getTasks() {
    try {
      const raw = localStorage.getItem(STORAGE_KEYS.AGENDA_TASKS);
      if (raw) return JSON.parse(raw);
      // Default Stark tactical tasks
      const defaultTasks = [
        {
          id: 'task_1',
          title: 'Sincronizar Telemetria do Reator ARC',
          description: 'Ajustar fluxo magnético das bobinas de confinamento.',
          date: new Date().toISOString().split('T')[0],
          time: '14:30',
          priority: 'ALTA', // CRITICA, ALTA, MEDIA, BAIXA
          completed: false,
          category: 'SISTEMA'
        },
        {
          id: 'task_2',
          title: 'Compilar Módulo Temperatura Mobile',
          description: 'Testar conexão MQTT no broker de testes.',
          date: new Date().toISOString().split('T')[0],
          time: '16:00',
          priority: 'MEDIA',
          completed: true,
          category: 'PROJETO'
        },
        {
          id: 'task_3',
          title: 'Verificação dos Satélites Verônica',
          description: 'Diagnóstico de transponders em órbita geoestacionária.',
          date: new Date(Date.now() + 86400000).toISOString().split('T')[0],
          time: '09:00',
          priority: 'CRITICA',
          completed: false,
          category: 'DEFESA'
        }
      ];
      localStorage.setItem(STORAGE_KEYS.AGENDA_TASKS, JSON.stringify(defaultTasks));
      return defaultTasks;
    } catch {
      return [];
    }
  }

  saveTasks(tasks) {
    localStorage.setItem(STORAGE_KEYS.AGENDA_TASKS, JSON.stringify(tasks));
    persistToDisk();
  }

  // ---- Grafo de memória de longo prazo (entidades + relações) ----
  // Tipos: project | person | decision | note. Relações: { from, to, kind }
  getEntities() {
    try {
      const raw = localStorage.getItem(STORAGE_KEYS.GRAPH_ENTITIES);
      if (raw) return JSON.parse(raw);
    } catch { /* ignora */ }
    const seed = [
      { id: 'ent_stark', type: 'person', label: 'Rafael Mendes', detail: 'Operador principal' },
      { id: 'ent_jarvis', type: 'note', label: 'J.A.R.V.I.S.', detail: 'Organismo synapse v5.0' },
    ];
    try { localStorage.setItem(STORAGE_KEYS.GRAPH_ENTITIES, JSON.stringify(seed)); } catch { /* ignora */ }
    return seed;
  }

  saveEntity(entity) {
    const list = this.getEntities();
    const full = { id: `ent_${Date.now()}`, type: 'note', label: 'Sem nome', detail: '', links: [], ...entity };
    const idx = list.findIndex((e) => e.id === full.id);
    if (idx >= 0) list[idx] = { ...list[idx], ...full };
    else list.unshift(full);
    localStorage.setItem(STORAGE_KEYS.GRAPH_ENTITIES, JSON.stringify(list));
    persistToDisk();
    return full;
  }

  deleteEntity(id) {
    const list = this.getEntities().filter((e) => e.id !== id)
      .map((e) => ({ ...e, links: (e.links || []).filter((l) => l.to !== id) }));
    localStorage.setItem(STORAGE_KEYS.GRAPH_ENTITIES, JSON.stringify(list));
    persistToDisk();
  }

  /** Constelação pronta para o SynapseGraph: projetos + entidades + tarefas. */
  getMemoryGraph() {
    const W = 560; const H = 300;
    const cx = W / 2; const cy = H / 2;
    const nodes = [{ id: 'core', label: 'J.A.R.V.I.S.', x: cx, y: cy, size: 10, color: '#2dfdd4' }];
    const links = [];
    const projects = this.getProjects().slice(0, 5);
    const entities = this.getEntities().slice(0, 8);
    const ring = [...projects.map((p) => ({ id: `proj:${p.id}`, label: p.name.slice(0, 14), color: '#8a5cff' })),
      ...entities.map((e) => ({ id: `ent:${e.id}`, label: String(e.label).slice(0, 14), color: e.type === 'person' ? '#ff3ec8' : e.type === 'decision' ? '#ffcf5c' : '#2dfdd4' }))];
    ring.forEach((n, i) => {
      const a = (i / Math.max(1, ring.length)) * Math.PI * 2 - Math.PI / 2;
      const x = cx + Math.cos(a) * (W / 2 - 60);
      const y = cy + Math.sin(a) * (H / 2 - 34);
      nodes.push({ ...n, x, y, size: 6 });
      links.push({ from: 'core', to: n.id });
    });
    // Relações entre entidades viram sinapses secundárias
    const byLabel = Object.fromEntries(ring.map((n) => [n.id, n]));
    for (const e of entities) {
      for (const l of e.links || []) {
        const to = `ent:${l.to}`;
        if (byLabel[`ent:${e.id}`] && byLabel[to]) links.push({ from: `ent:${e.id}`, to, kind: l.kind });
      }
    }
    return { nodes, links, W, H };
  }
}

export const memoryService = new MemoryMatrix();