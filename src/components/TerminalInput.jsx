import React from 'react';
import { Mic, MicOff } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function TerminalInput({ isListening, onToggleMic, isSpeaking = false }) {
  return (
    <footer className="syn-panel p-3 relative z-20 flex items-center justify-between gap-4" data-purpose="command-console">
      <div className="flex items-center gap-3 font-syn-body">
        <div className="syn-pill px-3 py-2 flex items-center gap-1.5" style={{ background: 'rgba(10,6,32,0.6)', border: `1px solid ${isListening ? 'rgba(45,253,212,0.5)' : 'rgba(138,92,255,0.3)'}` }} title={isListening ? 'Ouvindo' : 'Em repouso'}>
          {[0, 1, 2].map((i) => (
            <span
              key={i}
              className={isListening || isSpeaking ? 'syn-listening-dot' : ''}
              style={{
                width: 6, height: 6, borderRadius: '50%',
                background: isSpeaking ? '#ff3ec8' : isListening ? '#2dfdd4' : '#9aa6c9',
                boxShadow: isListening || isSpeaking ? `0 0 8px ${isSpeaking ? '#ff3ec8' : '#2dfdd4'}` : 'none',
                animationDelay: `${i * 0.25}s`,
                opacity: isListening || isSpeaking ? undefined : 0.5,
              }}
            />
          ))}
        </div>
        <div>
          <div className="font-syn-title text-xs font-medium tracking-wide" style={{ color: isSpeaking ? '#ff3ec8' : '#2dfdd4' }}>
            {isListening ? 'Escutando comandos' : isSpeaking ? 'Transmitindo' : 'Repouso vocal'}
          </div>
          <div className="font-syn-body text-[11px] font-light" style={{ color: '#9aa6c9' }}>
            Fale naturalmente · comandos diretos de sistema
          </div>
        </div>
      </div>

      <button
        onClick={() => { sounds.playClick(); onToggleMic(); }}
        className="syn-pill font-syn-title px-5 py-2.5 text-xs font-medium flex items-center gap-2"
        style={
          isListening
            ? { background: 'rgba(255,62,200,0.2)', color: '#ff3ec8', border: '1px solid rgba(255,62,200,0.5)', boxShadow: '0 0 18px rgba(255,62,200,0.45)' }
            : { background: 'rgba(45,253,212,0.16)', color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.4)', boxShadow: '0 0 14px rgba(45,253,212,0.35)' }
        }
      >
        {isListening ? <><MicOff className="w-4 h-4" /> Pausar escuta</> : <><Mic className="w-4 h-4" /> Ativar escuta</>}
      </button>
    </footer>
  );
}
