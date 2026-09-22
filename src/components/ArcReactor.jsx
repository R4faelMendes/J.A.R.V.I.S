import React, { useState } from 'react';
import { Zap, Layers, Sparkles } from 'lucide-react';
import { sounds } from '../services/soundEffects';

const MARKS = {
  mark_1: {
    label: 'Mark I Clássico',
    accent: '#d99a3f',
    accentGlow: 'rgba(217, 154, 63, 0.55)',
    secondary: '#8a5a25',
    caption: 'PROTÓTIPO ORIGINAL // FORJADO À MÃO',
  },
  mark_3: {
    label: 'Mark III Tri-Ring',
    accent: 'var(--primary)',
    accentGlow: 'var(--primary-glow)',
    secondary: 'var(--secondary)',
    caption: 'CONFIGURAÇÃO CLÁSSICA // TRI-ANEL',
  },
  mark_50: {
    label: 'Mark L Triangular',
    accent: '#ff2a5f',
    accentGlow: 'rgba(255, 42, 95, 0.55)',
    secondary: '#ffb020',
    caption: 'PLACAS NANOTECNOLÓGICAS // MONTAGEM ATIVA',
  },
  mark_85: {
    label: 'Mark LXXXV Quântico',
    accent: '#b26bff',
    accentGlow: 'rgba(178, 107, 255, 0.55)',
    secondary: '#22d3ee',
    caption: 'CAMPO QUÂNTICO // SINCRONIA DIMENSIONAL',
  },
};

/* ---------- Mark I — Industrial / analógico, pesado, com rebites ---------- */
function Mark1Assembly({ isReactive }) {
  const bolts = Array.from({ length: 10 });
  return (
    <>
      {/* Anel externo grosso, estático, "blindado" */}
      <div className="absolute inset-0 rounded-full border-[6px] border-amber-800/50" />
      <div className={`absolute inset-1 rounded-full border-4 border-dashed border-amber-500/70 ${isReactive ? 'animate-spin' : 'animate-spin-rev'}`} style={{ animationDuration: '26s' }} />

      {/* Rebites fixos ao redor do casco */}
      <div className="absolute inset-2">
        {bolts.map((_, i) => {
          const angle = (i * 36 * Math.PI) / 180;
          const r = 47;
          const x = 50 + r * Math.cos(angle);
          const y = 50 + r * Math.sin(angle);
          return (
            <span
              key={i}
              className="absolute w-2 h-2 rounded-[2px] bg-amber-600/80 border border-amber-300/40 shadow-[0_0_4px_rgba(217,154,63,0.6)]"
              style={{ left: `${x}%`, top: `${y}%`, transform: 'translate(-50%, -50%)' }}
            />
          );
        })}
      </div>

      {/* Placa hexagonal metálica interna */}
      <svg viewBox="0 0 200 200" className="absolute inset-10 w-[calc(100%-5rem)] h-[calc(100%-5rem)]">
        <polygon points="100,20 166,60 166,140 100,180 34,140 34,60" fill="url(#mark1grad)" stroke="#f2c078" strokeWidth="2" />
        <polygon points="100,20 166,60 166,140 100,180 34,140 34,60" fill="none" stroke="#3a2410" strokeWidth="6" opacity="0.4" />
        <defs>
          <radialGradient id="mark1grad" cx="50%" cy="50%" r="65%">
            <stop offset="0%" stopColor="#7a4a1c" />
            <stop offset="100%" stopColor="#2c1a0a" />
          </radialGradient>
        </defs>
      </svg>

      {/* Anel de engrenagem interna girando devagar, pesada */}
      <div className={`absolute inset-16 rounded-full border-4 border-amber-700 border-t-transparent border-b-transparent ${isReactive ? 'animate-spin-slow' : 'animate-spin-rev'}`} style={{ animationDuration: '18s' }} />
    </>
  );
}

