// J.A.R.V.I.S. — Localizador e controlador de recursos do PC
// (aplicativos instalados, atalhos, UWP/Start Menu, documentos e pastas de projetos)
//
// Este módulo é Node puro (sem dependência do Electron) para que possa ser
// testado isoladamente: `ELECTRON_RUN_AS_NODE=1 electron.exe <script>`.

const { exec, execFile } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');

// ---------------------------------------------------------------------------
// Utilidades
// ---------------------------------------------------------------------------

function normalize(str) {
  return String(str || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, ' ')
    .trim();
}

// Grupos de sinônimos em português -> nomes reais dos executáveis/aplicativos
const ALIAS_GROUPS = [
  { terms: ['visual studio code', 'vs code', 'vscode', 'code editor', 'editor de codigo', 'editor de codigo vscode'], match: ['code'] },
  { terms: ['navegador', 'browser', 'internet'], match: ['brave', 'chrome', 'msedge', 'firefox', 'opera'] },
  { terms: ['loja', 'microsoft store', 'store'], match: ['microsoft store', 'windows store'] },
  { terms: ['terminal', 'prompt', 'console', 'console de comandos'], match: ['windows terminal', 'powershell', 'cmd', 'command prompt'] },
  { terms: ['bloco de notas', 'notepad'], match: ['notepad'] },
  { terms: ['calculadora'], match: ['calculator'] },
  { terms: ['email', 'e mail', 'correio', 'mensagens'], match: ['outlook', 'mail', 'thunderbird'] },
  { terms: ['gerenciador de arquivos', 'explorador de arquivos', 'explorador'], match: ['explorer', 'file explorer'] },
  { terms: ['gerenciador de tarefas'], match: ['taskmgr', 'task manager'] },
  { terms: ['configuracoes do windows', 'configuracoes'], match: ['settings', 'systemsettings'] },
  { terms: ['documentos', 'pasta de documentos', 'meus documentos', 'pasta documentos'], match: ['documents', 'documentos'] },
  { terms: ['area de trabalho', 'desktop'], match: ['desktop', 'area de trabalho'] },
  { terms: ['downloads', 'baixados', 'pasta de downloads'], match: ['downloads', 'baixados'] },
  { terms: ['imagens', 'fotos', 'pasta de imagens'], match: ['pictures', 'imagens', 'screenshots'] },
  { terms: ['videos', 'pasta de videos'], match: ['videos'] },
  { terms: ['pasta de musica', 'musicas'], match: ['music', 'musica'] }
];

function expandTerms(term) {
  const out = new Set([term]);
  const apply = (t) => {
    out.add(t);
    for (const g of ALIAS_GROUPS) {
      if (g.terms.includes(t)) g.match.forEach(m => out.add(m));
    }
  };
  apply(term);
  // Expande também palavras isoladas ("procurar configurações do sistema" -> "settings")
  for (const w of String(term || '').split(' ')) {
    if (w.length > 2) apply(w);
  }
  return Array.from(out);
}

// Palavras genéricas/demais comuns: sozinhas não identificam um recurso
const GENERIC_WORDS = new Set([
  'abrir', 'abre', 'abre', 'procurar', 'procura', 'pesquisar', 'pesquisa', 'buscar',
  'fechar', 'fecha', 'executar', 'rodar', 'pasta', 'pastas', 'arquivo', 'arquivos',
  'documento', 'documentos', 'meu', 'minha', 'meus', 'minhas', 'programa', 'programas',
  'aplicativo', 'aplicativos', 'app', 'apps', 'coisa', 'coisas', 'sistema', 'aqui',
  'onde', 'esta', 'está', 'tem', 'quero', 'preciso', 'por', 'favor', 'favor'
]);

// Escore de similaridade entre um candidato e o termo buscado (0..110)
function squash(str) {
  return String(str).replace(/[^a-z0-9]/g, '');
}

