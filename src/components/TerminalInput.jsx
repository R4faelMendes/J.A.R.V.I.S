import React, { useState } from 'react';
import { Terminal, Shield, Zap, AlertTriangle, Send, Sparkles, Mic } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function TerminalInput({
  onExecuteCommand,
  isListening,
  onToggleMic,
  suggestions = ['Diagnóstico geral', 'Criar repo temperatura-mobile', 'Abrir no VS Code', 'Redirecionar energia', 'Entrar em hibernação']
}) {
  const [inputVal, setInputVal] = useState('');

  const handleSend = (textToSend) => {
    const query = (textToSend || inputVal).trim();
    if (!query) return;
    sounds.playClick();
    onExecuteCommand(query);
    setInputVal('');
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') {
      handleSend();
    }
  };

  return (
    <footer className="hud-panel rounded-lg p-3 border-[var(--panel-border)] relative z-20" data-purpose="command-console">
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      <div className="flex flex-col md:flex-row items-center justify-between gap-3">
        {/* Terminal Command Input Bar */}
        <div className="w-full md:flex-1 flex items-center space-x-2 bg-slate-950/80 rounded border border-cyan-500/40 px-3 py-1.5 focus-within:border-[var(--primary)] shadow-[inset_0_0_8px_var(--primary-dim)] transition-all">
          <span className="text-[var(--primary)] font-['JetBrains_Mono'] text-sm font-bold animate-pulse">
            &gt;
          </span>
          <input
            type="text"
            value={inputVal}
            onChange={(e) => setInputVal(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Aguardando diretiva vocal ou terminal (ex: 'Varredura', 'Abrir VS Code', 'Criar repositório')..."
            className="bg-transparent border-0 w-full text-xs sm:text-sm font-['JetBrains_Mono'] text-cyan-200 placeholder-cyan-600/70 focus:ring-0 focus:outline-none"
          />

          {/* Direct Send Button */}
          <button
            onClick={() => handleSend()}
            title="Enviar comando"
            className="p-1 text-cyan-400 hover:text-white transition-colors"
          >
            <Send className="w-3.5 h-3.5" />
          </button>

          <kbd className="hidden sm:inline-block px-1.5 py-0.5 text-[10px] font-['JetBrains_Mono'] text-cyan-400 bg-cyan-950/60 border border-cyan-500/30 rounded">
            ENTER
          </kbd>
        </div>

        {/* Rapid Action Triggers */}
        <div className="flex items-center flex-wrap gap-2 w-full md:w-auto justify-end">
          <button
            onClick={() => { sounds.playScan(); onExecuteCommand('varredura total'); }}
            className="px-3 py-1.5 text-xs font-['JetBrains_Mono'] font-bold uppercase rounded bg-cyan-950/60 border border-cyan-400/60 text-cyan-300 hover:bg-[var(--primary)] hover:text-black hover:shadow-[0_0_12px_var(--primary)] transition-all flex items-center gap-1.5"
            id="btn-scan"
          >
            <Zap className="w-3.5 h-3.5 text-cyan-400" />
            Varredura Total
          </button>

          <button
            onClick={() => { sounds.playPowerPulse(); onExecuteCommand('redirecionar energia para propulsores'); }}
            className="px-3 py-1.5 text-xs font-['JetBrains_Mono'] font-bold uppercase rounded bg-amber-950/60 border border-amber-400/60 text-amber-300 hover:bg-amber-500 hover:text-black hover:shadow-[0_0_12px_#ffb020] transition-all flex items-center gap-1.5"
            id="btn-power"
          >
            <Sparkles className="w-3.5 h-3.5 text-amber-400" />
            Redirecionar Energia
          </button>

          <button
            onClick={() => { sounds.playAlert(); onExecuteCommand('protocolo sentinela'); }}
            className="px-3 py-1.5 text-xs font-['JetBrains_Mono'] font-bold uppercase rounded bg-red-950/50 border border-red-400/60 text-red-300 hover:bg-red-500 hover:text-black hover:shadow-[0_0_12px_#ef4444] transition-all flex items-center gap-1.5"
            id="btn-defense"
          >
            <Shield className="w-3.5 h-3.5 text-red-400" />
            Protocolo Sentinela
          </button>
        </div>
      </div>

      {/* Suggestion Chips */}
      <div className="mt-2 pt-2 border-t border-cyan-500/15 flex items-center gap-1.5 overflow-x-auto text-[10px] font-['JetBrains_Mono'] scrollbar-none">
        <span className="text-cyan-500/80 flex-shrink-0 flex items-center gap-1">
          SUGESTÕES TÁTICAS:
        </span>
        {suggestions.map((sug, idx) => (
          <button
            key={idx}
            onClick={() => handleSend(sug)}
            className="px-2 py-0.5 rounded bg-cyan-950/40 hover:bg-[var(--primary-dim)] text-cyan-300/90 hover:text-white border border-cyan-500/20 hover:border-cyan-400/50 transition-all flex-shrink-0"
          >
            {sug}
          </button>
        ))}
      </div>
    </footer>
  );
}