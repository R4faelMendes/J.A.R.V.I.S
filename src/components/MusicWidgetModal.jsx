import React, { useState } from 'react';
import { X, Music, Play, Pause, SkipForward, SkipBack, Volume2, Disc, Radio, Globe } from 'lucide-react';
import { sounds } from '../services/soundEffects';

const IS_ELECTRON = typeof window !== 'undefined' && !!window.electronAPI?.isElectron;
const CHROME_UA = (typeof window !== 'undefined' && window.electronAPI?.chromeUserAgent) || '';

export default function MusicWidgetModal({ isOpen, onClose }) {
  const [isPlayingTheme, setIsPlayingTheme] = useState(false);
  const [activeTab, setActiveTab] = useState('ytmusic'); // 'ytmusic' ou 'jarvis_theme'
  const [ytUrl, setYtUrl] = useState('https://music.youtube.com');

  if (!isOpen) return null;

  const handleToggleThemeSong = () => {
    if (isPlayingTheme) {
      sounds.stopThemeSong();
      setIsPlayingTheme(false);
    } else {
      sounds.playThemeSong();
      setIsPlayingTheme(true);
    }
  };

  const handleMediaKey = (action) => {
    sounds.playClick();
    if (window.electronAPI?.mediaControl) {
      window.electronAPI.mediaControl(action);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-2 sm:p-4 bg-black/85 backdrop-blur-md animate-in fade-in duration-200">
      <div className="hud-panel rounded-xl w-full max-w-4xl h-[85vh] flex flex-col border-[var(--primary)] shadow-[0_0_45px_var(--primary-glow)] overflow-hidden">
        <div className="corner-bracket corner-tl"></div>
        <div className="corner-bracket corner-tr"></div>
        <div className="corner-bracket corner-bl"></div>
        <div className="corner-bracket corner-br"></div>

        {/* Header */}
        <div className="flex items-center justify-between px-4 sm:px-6 py-3 border-b border-cyan-500/20 bg-slate-950/80">
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded bg-cyan-950/60 border border-[var(--primary)] text-[var(--primary)]">
              <Music className="w-5 h-5" />
            </div>
            <div>
              <h2 className="font-['Orbitron'] text-base sm:text-lg font-bold text-[var(--primary)] glow-cyan tracking-wider">
                YOUTUBE MUSIC & CENTRAL DE ÁUDIO
              </h2>
              <p className="text-xs text-cyan-400/80 font-['JetBrains_Mono']">
                JANELA INTERATIVA EMBUTIDA NO J.A.R.V.I.S.
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            {/* Tabs */}
            <div className="flex bg-black/60 border border-cyan-500/30 rounded p-0.5 text-xs font-['JetBrains_Mono']">
              <button
                onClick={() => { sounds.playClick(); setActiveTab('ytmusic'); }}
                className={`px-3 py-1 rounded transition-all ${
                  activeTab === 'ytmusic'
                    ? 'bg-[var(--primary)] text-black font-bold shadow-[0_0_8px_var(--primary)]'
                    : 'text-cyan-400 hover:text-white'
                }`}
              >
                YouTube Music
              </button>
              <button
                onClick={() => { sounds.playClick(); setActiveTab('jarvis_theme'); }}
                className={`px-3 py-1 rounded transition-all ${
                  activeTab === 'jarvis_theme'
                    ? 'bg-amber-500 text-black font-bold shadow-[0_0_8px_#f59e0b]'
                    : 'text-amber-400 hover:text-white'
                }`}
              >
                Tema Stark/Jarvis
              </button>
            </div>

            {/* Player embutido: música dentro do app, sem janela extra */}

            <button
              onClick={() => {
                sounds.playClick();
                sounds.stopThemeSong();
                setIsPlayingTheme(false);
                onClose();
              }}
              className="p-1.5 rounded-lg border border-red-500/30 text-red-400 hover:bg-red-500 hover:text-black transition-all"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Content Area */}
        <div className="flex-1 flex flex-col overflow-hidden bg-black/60 relative">
          {activeTab === 'ytmusic' ? (
            <div className="flex-1 flex flex-col h-full">
              {/* Toolbar */}
              <div className="px-4 py-2 border-b border-cyan-500/20 bg-slate-950/60 flex items-center justify-between font-['JetBrains_Mono'] text-xs">
                <div className="flex items-center space-x-3">
                  <button
                    onClick={() => handleMediaKey('PREV')}
                    className="p-1.5 rounded bg-cyan-950/60 hover:bg-cyan-900 border border-cyan-500/30 text-cyan-300"
                    title="Música Anterior"
                  >
                    <SkipBack className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => handleMediaKey('PLAY_PAUSE')}
                    className="px-3 py-1.5 rounded bg-[var(--primary)] text-black font-bold hover:bg-white transition-all flex items-center gap-1.5"
                    title="Play / Pause Global"
                  >
                    <Play className="w-4 h-4" /> Play / Pause
                  </button>
                  <button
                    onClick={() => handleMediaKey('NEXT')}
                    className="p-1.5 rounded bg-cyan-950/60 hover:bg-cyan-900 border border-cyan-500/30 text-cyan-300"
                    title="Próxima Música"
                  >
                    <SkipForward className="w-4 h-4" />
                  </button>
                </div>

                <div className="flex items-center space-x-2">
                  <div className="text-[11px] text-cyan-400/80 flex items-center gap-1">
                    <Globe className="w-3.5 h-3.5 text-[var(--primary)]" />
                    <span>music.youtube.com — embutido, sem janela extra</span>
                  </div>
                </div>
              </div>

              {/* YouTube Music Embedded Interactive Window */}
              <div className="flex-1 relative w-full h-full bg-slate-950 flex flex-col items-center justify-center">
                {IS_ELECTRON ? (
                  // <webview> permite o login da conta Google/YouTube (iframe dava erro 401)
                  <webview
                    src="https://music.youtube.com"
                    useragent={CHROME_UA}
                    allowpopups="true"
                    className="border-0 absolute inset-0 z-10"
                    style={{ width: '100%', height: '100%', display: 'block' }}
                    title="YouTube Music Interactive Player"
                  />
                ) : (
                  <iframe
                    src="https://music.youtube.com"
                    title="YouTube Music Interactive Player"
                    className="w-full h-full border-0 absolute inset-0 z-10"
                    allow="autoplay; encrypted-media; clipboard-write; microphone"
                    sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals"
                  />
                )}
              </div>
            </div>
          ) : (
            /* Jarvis Cinematic Theme Generator */
            <div className="flex-1 flex flex-col items-center justify-center p-8 space-y-6 text-center">
              <div className="relative w-32 h-32 rounded-full border-4 border-amber-500/50 flex items-center justify-center shadow-[0_0_35px_rgba(245,158,11,0.5)]">
                <Disc className={`w-20 h-20 text-amber-400 ${isPlayingTheme ? 'animate-spin-slow' : ''}`} />
              </div>

              <div>
                <h3 className="font-['Orbitron'] text-xl font-bold text-amber-300 glow-amber">
                  TRILHA SONORA CINEMÁTICA // STARK SUITE
                </h3>
                <p className="text-xs text-amber-200/70 font-['JetBrains_Mono'] mt-1 max-w-md">
                  Sintetizador Web Audio nativo reproduzindo progressão orquestral sci-fi de baixa latência em tempo real.
                </p>
              </div>

              <button
                onClick={handleToggleThemeSong}
                className={`px-8 py-3 rounded-xl font-['Orbitron'] font-bold text-sm uppercase transition-all shadow-lg flex items-center gap-3 border ${
                  isPlayingTheme
                    ? 'bg-rose-500 hover:bg-rose-600 text-white border-rose-400 shadow-[0_0_20px_rgba(244,63,94,0.6)]'
                    : 'bg-amber-500 hover:bg-amber-400 text-black border-amber-300 shadow-[0_0_20px_rgba(245,158,11,0.6)]'
                }`}
              >
                {isPlayingTheme ? (
                  <>
                    <Pause className="w-5 h-5" /> Pausar Trilha Temática
                  </>
                ) : (
                  <>
                    <Play className="w-5 h-5" /> Tocar Música do J.A.R.V.I.S.
                  </>
                )}
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}