function scoreMatch(candidateNorm, termNorm) {
  if (!candidateNorm || !termNorm) return 0;
  if (candidateNorm === termNorm) return 100;
  if (candidateNorm.startsWith(termNorm)) return 86;
  const words = candidateNorm.split(' ').filter(Boolean);
  if (words.includes(termNorm)) return 76;
  if (candidateNorm.includes(termNorm)) return 62;
  const termWords = termNorm.split(' ').filter(w => w.length > 1);
  if (termWords.length > 1 && termWords.every(w => candidateNorm.includes(w))) return 54;

  // Nomes com pontuação/acentuação ("J.A.R.V.I.S", "vs-code")
  const cSq = squash(candidateNorm);
  const tSq = squash(termNorm);
  if (cSq && tSq) {
    if (cSq === tSq) return 100;
    if (cSq.startsWith(tSq)) return 84;
    // Prefixo invertido só quando o candidato cobre boa parte do termo
    // (evita "System" casar com "systemsettings" só por ser o começo)
    if (tSq.startsWith(cSq) && cSq.length >= Math.ceil(tSq.length * 0.6)) return 84;
    if (cSq.includes(tSq)) return 60;
  }
  return 0;
}

function bestScore(candidateNorm, terms) {
  let best = 0;
  for (const t of terms) {
    best = Math.max(best, scoreMatch(candidateNorm, t));
  }
  return best;
}

// Pontuação final: melhor termo isolado + bônus quando TODAS as palavras da
// busca aparecem no nome (ex.: "projeto temperatura" -> "temperatura-mobile")
function scoreCandidate(candidateNorm, terms, queryNorm) {
  let score = bestScore(candidateNorm, terms);
  const words = String(queryNorm || '').split(' ').filter(w => w.length > 2);
  if (score > 0 && words.length > 1 && words.every(w => candidateNorm.includes(w))) {
    score += 10;
  }
  return score;
}

function psCommand(script, timeout = 9000, maxBuffer = 12 * 1024 * 1024) {
  // UTF-8 no stdout: sem isso nomes com acento chegam molidos ao Node
  // ("Configurações" -> "Configura��es") e nunca casam com a busca por voz.
  const wrapped = `try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}; ${script}`;
  return new Promise((resolve) => {
    execFile(
      'powershell.exe',
      ['-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-Command', wrapped],
      { timeout, maxBuffer, windowsHide: true },
      (err, stdout) => resolve(err ? '' : String(stdout || ''))
    );
  });
}

// Executa um comando do PowerShell e informa se não houve erro.
// (Start-Process/explorer precisam do PowerShell: o cmd.exe não os conhece)
function runDetached(cmd, opts = {}) {
  return new Promise((resolve) => {
    execFile(
      'powershell.exe',
      ['-NoProfile', '-NonInteractive', '-Command', cmd],
      { windowsHide: false, timeout: 20000, ...opts },
      (err) => resolve(!err)
    );
  });
}

function startProcessCmd(target, args = '') {
  const t = psQuote(target);
  if (args && String(args).trim()) {
    return `Start-Process -FilePath ${t} -ArgumentList ${psQuote(String(args))}`;
  }
  return `Start-Process -FilePath ${t}`;
}

function psQuote(value) {
  return `'${String(value).replace(/'/g, "''")}'`;
}

// ---------------------------------------------------------------------------
// Cache de índices do sistema (evita varrer o disco a cada comando de voz)
// ---------------------------------------------------------------------------

const CACHE_TTL_MS = 5 * 60 * 1000;
const cache = {
  shortcuts: { data: null, at: 0 },
  startApps: { data: null, at: 0 },
  appPaths: { data: null, at: 0 },
  processes: { data: null, at: 0 },
  codeCli: { data: null, at: 0 }
};

function fresh(entry) {
  return entry.data && Date.now() - entry.at < CACHE_TTL_MS;
}

// ---------------------------------------------------------------------------
// 1. Processos em execução (focar/encerrar apps abertos)
// ---------------------------------------------------------------------------

