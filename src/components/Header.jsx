import React, { useState, useEffect } from 'react';
import { Database, Music, Settings, Gamepad2, Volume2, VolumeX } from 'lucide-react';
import { sounds } from '../services/soundEffects';

/** Header legado em linguagem SYNAPSE (pílulas, sem colchetes, sem anéis rígidos). */
export default function Header({
  onOpenModal,
  activeTheme,
  onThemeChange,
  userName = 'Tony Stark',
  accessLevel = 'Nível alpha',
  soundEnabled,
  onToggleSound,
  onToggleGameMode,
}) {
  const [timeLocal, setTimeLocal] = useState('');

  useEffect(() => {
    const update = () => setTimeLocal(new Date().toLocaleTimeString('pt-BR'));
    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, []);

  const themes = [
    { id: 'cyan', name: 'Sinapse teal', color: '#2dfdd4' },
    { id: 'amber', name: 'Âmbar suave', color: '#ffcf5c' },
    { id: 'crimson', name: 'Coral', color: '#ff5c7a' },
    { id: 'emerald', name: 'Menta', color: '#2dfdd4' },
    { id: 'purple', name: 'Violeta', color: '#8a5cff' },
  ];

  return (
    <header className="syn-panel px-4 py-2.5 flex flex-wrap items-center justify-between gap-3 relative z-20">
      <div className="flex items-center space-x-3 cursor-pointer" onClick={() => { sounds.playPowerPulse(); }}>
        <div className="relative flex items-center justify-center w-10 h-10 group">
          <span className="syn-blob absolute inset-0" style={{ background: 'radial-gradient(circle, rgba(45,253,212,0.6), rgba(138,92,255,0.3) 60%, transparent 70%)', animationDuration: '7s' }} />
          <span className="w-4 h-4 syn-core relative" style={{ borderRadius: '46% 54% 58% 42% / 48% 44% 56% 52%', background: '#2dfdd4', boxShadow: '0 0 12px rgba(45,253,212,0.7)' }} />
        </div>
        <div>
          <div className="flex items-center space-x-2">
            <h1 className="font-syn-title font-bold tracking-wide text-xl sm:text-2xl" style={{ color: '#eaf8f4', textShadow: '0 0 12px rgba(45,253,212,0.5)' }}>
              J.A.R.V.I.S.
            </h1>
            <span className="syn-pill font-syn-body text-[10px] px-2 py-0.5" style={{ border: '1px solid rgba(45,253,212,0.35)', color: '#2dfdd4' }}>
              synapse v5.0
            </span>
          </div>
          <p className="font-syn-body text-[11px] font-light hidden sm:block" style={{ color: '#9aa6c9' }}>
            Organismo neural · {userName} · {accessLevel}
          </p>
        </div>
      </div>

      <div className="hidden xl:flex items-center gap-2 font-syn-body text-xs" style={{ color: '#9aa6c9' }}>
        <span className="syn-pill px-3 py-1" style={{ border: '1px solid rgba(45,253,212,0.3)', color: '#2dfdd4' }}>tecido nominal</span>
        <span className="font-syn-body text-[11px] tabular-nums">{timeLocal}</span>
      </div>

      <div className="flex items-center gap-2">
        <div className="flex items-center syn-pill px-1.5 py-1 gap-1" style={{ background: 'rgba(10,6,32,0.6)', border: '1px solid rgba(45,253,212,0.18)' }}>
          <button onClick={() => { sounds.playClick(); onOpenModal('memory'); }} title="Memória" className="syn-pill p-1.5 flex items-center gap-1 font-syn-body text-[11px]" style={{ color: '#2dfdd4' }}>
            <Database className="w-4 h-4" /><span className="hidden md:inline">Memória</span>
          </button>
          <button onClick={() => { sounds.playClick(); onOpenModal('music'); }} title="Mídia" className="syn-pill p-1.5 flex items-center gap-1 font-syn-body text-[11px]" style={{ color: '#2dfdd4' }}>
            <Music className="w-4 h-4" /><span className="hidden md:inline">Mídia</span>
          </button>
          <button onClick={() => { sounds.playPowerPulse(); onToggleGameMode(); }} title="Hibernação" className="syn-pill p-1.5 flex items-center gap-1 font-syn-body text-[11px]" style={{ color: '#ffcf5c' }}>
            <Gamepad2 className="w-4 h-4" /><span className="hidden lg:inline">Repouso</span>
          </button>
          <button onClick={() => { sounds.playClick(); onOpenModal('settings'); }} title="Ajustes" className="syn-pill p-1.5" style={{ color: '#2dfdd4' }}>
            <Settings className="w-4 h-4" />
          </button>
          <button onClick={() => { onToggleSound(); sounds.playClick(); }} title={soundEnabled ? 'Silenciar' : 'Ativar som'} className="syn-pill p-1.5" style={{ color: '#9aa6c9' }}>
            {soundEnabled ? <Volume2 className="w-4 h-4" /> : <VolumeX className="w-4 h-4" />}
          </button>
        </div>
        <div className="hidden sm:flex items-center gap-1.5 syn-pill px-2 py-1.5" style={{ border: '1px solid rgba(45,253,212,0.18)', background: 'rgba(10,6,32,0.6)' }}>
          {themes.map((t) => (
            <button key={t.id} onClick={() => { sounds.playClick(); onThemeChange(t.id); }} title={t.name}
              className="syn-core" style={{ width: 12, height: 12, borderRadius: '50%', background: t.color, boxShadow: activeTheme === t.id ? `0 0 0 2px #eaf8f4, 0 0 8px ${t.color}` : `0 0 6px ${t.color}`, opacity: activeTheme === t.id ? 1 : 0.55 }} />
          ))}
        </div>
      </div>
    </header>
  );
}
