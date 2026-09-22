import React from 'react';
import {
  Home, CalendarDays, Music, Database, Workflow, Radar, Box
} from 'lucide-react';
import ArcReactor from './ArcReactor';
import { sounds } from '../services/soundEffects';

const NAV_ITEMS = [
  { id: 'home', label: 'Central de Voz', icon: Home },
  { id: 'agenda', label: 'Agenda', icon: CalendarDays },
  { id: 'music', label: 'YT Music', icon: Music },
  { id: 'memory', label: 'Memória', icon: Database },
  { id: 'automation', label: 'Automação', icon: Workflow },
  { id: 'tactical', label: 'Mapa Tático', icon: Radar },
  { id: 'schematics', label: 'Esquemáticos', icon: Box },
];

export default function Sidebar({
  activeView,
  onSelect,
  isSpeaking,
  isListening,
  reactorModel,
  onModelChange,
  userName = 'Tony Stark',
  accessLevel = 'NÍVEL ALPHA',
}) {
  return (
    <aside className="w-[300px] shrink-0 h-screen flex flex-col bg-[#11165a] border-r border-[var(--primary)]/25 relative z-20">
      {/* Brand */}
      <div className="px-5 pt-4 pb-1 flex items-center justify-between">
        <div>
          <h1 className="font-['Orbitron'] font-black tracking-widest text-[var(--primary)] text-xl glow-cyan leading-none">
            J.A.R.V.I.S.
          </h1>
          <p className="text-[10px] text-cyan-200/60 font-['JetBrains_Mono'] tracking-widest mt-1">
            STARK IND. // OS v4.8.2
          </p>
        </div>
        <span className="flex items-center gap-1.5 text-[10px] font-['JetBrains_Mono'] text-emerald-300 bg-emerald-950/40 px-2 py-0.5 rounded border border-emerald-500/30">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
          ONLINE
        </span>
      </div>

      {/* Reator ARC — círculo no topo da sidebar */}
      <div className="px-3 mt-1 shrink-0">
        <div className="rounded-xl border border-[var(--primary)]/25 bg-black/40">
          <ArcReactor
            compact
            isSpeaking={isSpeaking}
            isListening={isListening}
            model={reactorModel}
            onModelChange={onModelChange}
          />
        </div>
      </div>

      {/* Navegação — conteúdos abaixo do círculo */}
      <nav className="flex-1 overflow-y-auto px-3 py-3 space-y-1.5 font-['JetBrains_Mono']">
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          const active = activeView === item.id;
          return (
            <button
              key={item.id}
              onClick={() => { sounds.playClick(); onSelect(item.id); }}
              className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-left text-xs font-bold tracking-wide transition-all border ${
                active
                  ? 'bg-[var(--primary-dim)] border-[var(--primary)]/70 text-white shadow-[0_0_8px_var(--primary-glow)]'
                  : 'bg-transparent border-transparent text-cyan-100/70 hover:text-white hover:bg-white/5 hover:border-cyan-400/30'
              }`}
            >
              <Icon className={`w-4 h-4 shrink-0 ${active ? 'text-[var(--primary)]' : 'text-cyan-300/70'}`} />
              <span className="flex-1">{item.label}</span>
              {active && <span className="w-1.5 h-1.5 rounded-full bg-[var(--primary)] shadow-[0_0_6px_var(--primary)]" />}
            </button>
          );
        })}
      </nav>

      {/* Rodapé — operador */}
      <div className="px-4 py-3 border-t border-white/10 font-['JetBrains_Mono']">
        <div className="text-[11px] font-bold text-white/90 truncate">{userName}</div>
        <div className="text-[10px] text-[var(--secondary)] tracking-widest">{accessLevel}</div>
      </div>
    </aside>
  );
}
