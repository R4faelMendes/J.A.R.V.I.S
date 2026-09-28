import React, { useState } from 'react';
import { X, Dna, Plus, Trash2, Power } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { ruleEngine } from '../services/ruleEngine';
import { SynAccent } from './synapse';

export default function RulesModal({ isOpen, onClose, embedded = false }) {
  const [rules, setRules] = useState(() => ruleEngine.getRules());
  const [name, setName] = useState('');
  const [triggerKind, setTriggerKind] = useState('time');
  const [triggerAt, setTriggerAt] = useState('08:00');
  const [triggerMatch, setTriggerMatch] = useState('Code.exe');
  const [actionText, setActionText] = useState('');
  const [actionCommand, setActionCommand] = useState('');

  if (!isOpen && !embedded) return null;

  const refresh = () => setRules([...ruleEngine.getRules()]);

  const handleAdd = (e) => {
    e.preventDefault();
    if (!name.trim()) return;
    sounds.playSuccess();
    ruleEngine.addRule({
      name: name.trim(),
      trigger: triggerKind === 'time'
        ? { kind: 'time', at: triggerAt }
        : { kind: 'process', match: triggerMatch.trim() || 'Code.exe', mode: 'running' },
      action: actionCommand.trim()
        ? { kind: 'command', command: actionCommand.trim() }
        : { kind: 'speak', text: actionText.trim() || `Reflexo disparado: ${name.trim()}, Senhor.` },
    });
    setName(''); setActionText(''); setActionCommand('');
    refresh();
  };

  return (
    <div className={embedded ? 'flex-1 min-h-0 flex flex-col' : 'fixed inset-0 z-50 flex items-center justify-center p-3 bg-black/80 backdrop-blur-md'}>
      <div className={embedded ? 'syn-panel w-full flex-1 min-h-0 flex flex-col overflow-hidden' : 'syn-panel w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden'}>
        <SynAccent />
        <div className="flex items-center justify-between px-6 py-4" style={{ borderBottom: '1px solid rgba(45,253,212,0.15)' }}>
          <div className="flex items-center gap-3">
            <div className="syn-pill p-2" style={{ border: '1px solid rgba(45,253,212,0.4)', color: '#2dfdd4' }}>
              <Dna className="w-5 h-5" />
            </div>
            <div>
              <h2 className="font-syn-title text-lg font-bold" style={{ color: '#eaf8f4' }}>Reflexos</h2>
              <p className="font-syn-body text-xs font-light" style={{ color: '#9aa6c9' }}>o organismo age sozinho · hora do dia e processos observados</p>
            </div>
          </div>
          <button onClick={() => { sounds.playClick(); onClose(); }} className="syn-pill p-2" style={{ border: '1px solid rgba(255,92,122,0.4)', color: '#ff5c7a' }}>
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-6 space-y-5 font-syn-body">
          <form onSubmit={handleAdd} className="p-4 space-y-3" style={{ borderRadius: '20px 24px 18px 26px', background: 'rgba(138,92,255,0.06)', border: '1px solid rgba(138,92,255,0.25)' }}>
            <div className="font-syn-title text-xs font-medium flex items-center gap-2" style={{ color: '#2dfdd4' }}>
              <Plus className="w-4 h-4" /> Novo reflexo
            </div>
            <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nome do reflexo (ex: pausa para café)"
              className="syn-input w-full px-3 py-2 text-xs font-light" style={{ color: '#eaf8f4' }} />
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              <div className="flex gap-1.5">
                {['time', 'process'].map((k) => (
                  <button key={k} type="button" onClick={() => { sounds.playClick(); setTriggerKind(k); }}
                    className="syn-pill flex-1 px-3 py-1.5 text-[11px] font-medium"
                    style={triggerKind === k ? { background: 'rgba(45,253,212,0.18)', color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.45)' } : { color: '#9aa6c9', border: '1px solid rgba(154,166,201,0.25)' }}>
                    {k === 'time' ? 'Horário' : 'Processo'}
                  </button>
                ))}
              </div>
              {triggerKind === 'time' ? (
                <input type="time" value={triggerAt} onChange={(e) => setTriggerAt(e.target.value)} className="syn-input px-3 py-1.5 text-xs" style={{ color: '#eaf8f4' }} />
              ) : (
                <input value={triggerMatch} onChange={(e) => setTriggerMatch(e.target.value)} placeholder="Processo (ex: Code.exe)"
                  className="syn-input px-3 py-1.5 text-xs font-light" style={{ color: '#eaf8f4' }} />
              )}
            </div>
            <input value={actionText} onChange={(e) => setActionText(e.target.value)} placeholder="Fala ao disparar (opcional)"
              className="syn-input w-full px-3 py-2 text-xs font-light" style={{ color: '#eaf8f4' }} />
            <input value={actionCommand} onChange={(e) => setActionCommand(e.target.value)} placeholder="Ou comando a executar (ex: abrir agenda)"
              className="syn-input w-full px-3 py-2 text-xs font-light" style={{ color: '#eaf8f4' }} />
            <div className="flex justify-end">
              <button type="submit" className="syn-pill font-syn-title px-5 py-1.5 text-xs font-medium"
                style={{ background: 'rgba(45,253,212,0.18)', color: '#2dfdd4', border: '1px solid rgba(45,253,212,0.45)' }}>
                Criar reflexo
              </button>
            </div>
          </form>

          <div className="space-y-2">
            <div className="font-syn-body text-[11px] font-light" style={{ color: '#9aa6c9' }}>{rules.length} reflexo(s) gravado(s)</div>
            {rules.length === 0 && (
              <div className="text-xs font-light text-center py-6" style={{ color: '#9aa6c9' }}>Nenhum reflexo. O organismo só reage a comandos diretos por enquanto.</div>
            )}
            {rules.map((r) => (
              <div key={r.id} className="p-3 flex items-center justify-between gap-3"
                style={{ borderRadius: '18px 16px 20px 14px', background: 'rgba(10,6,32,0.5)', border: `1px solid ${r.enabled ? 'rgba(45,253,212,0.3)' : 'rgba(154,166,201,0.2)'}`, opacity: r.enabled ? 1 : 0.6 }}>
                <div className="min-w-0">
                  <div className="font-syn-title text-xs font-medium truncate" style={{ color: '#eaf8f4' }}>{r.name}</div>
                  <div className="font-syn-body text-[11px] font-light truncate" style={{ color: '#9aa6c9' }}>
                    {r.trigger?.kind === 'time' ? `quando for ${r.trigger.at}` : `quando ${r.trigger?.match} estiver ativo`} · {r.action?.kind === 'command' ? `executa “${r.action.command}”` : 'fala com você'}
                  </div>
                </div>
                <div className="flex items-center gap-1.5 shrink-0">
                  <button onClick={() => { sounds.playClick(); ruleEngine.toggleRule(r.id); refresh(); }} title={r.enabled ? 'Adormecer' : 'Despertar'}
                    className="syn-pill p-1.5" style={{ color: r.enabled ? '#2dfdd4' : '#9aa6c9', border: '1px solid rgba(45,253,212,0.3)' }}>
                    <Power className="w-3.5 h-3.5" />
                  </button>
                  <button onClick={() => { sounds.playAlert(); ruleEngine.removeRule(r.id); refresh(); }} title="Remover"
                    className="syn-pill p-1.5" style={{ color: '#ff5c7a', border: '1px solid rgba(255,92,122,0.3)' }}>
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
