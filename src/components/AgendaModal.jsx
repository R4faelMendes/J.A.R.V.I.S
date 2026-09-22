import React, { useState } from 'react';
import { X, Calendar as CalIcon, Plus, CheckCircle, Circle, Clock, AlertCircle, Trash2, Bell, ChevronLeft, ChevronRight, Archive } from 'lucide-react';
import { sounds } from '../services/soundEffects';
import { memoryService } from '../services/memoryService';

// Data local no formato AAAA-MM-DD (sem o desvio de fuso do toISOString)
const toLocalISO = (date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;

const TODAY_ISO = toLocalISO(new Date());
const MAX_MONTHS_AHEAD = 12;

const MONTH_NAMES = [
  'janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho',
  'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'
];

function isPastDay(year, month, day) {
  return toLocalISO(new Date(year, month, day)) < TODAY_ISO;
}

export default function AgendaModal({ isOpen, onClose }) {
  const [tasks, setTasks] = useState(() => memoryService.getTasks());
  const [viewMode, setViewMode] = useState('tasks'); // 'tasks', 'calendar'
  const [newTitle, setNewTitle] = useState('');
  const [newDate, setNewDate] = useState(TODAY_ISO);
  const [newTime, setNewTime] = useState('15:00');
  const [newPriority, setNewPriority] = useState('ALTA');
  const [newCategory, setNewCategory] = useState('PROJETO');
  const [showArchive, setShowArchive] = useState(false);
  const [selectedDate, setSelectedDate] = useState(TODAY_ISO);
  const [cursor, setCursor] = useState(() => {
    const now = new Date();
    return { year: now.getFullYear(), month: now.getMonth() };
  });
  const [formError, setFormError] = useState('');

  if (!isOpen) return null;

  const now = new Date();
  const todayISO = toLocalISO(now);
  const tomorrowISO = toLocalISO(new Date(now.getTime() + 86400000));

  const weekDays = ['DOM', 'SEG', 'TER', 'QUA', 'QUI', 'SEX', 'SÁB'];
  const daysInMonth = new Date(cursor.year, cursor.month + 1, 0).getDate();
  const firstWeekday = new Date(cursor.year, cursor.month, 1).getDay();
  const dayCells = [
    ...Array.from({ length: firstWeekday }, () => null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1)
  ];

  const isCurrentPeriod = cursor.year === now.getFullYear() && cursor.month === now.getMonth();
  const canGoBack = cursor.year > now.getFullYear() || (cursor.year === now.getFullYear() && cursor.month > now.getMonth());
  const canGoForward = cursor.year < now.getFullYear() + 1;

  const goPrev = () => {
    if (!canGoBack) {
      sounds.playAlert();
      return;
    }
    sounds.playClick();
    setCursor(({ year, month }) => (month === 0 ? { year: year - 1, month: 11 } : { year, month: month - 1 }));
  };

  const goNext = () => {
    if (!canGoForward) {
      sounds.playAlert();
      return;
    }
    sounds.playClick();
    setCursor(({ year, month }) => (month === 11 ? { year: year + 1, month: 0 } : { year, month: month + 1 }));
  };

  const handleToggleTask = (id) => {
    sounds.playClick();
    const updated = tasks.map(t => (t.id === id ? { ...t, completed: !t.completed } : t));
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

    // Diretriz: nenhum compromisso pode ser criado em um dia que já passou
    if (!newDate || newDate < todayISO) {
      setFormError('Não é possível agendar em uma data que já passou, Senhor.');
      sounds.playAlert();
      return;
    }

    setFormError('');
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

  const sortTasks = (list) =>
    [...list].sort((a, b) => {
      if (a.date !== b.date) return a.date < b.date ? -1 : 1;
      return String(a.time || '').localeCompare(String(b.time || ''));
    });

  const upcomingTasks = sortTasks(tasks.filter(t => t.date >= todayISO));
  const archivedTasks = sortTasks(tasks.filter(t => t.date < todayISO));
  const visibleTasks = showArchive ? sortTasks(tasks) : upcomingTasks;
  const tasksOnSelectedDate = sortTasks(tasks.filter(t => t.date === selectedDate && t.date >= todayISO));

  const dateLabel = (iso) => {
    if (iso === todayISO) return 'HOJE';
    if (iso === tomorrowISO) return 'AMANHÃ';
    return null;
  };

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
                DIRETIVAS OPERACIONAIS • DIAS PASSADOS BLOQUEADOS
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
                    min={todayISO}
                    onChange={(e) => { setNewDate(e.target.value); setFormError(''); }}
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
                {formError && (
                  <div className="text-[11px] font-['JetBrains_Mono'] text-red-300 bg-red-950/40 border border-red-500/40 rounded px-2 py-1">
                    {formError}
                  </div>
                )}
              </form>

              {/* Tasks List */}
              <div className="space-y-2">
                <div className="flex items-center justify-between text-xs font-['JetBrains_Mono'] text-cyan-400/80 border-b border-cyan-500/20 pb-1">
                  <span>DIRETIVAS AGENDADAS ({upcomingTasks.length} pendentes / {archivedTasks.length} no arquivo)</span>
                  <button
                    type="button"
                    onClick={() => { sounds.playClick(); setShowArchive(v => !v); }}
                    className={`flex items-center gap-1 px-2 py-0.5 rounded border transition-all ${
                      showArchive
                        ? 'bg-amber-950/60 border-amber-500/60 text-amber-300'
                        : 'bg-cyan-950/40 border-cyan-500/30 text-cyan-300 hover:border-cyan-300'
                    }`}
                    title="Exibir tarefas de dias que já passaram"
                  >
                    <Archive className="w-3 h-3" />
                    {showArchive ? 'OCULTAR ARQUIVO' : 'ARQUIVO'}
                  </button>
                </div>

                {visibleTasks.length === 0 && (
                  <div className="p-4 text-center text-xs font-['JetBrains_Mono'] text-cyan-400/70 border border-dashed border-cyan-500/20 rounded-lg">
                    Nenhuma diretiva registrada para os próximos dias, Senhor.
                  </div>
                )}

                {visibleTasks.map(task => {
                  const priorityColors = {
                    CRITICA: 'bg-red-950/60 border-red-500 text-red-300 shadow-[0_0_8px_#ef4444]',
                    ALTA: 'bg-amber-950/60 border-amber-500 text-amber-300 shadow-[0_0_8px_#ffb020]',
                    MEDIA: 'bg-cyan-950/60 border-cyan-500 text-cyan-300',
                    BAIXA: 'bg-slate-900 border-slate-600 text-slate-300'
                  };
                  const isArchived = task.date < todayISO;
                  const dayTag = dateLabel(task.date);

                  return (
                    <div
                      key={task.id}
                      className={`p-3 rounded-lg border transition-all flex items-center justify-between gap-3 ${
                        isArchived
                          ? 'bg-slate-950/60 border-slate-600/60 opacity-70'
                          : task.completed
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
                          <div className={`font-['JetBrains_Mono'] text-sm font-semibold ${
                            task.completed ? 'line-through text-cyan-500/60' : isArchived ? 'text-slate-400' : 'text-cyan-100'
                          }`}>
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
                            {dayTag && !task.completed && (
                              <span className="text-[10px] px-1 rounded bg-[var(--primary)] text-black font-bold">{dayTag}</span>
                            )}
                            {isArchived && (
                              <span className="text-[10px] px-1 rounded bg-slate-800 border border-slate-600 text-slate-300">ARQUIVADA</span>
                            )}
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
            /* Monthly Calendar Grid View — dias passados bloqueados */
            <div className="space-y-4">
              <div className="flex items-center justify-between text-sm font-['Orbitron'] font-bold text-[var(--primary)] uppercase glow-cyan">
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={goPrev}
                    disabled={!canGoBack}
                    title={canGoBack ? 'Mês anterior' : 'Dias passados não podem ser visualizados'}
                    className={`p-1.5 rounded border transition-all ${
                      canGoBack
                        ? 'border-cyan-500/40 text-cyan-300 hover:bg-[var(--primary-dim)] hover:border-[var(--primary)]'
                        : 'border-slate-700 text-slate-600 cursor-not-allowed opacity-50'
                    }`}
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </button>
                  <span>{MONTH_NAMES[cursor.month]} {cursor.year}</span>
                  <button
                    type="button"
                    onClick={goNext}
                    disabled={!canGoForward}
                    title={canGoForward ? 'Próximo mês' : 'Limite de 12 meses à frente'}
                    className={`p-1.5 rounded border transition-all ${
                      canGoForward
                        ? 'border-cyan-500/40 text-cyan-300 hover:bg-[var(--primary-dim)] hover:border-[var(--primary)]'
                        : 'border-slate-700 text-slate-600 cursor-not-allowed opacity-50'
                    }`}
                  >
                    <ChevronRight className="w-4 h-4" />
                  </button>
                </div>
                <span className="text-xs text-cyan-400/70 font-['JetBrains_Mono']">
                  {isCurrentPeriod ? 'MÊS ATUAL • FUSO BRASÍLIA (UTC-3)' : 'PRÓXIMOS COMPROMISSOS'}
                </span>
              </div>

              {!canGoBack && (
                <div className="flex items-center gap-2 text-[11px] font-['JetBrains_Mono'] text-amber-300/90 bg-amber-950/30 border border-amber-500/30 rounded px-2 py-1">
                  <AlertCircle className="w-3.5 h-3.5" />
                  Dias e meses que já passaram estão bloqueados nesta agenda, Senhor.
                </div>
              )}

              {/* Day Headers */}
              <div className="grid grid-cols-7 gap-1 text-center font-['JetBrains_Mono'] text-xs text-cyan-400/80 font-bold py-2 border-y border-cyan-500/20">
                {weekDays.map((d, i) => (
                  <div key={i}>{d}</div>
                ))}
              </div>

              {/* Day Cells */}
              <div className="grid grid-cols-7 gap-1.5 font-['JetBrains_Mono'] text-xs">
                {dayCells.map((day, index) => {
                  if (day === null) {
                    return <div key={`blank-${index}`} className="min-h-[64px]" />;
                  }

                  const iso = toLocalISO(new Date(cursor.year, cursor.month, day));
                  const isToday = iso === todayISO;
                  const isPast = isPastDay(cursor.year, cursor.month, day);
                  const isSelected = iso === selectedDate;
                  const dayTasks = tasks.filter(t => t.date === iso);
                  const hasTasks = dayTasks.length > 0;

                  if (isPast) {
                    return (
                      <div
                        key={iso}
                        title="Dia que já passou — visualização bloqueada"
                        className="p-2 min-h-[64px] rounded border border-slate-700/50 bg-slate-950/40 opacity-40 flex flex-col justify-between select-none"
                        style={{ cursor: 'not-allowed' }}
                      >
                        <div className="flex items-center justify-between">
                          <span className="font-bold text-slate-500 line-through">{day}</span>
                          <span className="text-[8px] px-1 rounded bg-slate-800 text-slate-500 font-bold">PASSADO</span>
                        </div>
                      </div>
                    );
                  }

                  return (
                    <div
                      key={iso}
                      onClick={() => { sounds.playClick(); setSelectedDate(iso); }}
                      title={dayTasks.length ? `${dayTasks.length} tarefa(s)` : 'Selecionar dia'}
                      className={`p-2 min-h-[64px] rounded border transition-all cursor-pointer flex flex-col justify-between ${
                        isSelected
                          ? 'bg-[var(--primary-dim)] border-[var(--primary)] shadow-[0_0_12px_var(--primary-glow)]'
                          : isToday
                            ? 'bg-cyan-900/30 border-[var(--primary)] shadow-[0_0_8px_var(--primary-glow)]'
                            : 'bg-cyan-950/20 border-cyan-500/20 hover:border-cyan-400'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <span className={`font-bold ${
                          isSelected || isToday ? 'text-[var(--primary)] font-extrabold text-sm glow-cyan' : 'text-cyan-300'
                        }`}>
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
                          <span>{dayTasks.length} tarefa{dayTasks.length > 1 ? 's' : ''}</span>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>

              {/* Painel do dia selecionado (apenas hoje ou futuro) */}
              <div className="p-4 rounded-lg bg-cyan-950/20 border border-cyan-500/30 space-y-2">
                <div className="text-xs uppercase font-['Orbitron'] text-[var(--primary)] font-bold flex items-center justify-between">
                  <span className="flex items-center gap-2">
                    <CalIcon className="w-4 h-4" />
                    COMPROMISSOS DE {selectedDate}
                  </span>
                  <span className="font-['JetBrains_Mono'] text-[10px] text-cyan-400/70 normal-case">
                    {tasksOnSelectedDate.length} registro(s)
                  </span>
                </div>

                {tasksOnSelectedDate.length === 0 ? (
                  <div className="text-[11px] font-['JetBrains_Mono'] text-cyan-400/70">
                    Nenhuma diretiva para este dia, Senhor.
                  </div>
                ) : (
                  tasksOnSelectedDate.map(task => (
                    <div
                      key={task.id}
                      className={`flex items-center justify-between gap-3 p-2 rounded border ${
                        task.completed ? 'bg-emerald-950/10 border-emerald-500/30 opacity-70' : 'bg-black/40 border-cyan-500/30'
                      }`}
                    >
                      <div className="flex items-center gap-2 flex-1 min-w-0">
                        <button onClick={() => handleToggleTask(task.id)} className="text-cyan-400 hover:text-[var(--primary)]">
                          {task.completed ? <CheckCircle className="w-4 h-4 text-emerald-400" /> : <Circle className="w-4 h-4" />}
                        </button>
                        <span className={`text-xs font-['JetBrains_Mono'] truncate ${task.completed ? 'line-through text-cyan-500/60' : 'text-cyan-100'}`}>
                          {task.title}
                        </span>
                      </div>
                      <span className="text-[10px] font-['JetBrains_Mono'] text-cyan-400/70 flex items-center gap-1">
                        <Clock className="w-3 h-3" /> {task.time}
                      </span>
                      <button
                        onClick={() => handleDeleteTask(task.id)}
                        className="p-1 rounded text-red-400/60 hover:text-red-400 hover:bg-red-950/40 transition-colors"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  ))
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