/* ---------- Mark III — Tri-anel clássico, elegante, entrelaçado ---------- */
function Mark3Assembly({ isReactive }) {
  return (
    <>
      <svg viewBox="0 0 200 200" className={`absolute inset-0 w-full h-full ${isReactive ? 'animate-spin' : 'animate-spin-slow'}`} style={{ animationDuration: isReactive ? '10s' : '24s' }}>
        <circle cx="100" cy="100" r="96" fill="none" stroke="var(--primary)" strokeWidth="2" strokeDasharray="60 40" opacity="0.55" />
      </svg>
      <svg viewBox="0 0 200 200" className="absolute inset-3 w-[calc(100%-1.5rem)] h-[calc(100%-1.5rem)] animate-spin-rev" style={{ animationDuration: '16s' }}>
        <circle cx="100" cy="100" r="90" fill="none" stroke="var(--secondary)" strokeWidth="2.5" strokeDasharray="45 65" opacity="0.6" />
      </svg>
      <svg viewBox="0 0 200 200" className="absolute inset-7 w-[calc(100%-3.5rem)] h-[calc(100%-3.5rem)] animate-spin-slow" style={{ animationDuration: '12s' }}>
        <circle cx="100" cy="100" r="82" fill="none" stroke="var(--primary)" strokeWidth="1.5" strokeDasharray="30 90" opacity="0.5" />
      </svg>

      {/* Placa triangular central, marca registrada do design clássico */}
      <svg viewBox="0 0 200 200" className="absolute inset-16 w-[calc(100%-8rem)] h-[calc(100%-8rem)] animate-pulse-glow">
        <polygon points="100,30 165,150 35,150" fill="none" stroke="var(--primary)" strokeWidth="2" opacity="0.8" />
        <polygon points="100,55 145,135 55,135" fill="var(--primary-dim)" stroke="var(--secondary)" strokeWidth="1" opacity="0.7" />
      </svg>

      <div className="absolute inset-8 rounded-full border border-cyan-300/50 shadow-[0_0_15px_rgba(0,240,255,0.35)]" />
    </>
  );
}

/* ---------- Mark L — Nanotecnologia: íris triangular em camadas ---------- */
function Mark50Assembly({ isReactive }) {
  const shards = [0, 60, 120, 180, 240, 300];
  return (
    <>
      <div className={`absolute inset-1 ${isReactive ? 'animate-spin' : 'animate-spin-slow'}`} style={{ animationDuration: '9s' }}>
        <svg viewBox="0 0 200 200" className="w-full h-full">
          <polygon points="100,4 190,150 10,150" fill="none" stroke="#ff2a5f" strokeWidth="2" opacity="0.55" />
        </svg>
      </div>
      <div className="absolute inset-1 animate-spin-rev" style={{ animationDuration: '11s' }}>
        <svg viewBox="0 0 200 200" className="w-full h-full">
          <polygon points="100,196 10,50 190,50" fill="none" stroke="#ffb020" strokeWidth="2" opacity="0.5" />
        </svg>
      </div>
      <div className={`absolute inset-6 ${isReactive ? 'animate-spin-rev' : 'animate-spin'}`} style={{ animationDuration: '15s' }}>
        <svg viewBox="0 0 200 200" className="w-full h-full">
          <polygon points="100,20 172,140 28,140" fill="rgba(255,42,95,0.06)" stroke="#ff2a5f" strokeWidth="1.5" opacity="0.7" />
        </svg>
      </div>

      {shards.map((deg, i) => {
        const angle = (deg * Math.PI) / 180;
        const r = 44;
        const x = 50 + r * Math.cos(angle);
        const y = 50 + r * Math.sin(angle);
        return (
          <span
            key={i}
            className="absolute w-1.5 h-1.5 bg-white animate-pulse"
            style={{
              left: `${x}%`, top: `${y}%`, transform: 'translate(-50%, -50%) rotate(45deg)',
              boxShadow: '0 0 6px #ff2a5f',
              animationDelay: `${i * 0.3}s`,
            }}
          />
        );
      })}

      <div className="absolute inset-14 rounded-full border border-rose-400/40 animate-pulse-glow" />
    </>
  );
}

