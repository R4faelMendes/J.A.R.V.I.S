import React from 'react';
import { X, PlayCircle, CheckCircle, Loader2, Sparkles, Terminal, ArrowRight } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { AutomationStatus } from '../services/commandExecutor';

export default function AutomationModal({ isOpen, onClose, automationState }) {
  if (!isOpen || !automationState) return null;

  const { status, currentStep, totalSteps, stepData } = automationState;
  const progressPercent = Math.min(100, Math.round((currentStep / totalSteps) * 100));

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 bg-black/85 backdrop-blur-lg animate-in fade-in duration-200">
      <div className="hud-panel rounded-xl w-full max-w-2xl flex flex-col border-[var(--primary)] shadow-[0_0_50px_var(--primary-glow)] overflow-hidden">
        <div className="corner-bracket corner-tl"></div>
        <div className="corner-bracket corner-tr"></div>
        <div className="corner-bracket corner-bl"></div>
        <div className="corner-bracket corner-br"></div>

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-cyan-500/20 bg-slate-950/70">
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded bg-cyan-950/60 border border-[var(--primary)] text-[var(--primary)]">
              {status === AutomationStatus.RUNNING ? (
                <Loader2 className="w-5 h-5 animate-spin text-[var(--primary)]" />
              ) : (
                <CheckCircle className="w-5 h-5 text-emerald-400" />
              )}
            </div>
            <div>
              <h2 className="font-['Orbitron'] text-base sm:text-lg font-bold text-[var(--primary)] glow-cyan tracking-wider">
                PIPELINE DE AUTOMAÇÃO DE SISTEMA
              </h2>
              <p className="text-xs text-cyan-400/80 font-['JetBrains_Mono']">
                EXECUÇÃO EMULADA // BARRAMENTO DE TECLADO, MOUSE & NAVEGADOR
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
        <div className="p-6 space-y-6 font-['JetBrains_Mono']">
          {/* Progress Bar */}
          <div>
            <div className="flex justify-between text-xs mb-1.5">
              <span className="text-cyan-400 font-bold">
                ETAPA {currentStep} DE {totalSteps}
              </span>
              <span className="text-[var(--primary)] font-bold glow-cyan">
                {progressPercent}% CONCLUÍDO
              </span>
            </div>
            <div className="w-full bg-slate-900 h-2.5 rounded-full overflow-hidden border border-cyan-500/40 p-[1px]">
              <div
                className="h-full bg-gradient-to-r from-[var(--primary)] via-cyan-300 to-emerald-400 rounded-full transition-all duration-500 shadow-[0_0_10px_var(--primary)]"
                style={{ width: `${progressPercent}%` }}
              />
            </div>
          </div>

          {/* Current Step Card */}
          <div className="p-4 rounded-xl bg-cyan-950/30 border border-cyan-500/40 shadow-inner space-y-2">
            <div className="text-xs text-[var(--secondary)] font-bold uppercase flex items-center gap-1.5">
              <Sparkles className="w-4 h-4 text-[var(--secondary)]" />
              Ação em Execução
            </div>
            <div className="text-base font-bold text-cyan-100 glow-cyan">
              {stepData?.title || 'Processando instrução...'}
            </div>
            <div className="text-xs text-cyan-300/80 bg-black/60 p-2.5 rounded border border-cyan-500/20 font-mono">
              $ {stepData?.detail || 'Aguardando sincronização de nós...'}
            </div>
          </div>

          {/* Status Message */}
          <div className="flex items-center justify-between pt-2">
            <div className="text-xs text-cyan-400/80 flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
              <span>
                {status === AutomationStatus.RUNNING
                  ? 'Executando diretivas táticas de automação...'
                  : 'Automação finalizada e sincronizada na memória de longo prazo.'}
              </span>
            </div>

            {status === AutomationStatus.COMPLETED && (
              <button
                onClick={() => { sounds.playClick(); onClose(); }}
                className="px-5 py-1.5 rounded bg-emerald-500 hover:bg-emerald-400 text-black font-['Orbitron'] font-bold text-xs uppercase transition-all shadow-[0_0_12px_#10b981]"
              >
                Concluir
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}