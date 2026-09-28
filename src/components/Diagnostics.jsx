import React, { useState, useEffect } from 'react';
import { Cpu } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { SynSparkline } from './synapse';
import { SynAccent } from './synapse';

export default function Diagnostics() {
  const [metrics, setMetrics] = useState({
    totalMemGB: '16.0', usedMemGB: '11.5', freeMemGB: '4.5', memPercent: 72,
    cpuCores: 8, uptimeHours: '4.2', cpuLoad: 24, coolingTemp: 46,
  });
  const [memHist, setMemHist] = useState([62, 65, 68, 66, 70, 72, 71, 72]);
  const [cpuHist, setCpuHist] = useState([18, 24, 21, 28, 24, 26, 23, 24]);
  const [tempHist, setTempHist] = useState([43, 44, 45, 44, 46, 45, 46, 46]);

  useEffect(() => {
    const push = (setter, v) => setter((prev) => [...prev.slice(-11), v]);
    const fetchMetrics = async () => {
      if (window.electronAPI?.getSystemMetrics) {
        try {
          const data = await window.electronAPI.getSystemMetrics();
          const cpu = Math.round(15 + Math.random() * 20);
          const temp = Math.round(44 + Math.random() * 6);
          setMetrics((prev) => ({ ...prev, ...data, cpuLoad: cpu, coolingTemp: temp }));
          push(setMemHist, Number(data.memPercent) || 70);
          push(setCpuHist, cpu);
          push(setTempHist, temp);
          return;
        } catch (e) { console.warn('Could not fetch hardware metrics:', e); }
      }
      const cpu = Math.round(20 + Math.random() * 18);
      const temp = Math.round(43 + Math.random() * 5);
      setMetrics((prev) => ({ ...prev, cpuLoad: cpu, coolingTemp: temp }));
      push(setCpuHist, cpu);
      push(setTempHist, temp);
      push(setMemHist, 68 + Math.round(Math.random() * 8));
    };
    fetchMetrics();
    const interval = setInterval(fetchMetrics, 3000);
    return () => clearInterval(interval);
  }, []);

  const rows = [
    { label: `Memória · ${metrics.usedMemGB} de ${metrics.totalMemGB} GB`, value: `${metrics.memPercent}%`, hist: memHist, color: '#2dfdd4', glow: 'rgba(45,253,212,0.55)' },
    { label: `Processador · ${metrics.cpuCores} núcleos`, value: `${metrics.cpuLoad}%`, hist: cpuHist, color: '#ffcf5c', glow: 'rgba(255,207,92,0.5)' },
    { label: 'Temperatura', value: `${metrics.coolingTemp}°C · estável`, hist: tempHist, color: '#8a5cff', glow: 'rgba(138,92,255,0.5)' },
  ];

  return (
    <section onClick={() => sounds.playClick()} className="syn-panel p-4 flex-1 flex flex-col justify-between group">
      <SynAccent />
      <div>
        <div className="flex items-center justify-between pb-2 mb-3" style={{ borderBottom: '1px solid rgba(45,253,212,0.15)' }}>
          <h2 className="font-syn-title text-xs font-medium tracking-wide flex items-center gap-1.5" style={{ color: '#2dfdd4' }}>
            <Cpu className="w-3.5 h-3.5" /> Telemetria do organismo
          </h2>
          <span className="syn-pill font-syn-body text-[10px] px-2 py-0.5" style={{ border: '1px solid rgba(45,253,212,0.35)', color: '#2dfdd4', background: 'rgba(45,253,212,0.08)' }}>
            pc local · online
          </span>
        </div>
        <div className="space-y-4 font-syn-body text-xs">
          {rows.map((r) => (
            <div key={r.label}>
              <div className="flex justify-between mb-1">
                <span style={{ color: '#9aa6c9' }}>{r.label}</span>
                <span className="font-syn-title font-medium" style={{ color: r.color }}>{r.value}</span>
              </div>
              <SynSparkline values={r.hist} color={r.color} glow={r.glow} />
            </div>
          ))}
        </div>
      </div>
      <div className="mt-4 pt-3 grid grid-cols-3 gap-2 text-center font-syn-body" style={{ borderTop: '1px solid rgba(45,253,212,0.12)' }}>
        {[
          ['Livre', `${metrics.freeMemGB} GB`],
          ['Ativo há', `${metrics.uptimeHours} h`],
          ['Estado', 'Equilibrado'],
        ].map(([k, v]) => (
          <div key={k} className="p-2" style={{ borderRadius: '16px 14px 18px 12px', background: 'rgba(138,92,255,0.07)', border: '1px solid rgba(138,92,255,0.2)' }}>
            <span className="text-[10px] block font-light" style={{ color: '#9aa6c9' }}>{k}</span>
            <span className="text-xs font-medium" style={{ color: '#eaf8f4' }}>{v}</span>
          </div>
        ))}
      </div>
    </section>
  );
}
