import React, { useEffect, useState } from 'react';
import { Mic, MicOff } from 'lucide-react';
import { sounds } from '../services/soundEffects';

export default function AudioVisualizer({
  isListening = false,
  isSpeaking = false,
  onToggleMic,
  lastDirective = 'Pronto, Senhor. O organismo está sincronizado.',
  interimTranscript = '',
  micPhase = 'idle',
  engineInfo = null,
}) {
  const [phase, setPhase] = useState(0);

  const isRecording = micPhase === 'recording';
  const isTranscribing = micPhase === 'transcribing';

  const engineLabel = (() => {
    if (!engineInfo) return 'voz: inicializando';
    const stt = engineInfo.stt === 'local'
      ? `whisper local (${String(engineInfo.model || 'base').toUpperCase()})`
      : engineInfo.stt === 'webspeech' ? 'web speech' : 'indisponível';
    const tts = engineInfo.tts === 'neural' ? 'voz neural ativa' : 'voz local';
    return `${stt} · ${tts}`;
  })();

  useEffect(() => {
    const interval = setInterval(() => setPhase((p) => p + 1), 120);
    return () => clearInterval(interval);
  }, []);

  const accent = isSpeaking ? '#ff3ec8' : '#2dfdd4';
  const glow = isSpeaking ? 'rgba(255,62,200,0.55)' : 'rgba(45,253,212,0.55)';

  const amplitude = isSpeaking ? 22 : isListening ? 15 : 6;
  const N = 60;
  const W = 600;
  const H = 64;
  const speed = isSpeaking ? 0.55 : isListening ? 0.35 : 0.12;
  const pts = Array.from({ length: N }, (_, i) => {
    const t = i / (N - 1);
    const env = Math.sin(t * Math.PI);
    const y =
      H / 2 +
      Math.sin(i * 0.55 + phase * speed) * amplitude * env * 0.6 +
      Math.sin(i * 0.22 - phase * speed * 0.7) * amplitude * env * 0.4;
    return `${(t * W).toFixed(1)},${y.toFixed(1)}`;
  });
  const d = `M ${pts[0]} ` + pts.slice(1).map((p, i) => {
    const [x0, y0] = pts[i].split(',').map(Number);
    const [x1, y1] = p.split(',').map(Number);
    const mx = (x0 + x1) / 2;
    return `C ${mx},${y0} ${mx},${y1} ${x1},${y1}`;
  }).join(' ');

  return (
    <div className="syn-panel p-4 relative overflow-hidden z-10">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <button
            onClick={() => { sounds.playClick(); onToggleMic?.(); }}
            title={isListening ? 'Pausar escuta' : 'Abrir escuta'}
            className="syn-pill w-8 h-8 flex items-center justify-center"
            style={{
              background: isListening ? 'rgba(255,62,200,0.25)' : 'rgba(45,253,212,0.15)',
              border: `1px solid ${isListening ? 'rgba(255,62,200,0.5)' : 'rgba(45,253,212,0.35)'}`,
              color: isListening ? '#ff3ec8' : '#2dfdd4',
              boxShadow: `0 0 12px ${glow}`,
            }}
          >
            {isListening ? <Mic className="w-4 h-4" /> : <MicOff className="w-4 h-4" />}
          </button>
          <span className="font-syn-title text-xs font-medium tracking-wide" style={{ color: accent }}>
            {isListening
              ? isRecording ? 'Gravando · fale naturalmente' : isTranscribing ? 'Interpretando áudio' : 'Escutando'
              : isSpeaking ? 'Transmitindo voz' : 'Canal em repouso'}
          </span>
        </div>
        <span className="font-syn-body text-[11px] hidden sm:inline" style={{ color: '#9aa6c9' }} title="Motor de reconhecimento e síntese">
          {engineLabel}
        </span>
      </div>

      <div className="rounded-2xl overflow-hidden" style={{ background: 'rgba(5,3,17,0.6)', border: '1px solid rgba(138,92,255,0.2)', borderRadius: '20px 24px 18px 26px' }}>
        <svg viewBox={`0 0 ${W} ${H}`} className="w-full h-16" preserveAspectRatio="none" aria-hidden>
          <defs>
            <linearGradient id="syn-wave" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0%" stopColor="#8a5cff" stopOpacity="0.7" />
              <stop offset="55%" stopColor={accent} />
              <stop offset="100%" stopColor={isSpeaking ? '#2dfdd4' : '#ff3ec8'} stopOpacity="0.8" />
            </linearGradient>
          </defs>
          <path d={d} fill="none" stroke="url(#syn-wave)" strokeWidth="2.5" strokeLinecap="round" style={{ filter: `drop-shadow(0 0 6px ${glow})` }} />
        </svg>
      </div>

      <div className="mt-2 font-syn-body text-[12px] flex items-center justify-between gap-2" style={{ color: '#eaf8f4' }}>
        <div className="flex-1 truncate">
          {interimTranscript ? (
            <span style={{ color: '#ffcf5c' }}>ouvindo: “{interimTranscript}…”</span>
          ) : isRecording ? (
            <span style={{ color: '#ff5c7a' }}>gravando · fale agora</span>
          ) : isTranscribing ? (
            <span style={{ color: '#ffcf5c' }}>decodificando comando…</span>
          ) : (
            <span className="italic font-light">“{lastDirective}”</span>
          )}
        </div>
        <span className="syn-pill font-syn-body text-[10px] px-2.5 py-0.5 shrink-0" style={{ border: '1px solid rgba(45,253,212,0.3)', color: '#9aa6c9' }}>
          {isSpeaking ? 'transmitindo' : 'estável'}
        </span>
      </div>
    </div>
  );
}
