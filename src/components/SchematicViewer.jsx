import React, { useState, useRef } from 'react';
import { Box, Upload } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { SynAccent } from './synapse';

export default function SchematicViewer() {
  const [mode, setMode] = useState('3d');
  const [mediaSrc, setMediaSrc] = useState(null);
  const fileInputRef = useRef(null);

  const handleFileUpload = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    sounds.playSuccess();
    const url = URL.createObjectURL(file);
    setMediaSrc(url);
    setMode(file.type.startsWith('video/') ? 'video' : 'image');
  };

  return (
    <section className="syn-panel p-4 flex-1 flex flex-col group">
      <SynAccent />
      <div className="flex items-center justify-between pb-2 mb-2 font-syn-body" style={{ borderBottom: '1px solid rgba(45,253,212,0.15)' }}>
        <h2 className="font-syn-title text-xs font-medium tracking-wide flex items-center gap-1.5" style={{ color: '#2dfdd4' }}>
          <Box className="w-3.5 h-3.5" /> Membrana visual
        </h2>
        <div className="flex items-center gap-1">
          <button onClick={() => { sounds.playClick(); setMode('3d'); }} className="syn-pill font-syn-body px-2.5 py-1 text-[10px]" style={mode === '3d' ? { background: 'rgba(45,253,212,0.2)', color: '#eaf8f4' } : { color: '#9aa6c9', border: '1px solid rgba(45,253,212,0.2)' }}>
            Sinapse
          </button>
          <button onClick={() => fileInputRef.current?.click()} title="Carregar foto ou vídeo" className="syn-pill font-syn-body px-2.5 py-1 text-[10px] flex items-center gap-1" style={{ color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.3)' }}>
            <Upload className="w-2.5 h-2.5" /> Mídia
          </button>
          <input type="file" ref={fileInputRef} onChange={handleFileUpload} accept="image/*,video/*" className="hidden" />
        </div>
      </div>

      <div className="relative flex-1 min-h-[170px] flex items-center justify-center p-2 overflow-hidden select-none" style={{ background: 'rgba(5,3,17,0.5)', border: '1px solid rgba(138,92,255,0.25)', borderRadius: '22px 26px 20px 28px' }}>
        {mode === '3d' && (
          <svg className="w-4/5 h-full" viewBox="0 0 300 190" aria-hidden>
            {[
              [60, 60, 120, 40, 170, 70],
              [70, 130, 130, 110, 190, 130],
              [120, 40, 150, 100, 130, 160],
              [200, 50, 220, 110, 190, 160],
            ].map(([x1, y1, x2, y2, x3, y3], i) => (
              <path key={i} d={`M ${x1},${y1} C ${(x1 + x2) / 2},${y1} ${(x1 + x2) / 2},${y2} ${x2},${y2} S ${(x2 + x3) / 2},${y3} ${x3},${y3}`}
                fill="none" stroke={i % 2 ? 'rgba(45,253,212,0.5)' : 'rgba(255,62,200,0.45)'} strokeWidth="1.6" strokeLinecap="round"
                style={{ filter: 'drop-shadow(0 0 4px rgba(138,92,255,0.5))' }} />
            ))}
            {[[60, 60], [120, 40], [170, 70], [70, 130], [130, 110], [190, 130], [150, 100], [220, 110]].map(([x, y], i) => (
              <g key={i}>
                <circle cx={x} cy={y} r="8" fill={i % 2 ? '#ff3ec8' : '#2dfdd4'} opacity="0.15" />
                <circle cx={x} cy={y} r="3.5" fill="#0a0620" stroke={i % 2 ? '#ff3ec8' : '#2dfdd4'} strokeWidth="1.6" style={{ filter: `drop-shadow(0 0 5px ${i % 2 ? '#ff3ec8' : '#2dfdd4'})` }} />
              </g>
            ))}
          </svg>
        )}
        {mode === 'image' && mediaSrc && (
          <img src={mediaSrc} alt="Membrana" className="max-h-full max-w-full object-contain" style={{ borderRadius: '18px 22px 16px 24px', filter: 'drop-shadow(0 0 10px rgba(45,253,212,0.35))' }} />
        )}
        {mode === 'video' && mediaSrc && (
          <video src={mediaSrc} autoPlay loop muted playsInline className="max-h-full max-w-full object-contain" style={{ borderRadius: '18px 22px 16px 24px' }} />
        )}
        <div className="absolute top-2 left-2 font-syn-body text-[10px] syn-pill px-2 py-0.5" style={{ background: 'rgba(5,3,17,0.7)', border: '1px solid rgba(45,253,212,0.25)', color: '#9aa6c9' }}>
          filamento · <strong style={{ color: '#eaf8f4' }}>{mode}</strong>
        </div>
      </div>
    </section>
  );
}
