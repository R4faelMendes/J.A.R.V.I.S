import React from 'react';
import { Home, CalendarDays, Music, Database, Workflow, Radar, Box, Dna } from 'lucide-react';
import ArcReactor from './ArcReactor';
import { sounds } from '../services/soundEffects';

const NAV_ITEMS = [
  { id: 'home', label: 'Central de voz', icon: Home },
  { id: 'agenda', label: 'Agenda', icon: CalendarDays },
  { id: 'music', label: 'YT Music', icon: Music },
  { id: 'memory', label: 'Memória', icon: Database },
  { id: 'automation', label: 'Automação', icon: Workflow },
  { id: 'rules', label: 'Reflexos', icon: Dna },
  { id: 'tactical', label: 'Tecido tático', icon: Radar },
  { id: 'schematics', label: 'Membrana', icon: Box },
];

export default function Sidebar({ activeView, onSelect, isSpeaking, isListening, reactorModel, onModelChange, userName = 'Tony Stark', accessLevel = 'Nível alpha', localMode = false }) {
  return (
    <aside className="w-[300px] shrink-0 h-screen flex flex-col relative z-20" style={{ background: 'linear-gradient(180deg, #0a0620, #050311)', borderRight: '1px solid rgba(45,253,212,0.15)' }}>
      <div className="px-5 pt-4 pb-1 flex items-center justify-between">
        <div>
          <h1 className="font-syn-title font-bold tracking-wide text-xl leading-none" style={{ color: '#eaf8f4', textShadow: '0 0 12px rgba(45,253,212,0.5)' }}>
            J.A.R.V.I.S.
          </h1>
          <p className="font-syn-body text-[10px] font-light tracking-widest mt-1" style={{ color: '#9aa6c9' }}>
            synapse · os v5.0
          </p>
        </div>
        <span className="syn-pill flex items-center gap-1.5 font-syn-body text-[10px] px-2.5 py-1" style={{ border: `1px solid ${localMode ? 'rgba(138,92,255,0.5)' : 'rgba(45,253,212,0.35)'}`, color: localMode ? '#8a5cff' : '#2dfdd4', background: localMode ? 'rgba(138,92,255,0.1)' : 'rgba(45,253,212,0.07)', boxShadow: `0 0 10px ${localMode ? 'rgba(138,92,255,0.4)' : 'rgba(45,253,212,0.3)'}` }} title={localMode ? 'Cérebro local ativo (Ollama)' : 'Cérebro remoto ativo'}>
          <span className="w-1.5 h-1.5 syn-core" style={{ borderRadius: '50%', background: localMode ? '#8a5cff' : '#2dfdd4' }} />
          {localMode ? 'local' : 'vivo'}
        </span>
      </div>

      <div className="px-3 mt-1 shrink-0">
        <div className="syn-panel" style={{ borderRadius: '24px 20px 26px 18px' }}>
          <ArcReactor compact isSpeaking={isSpeaking} isListening={isListening} model={reactorModel} onModelChange={onModelChange} />
        </div>
      </div>

      <nav className="flex-1 overflow-y-auto px-3 py-3 space-y-1.5 font-syn-body">
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          const active = activeView === item.id;
          return (
            <button
              key={item.id}
              onClick={() => { sounds.playClick(); onSelect(item.id); }}
              className="syn-pill w-full flex items-center gap-3 px-4 py-2.5 text-left text-xs font-medium transition-all"
              style={active
                ? { background: 'rgba(45,253,212,0.14)', border: '1px solid rgba(45,253,212,0.45)', color: '#eaf8f4', boxShadow: '0 0 12px rgba(45,253,212,0.3)' }
                : { background: 'transparent', border: '1px solid transparent', color: 'rgba(234,248,244,0.65)' }}
            >
              <Icon className="w-4 h-4 shrink-0" style={{ color: active ? '#2dfdd4' : '#9aa6c9' }} />
              <span className="flex-1">{item.label}</span>
              {active && <span className="w-1.5 h-1.5 syn-core" style={{ borderRadius: '50%', background: '#2dfdd4', boxShadow: '0 0 6px #2dfdd4' }} />}
            </button>
          );
        })}
      </nav>

      <div className="px-4 py-3 font-syn-body" style={{ borderTop: '1px solid rgba(138,92,255,0.2)' }}>
        <div className="text-[12px] font-medium truncate" style={{ color: '#eaf8f4' }}>{userName}</div>
        <div className="text-[10px] font-light tracking-widest" style={{ color: '#9aa6c9' }}>{accessLevel}</div>
      </div>
    </aside>
  );
}
