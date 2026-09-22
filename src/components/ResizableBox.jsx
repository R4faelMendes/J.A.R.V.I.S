import React, { useRef, useState, useEffect } from 'react';
import { memoryService } from '../services/memoryService';

const clamp = (value, min, max) => Math.min(Math.max(value, min), Math.max(min, max));
const round = (v) => Math.round(v);

// Geometria no início de um arrasto/ajuste: posição, margens e limites do layout
function measure(el) {
  const rect = el.getBoundingClientRect();
  const parentRect = el.parentElement.getBoundingClientRect();
  const prev = el.previousElementSibling;
  const ml = parseFloat(getComputedStyle(el).marginLeft) || 0;
  const mt = parseFloat(getComputedStyle(el).marginTop) || 0;
  return {
    x0: 0,
    y0: 0,
    left: rect.left,
    right: rect.right,
    top: rect.top,
    bottom: rect.bottom,
    w: rect.width,
    h: rect.height,
    ml,
    mt,
    natL: rect.left - ml, // posição natural (sem margem) — invariante durante o arrasto
    natT: rect.top - mt,
    minLeft: parentRect.left, // limites horizontais da coluna
    maxRight: parentRect.right,
    minTop: prev ? prev.getBoundingClientRect().bottom : parentRect.top // não invade o box de cima
  };
}

// Próximo tamanho dado o ponto de partida, o modo (lados/canto) e o deslocamento.
// Usado tanto pelo mouse (arrasto) quanto pelo teclado (setas).
function computeSize(m, mode, dx, dy, minWidth, minHeight, maxWidth, maxHeight) {
  let width = m.w;
  let height = m.h;
  let ml = m.ml;
  let mt = m.mt;

  if (mode.includes('e')) {
    const right = clamp(m.right + dx, m.left + minWidth, Math.min(m.maxRight, m.left + maxWidth));
    width = right - m.left;
  }
  if (mode.includes('w')) {
    const left = clamp(m.left + dx, Math.max(m.minLeft, m.right - maxWidth), m.right - minWidth);
    ml = left - m.natL;
    width = m.right - left;
  }
  if (mode.includes('s')) {
    const bottom = clamp(m.bottom + dy, m.top + minHeight, Math.min(window.innerHeight - 12, m.top + maxHeight));
    height = bottom - m.top;
  }
  if (mode.includes('n')) {
    const top = clamp(m.top + dy, Math.max(m.minTop, m.bottom - maxHeight), m.bottom - minHeight);
    mt = top - m.natT;
    height = m.bottom - top;
  }

  return { width: round(width), height: round(height), ml: round(ml), mt: round(mt) };
}

