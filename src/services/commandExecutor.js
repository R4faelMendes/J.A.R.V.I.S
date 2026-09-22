// J.A.R.V.I.S. Command Routing, System Actions & Automation Pipeline
import { sounds } from './soundEffects';
import { memoryService } from './memoryService';

export const AutomationStatus = {
  IDLE: 'IDLE',
  RUNNING: 'RUNNING',
  COMPLETED: 'COMPLETED',
  FAILED: 'FAILED'
};

// ---------------------------------------------------------------------------
// Interpretação de comandos de abertura / busca / encerramento no PC
// ---------------------------------------------------------------------------

const OPEN_VERBS = [
  'abrir', 'abre', 'abramos', 'abri', 'abre ai', 'iniciar', 'inicia', 'inicie', 'inicia ai',
  'executar', 'executa', 'execute', 'rodar', 'roda', 'rode', 'lançar', 'lanca', 'lança',
  'colocar', 'coloca', 'mostrar', 'mostra', 'carregar', 'carrega', 'vamos para'
];

const SEARCH_VERBS = [
  'procurar', 'procura', 'procura ai', 'localizar', 'localiza', 'buscar', 'busca',
  'pesquisar', 'pesquisa', 'onde está', 'onde esta', 'acha', 'ache', 'achar'
];

const CLOSE_VERBS = [
  'fechar', 'fecha', 'fechem', 'encerrar', 'encerra', 'finalizar', 'finaliza',
  'matar processo', 'mate', 'kill'
];

const FILLER_WORDS = new Set([
  'o', 'a', 'os', 'as', 'um', 'uma', 'uns', 'umas', 'de', 'do', 'da', 'dos', 'das',
  'em', 'no', 'na', 'nos', 'nas', 'com', 'sem', 'que', 'e', 'ou', 'pra', 'para', 'p',
  'mim', 'meu', 'minha', 'meus', 'minhas', 'seu', 'sua', 'esse', 'essa', 'este', 'esta',
  'isso', 'ele', 'ela', 'eles', 'elas', 'ai', 'lá', 'la', 'agora', 'favor', 'por favor',
  'abrir', 'abre', 'abri', 'iniciar', 'inicia', 'inicie', 'executar', 'executa', 'execute',
  'rodar', 'roda', 'rode', 'lançar', 'lanca', 'lança', 'mostrar', 'mostra', 'colocar', 'coloca',
  'carregar', 'carrega', 'procurar', 'procura', 'localizar', 'localiza', 'buscar', 'busca',
  'pesquisar', 'pesquisa', 'achar', 'ache', 'acha', 'fechar', 'fecha', 'encerrar', 'encerra',
  'finalizar', 'finaliza', 'aberto', 'aberta', 'pra mim', 'no meu', 'do meu', 'da minha'
]);

const TAIL_PHRASES = [
  'por favor', 'pra mim', 'para mim', 'no meu computador', 'no computador', 'nesse computador',
  'neste computador', 'no pc', 'nesse pc', 'neste pc', 'aqui', 'agora', 'se faz favor'
];

// Alvos internos do painel (não são programas do Windows)
const INTERNAL_TARGETS = [
  { keys: ['agenda', 'calendario', 'calendário', 'tarefas', 'tarefa', 'compromissos', 'compromisso', 'lembretes', 'lembrete'], modal: 'agenda', speech: 'Abrindo a agenda tática, Senhor.' },
  { keys: ['memoria', 'memória', 'memorias', 'memórias', 'historico', 'histórico'], modal: 'memory', speech: 'Abrindo a matriz de memória de longo prazo, Senhor.' },
  { keys: ['configuracao', 'configurações', 'configuracoes', 'configurar', 'ajustes', 'ajuste', 'preferencias', 'preferências', 'opcões', 'opcoes'], modal: 'settings', speech: 'Abrindo o painel de configurações, Senhor.' },
  { keys: ['musica', 'música', 'midia', 'mídia', 'player', 'tocador'], modal: 'music', speech: 'Abrindo o controle de mídia, Senhor.' }
];

