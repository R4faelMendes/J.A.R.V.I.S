import React, { useState, useEffect } from 'react';
import {
  Music, Search, RefreshCw, ListTodo, XCircle, Sparkles, Home,
  Shield, CalendarDays, Globe
} from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { memoryService } from '../services/memoryService';

const IS_ELECTRON = typeof window !== 'undefined' && !!window.electronAPI?.isElectron;
// User-Agent padrão Chrome para o emulador (o Google recusa Electron)
const CHROME_UA = (typeof window !== 'undefined' && window.electronAPI?.chromeUserAgent) || '';

const YT_HOME = 'https://music.youtube.com';
const CALENDAR_URL = 'https://calendar.google.com/calendar/u/0/r';
const ytSearchUrl = (q) => `${YT_HOME}/search?q=${encodeURIComponent(q)}`;

// Emulador de navegador do J.A.R.V.I.S — tudo dentro do box, SEM janelas externas.
// Guias (abas): Música (YouTube Music) | Agenda (Google Calendar com login) |
// Processos (antigo gerenciador de tarefas). As duas guias Google compartilham a
// mesma sessão: logou em uma, está logado na outra — e a sessão fica salva.
// Brave Shields emulado bloqueia anúncios/rastreadores (contador no rodapé).
// Por voz: "tocar <música>".
export default function MusicBox({ search }) {
  const [tab, setTab] = useState(() => {
    // Restaura a última guia aberta (memória permanente)
    try {
      const t = (memoryService.getPreferences() || {}).emulatorTab;
      return t === 'agenda' || t === 'processes' ? t : 'music';
    } catch { return 'music'; }
  });
  const [queryText, setQueryText] = useState('');
  const [srcMusic, setSrcMusic] = useState(() => {
    // Restaura a última página da guia Música
    try {
      const u = (memoryService.getPreferences() || {}).emulatorMusicUrl;
      return typeof u === 'string' && u.includes('music.youtube.com') ? u : YT_HOME;
    } catch { return YT_HOME; }
  });
  const [processes, setProcesses] = useState([]);
  const [isRefreshing, setIsRefreshing] = useState(false);

  // Brave Shields emulado (bloqueio de anúncios/rastreadores + contador)
  const [shields, setShields] = useState(() => {
    try {
      const v = (memoryService.getPreferences() || {}).shieldsEnabled;
      return { blocked: 0, enabled: v !== false };
    } catch { return { blocked: 0, enabled: true }; }
  });

  // Estado do Shields (contador de anúncios bloqueados + reaplica preferência)
  useEffect(() => {
    let alive = true;
    Promise.resolve(window.electronAPI?.shieldsStatus?.())
      .then((s) => {
        if (!alive || !s) return;
        let enabled = s.enabled !== false;
        try {
          const v = (memoryService.getPreferences() || {}).shieldsEnabled;
          if (v === false) enabled = false;
        } catch { /* ignora */ }
        if (enabled !== s.enabled) window.electronAPI?.shieldsSetEnabled?.(enabled);
        setShields({ blocked: s.blocked || 0, enabled });
      })
      .catch(() => { /* ignora */ });
    window.electronAPI?.onShieldsBlocked?.((total) => {
      if (alive) setShields((p) => ({ ...p, blocked: total }));
    });
    return () => { alive = false; };
  }, []);

  // Troca de guia e navegação ficam salvas entre reinícios
  const goTab = (t) => {
    setTab(t);
    memoryService.updatePreferences({ emulatorTab: t });
  };
  const openMusicUrl = (url) => {
    setSrcMusic(url);
    if (url.includes('music.youtube.com')) memoryService.updatePreferences({ emulatorMusicUrl: url });
  };

  // A agenda local (box da esquerda) pede para abrir a guia do Google aqui
  useEffect(() => {
    const onOpenTab = (e) => {
      const t = e?.detail;
      if (t === 'agenda' || t === 'music' || t === 'processes') goTab(t);
    };
    window.addEventListener('jarvis-open-tab', onOpenTab);
    return () => window.removeEventListener('jarvis-open-tab', onOpenTab);
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

  // Comando de voz "tocar X": navega a guia Música para a busca
  useEffect(() => {
    if (!search || !search.q) return;
    goTab('music');
    setQueryText(search.q);
    openMusicUrl(ytSearchUrl(search.q));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  const fetchProcesses = async () => {
    setIsRefreshing(true);
    if (window.electronAPI?.getRunningProcesses) {
      try {
        const list = await window.electronAPI.getRunningProcesses();
        if (list && list.length > 0) setProcesses(list);
      } catch (e) {
        console.warn('Erro ao ler processos do Windows:', e);
      }
    }
    setTimeout(() => setIsRefreshing(false), 600);
  };

  // Varre processos apenas quando a guia está visível (economia de CPU)
  useEffect(() => {
    if (tab !== 'processes') return undefined;
    fetchProcesses();
    const interval = setInterval(fetchProcesses, 5000);
    return () => clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab]);

  const submitSearch = (e) => {
    e.preventDefault();
    const q = queryText.trim();
    if (!q) return;
    sounds.playClick();
    openMusicUrl(ytSearchUrl(q));
  };

  const killProcess = (app) => {
    sounds.playAlert();
    if (window.electronAPI?.processControl) {
      window.electronAPI.processControl('KILL', app.process);
    }
    setProcesses(prev => prev.filter(a => a.pid !== app.pid));
  };

  const tabBtn = (id, Icon, label, title) => (
    <button
      onClick={() => { sounds.playClick(); goTab(id); }}
      title={title}
      className={`text-[10px] p-1 rounded border transition-colors flex items-center gap-1 ${
        tab === id
          ? 'bg-[var(--primary)] text-black border-cyan-300 font-bold'
          : 'bg-cyan-950/40 hover:bg-cyan-900 border-cyan-500/30 text-cyan-300'
      }`}
    >
      <Icon className="w-2.5 h-2.5" /> {label}
    </button>
  );

  return (
    <section data-emulator className="hud-panel rounded-2xl p-3 sm:p-4 border-[var(--panel-border)] flex flex-col">
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Header: emulador + guias (Música | Agenda Google | Processos) */}
      <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2 mb-2 font-['JetBrains_Mono'] gap-1">
        <h2 className="text-xs uppercase font-['Orbitron'] tracking-wider text-[var(--primary)] flex items-center gap-1.5 glow-cyan shrink-0">
          <Globe className="w-3.5 h-3.5 text-[var(--primary)]" />
          Emulador
        </h2>
        <div className="flex items-center space-x-1">
          {tabBtn('music', Music, 'Música', 'Guia YouTube Music (embutido, sem janela extra)')}
          {tabBtn('agenda', CalendarDays, 'Agenda', 'Guia da SUA agenda do Google (Google Calendar)')}
          {tabBtn('processes', ListTodo, 'Proc.', 'Antigo Gerenciador de Tarefas')}
        </div>
      </div>

      {tab === 'processes' ? (
        <>
          <div className="flex items-center justify-end mb-1.5 space-x-1">
            <button
              onClick={fetchProcesses}
              title="Atualizar lista de processos"
              className="text-[10px] bg-cyan-950/40 hover:bg-[var(--primary)] hover:text-black p-1 rounded border border-cyan-500/30 text-cyan-300 transition-colors"
            >
              <RefreshCw className={`w-3 h-3 ${isRefreshing ? 'animate-spin' : ''}`} />
            </button>
            <button
              onClick={() => { sounds.playPowerPulse(); fetchProcesses(); }}
              className="text-[10px] bg-cyan-950/40 hover:bg-[var(--primary)] hover:text-black px-1.5 py-0.5 rounded border border-cyan-500/30 text-cyan-300 transition-colors flex items-center gap-1"
            >
              <Sparkles className="w-2.5 h-2.5" /> Otimizar
            </button>
          </div>

          <div className="space-y-1.5 font-['JetBrains_Mono'] text-xs h-56 sm:h-64 overflow-y-auto scrollbar-none">
            {processes.map(app => (
              <div
                key={app.pid || app.id}
                className="p-1.5 rounded-lg bg-cyan-950/20 border border-cyan-500/20 flex items-center justify-between hover:border-[var(--primary)] transition-all"
              >
                <div className="overflow-hidden min-w-0">
                  <div className="font-semibold text-cyan-200 truncate text-[11px] flex items-center gap-1.5">
                    <span>{app.name}</span>
                    {app.pid && (
                      <span className="text-[9px] px-1 py-0.2 rounded bg-black/50 text-cyan-400 border border-cyan-500/30">
                        PID {app.pid}
                      </span>
                    )}
                  </div>
                  <div className="text-[9px] text-cyan-500 truncate flex items-center gap-2">
                    <span>{app.process}</span>
                    {app.memory && <span className="text-amber-300 font-semibold">{app.memory}</span>}
                  </div>
                </div>

                <button
                  onClick={() => killProcess(app)}
                  title={`Finalizar ${app.name}`}
                  className="p-1 text-rose-400/70 hover:text-rose-400 hover:bg-rose-950/40 rounded transition-colors ml-2 shrink-0"
                >
                  <XCircle className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>
        </>
      ) : (
        <>
          {/* Busca por voz ou texto (guia Música) */}
          {tab === 'music' && (
            <form onSubmit={submitSearch} className="flex items-center gap-1.5 mb-2">
              <div className="relative flex-1 min-w-0">
                <Search className="w-3 h-3 absolute left-2 top-1/2 -translate-y-1/2 text-cyan-500" />
                <input
                  type="text"
                  value={queryText}
                  onChange={(e) => setQueryText(e.target.value)}
                  placeholder='Fale: "tocar a música X"...'
                  className="w-full bg-black/60 border border-cyan-500/40 rounded pl-6 pr-2 py-1.5 text-[11px] text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                />
              </div>
              <button
                type="submit"
                title="Buscar no YouTube Music"
                className="p-1.5 rounded bg-cyan-950/60 hover:bg-[var(--primary)] hover:text-black border border-cyan-500/30 text-cyan-300 transition-colors shrink-0"
              >
                <Search className="w-3.5 h-3.5" />
              </button>
              <button
                type="button"
                onClick={() => { sounds.playClick(); setQueryText(''); openMusicUrl(YT_HOME); }}
                title="Página inicial do YouTube Music"
                className="p-1.5 rounded bg-cyan-950/60 hover:bg-[var(--primary)] hover:text-black border border-cyan-500/30 text-cyan-300 transition-colors shrink-0"
              >
                <Home className="w-3.5 h-3.5" />
              </button>
            </form>
          )}

          {/* Área do emulador: as duas guias Google ficam sempre carregadas
              (mesma sessão/partição) — a inativa fica oculta sem recarregar */}
          <div className="relative w-full h-56 sm:h-64 bg-black rounded-xl overflow-hidden border border-cyan-500/20">
            {IS_ELECTRON ? (
              <>
                <div className={tab === 'music' ? 'w-full h-full' : 'hidden'}>
                  <webview
                    key={srcMusic}
                    src={srcMusic}
                    useragent={CHROME_UA}
                    allowpopups="true"
                    className="border-0"
                    style={{ width: '100%', height: '100%', display: 'block' }}
                    title="YouTube Music no emulador do J.A.R.V.I.S."
                  />
                </div>
                <div className={tab === 'agenda' ? 'w-full h-full' : 'hidden'}>
                  <webview
                    src={CALENDAR_URL}
                    useragent={CHROME_UA}
                    allowpopups="true"
                    className="border-0"
                    style={{ width: '100%', height: '100%', display: 'block' }}
                    title="Agenda do Google no emulador do J.A.R.V.I.S."
                  />
                </div>
              </>
            ) : (
              <iframe
                key={tab === 'music' ? srcMusic : 'agenda'}
                src={tab === 'music' ? srcMusic : CALENDAR_URL}
                title="Emulador do J.A.R.V.I.S."
                className="w-full h-full border-0"
                allow="autoplay; encrypted-media; clipboard-write; picture-in-picture"
                sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals allow-presentation"
              />
            )}
          </div>
        </>
      )}

      {/* Rodapé: Shields na guia Música / status da guia ativa */}
      <div className="mt-2 pt-2 border-t border-cyan-500/20 flex items-center justify-between gap-2 text-[10px] font-['JetBrains_Mono'] text-cyan-400/80">
        {tab === 'agenda' ? (
          <>
            <span className="truncate">GUIA AGENDA • GOOGLE CALENDAR</span>
            <span className="text-emerald-400 font-bold shrink-0">SESSÃO COMPARTILHADA</span>
          </>
        ) : tab === 'music' ? (
          <>
            <button
              type="button"
              onClick={toggleShields}
              title={`Brave Shields emulado: ${shields.blocked} anúncios/rastreadores bloqueados — clique para ${shields.enabled ? 'desligar' : 'ligar'}`}
              className={`flex items-center gap-1 px-1.5 py-0.5 rounded border text-[10px] font-bold transition-colors ${
                shields.enabled
                  ? 'border-amber-400/40 bg-amber-500/10 text-amber-300 hover:bg-amber-500/25'
                  : 'border-cyan-500/30 bg-cyan-950/40 text-cyan-500'
              }`}
            >
              <Shield className={`w-3 h-3 ${shields.enabled ? '' : 'opacity-40'}`} />
              SHIELDS: {shields.enabled ? `${shields.blocked} BLOQ.` : 'OFF'}
            </button>
            <span className="text-emerald-400 font-bold shrink-0">PRONTO PARA VOZ</span>
          </>
        ) : (
          <>
            <span>{processes.length} APPS MONITORADOS</span>
            <span className="text-emerald-400 font-bold">PROCESSO REAL DO PC</span>
          </>
        )}
      </div>
    </section>
  );
}