function getRunningProcesses(force = false) {
  if (!force && fresh(cache.processes)) return Promise.resolve(cache.processes.data);
  return new Promise((resolve) => {
    exec('tasklist /FO CSV /NH', { timeout: 6000, windowsHide: true, maxBuffer: 4 * 1024 * 1024 }, (err, stdout) => {
      const list = [];
      if (!err && stdout) {
        for (const line of stdout.split('\n')) {
          const m = line.match(/^"([^"]+)","(\d+)"/);
          if (m) {
            list.push({ name: m[1].replace(/\.exe$/i, ''), pid: parseInt(m[2], 10), file: m[1] });
          }
        }
      }
      cache.processes = { data: list, at: Date.now() };
      resolve(list);
    });
  });
}

// ---------------------------------------------------------------------------
// 2. Atalhos (.lnk / .url) do Menu Iniciar e Área de Trabalho
// ---------------------------------------------------------------------------

async function getShortcuts() {
  if (fresh(cache.shortcuts)) return cache.shortcuts.data;
  // -Filter é muito mais rápido que -Include (varre direto no NTFS)
  const script = `
$sh = New-Object -ComObject WScript.Shell
$dirs = @(
  "$env:APPDATA\\Microsoft\\Windows\\Start Menu\\Programs",
  "$env:ProgramData\\Microsoft\\Windows\\Start Menu\\Programs",
  [Environment]::GetFolderPath('Desktop'),
  "$env:PUBLIC\\Desktop"
)
$out = foreach ($d in $dirs) {
  if (Test-Path $d) {
    $files = @(Get-ChildItem -Path $d -Recurse -Filter *.lnk -File -ErrorAction SilentlyContinue) +
             @(Get-ChildItem -Path $d -Recurse -Filter *.url -File -ErrorAction SilentlyContinue)
    foreach ($f in $files) {
      $target = $f.FullName; $a = ''
      if ($f.Extension -eq '.lnk') {
        try { $s = $sh.CreateShortcut($f.FullName); $target = $s.TargetPath; $a = $s.Arguments } catch {}
      }
      [PSCustomObject]@{ n = $f.BaseName; t = $target; a = $a; src = $f.FullName }
    }
  }
}
ConvertTo-Json -InputObject @($out) -Compress
`;
  const stdout = await psCommand(script, 20000);
  let data = [];
  try {
    const parsed = JSON.parse(stdout);
    data = (Array.isArray(parsed) ? parsed : [parsed]).filter(x => x && x.n);
  } catch { data = []; }
  cache.shortcuts = { data, at: Date.now() };
  return data;
}

// ---------------------------------------------------------------------------
// 3. Aplicativos registrados (App Paths do registro)
// ---------------------------------------------------------------------------

async function getAppPaths() {
  if (fresh(cache.appPaths)) return cache.appPaths.data;
  const script = `
$roots = @(
  'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths',
  'HKLM:\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\App Paths',
  'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths'
)
$out = foreach ($r in $roots) {
  if (Test-Path $r) {
    Get-ChildItem $r -ErrorAction SilentlyContinue | ForEach-Object {
      $p = (Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue).'(default)'
      if ($p) { [PSCustomObject]@{ n = $_.PSChildName -replace '\\.exe$', ''; t = $p } }
    }
  }
}
ConvertTo-Json -InputObject @($out) -Compress
`;
  const stdout = await psCommand(script, 10000);
  let data = [];
  try {
    const parsed = JSON.parse(stdout);
    data = (Array.isArray(parsed) ? parsed : [parsed]).filter(x => x && x.n && x.t);
  } catch { data = []; }
  cache.appPaths = { data, at: Date.now() };
  return data;
}

// ---------------------------------------------------------------------------
// 4. Apps do Menu Iniciar / loja (UWP — AppsFolder)
// ---------------------------------------------------------------------------

async function getStartApps() {
  if (fresh(cache.startApps)) return cache.startApps.data;
  const stdout = await psCommand('Get-StartApps | ConvertTo-Json -Compress', 10000);
  let data = [];
  try {
    const parsed = JSON.parse(stdout);
    data = (Array.isArray(parsed) ? parsed : [parsed]).filter(x => x && (x.Name || x.name));
    data = data.map(x => ({ n: x.Name || x.name, appId: x.AppID || x.AppId || x.appID }));
  } catch { data = []; }
  cache.startApps = { data, at: Date.now() };
  return data;
}

