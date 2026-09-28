import React, { useMemo, useState } from 'react';
import { sounds } from '../services/soundEffects';

/**
 * SYNAPSE — núcleo como organismo neural vivo.
 * Os 4 "Marks" viram estágios de consciência (mesmos ids, mesma API):
 *  mark_1  -> Embrionário   (1 blob, teal, poucas partículas)
 *  mark_3  -> Consciente     (3 blobs teal/violeta/magenta, padrão)
 *  mark_50 -> Expandido      (5 blobs + sinapses externas)
 *  mark_85 -> Transcendente  (gradiente triplo, pulso rápido, rastro)
 */

const STAGES = {
  mark_1: {
    label: 'Embrionário',
    caption: 'Estágio inicial // latência serena',
    blobs: [{ color: '#2dfdd4', size: 150, x: '50%', y: '50%', delay: '0s', dur: '11s' }],
    particleCount: 6,
    pulseFast: false,
    synapses: false,
  },
  mark_3: {
    label: 'Consciente',
    caption: 'Vigilância equilibrada // padrão',
    blobs: [
      { color: '#2dfdd4', size: 150, x: '46%', y: '52%', delay: '0s', dur: '9s' },
      { color: '#8a5cff', size: 120, x: '56%', y: '44%', delay: '-3s', dur: '10s' },
      { color: '#ff3ec8', size: 90, x: '52%', y: '58%', delay: '-6s', dur: '8s' },
    ],
    particleCount: 14,
    pulseFast: false,
    synapses: false,
  },
  mark_50: {
    label: 'Expandido',
    caption: 'Campo ampliado // múltiplas sinapses',
    blobs: [
      { color: '#2dfdd4', size: 150, x: '46%', y: '52%', delay: '0s', dur: '9s' },
      { color: '#8a5cff', size: 130, x: '58%', y: '42%', delay: '-2s', dur: '10s' },
      { color: '#ff3ec8', size: 100, x: '54%', y: '60%', delay: '-4s', dur: '8s' },
      { color: '#2dfdd4', size: 70, x: '38%', y: '42%', delay: '-5s', dur: '12s' },
      { color: '#8a5cff', size: 60, x: '62%', y: '60%', delay: '-7s', dur: '11s' },
    ],
    particleCount: 22,
    pulseFast: false,
    synapses: true,
  },
  mark_85: {
    label: 'Transcendente',
    caption: 'Fusão total // consciência plena',
    blobs: [
      { color: 'linear', size: 165, x: '50%', y: '50%', delay: '0s', dur: '7s' },
      { color: '#ff3ec8', size: 120, x: '55%', y: '45%', delay: '-2s', dur: '8s' },
      { color: '#2dfdd4', size: 110, x: '45%', y: '57%', delay: '-4s', dur: '7.5s' },
      { color: '#8a5cff', size: 85, x: '58%', y: '58%', delay: '-5s', dur: '9s' },
    ],
    particleCount: 30,
    pulseFast: true,
    synapses: true,
    trail: true,
  },
};

function blobBackground(color) {
  if (color === 'linear') {
    return 'radial-gradient(circle, rgba(45,253,212,0.55), rgba(138,92,255,0.45) 45%, rgba(255,62,200,0.4) 75%, transparent 70%)';
  }
  const hex = color;
  return `radial-gradient(circle, ${hex}cc, ${hex}55 55%, transparent 70%)`;
}

function Particles({ count, reactive, trail }) {
  const items = useMemo(
    () =>
      Array.from({ length: count }, (_, i) => ({
        left: `${8 + ((i * 37) % 84)}%`,
        bottom: `${6 + ((i * 53) % 30)}%`,
        size: 3 + ((i * 7) % 5),
        dur: `${4 + ((i * 13) % 6)}s`,
        delay: `${-((i * 17) % 60) / 10}s`,
      })),
    [count]
  );
  return (
    <>
      {items.map((p, i) => (
        <span
          key={i}
          className="syn-particle absolute"
          style={{
            left: p.left,
            bottom: p.bottom,
            width: p.size,
            height: p.size,
            background: i % 3 === 0 ? '#ff3ec8' : i % 3 === 1 ? '#2dfdd4' : '#8a5cff',
            boxShadow: trail
              ? `0 0 8px currentColor, 0 0 18px ${i % 2 ? '#ff3ec8' : '#2dfdd4'}`
              : `0 0 7px ${i % 3 === 0 ? '#ff3ec8' : i % 3 === 1 ? '#2dfdd4' : '#8a5cff'}`,
            animationDuration: reactive ? `${Math.max(2, parseFloat(p.dur) - 1.5)}s` : p.dur,
            animationDelay: p.delay,
            opacity: 0,
          }}
        />
      ))}
    </>
  );
}

