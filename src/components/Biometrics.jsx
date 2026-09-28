import React, { useState, useEffect } from 'react';
import { Activity } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { SynSparkline, SynAccent } from './synapse';

/** Biometria do organismo: curvas suaves com glow, sem segmentos rígidos. */
export default function Biometrics() {
  const [pulse, setPulse] = useState([62, 68, 71, 69, 74, 76, 73, 75]);
  const [resp, setResp] = useState([40, 44, 42, 46, 45, 48, 47, 49]);
  const [focus, setFocus] = useState([55, 60, 58, 63, 66, 64, 68, 70]);

  useEffect(() => {
    const t = setInterval(() => {
      const jitter = (base) => Math.max(20, Math.min(95, base + Math.round((Math.random() - 0.5) * 10)));
      setPulse((p) => [...p.slice(-11), jitter(p[p.length - 1])]);
      setResp((p) => [...p.slice(-11), jitter(p[p.length - 1])]);
      setFocus((p) => [...p.slice(-11), jitter(p[p.length - 1])]);
    }, 2500);
    return () => clearInterval(t);
  }, []);

  const rows = [
    { label: 'Pulso neural', value: `${pulse[pulse.length - 1]} bpm`, hist: pulse, color: '#2dfdd4', glow: 'rgba(45,253,212,0.55)' },
    { label: 'Ritmo respiratório', value: `${resp[resp.length - 1]} rpm`, hist: resp, color: '#8a5cff', glow: 'rgba(138,92,255,0.5)' },
    { label: 'Foco do operador', value: `${focus[focus.length - 1]}%`, hist: focus, color: '#ff3ec8', glow: 'rgba(255,62,200,0.55)' },
  ];

  return (
    <section onClick={() => sounds.playClick()} className="syn-panel p-4 flex-1 flex flex-col">
      <SynAccent />
      <div className="flex items-center justify-between pb-2 mb-3" style={{ borderBottom: '1px solid rgba(45,253,212,0.15)' }}>
        <h2 className="font-syn-title text-xs font-medium tracking-wide flex items-center gap-1.5" style={{ color: '#2dfdd4' }}>
          <Activity className="w-3.5 h-3.5" /> Biometria viva
        </h2>
        <span className="syn-pill font-syn-body text-[10px] px-2 py-0.5" style={{ border: '1px solid rgba(45,253,212,0.35)', color: '#2dfdd4' }}>sincronizado</span>
      </div>
      <div className="space-y-4 font-syn-body text-xs">
        {rows.map((r) => (
          <div key={r.label}>
            <div className="flex justify-between mb-1">
              <span className="font-light" style={{ color: '#9aa6c9' }}>{r.label}</span>
              <span className="font-syn-title font-medium" style={{ color: r.color }}>{r.value}</span>
            </div>
            <SynSparkline values={r.hist} color={r.color} glow={r.glow} />
          </div>
        ))}
      </div>
    </section>
  );
}
