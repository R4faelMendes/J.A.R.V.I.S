import React, { useState, useEffect, useRef } from 'react';
import Header from './components/Header';
import Biometrics from './components/Biometrics';
import Diagnostics from './components/Diagnostics';
import ArcReactor from './components/ArcReactor';
import AudioVisualizer from './components/AudioVisualizer';
import SchematicViewer from './components/SchematicViewer';
import MusicBox from './components/MusicBox';
import ResizableBox from './components/ResizableBox';
import TerminalInput from './components/TerminalInput';
import AgendaModal from './components/AgendaModal';
import MemoryModal from './components/MemoryModal';
import MusicWidgetModal from './components/MusicWidgetModal';
import SettingsModal from './components/SettingsModal';
import AutomationModal from './components/AutomationModal';
import GameModeOverlay from './components/GameModeOverlay';

import { speechService } from './services/speechService';
import { sounds } from './services/soundEffects';
import { commandExecutor, AutomationStatus } from './services/commandExecutor';
import { aiService } from './services/aiService';
import { memoryService } from './services/memoryService';
import { runSmokeTest } from './services/selfTest';

export default function App() {
  const [activeTheme, setActiveTheme] = useState(() => {
    return memoryService.getPreferences().theme || 'cyan';
  });
  const [soundEnabled, setSoundEnabled] = useState(() => (memoryService.getPreferences().soundEnabled !== false));
  const [isListening, setIsListening] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [interimTranscript, setInterimTranscript] = useState('');
  const [lastDirective, setLastDirective] = useState(
    'Pronto, Senhor. Todos os protocolos de combate e telemetria estão sincronizados.'
  );

  // Modals state
  const [activeModal, setActiveModal] = useState(null); // 'agenda', 'memory', 'music', 'settings', 'automation'
  const [automationState, setAutomationState] = useState(null);
  const [gameModeActive, setGameModeActive] = useState(false);
  const [ytMusicSearch, setYtMusicSearch] = useState(null); // comando de voz "tocar X"

  // Load preferences
  const prefs = memoryService.getPreferences();
  const userName = prefs.userName || 'Tony Stark';
  const accessLevel = prefs.accessLevel || 'NÍVEL ALPHA';

  const [micPhase, setMicPhase] = useState('idle');
  const [engineInfo, setEngineInfo] = useState(() => speechService.getEngineInfo());

  // Apply theme to document
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', activeTheme);
  }, [activeTheme]);

  // Calibra o motor de voz com as preferências salvas
  useEffect(() => {
    const p = memoryService.getPreferences() || {};
    speechService.configure({
      voiceEngine: p.voiceEngine || 'neural',
      voiceId: p.voiceId || 'pt-BR-AntonioNeural',
      localVoiceName: p.localVoiceName || '',
      voiceSpeed: p.voiceSpeed || 1.05,
      voicePitch: p.voicePitch || 0.85,
      sttModel: p.sttModel || 'base'
    });
    setEngineInfo(speechService.getEngineInfo());
    sounds.enabled = (memoryService.getPreferences().soundEnabled !== false);
  }, []);

  // Initial greeting once on mount
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

  // Escuta contínua ativada automaticamente (preferência "autoListen")
  useEffect(() => {
    const shouldAutoListen = (memoryService.getPreferences() || {}).autoListen !== false;
    if (!shouldAutoListen) return undefined;
    const timer = setTimeout(() => {
      handleToggleMic();
    }, 4500);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Autoteste ponta-a-ponta (J.A.R.V.I.S com ?smoke=1) — valida voz, microfone e controle do PC
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    if (!params.has('smoke')) return undefined;
    const autoclose = params.has('autoclose');
    const timer = setTimeout(async () => {
      try {
        const res = await runSmokeTest();
        if (autoclose) {
          setTimeout(() => { try { window.close(); } catch { /* ignora */ } }, 1500);
        }
        return res;
      } catch (err) {
        console.error('[SMOKE] erro no autoteste:', err);
        if (autoclose) setTimeout(() => { try { window.close(); } catch { /* ignora */ } }, 1500);
      }
      return null;
    }, 6500); // aguarda saudação, worker Whisper (4s) e índice de apps (6s)
    return () => clearTimeout(timer);
  }, []);

  // Abre um painel específico via URL (?modal=settings / ?modal=music) — usado pelo autoteste
  useEffect(() => {
    const modal = new URLSearchParams(window.location.search).get('modal');
    if (!modal) return undefined;
    const timer = setTimeout(() => setActiveModal(modal), 6000);
    return () => clearTimeout(timer);
  }, []);

  // Handle Voice Input
  const handleToggleMic = () => {
    speechService.toggleListening(
      (result) => {
        if (result.interim) {
          setInterimTranscript(result.interim);
        }
        if (result.final) {
          setInterimTranscript('');
          handleExecuteCommand(result.final);
        }
      },
      (status) => {
        setIsListening(!!status.isListening);
        setMicPhase(status.phase || (status.isListening ? 'listening' : 'idle'));
        setEngineInfo(speechService.getEngineInfo());
        if (status.phase === 'recording') setInterimTranscript('');
        if (status.error && status.message) {
          setLastDirective(status.message);
        }
      }
    );
  };

  // Central Command Execution Pipeline
  const handleExecuteCommand = async (commandText) => {
    if (!commandText.trim()) return;

    setLastDirective(commandText);
    sounds.playClick();

    // Parse command
    const res = commandExecutor.parseAndExecute(commandText);

    // 1. Game / Hibernation Mode
    if (res.action === 'TOGGLE_GAME_MODE') {
      setGameModeActive(res.active);
      speakJarvis(res.speech);
      return;
    }

    // 2. Power Actions (Windows Integration / Simulation)
    if (res.action === 'SYSTEM_POWER') {
      speakJarvis(res.speech);
      if (window.electronAPI && window.electronAPI.systemPower) {
        window.electronAPI.systemPower(res.type);
      }
      return;
    }

    // 2.1 Autostart with Windows Integration
    if (res.action === 'AUTOSTART_TOGGLE') {
      speakJarvis(res.speech);
      if (window.electronAPI && window.electronAPI.setAutostart) {
        window.electronAPI.setAutostart(res.enable);
      }
      return;
    }

    // 3. Music Actions
    if (res.action?.startsWith('MUSIC_')) {
      // Busca por voz preenche o box do YouTube Music (sem abrir janela extra)
      if (res.action === 'MUSIC_SEARCH_YT') {
        setYtMusicSearch({ q: res.query, ts: Date.now() });
      }
      speakJarvis(res.speech);
      return;
    }

    // 4. Memory Recall
    if (res.action === 'MEMORY_RECALL') {
      speakJarvis(res.speech);
      setActiveModal('memory');
      return;
    }

    // 5. Automation Sequence (e.g. GitHub Repo / VS Code via Brave/Browser)
    if (res.action === 'RUN_AUTOMATION') {
      speakJarvis(res.speech);
      setActiveModal('automation');

      // Check if Electron native automation is available
      if (window.electronAPI && window.electronAPI.runAutomation) {
        window.electronAPI.runAutomation(res.automationType, res.params);
      }

      await commandExecutor.runAutomationSequence(
        res.automationType,
        res.params,
        (step) => setAutomationState(step)
      );
      return;
    }

    // 6. Tactical Actions
    if (['SCAN_ALL', 'DIVERT_POWER', 'SENTINEL_PROTOCOL'].includes(res.action)) {
      speakJarvis(res.speech);
      return;
    }

    // 6.5 Painéis internos (agenda, memória, configurações, mídia)
    if (res.action === 'OPEN_MODAL') {
      speakJarvis(res.speech);
      setActiveModal(res.modal);
      return;
    }
    if (res.action === 'CLOSE_MODAL') {
      speakJarvis(res.speech);
      setActiveModal(null);
      return;
    }

    // 6.6 Controle de aplicativos, documentos e projetos do PC
    if (res.action === 'OPEN_RESOURCE' || res.action === 'CLOSE_RESOURCE') {
      await handlePcControl(res);
      return;
    }

    // 7. Agenda
    if (res.action === 'OPEN_AGENDA') {
      speakJarvis(res.speech);
      setActiveModal('agenda');
      return;
    }

    // 8. General AI Query (Nuvem gratuita / Cérebro local)
    if (res.action === 'QUERY_AI') {
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

  const speakJarvis = (text) => {
    speechService.speak(
      text,
      () => setIsSpeaking(true),
      () => setIsSpeaking(false)
    );
  };

  // Busca e abre/encerra recursos reais do computador (apps, docs e projetos)
  const handlePcControl = async (res) => {
    const isClose = res.action === 'CLOSE_RESOURCE';

    if (!window.electronAPI?.pcControl) {
      const msg = 'Senhor, o controle de arquivos e aplicativos só está disponível na versão desktop do J.A.R.V.I.S.';
      setLastDirective(msg);
      speakJarvis(msg);
      return;
    }

    setLastDirective(res.speech);
    speakJarvis(res.speech);

    try {
      const action = isClose ? 'close' : (res.mode === 'search' ? 'find' : 'open');
      const result = await window.electronAPI.pcControl(action, res.query);

      if (!result || result.error) {
        const msg = `Senhor, o subsistema de busca do computador retornou uma falha (${result?.error || 'desconhecida'}).`;
        setLastDirective(msg);
        speakJarvis(msg);
        return;
      }

      let message;
      if (isClose) {
        message = result.found
          ? `${result.name} foi encerrado, Senhor.`
          : `Não encontrei nenhum processo "${res.query}" em execução, Senhor.`;
      } else if (!result.found) {
        message = `Senhor, pesquisei por "${res.query}" nos seus aplicativos, documentos e projetos e não localizei nada. Posso verificar outro nome?`;
      } else if (action === 'find') {
        message = `Encontrei ${result.name}, Senhor. ${result.description || ''}${result.path ? ` Localização: ${result.path}.` : ''}`;
      } else if (result.kind === 'running') {
        message = `${result.name} já estava em execução, Senhor. Coloquei a janela em foco.`;
      } else if (result.opened) {
        message = `Abrindo ${result.name} agora, Senhor. ${result.description || ''}`;
      } else {
        message = `Encontrei ${result.name}, mas o Windows recusou a abertura, Senhor. Verifiquei em ${result.path || 'caminho desconhecido'}.`;
      }

      setLastDirective(message);
      sounds.playSuccess?.();
      speakJarvis(message);
    } catch (err) {
      const msg = `Senhor, falha ao controlar o computador: ${err?.message || 'erro desconhecido'}.`;
      setLastDirective(msg);
      speakJarvis(msg);
    }
  };

  const handleToggleSound = () => {
    const next = !soundEnabled;
    setSoundEnabled(next);
    sounds.enabled = next;
    memoryService.updatePreferences({ soundEnabled: next });
  };

  const handleThemeChange = (t) => {
    setActiveTheme(t);
    memoryService.updatePreferences({ theme: t });
  };

  const [reactorModel, setReactorModel] = useState(() => memoryService.getPreferences().reactorModel || 'mark_3');

  return (
    <div className="relative min-h-screen bg-[#02060f] text-[#b8e2f2] p-2 sm:p-4 flex flex-col justify-between overflow-x-hidden selection:bg-cyan-500/30 selection:text-cyan-200">
      {/* Background Ambience Overlays */}
      <div className="fixed inset-0 pointer-events-none cyber-grid z-0 opacity-50" />
      <div className="fixed inset-0 pointer-events-none bg-gradient-to-t from-[#010611] via-transparent to-[#020b18]/80 z-0" />

      {/* Main Container */}
      <div className="relative z-10 flex flex-col min-h-[96vh] space-y-3">
        {/* Top Header Bar */}
        <Header
          onOpenModal={(modal) => setActiveModal(modal)}
          activeTheme={activeTheme}
          onThemeChange={handleThemeChange}
          userName={userName}
          accessLevel={accessLevel}
          soundEnabled={soundEnabled}
          onToggleSound={handleToggleSound}
          onToggleGameMode={() => setGameModeActive(true)}
        />

        {/* Workspace 3-Column Holographic Grid
            <768px: coluna única | 768–1279px: 2 colunas (laterais em cima, reator embaixo)
            ≥1280px: 3 colunas clássicas */}
        <main className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-12 gap-3 flex-1">
          {/* Left Column: Agenda/Calendário + Telemetria Real de PC */}
          <aside className="md:col-span-1 xl:col-span-3 flex flex-col space-y-3 min-w-0">
            <ResizableBox id="biometrics"><Biometrics /></ResizableBox>
            <ResizableBox id="diagnostics"><Diagnostics /></ResizableBox>
          </aside>

          {/* Center Orbital Core & Voice Visualizer */}
          <section className="md:col-span-2 xl:col-span-6 md:order-last xl:order-none flex flex-col justify-between hud-panel rounded-lg p-3 sm:p-5 overflow-hidden border-[var(--panel-border)] min-h-[480px] min-w-0">
            <div className="corner-bracket corner-tl"></div>
            <div className="corner-bracket corner-tr"></div>
            <div className="corner-bracket corner-bl"></div>
            <div className="corner-bracket corner-br"></div>

            <ArcReactor
              isSpeaking={isSpeaking}
              isListening={isListening}
              model={reactorModel}
              onModelChange={(m) => {
                setReactorModel(m);
                memoryService.updatePreferences({ reactorModel: m });
              }}
            />

            <AudioVisualizer
              isListening={isListening}
              isSpeaking={isSpeaking}
              onToggleMic={handleToggleMic}
              lastDirective={lastDirective}
              interimTranscript={interimTranscript}
              micPhase={micPhase}
              engineInfo={engineInfo}
            />
          </section>

          {/* Right Schematics & Satellite Column */}
          <aside className="md:col-span-1 xl:col-span-3 flex flex-col space-y-3 min-w-0">
            <ResizableBox id="schematic"><SchematicViewer /></ResizableBox>
            <ResizableBox id="musicbox"><MusicBox search={ytMusicSearch} /></ResizableBox>
          </aside>
        </main>

        {/* Bottom Tactical Console */}
        <TerminalInput
          isListening={isListening}
          onToggleMic={handleToggleMic}
        />
      </div>

      {/* Holographic Modals */}
      <AgendaModal
        isOpen={activeModal === 'agenda'}
        onClose={() => setActiveModal(null)}
      />

      <MemoryModal
        isOpen={activeModal === 'memory'}
        onClose={() => setActiveModal(null)}
      />

      <MusicWidgetModal
        isOpen={activeModal === 'music'}
        onClose={() => setActiveModal(null)}
      />

      <SettingsModal
        isOpen={activeModal === 'settings'}
        onClose={() => setActiveModal(null)}
        activeTheme={activeTheme}
        onThemeChange={handleThemeChange}
      />

      <AutomationModal
        isOpen={activeModal === 'automation'}
        onClose={() => setActiveModal(null)}
        automationState={automationState}
      />

      {/* Game Mode / Hibernation Overlay */}
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