// ---------------------------------------------------------------------------
// 5. Varredura de documentos e pastas de projetos no perfil do usuário
// ---------------------------------------------------------------------------

const NOISE_DIRS = new Set([
  'node_modules', '.git', '.svn', 'appdata', '$recycle.bin', 'windows', 'program files',
  'program files (x86)', 'programdata', '.cache', '__pycache__', '.venv', 'venv', '.next',
  '.vs', '.idea', 'obj', '.gradle', '.android', '.electron', '.npm', '.config', 'packages',
  'nuget', '.nuget', 'dotnet', 'winsxs', 'system32'
]);

const DOC_EXTENSIONS = new Set([
  '.pdf', '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx', '.txt', '.md', '.csv', '.json',
  '.js', '.jsx', '.ts', '.tsx', '.py', '.java', '.cs', '.cpp', '.c', '.html', '.css', '.zip',
  '.png', '.jpg', '.jpeg', '.psd', '.fig', '.sketch', '.mp3', '.mp4', '.wav', '.pptx', '.rtf'
]);

const PROJECT_MARKERS = ['package.json', '.git', '.gitignore', '.sln', 'pom.xml', 'requirements.txt', 'Cargo.toml', 'composer.json'];

function userSearchRoots() {
  const home = os.homedir();
  const roots = [];
  const pushSafe = (p) => { if (p && fs.existsSync(p) && !roots.includes(p)) roots.push(p); };

  pushSafe(path.join(home, 'Desktop'));
  pushSafe(path.join(home, 'Documents'));
  pushSafe(path.join(home, 'Downloads'));
  pushSafe(path.join(home, 'Pictures'));
  pushSafe(path.join(home, 'Videos'));
  pushSafe(path.join(home, 'Music'));

  try {
    for (const entry of fs.readdirSync(home, { withFileTypes: true })) {
      if (!entry.isDirectory()) continue;
      const n = entry.name.toLowerCase();
      if (n.startsWith('onedrive')) pushSafe(path.join(home, entry.name));
      if (['projects', 'projetos', 'dev', 'repos', 'source', 'workspace', 'documents', 'desktop', 'downloads'].includes(n)) {
        pushSafe(path.join(home, entry.name));
      }
      if (n === 'source') pushSafe(path.join(home, entry.name, 'repos'));
    }
  } catch { /* ignora */ }

  return roots;
}

function walkForMatches(rootDir, terms, opts) {
  const { maxDepth = 4, limit = 30, timeoutMs = 6000 } = opts;
  return new Promise((resolve) => {
    const matches = [];
    const stack = [{ dir: rootDir, depth: 0 }];
    const started = Date.now();
    let done = false;

    const finish = () => {
      if (done) return;
      done = true;
      resolve(matches);
    };

    const tick = () => {
      if (done) return;
      if (matches.length >= limit || Date.now() - started > timeoutMs) return finish();
      const node = stack.pop();
      if (!node) return finish();

      let entries;
      try {
        entries = fs.readdirSync(node.dir, { withFileTypes: true });
      } catch {
        return schedule();
      }

      for (const entry of entries) {
        if (matches.length >= limit) break;
        const full = path.join(node.dir, entry.name);
        const norm = normalize(entry.name.replace(/\.[^.]+$/, '')) || normalize(entry.name);
        const score = bestScore(norm, terms) || bestScore(normalize(entry.name), terms);
        if (score > 0) {
          const isDir = entry.isDirectory();
          matches.push({ full, name: entry.name, isDir, score });
        }
        if (entry.isDirectory() && node.depth < maxDepth) {
          const low = entry.name.toLowerCase();
          if (!NOISE_DIRS.has(low) && !low.startsWith('.')) {
            stack.push({ dir: full, depth: node.depth + 1 });
          }
        }
      }
      schedule();
    };

    const schedule = () => {
      setImmediate(tick);
    };

    schedule();
  });
}

