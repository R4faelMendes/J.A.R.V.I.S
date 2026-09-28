import React, { useState, useEffect } from 'react';
import { Settings, Volume2, VolumeX, Gamepad2 } from 'lucide-react';
import { sounds } from '../services/soundEffects';

const VIEW_TITLES = {
  home: 'Central de voz', agenda: 'Agenda', music: 'YT Music', memory: 'Matriz de memória',
  automation: 'Automação', rules: 'Reflexos', tactical: 'Tecido tático', schematics: 'Membrana', settings: 'Ajustes',
};

const THEMES = [
  { id: 'cyan', name: 'Sinapse teal', color: '#2dfdd4' },
  { id: 'amber', name: 'Âmbar suave', color: '#ffcf5c' },
  { id: 'crimson', name: 'Coral', color: '#ff5c7a' },
  { id: 'emerald', name: 'Menta', color: '#2dfdd4' },
  { id: 'purple', name: 'Violeta profundo', color: '#8a5cff' },
];

export default function MiniHeader({ activeView, activeTheme, onThemeChange, soundEnabled, onToggleSound, onOpenSettings, onToggleGameMode, localMode = false }) {
  const [clock, setClock] = useState('');
  const [metrics, setMetrics] = useState({ memPercent: 0, cpuLoad: 0, coolingTemp: 0 });

  useEffect(() => {
    const tick = () => setClock(new Date().toLocaleTimeString('pt-BR'));
    tick();
    const t = setInterval(tick, 1000);
    return () => clearInterval(t);
  }, []);

  useEffect(() => {
    const fetchMetrics = async () => {
      if (window.electronAPI?.getSystemMetrics) {
        try {
          const data = await window.electronAPI.getSystemMetrics();
          setMetrics({ memPercent: Number(data.memPercent) || 0, cpuLoad: Math.round(15 + Math.random() * 20), coolingTemp: Math.round(44 + Math.random() * 6) });
          return;
        } catch { /* fallback */ }
      }
      setMetrics({ memPercent: Math.round(55 + Math.random() * 20), cpuLoad: Math.round(15 + Math.random() * 20), coolingTemp: Math.round(44 + Math.random() * 6) });
    };
    fetchMetrics();
    const t = setInterval(fetchMetrics, 4000);
    return () => clearInterval(t);
  }, []);

  return (
    <header className="h-14 shrink-0 flex items-center justify-between gap-3 px-4 font-syn-body relative z-10" style={{ background: 'rgba(5,3,17,0.85)', borderBottom: '1px solid rgba(45,253,212,0.15)', backdropFilter: 'blur(20px)' }}>
      <div className="flex items-center gap-2 min-w-0">
        <span className="w-2 h-2 syn-core shrink-0" style={{ borderRadius: '50%', background: localMode ? '#8a5cff' : '#2dfdd4', boxShadow: `0 0 8px ${localMode ? '#8a5cff' : '#2dfdd4'}` }} />
        <span className="font-syn-title text-[12px] font-medium tracking-wide truncate" style={{ color: '#eaf8f4' }}>
          {VIEW_TITLES[activeView] || 'J.A.R.V.I.S.'}
        </span>
        {localMode && <span className="syn-pill text-[10px] px-2 py-0.5" style={{ border: '1px solid rgba(138,92,255,0.5)', color: '#8a5cff' }}>cérebro local</span>}
      </div>

      <div className="hidden md:flex items-center gap-2 font-syn-body text-[11px] font-light" style={{ color: '#9aa6c9' }}>
        <span className="syn-pill px-2.5 py-1">ram <strong className="font-medium" style={{ color: '#2dfdd4' }}>{metrics.memPercent}%</strong></span>
        <span className="syn-pill px-2.5 py-1">cpu <strong className="font-medium" style={{ color: '#ffcf5c' }}>{metrics.cpuLoad}%</strong></span>
        <span className="syn-pill px-2.5 py-1">{metrics.coolingTemp}°C</span>
        <span className="tabular-nums">{clock}</span>
      </div>

      <div className="flex items-center gap-1.5">
        <div className="hidden sm:flex items-center gap-1.5 mr-1 syn-pill px-2 py-1.5" style={{ border: '1px solid rgba(45,253,212,0.18)', background: 'rgba(10,6,32,0.6)' }}>
          {THEMES.map((t) => (
            <button key={t.id} onClick={() => { sounds.playClick(); onThemeChange(t.id); }} title={t.name}
              className="syn-core transition-transform hover:scale-125"
              style={{ width: 12, height: 12, borderRadius: '50%', background: t.color, boxShadow: activeTheme === t.id ? `0 0 0 2px #eaf8f4, 0 0 8px ${t.color}` : `0 0 6px ${t.color}`, opacity: activeTheme === t.id ? 1 : 0.55 }} />
          ))}
        </div>
        <button onClick={() => { onToggleSound(); sounds.playClick(); }} title={soundEnabled ? 'Silenciar' : 'Ativar som'} className="syn-pill p-2 transition-all" style={{ color: '#9aa6c9' }}>
          {soundEnabled ? <Volume2 className="w-4 h-4" /> : <VolumeX className="w-4 h-4" />}
        </button>
        <button onClick={() => { sounds.playPowerPulse(); onToggleGameMode(); }} title="Hibernação" className="syn-pill p-2 transition-all" style={{ color: '#ffcf5c' }}>
          <Gamepad2 className="w-4 h-4" />
        </button>
        <button onClick={() => { sounds.playClick(); onOpenSettings(); }} title="Ajustes"
          className="syn-pill p-2 transition-all" style={activeView === 'settings' ? { background: 'rgba(45,253,212,0.2)', color: '#eaf8f4' } : { color: '#2dfdd4' }}>
          <Settings className="w-4 h-4" />
        </button>
      </div>
    </header>
  );
}
