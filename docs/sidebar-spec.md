# Sidebar — especificação extraída do protótipo (o protótipo vence)

Fonte: `https://proto-visionary-art.lovable.app` (HTML + `assets/styles-DgepDDVC.css`), 03/10/2026.

## Estrutura
- `nav[aria-label=Principal]`: fixa, vertical, esquerda; `w-[72px]`; altura `calc(100vh-2rem)`;
  margens `m-4 mr-0` (16px em cima/baixo/esquerda, 0 à direita); `rounded-[28px]`
  (`--radius-window: 28px`); `py-5` (20px cima/baixo); `items-center`; `flex-col`; fundo glass.
- Marca no topo: reator 40×40px (`--s:40px`), `mb-6` (24px até o 1º item), link para `/`,
  `aria-label="Início"`, com `data-state` (idle etc.).
- Lista `ul`: `flex-1 flex-col gap-1.5` (gap 6px, ocupa o espaço para empurrar o resto ao fim).
- Configurações: item igual aos demais, DEPOIS da lista (separado pelo flex-1).
- Avatar "LM": círculo 36×36 (`h-9 w-9`), borda `primary/40`, `font-display` 12px semibold,
  cor primary, no fim da barra.

## Item
- Link `h-11 w-11` (44×44px), `rounded-xl` (12px), ícone centralizado.
- Ícone: SVG Lucide 24×24 viewBox, renderizado `h-[19px] w-[19px]`, `stroke-width: 1.6`,
  `stroke: currentColor`, `fill: none`, `round` caps/joins. Conjunto: sparkles (Núcleo),
  message-square (Conversa), layout-grid (Apps), gauge (Desempenho), calendar-days (Agenda),
  mail (E-mails), workflow (Rotinas), smartphone (Celular), shield-check (Segurança),
  settings (Configurações).
- Normal: `text-muted-foreground`. Hover: `bg-accent` + `text-foreground`.
- Ativo (`aria-current="page"`, `data-status="active"`): `bg-primary/12`, `text-primary`,
  sombra interna `inset 0 0 0 1px primary/40`, MAIS barra lateral:
  `absolute -left-[14px] h-5 (20px) w-[3px] rounded-r-full bg-primary` com glow
  (`shadow 0 0 10px primary`).
- Tooltip: rótulo em `glass`, `absolute left-14`, centralizado vertical, `font-display text-xs`,
  `opacity-0 → 100` no hover. Sem rótulo visível — só ícone.
- Transição: `transition-all duration-200` (200ms). Foco teclado: anel primary 2px.
- Sem blur pesado; sem animação com janela oculta.

## Tokens (iguais ao Theme.cs)
`--radius-btn: 10px`, `--radius-panel: 20px`, `--radius-window: 28px`.
Cores via oklch (dark): primary ciano ~`#4FD6FF`, warning ouro, destructive vermelho,
success verde. Temas `data-reactor=dourado/carmesim` e `.day` (claro).

## WPF (SidebarControl)
- Largura 72, cantos 28, padding vertical 20, marca 40px + margem 24, gap 6, itens 44×44
  raio 12, ícone Path 19px StrokeThickness 1.6, barra ativa 3×20 à esquerda do item
  (Canvas overlay), tooltip nativo, Tab/setas, `AutomationProperties.Name`, foco com
  `FocusVisualStyle` em anel ciano 2px. Sem etiqueta de texto (igual ao protótipo).
