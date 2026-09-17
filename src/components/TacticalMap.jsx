import React, { useState, useEffect } from 'react';
import { Globe, Radio, ShieldCheck } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function TacticalMap() {
  const [uplink, setUplink] = useState(99.98);
  const [activeNode, setActiveNode] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => {
      setUplink(prev => +(99.90 + Math.random() * 0.09).toFixed(2));
      setActiveNode(prev => (prev + 1) % 6);
    }, 2000);
    return () => clearInterval(interval);
  }, []);

  return (
    <section
      onClick={() => sounds.playClick()}
      className="hud-panel rounded-lg p-3 sm:p-4 border-[var(--panel-border)] group cursor-pointer"
    >
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Header */}
      <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2 mb-2">
        <h2 className="text-xs uppercase font-['Orbitron'] tracking-wider text-[var(--primary)] flex items-center gap-1.5 glow-cyan">
          <Globe className="w-3.5 h-3.5 text-[var(--primary)]" />
          Malha de Nós Táticos
        </h2>
        <span className="text-[10px] text-cyan-400 font-['JetBrains_Mono'] bg-cyan-950/40 px-1.5 py-0.5 rounded border border-cyan-500/30">
          7/7 SATÉLITES
        </span>
      </div>

      <div className="flex items-center space-x-3">
        {/* Geometric Node Constellation Diagram */}
        <div className="w-24 h-24 relative flex-shrink-0 bg-cyan-950/30 rounded border border-cyan-500/30 flex items-center justify-center overflow-hidden">
          <svg className="w-full h-full p-2 text-cyan-400" viewBox="0 0 100 100">
            {/* Connective polygon paths */}
            <polygon
              fill="rgba(0, 240, 255, 0.08)"
              points="50,15 85,38 72,82 28,82 15,38"
              stroke="currentColor"
              strokeWidth="1.2"
            />
            <line stroke="currentColor" strokeDasharray="2 2" strokeWidth="1" x1="50" y1="15" x2="50" y2="50" />
            <line stroke="currentColor" strokeDasharray="2 2" strokeWidth="1" x1="85" y1="38" x2="50" y2="50" />
            <line stroke="currentColor" strokeDasharray="2 2" strokeWidth="1" x1="72" y1="82" x2="50" y2="50" />
            <line stroke="currentColor" strokeDasharray="2 2" strokeWidth="1" x1="28" y1="82" x2="50" y2="50" />
            <line stroke="currentColor" strokeDasharray="2 2" strokeWidth="1" x1="15" y1="38" x2="50" y2="50" />

            {/* Node points */}
            <circle cx="50" cy="15" r={activeNode === 0 ? "5" : "3.5"} className="fill-amber-400 text-amber-400" />
            <circle cx="85" cy="38" r={activeNode === 1 ? "5" : "3"} className="fill-cyan-300 text-cyan-300" />
            <circle cx="72" cy="82" r={activeNode === 2 ? "5" : "3"} className="fill-cyan-300 text-cyan-300" />
            <circle cx="28" cy="82" r={activeNode === 3 ? "5" : "3"} className="fill-cyan-300 text-cyan-300" />
            <circle cx="15" cy="38" r={activeNode === 4 ? "5" : "3"} className="fill-cyan-300 text-cyan-300" />

            {/* Center Master Node */}
            <circle cx="50" cy="50" r="4.5" className="fill-cyan-100 animate-ping" />
            <circle cx="50" cy="50" r="3" className="fill-cyan-200" />
          </svg>
        </div>

        {/* Constellation Metadata Status */}
        <div className="text-xs font-['JetBrains_Mono'] space-y-1">
          <div className="text-[11px] text-cyan-400/80">REDE GLOBAL:</div>
          <div className="text-cyan-200 font-bold tracking-tight">
            VERÔNICA // ÓRBITA GEO
          </div>
          <div className="text-[10px] text-emerald-400 flex items-center gap-1 font-semibold">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
            UPLINK SEGURO ({uplink}%)
          </div>
          <div className="text-[10px] text-[var(--secondary)] pt-0.5 flex items-center gap-1">
            <ShieldCheck className="w-3 h-3 text-[var(--secondary)]" />
            AMEAÇA AMBIENTAL: NULA
          </div>
        </div>
      </div>
    </section>
  );
}