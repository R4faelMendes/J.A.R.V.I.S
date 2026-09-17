import React, { useState, useEffect } from 'react';
import { Shield, Cpu, Lock, Calendar, Database, Music, Settings, Gamepad2, Volume2, VolumeX, Sparkles } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function Header({ 
  onOpenModal, 
  activeTheme, 
  onThemeChange, 
  userName = 'Tony Stark',
  accessLevel = 'NÍVEL ALPHA',
  soundEnabled,
  onToggleSound,
  onToggleGameMode
}) {
  const [timeUtc, setTimeUtc] = useState('');
  const [timeLocal, setTimeLocal] = useState('');

  useEffect(() => {
    const update = () => {
      const now = new Date();
      const hours = String(now.getUTCHours()).padStart(2, '0');
      const minutes = String(now.getUTCMinutes()).padStart(2, '0');
      const seconds = String(now.getUTCSeconds()).padStart(2, '0');
      setTimeUtc(`${hours}:${minutes}:${seconds} UTC`);
      setTimeLocal(now.toLocaleTimeString('pt-BR'));
    };
    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, []);

  const themes = [
    { id: 'cyan', name: 'Cyan Stark', color: 'bg-cyan-400' },
    { id: 'amber', name: 'Mark III Gold', color: 'bg-amber-400' },
    { id: 'crimson', name: 'Hulkbuster Red', color: 'bg-rose-500' },
    { id: 'emerald', name: 'Time Stone', color: 'bg-emerald-400' },
    { id: 'purple', name: 'Power Stone', color: 'bg-purple-400' }
  ];

  return (
    <header className="hud-panel rounded-lg px-4 py-2.5 flex flex-wrap items-center justify-between gap-3 border-[var(--panel-border)] relative z-20">
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Brand & Logo */}
      <div className="flex items-center space-x-3 cursor-pointer" onClick={() => { sounds.playPowerPulse(); }}>
        <div className="relative flex items-center justify-center w-10 h-10 rounded-full border border-[var(--primary)] bg-cyan-950/40 shadow-[0_0_14px_var(--primary-glow)] group">
          <span className="w-3 h-3 rounded-full bg-[var(--primary)] animate-ping absolute opacity-75"></span>
          <span className="w-4 h-4 rounded-full bg-[var(--primary)] shadow-[0_0_10px_var(--primary)] group-hover:scale-125 transition-transform"></span>
        </div>
        <div>
          <div className="flex items-center space-x-2">
            <h1 className="font-['Orbitron'] font-black tracking-widest text-[var(--primary)] text-xl sm:text-2xl glow-cyan">
              J.A.R.V.I.S.
            </h1>
            <span className="text-[10px] uppercase font-['JetBrains_Mono'] px-1.5 py-0.5 rounded bg-cyan-950/80 border border-[var(--primary)] text-cyan-200 shadow-sm">
              OS v4.8.2
            </span>
          </div>
          <p className="text-[11px] text-[var(--primary)] opacity-80 font-['JetBrains_Mono'] tracking-wider hidden sm:block">
            SISTEMA DE INTELIGÊNCIA TÁTICA AVANÇADA // STARK IND.
          </p>
        </div>
      </div>

      {/* Operational Center Diagnostics */}
      <div className="hidden xl:flex items-center space-x-6 text-xs font-['JetBrains_Mono']">
        <div className="flex items-center space-x-2">
          <span className="text-cyan-400/70">DIAGNÓSTICO:</span>
          <span className="text-emerald-400 font-bold tracking-wider flex items-center gap-1.5 bg-emerald-950/30 px-2 py-0.5 rounded border border-emerald-500/30">
            <span className="w-2 h-2 rounded-full bg-emerald-400 inline-block animate-pulse"></span>
            NOMINAL (100%)
          </span>
        </div>
        <div className="h-4 w-px bg-cyan-500/30"></div>
        <div className="flex items-center space-x-2">
          <span className="text-cyan-400/70">PROTOCOLO:</span>
          <span className="text-[var(--secondary)] glow-amber font-bold tracking-wider uppercase flex items-center gap-1">
            <Shield className="w-3.5 h-3.5" /> VANGUARDA ATIVA
          </span>
        </div>
        <div className="h-4 w-px bg-cyan-500/30"></div>
        <div className="flex items-center space-x-2">
          <span className="text-cyan-400/70">CRIPTOGRAFIA QUÂNTICA:</span>
          <span className="text-cyan-200 flex items-center gap-1">
            <Lock className="w-3 h-3 text-[var(--primary)]" /> 2048-QBIT ENGAGED
          </span>
        </div>
      </div>

      {/* Navigation Quick Tools & Theme Switcher */}
      <div className="flex items-center space-x-3">
        {/* Modals Quick Access Toolbar */}
        <div className="flex items-center bg-black/40 border border-[var(--panel-border)] rounded-md p-1 gap-1">
          <button
            onClick={() => { sounds.playClick(); onOpenModal('memory'); }}
            title="Matriz de Memória de Longo Prazo"
            className="p-1.5 rounded hover:bg-[var(--primary-dim)] text-[var(--primary)] hover:text-white transition-all flex items-center gap-1 text-xs font-mono"
          >
            <Database className="w-4 h-4" />
            <span className="hidden md:inline text-[11px]">Memória</span>
          </button>

          <button
            onClick={() => { sounds.playClick(); onOpenModal('music'); }}
            title="Controle de Áudio & Mídia"
            className="p-1.5 rounded hover:bg-[var(--primary-dim)] text-[var(--primary)] hover:text-white transition-all flex items-center gap-1 text-xs font-mono"
          >
            <Music className="w-4 h-4" />
            <span className="hidden md:inline text-[11px]">Mídia</span>
          </button>

          <button
            onClick={() => { sounds.playPowerPulse(); onToggleGameMode(); }}
            title="Modo Hibernação / Game Mode (Liberar RAM & GPU)"
            className="p-1.5 rounded hover:bg-amber-500/20 text-amber-400 hover:text-amber-200 transition-all flex items-center gap-1 text-xs font-mono"
          >
            <Gamepad2 className="w-4 h-4" />
            <span className="hidden lg:inline text-[11px]">Game Mode</span>
          </button>

          <button
            onClick={() => { sounds.playClick(); onOpenModal('settings'); }}
            title="Configurações & Chaves de IA"
            className="p-1.5 rounded hover:bg-[var(--primary-dim)] text-[var(--primary)] hover:text-white transition-all"
          >
            <Settings className="w-4 h-4" />
          </button>

          <button
            onClick={() => { onToggleSound(); sounds.playClick(); }}
            title={soundEnabled ? 'Silenciar Efeitos de Áudio' : 'Ativar Efeitos de Áudio'}
            className="p-1.5 rounded hover:bg-[var(--primary-dim)] text-cyan-300 transition-all"
          >
            {soundEnabled ? <Volume2 className="w-4 h-4 text-emerald-400" /> : <VolumeX className="w-4 h-4 text-red-400" />}
          </button>
        </div>

        {/* Theme Palette Dots */}
        <div className="hidden sm:flex items-center space-x-1 bg-black/40 border border-[var(--panel-border)] rounded-md px-1.5 py-1">
          {themes.map(t => (
            <button
              key={t.id}
              onClick={() => { sounds.playClick(); onThemeChange(t.id); }}
              title={`Tema: ${t.name}`}
              className={`w-3.5 h-3.5 rounded-full ${t.color} transition-transform hover:scale-125 ${
                activeTheme === t.id ? 'ring-2 ring-white scale-110 shadow-[0_0_8px_white]' : 'opacity-60 hover:opacity-100'
              }`}
            />
          ))}
        </div>

        {/* User Info & Live Clock */}
        <div className="flex items-center space-x-3 pl-2 border-l border-cyan-500/20">
          <div className="text-right font-['JetBrains_Mono']">
            <div className="text-sm sm:text-base font-bold text-[var(--primary)] tracking-wider glow-cyan">
              {timeLocal}
            </div>
            <div className="text-[10px] text-cyan-400/80 font-semibold tracking-tight uppercase">
              HORÁRIO DE BRASÍLIA // GMT-3
            </div>
          </div>
          <div 
            onClick={() => { sounds.playAlert(); }}
            className="w-8 h-8 rounded border border-[var(--secondary)] bg-amber-950/40 flex items-center justify-center text-[var(--secondary)] shadow-[0_0_10px_var(--secondary-glow)] cursor-pointer hover:bg-amber-900/60 transition-colors"
          >
            <Lock className="w-4 h-4" />
          </div>
        </div>
      </div>
    </header>
  );
}