// Painel redimensionável no estilo de janelas do Windows — em TODOS os lados:
// bordas esquerda/direita/topo/base + os 4 cantos (arraste ou setas do teclado).
// - Duplo clique/Enter restaura o tamanho automático
// - O tamanho nunca ultrapassa a tela nem invade os boxes vizinhos
// - A escolha fica salva na memória permanente do J.A.R.V.I.S.
export default function ResizableBox({
  id,
  children,
  minWidth = 200,
  minHeight = 150,
  maxWidth = 1000,
  maxHeight = 1600
}) {
  const boxRef = useRef(null);
  const dragRef = useRef(null);
  const pendingRef = useRef(null);
  const [size, setSize] = useState(() => {
    try {
      const saved = memoryService.getBoxSizes()[id];
      if (saved && saved.width >= minWidth && saved.height >= minHeight) {
        return {
          width: saved.width,
          height: saved.height,
          ml: typeof saved.ml === 'number' ? saved.ml : null,
          mt: typeof saved.mt === 'number' ? saved.mt : null
        };
      }
    } catch { /* ignora */ }
    return null;
  });

  // Sempre que a janela do app muda, o box é reencaixado (largura, altura e margens)
  useEffect(() => {
    if (!size) return undefined;
    const onResize = () => {
      const el = boxRef.current;
      if (!el || !el.parentElement) return;
      const m = measure(el);
      const parentW = el.parentElement.clientWidth;

      const width = clamp(size.width, minWidth, Math.min(maxWidth, parentW));
      let ml = size.ml;
      if (typeof ml === 'number') ml = round(clamp(ml, 0, Math.max(0, parentW - width)));

      let mt = size.mt;
      const minMT = m.minTop - m.natT;
      if (typeof mt === 'number') mt = round(clamp(mt, minMT, minMT + 2000));

      const activeMt = typeof mt === 'number' ? mt : m.mt;
      const maxBottom = window.innerHeight - 12 - (m.natT + activeMt);
      const height = clamp(size.height, minHeight, Math.max(minHeight, Math.min(maxHeight, maxBottom)));

      const next = { width: round(width), height: round(height), ml, mt };
      if (next.width !== size.width || next.height !== size.height
        || next.ml !== size.ml || next.mt !== size.mt) {
        setSize(next);
        memoryService.setBoxSize(id, next);
      }
    };
    window.addEventListener('resize', onResize);
    onResize(); // já valida o tamanho salvo logo na abertura
    return () => window.removeEventListener('resize', onResize);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [size, id]);

  const startDrag = (mode) => (e) => {
    e.preventDefault();
    e.stopPropagation();
    const el = boxRef.current;
    if (!el) return;

    // Captura o ponteiro: o arrasto continua suave mesmo passando por iframes
    try { e.currentTarget.setPointerCapture(e.pointerId); } catch { /* ignora */ }

    const m = measure(el);
    m.x0 = e.clientX;
    m.y0 = e.clientY;
    dragRef.current = m;
    pendingRef.current = null;
    el.classList.add('resizing');
    document.body.classList.add('box-resizing');
    const prevCursor = document.body.style.cursor;
    document.body.style.cursor = mode === 'e' || mode === 'w' ? 'ew-resize'
      : mode === 'n' || mode === 's' ? 'ns-resize'
        : mode === 'ne' || mode === 'sw' ? 'nesw-resize' : 'nwse-resize';

    const onMove = (ev) => {
      const d = dragRef.current;
      if (!d) return;
      const next = computeSize(d, mode, ev.clientX - d.x0, ev.clientY - d.y0,
        minWidth, minHeight, maxWidth, maxHeight);
      pendingRef.current = next;
      setSize(next);
    };

    const onUp = () => {
      window.removeEventListener('pointermove', onMove);
      window.removeEventListener('pointerup', onUp);
      el.classList.remove('resizing');
      document.body.classList.remove('box-resizing');
      document.body.style.cursor = prevCursor;
      dragRef.current = null;
      if (pendingRef.current) memoryService.setBoxSize(id, pendingRef.current);
      pendingRef.current = null;
    };

    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp);
  };

  // Redimensionar pelo teclado (acessibilidade): setas ajustam, Shift amplia, Enter restaura
  const handleKey = (mode) => (e) => {
    const el = boxRef.current;
    if (!el) return;
    if (e.key === 'Enter') {
      e.preventDefault();
      resetSize();
      return;
    }
    const step = e.shiftKey ? 64 : 16;
    let dx = 0;
    let dy = 0;
    if (e.key === 'ArrowRight') dx = step;
    else if (e.key === 'ArrowLeft') dx = -step;
    else if (e.key === 'ArrowDown') dy = step;
    else if (e.key === 'ArrowUp') dy = -step;
    else return;
    e.preventDefault();
    const m = measure(el);
    const next = computeSize(m, mode, dx, dy, minWidth, minHeight, maxWidth, maxHeight);
    setSize(next);
    memoryService.setBoxSize(id, next);
  };

  const resetSize = () => {
    setSize(null);
    memoryService.setBoxSize(id, null);
  };

  const handleProps = (mode, label) => ({
    title: `${label} • arraste, ou foque e use as setas (Shift = maior) • duplo clique/Enter restaura`,
    onPointerDown: startDrag(mode),
    onKeyDown: handleKey(mode),
    onDoubleClick: (e) => { e.preventDefault(); resetSize(); },
    tabIndex: 0,
    role: 'separator',
    'aria-label': label
  });

  return (
    <div
      ref={boxRef}
      className={`resizable-box${size ? ' has-explicit-size' : ''}`}
      style={size
        ? {
          width: size.width,
          height: size.height,
          ...(typeof size.ml === 'number' ? { marginLeft: size.ml } : {}),
          ...(typeof size.mt === 'number' ? { marginTop: size.mt } : {})
        }
        : undefined}
    >
      <div className="resizable-content">{children}</div>

      {/* Alças das 4 bordas */}
      <div className="resizable-handle resizable-handle-e" {...handleProps('e', 'Redimensionar (borda direita)')} />
      <div className="resizable-handle resizable-handle-w" {...handleProps('w', 'Redimensionar (borda esquerda)')} />
      <div className="resizable-handle resizable-handle-n" {...handleProps('n', 'Redimensionar (borda superior)')} />
      <div className="resizable-handle resizable-handle-s" {...handleProps('s', 'Redimensionar (borda inferior)')} />

      {/* Alças dos 4 cantos */}
      <div className="resizable-handle resizable-handle-ne" {...handleProps('ne', 'Redimensionar (canto superior direito)')} />
      <div className="resizable-handle resizable-handle-nw" {...handleProps('nw', 'Redimensionar (canto superior esquerdo)')} />
      <div className="resizable-handle resizable-handle-sw" {...handleProps('sw', 'Redimensionar (canto inferior esquerdo)')} />
      <div className="resizable-handle resizable-handle-se" {...handleProps('se', 'Redimensionar (canto inferior direito)')} />
    </div>
  );
}