function NeuralPulse({ fast, speaking }) {
  return (
    <svg viewBox="0 0 200 200" className={`absolute inset-0 w-full h-full ${fast ? 'syn-pulse-fast' : 'syn-pulse'}`} aria-hidden>
      <path d="M 20,120 C 50,90 60,150 90,120 S 130,80 160,115" fill="none" stroke={speaking ? '#ff3ec8' : '#2dfdd4'} strokeWidth="2" strokeLinecap="round" opacity="0.8" />
      <path d="M 30,150 C 60,130 80,170 110,145 S 145,120 175,140" fill="none" stroke="#8a5cff" strokeWidth="1.5" strokeLinecap="round" opacity="0.6" />
      <path d="M 25,85 C 55,70 75,100 105,82 S 150,60 178,80" fill="none" stroke={speaking ? '#2dfdd4' : '#ff3ec8'} strokeWidth="1.2" strokeLinecap="round" opacity="0.45" />
    </svg>
  );
}

function SynapseRing({ visible }) {
  if (!visible) return null;
  const pts = [
    [100, 18], [163, 55], [163, 135], [100, 172], [37, 135], [37, 55],
  ];
  return (
    <svg viewBox="0 0 200 200" className="absolute inset-0 w-full h-full" aria-hidden>
      {pts.map(([x, y], i) => {
        const next = pts[(i + 1) % pts.length];
        const mx = (x + next[0]) / 2;
        return (
          <path
            key={i}
            d={`M ${x},${y} C ${mx},${y} ${mx},${next[1]} ${next[0]},${next[1]}`}
            fill="none"
            stroke={i % 2 ? 'rgba(45,253,212,0.45)' : 'rgba(255,62,200,0.4)'}
            strokeWidth="1.2"
            strokeLinecap="round"
          />
        );
      })}
      {pts.map(([x, y], i) => (
        <circle key={`n${i}`} cx={x} cy={y} r="3.5" fill="#0a0620" stroke={i % 2 ? '#2dfdd4' : '#ff3ec8'} strokeWidth="1.6" style={{ filter: `drop-shadow(0 0 5px ${i % 2 ? '#2dfdd4' : '#ff3ec8'})` }} />
      ))}
    </svg>
  );
}

function CoreLabel({ listening, speaking }) {
  const state = listening ? 'Ouvindo' : speaking ? 'Falando' : 'Estável';
  const color = listening ? '#2dfdd4' : speaking ? '#ff3ec8' : '#9aa6c9';
  return (
    <div className="absolute inset-0 m-auto w-20 h-20 syn-core flex flex-col items-center justify-center z-10 overflow-hidden"
      style={{
        borderRadius: '46% 54% 58% 42% / 48% 44% 56% 52%',
        background: 'radial-gradient(circle at 40% 35%, rgba(45,253,212,0.35), rgba(10,6,32,0.95) 60%, #050311)',
        border: `1.5px solid ${listening ? 'rgba(45,253,212,0.6)' : speaking ? 'rgba(255,62,200,0.6)' : 'rgba(154,166,201,0.35)'}`,
        boxShadow: listening
          ? '0 0 22px rgba(45,253,212,0.55), inset 0 0 18px rgba(45,253,212,0.2)'
          : speaking
            ? '0 0 26px rgba(255,62,200,0.6), inset 0 0 20px rgba(255,62,200,0.22)'
            : '0 0 14px rgba(138,92,255,0.4), inset 0 0 14px rgba(138,92,255,0.15)',
      }}
    >
      <span className="font-syn-title text-[8px] font-bold tracking-widest" style={{ color }}>JARVIS</span>
      <span className="font-syn-title text-sm font-bold text-[#eaf8f4]">{listening ? '···' : speaking ? '≈≈' : '◉'}</span>
      <span className="font-syn-body text-[7px]" style={{ color }}>{state}</span>
    </div>
  );
}

