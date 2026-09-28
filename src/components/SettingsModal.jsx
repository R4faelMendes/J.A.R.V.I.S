import React, { useState, useEffect } from 'react';
import { X, Settings, User, Palette, Volume2, Cpu, Check, Shield, Mic, Gauge, Ear, Bot, RotateCcw } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { memoryService } from '../services/memoryService';
import { speechService } from '../services/speechService';
import { aiService } from '../services/aiService';

export default function SettingsModal({ isOpen, onClose, activeTheme, onThemeChange, embedded = false }) {
  const [prefs, setPrefs] = useState(() => {
    try {
      return memoryService.getPreferences() || {};
    } catch {
      return {};
    }
  });
  const [savedMessage, setSavedMessage] = useState(false);
  const [customColor, setCustomColor] = useState('#00f0ff');
  const [autostartEnabled, setAutostartEnabled] = useState(false);
  const [neuralVoices, setNeuralVoices] = useState([]);
  const [localVoices, setLocalVoices] = useState([]);
  const [micTest, setMicTest] = useState(null);
  const [testingMic, setTestingMic] = useState(false);
  const [inputDevices, setInputDevices] = useState([]);
  const [n8nTest, setN8nTest] = useState(null);

  useEffect(() => {
    if (isOpen && window.electronAPI?.getAutostart) {
      window.electronAPI.getAutostart().then(val => {
        setAutostartEnabled(!!val);
      }).catch(() => {});
    }
    if (isOpen) {
      setLocalVoices(speechService.listLocalVoices());
      if (window.electronAPI?.listNeuralVoices) {
        window.electronAPI.listNeuralVoices()
          .then(voices => setNeuralVoices(Array.isArray(voices) ? voices : []))
          .catch(() => setNeuralVoices([]));
      }
      // Microfones do sistema (a 1ª permissão libera os rótulos)
      speechService.listInputDevices()
        .then(devices => setInputDevices(Array.isArray(devices) ? devices : []))
        .catch(() => setInputDevices([]));
    }
  }, [isOpen]);

  // MEMÓRIA PERMANENTE: toda alteração feita no modal é salva automaticamente
  // (não é mais preciso clicar em "Salvar Diretivas" para não perder tudo ao fechar)
  useEffect(() => {
    if (!isOpen) return undefined;
    try {
      memoryService.updatePreferences(prefs);
      speechService.configure({
        voiceEngine: prefs.voiceEngine || 'neural',
        voiceId: prefs.voiceId || 'pt-BR-AntonioNeural',
        localVoiceName: prefs.localVoiceName || '',
        voiceSpeed: prefs.voiceSpeed || 1.05,
        voicePitch: prefs.voicePitch || 0.85,
        sttModel: prefs.sttModel || 'base',
        micDeviceId: prefs.micDeviceId || ''
      });
    } catch { /* ignora */ }
    return undefined;
  }, [prefs, isOpen]);

  if (!isOpen && !embedded) return null;

  const themes = [
    { id: 'cyan', name: 'Cyan Stark (Clássico)', border: 'border-cyan-400', color: 'bg-cyan-400' },
    { id: 'amber', name: 'Mark III Dourado / Âmbar', border: 'border-amber-400', color: 'bg-amber-400' },
    { id: 'crimson', name: 'Hulkbuster Vermelho Carmesim', border: 'border-rose-500', color: 'bg-rose-500' },
    { id: 'emerald', name: 'Time Stone Esmeralda', border: 'border-emerald-400', color: 'bg-emerald-400' },
    { id: 'purple', name: 'Power Stone Cósmico', border: 'border-purple-400', color: 'bg-purple-400' }
  ];

  const handleSave = (e) => {
    e.preventDefault();
    sounds.playSuccess();
    try {
      memoryService.updatePreferences(prefs);
      speechService.configure({
        voiceEngine: prefs.voiceEngine || 'neural',
        voiceId: prefs.voiceId || 'pt-BR-AntonioNeural',
        localVoiceName: prefs.localVoiceName || '',
        voiceSpeed: prefs.voiceSpeed || 1.05,
        voicePitch: prefs.voicePitch || 0.85,
        sttModel: prefs.sttModel || 'base',
        micDeviceId: prefs.micDeviceId || ''
      });
    } catch (err) {
      console.warn('Erro ao salvar preferências:', err);
    }
    setSavedMessage(true);
    setTimeout(() => setSavedMessage(false), 2000);
  };

  const handleCustomColor = (color) => {
    setCustomColor(color);
    try {
      document.documentElement.style.setProperty('--primary', color);
      document.documentElement.style.setProperty('--primary-glow', `${color}70`);
      document.documentElement.style.setProperty('--primary-dim', `${color}25`);
    } catch (err) {}
    sounds.playClick();
  };

  const handleTestVoice = () => {
    sounds.playClick();
    try {
      speechService.configure({
        voiceEngine: prefs.voiceEngine || 'neural',
        voiceId: prefs.voiceId || 'pt-BR-AntonioNeural',
        localVoiceName: prefs.localVoiceName || '',
        voiceSpeed: prefs.voiceSpeed || 1.05,
        voicePitch: prefs.voicePitch || 0.85
      });
      speechService.speak('Sistemas de síntese vocal calibrados e operando em capacidade total, Senhor.');
    } catch (e) { /* ignora */ }
  };

  // Aplica um preset de voz (tom do Jarvis) e toca uma amostra
  const applyVoicePreset = (preset) => {
    sounds.playClick();
    const patch = { ...preset.cfg, jarvisVoicePreset: preset.id };
    setPrefs(p => ({ ...p, ...patch }));
    try {
      speechService.configure(patch);
      speechService.speak('Voz calibrada, Senhor. Todos os sistemas prontos para novas diretivas.');
    } catch { /* ignora */ }
  };

  // Restaura a voz do J.A.R.V.I.S. para o padrão atual (Antonio clássico)
  const resetVoiceDefaults = () => {
    sounds.playClick();
    const patch = {
      voiceEngine: 'neural',
      voiceId: 'pt-BR-AntonioNeural',
      localVoiceName: '',
      voicePitch: 0.85,
      voiceSpeed: 1.05,
      jarvisVoicePreset: 'classico'
    };
    setPrefs(p => ({ ...p, ...patch }));
    try {
      speechService.configure(patch);
      speechService.speak('Voz padrão do J.A.R.V.I.S. restaurada, Senhor. Calibração concluída.');
    } catch { /* ignora */ }
  };

  const handleVoiceChange = (value) => {
    if (value.startsWith('neural::')) {
      setPrefs(p => ({ ...p, voiceEngine: 'neural', voiceId: value.slice(8), jarvisVoicePreset: 'custom' }));
    } else if (value.startsWith('local::')) {
      setPrefs(p => ({ ...p, voiceEngine: 'local', localVoiceName: value.slice(7), jarvisVoicePreset: 'custom' }));
    }
    sounds.playClick();
  };

  const selectedVoiceValue = prefs.voiceEngine === 'local'
    ? `local::${prefs.localVoiceName || ''}`
    : `neural::${prefs.voiceId || 'pt-BR-AntonioNeural'}`;

  const handleTestMic = async () => {
    sounds.playClick();
    setTestingMic(true);
    setMicTest(null);
    // Aplica o microfone escolhido (sem exigir "Salvar") antes de testar
    speechService.configure({ micDeviceId: prefs.micDeviceId || '' });
    const result = await speechService.testMicrophone();
    setTestingMic(false);
    if (result.ok) sounds.playSuccess();
    else sounds.playAlert();
    setMicTest(result);
  };

  const handleToggleAutostart = async () => {
    const next = !autostartEnabled;
    setAutostartEnabled(next);
    sounds.playClick();
    if (window.electronAPI?.setAutostart) {
      await window.electronAPI.setAutostart(next);
    }
  };

  return (
    <div className={embedded ? "flex-1 min-h-0 flex flex-col" : "fixed inset-0 z-50 flex items-center justify-center p-3 bg-black/80 backdrop-blur-md animate-in fade-in duration-200"}>
      <div className={embedded ? "syn-panel rounded-xl w-full flex-1 min-h-0 flex flex-col border-[var(--primary)] overflow-hidden" : "syn-panel rounded-xl w-full max-w-2xl max-h-[90vh] flex flex-col border-[var(--primary)] shadow-[0_0_40px_var(--primary-glow)] overflow-hidden"}>





        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-cyan-500/20 bg-slate-950/60">
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded bg-cyan-950/60 border border-[var(--primary)] text-[var(--primary)]">
              <Settings className="w-5 h-5" />
            </div>
            <div>
              <h2 className="font-syn-title text-lg font-bold" style={{ color: '#eaf8f4' }}>
                Ajustes do organismo
              </h2>
              <p className="font-syn-body text-xs font-light" style={{ color: '#9aa6c9' }}>
                Inteligência, voz, vigília e preferências de convivência
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

        {/* Body Form */}
        <form onSubmit={handleSave} className="flex-1 overflow-y-auto p-6 space-y-6 font-syn-body">
          {/* Section 1: Themes */}
          <div>
            <label className="text-xs uppercase text-cyan-300 font-bold mb-3 flex items-center gap-2">
              <Palette className="w-4 h-4 text-[var(--primary)]" />
              Paleta Holográfica & Cores do Reator
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
              {themes.map(t => (
                <div
                  key={t.id}
                  onClick={() => {
                    sounds.playClick();
                    onThemeChange(t.id);
                    setPrefs(p => ({ ...p, theme: t.id }));
                  }}
                  className={`p-3 rounded-lg border flex items-center justify-between cursor-pointer transition-all ${
                    activeTheme === t.id
                      ? 'bg-[var(--primary-dim)] border-[var(--primary)] shadow-[0_0_12px_var(--primary-glow)]'
                      : 'bg-cyan-950/20 border-cyan-500/20 hover:border-cyan-400'
                  }`}
                >
                  <div className="flex items-center space-x-2.5">
                    <span className={`w-4 h-4 rounded-full ${t.color}`} />
                    <span className="text-xs text-cyan-100 font-semibold">{t.name}</span>
                  </div>
                  {activeTheme === t.id && (
                    <Check className="w-4 h-4 text-[var(--primary)]" />
                  )}
                </div>
              ))}
            </div>

            {/* Custom Color Palette Picker */}
            <div className="mt-3 p-2.5 rounded bg-black/40 border border-cyan-500/30 flex items-center justify-between">
              <div className="text-xs text-cyan-200 flex items-center gap-2">
                <span>🎨 Paleta Personalizada Livre (HEX/RGB):</span>
                <input
                  type="color"
                  value={customColor}
                  onChange={(e) => handleCustomColor(e.target.value)}
                  className="w-7 h-7 rounded border border-cyan-400 cursor-pointer bg-transparent"
                />
                <span className="text-cyan-400 font-mono">{customColor}</span>
              </div>
            </div>
          </div>

          {/* Windows Startup Option */}
          <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 flex items-center justify-between">
            <div>
              <div className="text-xs uppercase text-cyan-300 font-bold flex items-center gap-2">
                <Shield className="w-4 h-4 text-[var(--primary)]" />
                Inicialização com o Windows
              </div>
              <p className="text-[11px] text-cyan-400/70 mt-0.5">
                Iniciar o J.A.R.V.I.S. automaticamente ao ligar o computador da escola/pessoal.
              </p>
            </div>
            <button
              type="button"
              onClick={handleToggleAutostart}
              className={`px-3 py-1.5 rounded text-xs font-bold font-mono transition-all border ${
                autostartEnabled
                  ? 'bg-emerald-500 text-black border-emerald-400 shadow-[0_0_8px_#10b981]'
                  : 'bg-black/60 text-slate-400 border-slate-600'
              }`}
            >
              {autostartEnabled ? 'ATIVADO' : 'DESATIVADO'}
            </button>
          </div>

          {/* Section 2: User Profile */}
          <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 space-y-3">
            <div className="text-xs uppercase text-cyan-300 font-bold flex items-center gap-2">
              <User className="w-4 h-4 text-[var(--primary)]" />
              Perfil de Operador & Nível de Autorização
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="text-[11px] text-cyan-400/80 block mb-1">Nome do Operador</label>
                <input
                  type="text"
                  value={prefs.userName || ''}
                  onChange={(e) => setPrefs({ ...prefs, userName: e.target.value })}
                  className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                />
              </div>
              <div>
                <label className="text-[11px] text-cyan-400/80 block mb-1">Credencial de Segurança</label>
                <input
                  type="text"
                  value={prefs.accessLevel || 'NÍVEL ALPHA'}
                  onChange={(e) => setPrefs({ ...prefs, accessLevel: e.target.value })}
                  className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                />
              </div>
            </div>
          </div>

          {/* Section 3: AI Cognitive Engine */}
          <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 space-y-3">
            <div className="text-xs uppercase text-cyan-300 font-bold flex items-center gap-2">
              <Cpu className="w-4 h-4 text-[var(--primary)]" />
              Cérebro de Inteligência Artificial
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs">
              {[
                { id: 'free', label: 'Nuvem gratuita (sem chave) — recomendada' },
                { id: 'local', label: 'Cérebro local primeiro (Ollama)' }
              ].map(provider => (
                <button
                  key={provider.id}
                  type="button"
                  onClick={() => { sounds.playClick(); setPrefs({ ...prefs, aiProvider: provider.id }); }}
                  className={`p-2 rounded border transition-all text-center ${
                    (prefs.aiProvider || 'free') === provider.id
                      ? 'bg-[var(--primary)] text-black font-bold border-cyan-300'
                      : 'bg-black/50 border-cyan-500/20 text-cyan-300 hover:border-cyan-400'
                  }`}
                >
                  {provider.label}
                </button>
              ))}
            </div>
            <div>
              <label className="text-[11px] text-cyan-400/80 block mb-1">Modelo Ollama local (fallback automático)</label>
              <input
                type="text"
                value={prefs.ollamaModel || 'llama3.1'}
                onChange={(e) => setPrefs({ ...prefs, ollamaModel: e.target.value })}
                placeholder="llama3.1"
                className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs font-syn-body text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
              />
              <p className="text-[10px] text-cyan-500/70 font-syn-body mt-1">
                Requer Ollama instalado (`ollama serve` + `ollama pull llama3.1`). Quando a nuvem falha, o organismo pensa localmente — o núcleo ganha glow violeta.
              </p>
            </div>
            <p className="text-[10px] text-cyan-500/70 font-syn-body">
              Nenhuma chave de API é necessária: a Nuvem Gratuita responde direto, sem cadastro e sem token.
            </p>
          </div>

          {/* Section 3a: Cérebro n8n (agente com ferramentas reais) */}
          <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 space-y-3">
            <div className="text-xs uppercase text-cyan-300 font-bold flex items-center gap-2">
              <Bot className="w-4 h-4 text-[var(--primary)]" />
              Cérebro n8n (age de verdade)
            </div>
            <p className="text-[11px] font-syn-body font-light" style={{ color: '#9aa6c9' }}>
              Workflow “JARVIS Agent” no n8n (pasta <code>n8n/</code> do projeto): e-mail, agenda,
              contatos e pesquisa web. Quando ativo, vira o primeiro provedor; se falhar, a fila
              cai para nuvem → Ollama → tático sem você perceber.
            </p>
            <button
              type="button"
              onClick={() => { sounds.playClick(); setPrefs((p) => ({ ...p, n8nEnabled: !p.n8nEnabled })); }}
              className="syn-pill px-4 py-1.5 text-xs font-bold"
              style={prefs.n8nEnabled
                ? { background: 'rgba(45,253,212,0.18)', color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.45)' }
                : { color: '#9aa6c9', border: '1px solid rgba(154,166,201,0.3)' }}
            >
              {prefs.n8nEnabled ? 'Cérebro n8n ativo' : 'Cérebro n8n desligado'}
            </button>
            <div>
              <label className="text-[11px] text-cyan-400/80 block mb-1">URL do webhook (POST /webhook/jarvis-agent)</label>
              <input
                type="url"
                value={prefs.n8nWebhookUrl || ''}
                onChange={(e) => setPrefs({ ...prefs, n8nWebhookUrl: e.target.value })}
                placeholder="https://seu-n8n/webhook/jarvis-agent"
                className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs font-syn-body text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
              />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="text-[11px] text-cyan-400/80 block mb-1">Chave (Header Auth X-Jarvis-Key)</label>
                <input
                  type="password"
                  value={prefs.n8nKey || ''}
                  onChange={(e) => setPrefs({ ...prefs, n8nKey: e.target.value })}
                  placeholder="token secreto do webhook"
                  autoComplete="off"
                  className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs font-syn-body text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                />
              </div>
              <div>
                <label className="text-[11px] text-cyan-400/80 block mb-1">ID do operador (userId)</label>
                <input
                  type="text"
                  value={prefs.n8nUserId || 'tony-stark'}
                  onChange={(e) => setPrefs({ ...prefs, n8nUserId: e.target.value })}
                  placeholder="tony-stark"
                  className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs font-syn-body text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={async () => {
                  sounds.playClick();
                  setN8nTest({ ok: null, message: 'Consultando o cérebro…' });
                  const res = await aiService.testN8n();
                  if (res.ok) sounds.playSuccess();
                  else sounds.playAlert();
                  setN8nTest(res);
                }}
                className="px-3 py-1.5 rounded bg-cyan-950/80 border border-cyan-400 text-cyan-300 hover:bg-[var(--primary)] hover:text-black transition-all text-xs font-bold"
              >
                Testar conexão
              </button>
              {n8nTest && (
                <span className="text-[11px] font-syn-body" style={{ color: n8nTest.ok ? '#2dfdd4' : n8nTest.ok === null ? '#9aa6c9' : '#ff5c7a' }}>
                  {n8nTest.message}
                </span>
              )}
            </div>
          </div>

          {/* Section 3b: Vigília visual (opt-in explícito) */}
          <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 space-y-3">
            <div className="text-xs uppercase text-cyan-300 font-bold flex items-center gap-2">
              <Shield className="w-4 h-4 text-[var(--primary)]" />
              Vigília visual (opt-in)
            </div>
            <p className="text-[11px] font-syn-body font-light" style={{ color: '#9aa6c9' }}>
              Com seu consentimento, o organismo observa o tecido de processos e comenta proativamente
              (ex: muitas frentes abertas). Nenhum pixel sai da máquina sem o modo com visão configurado.
            </p>
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                onClick={() => { sounds.playClick(); setPrefs((p) => ({ ...p, screenWatch: !p.screenWatch })); }}
                className="syn-pill px-4 py-1.5 text-xs font-bold"
                style={prefs.screenWatch
                  ? { background: 'rgba(45,253,212,0.18)', color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.45)' }
                  : { color: '#9aa6c9', border: '1px solid rgba(154,166,201,0.3)' }}
              >
                {prefs.screenWatch ? 'Vigília ativa' : 'Vigília desligada'}
              </button>
              <label className="text-[11px] text-cyan-400/80 flex items-center gap-1.5">
                Intervalo
                <select
                  value={prefs.screenWatchIntervalSec || 60}
                  onChange={(e) => setPrefs((p) => ({ ...p, screenWatchIntervalSec: Number(e.target.value) }))}
                  className="bg-black/60 border border-cyan-500/40 rounded px-2 py-1 text-xs text-cyan-200 focus:outline-none"
                >
                  <option value={30}>30s</option>
                  <option value={60}>60s</option>
                  <option value={180}>3 min</option>
                  <option value={300}>5 min</option>
                </select>
              </label>
            </div>
          </div>

          {/* Section 4: Voice Synthesizer Calibration */}
          <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 space-y-4">
            <div className="text-xs uppercase text-cyan-300 font-bold flex items-center gap-2">
              <Volume2 className="w-4 h-4 text-[var(--primary)]" />
              Calibração Vocal do J.A.R.V.I.S.
            </div>

            {/* Presets de voz (tom do Jarvis do filme) */}
            <div>
              <label className="text-[11px] text-cyan-400/80 block mb-1">Predefinições de voz</label>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                {[
                  {
                    id: 'classico',
                    name: 'J.A.R.V.I.S. Clássico',
                    desc: 'Brilhante, profundo e direto',
                    cfg: { voiceEngine: 'neural', voiceId: 'pt-BR-AntonioNeural', voicePitch: 0.85, voiceSpeed: 1.05 }
                  },
                  {
                    id: 'filme',
                    name: 'Stark / Filme (inglês)',
                    desc: 'Britânico, como o Jarvis original',
                    cfg: { voiceEngine: 'neural', voiceId: 'en-GB-ThomasNeural', voicePitch: 0.9, voiceSpeed: 1.0 }
                  },
                  {
                    id: 'padrao',
                    name: 'Neural Padrão',
                    desc: 'Antonio neutro, sem alterações',
                    cfg: { voiceEngine: 'neural', voiceId: 'pt-BR-AntonioNeural', voicePitch: 1.0, voiceSpeed: 1.0 }
                  }
                ].map(preset => (
                  <button
                    key={preset.id}
                    type="button"
                    onClick={() => applyVoicePreset(preset)}
                    className={`p-2 rounded border text-left transition-all ${
                      (prefs.jarvisVoicePreset || 'classico') === preset.id
                        ? 'bg-[var(--primary-dim)] border-[var(--primary)] shadow-[0_0_10px_var(--primary-glow)]'
                        : 'bg-cyan-950/20 border-cyan-500/20 hover:border-cyan-400'
                    }`}
                  >
                    <div className="text-[11px] font-bold text-cyan-100">{preset.name}</div>
                    <div className="text-[10px] text-cyan-400/80">{preset.desc}</div>
                  </button>
                ))}
              </div>
            </div>

            {/* Voice selection: neural (Microsoft) x local (sistema) */}
            <div>
              <label className="text-[11px] text-cyan-400/80 block mb-1">Voz do assistente</label>
              <select
                value={selectedVoiceValue}
                onChange={(e) => handleVoiceChange(e.target.value)}
                className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-2 text-xs font-syn-body text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
              >
                <optgroup label="Neural Microsoft (fluida • recomendada • requer internet)">
                  {neuralVoices.length === 0 && (
                    <option value="neural::pt-BR-AntonioNeural">Neural (carregando lista...)</option>
                  )}
                  {neuralVoices.map(v => (
                    <option key={v.shortName} value={`neural::${v.shortName}`}>
                      {v.friendlyName}{v.gender ? ` • ${v.gender === 'Female' ? 'Feminina' : 'Masculina'}` : ''}
                    </option>
                  ))}
                </optgroup>
                <optgroup label="Vozes locais do Windows (offline • mais robóticas)">
                  {localVoices.length === 0 && (
                    <option value="local::">Nenhuma voz local detectada</option>
                  )}
                  {localVoices.map(v => (
                    <option key={v.name} value={`local::${v.name}`}>
                      {v.name} ({v.lang})
                    </option>
                  ))}
                </optgroup>
              </select>
              <p className="text-[10px] text-cyan-500/70 mt-1 font-syn-body">
                {prefs.voiceEngine === 'local'
                  ? 'Voz local: funciona sem internet, porém com entonação mais robótica.'
                  : 'Voz neural: entonação natural e fluída (estilo ChatGPT), sintetizada em tempo real.'}
              </p>
            </div>

            {/* Speed / Pitch */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="text-[11px] text-cyan-400/80 flex items-center justify-between mb-1">
                  <span>Velocidade da fala</span>
                  <span className="text-[var(--primary)] font-bold">{Number(prefs.voiceSpeed || 1.0).toFixed(2)}x</span>
                </label>
                <input
                  type="range"
                  min="0.7"
                  max="1.5"
                  step="0.05"
                  value={prefs.voiceSpeed || 1.0}
                  onChange={(e) => setPrefs(p => ({ ...p, voiceSpeed: parseFloat(e.target.value) }))}
                  className="w-full accent-cyan-400"
                />
              </div>
              <div>
                <label className="text-[11px] text-cyan-400/80 flex items-center justify-between mb-1">
                  <span>Tom de voz</span>
                  <span className="text-[var(--primary)] font-bold">{Number(prefs.voicePitch || 1.0).toFixed(2)}</span>
                </label>
                <input
                  type="range"
                  min="0.7"
                  max="1.4"
                  step="0.05"
                  value={prefs.voicePitch || 1.0}
                  onChange={(e) => setPrefs(p => ({ ...p, voicePitch: parseFloat(e.target.value) }))}
                  className="w-full accent-cyan-400"
                />
              </div>
            </div>

            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={resetVoiceDefaults}
                title="Voltar para a voz padrão do J.A.R.V.I.S. (Antonio clássico, tom 0.85, velocidade 1.05)"
                className="px-3 py-1.5 rounded bg-cyan-950/80 border border-cyan-500/40 text-cyan-300 hover:border-cyan-400 hover:text-white transition-all text-xs font-bold flex items-center gap-1.5"
              >
                <RotateCcw className="w-3.5 h-3.5" /> Restaurar Voz Padrão
              </button>
              <button
                type="button"
                onClick={handleTestVoice}
                className="px-3 py-1.5 rounded bg-cyan-950/80 border border-cyan-400 text-cyan-300 hover:bg-[var(--primary)] hover:text-black transition-all text-xs font-bold"
              >
                Testar Voz
              </button>
            </div>
          </div>

          {/* Section 5: Microphone & Voice Recognition */}
          <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 space-y-3">
            <div className="text-xs uppercase text-cyan-300 font-bold flex items-center gap-2">
              <Mic className="w-4 h-4 text-[var(--primary)]" />
              Microfone & Reconhecimento de Voz
            </div>

            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="text-[11px] text-cyan-400/70 font-syn-body">
                Motor ativo:{' '}
                <span className="text-[var(--primary)] font-bold">
                  {speechService.getEngineInfo().stt === 'local'
                    ? `Whisper local offline (${prefs.sttModel || 'base'})`
                    : speechService.getEngineInfo().stt === 'webspeech' ? 'Web Speech (online)' : 'indisponível'}
                </span>
                {' '}• Síntese:{' '}
                <span className="text-[var(--primary)] font-bold">
                  {speechService.getEngineInfo().tts === 'neural' ? 'Neural Microsoft' : 'Local do sistema'}
                </span>
              </div>
              <button
                type="button"
                onClick={handleTestMic}
                disabled={testingMic}
                className="px-3 py-1.5 rounded bg-cyan-950/80 border border-cyan-400 text-cyan-300 hover:bg-[var(--primary)] hover:text-black transition-all text-xs font-bold disabled:opacity-60"
              >
                {testingMic ? 'Testando...' : 'Testar Microfone'}
              </button>
            </div>

            {micTest && (
              <div className={`text-[11px] font-syn-body rounded px-2 py-1.5 border ${
                micTest.ok
                  ? micTest.peak > 3
                    ? 'bg-emerald-950/40 border-emerald-500/50 text-emerald-300'
                    : 'bg-amber-950/40 border-amber-500/50 text-amber-300'
                  : 'bg-red-950/40 border-red-500/50 text-red-300'
              }`}>
                {micTest.ok
                  ? (micTest.peak > 3
                    ? `Microfone OK (${micTest.device}) — pico capturado de ${micTest.peak}%. O J.A.R.V.I.S. está ouvindo você.`
                    : `Microfone acessível (${micTest.device}), mas o nível capturado está muito baixo. Aproxime-se ou aumente o ganho no Windows.`)
                  : micTest.message}
              </div>
            )}

            <div>
              <label className="text-[11px] text-cyan-400/80 block mb-1 flex items-center gap-1">
                <Mic className="w-3 h-3" /> Microfone de entrada
              </label>
              <select
                value={prefs.micDeviceId || ''}
                onChange={(e) => {
                  setPrefs(p => ({ ...p, micDeviceId: e.target.value }));
                  sounds.playClick();
                }}
                className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-2 text-xs text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
              >
                <option value="">Padrão do Windows (automático)</option>
                {inputDevices.map(d => (
                  <option key={d.deviceId} value={d.deviceId}>{d.label}</option>
                ))}
              </select>
              <p className="text-[10px] text-cyan-500/70 mt-1 font-syn-body">
                Se o J.A.R.V.I.S. não escuta, escolha aqui o microfone correto e use "Testar Microfone" falando.
              </p>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="text-[11px] text-cyan-400/80 block mb-1 flex items-center gap-1">
                  <Gauge className="w-3 h-3" /> Modelo de reconhecimento local
                </label>
                <select
                  value={prefs.sttModel || 'base'}
                  onChange={(e) => setPrefs(p => ({ ...p, sttModel: e.target.value }))}
                  className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-2 text-xs font-syn-body text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                >
                  <option value="base">Equilibrado (recomendado • ~76 MB)</option>
                  <option value="small">Mais preciso (~147 MB, um pouco mais lento)</option>
                  <option value="tiny">Mais rápido (~40 MB, menor precisão)</option>
                </select>
              </div>
              <div>
                <label className="text-[11px] text-cyan-400/80 block mb-1 flex items-center gap-1">
                  <Ear className="w-3 h-3" /> Escuta automática
                </label>
                <div className="flex gap-2">
                  {[true, false].map(value => (
                    <button
                      key={String(value)}
                      type="button"
                      onClick={() => { sounds.playClick(); setPrefs(p => ({ ...p, autoListen: value })); }}
                      className={`flex-1 px-3 py-2 rounded border text-xs font-bold transition-all ${
                        (prefs.autoListen !== false) === value
                          ? 'bg-[var(--primary)] text-black border-cyan-300'
                          : 'bg-black/60 text-slate-400 border-slate-600 hover:border-cyan-500'
                      }`}
                    >
                      {value ? 'Ligada (recomendada)' : 'Desligada'}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <p className="text-[10px] text-cyan-500/70 font-syn-body flex items-start gap-1">
              <Bot className="w-3 h-3 mt-0.5 shrink-0" />
              O reconhecimento roda offline no seu computador (Whisper). Fale depois do sinal e pause ao final da frase —
              o J.A.R.V.I.S. detecta automaticamente quando você termina de falar.
            </p>
          </div>

          {/* Save Action Footer */}
          <div className="flex items-center justify-end space-x-3 pt-2">
            {savedMessage && (
              <span className="text-xs text-emerald-400 font-bold flex items-center gap-1 animate-pulse">
                <Check className="w-4 h-4" /> Configurações salvas na memória neural!
              </span>
            )}
            <button
              type="submit"
              className="px-6 py-2 rounded bg-[var(--primary)] hover:bg-white text-black font-syn-title font-bold text-xs uppercase transition-all shadow-[0_0_15px_var(--primary-glow)]"
            >
              Salvar Diretivas
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}