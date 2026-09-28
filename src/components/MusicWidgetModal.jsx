import React, { useState, useRef, useEffect } from 'react';
import { X, Music, Search, Minimize2, Maximize2, Shield } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { memoryService } from '../services/memoryService';

const YT_MUSIC_HOME = 'https://music.youtube.com';
// Cosmético do Brave Shields emulado: esconde os slots de anúncio que
// sobrariam como espaço vazio (mesmos seletores de YT_AD_CSS em electron-main)
const YT_AD_CSS = `#masthead-ad, .ytp-ad-text-overlay, .ytp-ad-message-container,
ytmusic-ad-slot-renderer, ytmusic-companion-promo-renderer,
ytmusic-player-ad-overlay-renderer { display: none !important; }`;
const buildYtMusicUrl = (term = '') => {
  const normalized = term.trim();
  if (!normalized) return YT_MUSIC_HOME;
  if (/^https?:\/\//i.test(normalized)) return normalized;
  return `${YT_MUSIC_HOME}/search?q=${encodeURIComponent(normalized)}`;
};

const DEFAULT_STREAMS = [
  {
    id: 'stark-suite',
    title: 'Stark Suite',
    artist: 'J.A.R.V.I.S.',
    query: 'jarvis cinematic soundtrack',
    src: buildYtMusicUrl('jarvis cinematic soundtrack')
  },
  {
    id: 'focus-mode',
    title: 'Focus Mode',
    artist: 'J.A.R.V.I.S.',
    query: 'ambient electronic focus music',
    src: buildYtMusicUrl('ambient electronic focus music')
  },
  {
    id: 'launch-sequence',
    title: 'Launch Sequence',
    artist: 'J.A.R.V.I.S.',
    query: 'epic sci fi instrumental',
    src: buildYtMusicUrl('epic sci fi instrumental')
  }
];

export default function MusicWidgetModal({ isOpen, onClose, embedded = false, searchQuery = '' }) {
  const prefs = memoryService.getPreferences();
  const initialQueue = Array.isArray(prefs.musicQueue) && prefs.musicQueue.length ? prefs.musicQueue : DEFAULT_STREAMS;
  const initialTrack = initialQueue.find((item) => item.src === prefs.lastMusicQuery) || initialQueue[0];

  const [queue, setQueue] = useState(initialQueue);
  const [currentIndex, setCurrentIndex] = useState(Math.max(0, queue.findIndex((item) => item.id === initialTrack?.id)));
  const [isPlaying, setIsPlaying] = useState(false);
  const [isDocked, setIsDocked] = useState(Boolean(prefs.musicDock));
  const [customUrl, setCustomUrl] = useState('');
  const [query, setQuery] = useState(searchQuery || initialTrack?.query || '');
  const [currentSrc, setCurrentSrc] = useState(initialTrack?.src || buildYtMusicUrl('jarvis cinematic soundtrack'));
  const boxRef = useRef(null);
  const [dims, setDims] = useState('');

  // Brave Shields emulado (bloqueio de anúncios/rastreadores + contador)
  const [shields, setShields] = useState({ blocked: 0, enabled: true });
  useEffect(() => {
    let alive = true;
    Promise.resolve(window.electronAPI?.shieldsStatus?.())
      .then((s) => {
        if (!alive || !s) return;
        const v = (memoryService.getPreferences() || {}).shieldsEnabled;
        const enabled = typeof v === 'boolean' ? v : s.enabled !== false;
        if (enabled !== s.enabled) window.electronAPI?.shieldsSetEnabled?.(enabled);
        setShields({ blocked: s.blocked || 0, enabled });
      })
      .catch(() => { /* ignora */ });
    window.electronAPI?.onShieldsBlocked?.((total) => {
      if (alive) setShields((p) => ({ ...p, blocked: total }));
    });
    return () => { alive = false; };
  }, []);

  const toggleShields = () => {
    sounds.playClick();
    setShields((p) => {
      const next = { ...p, enabled: !p.enabled };
      window.electronAPI?.shieldsSetEnabled?.(next.enabled);
      memoryService.updatePreferences({ shieldsEnabled: next.enabled });
      return next;
    });
  };

  const activeTrack = queue[currentIndex] || queue[0] || DEFAULT_STREAMS[0];

  useEffect(() => {
    const nextSrc = activeTrack?.src || buildYtMusicUrl(query);
    setCurrentSrc(nextSrc);
    setQuery(activeTrack?.query || query || '');
  }, [activeTrack, query]);

  useEffect(() => {
    if (searchQuery && searchQuery.trim()) {
      const sanitized = searchQuery.trim();
      setQuery(sanitized);
      setCurrentSrc(buildYtMusicUrl(sanitized));
    }
  }, [searchQuery]);

  useEffect(() => {
    memoryService.updatePreferences({
      musicDock: isDocked,
      musicMode: 'ytmusic',
      lastMusicQuery: currentSrc || buildYtMusicUrl('jarvis cinematic soundtrack'),
      musicQueue: queue.slice(0, 8)
    });
  }, [isDocked, currentSrc, queue]);

  useEffect(() => {
    if (!embedded) return undefined;
    const measure = () => {
      const box = boxRef.current;
      if (box) setDims(`box ${Math.round(box.clientWidth)}x${Math.round(box.clientHeight)}`);
    };
    measure();
    const t = setTimeout(measure, 1500);
    window.addEventListener('resize', measure);
    return () => { clearTimeout(t); window.removeEventListener('resize', measure); };
  }, [embedded]);

  if (!isOpen && !embedded) return null;

  const handlePlayPause = () => {
    setIsPlaying((prev) => !prev);
    setCurrentSrc(buildYtMusicUrl(query || activeTrack?.query || ''));
    sounds.playClick();
  };

  const handleNext = () => {
    setCurrentIndex((prev) => (prev + 1) % queue.length);
    setIsPlaying(true);
  };

  const handlePrev = () => {
    setCurrentIndex((prev) => (prev - 1 + queue.length) % queue.length);
    setIsPlaying(true);
  };

  const handleAddCustomStream = () => {
    const trimmed = customUrl.trim();
    if (!trimmed) return;
    const normalized = trimmed.startsWith('http') ? trimmed : buildYtMusicUrl(trimmed);
    const newTrack = {
      id: `custom-${Date.now()}`,
      title: trimmed,
      artist: 'YouTube Music',
      query: trimmed,
      src: normalized
    };
    setQueue((prev) => [newTrack, ...prev].slice(0, 8));
    setCurrentIndex(0);
    setCurrentSrc(normalized);
    setQuery(trimmed);
    setCustomUrl('');
    setIsPlaying(true);
  };

  return (
    <div className={embedded ? (isDocked ? "flex-1 min-h-0 flex flex-col w-full h-[340px]" : "flex-1 min-h-0 flex flex-col w-full h-full") : "fixed inset-0 z-50 flex items-center justify-center p-2 sm:p-4 bg-black/85 backdrop-blur-md animate-in fade-in duration-200"}>
      <div ref={boxRef} className={embedded ? (isDocked ? "syn-panel rounded-xl w-full h-[340px] flex flex-col border-[var(--primary)] overflow-hidden shadow-[0_0_24px_var(--primary-glow)]" : "syn-panel rounded-xl w-full h-full flex-1 min-h-0 flex flex-col border-[var(--primary)] overflow-hidden") : "syn-panel rounded-xl w-full max-w-[98vw] h-[95vh] flex flex-col border-[var(--primary)] shadow-[0_0_45px_var(--primary-glow)] overflow-hidden"}>





        <div className="flex items-center justify-between px-3 sm:px-4 py-2 border-b border-cyan-500/20 bg-slate-950/80">
          <div className="flex items-center gap-2 min-w-0">
            <div className="p-1.5 rounded bg-cyan-950/60 border border-[var(--primary)] text-[var(--primary)]">
              <Music className="w-4 h-4" />
            </div>
            <div className="min-w-0">
              <h2 className="font-syn-title text-[10px] sm:text-xs font-medium tracking-wide truncate" style={{ color: '#eaf8f4' }}>
                Membrana musical
              </h2>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={toggleShields}
              className={`flex items-center gap-1 px-2 py-1 rounded border text-[10px] font-mono transition-all ${
                shields.enabled
                  ? 'border-emerald-500/40 bg-emerald-950/40 text-emerald-300 hover:border-emerald-400'
                  : 'border-white/15 bg-white/5 text-slate-400 hover:text-slate-200'
              }`}
              title={`Brave Shields emulado: ${shields.blocked} anúncios/rastreadores bloqueados — clique para ${shields.enabled ? 'desligar' : 'ligar'}`}
            >
              <Shield className="w-3 h-3" />
              {shields.enabled ? `${shields.blocked} BLOQ.` : 'SHIELDS OFF'}
            </button>

            <div className="flex items-center gap-2 bg-slate-900/80 border border-cyan-500/30 rounded px-2 py-1">
              <input
                value={customUrl}
                onChange={(e) => setCustomUrl(e.target.value)}
                placeholder="Buscar no YouTube Music"
                className="w-40 sm:w-56 bg-transparent text-[10px] text-cyan-100 placeholder:text-cyan-500/40 outline-none"
              />
              <button
                onClick={() => {
                  if (!customUrl.trim()) return;
                  const next = customUrl.trim();
                  const normalized = next.startsWith('http') ? next : buildYtMusicUrl(next);
                  setQuery(next);
                  setCurrentSrc(normalized);
                  setCustomUrl('');
                  setIsPlaying(true);
                  sounds.playClick();
                }}
                className="p-1 rounded bg-[var(--primary)] text-black font-bold"
                title="Buscar"
              >
                <Search className="w-3.5 h-3.5" />
              </button>
            </div>

            <button
              onClick={() => setIsDocked((prev) => !prev)}
              className="p-1.5 rounded-lg border border-cyan-500/30 bg-cyan-950/40 text-cyan-200 hover:bg-cyan-900/70 transition-all"
              title={isDocked ? 'Expandir player' : 'Minimizar player'}
            >
              {isDocked ? <Maximize2 className="w-4 h-4" /> : <Minimize2 className="w-4 h-4" />}
            </button>

            <button
              onClick={() => {
                sounds.playClick();
                onClose();
              }}
              className="p-1.5 rounded-lg border border-white/15 text-slate-400 hover:text-red-300 hover:border-red-400/50 hover:bg-red-500/10 transition-all"
              title="Voltar"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>

        <div className="flex-1 min-h-0 overflow-hidden bg-black relative">
          <div className="w-full h-full">
            <iframe
              key={currentSrc}
              src={currentSrc}
              title="YouTube Music full screen"
              onLoad={(e) => {
                // Cosmético do Shields dentro do iframe (mesma sessão/webSecurity
                // off): esconde slots de anúncio que passariam pelo bloqueio de rede
                try {
                  const doc = e.target?.contentDocument;
                  if (doc && !doc.getElementById('jarvis-shields-css')) {
                    const style = doc.createElement('style');
                    style.id = 'jarvis-shields-css';
                    style.textContent = YT_AD_CSS;
                    (doc.head || doc.documentElement).appendChild(style);
                  }
                } catch { /* cross-origin sem permissão — ignora */ }
              }}
              className="w-full h-full border-0"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
              referrerPolicy="strict-origin-when-cross-origin"
              sandbox="allow-scripts allow-same-origin allow-forms allow-popups allow-modals allow-presentation"
            />
          </div>

          {dims ? <span className="absolute bottom-2 right-2 text-[10px] text-amber-300/80 bg-black/50 px-1.5 py-0.5 rounded border border-amber-500/30">{dims}</span> : null}
        </div>
      </div>
    </div>
  );
}
