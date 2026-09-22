import React, { useState, useEffect } from 'react';
import { Settings, Volume2, VolumeX, Gamepad2 } from 'lucide-react';
import { sounds } from '../services/soundEffects';

const VIEW_TITLES = {
  home: 'CENTRAL DE VOZ',
  agenda: 'AGENDA',
  music: 'YT MUSIC // ÁUDIO',
  memory: 'MATRIZ DE MEMÓRIA',
  automation: 'PIPELINE DE AUTOMAÇÃO',
  tactical: 'MAPA TÁTICO // PROCESSOS',
  schematics: 'MÓDULO ESQUEMÁTICO',
  settings: 'CONFIGURAÇÕES',
};

const THEMES = [
  { id: 'cyan', name: 'Cyan Stark', color: 'bg-cyan-400' },
  { id: 'amber', name: 'Mark III Gold', color: 'bg-amber-400' },
  { id: 'crimson', name: 'Hulkbuster Red', color: 'bg-rose-500' },
  { id: 'emerald', name: 'Time Stone', color: 'bg-emerald-400' },
  { id: 'purple', name: 'Power Stone', color: 'bg-purple-400' },
];

export default function MiniHeader({
  activeView,
  activeTheme,
  onThemeChange,
  soundEnabled,
  onToggleSound,
  onOpenSettings,
  onToggleGameMode,
}) {
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
          setMetrics({
            memPercent: Number(data.memPercent) || 0,
            cpuLoad: Math.round(15 + Math.random() * 20),
            coolingTemp: Math.round(44 + Math.random() * 6),
          });
          return;
        } catch { /* fallback abaixo */ }
      }
      setMetrics({
        memPercent: Math.round(55 + Math.random() * 20),
        cpuLoad: Math.round(15 + Math.random() * 20),
        coolingTemp: Math.round(44 + Math.random() * 6),
      });
    };
    fetchMetrics();
    const t = setInterval(fetchMetrics, 4000);
    return () => clearInterval(t);
  }, []);

  return (
    <header className="h-12 shrink-0 flex items-center justify-between gap-3 px-4 bg-[#0d0d0f] border-b border-[var(--primary)]/20 font-['JetBrains_Mono'] relative z-10">
      {/* Título da view ativa */}
      <div className="flex items-center gap-2 min-w-0">
        <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse shrink-0" />
        <span className="text-[11px] font-bold tracking-widest text-[var(--primary)] glow-cyan truncate">
          {VIEW_TITLES[activeView] || 'J.A.R.V.I.S.'}
        </span>
      </div>

      {/* Telemetria compacta do PC */}
      <div className="hidden md:flex items-center gap-2 text-[10px] text-cyan-200/80">
        <span className="px-2 py-0.5 rounded bg-white/5 border border-cyan-500/20">
          RAM <strong className="text-[var(--primary)]">{metrics.memPercent}%</strong>
        </span>
        <span className="px-2 py-0.5 rounded bg-white/5 border border-amber-500/20">
          CPU <strong className="text-[var(--secondary)]">{metrics.cpuLoad}%</strong>
        </span>
        <span className="px-2 py-0.5 rounded bg-white/5 border border-cyan-500/20">
          {metrics.coolingTemp}°C
        </span>
        <span className="text-cyan-400/70 tabular-nums">{clock}</span>
      </div>

      {/* Ações: temas + som + game + config */}
      <div className="flex items-center gap-1.5">
        <div className="hidden sm:flex items-center gap-1 mr-1">
          {THEMES.map((t) => (
            <button
              key={t.id}
              onClick={() => { sounds.playClick(); onThemeChange(t.id); }}
              title={t.name}
              className={`w-3 h-3 rounded-full ${t.color} transition-transform hover:scale-125 ${
                activeTheme === t.id ? 'ring-2 ring-white scale-110' : 'opacity-50 hover:opacity-100'
              }`}
            />
          ))}
        </div>

        <button
          onClick={() => { onToggleSound(); sounds.playClick(); }}
          title={soundEnabled ? 'Silenciar' : 'Ativar som'}
          className="p-1.5 rounded hover:bg-white/10 text-cyan-300 transition-colors"
        >
          {soundEnabled ? <Volume2 className="w-4 h-4 text-emerald-400" /> : <VolumeX className="w-4 h-4 text-red-400" />}
        </button>

        <button
          onClick={() => { sounds.playPowerPulse(); onToggleGameMode(); }}
          title="Modo Game / Hibernação"
          className="p-1.5 rounded hover:bg-amber-500/20 text-amber-400 transition-colors"
        >
          <Gamepad2 className="w-4 h-4" />
        </button>

        <button
          onClick={() => { sounds.playClick(); onOpenSettings(); }}
          title="Configurações"
          className={`p-1.5 rounded transition-colors ${
            activeView === 'settings'
              ? 'bg-[var(--primary)] text-black'
              : 'hover:bg-white/10 text-[var(--primary)]'
          }`}
        >
          <Settings className="w-4 h-4" />
        </button>
      </div>
    </header>
  );
}