function normalizeText(text) {
  return String(text || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9\s.+#-]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

function cleanTarget(text) {
  let s = String(text || '').toLowerCase().trim();
  for (const phrase of TAIL_PHRASES) {
    s = s.split(phrase).join(' ');
  }
  s = s.replace(/\s+/g, ' ').trim();
  const words = s
    .split(/\s+/)
    .map(w => w.replace(/^[,.:;!?]+|[,.:;!?]+$/g, ''))
    .filter(w => w && !FILLER_WORDS.has(w));
  return words.join(' ').trim();
}

function findInternalTarget(targetNorm) {
  const norm = normalizeText(targetNorm);
  if (!norm) return null;
  return INTERNAL_TARGETS.find(t => t.keys.some(k => norm === k || norm.includes(k))) || null;
}

function matchVerbCommand(raw, verbs) {
  const text = String(raw).trim();
  for (const verb of verbs) {
    if (text === verb) return { verb, target: '' };
    if (text.startsWith(`${verb} `)) {
      return { verb, target: text.slice(verb.length + 1) };
    }
    if (text.startsWith(`${verb},`) || text.startsWith(`${verb}:`)) {
      return { verb, target: text.slice(verb.length + 1) };
    }
  }
  return null;
}

class CommandExecutor {
  constructor() {
    this.musicState = {
      isPlaying: true,
      currentTrack: 'AC/DC - Back in Black (Stark High-Gain Mix)',
      volume: 80,
      playlist: [
        'AC/DC - Back in Black (Stark High-Gain Mix)',
        'Black Sabbath - Iron Man',
        'The Clash - Should I Stay or Should I Go',
        'Daft Punk - Tron: Legacy (End Titles)',
        'Hans Zimmer - Driving with the Top Down'
      ],
      trackIndex: 0
    };
    this.currentAutomation = null;
    this.onAutomationStep = null;
  }

  // ---- Music Controller ----
  toggleMusic() {
    this.musicState.isPlaying = !this.musicState.isPlaying;
    sounds.playClick();
    return {
      success: true,
      message: this.musicState.isPlaying ? 'Retomando reprodução de áudio.' : 'Música pausada.',
      state: { ...this.musicState }
    };
  }

  nextTrack() {
    this.musicState.trackIndex = (this.musicState.trackIndex + 1) % this.musicState.playlist.length;
    this.musicState.currentTrack = this.musicState.playlist[this.musicState.trackIndex];
    sounds.playClick();
    return {
      success: true,
      message: `Tocando agora: ${this.musicState.currentTrack}`,
      state: { ...this.musicState }
    };
  }

  previousTrack() {
    this.musicState.trackIndex = (this.musicState.trackIndex - 1 + this.musicState.playlist.length) % this.musicState.playlist.length;
    this.musicState.currentTrack = this.musicState.playlist[this.musicState.trackIndex];
    sounds.playClick();
    return {
      success: true,
      message: `Tocando agora: ${this.musicState.currentTrack}`,
      state: { ...this.musicState }
    };
  }

  setVolume(vol) {
    this.musicState.volume = Math.max(0, Math.min(100, vol));
    sounds.playBeep(900 + vol * 3, 0.05);
    return {
      success: true,
      message: `Volume ajustado para ${this.musicState.volume}%.`,
      state: { ...this.musicState }
    };
  }

  // ---- Automation Simulator (GitHub Repo, VS Code, Browser control) ----
  async runAutomationSequence(type, params, onStepUpdate) {
    this.onStepUpdate = onStepUpdate;
    sounds.playScan();

    if (type === 'CREATE_GITHUB_REPO') {
      const repoName = params.repoName || 'temperatura-mobile';
      const steps = [
        { id: 1, title: 'Inicializando Navegador Seguro...', detail: 'Abrindo Chromium com perfil Stark-Auth', duration: 1000 },
        { id: 2, title: 'Navegando para GitHub...', detail: 'https://github.com/new', duration: 1200 },
        { id: 3, title: 'Localizando campos de formulário...', detail: 'Foco no campo input #repository_name', duration: 900 },
        { id: 4, title: `Digitando "${repoName}"...`, detail: 'Controle de teclado emulado via barramento tático', duration: 1100 },
        { id: 5, title: 'Configurando Visibilidade & README...', detail: 'Repositório Privado • Inicializar com README.md', duration: 800 },
        { id: 6, title: 'Acionando botão "Create Repository"...', detail: 'Clique simulado em coordenadas (x: 840, y: 620)', duration: 1400 },
        { id: 7, title: 'Repositório Criado com Sucesso!', detail: `https://github.com/tonystark/${repoName}`, duration: 600 }
      ];

      for (let i = 0; i < steps.length; i++) {
        const step = steps[i];
        if (onStepUpdate) {
          onStepUpdate({
            status: AutomationStatus.RUNNING,
            currentStep: i + 1,
            totalSteps: steps.length,
            stepData: step
          });
        }
        sounds.playBeep(700 + i * 80, 0.06);
        await new Promise(r => setTimeout(r, step.duration));
      }

      sounds.playSuccess();
      // Record to long term memory
      memoryService.saveProject({
        name: repoName,
        description: `Repositório criado automaticamente pelo JARVIS em ${new Date().toLocaleDateString('pt-BR')}`,
        techStack: ['Node.js', 'React Native'],
        importantFiles: ['README.md', '.gitignore'],
        lastErrors: [],
        gitBranch: 'main'
      });

      if (onStepUpdate) {
        onStepUpdate({
          status: AutomationStatus.COMPLETED,
          currentStep: steps.length,
          totalSteps: steps.length,
          stepData: steps[steps.length - 1]
        });
      }

      return `Senhor, o repositório "${repoName}" foi criado no GitHub com sucesso e sincronizado na sua matriz de projetos.`;
    }

    if (type === 'OPEN_VSCODE') {
      const project = params.project || 'temperatura-mobile';
      if (onStepUpdate) {
        onStepUpdate({
          status: AutomationStatus.RUNNING,
          currentStep: 1,
          totalSteps: 2,
          stepData: { title: 'Localizando diretório do projeto...', detail: `C:/Users/Tony/Projects/${project}` }
        });
      }
      await new Promise(r => setTimeout(r, 800));
      sounds.playPowerPulse();
      if (onStepUpdate) {
        onStepUpdate({
          status: AutomationStatus.COMPLETED,
          currentStep: 2,
          totalSteps: 2,
          stepData: { title: 'Visual Studio Code Inicializado', detail: `code C:/Users/Tony/Projects/${project}` }
        });
      }
      return `Abrindo o projeto "${project}" no Visual Studio Code, Senhor.`;
    }

    return 'Automação concluída.';
  }

  // ---- System Commands Parser ----
  parseAndExecute(input) {
    const raw = input.toLowerCase().trim();
    memoryService.logCommand(input);

    // Detecção antecipada de comandos direcionados ao PC
    const openCmd = matchVerbCommand(raw, OPEN_VERBS);
    const searchCmd = matchVerbCommand(raw, SEARCH_VERBS);
    const closeCmd = matchVerbCommand(raw, CLOSE_VERBS);

    const openTarget = openCmd ? cleanTarget(openCmd.target) : '';
    const searchTarget = searchCmd ? cleanTarget(searchCmd.target) : '';
    const closeTarget = closeCmd ? cleanTarget(closeCmd.target) : '';
    const isSystemCommand = !!(openCmd || searchCmd || closeCmd);

    // 1. Fechar / encerrar (aplicativos do Windows, modais internos e o J.A.R.V.I.S.)
    if (raw === 'sair' || raw === 'exit') {
      sounds.playPowerPulse();
      return { action: 'SYSTEM_POWER', type: 'EXIT_APP', speech: 'Desconectando interface e encerrando o aplicativo. Até breve, Senhor.' };
    }

    if (closeCmd && closeTarget) {
      const closeNorm = normalizeText(closeTarget);
      const closeInternal = findInternalTarget(closeNorm);

      if (closeInternal) {
        sounds.playClick();
        return {
          action: 'CLOSE_MODAL',
          modal: closeInternal.modal,
          speech: `Encerrando ${closeInternal.modal === 'agenda' ? 'a agenda' : 'o painel'}, Senhor.`
        };
      }

      if (closeNorm.includes('vscode') || closeNorm.includes('vs code') || closeNorm === 'code' || closeNorm === 'visual studio code') {
        sounds.playAlert();
        if (typeof window !== 'undefined' && window.electronAPI?.processControl) {
          window.electronAPI.processControl('KILL', 'vscode');
        }
        return { action: 'PROCESS_KILL', speech: 'Encerrando o Visual Studio Code imediatamente, Senhor.' };
      }
      if (closeNorm.includes('brave') || closeNorm.includes('navegador')) {
        sounds.playAlert();
        if (typeof window !== 'undefined' && window.electronAPI?.processControl) {
          window.electronAPI.processControl('KILL', 'brave');
        }
        return { action: 'PROCESS_KILL', speech: 'Encerrando o navegador Brave, Senhor.' };
      }
      if (closeNorm.includes('jarvis') || closeNorm.includes('aplicativo') || closeNorm.includes('programa')) {
        sounds.playPowerPulse();
        return { action: 'SYSTEM_POWER', type: 'EXIT_APP', speech: 'Desconectando interface e encerrando o aplicativo. Até breve, Senhor.' };
      }

      // Encerramento genérico: qualquer processo em execução
      sounds.playAlert();
      return {
        action: 'CLOSE_RESOURCE',
        query: closeTarget,
        speech: `Procurando ${closeTarget} em execução, Senhor.`
      };
    }

    // Windows Autostart Voice Command
    if (raw.includes('iniciar com o windows') || raw.includes('inicialização com o windows') || raw.includes('inicializacao com o windows') || raw.includes('abrir com o windows') || raw.includes('iniciar junto com o windows')) {
      if (raw.includes('desativar') || raw.includes('remover') || raw.includes('cancelar') || raw.includes('não') || raw.includes('nao')) {
        sounds.playClick();
        return {
          action: 'AUTOSTART_TOGGLE',
          enable: false,
          speech: 'Inicialização automática com o Windows desativada, Senhor.'
        };
      }
      sounds.playSuccess();
      return {
        action: 'AUTOSTART_TOGGLE',
        enable: true,
        speech: 'Diretiva confirmada, Senhor. O J.A.R.V.I.S. agora será inicializado automaticamente junto com o Windows.'
      };
    }

    // 2. Music Controls with YouTube Music on Brave
    // "tocar <algo>" -> preenche o box do YouTube Music com a busca (sem janela extra)
    const playVerb = raw.match(/^(?:quer(?:\s+to)?\s+)?(?:tocar|colocar|coloca|bota|botar|poe|põe|passa|manda|quer)\s+(.+)$/);
    if (!isSystemCommand && playVerb) {
      const target = playVerb[1]
        .replace(/\b(no youtube music|no youtube music|no youtubemusic|no youtube|na youtube music|no spoty|aqui|agora|por favor|pra mim|para mim|por gentileza)\b/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();
      const generico = ['', 'musica', 'música', 'uma musica', 'uma música', 'algo', 'alguma coisa', 'youtube', 'youtube music', 'a musica'];
      if (!generico.includes(target)) {
        sounds.playSuccess();
        return {
          action: 'MUSIC_SEARCH_YT',
          query: target,
          speech: `Procurando ${target} no YouTube Music, Senhor.`
        };
      }
    }
    if (!isSystemCommand && (raw.includes('tocar musica') || raw.includes('play') || raw.includes('despausar') || raw.includes('pausar musica') || raw.includes('pausa'))) {
      if (typeof window !== 'undefined' && window.electronAPI?.mediaControl) {
        window.electronAPI.mediaControl('PLAY_PAUSE');
      }
      const res = this.toggleMusic();
      return { action: 'MUSIC_PLAY_PAUSE', speech: 'Sinal de reprodução enviado ao YouTube Music no Brave.', state: res.state };
    }
    if (!isSystemCommand && (raw.includes('proxima musica') || raw.includes('pular musica') || raw.includes('next'))) {
      if (typeof window !== 'undefined' && window.electronAPI?.mediaControl) {
        window.electronAPI.mediaControl('NEXT');
      }
      const res = this.nextTrack();
      return { action: 'MUSIC_NEXT', speech: 'Avançando faixa no YouTube Music.', state: res.state };
    }
    if (!isSystemCommand && (raw.includes('musica anterior') || raw.includes('voltar musica'))) {
      if (typeof window !== 'undefined' && window.electronAPI?.mediaControl) {
        window.electronAPI.mediaControl('PREV');
      }
      const res = this.previousTrack();
      return { action: 'MUSIC_PREV', speech: 'Retornando faixa no YouTube Music.', state: res.state };
    }

    // 3. Painéis internos (agenda, memória, configurações, mídia)
    let internalProbe = '';
    if (openCmd) internalProbe = openTarget;
    else if (searchCmd) internalProbe = searchTarget;
    else if (raw.split(/\s+/).length <= 4) internalProbe = raw;

    const internal = findInternalTarget(internalProbe);
    if (internal) {
      sounds.playClick();
      return { action: 'OPEN_MODAL', modal: internal.modal, speech: internal.speech };
    }

    // 4. Abrir / procurar apps, documentos e projetos no PC
    if (openTarget) {
      return {
        action: 'OPEN_RESOURCE',
        mode: 'open',
        query: openTarget,
        speech: `Procurando ${openTarget} no seu computador, Senhor.`
      };
    }
    if (searchTarget) {
      return {
        action: 'OPEN_RESOURCE',
        mode: 'search',
        query: searchTarget,
        speech: `Procurando ${searchTarget} nos seus arquivos, Senhor.`
      };
    }

    // 7. Tactical Actions
    if (!isSystemCommand && (raw.includes('varredura') || raw.includes('diagnostico') || raw.includes('diagnóstico') || raw.includes('status'))) {
      sounds.playScan();
      return {
        action: 'SCAN_ALL',
        speech: 'Varredura completa concluída. Todos os subsistemas e matrizes de confinamento do reator ARC estão operando a 100% de capacidade nominal.'
      };
    }
    if (!isSystemCommand && (raw.includes('energia') || raw.includes('redirecionar') || raw.includes('propulsores') || raw.includes('escudos'))) {
      sounds.playPowerPulse();
      return {
        action: 'DIVERT_POWER',
        speech: 'Potência de 3.2 Giga-Joules do Reator ARC redistribuída com prioridade para propulsores e escudos magnéticos.'
      };
    }
    if (!isSystemCommand && (raw.includes('sentinela') || raw.includes('veronica') || raw.includes('verônica') || raw.includes('defesa'))) {
      sounds.playAlert();
      return {
        action: 'SENTINEL_PROTOCOL',
        speech: 'Alerta de defesa: Protocolo Sentinela acionado. Satélite Verônica em espera orbital com uplink seguro de 99.98%.'
      };
    }

    // 8. Agenda (comando curto e direto)
    if (!isSystemCommand && (raw.includes('agenda') || raw.includes('tarefas') || raw.includes('compromisso') || raw.includes('calendario') || raw.includes('calendário'))) {
      sounds.playClick();
      return {
        action: 'OPEN_AGENDA',
        speech: 'Abrindo a agenda tática. Você possui tarefas pendentes de alta prioridade para sincronização de reator e verificação orbital.'
      };
    }

    // Default to AI Query
    return {
      action: 'QUERY_AI',
      prompt: input
    };
  }
}

export const commandExecutor = new CommandExecutor();