import React, { useState } from 'react';
import { X, Database, Search, Code, FileText, AlertTriangle, Terminal, Plus, Sparkles, GitBranch } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { memoryService } from '../services/memoryService';

export default function MemoryModal({ isOpen, onClose, embedded = false }) {
  const [projects, setProjects] = useState(() => memoryService.getProjects());
  const [search, setSearch] = useState('');
  const [activeTab, setActiveTab] = useState('projects'); // 'projects', 'commands'
  const [commandHistory] = useState(() => memoryService.getCommandHistory());
  const [selectedProject, setSelectedProject] = useState(projects[0] || null);

  if (!isOpen && !embedded) return null;

  const filteredProjects = projects.filter(p => 
    p.name.toLowerCase().includes(search.toLowerCase()) ||
    p.description.toLowerCase().includes(search.toLowerCase()) ||
    p.techStack.some(t => t.toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <div className={embedded ? "flex-1 min-h-0 flex flex-col" : "fixed inset-0 z-50 flex items-center justify-center p-3 bg-black/80 backdrop-blur-md animate-in fade-in duration-200"}>
      <div className={embedded ? "hud-panel rounded-xl w-full flex-1 min-h-0 flex flex-col border-[var(--primary)] overflow-hidden" : "hud-panel rounded-xl w-full max-w-5xl max-h-[90vh] flex flex-col border-[var(--primary)] shadow-[0_0_40px_var(--primary-glow)] overflow-hidden"}>
        <div className="corner-bracket corner-tl"></div>
        <div className="corner-bracket corner-tr"></div>
        <div className="corner-bracket corner-bl"></div>
        <div className="corner-bracket corner-br"></div>

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-cyan-500/20 bg-slate-950/60">
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded bg-cyan-950/60 border border-[var(--primary)] text-[var(--primary)]">
              <Database className="w-5 h-5" />
            </div>
            <div>
              <h2 className="font-['Orbitron'] text-lg font-bold text-[var(--primary)] glow-cyan tracking-wider">
                MATRIZ DE MEMÓRIA DE LONGO PRAZO
              </h2>
              <p className="text-xs text-cyan-400/80 font-['JetBrains_Mono']">
                BANCO DE DADOS PERSISTENTE // PROJETOS, HISTÓRICO & ERROS REGISTRADOS
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-3">
            <div className="flex bg-cyan-950/50 border border-cyan-500/30 rounded p-0.5 text-xs font-['JetBrains_Mono']">
              <button
                onClick={() => { sounds.playClick(); setActiveTab('projects'); }}
                className={`px-3 py-1 rounded transition-all ${
                  activeTab === 'projects' ? 'bg-[var(--primary)] text-black font-bold' : 'text-cyan-300 hover:text-white'
                }`}
              >
                Projetos Memorizados
              </button>
              <button
                onClick={() => { sounds.playClick(); setActiveTab('commands'); }}
                className={`px-3 py-1 rounded transition-all ${
                  activeTab === 'commands' ? 'bg-[var(--primary)] text-black font-bold' : 'text-cyan-300 hover:text-white'
                }`}
              >
                Histórico de Comandos
              </button>
            </div>

            <button
              onClick={() => { sounds.playClick(); onClose(); }}
              className="p-1.5 rounded-lg border border-red-500/30 text-red-400 hover:bg-red-500 hover:text-black transition-all"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-hidden flex flex-col md:flex-row">
          {activeTab === 'projects' ? (
            <>
              {/* Left Projects List */}
              <div className="w-full md:w-1/3 border-r border-cyan-500/20 p-4 flex flex-col space-y-3 bg-black/20">
                {/* Search Bar */}
                <div className="relative">
                  <Search className="w-4 h-4 absolute left-3 top-2.5 text-cyan-500" />
                  <input
                    type="text"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder="Filtrar por nome ou stack..."
                    className="w-full bg-slate-950/80 border border-cyan-500/40 rounded pl-9 pr-3 py-1.5 text-xs font-['JetBrains_Mono'] text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                  />
                </div>

                <div className="flex-1 overflow-y-auto space-y-2">
                  {filteredProjects.map(proj => (
                    <div
                      key={proj.id}
                      onClick={() => { sounds.playClick(); setSelectedProject(proj); }}
                      className={`p-3 rounded-lg border transition-all cursor-pointer ${
                        selectedProject?.id === proj.id
                          ? 'bg-[var(--primary-dim)] border-[var(--primary)] shadow-[0_0_12px_var(--primary-glow)]'
                          : 'bg-cyan-950/20 border-cyan-500/20 hover:border-cyan-400'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <span className="font-['Orbitron'] text-xs font-bold text-cyan-200 glow-cyan truncate">
                          {proj.name}
                        </span>
                        <span className="text-[10px] text-emerald-400 font-mono font-semibold">
                          {proj.status}
                        </span>
                      </div>
                      <p className="text-[11px] text-cyan-400/70 font-['JetBrains_Mono'] truncate mt-1">
                        {proj.description}
                      </p>
                      <div className="flex items-center gap-1 mt-2 flex-wrap">
                        {proj.techStack.slice(0, 3).map((t, idx) => (
                          <span key={idx} className="text-[9px] font-mono px-1.5 py-0.2 rounded bg-cyan-900/40 border border-cyan-500/30 text-cyan-300">
                            {t}
                          </span>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Right Project Details */}
              <div className="flex-1 p-6 overflow-y-auto space-y-5 bg-slate-950/40 font-['JetBrains_Mono']">
                {selectedProject ? (
                  <>
                    <div className="border-b border-cyan-500/20 pb-3 flex items-start justify-between">
                      <div>
                        <h3 className="font-['Orbitron'] text-xl font-bold text-[var(--primary)] glow-cyan flex items-center gap-2">
                          <Code className="w-5 h-5 text-[var(--primary)]" />
                          {selectedProject.name}
                        </h3>
                        <p className="text-xs text-cyan-300/80 mt-1">
                          {selectedProject.description}
                        </p>
                      </div>
                      <div className="text-right text-[11px] text-cyan-400/70">
                        <div className="flex items-center justify-end gap-1 text-[var(--secondary)]">
                          <GitBranch className="w-3.5 h-3.5" />
                          <span>{selectedProject.gitBranch || 'main'}</span>
                        </div>
                        <div>Última alteração: {selectedProject.lastModified}</div>
                      </div>
                    </div>

                    {/* Tech Stack */}
                    <div>
                      <h4 className="text-xs uppercase text-cyan-400 font-bold mb-2 flex items-center gap-1.5">
                        <Sparkles className="w-3.5 h-3.5 text-cyan-400" />
                        Tecnologias & Arquitetura
                      </h4>
                      <div className="flex flex-wrap gap-2">
                        {selectedProject.techStack.map((tech, i) => (
                          <span key={i} className="px-2.5 py-1 rounded bg-cyan-950/60 border border-cyan-500/40 text-cyan-200 text-xs shadow-sm">
                            {tech}
                          </span>
                        ))}
                      </div>
                    </div>

                    {/* Important Files */}
                    <div>
                      <h4 className="text-xs uppercase text-cyan-400 font-bold mb-2 flex items-center gap-1.5">
                        <FileText className="w-3.5 h-3.5 text-cyan-400" />
                        Arquivos Críticos
                      </h4>
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                        {selectedProject.importantFiles.map((f, i) => (
                          <div key={i} className="p-2 rounded bg-cyan-950/30 border border-cyan-500/20 text-xs text-cyan-300 flex items-center gap-2">
                            <span className="text-[var(--primary)]">#</span>
                            <span className="truncate">{f}</span>
                          </div>
                        ))}
                      </div>
                    </div>

                    {/* Last Recorded Errors */}
                    <div>
                      <h4 className="text-xs uppercase text-amber-400 font-bold mb-2 flex items-center gap-1.5">
                        <AlertTriangle className="w-3.5 h-3.5 text-amber-400" />
                        Últimos Erros & Diagnósticos Registrados
                      </h4>
                      {selectedProject.lastErrors && selectedProject.lastErrors.length > 0 ? (
                        <div className="space-y-1.5">
                          {selectedProject.lastErrors.map((err, i) => (
                            <div key={i} className="p-2.5 rounded bg-amber-950/20 border border-amber-500/30 text-xs text-amber-200">
                              ⚠️ {err}
                            </div>
                          ))}
                        </div>
                      ) : (
                        <div className="text-xs text-emerald-400 p-2 rounded bg-emerald-950/20 border border-emerald-500/20">
                          Nenhum erro pendente registrado. Sistema estável.
                        </div>
                      )}
                    </div>

                    {/* Commands Used */}
                    <div>
                      <h4 className="text-xs uppercase text-cyan-400 font-bold mb-2 flex items-center gap-1.5">
                        <Terminal className="w-3.5 h-3.5 text-cyan-400" />
                        Comandos Operacionais Frequentes
                      </h4>
                      <div className="flex flex-wrap gap-2">
                        {selectedProject.commandsUsed?.map((cmd, i) => (
                          <code key={i} className="px-2.5 py-1 rounded bg-black border border-cyan-500/30 text-xs text-cyan-300 font-mono">
                            {cmd}
                          </code>
                        ))}
                      </div>
                    </div>
                  </>
                ) : (
                  <div className="text-center text-cyan-500 py-12">
                    Selecione um projeto para inspecionar os registros de memória neural.
                  </div>
                )}
              </div>
            </>
          ) : (
            /* Command History List */
            <div className="flex-1 p-6 overflow-y-auto space-y-2 font-['JetBrains_Mono']">
              <div className="text-xs uppercase text-cyan-400 font-bold border-b border-cyan-500/20 pb-2 mb-3">
                REGISTRO DE INSTRUÇÕES E DIRETIVAS RECENTES ({commandHistory.length})
              </div>
              {commandHistory.length === 0 ? (
                <div className="text-cyan-500 text-xs py-8 text-center">Nenhum comando registrado ainda nesta sessão.</div>
              ) : (
                commandHistory.map((h, i) => (
                  <div key={i} className="p-2.5 rounded bg-cyan-950/30 border border-cyan-500/20 text-xs flex items-center justify-between">
                    <span className="text-cyan-200 flex items-center gap-2">
                      <span className="text-[var(--primary)] font-bold">&gt;</span>
                      {h.command}
                    </span>
                    <span className="text-[10px] text-cyan-500">{h.timestamp}</span>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}