async function searchUserFiles(terms) {
  const roots = userSearchRoots();
  const collected = [];

  // As próprias pastas raiz também são candidatas ("abrir documentos")
  for (const root of roots) {
    const base = path.basename(root);
    const score = Math.max(
      bestScore(normalize(base), terms),
      bestScore(squash(normalize(base)), terms.map(squash))
    );
    if (score > 0) collected.push({ full: root, name: base, isDir: true, score });
  }

  for (const root of roots) {
    if (collected.length >= 30) break;
    const found = await walkForMatches(root, terms, { maxDepth: 4, limit: 30, timeoutMs: 6000 });
    collected.push(...found);
  }
  return collected;
}

// ---------------------------------------------------------------------------
// Montagem da lista de candidatos
// ---------------------------------------------------------------------------

// Mapa "nome do executável" -> nomes de exibição conhecidos (atalhos/App Paths).
// Permite reconhecer "calculadora" quando o processo se chama "win32calc.exe".
function buildExeDisplayMap(shortcuts, appPaths) {
  const map = new Map();
  const add = (exeBase, display) => {
    const key = normalize(exeBase);
    if (!key || !display) return;
    if (!map.has(key)) map.set(key, []);
    const list = map.get(key);
    if (!list.includes(display)) list.push(display);
  };
  for (const s of shortcuts || []) {
    if (s && s.t) add(path.basename(String(s.t)).replace(/\.[^.]+$/, ''), s.n);
  }
  for (const a of appPaths || []) {
    if (a && a.t) add(path.basename(String(a.t)).replace(/\.[^.]+$/, ''), a.n);
  }
  return map;
}

function processScore(proc, terms, queryNorm, exeMap) {
  let score = scoreCandidate(normalize(proc.name), terms, queryNorm);
  const displays = exeMap ? exeMap.get(normalize(proc.name)) : null;
  if (displays) {
    for (const display of displays) {
      score = Math.max(score, scoreCandidate(normalize(display), terms, queryNorm));
    }
  }
  return score;
}

function kindPriority(kind) {
  switch (kind) {
    case 'running': return 30;   // app já aberto: quase sempre é isso que o usuário quer
    case 'shortcut': return 26;  // atalho do Menu Iniciar / Área de Trabalho
    case 'startapp': return 24;  // app registrado no Menu Iniciar
    case 'apppath': return 22;   // executável registrado no registro do Windows
    case 'project': return 16;   // pasta com .git / package.json
    case 'folder': return 12;
    case 'file': return 6;
    case 'exe': return 5;
    default: return 1;
  }
}

function classifyFile(full) {
  const stat = safeStat(full);
  if (!stat) return null;
  if (stat.isDirectory()) {
    let isProject = false;
    try {
      const kids = fs.readdirSync(full);
      isProject = PROJECT_MARKERS.some(m => kids.includes(m));
    } catch { /* ignora */ }
    return isProject ? 'project' : 'folder';
  }
  return path.extname(full).toLowerCase() === '.exe' ? 'exe' : 'file';
}

function safeStat(p) {
  try { return fs.statSync(p); } catch { return null; }
}

