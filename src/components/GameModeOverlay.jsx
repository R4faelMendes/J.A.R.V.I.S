import React from 'react';
import { Gamepad2, Power, Zap, Mic, ShieldAlert } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function GameModeOverlay({ isActive, onWakeUp, isListening, onToggleMic }) {
  if (!isActive) return null;

  return (
    <div className="fixed inset-0 z-40 bg-black/95 flex flex-col items-center justify-between p-6 select-none animate-in fade-in duration-300">
      {/* Top minimal status */}
      <div className="w-full flex items-center justify-between border-b border-amber-500/20 pb-3 font-['JetBrains_Mono'] text-xs">
        <div className="flex items-center space-x-2 text-amber-400 font-bold">
          <Gamepad2 className="w-5 h-5 animate-pulse" />
          <span className="font-['Orbitron'] text-sm tracking-wider">MODO HIBERNAÇÃO // OTIMIZAÇÃO PARA JOGOS ATIVA</span>
        </div>
        <div className="text-emerald-400 font-bold flex items-center gap-1.5 bg-emerald-950/40 px-3 py-1 rounded border border-emerald-500/30">
          <span className="w-2 h-2 rounded-full bg-emerald-400 animate-ping"></span>
          2.8 GB RAM &amp; 95% GPU LIBERADOS
        </div>
      </div>

      {/* Center Minimalist Pulsing Ring */}
      <div className="flex flex-col items-center space-y-6 text-center">
        <div 
          onClick={() => { sounds.playSuccess(); onWakeUp(); }}
          className="relative w-48 h-48 rounded-full border-2 border-dashed border-amber-400/40 flex items-center justify-center cursor-pointer hover:scale-105 transition-transform group"
          title="Clique para acordar o J.A.R.V.I.S."
        >
          <div className="absolute inset-4 rounded-full border border-amber-400/60 animate-spin-slow"></div>
          <div className="w-24 h-24 rounded-full bg-amber-950/40 border-2 border-amber-400 flex flex-col items-center justify-center text-amber-300 shadow-[0_0_25px_rgba(255,176,32,0.4)] group-hover:bg-amber-500 group-hover:text-black transition-colors">
            <Power className="w-8 h-8 mb-1" />
            <span className="text-[10px] font-['Orbitron'] font-bold">ACORDAR</span>
          </div>
        </div>

        <div className="space-y-2 font-['JetBrains_Mono'] max-w-md">
          <div className="text-sm font-bold text-amber-300 glow-amber">
            J.A.R.V.I.S. EM ESCUTA PASSIVA
          </div>
          <p className="text-xs text-amber-200/70">
            Renderizações complexas e atualizações de telemetria foram suspensas para garantir taxa máxima de quadros (FPS) no seu jogo.
          </p>
          <div className="text-[11px] text-cyan-300 bg-cyan-950/40 px-3 py-1.5 rounded border border-cyan-500/30 mt-2">
            💡 Diga: <span className="font-bold text-white">"Jarvis, acordar"</span> ou clique no núcleo acima.
          </div>
        </div>
      </div>

      {/* Bottom Minimal Bar */}
      <div className="w-full flex items-center justify-between border-t border-amber-500/20 pt-3 font-['JetBrains_Mono'] text-xs">
        <button
          onClick={() => { sounds.playClick(); onToggleMic(); }}
          className={`flex items-center gap-2 px-3 py-1.5 rounded border transition-all ${
            isListening 
              ? 'bg-amber-500 text-black font-bold border-amber-400' 
              : 'bg-black border-amber-500/30 text-amber-300 hover:border-amber-400'
          }`}
        >
          <Mic className="w-4 h-4" />
          <span>{isListening ? 'Microfone Ouvindo...' : 'Ativar Microfone'}</span>
        </button>

        <button
          onClick={() => { sounds.playSuccess(); onWakeUp(); }}
          className="px-5 py-1.5 rounded bg-amber-500 hover:bg-amber-400 text-black font-['Orbitron'] font-bold text-xs uppercase shadow-[0_0_15px_rgba(255,176,32,0.5)] transition-all"
        >
          Restaurar Painel Completo
        </button>
      </div>
    </div>
  );
}