export default function ArcReactor({
  isSpeaking = false,
  isListening = false,
  model = 'mark_3',
  onModelChange,
  compact = false,
}) {
  const [pulseBoost, setPulseBoost] = useState(false);
  const stage = STAGES[model] || STAGES.mark_3;
  const reactive = isListening || isSpeaking || pulseBoost;

  const handleCoreClick = () => {
    sounds.playPowerPulse();
    setPulseBoost(true);
    setTimeout(() => setPulseBoost(false), 1200);
  };

  const size = compact ? 'w-[196px] h-[196px]' : 'w-64 h-64 sm:w-[22rem] sm:h-[22rem]';

  return (
    <div className={`flex flex-col items-center select-none ${compact ? 'px-2 py-3' : ''}`} data-purpose="arc-reactor-core">
      {!compact && (
        <div className="flex items-center justify-between w-full z-10 mb-2 font-syn-body">
          <div>
            <div className="font-syn-title text-[11px] tracking-wide font-bold" style={{ color: isSpeaking ? '#ff3ec8' : '#2dfdd4' }}>
              Núcleo neural // {stage.label.toLowerCase()}
            </div>
            <div className="text-xs" style={{ color: '#9aa6c9' }}>
              {isListening ? 'Campo receptivo aberto, escutando' : isSpeaking ? 'Emitindo resposta' : 'Respiração basal sincronizada'}
            </div>
          </div>
          <StagePicker model={model} onModelChange={onModelChange} />
        </div>
      )}

      {compact && (
        <div className="flex items-center gap-1.5">
          <span className="w-1.5 h-1.5 syn-core" style={{ borderRadius: '50%', background: isSpeaking ? '#ff3ec8' : '#2dfdd4', boxShadow: `0 0 8px ${isSpeaking ? '#ff3ec8' : '#2dfdd4'}` }} />
          <span className="font-syn-title text-[10px] font-medium tracking-[0.18em]" style={{ color: '#2dfdd4' }}>Núcleo synapse</span>
        </div>
      )}

      <div onClick={handleCoreClick} title="Núcleo vivo — toque para um pulso" className={`relative ${size} my-2 cursor-pointer overflow-visible`}>
        {stage.blobs.map((b, i) => (
          <div
            key={i}
            className="syn-blob absolute"
            style={{
              width: b.size,
              height: b.size,
              left: b.x,
              top: b.y,
              transform: 'translate(-50%, -50%)',
              marginLeft: -b.size / 2,
              marginTop: -b.size / 2,
              background: blobBackground(b.color),
              animationDelay: b.delay,
              animationDuration: reactive ? `${Math.max(4, parseFloat(b.dur) - 2.5)}s` : b.dur,
              opacity: 0.75,
            }}
          />
        ))}

        <NeuralPulse fast={stage.pulseFast || reactive} speaking={isSpeaking} />
        <SynapseRing visible={stage.synapses} />
        <Particles count={reactive ? stage.particleCount + 6 : stage.particleCount} reactive={reactive} trail={stage.trail} />
        <CoreLabel listening={isListening} speaking={isSpeaking} />
      </div>

      <StagePicker model={model} onModelChange={onModelChange} compact={compact} />
      <div className="mt-1.5 font-syn-body text-[11px]" style={{ color: '#9aa6c9' }}>
        {isListening ? 'escutando sua diretiva' : isSpeaking ? 'transmitindo resposta' : stage.caption}
      </div>
    </div>
  );
}

function StagePicker({ model, onModelChange, compact }) {
  const order = ['mark_1', 'mark_3', 'mark_50', 'mark_85'];
  const short = { mark_1: 'I', mark_3: 'II', mark_50: 'III', mark_85: 'IV' };
  return (
    <div className="flex items-center gap-1 p-1 syn-pill" style={{ background: 'rgba(10,6,32,0.7)', border: '1px solid rgba(45,253,212,0.18)' }}>
      {order.map((id) => (
        <button
          key={id}
          title={STAGES[id].label}
          onClick={() => { sounds.playClick(); onModelChange?.(id); }}
          className="syn-pill font-syn-title px-2.5 py-1 text-[10px] font-medium"
          style={
            model === id
              ? { background: 'rgba(45,253,212,0.2)', color: '#eaf8f4', boxShadow: '0 0 10px rgba(45,253,212,0.4)' }
              : { color: '#9aa6c9' }
          }
        >
          {compact ? short[id] : STAGES[id].label}
        </button>
      ))}
    </div>
  );
}
