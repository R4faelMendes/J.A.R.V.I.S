import React, { useState, useEffect } from 'react';
import { Mic, MicOff, Radio, Volume2 } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function AudioVisualizer({
  isListening = false,
  isSpeaking = false,
  onToggleMic,
  lastDirective = 'Pronto, Senhor. Todos os protocolos de combate e telemetria estão sincronizados.',
  interimTranscript = '',
  micPhase = 'idle',
  engineInfo = null
}) {
  const [bars, setBars] = useState(Array(24).fill(12));

  const isRecording = micPhase === 'recording';
  const isTranscribing = micPhase === 'transcribing';

  const engineLabel = (() => {
    if (!engineInfo) return 'MOTOR DE VOZ: INICIALIZANDO';
    const stt = engineInfo.stt === 'local'
      ? `WHISPER LOCAL (${String(engineInfo.model || 'base').toUpperCase()})`
      : engineInfo.stt === 'webspeech' ? 'WEB SPEECH' : 'INDISPONÍVEL';
    const tts = engineInfo.tts === 'neural' ? 'VOZ NEURAL ATIVA' : 'VOZ LOCAL';
    return `${stt} // ${tts}`;
  })();

  // Dynamic bar height animation
  useEffect(() => {
    const interval = setInterval(() => {
      setBars(prev => prev.map((_, i) => {
        if (isSpeaking) {
          // Energetic waveform when Jarvis is talking
          return Math.floor(Math.random() * 28) + 8;
        } else if (isListening) {
          // Sensitive pulse when microphone is active
          return Math.floor(Math.random() * 22) + 6;
        } else {
          // Subtle idle resting rhythm
          const wave = Math.sin(Date.now() / 400 + i * 0.4) * 6 + 10;
          return Math.max(4, Math.floor(wave));
        }
      }));
    }, 100);

    return () => clearInterval(interval);
  }, [isListening, isSpeaking]);

  return (
    <div className="z-10 bg-slate-950/70 border border-cyan-500/30 rounded-lg p-3 backdrop-blur-md relative overflow-hidden shadow-lg">
      {/* Top Controls & Audio Rate */}
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center space-x-2">
          <button
            onClick={() => {
              sounds.playClick();
              if (onToggleMic) onToggleMic();
            }}
            title={isListening ? 'Clique para silenciar o microfone' : 'Clique para ativar comando de voz'}
            className={`w-7 h-7 rounded-full flex items-center justify-center transition-all ${
              isListening
                ? 'bg-[var(--primary)] text-black shadow-[0_0_14px_var(--primary)] animate-pulse'
                : 'bg-cyan-950/40 border border-cyan-400 text-cyan-300 hover:bg-[var(--primary)] hover:text-black'
            }`}
          >
            {isListening ? <Mic className="w-4 h-4" /> : <MicOff className="w-4 h-4" />}
          </button>
          <span className={`text-xs font-['JetBrains_Mono'] font-bold tracking-wide flex items-center gap-1.5 ${
            isListening ? 'text-[var(--secondary)] glow-amber' : isSpeaking ? 'text-[var(--primary)] glow-cyan' : 'text-cyan-300/80'
          }`}>
            <Radio className="w-3.5 h-3.5 animate-pulse text-[var(--primary)]" />
            {isListening 
              ? isRecording
                ? 'GRAVANDO COMANDO • FALE NORMALMENTE...'
                : isTranscribing
                  ? 'INTERPRETANDO ÁUDIO • J.A.R.V.I.S. PROCESSANDO...'
                  : 'CANAL DE ÁUDIO TÁTICO • J.A.R.V.I.S. ESCUTANDO...' 
              : isSpeaking 
                ? 'J.A.R.V.I.S. TRANSMITINDO VOZ...' 
                : 'CANAL DE ÁUDIO EM ESPERA • MICROFONE PRONTO'}
          </span>
        </div>
        <span className="text-[11px] font-['JetBrains_Mono'] text-cyan-400/80 hidden sm:inline" title="Motor de reconhecimento e síntese de voz">
          {engineLabel}
        </span>
      </div>

      {/* Dynamic Waveform Frequency Bars */}
      <div className="flex items-center justify-between h-10 gap-1 px-1 overflow-hidden bg-black/30 rounded border border-cyan-500/10">
        {bars.map((height, idx) => {
          const isAmber = idx % 4 === 0 || idx % 7 === 0;
          return (
            <span
              key={idx}
              className={`w-1 rounded-full transition-all duration-100 ${
                isAmber 
                  ? 'bg-amber-400 shadow-[0_0_6px_#ffb020]' 
                  : 'bg-[var(--primary)] shadow-[0_0_6px_var(--primary)]'
              }`}
              style={{ height: `${height}px` }}
            />
          );
        })}
      </div>

      {/* Live Transcript / Response Feedback */}
      <div className="mt-2 text-[11px] font-['JetBrains_Mono'] text-cyan-200/90 flex flex-col sm:flex-row items-start sm:items-center justify-between border-t border-cyan-500/20 pt-1.5 gap-1">
        <div className="flex-1 truncate max-w-full">
          {interimTranscript ? (
            <span className="text-amber-300 font-semibold animate-pulse">
              &gt; Ouvindo: "{interimTranscript}..."
            </span>
          ) : isRecording ? (
            <span className="text-red-300 font-semibold animate-pulse">
              &gt; ▌ Gravando... fale agora
            </span>
          ) : isTranscribing ? (
            <span className="text-amber-300 font-semibold animate-pulse">
              &gt; Decodificando comando de voz...
            </span>
          ) : (
            <span className="italic">
              "{lastDirective}"
            </span>
          )}
        </div>
        <span className="text-cyan-400 uppercase font-semibold text-[10px] bg-cyan-950/60 px-1.5 py-0.5 rounded border border-cyan-500/30 flex-shrink-0">
          FEED: {isSpeaking ? 'TRANSMITINDO' : 'ESTÁVEL'}
        </span>
      </div>
    </div>
  );
}