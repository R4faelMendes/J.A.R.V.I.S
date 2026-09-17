import React, { useState } from 'react';
import { Calendar as CalendarIcon, Clock, ChevronLeft, ChevronRight, Plus, CheckCircle2, Trash2, X } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { memoryService } from '../services/memoryService';

export default function AgendaPanel() {
  const [tasks, setTasks] = useState(() => memoryService.getTasks());
  const [selectedDayOffset, setSelectedDayOffset] = useState(0);
  const [isAddingTask, setIsAddingTask] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [newTime, setNewTime] = useState('14:30');
  const [newDate, setNewDate] = useState(() => new Date().toISOString().split('T')[0]);
  const [newDesc, setNewDesc] = useState('');

  // Calcular data ativa baseada no offset de dias (permite navegar para o futuro!)
  const activeDate = new Date();
  activeDate.setDate(activeDate.getDate() + selectedDayOffset);
  const activeDateISO = activeDate.toISOString().split('T')[0];
  const currentDateStr = activeDate.toLocaleDateString('pt-BR', { weekday: 'short', day: 'numeric', month: 'short' });

  const cardStyles = [
    'bg-sky-400/20 border-sky-400 text-sky-200 hover:bg-sky-400/30',
    'bg-amber-400/20 border-amber-400 text-amber-200 hover:bg-amber-400/30',
    'bg-rose-400/20 border-rose-400 text-rose-200 hover:bg-rose-400/30',
    'bg-teal-400/20 border-teal-400 text-teal-200 hover:bg-teal-400/30',
  ];

  const handleToggle = (id) => {
    sounds.playClick();
    const updated = tasks.map(t => t.id === id ? { ...t, completed: !t.completed } : t);
    setTasks(updated);
    memoryService.saveTasks(updated);
  };

  const handleDelete = (e, id) => {
    e.stopPropagation();
    sounds.playAlert();
    const updated = tasks.filter(t => t.id !== id);
    setTasks(updated);
    memoryService.saveTasks(updated);
  };

  const handleAddTask = (e) => {
    e.preventDefault();
    if (!newTitle.trim()) return;

    sounds.playSuccess();
    const newTask = {
      id: `task_${Date.now()}`,
      title: newTitle.trim(),
      date: newDate || activeDateISO,
      time: newTime,
      description: newDesc.trim() || 'Diretiva operacional J.A.R.V.I.S.',
      completed: false,
      priority: 'ALTA'
    };

    const updated = [newTask, ...tasks];
    setTasks(updated);
    memoryService.saveTasks(updated);
    setNewTitle('');
    setNewDesc('');
    setIsAddingTask(false);
  };

  // Filtrar tarefas da data selecionada OU tarefas gerais
  const displayedTasks = tasks.filter(t => !t.date || t.date === activeDateISO);

  return (
    <section className="hud-panel rounded-lg p-3 border-[var(--panel-border)] flex flex-col flex-1 min-h-[320px]">
      <div className="corner-bracket corner-tl"></div>
      <div className="corner-bracket corner-tr"></div>
      <div className="corner-bracket corner-bl"></div>
      <div className="corner-bracket corner-br"></div>

      {/* Top Header Bar */}
      <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2 mb-2 font-['JetBrains_Mono']">
        <div className="flex items-center space-x-2">
          <CalendarIcon className="w-4 h-4 text-[var(--primary)]" />
          <span className="text-xs uppercase font-['Orbitron'] font-bold text-[var(--primary)] glow-cyan">
            Agenda Tática
          </span>
        </div>

        {/* Date Selector & Add button */}
        <div className="flex items-center space-x-1.5">
          <button
            onClick={() => { sounds.playClick(); setIsAddingTask(prev => !prev); }}
            title="Adicionar Lembrete / Tarefa"
            className="px-2 py-0.5 rounded bg-[var(--primary)] text-black font-bold text-[10px] flex items-center gap-1 hover:bg-white transition-all shadow-[0_0_8px_var(--primary)]"
          >
            <Plus className="w-3 h-3" /> Lembrete
          </button>

          <div className="flex items-center space-x-1 text-[11px] bg-cyan-950/40 px-2 py-0.5 rounded border border-cyan-500/30 text-cyan-200">
            <button onClick={() => { sounds.playClick(); setSelectedDayOffset(o => o - 1); }} className="hover:text-white">
              <ChevronLeft className="w-3.5 h-3.5" />
            </button>
            <span className="font-semibold uppercase tracking-tight">{currentDateStr}</span>
            <button onClick={() => { sounds.playClick(); setSelectedDayOffset(o => o + 1); }} className="hover:text-white">
              <ChevronRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>

      {/* New Task Inline Creator Form */}
      {isAddingTask && (
        <form onSubmit={handleAddTask} className="mb-2 p-2 rounded bg-slate-950/90 border border-[var(--primary)] space-y-2 font-['JetBrains_Mono'] text-xs animate-in slide-in-from-top-2">
          <div className="flex items-center justify-between">
            <span className="text-[10px] text-[var(--primary)] font-bold uppercase">NOVO LEMBRETE OPERACIONAL</span>
            <button type="button" onClick={() => setIsAddingTask(false)} className="text-red-400 hover:text-white">
              <X className="w-3.5 h-3.5" />
            </button>
          </div>

          <div className="grid grid-cols-3 gap-2">
            <input
              type="text"
              placeholder="O que você precisa fazer?"
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              autoFocus
              className="col-span-3 bg-black/60 border border-cyan-500/40 rounded px-2 py-1 text-xs text-white focus:outline-none focus:border-[var(--primary)]"
            />
            <input
              type="date"
              value={newDate}
              onChange={(e) => setNewDate(e.target.value)}
              className="col-span-2 bg-black/60 border border-cyan-500/40 rounded px-1.5 py-1 text-xs text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
            />
            <input
              type="time"
              value={newTime}
              onChange={(e) => setNewTime(e.target.value)}
              className="col-span-1 bg-black/60 border border-cyan-500/40 rounded px-1.5 py-1 text-xs text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
            />
          </div>

          <div className="flex justify-end gap-1.5">
            <button
              type="submit"
              className="px-3 py-1 rounded bg-[var(--primary)] text-black font-bold text-[10px] hover:bg-white transition-all shadow-[0_0_8px_var(--primary)]"
            >
              Salvar Lembrete
            </button>
          </div>
        </form>
      )}

      {/* Days of week bar */}
      <div className="grid grid-cols-7 gap-1 text-center font-['JetBrains_Mono'] text-[10px] pb-2 border-b border-cyan-500/10">
        {days.map((d, i) => {
          return (
            <div
              key={d}
              className={`py-0.5 rounded cursor-pointer transition-all ${
                i === (activeDate.getDay() === 0 ? 6 : activeDate.getDay() - 1)
                  ? 'bg-[var(--primary)] text-black font-bold shadow-[0_0_8px_var(--primary)]'
                  : 'bg-black/30 text-cyan-400 hover:text-white'
              }`}
              onClick={() => {
                sounds.playClick();
                // Ajustar offset para o dia clicado da semana
                const currentDay = today.getDay() === 0 ? 6 : today.getDay() - 1;
                setSelectedDayOffset(i - currentDay);
              }}
            >
              {d}
            </div>
          );
        })}
      </div>

      {/* Timeline with tasks styled like reference image */}
      <div className="flex-1 overflow-y-auto space-y-1.5 mt-2 pr-1 scrollbar-none font-['JetBrains_Mono']">
        {displayedTasks.length === 0 ? (
          <div className="text-center py-6 text-xs text-cyan-500/60 font-['JetBrains_Mono']">
            Nenhum lembrete para esta data. Clique em "+ Lembrete" acima.
          </div>
        ) : displayedTasks.map((task, idx) => {
          const style = cardStyles[idx % cardStyles.length];
          return (
            <div
              key={task.id}
              onClick={() => handleToggle(task.id)}
              className={`p-2 rounded-md border text-xs cursor-pointer transition-all flex items-start justify-between gap-2 shadow-sm ${style} ${
                task.completed ? 'opacity-40 line-through' : ''
              }`}
            >
              <div className="flex-1 overflow-hidden">
                <div className="flex items-center space-x-1.5 text-[10px] font-bold opacity-80 mb-0.5">
                  <Clock className="w-2.5 h-2.5" />
                  <span>{task.time || '10:00'}</span>
                </div>
                <div className="font-bold truncate text-[11px] text-white">
                  {task.title}
                </div>
                <div className="text-[10px] opacity-75 truncate">
                  {task.description || 'Diretiva operacional Stark'}
                </div>
              </div>

              <div className="flex items-center space-x-1.5 flex-shrink-0">
                <CheckCircle2 className={`w-3.5 h-3.5 ${task.completed ? 'text-emerald-400' : 'opacity-40'}`} />
                <button
                  type="button"
                  onClick={(e) => handleDelete(e, task.id)}
                  title="Excluir Lembrete"
                  className="p-1 text-red-400/60 hover:text-red-400 hover:bg-red-950/40 rounded transition-colors"
                >
                  <Trash2 className="w-3 h-3" />
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {/* Footer Quick Summary */}
      <div className="mt-2 pt-1.5 border-t border-cyan-500/20 flex items-center justify-between text-[10px] font-['JetBrains_Mono'] text-cyan-400/80">
        <span>{tasks.filter(t => !t.completed).length} PENDENTES</span>
        <span className="text-emerald-400 font-bold">{tasks.filter(t => t.completed).length} CONCLUÍDAS</span>
      </div>
    </section>
  );
}