/* ---------- Mark LXXXV — Reticulado quântico hexagonal, etéreo ---------- */
function Mark85Assembly({ isReactive }) {
  const hexCenters = [
    [100, 60], [135, 80], [135, 120], [100, 140], [65, 120], [65, 80],
  ];
  const nodes = [
    { inset: 'inset-4', dur: '8s', dir: 'animate-spin-slow' },
    { inset: 'inset-7', dur: '10s', dir: 'animate-spin-rev' },
    { inset: 'inset-10', dur: '12s', dir: 'animate-spin-slow' },
    { inset: 'inset-14', dur: '14s', dir: 'animate-spin-rev' },
    { inset: 'inset-20', dur: '16s', dir: 'animate-spin-slow' },
    { inset: 'inset-24', dur: '18s', dir: 'animate-spin-rev' },
  ];
  return (
    <>
      <svg viewBox="0 0 200 200" className="absolute inset-2 w-[calc(100%-1rem)] h-[calc(100%-1rem)] animate-spin-slow" style={{ animationDuration: '40s' }}>
        {hexCenters.map(([cx, cy], i) => (
          <polygon
            key={i}
            points={Array.from({ length: 6 }).map((_, k) => {
              const a = (k * 60 * Math.PI) / 180;
              return `${cx + 22 * Math.cos(a)},${cy + 22 * Math.sin(a)}`;
            }).join(' ')}
            fill="none"
            stroke="#b26bff"
            strokeWidth="1"
            opacity="0.4"
          />
        ))}
        <polygon
          points={Array.from({ length: 6 }).map((_, k) => {
            const a = (k * 60 * Math.PI) / 180;
            return `${100 + 40 * Math.cos(a)},${100 + 40 * Math.sin(a)}`;
          }).join(' ')}
          fill="none"
          stroke="#22d3ee"
          strokeWidth="1.2"
          opacity="0.55"
        />
      </svg>

      {[0, 1, 2].map((i) => (
        <div
          key={i}
          className="absolute rounded-full border animate-pulse-glow"
          style={{
            borderColor: i % 2 === 0 ? 'rgba(178,107,255,0.35)' : 'rgba(34,211,238,0.3)',
            inset: `${(i + 1) * 0.9}rem`,
            animationDuration: `${3 + i}s`,
            animationDelay: `${i * 0.4}s`,
          }}
        />
      ))}

      {nodes.map((n, i) => (
        <div key={i} className={`absolute ${n.inset} ${n.dir}`} style={{ animationDuration: n.dur }}>
          <span
            className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 w-1.5 h-1.5 rounded-full"
            style={{ background: i % 2 === 0 ? '#b26bff' : '#22d3ee', boxShadow: `0 0 8px ${i % 2 === 0 ? '#b26bff' : '#22d3ee'}` }}
          />
        </div>
      ))}

      <div className={`absolute inset-16 rounded-full border border-violet-400/50 ${isReactive ? 'animate-spin' : 'animate-spin-slow'}`} style={{ animationDuration: '20s' }} />
    </>
  );
}

