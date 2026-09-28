import React from 'react';

/** Assinatura visual discreta que substitui os corner-brackets aposentados. */
export function SynAccent({ style, className = '' }) {
  return (
    <span
      aria-hidden
      className={`syn-panel-accent ${className}`}
      style={{
        background: 'radial-gradient(circle, rgba(45,253,212,0.5), rgba(138,92,255,0.25) 70%, transparent)',
        right: 10,
        top: 10,
        ...style,
      }}
    />
  );
}

/** Linha orgânica de sparkline (stroke arredondado + glow, sem segmentos). */
export function SynSparkline({ values = [], width = 220, height = 44, color = '#2dfdd4', glow = 'rgba(45,253,212,0.55)', id }) {
  const gid = id || `syn-spark-${Math.abs(values.join(',').length)}`;
  const max = Math.max(1, ...values);
  const pts = values.map((v, i) => {
    const x = (i / Math.max(1, values.length - 1)) * (width - 8) + 4;
    const y = height - 6 - (v / max) * (height - 14);
    return [x, y];
  });
  const d = pts.length
    ? `M ${pts[0][0]},${pts[0][1]}` +
      pts.slice(1).map((p, i) => {
        const prev = pts[i];
        const cx = (prev[0] + p[0]) / 2;
        return ` C ${cx},${prev[1]} ${cx},${p[1]} ${p[0]},${p[1]}`;
      }).join('')
    : '';
  return (
    <svg viewBox={`0 0 ${width} ${height}`} className="w-full" style={{ height }} aria-hidden>
      <defs>
        <linearGradient id={gid} x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="#8a5cff" stopOpacity="0.6" />
          <stop offset="60%" stopColor={color} />
          <stop offset="100%" stopColor="#ff3ec8" stopOpacity="0.7" />
        </linearGradient>
      </defs>
      <path d={d} fill="none" stroke={`url(#${gid})`} strokeWidth="2.5" strokeLinecap="round" style={{ filter: `drop-shadow(0 0 5px ${glow})` }} />
      {pts.map((p, i) => (
        <circle key={i} cx={p[0]} cy={p[1]} r={i === pts.length - 1 ? 3 : 1.6} fill={color} opacity={i === pts.length - 1 ? 1 : 0.55} style={{ filter: `drop-shadow(0 0 4px ${glow})` }} />
      ))}
    </svg>
  );
}

/** Constelação de sinapses: nós com glow ligados por curvas de Bézier. */
export function SynapseGraph({ nodes = [], links = [], width = 560, height = 300, onSelect }) {
  const byId = Object.fromEntries(nodes.map((n) => [n.id, n]));
  return (
    <svg viewBox={`0 0 ${width} ${height}`} className="w-full h-auto" role="img" aria-label="Constelação de memória">
      {links.map((l, i) => {
        const a = byId[l.from];
        const b = byId[l.to];
        if (!a || !b) return null;
        const mx = (a.x + b.x) / 2;
        return (
          <path
            key={i}
            d={`M ${a.x},${a.y} C ${mx},${a.y} ${mx},${b.y} ${b.x},${b.y}`}
            fill="none"
            stroke={l.kind === 'decision' ? 'rgba(255,62,200,0.5)' : 'rgba(45,253,212,0.4)'}
            strokeWidth={l.strong ? 2 : 1.2}
            strokeLinecap="round"
            style={{ filter: 'drop-shadow(0 0 4px rgba(138,92,255,0.5))' }}
          />
        );
      })}
      {nodes.map((n) => (
        <g key={n.id} onClick={() => onSelect?.(n)} style={{ cursor: onSelect ? 'pointer' : 'default' }}>
          <circle cx={n.x} cy={n.y} r={(n.size || 6) + 5} fill={n.color || '#2dfdd4'} opacity="0.18" />
          <circle cx={n.x} cy={n.y} r={n.size || 6} fill="#0a0620" stroke={n.color || '#2dfdd4'} strokeWidth="2" style={{ filter: `drop-shadow(0 0 6px ${n.color || '#2dfdd4'})` }} />
          <text x={n.x} y={n.y + (n.size || 6) + 13} textAnchor="middle" fontSize="10" fill="#eaf8f4" fontFamily="Manrope, sans-serif">
            {n.label}
          </text>
        </g>
      ))}
    </svg>
  );
}
