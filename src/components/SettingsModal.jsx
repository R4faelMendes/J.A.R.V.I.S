import React, { useState } from 'react';
import { X, Settings, Key, User, Palette, Volume2, Cpu, Check, Shield } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { memoryService } from '../services/memoryService';
import { speechService } from '../services/speechService';

export default function SettingsModal({ isOpen, onClose, activeTheme, onThemeChange }) {
  const [prefs, setPrefs] = useState(() => memoryService.getPreferences());
  const [savedMessage, setSavedMessage] = useState(false);

  if (!isOpen) return null;

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
    memoryService.updatePreferences(prefs);
    setSavedMessage(true);
    setTimeout(() => setSavedMessage(false), 2000);
  };

  const handleTestVoice = () => {
    sounds.playClick();
    speechService.speak('Olá, Senhor Stark. Todos os módulos vocais e diretivas neurais estão calibrados com perfeição.');
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 bg-black/80 backdrop-blur-md animate-in fade-in duration-200">
      <div className="hud-panel rounded-xl w-full max-w-2xl max-h-[90vh] flex flex-col border-[var(--primary)] shadow-[0_0_40px_var(--primary-glow)] overflow-hidden">
        <div className="corner-bracket corner-tl"></div>
        <div className="corner-bracket corner-tr"></div>
        <div className="corner-bracket corner-bl"></div>
        <div className="corner-bracket corner-br"></div>

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-cyan-500/20 bg-slate-950/60">
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded bg-cyan-950/60 border border-[var(--primary)] text-[var(--primary)]">
              <Settings className="w-5 h-5" />
            </div>
            <div>
              <h2 className="font-['Orbitron'] text-lg font-bold text-[var(--primary)] glow-cyan tracking-wider">
                CENTRAL DE CONFIGURAÇÃO DO SISTEMA
              </h2>
              <p className="text-xs text-cyan-400/80 font-['JetBrains_Mono']">
                PARÂMETROS DE IA • PREFERÊNCIAS VISUAIS & DIRETIVAS DE ACESSO
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
        <form onSubmit={handleSave} className="flex-1 overflow-y-auto p-6 space-y-6 font-['JetBrains_Mono']">
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

            <div className="grid grid-cols-3 gap-2 text-xs">
              {[
                { id: 'gemini', label: 'Google Gemini' },
                { id: 'openai', label: 'OpenAI (GPT-4o)' },
                { id: 'local', label: 'Cérebro Local Tático' }
              ].map(provider => (
                <button
                  key={provider.id}
                  type="button"
                  onClick={() => { sounds.playClick(); setPrefs({ ...prefs, aiProvider: provider.id }); }}
                  className={`p-2 rounded border transition-all text-center ${
                    prefs.aiProvider === provider.id
                      ? 'bg-[var(--primary)] text-black font-bold border-cyan-300'
                      : 'bg-black/50 border-cyan-500/20 text-cyan-300 hover:border-cyan-400'
                  }`}
                >
                  {provider.label}
                </button>
              ))}
            </div>

            {prefs.aiProvider === 'gemini' && (
              <div>
                <label className="text-[11px] text-cyan-400/80 block mb-1">Chave API do Google Gemini (Opcional)</label>
                <input
                  type="password"
                  value={prefs.geminiApiKey || ''}
                  onChange={(e) => setPrefs({ ...prefs, geminiApiKey: e.target.value })}
                  placeholder="AIzaSy..."
                  className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                />
              </div>
            )}

            {prefs.aiProvider === 'openai' && (
              <div>
                <label className="text-[11px] text-cyan-400/80 block mb-1">Chave API da OpenAI (Opcional)</label>
                <input
                  type="password"
                  value={prefs.openAiApiKey || ''}
                  onChange={(e) => setPrefs({ ...prefs, openAiApiKey: e.target.value })}
                  placeholder="sk-..."
                  className="w-full bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                />
              </div>
            )}
          </div>

          {/* Section 4: Voice Synthesizer Calibration */}
          <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 flex items-center justify-between">
            <div>
              <div className="text-xs uppercase text-cyan-300 font-bold flex items-center gap-2">
                <Volume2 className="w-4 h-4 text-[var(--primary)]" />
                Calibração Vocal do J.A.R.V.I.S.
              </div>
              <p className="text-[11px] text-cyan-400/70 mt-0.5">
                Síntese com tom elegante e respostas automatizadas em áudio.
              </p>
            </div>
            <button
              type="button"
              onClick={handleTestVoice}
              className="px-3 py-1.5 rounded bg-cyan-950/80 border border-cyan-400 text-cyan-300 hover:bg-[var(--primary)] hover:text-black transition-all text-xs font-bold"
            >
              Testar Voz
            </button>
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
              className="px-6 py-2 rounded bg-[var(--primary)] hover:bg-white text-black font-['Orbitron'] font-bold text-xs uppercase transition-all shadow-[0_0_15px_var(--primary-glow)]"
            >
              Salvar Diretivas
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}