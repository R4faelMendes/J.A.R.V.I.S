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
import RulesModal from './components/RulesModal';
import GameModeOverlay from './components/GameModeOverlay';

import { speechService } from './services/speechService';
import { sounds } from './services/soundEffects';
import { commandExecutor } from './services/commandExecutor';
import { aiService } from './services/aiService';
import { memoryService } from './services/memoryService';
import { ruleEngine } from './services/ruleEngine';
import { biometricService, SENSITIVE_ACTIONS } from './services/biometricService';
import { visionService } from './services/visionService';
import { runSmokeTest } from './services/selfTest';

export default function App() {
  const [activeTheme, setActiveTheme] = useState(() => {
    return memoryService.getPreferences().theme || 'cyan';
  });
  const [soundEnabled, setSoundEnabled] = useState(true);
  const [isListening, setIsListening] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [interimTranscript, setInterimTranscript] = useState('');
  const [lastDirective, setLastDirective] = useState(
    'Pronto, Senhor. O organismo despertou e todos os filamentos estão sincronizados.'
  );

  // Novo layout estilo Codex: sidebar + área escura
  const [activeView, setActiveViewRaw] = useState('home'); // home, agenda, music, memory, automation, tactical, schematics, settings
  // Ao abrir a view de música ela fica montada para sempre: o X só a OCULTA —
  // o iframe continua vivo, o áudio não para e a fila/busca são preservadas
  const [musicOpened, setMusicOpened] = useState(false);
  const setActiveView = (view) => {
    if (view === 'music') setMusicOpened(true);
    setActiveViewRaw(view);
  };
  const [automationState, setAutomationState] = useState(null);
  const [gameModeActive, setGameModeActive] = useState(false);
  const [reactorModel, setReactorModel] = useState('mark_3');
  const [textCommand, setTextCommand] = useState('');
  const [ytSearch, setYtSearch] = useState('');
  const [localMode, setLocalMode] = useState(false); // glow violeta quando o cérebro local responde

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
        `J.A.R.V.I.S. vivo. Conexão neural estabelecida, Senhor. O organismo respira em ritmo basal.`,
        () => setIsSpeaking(true),
        () => setIsSpeaking(false)
      );
    }, 1000);
    return () => clearTimeout(timer);
  }, []);

  // Fase 1 — organismo proativo: reflexos (regras) + vigília visual (opt-in)
  useEffect(() => {
    ruleEngine.start((rule, context) => {
      if (rule.action?.kind === 'command' && rule.action.command) {
        handleExecuteCommand(rule.action.command);
      } else {
        const text = rule.action?.text || `Reflexo disparado: ${rule.name}, Senhor.`;
        setLastDirective(text);
        speakJarvis(text);
      }
      void context;
    });
    visionService.start((hint) => {
      const text = `Uma observação, Senhor: ${hint}.`;
      setLastDirective(text);
    });
    return () => { ruleEngine.stop(); visionService.stop(); };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Reage a mudanças de consentimento vindas dos ajustes (sem reload)
  useEffect(() => {
    if (activeView === 'settings') return undefined;
    visionService.start((hint) => setLastDirective(`Uma observação, Senhor: ${hint}.`));
    return undefined;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeView]);

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

  // Pipeline central de comandos — com trava biométrica em ações sensíveis
  const handleExecuteCommand = async (commandText) => {
    if (!commandText.trim()) return;

    setLastDirective(commandText);
    sounds.playClick();

    const res = commandExecutor.parseAndExecute(commandText);

    // Trava biométrica: energia do sistema e encerramento de processos exigem
    // desbloqueio (Windows Hello no desktop; confirmação explícita fora dele)
    if (SENSITIVE_ACTIONS.has(res.action) || res.action === 'PROCESS_KILL') {
      const label = res.action === 'SYSTEM_POWER'
        ? `ação de energia (${res.type})`
        : `encerramento de processo (${res.query || res.speech || 'selecionado'})`;
      setLastDirective('Verificando identidade do operador…');
      const unlock = await biometricService.unlock(label);
      if (!unlock.ok) {
        const msg = 'Identidade não confirmada, Senhor. Ação sensível bloqueada.';
        setLastDirective(msg);
        speakJarvis(msg);
        return;
      }
    }

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
        setLocalMode(aiService.isLocalMode());
        speakJarvis(aiAnswer);
      } catch {
        const fallback = 'Senhor, identifiquei sua solicitação, mas os servidores de IA remotos apresentaram latência. Módulos locais permanecem ativos.';
        setLastDirective(fallback);
        setLocalMode(true);
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
        // Renderizada fixa, logo abaixo do switch: montada uma 1ª vez e só
        // ocultada ao fechar — assim o áudio continua e nada reinicia
        return null;
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
          <div className="syn-panel rounded-xl p-6 max-w-2xl mx-auto font-syn-body">




            <h2 className="font-syn-title text-lg font-bold mb-2" style={{ color: '#eaf8f4' }}>
              Automação em repouso
            </h2>
            <p className="font-syn-body text-xs font-light mb-4" style={{ color: '#9aa6c9' }}>
              Nenhuma sequência em execução. Inicie por voz ou pelos atalhos abaixo, Senhor.
            </p>
            <div className="flex flex-wrap gap-2">
              <button
                onClick={() => handleExecuteCommand('abrir vscode')}
                className="syn-pill font-syn-title px-4 py-2 text-xs font-medium"
                style={{ background: 'rgba(45,253,212,0.18)', color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.45)', boxShadow: '0 0 14px rgba(45,253,212,0.3)' }}
              >
                Abrir VS Code
              </button>
              <button
                onClick={() => runAutomation('CREATE_GITHUB_REPO', { repoName: 'temperatura-mobile' }, 'Iniciando sequência de criação de repositório no GitHub, Senhor.')}
                className="syn-pill font-syn-title px-4 py-2 text-xs font-medium"
                style={{ color: '#eaf8f4', border: '1px solid rgba(138,92,255,0.45)', background: 'rgba(138,92,255,0.12)' }}
              >
                Criar repo GitHub
              </button>
            </div>
          </div>
        );
      case 'rules':
        return (
          <div className="h-full min-h-0 flex flex-col">
            <RulesModal isOpen embedded onClose={goHome} />
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
            <div className="syn-panel p-4 relative overflow-hidden">
              <div className="font-syn-body text-[11px] font-light mb-1" style={{ color: '#9aa6c9' }}>
                Última diretiva
              </div>
              <p className="font-syn-body text-sm italic font-light" style={{ color: '#eaf8f4' }}>
                &ldquo;{lastDirective}&rdquo;
              </p>
              <div className="mt-3 flex flex-wrap gap-2 font-syn-body text-[11px]">
                <span className="syn-pill px-2.5 py-1" style={{ border: '1px solid rgba(45,253,212,0.25)', color: '#9aa6c9' }}>
                  {pending} tarefas pendentes
                </span>
                <span className="syn-pill px-2.5 py-1" style={{ border: '1px solid rgba(138,92,255,0.35)', color: '#9aa6c9' }}>
                  {projects.length} projetos
                </span>
                <span className="syn-pill px-2.5 py-1" style={isListening
                  ? { border: '1px solid rgba(45,253,212,0.5)', color: '#2dfdd4', background: 'rgba(45,253,212,0.1)' }
                  : isSpeaking
                    ? { border: '1px solid rgba(255,62,200,0.5)', color: '#ff3ec8', background: 'rgba(255,62,200,0.1)' }
                    : { border: '1px solid rgba(154,166,201,0.3)', color: '#9aa6c9' }}>
                  {isListening ? 'escutando' : isSpeaking ? 'transmitindo' : 'pronto'}
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
              className="syn-panel p-2.5 pl-4 flex items-center gap-2"
              style={{ borderRadius: '999px' }}
            >
              <span className="font-syn-body text-sm font-medium pl-1" style={{ color: '#2dfdd4' }}>›</span>
              <input
                value={textCommand}
                onChange={(e) => setTextCommand(e.target.value)}
                placeholder="Sussurre uma diretiva ou fale ao microfone…"
                className="flex-1 bg-transparent outline-none font-syn-body font-light text-sm"
                style={{ color: '#eaf8f4' }}
              />
              <button
                type="submit"
                className="syn-pill font-syn-title px-5 py-2 text-xs font-medium"
                style={{ background: 'rgba(45,253,212,0.18)', color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.45)', boxShadow: '0 0 14px rgba(45,253,212,0.3)' }}
              >
                Enviar
              </button>
            </form>

            <div className="flex flex-wrap gap-2 font-syn-body text-[11px]">
              {[
                { label: 'Varredura completa', cmd: 'varredura completa dos sistemas' },
                { label: 'Divertir energia', cmd: 'redirecionar energia para os propulsores' },
                { label: 'Protocolo sentinela', cmd: 'ativar protocolo sentinela' },
                { label: 'Abrir agenda', cmd: 'abrir agenda' },
              ].map((a) => (
                <button
                  key={a.label}
                  onClick={() => handleExecuteCommand(a.cmd)}
                  className="syn-pill px-3.5 py-1.5 font-light"
                  style={{ border: '1px solid rgba(45,253,212,0.25)', color: '#9aa6c9', background: 'rgba(10,6,32,0.5)' }}
                >
                  {a.label}
                </button>
              ))}
            </div>

            <TerminalInput isListening={isListening} isSpeaking={isSpeaking} onToggleMic={handleToggleMic} />
          </div>
        );
      }
    }
  };

  return (
    <div className="flex h-screen w-screen overflow-hidden font-syn-body" style={{ background: '#050311', color: '#eaf8f4' }}>
      {/* Sidebar — núcleo synapse + navegação */}
      <Sidebar
        activeView={activeView}
        onSelect={(v) => setActiveView(v)}
        isSpeaking={isSpeaking}
        isListening={isListening}
        reactorModel={reactorModel}
        onModelChange={(m) => setReactorModel(m)}
        userName={userName}
        accessLevel={accessLevel}
        localMode={localMode}
      />

      {/* Área viva — mini header + conteúdo selecionado */}
      <div className="flex-1 flex flex-col min-w-0" style={{ background: 'radial-gradient(120% 90% at 50% 0%, #0a0620 0%, #050311 70%)' }}>
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
          localMode={localMode}
        />
        <main className="flex-1 flex flex-col min-h-0 p-4">
          {renderView()}
          {/* YT Music mantido MONTADO (só oculto quando fechado): o áudio
              continua tocando e o estado do player é preservado ao voltar */}
          <div className={activeView === 'music' ? 'flex-1 min-h-0 flex flex-col h-full' : 'hidden'}>
            {(musicOpened || activeView === 'music') && (
              <MusicWidgetModal isOpen embedded onClose={goHome} searchQuery={ytSearch} />
            )}
          </div>
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
