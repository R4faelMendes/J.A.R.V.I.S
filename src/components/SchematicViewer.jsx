import React, { useState, useEffect } from 'react';
import { Box, RotateCw, Layers, ZoomIn } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function SchematicViewer() {
  const [angle, setAngle] = useState(314.2);
  const [isRotating, setIsRotating] = useState(true);
  const [exploded, setExploded] = useState(false);
  const [nanotechLayer, setNanotechLayer] = useState(false);

  useEffect(() => {
    let interval;
    if (isRotating) {
      interval = setInterval(() => {
        setAngle(prev => +((prev + 0.8) % 360).toFixed(1));
      }, 50);
    }
    return () => clearInterval(interval);
  }, [isRotating]);

  const toggleRotate = () => {
    sounds.playClick();
    setIsRotating(!isRotating);
  };

  const toggleExploded = () => {
    sounds.playPowerPulse();
    setExploded(!exploded);
  };

  const toggleNanotech = () => {
    sounds.playScan();
    setNanotechLayer(!nanotechLayer);
  };

  return (
    <section className="hud-panel rounded-lg p-3 sm:p-4 flex-1 flex flex-col border-[var(--panel-border)] group">
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Header */}
      <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2 mb-2">
        <h2 className="text-xs uppercase font-['Orbitron'] tracking-wider text-[var(--primary)] flex items-center gap-2 glow-cyan">
          <Box className="w-3.5 h-3.5 text-[var(--primary)]" />
          Esquemático Holográfico 3D
        </h2>
        <span className="text-[10px] text-[var(--secondary)] font-['JetBrains_Mono']">
          MOD: MARK-LXXXV
        </span>
      </div>

      {/* 3D Wireframe Canvas / SVG Rendering */}
      <div className="relative flex-1 min-h-[170px] bg-cyan-950/20 rounded border border-cyan-500/30 flex items-center justify-center p-2 overflow-hidden select-none">
        <div className="absolute inset-0 bg-gradient-to-b from-cyan-500/5 to-transparent pointer-events-none"></div>
        <div className="absolute inset-0 cyber-grid opacity-40"></div>

        {/* Wireframe Robot Arm SVG */}
        <svg
          className={`w-4/5 h-4/5 text-cyan-300 stroke-current fill-none transition-all duration-700 ${
            nanotechLayer ? 'text-amber-300 filter drop-shadow-[0_0_8px_#ffb020]' : 'text-cyan-300'
          }`}
          viewBox="0 0 200 180"
          style={{
            transform: `rotateY(${angle % 40}deg) ${exploded ? 'scale(1.15)' : 'scale(1)'}`,
            transformStyle: 'preserve-3d'
          }}
        >
          {/* Base */}
          <ellipse
            cx="100"
            cy="155"
            rx={exploded ? 50 : 42}
            ry="12"
            strokeDasharray="3 2"
            strokeWidth="1.5"
            className="text-cyan-500"
          />
          <ellipse cx="100" cy="150" rx="34" ry="9" strokeWidth="1.8" />

          {/* Turret / Lower Joint */}
          <rect
            x="88"
            y={exploded ? 122 : 132}
            width="24"
            height="18"
            rx="2"
            strokeWidth="1.5"
          />
          <circle
            cx="100"
            cy={exploded ? 130 : 140}
            r="4"
            strokeWidth="1.5"
            className="text-amber-400 fill-amber-400/20"
          />

          {/* Arm Section 1 */}
          <line
            x1="100"
            y1={exploded ? 122 : 132}
            x2={exploded ? 135 : 125}
            y2={exploded ? 82 : 92}
            strokeWidth="2.5"
          />
          <line
            x1="94"
            y1={exploded ? 124 : 134}
            x2={exploded ? 129 : 119}
            y2={exploded ? 84 : 94}
            strokeDasharray="2 2"
            strokeWidth="1"
            className="text-cyan-600"
          />

          {/* Middle Pivot Joint */}
          <circle
            cx={exploded ? 135 : 125}
            cy={exploded ? 82 : 92}
            r="7"
            strokeWidth="2"
            className="text-cyan-200"
          />
          <circle
            cx={exploded ? 135 : 125}
            cy={exploded ? 82 : 92}
            r="2.5"
            className="fill-cyan-400"
          />

          {/* Arm Section 2 */}
          <line
            x1={exploded ? 135 : 125}
            y1={exploded ? 82 : 92}
            x2={exploded ? 70 : 80}
            y2={exploded ? 42 : 52}
            strokeWidth="2.5"
          />
          <line
            x1={exploded ? 139 : 129}
            y1={exploded ? 78 : 88}
            x2={exploded ? 74 : 84}
            y2={exploded ? 38 : 48}
            strokeDasharray="2 2"
            strokeWidth="1"
            className="text-cyan-600"
          />

          {/* Wrist Joint */}
          <circle
            cx={exploded ? 70 : 80}
            cy={exploded ? 42 : 52}
            r="5"
            strokeWidth="2"
            className="text-amber-300"
          />

          {/* End Effector / Claw Gripper */}
          <path
            d={exploded ? 'M70,42 L48,28 L30,35' : 'M80,52 L60,40 L45,45'}
            strokeLinecap="round"
            strokeWidth="2"
          />
          <path
            d={exploded ? 'M70,42 L52,58 L38,55' : 'M80,52 L65,65 L52,62'}
            strokeLinecap="round"
            strokeWidth="2"
          />
          <circle cx={exploded ? 30 : 45} cy={exploded ? 35 : 45} r="2" className="fill-amber-400" />
          <circle cx={exploded ? 38 : 52} cy={exploded ? 55 : 62} r="2" className="fill-amber-400" />

          {/* Target Focal Ring Around Claw */}
          <circle
            cx={exploded ? 34 : 50}
            cy={exploded ? 45 : 52}
            r="18"
            strokeDasharray="2 2"
            strokeWidth="0.8"
            className="text-cyan-400 animate-spin-slow"
          />
        </svg>

        {/* Telemetry Markers */}
        <div className="absolute top-2 left-2 text-[9px] font-['JetBrains_Mono'] text-cyan-400 bg-black/60 px-1.5 py-0.5 rounded border border-cyan-500/30">
          EIXO ROTACIONAL: <span className="text-white font-bold">{angle}°</span>
        </div>
        <div className="absolute bottom-2 right-2 text-[9px] font-['JetBrains_Mono'] text-[var(--secondary)] bg-black/60 px-1.5 py-0.5 rounded border border-amber-500/30">
          {nanotechLayer ? 'CAMADA NANOTECH // ATIVA' : 'GARRA DE TITÂNIO // ATIVO'}
        </div>
      </div>

      {/* Control Buttons */}
      <div className="mt-2.5 grid grid-cols-3 gap-1 text-[11px] font-['JetBrains_Mono']">
        <button
          onClick={toggleExploded}
          className={`px-1.5 py-1 rounded border transition-all truncate text-center ${
            exploded
              ? 'bg-cyan-500 text-black font-bold border-cyan-400'
              : 'bg-cyan-950/50 border-cyan-500/30 text-cyan-300 hover:border-cyan-400'
          }`}
        >
          {exploded ? 'Montagem' : 'Explodida'}
        </button>

        <button
          onClick={toggleRotate}
          className={`px-1.5 py-1 rounded border transition-all truncate text-center ${
            isRotating
              ? 'bg-cyan-950/80 border-cyan-400 text-cyan-200'
              : 'bg-black/50 border-cyan-500/20 text-cyan-500'
          }`}
        >
          {isRotating ? 'Pausar 360°' : 'Girar 360°'}
        </button>

        <button
          onClick={toggleNanotech}
          className={`px-1.5 py-1 rounded border transition-all truncate text-center ${
            nanotechLayer
              ? 'bg-amber-500 text-black font-bold border-amber-400'
              : 'bg-amber-950/40 border-amber-500/40 text-amber-300 hover:border-amber-400'
          }`}
        >
          Nanotech
        </button>
      </div>
    </section>
  );
}