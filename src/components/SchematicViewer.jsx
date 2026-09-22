import React, { useState, useRef } from 'react';
import { Box, Image, Video, Upload, RotateCw, X } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function SchematicViewer() {
  const [mode, setMode] = useState('3d'); // '3d', 'image', 'video'
  const [mediaSrc, setMediaSrc] = useState(null);
  const [angle, setAngle] = useState(314.2);
  const fileInputRef = useRef(null);

  const handleFileUpload = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    sounds.playSuccess();
    const url = URL.createObjectURL(file);
    setMediaSrc(url);
    if (file.type.startsWith('video/')) {
      setMode('video');
    } else {
      setMode('image');
    }
  };

  return (
    <section className="hud-panel rounded-2xl p-3 sm:p-4 flex-1 flex flex-col border-[var(--panel-border)] group">
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Header with Mode Switcher */}
      <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2 mb-2 font-['JetBrains_Mono']">
        <h2 className="text-xs uppercase font-['Orbitron'] tracking-wider text-[var(--primary)] flex items-center gap-1.5 glow-cyan">
          <Box className="w-3.5 h-3.5 text-[var(--primary)]" />
          Módulo Visual & Esquemático
        </h2>
        
        {/* Toggle Mode buttons */}
        <div className="flex items-center space-x-1">
          <button
            onClick={() => { sounds.playClick(); setMode('3d'); }}
            className={`px-1.5 py-0.5 rounded text-[10px] ${mode === '3d' ? 'bg-[var(--primary)] text-black font-bold' : 'text-cyan-400'}`}
          >
            3D
          </button>
          <button
            onClick={() => fileInputRef.current?.click()}
            title="Carregar Foto ou Vídeo"
            className="px-1.5 py-0.5 rounded text-[10px] bg-cyan-950/40 border border-cyan-500/30 text-cyan-300 hover:text-white flex items-center gap-1"
          >
            <Upload className="w-2.5 h-2.5" /> Mídia
          </button>
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileUpload}
            accept="image/*,video/*"
            className="hidden"
          />
        </div>
      </div>

      {/* Media Canvas Display */}
      <div className="relative flex-1 min-h-[170px] bg-cyan-950/20 rounded border border-cyan-500/30 flex items-center justify-center p-2 overflow-hidden select-none">
        <div className="absolute inset-0 cyber-grid opacity-30 pointer-events-none" />

        {mode === '3d' && (
          <svg className="w-4/5 h-4/5 text-cyan-300 stroke-current fill-none transition-all duration-700" viewBox="0 0 200 180">
            <ellipse cx="100" cy="155" rx="42" ry="12" strokeDasharray="3 2" strokeWidth="1.5" className="text-cyan-500" />
            <ellipse cx="100" cy="150" rx="34" ry="9" strokeWidth="1.8" />
            <rect x="88" y="132" width="24" height="18" rx="2" strokeWidth="1.5" />
            <circle cx="100" cy="140" r="4" strokeWidth="1.5" className="text-amber-400 fill-amber-400/20" />
            <line x1="100" y1="132" x2="125" y2="92" strokeWidth="2.5" />
            <circle cx="125" cy="92" r="7" strokeWidth="2" className="text-cyan-200" />
            <line x1="125" y1="92" x2="80" y2="52" strokeWidth="2.5" />
            <circle cx="80" cy="52" r="5" strokeWidth="2" className="text-amber-300" />
            <path d="M80,52 L60,40 L45,45" strokeLinecap="round" strokeWidth="2" />
            <path d="M80,52 L65,65 L52,62" strokeLinecap="round" strokeWidth="2" />
            <circle cx="50" cy="52" r="18" strokeDasharray="2 2" strokeWidth="0.8" className="text-cyan-400 animate-spin-slow" />
          </svg>
        )}

        {mode === 'image' && mediaSrc && (
          <div className="relative w-full h-full flex items-center justify-center">
            <img src={mediaSrc} alt="Hologram Preview" className="max-h-full max-w-full object-contain filter drop-shadow-[0_0_10px_var(--primary-glow)] rounded" />
            <div className="absolute inset-0 bg-gradient-to-b from-cyan-500/10 to-transparent pointer-events-none hologram-scan" />
          </div>
        )}

        {mode === 'video' && mediaSrc && (
          <div className="relative w-full h-full flex items-center justify-center">
            <video src={mediaSrc} autoPlay loop muted playsInline className="max-h-full max-w-full object-contain filter drop-shadow-[0_0_10px_var(--primary-glow)] rounded" />
            <div className="absolute inset-0 bg-gradient-to-b from-cyan-500/10 to-transparent pointer-events-none hologram-scan" />
          </div>
        )}

        {/* Telemetry Overlay */}
        <div className="absolute top-2 left-2 text-[9px] font-['JetBrains_Mono'] text-cyan-400 bg-black/60 px-1.5 py-0.5 rounded border border-cyan-500/30">
          FEED: <span className="text-white font-bold">{mode.toUpperCase()} ATIVO</span>
        </div>
      </div>
    </section>
  );
}