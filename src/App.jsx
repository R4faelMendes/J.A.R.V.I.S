import React, { useState, useEffect } from 'react';
import Sidebar from './components/Sidebar';
import MiniHeader from './components/MiniHeader';
import AudioVisualizer from './components/AudioVisualizer';
import TerminalInput from './components/TerminalInput';
import TacticalMap from './components/TacticalMap';
import SchematicViewer from './components/SchematicViewer';
import AgendaModal from './components/AgendaModal';
import MemoryModal from './components/MemoryModal';
import MusicWidgetModal from './components/MusicWidgetModal';
import SettingsModal from './components/SettingsModal';
import AutomationModal from './components/AutomationModal';
import GameModeOverlay from './components/GameModeOverlay';

import { speechService } from './services/speechService';
import { sounds } from './services/soundEffects';
import { commandExecutor } from './services/commandExecutor';
import { aiService } from './services/aiService';
import { memoryService } from './services/memoryService';

export default function App() {
  const [activeTheme, setActiveTheme] = useState(() => {
    return memoryService.getPreferences().theme || 'cyan';
  });
  const [soundEnabled, setSoundEnabled] = useState(true);
  const [isListening, setIsListening] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [interimTranscript, setInterimTranscript] = useState('');
  const [lastDirective, setLastDirective] = useState(
    'Pronto, Senhor. Todos os protocolos de combate e telemetria estão sincronizados.'
  );

  // Novo layout estilo Codex: sidebar + área escura
  const [activeView, setActiveView] = useState('home'); // home, agenda, music, memory, automation, tactical, schematics, settings
  const [automationState, setAutomationState] = useState(null);
  const [gameModeActive, setGameModeActive] = useState(false);
  const [reactorModel, setReactorModel] = useState('mark_3');
  const [textCommand, setTextCommand] = useState('');
  const [ytSearch, setYtSearch] = useState('');

  const prefs = memoryService.getPreferences();
  const userName = prefs.userName || 'Tony Stark';
  const accessLevel = prefs.accessLevel || 'NÍVEL ALPHA';

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', activeTheme);
  }, [activeTheme]);

  useEffect(() => {
    const timer = setTimeout(() => {
      sounds.playPowerPulse();
      speechService.speak(
        `J.A.R.V.I.S. operacional. Conexão neural estabelecida, Senhor Stark. Todos os subsistemas nominais.`,
        () => setIsSpeaking(true),
        () => setIsSpeaking(false)
      );
    }, 1000);
    return () => clearTimeout(timer);
  }, []);

  const speakJarvis = (text) => {
    speechService.speak(
      text,
      () => setIsSpeaking(true),
      () => setIsSpeaking(false)
    );
  };

  const handleToggleMic = () => {
    speechService.toggleListening(
      (result) => {
        if (result.interim) setInterimTranscript(result.interim);
        if (result.final) {
          setInterimTranscript('');
          handleExecuteCommand(result.final);
        }
      },
      (status) => setIsListening(status.isListening)
    );
  };

  const runAutomation = async (automationType, params, speech) => {
    if (speech) speakJarvis(speech);
    setActiveView('automation');
    if (window.electronAPI && window.electronAPI.runAutomation) {
      window.electronAPI.runAutomation(automationType, params);
    }
    await commandExecutor.runAutomationSequence(
      automationType,
      params,
      (step) => setAutomationState(step)
    );
  };

  // Pipeline central de comandos — mesma essência, agora navegando na área escura
  const handleExecuteCommand = async (commandText) => {
    if (!commandText.trim()) return;

    setLastDirective(commandText);
    sounds.playClick();

    const res = commandExecutor.parseAndExecute(commandText);

    if (res.action === 'TOGGLE_GAME_MODE') {
      setGameModeActive(res.active);
      speakJarvis(res.speech);
      return;
    }

    if (res.action === 'SYSTEM_POWER') {
      speakJarvis(res.speech);
      if (window.electronAPI && window.electronAPI.systemPower) {
        window.electronAPI.systemPower(res.type);
      }
      return;
    }

    if (res.action?.startsWith('MUSIC_')) {
      speakJarvis(res.speech);
      setActiveView('music');
      return;
    }

    if (res.action === 'MEMORY_RECALL') {
      speakJarvis(res.speech);
      setActiveView('memory');
      return;
    }

    if (res.action === 'RUN_AUTOMATION') {
      await runAutomation(res.automationType, res.params, res.speech);
      return;
    }

    if (res.action === 'OPEN_MODAL') {
      speakJarvis(res.speech);
      setActiveView(res.modal);
      return;
    }

    if (res.action === 'CLOSE_MODAL') {
      speakJarvis(res.speech);
      goHome();
      return;
    }

    if (res.action === 'AUTOSTART_TOGGLE') {
      if (window.electronAPI?.setAutostart) {
        await window.electronAPI.setAutostart(res.enable);
      }
      return;
    }

    if (res.action === 'MUSIC_SEARCH_YT') {
      setYtSearch(res.query || '');
      setActiveView('music');
      speakJarvis(res.speech);
      return;
    }

    if (res.action === 'OPEN_RESOURCE') {
      setLastDirective(`Localizando ${res.query}...`);
      try {
        if (window.electronAPI?.pcControl) {
          const r = await window.electronAPI.pcControl(res.mode || 'open', res.query);
          const msg = r?.description || (r?.opened ? `Aberto: ${res.query}, Senhor.` : `Não encontrei ${res.query}, Senhor.`);
          setLastDirective(msg);
          speakJarvis(msg);
        } else {
          speakJarvis('Controle do PC disponível apenas no aplicativo desktop, Senhor.');
        }
      } catch {
        speakJarvis('Falha ao acessar o controle do PC, Senhor.');
      }
      return;
    }

    if (['SCAN_ALL', 'DIVERT_POWER', 'SENTINEL_PROTOCOL'].includes(res.action)) {
      speakJarvis(res.speech);
      setActiveView('tactical');
      return;
    }

    if (res.action === 'OPEN_AGENDA') {
      speakJarvis(res.speech);
      setActiveView('agenda');
      return;
    }

    if (res.action === 'QUERY_AI') {
      setActiveView('home');
      setLastDirective('Consultando matriz cognitiva...');
      try {
        const aiAnswer = await aiService.query(commandText);
        setLastDirective(aiAnswer);
        speakJarvis(aiAnswer);
      } catch {
        const fallback = 'Senhor, identifiquei sua solicitação, mas os servidores de IA remotos apresentaram latência. Módulos locais permanecem ativos.';
        setLastDirective(fallback);
        speakJarvis(fallback);
      }
    }
  };

  const handleToggleSound = () => {
    const next = !soundEnabled;
    setSoundEnabled(next);
    sounds.enabled = next;
  };

  const goHome = () => setActiveView('home');

  const renderView = () => {
    switch (activeView) {
      case 'agenda':
        return (
          <div className="h-full min-h-0 flex flex-col">
            <AgendaModal isOpen embedded onClose={goHome} />
          </div>
        );
      case 'music':
        return (
          <div className="flex-1 min-h-0 flex flex-col h-full">
            <MusicWidgetModal isOpen embedded onClose={goHome} searchQuery={ytSearch} />
          </div>
        );
      case 'memory':
        return (
          <div className="h-full min-h-0 flex flex-col">
            <MemoryModal isOpen embedded onClose={goHome} />
          </div>
        );
      case 'automation':
        if (automationState) {
          return (
            <div className="h-full min-h-0 flex flex-col">
              <AutomationModal isOpen embedded automationState={automationState} onClose={goHome} />
            </div>
          );
        }
        return (
          <div className="hud-panel rounded-xl p-6 max-w-2xl mx-auto font-['JetBrains_Mono']">
            <div className="corner-bracket corner-tl"></div>
            <div className="corner-bracket corner-tr"></div>
            <div className="corner-bracket corner-bl"></div>
            <div className="corner-bracket corner-br"></div>
            <h2 className="font-['Orbitron'] text-lg font-bold text-[var(--primary)] glow-cyan tracking-wider mb-2">
              PIPELINE DE AUTOMAÇÃO
            </h2>
            <p className="text-xs text-cyan-300/80 mb-4">
              Nenhuma sequência em execução. Inicie por voz ou pelos atalhos abaixo, Senhor.
            </p>
            <div className="flex flex-wrap gap-2">
              <button
                onClick={() => handleExecuteCommand('abrir vscode')}
                className="px-4 py-2 rounded bg-[var(--primary)] text-black font-bold text-xs hover:bg-white transition-all"
              >
                Abrir VS Code
              </button>
              <button
                onClick={() => runAutomation('CREATE_GITHUB_REPO', { repoName: 'temperatura-mobile' }, 'Iniciando sequência de criação de repositório no GitHub, Senhor.')}
                className="px-4 py-2 rounded bg-cyan-950/60 border border-cyan-500/40 text-cyan-200 font-bold text-xs hover:border-[var(--primary)] transition-all"
              >
                Criar repo GitHub
              </button>
            </div>
          </div>
        );
      case 'tactical':
        return (
          <div className="max-w-3xl mx-auto">
            <TacticalMap />
          </div>
        );
      case 'schematics':
        return (
          <div className="max-w-3xl mx-auto h-full">
            <SchematicViewer />
          </div>
        );
      case 'settings':
        return (
          <div className="h-full min-h-0 flex flex-col">
            <SettingsModal
              isOpen
              embedded
              onClose={goHome}
              activeTheme={activeTheme}
              onThemeChange={(t) => {
                setActiveTheme(t);
                memoryService.updatePreferences({ theme: t });
              }}
            />
          </div>
        );
      case 'home':
      default: {
        const tasks = memoryService.getTasks();
        const pending = tasks.filter((t) => !t.completed).length;
        const projects = memoryService.getProjects();
        return (
          <div className="max-w-3xl mx-auto flex flex-col gap-3">
            {/* Status da diretiva */}
            <div className="hud-panel rounded-xl p-4 relative overflow-hidden">
              <div className="corner-bracket corner-tl"></div>
              <div className="corner-bracket corner-tr"></div>
              <div className="corner-bracket corner-bl"></div>
              <div className="corner-bracket corner-br"></div>
              <div className="text-[10px] font-['JetBrains_Mono'] tracking-widest text-cyan-400/70 mb-1">
                ÚLTIMA DIRETIVA
              </div>
              <p className="font-['JetBrains_Mono'] text-sm text-cyan-100 italic">
                &ldquo;{lastDirective}&rdquo;
              </p>
              <div className="mt-3 flex gap-2 text-[10px] font-['JetBrains_Mono']">
                <span className="px-2 py-0.5 rounded bg-white/5 border border-cyan-500/20 text-cyan-200">
                  {pending} TAREFAS PENDENTES
                </span>
                <span className="px-2 py-0.5 rounded bg-white/5 border border-cyan-500/20 text-cyan-200">
                  {projects.length} PROJETOS
                </span>
                <span className={`px-2 py-0.5 rounded border ${isListening ? 'bg-red-500/20 border-red-500/40 text-red-300' : 'bg-white/5 border-emerald-500/30 text-emerald-300'}`}>
                  {isListening ? 'ESCUTANDO' : isSpeaking ? 'TRANSMITINDO' : 'PRONTO'}
                </span>
              </div>
            </div>

            <AudioVisualizer
              isListening={isListening}
              isSpeaking={isSpeaking}
              onToggleMic={handleToggleMic}
              lastDirective={lastDirective}
              interimTranscript={interimTranscript}
            />

            {/* Entrada de texto + ações rápidas */}
            <form
              onSubmit={(e) => {
                e.preventDefault();
                handleExecuteCommand(textCommand);
                setTextCommand('');
              }}
              className="hud-panel rounded-xl p-3 flex items-center gap-2"
            >
              <span className="text-[var(--primary)] font-['JetBrains_Mono'] text-sm font-bold pl-1">&gt;</span>
              <input
                value={textCommand}
                onChange={(e) => setTextCommand(e.target.value)}
                placeholder="Digite uma diretiva ou fale com o microfone..."
                className="flex-1 bg-transparent outline-none text-sm font-['JetBrains_Mono'] text-cyan-100 placeholder:text-cyan-400/40"
              />
              <button
                type="submit"
                className="px-4 py-1.5 rounded bg-[var(--primary)] text-black text-xs font-bold font-['Orbitron'] hover:bg-white transition-all"
              >
                EXECUTAR
              </button>
            </form>

            <div className="flex flex-wrap gap-2 font-['JetBrains_Mono'] text-[11px]">
              {[
                { label: 'Varredura completa', cmd: 'varredura completa dos sistemas' },
                { label: 'Divertir energia', cmd: 'redirecionar energia para os propulsores' },
                { label: 'Protocolo sentinela', cmd: 'ativar protocolo sentinela' },
                { label: 'Abrir agenda', cmd: 'abrir agenda' },
              ].map((a) => (
                <button
                  key={a.label}
                  onClick={() => handleExecuteCommand(a.cmd)}
                  className="px-3 py-1.5 rounded bg-white/5 border border-cyan-500/25 text-cyan-200 hover:border-[var(--primary)] hover:text-white transition-all"
                >
                  {a.label}
                </button>
              ))}
            </div>

            <TerminalInput isListening={isListening} onToggleMic={handleToggleMic} />
          </div>
        );
      }
    }
  };

  return (
    <div className="flex h-screen w-screen overflow-hidden bg-[#0b0e1a] text-[#b8e2f2] selection:bg-cyan-500/30 selection:text-cyan-200">
      {/* Sidebar estilo Codex — Reator ARC + conteúdos */}
      <Sidebar
        activeView={activeView}
        onSelect={(v) => setActiveView(v)}
        isSpeaking={isSpeaking}
        isListening={isListening}
        reactorModel={reactorModel}
        onModelChange={(m) => setReactorModel(m)}
        userName={userName}
        accessLevel={accessLevel}
      />

      {/* Área escura — mini header + conteúdo selecionado */}
      <div className="flex-1 flex flex-col min-w-0 bg-[#121212]">
        <MiniHeader
          activeView={activeView}
          activeTheme={activeTheme}
          onThemeChange={(t) => {
            setActiveTheme(t);
            memoryService.updatePreferences({ theme: t });
          }}
          soundEnabled={soundEnabled}
          onToggleSound={handleToggleSound}
          onOpenSettings={() => setActiveView('settings')}
          onToggleGameMode={() => setGameModeActive(true)}
        />
        <main className="flex-1 flex flex-col min-h-0 p-4">
          {renderView()}
        </main>
      </div>

      <GameModeOverlay
        isActive={gameModeActive}
        onWakeUp={() => {
          setGameModeActive(false);
          speakJarvis('Reativando todos os sistemas holográficos, Senhor.');
        }}
        isListening={isListening}
        onToggleMic={handleToggleMic}
      />
    </div>
  );
}
