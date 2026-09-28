import React from 'react';
import { Mic } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function GameModeOverlay({ isActive, onWakeUp, isListening, onToggleMic }) {
  if (!isActive) return null;
  return (
    <div className="fixed inset-0 z-40 flex flex-col items-center justify-between p-6 select-none" style={{ background: 'rgba(5,3,17,0.96)' }}>
      <div className="w-full flex items-center justify-between pb-3 font-syn-body text-xs" style={{ borderBottom: '1px solid rgba(138,92,255,0.25)' }}>
        <span className="font-syn-title text-sm font-medium tracking-wide" style={{ color: '#9aa6c9' }}>Hibernação · organismo em repouso</span>
        <span className="syn-pill font-syn-body text-[11px] px-3 py-1" style={{ border: '1px solid rgba(45,253,212,0.3)', color: '#2dfdd4', background: 'rgba(45,253,212,0.07)' }}>
          escuta passiva · recursos liberados
        </span>
      </div>

      <div className="flex flex-col items-center gap-6 text-center">
        <div onClick={() => { sounds.playSuccess(); onWakeUp(); }} className="relative w-52 h-52 cursor-pointer" title="Toque para despertar">
          <div className="syn-blob absolute inset-2" style={{ background: 'radial-gradient(circle, rgba(138,92,255,0.4), rgba(45,253,212,0.15) 60%, transparent 70%)', animationDuration: '14s', opacity: 0.5 }} />
          <div className="syn-core-slow absolute inset-8 flex flex-col items-center justify-center"
            style={{ borderRadius: '44% 56% 58% 42% / 50% 46% 54% 50%', background: 'radial-gradient(circle at 45% 40%, rgba(138,92,255,0.35), #0a0620 65%)', border: '1.5px solid rgba(138,92,255,0.5)', boxShadow: '0 0 30px rgba(138,92,255,0.35), inset 0 0 24px rgba(138,92,255,0.15)' }}>
            <span className="font-syn-title text-[11px] font-medium" style={{ color: '#eaf8f4' }}>despertar</span>
            <span className="font-syn-body text-[10px] font-light" style={{ color: '#9aa6c9' }}>toque para respirar fundo</span>
          </div>
        </div>
        <div className="font-syn-body max-w-md">
          <div className="font-syn-title text-sm font-medium" style={{ color: '#eaf8f4' }}>J.A.R.V.I.S. dormindo, ouvido entreaberto</div>
          <p className="text-xs font-light mt-1" style={{ color: '#9aa6c9' }}>
            Renderizações e telemetria desaceleraram para uma respiração de 8 segundos. Diga “Jarvis, acordar” ou toque o núcleo.
          </p>
        </div>
      </div>

      <div className="w-full flex items-center justify-between pt-3 font-syn-body text-xs" style={{ borderTop: '1px solid rgba(138,92,255,0.25)' }}>
        <button onClick={() => { sounds.playClick(); onToggleMic(); }} className="syn-pill flex items-center gap-2 px-3 py-1.5" style={{ border: '1px solid rgba(45,253,212,0.35)', color: '#2dfdd4', background: isListening ? 'rgba(45,253,212,0.15)' : 'transparent' }}>
          <Mic className="w-4 h-4" /> {isListening ? 'Ouvindo…' : 'Ativar escuta'}
        </button>
        <button onClick={() => { sounds.playSuccess(); onWakeUp(); }} className="syn-pill font-syn-title px-5 py-1.5 text-xs font-medium" style={{ background: 'rgba(45,253,212,0.18)', color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.45)', boxShadow: '0 0 16px rgba(45,253,212,0.35)' }}>
          Restaurar organismo
        </button>
      </div>
    </div>
  );
}