async function findResource(rawQuery) {
  const query = String(rawQuery || '').trim();
  const queryNorm = normalize(query);
  const terms = expandTerms(queryNorm);
  // Também considera cada palavra isolada ("projeto clima" -> "clima")
  for (const word of queryNorm.split(' ')) {
    if (word.length > 2 && !terms.includes(word)) terms.push(word);
  }
  if (!terms[0]) return { query, found: false, candidates: [] };

  // Gate de credibilidade: em buscas com várias palavras, exige que a palavra
  // "distintiva" apareça no candidato. Evita falsos positivos como procurar
  // "arquivo inexistente 98765" e encontrar "Explorador de Arquivos".
  const distinctiveWords = queryNorm.split(' ').filter(w => w.length > 2 && !GENERIC_WORDS.has(w));
  const strongTerms = terms.filter(t =>
    t.split(' ').filter(w => w.length > 2).some(w => !GENERIC_WORDS.has(w))
  );
  const credible = (nameNorm) => {
    if (!nameNorm || distinctiveWords.length === 0) return true;
    if (strongTerms.some(t => scoreMatch(nameNorm, t) >= 70)) return true;
    const hits = distinctiveWords.filter(w => nameNorm.includes(w)).length;
    return hits * 2 >= distinctiveWords.length;
  };

  const candidates = [];

  const push = (c, extraNames = []) => {
    if (!c) return;
    const nameNorm = normalize(c.name);
    const names = [nameNorm, ...extraNames.filter(Boolean).map(normalize)];
    if (!names.some(credible)) return;
    let score = scoreCandidate(nameNorm, terms, queryNorm) || c.score || 0;
    // Nome que bate exatamente com o termo (inclusive sinônimo) quase sempre é
    // a resposta certa: vence prioridade de processos em execução.
    if (score > 0 && names.some(n => terms.some(t => t === n || (squash(t) && squash(t) === squash(n))))) {
      score += 20;
    }
    if (score <= 0) return;
    candidates.push({ ...c, score: score + kindPriority(c.kind) });
  };

  // 1) processos em execução
  const [shortcuts, appPaths] = await Promise.all([getShortcuts(), getAppPaths()]);
  const processes = await getRunningProcesses();
  const exeMap = buildExeDisplayMap(shortcuts, appPaths);
  for (const proc of processes) {
    push({
      kind: 'running',
      name: proc.name,
      path: proc.file,
      pid: proc.pid,
      score: processScore(proc, terms, queryNorm, exeMap)
    }, exeMap.get(normalize(proc.name)) || []);
  }

  // 2) atalhos do Menu Iniciar / Área de Trabalho
  for (const s of shortcuts) {
    push({
      kind: 'shortcut',
      name: s.n,
      path: s.src,
      target: s.t,
      args: s.a,
      score: bestScore(normalize(s.n), terms)
    });
  }

  // 3) registro App Paths (executáveis)
  for (const a of appPaths) {
    push({
      kind: 'apppath',
      name: a.n,
      path: a.t,
      target: a.t,
      score: bestScore(normalize(a.n), terms)
    });
  }

  // 4) apps UWP / Menu Iniciar
  const startApps = await getStartApps();
  for (const a of startApps) {
    push({
      kind: 'startapp',
      name: a.n,
      appId: a.appId,
      score: bestScore(normalize(a.n), terms)
    });
  }

  // 5) documentos, pastas e projetos
  const files = await searchUserFiles(terms);
  for (const f of files) {
    const kind = classifyFile(f.full);
    if (!kind) continue;
    push({
      kind,
      name: f.name,
      path: f.full,
      score: f.score
    }, [f.full]);
  }

  candidates.sort((a, b) => b.score - a.score);
  const unique = [];
  const seen = new Set();
  for (const c of candidates) {
    const key = `${c.kind}:${(c.path || c.appId || c.name || '').toLowerCase()}`;
    if (seen.has(key)) continue;
    seen.add(key);
    unique.push(c);
  }

  const best = unique[0] || null;
  return {
    query,
    found: !!best,
    kind: best?.kind || null,
    name: best?.name || null,
    path: best?.path || null,
    appId: best?.appId || null,
    pid: best?.pid || null,
    candidates: unique.slice(0, 6)
  };
}

// ---------------------------------------------------------------------------
// Abertura dos recursos encontrados
// ---------------------------------------------------------------------------

async function hasCodeCli() {
  if (fresh(cache.codeCli)) return cache.codeCli.data;
  const out = await psCommand('try { (Get-Command code -ErrorAction Stop).Source } catch { "" }', 5000);
  const found = !!out.trim();
  cache.codeCli = { data: found, at: Date.now() };
  return found;
}

