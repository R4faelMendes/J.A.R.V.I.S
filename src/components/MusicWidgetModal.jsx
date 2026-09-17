import React, { useState } from 'react';
import { X, Music, Play, Pause, SkipForward, SkipBack, Volume2, Disc, Radio } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { commandExecutor } from '../services/commandExecutor';

export default function MusicWidgetModal({ isOpen, onClose }) {
  const [musicState, setMusicState] = useState(() => commandExecutor.musicState);

  if (!isOpen) return null;

  const handlePlayPause = () => {
    const res = commandExecutor.toggleMusic();
    setMusicState({ ...res.state });
  };

  const handleNext = () => {
    const res = commandExecutor.nextTrack();
    setMusicState({ ...res.state });
  };

  const handlePrev = () => {
    const res = commandExecutor.previousTrack();
    setMusicState({ ...res.state });
  };

  const handleVolume = (e) => {
    const val = Number(e.target.value);
    const res = commandExecutor.setVolume(val);
    setMusicState({ ...res.state });
  };

  const handleSelectTrack = (idx) => {
    sounds.playClick();
    commandExecutor.musicState.trackIndex = idx;
    commandExecutor.musicState.currentTrack = commandExecutor.musicState.playlist[idx];
    commandExecutor.musicState.isPlaying = true;
    setMusicState({ ...commandExecutor.musicState });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 bg-black/80 backdrop-blur-md animate-in fade-in duration-200">
      <div className="hud-panel rounded-xl w-full max-w-xl flex flex-col border-[var(--primary)] shadow-[0_0_40px_var(--primary-glow)] overflow-hidden">
        <div className="corner-bracket corner-tl"></div>
        <div className="corner-bracket corner-tr"></div>
        <div className="corner-bracket corner-bl"></div>
        <div className="corner-bracket corner-br"></div>

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-cyan-500/20 bg-slate-950/60">
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded bg-cyan-950/60 border border-[var(--primary)] text-[var(--primary)]">
              <Music className="w-5 h-5" />
            </div>
            <div>
              <h2 className="font-['Orbitron'] text-lg font-bold text-[var(--primary)] glow-cyan tracking-wider">
                CONTROLE DE ÁUDIO & MÍDIA
              </h2>
              <p className="text-xs text-cyan-400/80 font-['JetBrains_Mono']">
                STREAMING INTEGRADO // SPOTIFY & STARK SOUND SYSTEM
              </p>
            </div>
          </div>

          <button
            onClick={() => { sounds.playClick(); onClose(); }}
            className="p-1.5 rounded-lg border border-red-500/30 text-red-400 hover:bg-red-500 hover:text-black transition-all"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-6">
          {/* Active Track Showcase */}
          <div className="p-4 rounded-xl bg-cyan-950/30 border border-cyan-500/30 flex items-center space-x-4 relative overflow-hidden">
            <div className="relative w-16 h-16 rounded-lg bg-black border border-[var(--primary)] flex items-center justify-center text-[var(--primary)] shadow-[0_0_15px_var(--primary-glow)] flex-shrink-0">
              <Disc className={`w-10 h-10 ${musicState.isPlaying ? 'animate-spin-slow' : ''}`} />
            </div>

            <div className="flex-1 overflow-hidden font-['JetBrains_Mono']">
              <div className="text-[10px] text-emerald-400 flex items-center gap-1 font-bold">
                <Radio className="w-3 h-3 animate-pulse" />
                {musicState.isPlaying ? 'REPRODUZINDO' : 'EM PAUSA'}
              </div>
              <div className="text-base font-bold text-cyan-100 truncate glow-cyan">
                {musicState.currentTrack}
              </div>
              <div className="text-xs text-cyan-400/70">
                Qualidade: Hi-Res FLAC 96kHz / 24-bit
              </div>
            </div>
          </div>

          {/* Equalizer Wave simulation */}
          <div className="flex items-center justify-center gap-1 h-8 px-2">
            {Array.from({ length: 28 }).map((_, i) => (
              <span
                key={i}
                className="w-1 rounded-full bg-[var(--primary)] transition-all duration-150"
                style={{
                  height: musicState.isPlaying ? `${Math.floor(Math.random() * 24) + 6}px` : '4px',
                  opacity: musicState.isPlaying ? 0.9 : 0.3
                }}
              />
            ))}
          </div>

          {/* Playback Controls */}
          <div className="flex items-center justify-center space-x-6">
            <button
              onClick={handlePrev}
              className="p-3 rounded-full bg-cyan-950/50 border border-cyan-500/30 text-cyan-300 hover:text-white hover:border-cyan-400 transition-all"
            >
              <SkipBack className="w-5 h-5" />
            </button>

            <button
              onClick={handlePlayPause}
              className="p-4 rounded-full bg-[var(--primary)] text-black font-bold shadow-[0_0_20px_var(--primary-glow)] hover:scale-110 transition-transform"
            >
              {musicState.isPlaying ? <Pause className="w-6 h-6" /> : <Play className="w-6 h-6 ml-0.5" />}
            </button>

            <button
              onClick={handleNext}
              className="p-3 rounded-full bg-cyan-950/50 border border-cyan-500/30 text-cyan-300 hover:text-white hover:border-cyan-400 transition-all"
            >
              <SkipForward className="w-5 h-5" />
            </button>
          </div>

          {/* Volume Control */}
          <div className="flex items-center space-x-3 bg-black/40 p-3 rounded-lg border border-cyan-500/20 font-['JetBrains_Mono']">
            <Volume2 className="w-4 h-4 text-cyan-400" />
            <input
              type="range"
              min="0"
              max="100"
              value={musicState.volume}
              onChange={handleVolume}
              className="flex-1 accent-[var(--primary)] cursor-pointer"
            />
            <span className="text-xs text-cyan-200 font-bold w-10 text-right">
              {musicState.volume}%
            </span>
          </div>

          {/* Playlist Queue */}
          <div className="space-y-1.5 font-['JetBrains_Mono']">
            <div className="text-[11px] text-cyan-400/80 uppercase font-bold">
              LISTA DE REPRODUÇÃO TÁTICA
            </div>
            <div className="max-h-36 overflow-y-auto space-y-1">
              {musicState.playlist.map((track, idx) => (
                <div
                  key={idx}
                  onClick={() => handleSelectTrack(idx)}
                  className={`p-2 rounded text-xs flex items-center justify-between cursor-pointer border transition-all ${
                    idx === musicState.trackIndex
                      ? 'bg-[var(--primary-dim)] border-[var(--primary)] text-cyan-100 font-bold'
                      : 'bg-cyan-950/20 border-cyan-500/10 text-cyan-400/80 hover:bg-cyan-950/40 hover:text-cyan-200'
                  }`}
                >
                  <span className="truncate">{track}</span>
                  {idx === musicState.trackIndex && (
                    <span className="text-[10px] text-emerald-400 font-bold ml-2">TOCANDO</span>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}