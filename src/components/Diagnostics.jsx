import React, { useState, useEffect } from 'react';
import { Cpu, Flame, Snowflake, ShieldCheck } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function Diagnostics() {
  const [tflops, setTflops] = useState(84.2);
  const [arcEff, setArcEff] = useState(96.8);
  const [tempKelvin, setTempKelvin] = useState(18);
  const [nanites, setNanites] = useState(99.7);
  const [buffer, setBuffer] = useState(0.4);
  const [flux, setFlux] = useState(1.2);

  useEffect(() => {
    const interval = setInterval(() => {
      setTflops(prev => +(82.0 + Math.random() * 4.5).toFixed(1));
      setArcEff(prev => +(95.5 + Math.random() * 2.8).toFixed(1));
      setTempKelvin(prev => Math.round(17 + Math.random() * 2));
      setNanites(prev => +(99.4 + Math.random() * 0.5).toFixed(1));
      setBuffer(prev => +(0.3 + Math.random() * 0.3).toFixed(1));
      setFlux(prev => +(1.1 + Math.random() * 0.3).toFixed(1));
    }, 2800);
    return () => clearInterval(interval);
  }, []);

  return (
    <section 
      onClick={() => sounds.playClick()}
      className="hud-panel rounded-lg p-3 sm:p-4 flex-1 flex flex-col justify-between border-[var(--panel-border)] group"
    >
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      <div>
        <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2 mb-3">
          <h2 className="text-xs uppercase font-['Orbitron'] tracking-wider text-[var(--primary)] glow-cyan flex items-center gap-1.5">
            <Cpu className="w-3.5 h-3.5 text-[var(--primary)]" />
            Diagnóstico de Subsistemas
          </h2>
          <span className="text-[10px] text-[var(--primary)] font-['JetBrains_Mono']">FRAME #4092</span>
        </div>

        {/* Dynamic Metric Bars */}
        <div className="space-y-3 font-['JetBrains_Mono'] text-xs">
          {/* Metric 1: Quantum Compute */}
          <div>
            <div className="flex justify-between mb-1">
              <span className="text-cyan-400/80">Processamento Quântico</span>
              <span className="text-[var(--primary)] font-bold glow-cyan">{tflops} TFLOPS</span>
            </div>
            <div className="w-full bg-slate-900/80 h-2 rounded overflow-hidden border border-cyan-500/30 p-[1px]">
              <div 
                className="bg-gradient-to-r from-[var(--primary)] to-blue-400 h-full rounded shadow-[0_0_8px_var(--primary)] transition-all duration-700" 
                style={{ width: `${Math.min(100, (tflops / 90) * 100)}%` }}
              />
            </div>
          </div>

          {/* Metric 2: ARC Reactor Relay */}
          <div>
            <div className="flex justify-between mb-1">
              <span className="text-[var(--secondary)] flex items-center gap-1">
                <Flame className="w-3 h-3 text-[var(--secondary)]" />
                Matriz Reatora ARC
              </span>
              <span className="text-[var(--secondary)] font-bold glow-amber">{arcEff}% EFF</span>
            </div>
            <div className="w-full bg-slate-900/80 h-2 rounded overflow-hidden border border-amber-500/30 p-[1px]">
              <div 
                className="bg-gradient-to-r from-amber-500 to-yellow-300 h-full rounded shadow-[0_0_8px_#ffb020] transition-all duration-700"
                style={{ width: `${arcEff}%` }}
              />
            </div>
          </div>

          {/* Metric 3: Resfriamento Criogênico */}
          <div>
            <div className="flex justify-between mb-1">
              <span className="text-cyan-400/80 flex items-center gap-1">
                <Snowflake className="w-3 h-3 text-cyan-400" />
                Resfriamento Criogênico
              </span>
              <span className="text-cyan-300 font-bold">{tempKelvin}° KELVIN</span>
            </div>
            <div className="w-full bg-slate-900/80 h-2 rounded overflow-hidden border border-cyan-500/30 p-[1px]">
              <div 
                className="bg-[var(--primary)] h-full rounded transition-all duration-700 shadow-[0_0_6px_var(--primary)]"
                style={{ width: '42%' }}
              />
            </div>
          </div>

          {/* Metric 4: Barramento Neural Nanite */}
          <div>
            <div className="flex justify-between mb-1">
              <span className="text-cyan-400/80 flex items-center gap-1">
                <ShieldCheck className="w-3 h-3 text-emerald-400" />
                Integridade de Nanites
              </span>
              <span className="text-emerald-300 font-bold">{nanites}%</span>
            </div>
            <div className="w-full bg-slate-900/80 h-2 rounded overflow-hidden border border-cyan-500/30 p-[1px]">
              <div 
                className="bg-gradient-to-r from-[var(--primary)] to-emerald-400 h-full rounded transition-all duration-700"
                style={{ width: `${nanites}%` }}
              />
            </div>
          </div>
        </div>
      </div>

      {/* Mini Telemetry Quick Stats */}
      <div className="mt-4 pt-3 border-t border-cyan-500/20 grid grid-cols-3 gap-1 text-center font-['JetBrains_Mono']">
        <div className="p-1.5 rounded bg-cyan-950/20 border border-cyan-500/20 hover:border-[var(--primary)] transition-all">
          <span className="text-[9px] text-cyan-400/60 block">BUFFER</span>
          <span className="text-xs text-cyan-200 font-bold">{buffer} ms</span>
        </div>
        <div className="p-1.5 rounded bg-cyan-950/20 border border-cyan-500/20 hover:border-[var(--primary)] transition-all">
          <span className="text-[9px] text-cyan-400/60 block">FLUXO</span>
          <span className="text-xs text-cyan-200 font-bold">{flux} TB/s</span>
        </div>
        <div className="p-1.5 rounded bg-cyan-950/20 border border-cyan-500/20 hover:border-emerald-400 transition-all">
          <span className="text-[9px] text-cyan-400/60 block">SEGURANÇA</span>
          <span className="text-xs text-emerald-300 font-bold">ISOLADO</span>
        </div>
      </div>
    </section>
  );
}