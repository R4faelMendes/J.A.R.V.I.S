import React, { useState } from 'react';
import { Zap, Layers, Sparkles } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function ArcReactor({ 
  isSpeaking = false, 
  isListening = false,
  model = 'mark_3', // 'mark_1', 'mark_3', 'mark_50', 'mark_85'
  onModelChange 
}) {
  const [pulseBoost, setPulseBoost] = useState(false);

  const handleCoreClick = () => {
    sounds.playPowerPulse();
    setPulseBoost(true);
    setTimeout(() => setPulseBoost(false), 1200);
  };

  const models = [
    { id: 'mark_1', name: 'Mark I Clássico' },
    { id: 'mark_3', name: 'Mark III Tri-Ring' },
    { id: 'mark_50', name: 'Mark L Triangular' },
    { id: 'mark_85', name: 'Mark LXXXV Quântico' },
  ];

  const isReactive = isListening || isSpeaking || pulseBoost;

  return (
    <div className="relative flex flex-col justify-between flex-1 overflow-hidden" data-purpose="arc-reactor-core">
      {/* Header Telemetry & Reactor Model Switcher */}
      <div className="flex items-center justify-between z-10 mb-2 font-['JetBrains_Mono']">
        <div>
          <div className="text-[11px] tracking-widest text-[var(--secondary)] font-bold glow-amber uppercase flex items-center gap-1.5">
            <span className={`w-2.5 h-2.5 rounded-full ${isReactive ? 'bg-emerald-400 animate-ping' : 'bg-[var(--secondary)] animate-pulse'}`}></span>
            NÚCLEO ORBITAL // REATOR DE ARCO HOLOGRÁFICO
          </div>
          <div className="text-xs text-cyan-300/70">
            {isListening ? '🔥 REAÇÃO DE VOZ ATIVA: ESCUTANDO DIRETIVA' : 'CAMPO MAGNÉTICO SINCRONIZADO'}
          </div>
        </div>

        {/* Reactor Model Switcher */}
        <div className="flex bg-black/50 border border-cyan-500/30 rounded p-0.5 text-[10px]">
          {models.map(m => (
            <button
              key={m.id}
              onClick={() => { sounds.playClick(); onModelChange?.(m.id); }}
              className={`px-1.5 py-0.5 rounded transition-all ${
                model === m.id
                  ? 'bg-[var(--primary)] text-black font-bold shadow-[0_0_8px_var(--primary)]'
                  : 'text-cyan-400 hover:text-white'
              }`}
            >
              {m.name.split(' ')[0]}
            </button>
          ))}
        </div>
      </div>

      {/* Central Holographic Reactor Graphic */}
      <div className="relative my-2 flex items-center justify-center flex-1 min-h-[300px]">
        {/* Shockwave expanding halos when listening or speaking */}
        {isReactive && (
          <>
            <div className="absolute w-80 h-80 sm:w-[420px] sm:h-[420px] rounded-full border-2 border-[var(--primary)] animate-ping opacity-35 pointer-events-none" />
            <div className="absolute w-64 h-64 sm:w-80 sm:h-80 rounded-full bg-[var(--primary-glow)] blur-3xl pointer-events-none animate-pulse" />
          </>
        )}

        {/* Background Ambient Glows */}
        <div className={`absolute w-72 h-72 sm:w-96 sm:h-96 rounded-full bg-cyan-500/15 blur-3xl pointer-events-none transition-all duration-500 ${isReactive ? 'scale-125 opacity-100 bg-cyan-400/30' : 'opacity-60'}`} />

        {/* Concentric Holographic Rings Container */}
        <div 
          onClick={handleCoreClick}
          className={`relative w-64 h-64 sm:w-80 sm:h-80 flex items-center justify-center cursor-pointer group select-none transition-transform duration-300 ${
            isReactive ? 'scale-105' : ''
          }`}
          title="Clique para descarregar pulso de energia do reator"
        >
          {/* Outermost Dial Ring with Dashes */}
          <div className={`absolute inset-0 rounded-full border-2 border-dashed border-cyan-400/40 ${isReactive ? 'animate-spin border-[var(--primary)]' : 'animate-spin-slow'} transition-all`} />

          {/* Model 1: Mark I Classic Ring */}
          {model === 'mark_1' && (
            <div className="absolute inset-6 rounded-full border-8 border-amber-500/60 border-dashed animate-spin-rev" />
          )}

          {/* Model 3: Mark L Nanotech Triangular Ring */}
          {model === 'mark_50' && (
            <div className="absolute inset-8 border-4 border-rose-500/70 rotate-45 animate-pulse-glow" style={{ clipPath: 'polygon(50% 0%, 0% 100%, 100% 100%)' }} />
          )}

          {/* Outer Amber Segmented Ring */}
          <div className={`absolute inset-4 rounded-full border-4 border-[var(--secondary)] border-t-transparent border-b-transparent ${isReactive ? 'animate-spin-slow' : 'animate-spin-rev'} shadow-[0_0_20px_var(--secondary-glow)]`} />

          {/* Concentric Cyan Solid Ring */}
          <div className="absolute inset-8 rounded-full border border-cyan-300/60 shadow-[0_0_15px_rgba(0,240,255,0.4)]" />

          {/* Tick Marker Ring */}
          <div className="absolute inset-12 rounded-full border border-cyan-500/30 flex items-center justify-center">
            <div className="w-full h-0.5 bg-cyan-400/30 rotate-45"></div>
            <div className="w-full h-0.5 bg-cyan-400/30 -rotate-45 absolute"></div>
          </div>

          {/* Segmented Cyan Inner Drive */}
          <div className={`absolute inset-16 rounded-full border-4 border-[var(--primary)] border-l-transparent border-r-transparent ${isReactive ? 'animate-spin' : 'animate-spin-slow'} shadow-[0_0_25px_var(--primary-glow)] transition-all`} />

          {/* Center Arc Eye Indicator with Dynamic Voice Shockwave */}
          <div className={`relative w-24 h-24 rounded-full bg-gradient-to-br from-cyan-950 via-slate-900 to-black border-2 border-[var(--primary)] flex flex-col items-center justify-center shadow-[0_0_25px_var(--primary)] ${isReactive ? 'scale-115 border-white shadow-[0_0_35px_var(--primary)]' : 'animate-pulse-glow'} z-10 transition-all duration-300`}>
            <span className="text-[9px] font-['JetBrains_Mono'] text-[var(--primary)] font-bold tracking-tighter">JARVIS</span>
            <span className="text-sm font-['Orbitron'] font-extrabold text-cyan-100 glow-cyan">
              {isReactive ? '100%' : '98.4%'}
            </span>
            <span className="text-[8px] font-['JetBrains_Mono'] text-[var(--secondary)] font-bold">
              {isListening ? 'ESCUTANDO' : isSpeaking ? 'TRANSMITINDO' : 'ESTÁVEL'}
            </span>
          </div>

          {/* Crosshairs Labels */}
          <div className="absolute -top-3 text-[9px] font-['JetBrains_Mono'] text-[var(--primary)] tracking-widest bg-[#021327] px-2 py-0.5 border border-cyan-500/40 rounded shadow-md">
            ARC-REACT // {model.toUpperCase()}
          </div>
          <div className="absolute -bottom-3 text-[9px] font-['JetBrains_Mono'] text-[var(--secondary)] tracking-widest bg-[#021327] px-2 py-0.5 border border-amber-500/40 rounded shadow-md">
            {isReactive ? 'ENERGIA EM EXPANSÃO' : 'ESTÁVEL // 100%'}
          </div>
        </div>
      </div>
    </div>
  );
}