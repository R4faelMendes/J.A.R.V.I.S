import React, { useState, useEffect, useMemo } from 'react';
import { Layers, XCircle, RefreshCw } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { biometricService } from '../services/biometricService';
import { SynAccent } from './synapse';

export default function TacticalMap() {
  const [activeApps, setActiveApps] = useState([
    { id: 'code', name: 'Code', process: 'Code.exe', pid: '1420', memory: '185.000 K', status: 'Ativo' },
    { id: 'brave', name: 'Brave', process: 'brave.exe', pid: '8920', memory: '320.000 K', status: 'Ativo' },
    { id: 'electron', name: 'J.A.R.V.I.S.', process: 'electron.exe', pid: '4102', memory: '120.000 K', status: 'Ativo' },
  ]);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const fetchProcesses = async () => {
    setIsRefreshing(true);
    if (window.electronAPI?.getRunningProcesses) {
      try {
        const list = await window.electronAPI.getRunningProcesses();
        if (list && list.length > 0) setActiveApps(list);
      } catch (e) { console.warn('Erro ao ler processos:', e); }
    }
    setTimeout(() => setIsRefreshing(false), 600);
  };

  useEffect(() => {
    fetchProcesses();
    const interval = setInterval(fetchProcesses, 8000);
    return () => clearInterval(interval);
  }, []);

  const graph = useMemo(() => {
    const shown = activeApps.slice(0, 7);
    const W = 560; const H = 150;
    const cx = W / 2; const cy = H / 2;
    const nodes = [{ id: 'core', label: 'Núcleo', x: cx, y: cy, size: 9, color: '#2dfdd4' }];
    const links = [];
    shown.forEach((app, i) => {
      const a = (i / Math.max(1, shown.length)) * Math.PI * 2 - Math.PI / 2;
      const x = cx + Math.cos(a) * 190;
      const y = cy + Math.sin(a) * 52;
      const id = String(app.pid || app.id);
      nodes.push({ id, label: app.name.slice(0, 12), x, y, size: 5, color: i % 3 === 0 ? '#ff3ec8' : i % 3 === 1 ? '#2dfdd4' : '#8a5cff' });
      links.push({ from: 'core', to: id });
    });
    return { nodes, links, W, H };
  }, [activeApps]);

  const handleKillProcess = async (app) => {
    sounds.playAlert();
    const unlock = await biometricService.unlock(`encerrar ${app.name} (${app.process})`);
    if (!unlock.ok) return;
    if (window.electronAPI?.processControl) window.electronAPI.processControl('KILL', app.process);
    setActiveApps((prev) => prev.filter((a) => a.pid !== app.pid));
  };

  return (
    <section className="syn-panel p-4 flex flex-col justify-between">
      <SynAccent />
      <div className="flex items-center justify-between pb-2 mb-2" style={{ borderBottom: '1px solid rgba(45,253,212,0.15)' }}>
        <h2 className="font-syn-title text-xs font-medium tracking-wide flex items-center gap-1.5" style={{ color: '#2dfdd4' }}>
          <Layers className="w-3.5 h-3.5" /> Tecido de processos
        </h2>
        <div className="flex items-center gap-1.5">
          <button onClick={fetchProcesses} title="Atualizar" className="syn-pill p-1.5" style={{ border: '1px solid rgba(45,253,212,0.3)', color: '#2dfdd4' }}>
            <RefreshCw className={`w-3 h-3 ${isRefreshing ? 'animate-spin' : ''}`} />
          </button>
          <button onClick={() => { sounds.playPowerPulse(); fetchProcesses(); }} className="syn-pill font-syn-body text-[10px] px-2.5 py-1" style={{ border: '1px solid rgba(45,253,212,0.3)', color: '#2dfdd4' }}>
            Harmonizar
          </button>
        </div>
      </div>

      <svg viewBox={`0 0 ${graph.W} ${graph.H}`} className="w-full h-32 mb-2" aria-hidden>
        {graph.links.map((l, i) => {
          const a = graph.nodes.find((n) => n.id === l.from);
          const b = graph.nodes.find((n) => n.id === l.to);
          if (!a || !b) return null;
          const mx = (a.x + b.x) / 2;
          return <path key={i} d={`M ${a.x},${a.y} C ${mx},${a.y} ${mx},${b.y} ${b.x},${b.y}`} fill="none" stroke="rgba(45,253,212,0.35)" strokeWidth="1.2" strokeLinecap="round" style={{ filter: 'drop-shadow(0 0 3px rgba(138,92,255,0.5))' }} />;
        })}
        {graph.nodes.map((n) => (
          <g key={n.id}>
            <circle cx={n.x} cy={n.y} r={n.size + 4} fill={n.color} opacity="0.15" />
            <circle cx={n.x} cy={n.y} r={n.size} fill="#0a0620" stroke={n.color} strokeWidth="1.8" style={{ filter: `drop-shadow(0 0 5px ${n.color})` }} />
            <text x={n.x} y={n.y + n.size + 11} textAnchor="middle" fontSize="9" fill="#9aa6c9" fontFamily="Manrope, sans-serif">{n.label}</text>
          </g>
        ))}
      </svg>

      <div className="space-y-1.5 font-syn-body text-xs max-h-48 overflow-y-auto">
        {activeApps.map((app) => (
          <div key={app.pid || app.id} className="p-2 flex items-center justify-between" style={{ borderRadius: '16px 14px 18px 12px', background: 'rgba(10,6,32,0.5)', border: '1px solid rgba(45,253,212,0.15)' }}>
            <div className="overflow-hidden">
              <div className="font-medium truncate text-[11px] flex items-center gap-1.5" style={{ color: '#eaf8f4' }}>
                <span className="w-1.5 h-1.5 syn-core" style={{ borderRadius: '50%', background: '#2dfdd4', boxShadow: '0 0 6px #2dfdd4' }} />
                {app.name}
                {app.pid && <span className="syn-pill text-[9px] px-1.5" style={{ border: '1px solid rgba(138,92,255,0.35)', color: '#9aa6c9' }}>pid {app.pid}</span>}
              </div>
              <div className="text-[10px] truncate font-light" style={{ color: '#9aa6c9' }}>{app.process} {app.memory && <span style={{ color: '#ffcf5c' }}>{app.memory}</span>}</div>
            </div>
            <button onClick={() => handleKillProcess(app)} title={`Encerrar ${app.name}`} className="p-1.5 syn-pill" style={{ color: '#ff5c7a' }}>
              <XCircle className="w-4 h-4" />
            </button>
          </div>
        ))}
      </div>

      <div className="mt-2 pt-2 flex items-center justify-between font-syn-body text-[11px] font-light" style={{ borderTop: '1px solid rgba(45,253,212,0.12)', color: '#9aa6c9' }}>
        <span>{activeApps.length} filamentos monitorados</span>
        <span style={{ color: '#2dfdd4' }}>tecido vivo</span>
      </div>
    </section>
  );
}