function describe(result) {
  if (!result?.found) return null;
  switch (result.kind) {
    case 'running': return `${result.name} já está em execução`;
    case 'shortcut': return `atalho: ${result.path}`;
    case 'startapp': return `aplicativo do Windows: ${result.name}`;
    case 'apppath': return `executável: ${result.path}`;
    case 'project': return `pasta de projeto: ${result.path}`;
    case 'folder': return `pasta: ${result.path}`;
    case 'exe': return `executável: ${result.path}`;
    case 'file': return `arquivo: ${result.path}`;
    default: return result.path || result.name;
  }
}

async function focusProcess(pid, name) {
  const script = pid
    ? `$w = New-Object -ComObject WScript.Shell; [void]$w.AppActivate(${Number(pid)})`
    : `$w = New-Object -ComObject WScript.Shell; [void]$w.AppActivate(${psQuote(name)})`;
  const out = await psCommand(`
    ${script}
    Start-Sleep -Milliseconds 300
    if ($?) { 'ok' } else { 'fail' }
  `, 5000);
  return out.trim().includes('ok');
}

async function openCandidate(item) {
  if (!item) return { opened: false, reason: 'not-found' };

  if (item.kind === 'running') {
    const ok = await focusProcess(item.pid, item.name);
    return { opened: ok, mode: 'focus', detail: ok ? `${item.name} em foco` : 'não foi possível focar a janela' };
  }

  if (item.kind === 'startapp') {
    const appId = String(item.appId || '').replace(/'/g, "''");
    const ok = await runDetached(`Start-Process -FilePath explorer.exe -ArgumentList ('"' + 'shell:AppsFolder\\${appId}' + '"')`);
    return { opened: ok, mode: 'uwp', detail: item.name };
  }

  if (item.kind === 'project') {
    const useCode = await hasCodeCli();
    if (useCode) {
      const ok = await runDetached(`code -n ${psQuote(item.path)}`);
      if (ok) return { opened: true, mode: 'vscode', detail: item.path };
    }
    const ok = await runDetached(startProcessCmd(item.path));
    return { opened: ok, mode: 'explorer', detail: item.path };
  }

  if (item.kind === 'folder') {
    const ok = await runDetached(startProcessCmd(item.path));
    return { opened: ok, mode: 'explorer', detail: item.path };
  }

  // shortcut / apppath / file / exe — usa a associação padrão do Windows
  const target = item.kind === 'shortcut' ? item.path : (item.target || item.path);
  if (!target || !fs.existsSync(target)) {
    return { opened: false, reason: 'missing', detail: target };
  }
  const ok = await runDetached(startProcessCmd(target, item.args || ''));
  return { opened: ok, mode: 'shell', detail: target };
}

async function closeResource(rawQuery) {
  const query = String(rawQuery || '').trim();
  const queryNorm = normalize(query);
  const terms = expandTerms(queryNorm);
  for (const word of queryNorm.split(' ')) {
    if (word.length > 2 && !terms.includes(word)) terms.push(word);
  }

  const [processes, shortcuts, appPaths] = await Promise.all([
    getRunningProcesses(true),
    getShortcuts(),
    getAppPaths()
  ]);
  const exeMap = buildExeDisplayMap(shortcuts, appPaths);

  let best = null;
  let bestScoreValue = 0;
  for (const p of processes) {
    const s = processScore(p, terms, queryNorm, exeMap);
    if (s > bestScoreValue) { bestScoreValue = s; best = p; }
  }
  if (!best || bestScoreValue < 50) {
    return { query, found: false };
  }
  await new Promise((resolve) => {
    exec(`taskkill /F /IM "${best.file}"`, { windowsHide: true }, () => resolve());
  });
  cache.processes = { data: null, at: 0 };
  return { query, found: true, name: best.name, path: best.file };
}

// Pré-carrega os índices em segundo plano para que o 1º comando de voz seja rápido
function warmupIndexes() {
  return Promise.allSettled([
    getRunningProcesses(),
    getShortcuts(),
    getAppPaths(),
    getStartApps()
  ]);
}

module.exports = {
  findResource,
  openCandidate,
  closeResource,
  warmupIndexes,
  describe,
  scoreCandidate,
  getRunningProcesses,
  getShortcuts,
  getStartApps,
  getAppPaths,
  normalize,
  scoreMatch
};
