import React, { useState } from 'react';
import { Zap, Activity } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function ArcReactor({ powerOutput = '3.2 GIGA-JOULES', cognitiveLatency = '0.82 MS', isSpeaking = false }) {
  const [pulseBoost, setPulseBoost] = useState(false);

  const handleCoreClick = () => {
    sounds.playPowerPulse();
    setPulseBoost(true);
    setTimeout(() => setPulseBoost(false), 1200);
  };

  return (
    <div className="relative flex flex-col justify-between flex-1 overflow-hidden" data-purpose="arc-reactor-core">
      {/* Header Telemetry */}
      <div className="flex items-center justify-between z-10 mb-2">
        <div>
          <div className="text-[11px] font-['JetBrains_Mono'] tracking-widest text-[var(--secondary)] font-bold glow-amber uppercase flex items-center gap-1.5">
            <span className="w-2 h-2 rounded-full bg-[var(--secondary)] animate-pulse"></span>
            NÚCLEO ORBITAL // REATOR DE ARCO HOLOGRÁFICO
          </div>
          <div className="text-xs text-cyan-300/70 font-['JetBrains_Mono']">
            COBERTURA TÁTICA ATIVA • CAMPO REPULSOR SINCRONIZADO
          </div>
        </div>
        <div className="text-right font-['JetBrains_Mono'] text-[10px] text-cyan-400/70 hidden sm:block">
          LATÊNCIA COGNITIVA: <span className="text-cyan-200 font-bold">{cognitiveLatency}</span><br />
          POTÊNCIA DE SAÍDA: <span className="text-[var(--secondary)] font-bold">{powerOutput}</span>
        </div>
      </div>

      {/* Central Holographic Reactor Graphic */}
      <div className="relative my-4 flex items-center justify-center flex-1 min-h-[300px]">
        {/* Background Ambient Glows */}
        <div className={`absolute w-72 h-72 sm:w-96 sm:h-96 rounded-full bg-cyan-500/15 blur-3xl pointer-events-none transition-all duration-500 ${pulseBoost || isSpeaking ? 'scale-125 opacity-100 bg-cyan-400/30' : 'opacity-60'}`} />
        <div className="absolute w-44 h-44 rounded-full bg-amber-500/10 blur-2xl pointer-events-none" />

        {/* Concentric Holographic Rings Container */}
        <div 
          onClick={handleCoreClick}
          className="relative w-64 h-64 sm:w-80 sm:h-80 flex items-center justify-center cursor-pointer group select-none"
          title="Clique para descarregar pulso de energia do reator"
        >
          {/* Outermost Dial Ring with Dashes */}
          <div className="absolute inset-0 rounded-full border-2 border-dashed border-cyan-400/40 animate-spin-slow group-hover:border-cyan-300 transition-colors" />

          {/* Outer Amber Segmented Ring */}
          <div className="absolute inset-4 rounded-full border-4 border-[var(--secondary)] border-t-transparent border-b-transparent animate-spin-rev shadow-[0_0_20px_var(--secondary-glow)]" />

          {/* Concentric Cyan Solid Ring */}
          <div className="absolute inset-8 rounded-full border border-cyan-300/60 shadow-[0_0_15px_rgba(0,240,255,0.4)]" />

          {/* Tick Marker Ring */}
          <div className="absolute inset-12 rounded-full border border-cyan-500/30 flex items-center justify-center">
            <div className="w-full h-0.5 bg-cyan-400/30 rotate-45"></div>
            <div className="w-full h-0.5 bg-cyan-400/30 -rotate-45 absolute"></div>
          </div>

          {/* Segmented Cyan Inner Drive */}
          <div className={`absolute inset-16 rounded-full border-4 border-[var(--primary)] border-l-transparent border-r-transparent animate-spin-slow shadow-[0_0_25px_var(--primary-glow)] transition-all ${isSpeaking ? 'border-amber-300' : ''}`} />

          {/* Inner Amber Core Orbit */}
          <div className="absolute inset-20 rounded-full border-2 border-amber-300/80 border-t-transparent animate-spin-rev" />

          {/* Center Arc Eye Indicator */}
          <div className={`relative w-24 h-24 rounded-full bg-gradient-to-br from-cyan-950 via-slate-900 to-black border-2 border-[var(--primary)] flex flex-col items-center justify-center shadow-[0_0_25px_var(--primary)] animate-pulse-glow z-10 transition-transform duration-300 group-hover:scale-110 ${pulseBoost ? 'scale-125 border-white' : ''}`}>
            <span className="text-[9px] font-['JetBrains_Mono'] text-[var(--primary)] font-bold tracking-tighter">JARVIS</span>
            <span className="text-sm font-['Orbitron'] font-extrabold text-cyan-100 glow-cyan">
              {pulseBoost ? '120%' : '98.4%'}
            </span>
            <span className="text-[8px] font-['JetBrains_Mono'] text-[var(--secondary)] font-bold">
              {isSpeaking ? 'FALANDO' : 'ONLINE'}
            </span>
          </div>

          {/* Circular Target Crosshairs Overlay Badges */}
          <div className="absolute -top-3 text-[9px] font-['JetBrains_Mono'] text-[var(--primary)] tracking-widest bg-[#021327] px-2 py-0.5 border border-cyan-500/40 rounded shadow-md">
            ARC-REACT 01
          </div>
          <div className="absolute -bottom-3 text-[9px] font-['JetBrains_Mono'] text-[var(--secondary)] tracking-widest bg-[#021327] px-2 py-0.5 border border-amber-500/40 rounded shadow-md">
            ESTÁVEL // 100%
          </div>
        </div>
      </div>
    </div>
  );
}