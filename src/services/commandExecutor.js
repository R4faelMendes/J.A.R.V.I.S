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

    // 1. Hibernation / Game Mode
    if (raw.includes('hibernar') || raw.includes('modo jogo') || raw.includes('game mode') || raw.includes('modo game') || raw.includes('suspender')) {
      sounds.playPowerPulse();
      return {
        action: 'TOGGLE_GAME_MODE',
        active: true,
        speech: 'Entrando em Modo Hibernação e Otimização para Jogos. Recursos de GPU e memória liberados. Estarei em escuta passiva, Senhor.'
      };
    }
    if (raw.includes('acordar') || raw.includes('sair da hibernação') || raw.includes('desativar modo jogo') || raw.includes('reativar')) {
      sounds.playSuccess();
      return {
        action: 'TOGGLE_GAME_MODE',
        active: false,
        speech: 'Sistemas reativados. Todos os módulos holográficos e telemetria voltando à capacidade máxima.'
      };
    }

    // 2. Power Controls & App Exit
    if (raw.includes('fechar jarvis') || raw.includes('fechar aplicativo') || raw.includes('encerrar') || raw === 'sair' || raw === 'exit') {
      sounds.playPowerPulse();
      return {
        action: 'SYSTEM_POWER',
        type: 'EXIT_APP',
        speech: 'Desconectando interface e encerrando o aplicativo. Até breve, Senhor.'
      };
    }
    if (raw.includes('desligar o pc') || raw.includes('desligar o windows') || raw.includes('desligar computador') || raw.includes('shutdown')) {
      sounds.playAlert();
      return {
        action: 'SYSTEM_POWER',
        type: 'SHUTDOWN',
        speech: 'Iniciando sequência de desligamento do Windows. Salvando dados de telemetria e desconectando nós neurais. Boa noite, Senhor.'
      };
    }
    if (raw.includes('reiniciar o pc') || raw.includes('reiniciar computador') || raw.includes('restart')) {
      sounds.playAlert();
      return {
        action: 'SYSTEM_POWER',
        type: 'RESTART',
        speech: 'Reiniciando o sistema operacional em 5 segundos.'
      };
    }
    if (raw.includes('bloquear tela') || raw.includes('travar pc') || raw.includes('lock')) {
      sounds.playClick();
      return {
        action: 'SYSTEM_POWER',
        type: 'LOCK',
        speech: 'Estação de trabalho bloqueada com criptografia quântica Stark.'
      };
    }

    // 3. Music Controls
    if (raw.includes('tocar musica') || raw.includes('play') || raw.includes('despausar musica')) {
      const res = this.toggleMusic();
      return { action: 'MUSIC_PLAY', speech: 'Reproduzindo trilha tática.', state: res.state };
    }
    if (raw.includes('pausar musica') || raw.includes('pausa') || raw.includes('parar musica') || raw.includes('pause')) {
      this.musicState.isPlaying = false;
      return { action: 'MUSIC_PAUSE', speech: 'Áudio pausado.', state: { ...this.musicState } };
    }
    if (raw.includes('proxima musica') || raw.includes('pular musica') || raw.includes('next')) {
      const res = this.nextTrack();
      return { action: 'MUSIC_NEXT', speech: res.message, state: res.state };
    }
    if (raw.includes('aumentar volume') || raw.includes('volume mais alto')) {
      const res = this.setVolume(this.musicState.volume + 15);
      return { action: 'MUSIC_VOL', speech: res.message, state: res.state };
    }
    if (raw.includes('abaixar volume') || raw.includes('diminuir volume') || raw.includes('volume mais baixo')) {
      const res = this.setVolume(this.musicState.volume - 15);
      return { action: 'MUSIC_VOL', speech: res.message, state: res.state };
    }

    // 4. Memory Recall of Projects
    if (raw.includes('lembra') || raw.includes('projeto') || raw.includes('temperatura')) {
      const project = memoryService.findProjectByName('temperatura');
      if (project) {
        sounds.playSuccess();
        const speech = `Sim, Senhor. Lembro-me perfeitamente do projeto "${project.name}". É um ${project.description}. As tecnologias utilizadas são ${project.techStack.join(', ')}. Os arquivos principais incluem ${project.importantFiles.slice(0, 2).join(' e ')}. O último erro registrado foi "${project.lastErrors[0]}".`;
        return {
          action: 'MEMORY_RECALL',
          data: project,
          speech
        };
      }
    }

    // 5. Automation: Create GitHub Repo
    if (raw.includes('github') && (raw.includes('crie') || raw.includes('novo repositorio') || raw.includes('criar'))) {
      let repoName = 'temperatura-mobile';
      const match = raw.match(/chamado\s+([a-zA-Z0-9_-]+)/i) || raw.match(/repositório\s+([a-zA-Z0-9_-]+)/i);
      if (match && match[1]) repoName = match[1];

      return {
        action: 'RUN_AUTOMATION',
        automationType: 'CREATE_GITHUB_REPO',
        params: { repoName },
        speech: `Iniciando automação de navegador para criar o repositório "${repoName}" no GitHub.`
      };
    }

    // 6. Open VS Code
    if (raw.includes('vscode') || raw.includes('vs code') || raw.includes('abrir codigo') || raw.includes('abrir código') || raw.includes('abrir no vscode')) {
      return {
        action: 'RUN_AUTOMATION',
        automationType: 'OPEN_VSCODE',
        params: { project: 'temperatura-mobile' },
        speech: 'Abrindo o ambiente de desenvolvimento Visual Studio Code com o projeto selecionado.'
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