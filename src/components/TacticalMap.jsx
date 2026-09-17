import React, { useState, useEffect } from 'react';
import { Layers, XCircle, Sparkles, RefreshCw } from 'lucide-react';
import { sounds } from '../services/soundEffects';

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
        if (list && list.length > 0) {
          setActiveApps(list);
        }
      } catch (e) {
        console.warn('Erro ao ler processos do Windows:', e);
      }
    }
    setTimeout(() => setIsRefreshing(false), 600);
  };

  useEffect(() => {
    fetchProcesses();
    const interval = setInterval(fetchProcesses, 5000);
    return () => clearInterval(interval);
  }, []);

  const handleKillProcess = (app) => {
    sounds.playAlert();
    if (window.electronAPI?.processControl) {
      window.electronAPI.processControl('KILL', app.process);
    }
    setActiveApps(prev => prev.filter(a => a.pid !== app.pid));
  };

  const handleOptimizeRAM = () => {
    sounds.playPowerPulse();
    fetchProcesses();
  };

  return (
    <section className="hud-panel rounded-lg p-3 sm:p-4 border-[var(--panel-border)] flex flex-col justify-between">
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Header */}
      <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2 mb-2 font-['JetBrains_Mono']">
        <h2 className="text-xs uppercase font-['Orbitron'] tracking-wider text-[var(--primary)] flex items-center gap-1.5 glow-cyan">
          <Layers className="w-3.5 h-3.5 text-[var(--primary)]" />
          Gerenciador de Tarefas do Windows
        </h2>
        <div className="flex items-center space-x-1">
          <button
            onClick={fetchProcesses}
            title="Atualizar lista de processos"
            className="text-[10px] bg-cyan-950/40 hover:bg-[var(--primary)] hover:text-black p-1 rounded border border-cyan-500/30 text-cyan-300 transition-colors"
          >
            <RefreshCw className={`w-3 h-3 ${isRefreshing ? 'animate-spin' : ''}`} />
          </button>
          <button
            onClick={handleOptimizeRAM}
            className="text-[10px] bg-cyan-950/40 hover:bg-[var(--primary)] hover:text-black px-1.5 py-0.5 rounded border border-cyan-500/30 text-cyan-300 transition-colors flex items-center gap-1"
          >
            <Sparkles className="w-2.5 h-2.5" /> Otimizar
          </button>
        </div>
      </div>

      {/* Process list */}
      <div className="space-y-1.5 font-['JetBrains_Mono'] text-xs max-h-48 overflow-y-auto scrollbar-none">
        {activeApps.map(app => (
          <div
            key={app.pid || app.id}
            className="p-1.5 rounded bg-cyan-950/20 border border-cyan-500/20 flex items-center justify-between hover:border-[var(--primary)] transition-all"
          >
            <div className="overflow-hidden">
              <div className="font-semibold text-cyan-200 truncate text-[11px] flex items-center gap-1.5">
                <span>{app.name}</span>
                {app.pid && (
                  <span className="text-[9px] px-1 py-0.2 rounded bg-black/50 text-cyan-400 border border-cyan-500/30">
                    PID {app.pid}
                  </span>
                )}
              </div>
              <div className="text-[9px] text-cyan-500 truncate flex items-center gap-2">
                <span>{app.process}</span>
                {app.memory && <span className="text-amber-300 font-semibold">{app.memory}</span>}
              </div>
            </div>

            <button
              onClick={() => handleKillProcess(app)}
              title={`Finalizar ${app.name}`}
              className="p-1 text-rose-400/70 hover:text-rose-400 hover:bg-rose-950/40 rounded transition-colors ml-2"
            >
              <XCircle className="w-4 h-4" />
            </button>
          </div>
        ))}
      </div>

      {/* Quick Status */}
      <div className="mt-2 pt-2 border-t border-cyan-500/20 flex items-center justify-between text-[10px] font-['JetBrains_Mono'] text-cyan-400/80">
        <span>{activeApps.length} APPS MONITORADOS</span>
        <span className="text-emerald-400 font-bold">PROCESSO REAL DO PC</span>
      </div>
    </section>
  );
}