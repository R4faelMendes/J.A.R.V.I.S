import React from 'react';
import { Mic, MicOff, Activity, Radio, Volume2 } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function TerminalInput({
  isListening,
  onToggleMic,
}) {
  return (
    <footer className="hud-panel rounded-lg p-3 border-[var(--panel-border)] relative z-20 flex items-center justify-between gap-4" data-purpose="command-console">
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Left status & voice indicator */}
      <div className="flex items-center space-x-3 font-['JetBrains_Mono']">
        <div className={`p-2 rounded-full border transition-all ${
          isListening 
            ? 'bg-red-500/20 border-red-500 text-red-400 shadow-[0_0_15px_rgba(239,68,68,0.5)]' 
            : 'bg-cyan-950/40 border-cyan-500/30 text-cyan-400'
        }`}>
          <Radio className={`w-4 h-4 ${isListening ? 'animate-pulse' : ''}`} />
        </div>
        <div>
          <div className="text-xs font-bold uppercase tracking-wider flex items-center gap-2">
            <span className={`w-2 h-2 rounded-full ${isListening ? 'bg-red-500 animate-ping' : 'bg-cyan-400'}`}></span>
            <span className={isListening ? 'text-red-400' : 'text-cyan-300'}>
              {isListening ? 'TRANSMISSÃO VOCAL ATIVA // ESCUTANDO COMANDOS' : 'MODO DE ESPERA VOCAL // STANDBY'}
            </span>
          </div>
          <div className="text-[10px] text-cyan-500/80">
            FALE NATURALMENTE COM O J.A.R.V.I.S. • COMANDOS DIRETOS DE SISTEMA
          </div>
        </div>
      </div>

      {/* Right Voice Control Toggle Button */}
      <div className="flex items-center space-x-3">
        <button
          onClick={() => {
            sounds.playClick();
            onToggleMic();
          }}
          className={`px-5 py-2 rounded-lg font-['Orbitron'] font-bold text-xs uppercase transition-all flex items-center gap-2 border ${
            isListening
              ? 'bg-red-500 hover:bg-red-600 text-white border-red-400 shadow-[0_0_20px_rgba(239,68,68,0.6)] animate-pulse'
              : 'bg-[var(--primary)] hover:bg-white text-black border-cyan-300 shadow-[0_0_15px_var(--primary-glow)]'
          }`}
        >
          {isListening ? (
            <>
              <MicOff className="w-4 h-4" /> Desativar Microfone
            </>
          ) : (
            <>
              <Mic className="w-4 h-4" /> Ativar Microfone
            </>
          )}
        </button>
      </div>
    </footer>
  );
}