export default function ArcReactor({
  isSpeaking = false,
  isListening = false,
  model = 'mark_3', // 'mark_1', 'mark_3', 'mark_50', 'mark_85'
  onModelChange,
  compact = false
}) {
  const [pulseBoost, setPulseBoost] = useState(false);
  const current = MARKS[model] || MARKS.mark_3;

  const handleCoreClick = () => {
    sounds.playPowerPulse();
    setPulseBoost(true);
    setTimeout(() => setPulseBoost(false), 1200);
  };

  const isReactive = isListening || isSpeaking || pulseBoost;

  // Versão compacta e alinhada para a sidebar (sem callouts laterais, sem overflow)
  if (compact) {
    const shortLabels = { mark_1: 'I', mark_3: 'III', mark_50: 'L', mark_85: 'LXXXV' };
    return (
      <div className="flex flex-col items-center px-2 py-3 select-none" data-purpose="arc-reactor-core">
        <div className="flex items-center gap-1.5">
          <span className={`w-1.5 h-1.5 rounded-full ${isReactive ? 'bg-emerald-400 animate-pulse' : 'bg-[var(--secondary)]'}`} />
          <span className="text-[10px] font-bold tracking-[0.22em] text-[var(--primary)] font-['JetBrains_Mono']">
            REATOR ARC
          </span>
        </div>

        <div
          onClick={handleCoreClick}
          title="Núcleo do reator — clique para pulso de energia"
          className="relative w-[196px] h-[196px] my-2 cursor-pointer"
        >
          {/* Brilho contido */}
          <div
            className="absolute inset-6 rounded-full blur-2xl opacity-25 pointer-events-none"
            style={{ backgroundColor: current.accentGlow }}
          />
          {isReactive && (
            <div
              className="absolute inset-0 rounded-full border animate-ping opacity-20 pointer-events-none"
              style={{ borderColor: current.accent }}
            />
          )}

          {model === 'mark_1' && <Mark1Assembly isReactive={isReactive} />}
          {model === 'mark_3' && <Mark3Assembly isReactive={isReactive} />}
          {model === 'mark_50' && <Mark50Assembly isReactive={isReactive} />}
          {model === 'mark_85' && <Mark85Assembly isReactive={isReactive} />}

          {/* Anel de progresso */}
          <svg viewBox="0 0 100 100" className="absolute inset-[21%] -rotate-90 z-[5]">
            <circle cx="50" cy="50" r="46" fill="none" stroke="rgba(255,255,255,0.08)" strokeWidth="3" />
            <circle
              cx="50" cy="50" r="46" fill="none"
              stroke={current.accent} strokeWidth="3" strokeLinecap="round"
              strokeDasharray={2 * Math.PI * 46}
              strokeDashoffset={isReactive ? 2 * Math.PI * 46 * 0.08 : 2 * Math.PI * 46 * 0.32}
              className="transition-all duration-700"
              style={{ filter: `drop-shadow(0 0 3px ${current.accent})` }}
            />
          </svg>

          {/* Núcleo central */}
          <div
            className="absolute inset-0 m-auto w-16 h-16 rounded-full bg-gradient-to-br from-cyan-950 via-slate-900 to-black border-2 flex flex-col items-center justify-center z-10 overflow-hidden"
            style={{ borderColor: current.accent, boxShadow: `0 0 16px ${current.accentGlow}` }}
          >
            <span className="text-[8px] font-bold tracking-widest font-['JetBrains_Mono']" style={{ color: current.accent }}>JARVIS</span>
            <span className="text-xs font-extrabold text-cyan-100 font-['Orbitron']">
              {isReactive ? '100%' : '98.4%'}
            </span>
            <span className="text-[7px] font-bold font-['JetBrains_Mono']" style={{ color: current.secondary }}>
              {isListening ? 'ESCUTANDO' : isSpeaking ? 'TRANSMITINDO' : 'ESTÁVEL'}
            </span>
          </div>
        </div>

        {/* Seletor de modelo compacto */}
        <div className="flex items-center bg-black/50 border border-white/10 rounded-md p-0.5 font-['JetBrains_Mono']">
          {Object.entries(MARKS).map(([id, m]) => (
            <button
              key={id}
              title={m.label}
              onClick={() => { sounds.playClick(); onModelChange?.(id); }}
              className={`px-2 py-0.5 rounded text-[10px] font-bold transition-all ${model === id ? 'text-black' : 'text-cyan-300/70 hover:text-white'}`}
              style={model === id ? { backgroundColor: m.accent } : undefined}
            >
              {shortLabels[id]}
            </button>
          ))}
        </div>

        <div className="mt-1.5 text-[9px] tracking-widest text-cyan-200/50 font-['JetBrains_Mono']">
          {isListening ? 'ESCUTANDO DIRETIVA' : isSpeaking ? 'TRANSMITINDO RESPOSTA' : current.caption}
        </div>
      </div>
    );
  }

  return (
    <div className="relative flex flex-col justify-between flex-1 overflow-hidden" data-purpose="arc-reactor-core">
      {/* Header Telemetry & Reactor Model Switcher */}
      <div className="flex items-center justify-between z-10 mb-2 font-['JetBrains_Mono']">
        <div>
          <div className="text-[11px] tracking-widest text-[var(--secondary)] font-bold glow-amber uppercase flex items-center gap-1.5">
            <span className={`w-2.5 h-2.5 rounded-full ${isReactive ? 'bg-emerald-400 animate-ping' : 'bg-[var(--secondary)] animate-pulse'}`}></span>
            <Sparkles size={11} className="text-[var(--secondary)]" />
            NÚCLEO ORBITAL // REATOR DE ARCO HOLOGRÁFICO
          </div>
          <div className="text-xs text-cyan-300/70">
            {isListening ? 'REAÇÃO DE VOZ ATIVA // ESCUTANDO DIRETIVA' : 'CAMPO MAGNÉTICO SINCRONIZADO'}
          </div>
        </div>

        {/* Reactor Model Switcher */}
        <div className="flex bg-black/50 border border-cyan-500/30 rounded p-0.5 text-[10px]">
          {Object.entries(MARKS).map(([id, m]) => (
            <button
              key={id}
              onClick={() => { sounds.playClick(); onModelChange?.(id); }}
              className={`px-1.5 py-0.5 rounded transition-all ${model === id ? 'text-black font-bold' : 'text-cyan-400 hover:text-white'}`}
              style={model === id ? { backgroundColor: m.accent, boxShadow: `0 0 8px ${m.accentGlow}` } : undefined}
            >
              {m.label.split(' ')[0]}
            </button>
          ))}
        </div>
      </div>

      {/* Central Holographic Reactor Graphic */}
      <div className="relative my-2 flex items-center justify-center flex-1 min-h-[320px]">
        {/* Shockwave expanding halos when listening or speaking */}
        {isReactive && (
          <>
            <div className="absolute w-80 h-80 sm:w-[440px] sm:h-[440px] rounded-full border-2 animate-ping opacity-35 pointer-events-none" style={{ borderColor: current.accent }} />
            <div className="absolute w-64 h-64 sm:w-80 sm:h-80 rounded-full blur-3xl pointer-events-none animate-pulse" style={{ backgroundColor: current.accentGlow }} />
          </>
        )}

        {/* Background Ambient Glows */}
        <div className={`absolute w-72 h-72 sm:w-96 sm:h-96 rounded-full blur-3xl pointer-events-none transition-all duration-500 ${isReactive ? 'scale-125 opacity-100' : 'opacity-50'}`} style={{ backgroundColor: current.accentGlow }} />

        {/* HUD Callout: left readout with leader line */}
        <div className="hidden sm:flex absolute left-0 top-6 items-center gap-1.5 z-20 pointer-events-none">
          <div className="flex flex-col items-end text-right leading-none">
            <span className="text-[8px] font-['JetBrains_Mono'] text-[var(--primary)]/80 tracking-widest">FLUXO ENERGÉTICO</span>
            <span className="text-[11px] font-['Orbitron'] font-bold text-cyan-100 glow-cyan">{isReactive ? '9.7' : '8.9'} GJ/s</span>
          </div>
          <Zap size={11} className="text-[var(--primary)]" />
          <div className="w-10 h-px bg-gradient-to-r from-[var(--primary)]/70 to-transparent" />
        </div>

        {/* HUD Callout: right readout with leader line */}
        <div className="hidden sm:flex absolute right-0 bottom-6 items-center gap-1.5 z-20 pointer-events-none">
          <div className="w-10 h-px bg-gradient-to-l from-[var(--secondary)]/70 to-transparent" />
          <Layers size={11} className="text-[var(--secondary)]" />
          <div className="flex flex-col items-start text-left leading-none">
            <span className="text-[8px] font-['JetBrains_Mono'] text-[var(--secondary)]/80 tracking-widest">CAMADAS DE BLINDAGEM</span>
            <span className="text-[11px] font-['Orbitron'] font-bold text-amber-100">{isReactive ? '12' : '11'}/12</span>
          </div>
        </div>

        {/* Concentric Holographic Rings Container — each Mark renders its own assembly */}
        <div
          onClick={handleCoreClick}
          className={`relative w-64 h-64 sm:w-[22rem] sm:h-[22rem] flex items-center justify-center cursor-pointer group select-none transition-transform duration-300 ${
            isReactive ? 'scale-105' : ''
          }`}
          title="Clique para descarregar pulso de energia do reator"
        >
          {model === 'mark_1' && <Mark1Assembly isReactive={isReactive} />}
          {model === 'mark_3' && <Mark3Assembly isReactive={isReactive} />}
          {model === 'mark_50' && <Mark50Assembly isReactive={isReactive} />}
          {model === 'mark_85' && <Mark85Assembly isReactive={isReactive} />}

          {/* Rotating Progress Arc around the core (comum a todos, cor por Mark) */}
          <svg viewBox="0 0 100 100" className="absolute inset-[4.2rem] w-[calc(100%-8.4rem)] h-[calc(100%-8.4rem)] -rotate-90 z-[5]">
            <circle cx="50" cy="50" r="46" fill="none" stroke="rgba(255,255,255,0.08)" strokeWidth="2" />
            <circle
              cx="50" cy="50" r="46" fill="none"
              stroke={current.accent} strokeWidth="2.5" strokeLinecap="round"
              strokeDasharray={2 * Math.PI * 46}
              strokeDashoffset={isReactive ? 2 * Math.PI * 46 * 0.08 : 2 * Math.PI * 46 * 0.32}
              className="transition-all duration-700"
              style={{ filter: `drop-shadow(0 0 4px ${current.accent})` }}
            />
          </svg>

          {/* Center Arc Eye Indicator with Dynamic Voice Shockwave */}
          <div
            className={`relative w-24 h-24 rounded-full bg-gradient-to-br from-cyan-950 via-slate-900 to-black border-2 flex flex-col items-center justify-center ${isReactive ? 'scale-115' : 'animate-pulse-glow'} z-10 transition-all duration-300 overflow-hidden`}
            style={{ borderColor: current.accent, boxShadow: `0 0 25px ${current.accentGlow}` }}
          >
            <div className="absolute inset-0 rounded-full opacity-30 pointer-events-none" style={{ background: 'repeating-linear-gradient(180deg, transparent 0px, transparent 3px, rgba(255,255,255,0.15) 4px)' }} />
            <span className="text-[9px] font-['JetBrains_Mono'] font-bold tracking-tighter z-10" style={{ color: current.accent }}>JARVIS</span>
            <span className="text-sm font-['Orbitron'] font-extrabold text-cyan-100 glow-cyan z-10">
              {isReactive ? '100%' : '98.4%'}
            </span>
            <span className="text-[8px] font-['JetBrains_Mono'] font-bold z-10" style={{ color: current.secondary }}>
              {isListening ? 'ESCUTANDO' : isSpeaking ? 'TRANSMITINDO' : 'ESTÁVEL'}
            </span>
          </div>

          {/* Crosshairs Labels */}
          <div className="absolute -top-3 text-[9px] font-['JetBrains_Mono'] tracking-widest bg-[#021327] px-2 py-0.5 border rounded shadow-md" style={{ color: current.accent, borderColor: current.accentGlow }}>
            ARC-REACT // {model.toUpperCase()}
          </div>
          <div className="absolute -bottom-3 text-[9px] font-['JetBrains_Mono'] tracking-widest bg-[#021327] px-2 py-0.5 border rounded shadow-md" style={{ color: current.secondary, borderColor: current.accentGlow }}>
            {current.caption}
          </div>
        </div>
      </div>
    </div>
  );
}