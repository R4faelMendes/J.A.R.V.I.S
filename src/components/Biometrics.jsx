import React, { useState, useEffect, useRef } from 'react';
import { Heart, Activity, Zap } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function Biometrics() {
  const [bpm, setBpm] = useState(72);
  const [o2, setO2] = useState(99.2);
  const [sync, setSync] = useState(98.8);
  const canvasRef = useRef(null);

  // Dynamic heartbeat & ECG canvas rendering
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    let animationId;
    let offset = 0;

    const render = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.lineWidth = 2;
      ctx.strokeStyle = '#00f0ff';
      ctx.shadowBlur = 8;
      ctx.shadowColor = '#00f0ff';

      ctx.beginPath();
      const height = canvas.height;
      const width = canvas.width;
      const midY = height / 2;

      for (let x = 0; x < width; x++) {
        const t = (x + offset) % 180;
        let y = midY;

        // Realistic P-Q-R-S-T wave pattern
        if (t > 40 && t <= 50) {
          y -= 6; // P wave
        } else if (t > 50 && t <= 60) {
          y += 3; // Q wave
        } else if (t > 60 && t <= 75) {
          y -= 22; // R spike peak
        } else if (t > 75 && t <= 85) {
          y += 10; // S wave valley
        } else if (t > 95 && t <= 115) {
          y -= 8; // T wave
        }

        if (x === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();

      offset += 1.6;
      animationId = requestAnimationFrame(render);
    };

    render();

    // Subtle natural fluctuations
    const pulseInterval = setInterval(() => {
      setBpm(prev => {
        const delta = (Math.random() - 0.48) * 2;
        return Math.round(Math.min(84, Math.max(68, prev + delta)));
      });
      setO2(prev => +(98.8 + Math.random() * 0.8).toFixed(1));
      setSync(prev => +(98.2 + Math.random() * 1.2).toFixed(1));
    }, 2400);

    return () => {
      cancelAnimationFrame(animationId);
      clearInterval(pulseInterval);
    };
  }, []);

  return (
    <section 
      onClick={() => sounds.playClick()}
      className="hud-panel rounded-lg p-3 sm:p-4 border-[var(--panel-border)] transition-all cursor-pointer group"
    >
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Header */}
      <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2 mb-3">
        <h2 className="text-xs uppercase font-['Orbitron'] tracking-wider text-[var(--primary)] flex items-center gap-2 glow-cyan">
          <Heart className="w-3.5 h-3.5 text-[var(--primary)] animate-pulse" />
          Telemetria Biocibernética
        </h2>
        <span className="text-[10px] text-emerald-400 font-['JetBrains_Mono'] px-1.5 py-0.5 border border-emerald-500/40 rounded bg-emerald-950/40 flex items-center gap-1">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping"></span>
          ONLINE
        </span>
      </div>

      {/* Waveform & Heart Rate */}
      <div className="space-y-3 text-xs font-['JetBrains_Mono']">
        <div>
          <div className="flex justify-between text-cyan-300 text-[11px] mb-1">
            <span className="flex items-center gap-1">
              <Activity className="w-3 h-3 text-[var(--primary)]" />
              Frequência Cardíaca
            </span>
            <span className="text-[var(--primary)] font-bold glow-cyan">
              {bpm} BPM // RITMO SENOIDAL
            </span>
          </div>

          {/* ECG Canvas Waveform */}
          <div className="w-full h-10 bg-cyan-950/20 rounded border border-cyan-500/30 overflow-hidden relative shadow-inner">
            <canvas ref={canvasRef} width={280} height={40} className="w-full h-full" />
            <div className="absolute top-1 right-2 text-[8px] text-cyan-400/50">LEAD-II ECG</div>
          </div>
        </div>

        {/* Metric Cards */}
        <div className="grid grid-cols-2 gap-2 pt-1">
          <div className="p-2 rounded bg-cyan-950/30 border border-cyan-500/20 group-hover:border-[var(--primary)] transition-all">
            <span className="text-[10px] text-cyan-400/70 block">Saturação O₂</span>
            <span className="text-base font-['Orbitron'] font-bold text-cyan-200 glow-cyan">
              {o2}%
            </span>
          </div>
          <div className="p-2 rounded bg-cyan-950/30 border border-cyan-500/20 group-hover:border-[var(--primary)] transition-all">
            <span className="text-[10px] text-cyan-400/70 block">Sincronia Neural</span>
            <span className="text-base font-['Orbitron'] font-bold text-cyan-200 glow-cyan">
              {sync}%
            </span>
          </div>
        </div>
      </div>
    </section>
  );
}