// J.A.R.V.I.S. Command Routing, System Actions & Automation Pipeline
import { sounds } from './soundEffects';
import { memoryService } from './memoryService';

export const AutomationStatus = {
  IDLE: 'IDLE',
  RUNNING: 'RUNNING',
  COMPLETED: 'COMPLETED',
  FAILED: 'FAILED'
};

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

    // 1. App Process Kill/Close (Explicit distinction)
    if (raw.includes('fechar') || raw.includes('encerrar') || raw.includes('matar processo')) {
      if (raw.includes('vscode') || raw.includes('vs code') || raw.includes('código') || raw.includes('codigo')) {
        sounds.playAlert();
        if (typeof window !== 'undefined' && window.electronAPI?.processControl) {
          window.electronAPI.processControl('KILL', 'vscode');
        }
        return {
          action: 'PROCESS_KILL',
          speech: 'Encerrando o Visual Studio Code imediatamente, Senhor.'
        };
      }
      if (raw.includes('brave') || raw.includes('navegador')) {
        sounds.playAlert();
        if (typeof window !== 'undefined' && window.electronAPI?.processControl) {
          window.electronAPI.processControl('KILL', 'brave');
        }
        return { action: 'PROCESS_KILL', speech: 'Encerrando o navegador Brave, Senhor.' };
      }
      if (raw.includes('jarvis') || raw.includes('aplicativo') || raw === 'sair' || raw === 'exit') {
        sounds.playPowerPulse();
        return { action: 'SYSTEM_POWER', type: 'EXIT_APP', speech: 'Desconectando interface e encerrando o aplicativo. Até breve, Senhor.' };
      }
    }

    // 2. Music Controls with YouTube Music on Brave
    if (raw.includes('tocar musica') || raw.includes('play') || raw.includes('despausar') || raw.includes('pausar musica') || raw.includes('pausa')) {
      if (typeof window !== 'undefined' && window.electronAPI?.mediaControl) {
        window.electronAPI.mediaControl('PLAY_PAUSE');
      }
      const res = this.toggleMusic();
      return { action: 'MUSIC_PLAY_PAUSE', speech: 'Sinal de reprodução enviado ao YouTube Music no Brave.', state: res.state };
    }
    if (raw.includes('proxima musica') || raw.includes('pular musica') || raw.includes('next')) {
      if (typeof window !== 'undefined' && window.electronAPI?.mediaControl) {
        window.electronAPI.mediaControl('NEXT');
      }
      const res = this.nextTrack();
      return { action: 'MUSIC_NEXT', speech: 'Avançando faixa no YouTube Music.', state: res.state };
    }
    if (raw.includes('musica anterior') || raw.includes('voltar musica')) {
      if (typeof window !== 'undefined' && window.electronAPI?.mediaControl) {
        window.electronAPI.mediaControl('PREV');
      }
      const res = this.previousTrack();
      return { action: 'MUSIC_PREV', speech: 'Retornando faixa no YouTube Music.', state: res.state };
    }

    // 3. Open VS Code (ONLY when NOT closing)
    if ((raw.includes('abrir vscode') || raw.includes('abrir vs code') || raw.includes('abrir codigo') || raw.includes('abrir código')) && !raw.includes('fechar')) {
      return {
        action: 'RUN_AUTOMATION',
        automationType: 'OPEN_VSCODE',
        params: { project: 'temperatura-mobile' },
        speech: 'Abrindo o ambiente de desenvolvimento Visual Studio Code, Senhor.'
      };
    }

    // 7. Tactical Actions
    if (raw.includes('varredura') || raw.includes('diagnostico') || raw.includes('diagnóstico') || raw.includes('status')) {
      sounds.playScan();
      return {
        action: 'SCAN_ALL',
        speech: 'Varredura completa concluída. Todos os subsistemas e matrizes de confinamento do reator ARC estão operando a 100% de capacidade nominal.'
      };
    }
    if (raw.includes('energia') || raw.includes('redirecionar') || raw.includes('propulsores') || raw.includes('escudos')) {
      sounds.playPowerPulse();
      return {
        action: 'DIVERT_POWER',
        speech: 'Potência de 3.2 Giga-Joules do Reator ARC redistribuída com prioridade para propulsores e escudos magnéticos.'
      };
    }
    if (raw.includes('sentinela') || raw.includes('veronica') || raw.includes('verônica') || raw.includes('defesa')) {
      sounds.playAlert();
      return {
        action: 'SENTINEL_PROTOCOL',
        speech: 'Alerta de defesa: Protocolo Sentinela acionado. Satélite Verônica em espera orbital com uplink seguro de 99.98%.'
      };
    }

    // 8. Agenda
    if (raw.includes('agenda') || raw.includes('tarefas') || raw.includes('compromisso') || raw.includes('calendario') || raw.includes('calendário')) {
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