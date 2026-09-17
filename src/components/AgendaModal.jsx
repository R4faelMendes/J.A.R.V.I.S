import React, { useState } from 'react';
import { X, Calendar as CalIcon, Plus, CheckCircle, Circle, Clock, AlertCircle, Trash2, Bell } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { memoryService } from '../services/memoryService';

export default function AgendaModal({ isOpen, onClose }) {
  const [tasks, setTasks] = useState(() => memoryService.getTasks());
  const [viewMode, setViewMode] = useState('tasks'); // 'tasks', 'calendar'
  const [newTitle, setNewTitle] = useState('');
  const [newDate, setNewDate] = useState(new Date().toISOString().split('T')[0]);
  const [newTime, setNewTime] = useState('15:00');
  const [newPriority, setNewPriority] = useState('ALTA');
  const [newCategory, setNewCategory] = useState('PROJETO');

  if (!isOpen) return null;

  const handleToggleTask = (id) => {
    sounds.playClick();
    const updated = tasks.map(t => t.id === id ? { ...t, completed: !t.completed } : t);
    setTasks(updated);
    memoryService.saveTasks(updated);
  };

  const handleDeleteTask = (id) => {
    sounds.playClick();
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
      date: newDate,
      time: newTime,
      priority: newPriority,
      completed: false,
      category: newCategory
    };

    const updated = [newTask, ...tasks];
    setTasks(updated);
    memoryService.saveTasks(updated);
    setNewTitle('');
  };

  // Days of current month simulation
  const now = new Date();
  const currentMonthName = now.toLocaleString('pt-BR', { month: 'long', year: 'numeric' });
  const weekDays = ['DOM', 'SEG', 'TER', 'QUA', 'QUI', 'SEX', 'SÁB'];
  const daysInMonth = Array.from({ length: 30 }, (_, i) => i + 1);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 bg-black/80 backdrop-blur-md animate-in fade-in duration-200">
      <div className="hud-panel rounded-xl w-full max-w-4xl max-h-[90vh] flex flex-col border-[var(--primary)] shadow-[0_0_40px_var(--primary-glow)] overflow-hidden">
        <div className="corner-bracket corner-tl"></div>
        <div className="corner-bracket corner-tr"></div>
        <div className="corner-bracket corner-bl"></div>
        <div className="corner-bracket corner-br"></div>

        {/* Modal Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-cyan-500/20 bg-slate-950/60">
          <div className="flex items-center space-x-3">
            <div className="p-2 rounded bg-cyan-950/60 border border-[var(--primary)] text-[var(--primary)]">
              <CalIcon className="w-5 h-5" />
            </div>
            <div>
              <h2 className="font-['Orbitron'] text-lg font-bold text-[var(--primary)] glow-cyan tracking-wider">
                AGENDA & CRONOGRAMA TÁTICO
              </h2>
              <p className="text-xs text-cyan-400/80 font-['JetBrains_Mono']">
                DIRETIVAS OPERACIONAIS • ALARMES E TAREFAS CRÍTICAS
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-3">
            {/* View Switcher */}
            <div className="flex bg-cyan-950/50 border border-cyan-500/30 rounded p-0.5 text-xs font-['JetBrains_Mono']">
              <button
                onClick={() => { sounds.playClick(); setViewMode('tasks'); }}
                className={`px-3 py-1 rounded transition-all ${
                  viewMode === 'tasks' ? 'bg-[var(--primary)] text-black font-bold' : 'text-cyan-300 hover:text-white'
                }`}
              >
                Tarefas & Alarmes
              </button>
              <button
                onClick={() => { sounds.playClick(); setViewMode('calendar'); }}
                className={`px-3 py-1 rounded transition-all ${
                  viewMode === 'calendar' ? 'bg-[var(--primary)] text-black font-bold' : 'text-cyan-300 hover:text-white'
                }`}
              >
                Calendário Mensal
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

        {/* Modal Body */}
        <div className="flex-1 overflow-y-auto p-6 space-y-6">
          {viewMode === 'tasks' ? (
            <>
              {/* Add New Task Card */}
              <form onSubmit={handleAddTask} className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 space-y-3">
                <div className="text-xs uppercase font-['Orbitron'] text-[var(--primary)] font-bold flex items-center gap-2">
                  <Plus className="w-4 h-4 text-[var(--primary)]" />
                  Nova Diretiva / Tarefa
                </div>
                <div className="grid grid-cols-1 md:grid-cols-12 gap-3">
                  <input
                    type="text"
                    value={newTitle}
                    onChange={(e) => setNewTitle(e.target.value)}
                    placeholder="Descrição da tarefa (ex: 'Compilar kernel Stark', 'Reunião Pepper')..."
                    className="md:col-span-5 bg-black/60 border border-cyan-500/40 rounded px-3 py-1.5 text-xs font-['JetBrains_Mono'] text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                  />
                  <input
                    type="date"
                    value={newDate}
                    onChange={(e) => setNewDate(e.target.value)}
                    className="md:col-span-2 bg-black/60 border border-cyan-500/40 rounded px-2 py-1.5 text-xs font-['JetBrains_Mono'] text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                  />
                  <input
                    type="time"
                    value={newTime}
                    onChange={(e) => setNewTime(e.target.value)}
                    className="md:col-span-2 bg-black/60 border border-cyan-500/40 rounded px-2 py-1.5 text-xs font-['JetBrains_Mono'] text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                  />
                  <select
                    value={newPriority}
                    onChange={(e) => setNewPriority(e.target.value)}
                    className="md:col-span-2 bg-black/60 border border-cyan-500/40 rounded px-2 py-1.5 text-xs font-['JetBrains_Mono'] text-cyan-200 focus:outline-none focus:border-[var(--primary)]"
                  >
                    <option value="CRITICA">CRÍTICA</option>
                    <option value="ALTA">ALTA</option>
                    <option value="MEDIA">MÉDIA</option>
                    <option value="BAIXA">BAIXA</option>
                  </select>
                  <button
                    type="submit"
                    className="md:col-span-1 bg-[var(--primary)] hover:bg-white text-black font-bold font-['JetBrains_Mono'] rounded py-1.5 text-xs uppercase transition-all shadow-[0_0_10px_var(--primary-glow)]"
                  >
                    Add
                  </button>
                </div>
              </form>

              {/* Tasks List */}
              <div className="space-y-2">
                <div className="flex items-center justify-between text-xs font-['JetBrains_Mono'] text-cyan-400/80 border-b border-cyan-500/20 pb-1">
                  <span>DIRETIVAS AGENDADAS ({tasks.length})</span>
                  <span>STATUS / PRIORIDADE</span>
                </div>

                {tasks.map(task => {
                  const priorityColors = {
                    CRITICA: 'bg-red-950/60 border-red-500 text-red-300 shadow-[0_0_8px_#ef4444]',
                    ALTA: 'bg-amber-950/60 border-amber-500 text-amber-300 shadow-[0_0_8px_#ffb020]',
                    MEDIA: 'bg-cyan-950/60 border-cyan-500 text-cyan-300',
                    BAIXA: 'bg-slate-900 border-slate-600 text-slate-300'
                  };

                  return (
                    <div
                      key={task.id}
                      className={`p-3 rounded-lg border transition-all flex items-center justify-between gap-3 ${
                        task.completed 
                          ? 'bg-emerald-950/10 border-emerald-500/30 opacity-60' 
                          : 'bg-cyan-950/30 border-cyan-500/30 hover:border-[var(--primary)]'
                      }`}
                    >
                      <div className="flex items-center space-x-3 flex-1">
                        <button
                          onClick={() => handleToggleTask(task.id)}
                          className="text-cyan-400 hover:text-[var(--primary)] transition-colors"
                        >
                          {task.completed ? (
                            <CheckCircle className="w-5 h-5 text-emerald-400" />
                          ) : (
                            <Circle className="w-5 h-5" />
                          )}
                        </button>
                        <div>
                          <div className={`font-['JetBrains_Mono'] text-sm font-semibold ${task.completed ? 'line-through text-cyan-500/60' : 'text-cyan-100'}`}>
                            {task.title}
                          </div>
                          <div className="flex items-center space-x-3 text-[11px] font-['JetBrains_Mono'] text-cyan-400/70 pt-0.5">
                            <span className="flex items-center gap-1">
                              <CalIcon className="w-3 h-3" /> {task.date}
                            </span>
                            <span className="flex items-center gap-1">
                              <Clock className="w-3 h-3" /> {task.time}
                            </span>
                            <span className="text-[10px] uppercase px-1 rounded bg-cyan-900/40 border border-cyan-500/20">
                              {task.category}
                            </span>
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center space-x-2">
                        <span className={`text-[10px] font-bold font-['JetBrains_Mono'] px-2 py-0.5 rounded border ${priorityColors[task.priority] || priorityColors.MEDIA}`}>
                          {task.priority}
                        </span>
                        <button
                          onClick={() => handleDeleteTask(task.id)}
                          className="p-1 rounded text-red-400/60 hover:text-red-400 hover:bg-red-950/40 transition-colors"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </>
          ) : (
            /* Monthly Calendar Grid View */
            <div className="space-y-4">
              <div className="flex items-center justify-between text-sm font-['Orbitron'] font-bold text-[var(--primary)] uppercase glow-cyan">
                <span>{currentMonthName}</span>
                <span className="text-xs text-cyan-400/70 font-['JetBrains_Mono']">FUSO HORÁRIO: LOCAL BRASIL (UTC-3)</span>
              </div>

              {/* Day Headers */}
              <div className="grid grid-cols-7 gap-1 text-center font-['JetBrains_Mono'] text-xs text-cyan-400/80 font-bold py-2 border-y border-cyan-500/20">
                {weekDays.map((d, i) => (
                  <div key={i}>{d}</div>
                ))}
              </div>

              {/* Day Cells */}
              <div className="grid grid-cols-7 gap-1.5 font-['JetBrains_Mono'] text-xs">
                {daysInMonth.map(day => {
                  const isToday = day === now.getDate();
                  const hasTasks = tasks.some(t => t.date.endsWith(`-${String(day).padStart(2, '0')}`));

                  return (
                    <div
                      key={day}
                      onClick={() => sounds.playClick()}
                      className={`p-2 min-h-[64px] rounded border transition-all cursor-pointer flex flex-col justify-between ${
                        isToday
                          ? 'bg-[var(--primary-dim)] border-[var(--primary)] shadow-[0_0_12px_var(--primary-glow)]'
                          : 'bg-cyan-950/20 border-cyan-500/20 hover:border-cyan-400'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <span className={`font-bold ${isToday ? 'text-[var(--primary)] font-extrabold text-sm glow-cyan' : 'text-cyan-300'}`}>
                          {day}
                        </span>
                        {isToday && (
                          <span className="text-[8px] px-1 rounded bg-[var(--primary)] text-black font-bold">
                            HOJE
                          </span>
                        )}
                      </div>
                      {hasTasks && (
                        <div className="flex items-center gap-1 text-[9px] text-amber-300 bg-amber-950/50 px-1 py-0.5 rounded border border-amber-500/30 truncate">
                          <Bell className="w-2.5 h-2.5" />
                          <span>Tarefa</span>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}