import React, { useState, useEffect, useRef } from 'react';
import Header from './components/Header';
import Biometrics from './components/Biometrics';
import Diagnostics from './components/Diagnostics';
import ArcReactor from './components/ArcReactor';
import AudioVisualizer from './components/AudioVisualizer';
import SchematicViewer from './components/SchematicViewer';
import TacticalMap from './components/TacticalMap';
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

  // Modals state
  const [activeModal, setActiveModal] = useState(null); // 'agenda', 'memory', 'music', 'settings', 'automation'
  const [automationState, setAutomationState] = useState(null);
  const [gameModeActive, setGameModeActive] = useState(false);

  // Load preferences
  const prefs = memoryService.getPreferences();
  const userName = prefs.userName || 'Tony Stark';
  const accessLevel = prefs.accessLevel || 'NÍVEL ALPHA';

  // Apply theme to document
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', activeTheme);
  }, [activeTheme]);

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
        setIsListening(status.isListening);
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

    // 3. Music Actions
    if (res.action?.startsWith('MUSIC_')) {
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

    // 7. Agenda
    if (res.action === 'OPEN_AGENDA') {
      speakJarvis(res.speech);
      setActiveModal('agenda');
      return;
    }

    // 8. General AI Query (Gemini / OpenAI / Local Brain)
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

  const handleToggleSound = () => {
    const next = !soundEnabled;
    setSoundEnabled(next);
    sounds.enabled = next;
  };

  const [reactorModel, setReactorModel] = useState('mark_3');

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
          onThemeChange={(t) => setActiveTheme(t)}
          userName={userName}
          accessLevel={accessLevel}
          soundEnabled={soundEnabled}
          onToggleSound={handleToggleSound}
          onToggleGameMode={() => setGameModeActive(true)}
        />

        {/* Workspace 3-Column Holographic Grid */}
        <main className="grid grid-cols-1 lg:grid-cols-12 gap-3 flex-1">
          {/* Left Column: Agenda/Calendário + Telemetria Real de PC */}
          <aside className="lg:col-span-3 flex flex-col space-y-3">
            <Biometrics />
            <Diagnostics />
          </aside>

          {/* Center Orbital Core & Voice Visualizer */}
          <section className="lg:col-span-6 flex flex-col justify-between hud-panel rounded-lg p-3 sm:p-5 overflow-hidden border-[var(--panel-border)] min-h-[480px]">
            <div className="corner-bracket corner-tl"></div>
            <div className="corner-bracket corner-tr"></div>
            <div className="corner-bracket corner-bl"></div>
            <div className="corner-bracket corner-br"></div>

            <ArcReactor
              isSpeaking={isSpeaking}
              isListening={isListening}
              model={reactorModel}
              onModelChange={(m) => setReactorModel(m)}
            />

            <AudioVisualizer
              isListening={isListening}
              isSpeaking={isSpeaking}
              onToggleMic={handleToggleMic}
              lastDirective={lastDirective}
              interimTranscript={interimTranscript}
            />
          </section>

          {/* Right Schematics & Satellite Column */}
          <aside className="lg:col-span-3 flex flex-col space-y-3">
            <SchematicViewer />
            <TacticalMap />
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
        onThemeChange={(t) => setActiveTheme(t)}
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

