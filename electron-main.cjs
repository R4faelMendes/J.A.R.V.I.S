// Electron Main Process for J.A.R.V.I.S. Desktop Application
const { app, BrowserWindow, ipcMain, shell } = require('electron');
const path = require('path');
const os = require('os');
const fs = require('fs');
const { exec, execSync, spawn } = require('child_process');

// Áudio liberado sem exigir interação prévia (escuta contínua + resposta por voz)
app.commandLine.appendSwitch('autoplay-policy', 'no-user-gesture-required');

// ---- Instância única ----
// Duas instâncias abertas ao mesmo tempo brigam pelo arquivo de memória: a que
// fecha por último sobrescreve as configurações salvas pela outra (sensação de
// "config não salva"). A segunda cópia só foca a primeira janela.
if (process.env.JARVIS_SMOKE !== '1') {
  const gotLock = app.requestSingleInstanceLock();
  if (!gotLock) {
    app.quit();
  } else {
    app.on('second-instance', () => {
      try {
        if (mainWindow && !mainWindow.isDestroyed()) {
          if (mainWindow.isMinimized()) mainWindow.restore();
          mainWindow.show();
          mainWindow.focus();
        }
      } catch { /* ignora */ }
    });
  }
}

const systemTools = require('./system-tools.cjs');

let mainWindow;

function notifyRenderer(channel, payload) {
  try {
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.send(channel, payload);
    }
  } catch { /* janela pode já ter sido fechada */ }
}

async function loadApp(win) {
  const isDev = process.env.NODE_ENV === 'development' || process.argv.includes('--dev');
  const indexPath = path.join(__dirname, 'dist', 'index.html');
  // Modo autoteste: JARVIS_SMOKE=1 carrega a UI com ?smoke=1 (executa src/services/selfTest.js)
  const smokeParts = [];
  if (process.env.JARVIS_SMOKE === '1') smokeParts.push('smoke=1', 'autoclose=1');
  if (process.env.JARVIS_MODAL) smokeParts.push(`modal=${encodeURIComponent(process.env.JARVIS_MODAL)}`);
  const smokeQuery = smokeParts.length ? `?${smokeParts.join('&')}` : '';

  if (isDev) {
    try {
      await win.loadURL(`http://127.0.0.1:5173${smokeQuery}`);
      return;
    } catch (err) {
      console.warn('[J.A.R.V.I.S.] Dev server nao encontrado em http://127.0.0.1:5173. Tentando build local...', err.message);
    }
  }

  // Se dist/index.html não existir, compila o build de produção automaticamente
  if (!fs.existsSync(indexPath)) {
    console.log('[J.A.R.V.I.S.] dist/index.html nao encontrado. Compilando com Vite...');
    try {
      execSync('npm run build', { cwd: __dirname, stdio: 'inherit' });
    } catch (err) {
      console.error('[J.A.R.V.I.S.] Erro ao compilar com Vite:', err);
    }
  }

  if (fs.existsSync(indexPath)) {
    if (smokeQuery) await win.loadFile(indexPath, { search: smokeQuery.slice(1) });
    else win.loadFile(indexPath);
  } else {
    win.loadURL('http://127.0.0.1:5173').catch(() => {
      win.loadURL(`data:text/html,
        <body style="background:#02060f;color:#00f0ff;font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;flex-direction:column;">
          <h1 style="font-size:24px;">J.A.R.V.I.S. // Falha ao Inicializar</h1>
          <p style="color:#b8e2f2;">O arquivo dist/index.html nao foi encontrado e o servidor Vite nao esta rodando.</p>
          <p style="color:#94a3b8;font-size:13px;">Execute: <code>npm run build</code> ou <code>npm run electron:dev</code></p>
        </body>
      `);
    });
  }
}

const startupDir = process.env.APPDATA
  ? path.join(process.env.APPDATA, 'Microsoft', 'Windows', 'Start Menu', 'Programs', 'Startup')
  : null;
const startupVbsPath = startupDir ? path.join(startupDir, 'JARVIS-Startup.vbs') : null;
const projectVbsPath = path.join(__dirname, 'iniciar-jarvis.vbs');

function setAutostartConfig(enable) {
  try {
    if (app.isPackaged) {
      app.setLoginItemSettings({
        openAtLogin: enable,
        path: process.execPath
      });
    } else {
      app.setLoginItemSettings({
        openAtLogin: enable,
        path: process.execPath,
        args: [path.resolve(__dirname)]
      });
    }

    if (startupVbsPath) {
      if (enable) {
        const vbsContent = `Set WshShell = CreateObject("WScript.Shell")\r\nWshShell.Run "wscript.exe " & Chr(34) & "${projectVbsPath.replace(/\\/g, '\\\\')}" & Chr(34), 0, False\r\n`;
        fs.writeFileSync(startupVbsPath, vbsContent, 'utf-8');
      } else {
        if (fs.existsSync(startupVbsPath)) {
          fs.unlinkSync(startupVbsPath);
        }
      }
    }
    return true;
  } catch (err) {
    console.error('Error in setAutostartConfig:', err);
    return false;
  }
}

function getAutostartConfig() {
  try {
    if (startupVbsPath && fs.existsSync(startupVbsPath)) {
      return true;
    }
    const settings = app.getLoginItemSettings();
    return !!settings.openAtLogin;
  } catch {
    return false;
  }
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 800,
    minHeight: 600,
    backgroundColor: '#02060f',
    frame: true,
    title: 'J.A.R.V.I.S. // Painel Holográfico Tático v4.8',
    autoHideMenuBar: true,
    webPreferences: {
      preload: path.join(__dirname, 'electron-preload.cjs'),
      nodeIntegration: false,
      contextIsolation: true,
      webSecurity: false, // Permite carregar iframes e webviews de serviços externos como YouTube Music
      allowRunningInsecureContent: true,
      webviewTag: true // YouTube Music em <webview> (permite login da conta Google)
    }
  });

  // Interceptar requisições para remover restrições de X-Frame-Options do YouTube Music
  mainWindow.webContents.session.webRequest.onHeadersReceived({ urls: ['*://*/*'] }, (details, callback) => {
    const responseHeaders = { ...details.responseHeaders };
    for (const key of Object.keys(responseHeaders)) {
      const lower = key.toLowerCase();
      if (lower === 'x-frame-options' || lower === 'content-security-policy') {
        delete responseHeaders[key];
      }
    }
    callback({ cancel: false, responseHeaders });
  });

  // Brave Shields (emulado): bloqueia anúncios/rastreadores em todas as janelas
  initShields(mainWindow.webContents.session);

  // Conceder permissão automática para microfone e gravação de áudio
  mainWindow.webContents.session.setPermissionRequestHandler((webContents, permission, callback) => {
    if (permission === 'media' || permission === 'microphone' || permission === 'audioCapture') {
      callback(true); // Autorizar microfone sempre
      return;
    }
    callback(true);
  });

  loadApp(mainWindow);

  // Sincroniza inicialização com o Windows se ativada
  try {
    if (getAutostartConfig()) {
      setAutostartConfig(true);
    }
  } catch (err) {
    console.warn('Autostart sync warning:', err);
  }
}

// ---------------------------------------------------------------------------
// MEMÓRIA PERSISTENTE EM DISCO — preferências, agenda e tamanhos dos boxes
// Salvas em arquivo no perfil do usuário para sobreviver a qualquer reinício
// (o localStorage do renderer pode sumir se a origem/partição mudar).
// ---------------------------------------------------------------------------
function memoryFilePath() {
  return path.join(app.getPath('userData'), 'jarvis-memory.json');
}

ipcMain.on('memory-load-sync', (event) => {
  try {
    const file = memoryFilePath();
    if (fs.existsSync(file)) {
      // remove BOM (arquivos editados no Bloco de Notas podem ter)
      const raw = fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '');
      event.returnValue = JSON.parse(raw);
      return;
    }
  } catch (err) {
    console.warn('[J.A.R.V.I.S.] Falha ao ler memória em disco:', err.message);
  }
  event.returnValue = null;
});

function writeMemoryBundle(bundle) {
  try {
    fs.writeFileSync(memoryFilePath(), JSON.stringify(bundle, null, 2), 'utf8');
    return { ok: true };
  } catch (err) {
    return { ok: false, error: err.message };
  }
}

ipcMain.handle('memory-save', (event, bundle) => writeMemoryBundle(bundle));
ipcMain.on('memory-save-sync', (event, bundle) => {
  event.returnValue = writeMemoryBundle(bundle);
});

// UA padrão do Chrome (sem "Electron"): navegadores embutidos são bloqueados
// pelo login da Google/YouTube ("This browser or app may not be secure" / 401).
function chromeUserAgent() {
  const ua = app.userAgentFallback || '';
  const platform = (ua.match(/\(([^)]+)\)/) || [])[1] || 'Windows NT 10.0; Win64; x64';
  const webkit = (ua.match(/AppleWebKit\/[\d.]+/) || ['AppleWebKit/537.36'])[0];
  const chrome = (ua.match(/Chrome\/[\d.]+/) || ['Chrome/131.0.0.0'])[0];
  const safari = (ua.match(/Safari\/[\d.]+/) || ['Safari/537.36'])[0];
  return `Mozilla/5.0 (${platform}) ${webkit} (KHTML, like Gecko) ${chrome} ${safari}`;
}

// ---------------------------------------------------------------------------
// BRAVE "EMULADO" — chrome de navegador simulado dentro do box + Shields
// ---------------------------------------------------------------------------

// Client Hints no formato do Google Chrome: o login do Google verifica as
// marcas de sec-ch-ua ("Google Chrome") e recusa navegadores sem elas
// ("This browser or app may not be secure").
function chromeMetadata() {
  const ua = app.userAgentFallback || '';
  const chromeFull = (ua.match(/Chrome\/([\d.]+)/) || ['', '131.0.6778.86'])[1];
  const major = chromeFull.split('.')[0];
  return {
    brands: [
      { brand: 'Google Chrome', version: major },
      { brand: 'Chromium', version: major },
      { brand: 'Not_A Brand', version: '24' }
    ],
    fullVersion: chromeFull,
    platform: 'Windows',
    platformVersion: '10.0.0',
    architecture: 'x86',
    model: '',
    mobile: false,
    bitness: 'x86'
  };
}

// Toda janela/webview gerada (YouTube Music, Agenda Google, popups de login)
// recebe User-Agent e Client Hints idênticos ao Google Chrome.
// Scripts injetados ANTES de qualquer código da página removem os sinais de
// "navegador automatizado/webview" que fazem o Google recusar o login
// ("This browser or app may not be secure"): navigator.webdriver, a ausência de
// window.chrome (runtime/app/csi/loadTimes) e navigator.plugins zerado.
const GOOGLE_LOGIN_STEALTH = `
(() => {
  // 1) navigator.webdriver — webview costuma expor true/undefined = sinal de bot
  try {
    Object.defineProperty(Navigator.prototype, 'webdriver', { get: function () { return false; }, configurable: true });
  } catch (e) {}

  try {
    // 2) window.chrome — Chrome real tem runtime/app/csi/loadTimes; no Electron
    // os membros existem como getters SOMENTE-LEITURA e a atribuição simples é
    // ignorada em silêncio. Montamos um objeto NOVO já com os membros forçados
    // (sem copiar descritores, que podem ser non-configurable) e trocamos a
    // referência da janela por ele. __sp = fase (diagnóstico do autoteste).
    window.__sp = 1;
    var force = function (obj, key, val) {
      try { Object.defineProperty(obj, key, { value: val, configurable: true, writable: true, enumerable: true }); return true; }
      catch (e4) { try { obj[key] = val; return true; } catch (e5) { return false; } }
    };
    var g = {};
    force(g, 'runtime', {
      id: undefined,
      connect: function () { return { onMessage: { addListener: function () {} }, onDisconnect: { addListener: function () {} }, postMessage: function () {}, disconnect: function () {} }; },
      sendMessage: function () {},
      onMessage: { addListener: function () {} },
      onInstalled: { addListener: function () {} }
    });
    window.__sp = 2;
    force(g, 'app', {
      isInstalled: false,
      getIsInstalled: function () { return false; },
      InstallState: { DISABLED: 'disabled', INSTALLED: 'installed', NOT_INSTALLED: 'not_installed' },
      RunningState: { CANNOT_RUN: 'cannot_run', READY_TO_RUN: 'ready_to_run', RUNNING: 'running' }
    });
    window.__sp = 3;
    force(g, 'loadTimes', function () {
      var t = (typeof performance === 'object' && performance.timeOrigin) ? performance.timeOrigin / 1000 : Date.now() / 1000;
      return { requestTime: t, startLoadTime: t, commitLoadTime: t, finishDocumentLoadTime: t, finishLoadTime: t, firstPaintTime: t, firstPaintAfterLoadTime: 0, navigationType: 'Other', wasFetchedViaSpdy: false, wasNpnNegotiated: false, npnNegotiatedProtocol: 'h2', wasAlternateProtocolAvailable: false, connectionInfo: 'h2', entryPoints: [] };
    });
    force(g, 'csi', function () { return { startE: Date.now(), onloadT: Date.now(), pageT: (typeof performance === 'object' ? performance.now() : 0), tran: 0 }; });
    force(g, 'metricsReportingEnabled', false);
    window.__sp = 4;
    var oldChrome = undefined;
    try { oldChrome = window.chrome; } catch (e2) { oldChrome = undefined; }
    if (oldChrome && typeof oldChrome === 'object') {
      var names = Object.getOwnPropertyNames(oldChrome);
      for (var ni = 0; ni < names.length; ni++) {
        var key = names[ni];
        if (key === 'runtime' || key === 'app' || key === 'loadTimes' || key === 'csi' || key === 'metricsReportingEnabled') continue;
        try {
          var v = oldChrome[key];
          if (v !== undefined) force(g, key, v);
        } catch (e3) {}
      }
    }
    window.__sp = 5;
    Object.defineProperty(window, 'chrome', { value: g, configurable: true, writable: true, enumerable: true });
    window.__sp = 9;
  } catch (e) { window.__spErr = String((e && e.message) || e); }

  try {
    // 3) navigator.plugins/mimeTypes — webview reporta 0; Chrome sempre tem PDF
    window.__sp3 = 1;
    var pluginDefs = [
      ['Chrome PDF Viewer', 'internal-pdf-viewer', 'Portable Document Format'],
      ['Chrome PDF Plugin', 'internal-pdf-viewer', 'Portable Document Format'],
      ['Native Client', 'internal-nacl-plugin', '']
    ];
    var pluginList = [];
    for (var i = 0; i < pluginDefs.length; i++) {
      (function (d) {
        var p = Object.create(Plugin.prototype);
        Object.defineProperty(p, 'name', { get: function () { return d[0]; } });
        Object.defineProperty(p, 'filename', { get: function () { return d[1]; } });
        Object.defineProperty(p, 'description', { get: function () { return d[2]; } });
        Object.defineProperty(p, 'length', { get: function () { return 0; } });
        p.item = function () { return null; };
        p.namedItem = function () { return null; };
        pluginList.push(p);
      })(pluginDefs[i]);
    }
    var pluginsArr = Object.create(PluginArray.prototype);
    Object.defineProperty(pluginsArr, 'length', { get: function () { return pluginList.length; } });
    pluginsArr.item = function (idx) { return pluginList[idx] || null; };
    pluginsArr.namedItem = function (name) { for (var j = 0; j < pluginList.length; j++) { if (pluginList[j].name === name) return pluginList[j]; } return null; };
    for (var k = 0; k < pluginList.length; k++) { (function (idx) { Object.defineProperty(pluginsArr, String(idx), { get: function () { return pluginList[idx]; } }); })(k); }
    Object.defineProperty(Navigator.prototype, 'plugins', { get: function () { return pluginsArr; }, configurable: true });
    window.__sp3 = 2;

    var mimeDefs = [
      ['application/pdf', 'pdf'],
      ['text/pdf', 'pdf'],
      ['application/x-google-chrome-pdf', 'pdf']
    ];
    var mimeList = [];
    for (var m = 0; m < mimeDefs.length; m++) {
      (function (d) {
        var mt = Object.create(MimeType.prototype);
        Object.defineProperty(mt, 'type', { get: function () { return d[0]; } });
        Object.defineProperty(mt, 'suffixes', { get: function () { return d[1]; } });
        Object.defineProperty(mt, 'description', { get: function () { return 'Portable Document Format'; } });
        Object.defineProperty(mt, 'enabledPlugin', { get: function () { return pluginList[0]; } });
        mimeList.push(mt);
      })(mimeDefs[m]);
    }
    var mimesArr = Object.create(MimeTypeArray.prototype);
    Object.defineProperty(mimesArr, 'length', { get: function () { return mimeList.length; } });
    mimesArr.item = function (idx) { return mimeList[idx] || null; };
    mimesArr.namedItem = function (name) { for (var j = 0; j < mimeList.length; j++) { if (mimeList[j].type === name) return mimeList[j]; } return null; };
    for (var n = 0; n < mimeList.length; n++) { (function (idx) { Object.defineProperty(mimesArr, String(idx), { get: function () { return mimeList[idx]; } }); })(n); }
    Object.defineProperty(Navigator.prototype, 'mimeTypes', { get: function () { return mimesArr; }, configurable: true });
    Object.defineProperty(Navigator.prototype, 'pdfViewerEnabled', { get: function () { return true; }, configurable: true });
    window.__sp3 = 3;
  } catch (e) {}
})();
`;

app.on('web-contents-created', (event, contents) => {
  try {
    const type = contents.getType();
    if (type !== 'webview' && type !== 'window') return;
    contents.setUserAgent(chromeUserAgent());
    try { contents.debugger.attach('1.3'); } catch { /* já anexado */ }
    const send = (method, params) => {
      try { contents.debugger.sendCommand(method, params).catch(() => { /* ignora */ }); } catch { /* ignora */ }
    };
    send('Emulation.setUserAgentOverride', {
      userAgent: chromeUserAgent(),
      acceptLanguage: 'pt-BR,pt;q=0.9,en;q=0.8',
      platform: 'Win32',
      userAgentMetadata: chromeMetadata()
    });
    send('Page.enable');
    // Documentos futuros (login = navegação nova) recebem o script ANTES de
    // qualquer código da página...
    send('Page.addScriptToEvaluateOnNewDocument', { source: GOOGLE_LOGIN_STEALTH });
    // ...o documento atual recebe na hora (a navegação inicial disputa com o
    // envio do CDP; sem isto a 1ª página ficaria sem o patch)...
    send('Runtime.evaluate', { expression: GOOGLE_LOGIN_STEALTH });
    // ...e cada documento que termina de carregar recebe de novo (popups de
    // login do Google criados como nova janela)
    contents.on('did-finish-load', () => {
      send('Runtime.evaluate', { expression: GOOGLE_LOGIN_STEALTH });
    });
  } catch { /* ignora */ }
});

// ---- Brave Shields (emulado): bloqueia anúncios/rastreadores como o Brave ----
const SHIELDS_BLOCKLIST = [
  'doubleclick.net', 'googlesyndication.com', 'googleadservices.com',
  'adservice.google.com', 'ads.pubmatic.com', 'adnxs.com',
  'amazon-adsystem.com', 'scorecardresearch.com', 'moatads.com',
  'taboola.com', 'outbrain.com', 'google-analytics.com',
  'ads.youtube.com', 'media.net', 'criteo.com', 'zedo.com'
];

let shieldsEnabled = true;
let shieldsBlocked = 0;

function isShieldsBlocked(rawUrl) {
  try {
    const url = new URL(rawUrl);
    const host = url.hostname.toLowerCase();
    if (SHIELDS_BLOCKLIST.some((d) => host === d || host.endsWith(`.${d}`))) return true;
    const pathQuery = `${url.pathname}${url.search}`.toLowerCase();
    return pathQuery.includes('/pagead/') || pathQuery.includes('/api/stats/ads')
      || pathQuery.includes('/get_video_ads') || pathQuery.includes('ad_break');
  } catch {
    return false;
  }
}

function initShields(sess) {
  sess.webRequest.onBeforeRequest({ urls: ['*://*/*'] }, (details, callback) => {
    if (shieldsEnabled && isShieldsBlocked(details.url)) {
      shieldsBlocked += 1;
      notifyRenderer('shields-blocked', shieldsBlocked);
      callback({ cancel: true });
      return;
    }
    callback({ cancel: false });
  });
}

ipcMain.handle('shields-status', () => ({ blocked: shieldsBlocked, enabled: shieldsEnabled }));
ipcMain.on('shields-set-enabled', (event, enabled) => { shieldsEnabled = enabled !== false; });

// Windows native system commands
ipcMain.on('system-power', (event, type) => {
  if (type === 'SHUTDOWN') {
    exec('shutdown /s /t 10 /c "J.A.R.V.I.S.: Encerrando sessão do Windows..."');
  } else if (type === 'RESTART') {
    exec('shutdown /r /t 5');
  } else if (type === 'LOCK') {
    exec('rundll32.exe user32.dll,LockWorkStation');
  } else if (type === 'EXIT_APP') {
    app.quit();
  }
});

// Real PC Hardware Metrics Telemetry
ipcMain.handle('get-system-metrics', async () => {
  const totalMem = os.totalmem();
  const freeMem = os.freemem();
  const usedMem = totalMem - freeMem;
  const memUsagePercent = Math.round((usedMem / totalMem) * 100);
  const cpus = os.cpus();
  
  return {
    totalMemGB: (totalMem / (1024 ** 3)).toFixed(1),
    usedMemGB: (usedMem / (1024 ** 3)).toFixed(1),
    freeMemGB: (freeMem / (1024 ** 3)).toFixed(1),
    memPercent: memUsagePercent,
    cpuModel: cpus[0]?.model || 'Intel / AMD Processor',
    cpuCores: cpus.length,
    cpuSpeed: cpus[0]?.speed || 0,
    uptimeHours: (os.uptime() / 3600).toFixed(1)
  };
});

// Media Key Controller for YouTube Music on Brave / Windows
ipcMain.on('media-control', (event, action) => {
  // Uses PowerShell WScript.Shell to send global media keys without focusing the window
  const keyCodes = {
    PLAY_PAUSE: '{MEDIA_PLAY_PAUSE}',
    NEXT: '{MEDIA_NEXT_TRACK}',
    PREV: '{MEDIA_PREV_TRACK}'
  };
  const key = keyCodes[action] || '{MEDIA_PLAY_PAUSE}';
  const cmd = `powershell -Command "$wscript = New-Object -ComObject WScript.Shell; $wscript.SendKeys('${key}')"`;
  exec(cmd);
});

// ---------------------------------------------------------------------------
// BRAVE BROWSER — navegador oficial do YouTube Music no J.A.R.V.I.S.
// O Google recusa login em webviews/contexts embutidos ("This browser or app
// may not be secure"), então o player real roda no Brave: navegador de verdade
// (login da conta normalmente), sem anúncios (Brave Shields) e com o perfil
// do usuário persistido. Aberto em modo "--app" = janela limpa, sem abas nem
// barra de endereço.
// ---------------------------------------------------------------------------
const BRAVE_CANDIDATES = [
  process.env.LOCALAPPDATA
    ? path.join(process.env.LOCALAPPDATA, 'BraveSoftware', 'Brave-Browser', 'Application', 'brave.exe')
    : null,
  'C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe',
  'C:\\Program Files (x86)\\BraveSoftware\\Brave-Browser\\Application\\brave.exe'
];

function findBrave() {
  for (const candidate of BRAVE_CANDIDATES) {
    try {
      if (candidate && fs.existsSync(candidate)) return candidate;
    } catch { /* ignora */ }
  }
  return null;
}

// O J.A.R.V.I.S NUNCA abre o Brave (nem nenhuma janela externa): o YouTube Music
// e a agenda do Google rodam no emulador embutido. Só resta a detecção do
// caminho, usada pelo autoteste e pelo finder de aplicativos "abrir X".
ipcMain.handle('brave-status', () => {
  const brave = findBrave();
  return { found: !!brave, path: brave || '' };
});

// App Process Management (Close / Open apps without collision)
ipcMain.on('process-control', (event, { action, target }) => {
  if (action === 'KILL') {
    const processMap = {
      vscode: 'Code.exe',
      brave: 'brave.exe',
      spotify: 'Spotify.exe',
      discord: 'Discord.exe',
      chrome: 'chrome.exe'
    };
    const exe = processMap[target.toLowerCase()] || (target.endsWith('.exe') ? target : `${target}.exe`);
    exec(`taskkill /F /IM ${exe}`);
  }
});

// Real Windows Task Manager Process List Query
ipcMain.handle('get-running-processes', async () => {
  return new Promise((resolve) => {
    // Executa tasklist para listar processos com nome e consumo de memória
    exec('tasklist /FO CSV /NH', (err, stdout) => {
      if (err || !stdout) {
        resolve([]);
        return;
      }

      try {
        const lines = stdout.trim().split('\n');
        const procMap = new Map();

        lines.forEach(line => {
          const parts = line.split('","').map(p => p.replace(/"/g, '').trim());
          if (parts.length >= 5) {
            const name = parts[0];
            const pid = parts[1];
            const mem = parts[4]; // ex: "154.200 K"

            // Lista de processos de sistema/driver de fundo a ignorar completamente
            const systemNoise = [
              'svchost', 'system', 'smss', 'csrss', 'services', 'lsass', 'wininit', 'winlogon',
              'fontdrvhost', 'dwm', 'sihost', 'taskhostw', 'explorer', 'shellexperiencehost',
              'searchui', 'searchapp', 'startmenuexperiencehost', 'textinputhost', 'runtimebroker',
              'wudfhost', 'conhost', 'cmd', 'powershell', 'node', 'registry', 'memory compression',
              'spoolsv', 'securityhealthservice', 'sgrmbroker', 'ctfmon', 'dasHost', 'smartscreen'
            ];
            
            const lowerName = name.toLowerCase().replace('.exe', '');
            const isIgnored = systemNoise.some(noise => lowerName.startsWith(noise));

            if (!isIgnored && !procMap.has(name)) {
              procMap.set(name, {
                id: lowerName,
                name: name.replace('.exe', ''),
                process: name,
                pid: pid,
                memory: mem,
                status: 'Ativo'
              });
            }
          }
        });

        const list = Array.from(procMap.values()).slice(0, 10);
        resolve(list);
      } catch (parseErr) {
        resolve([]);
      }
    });
  });
});

// Auto-Start with Windows configuration
ipcMain.handle('set-autostart', (event, enable) => {
  return setAutostartConfig(enable);
});

ipcMain.handle('get-autostart', () => {
  return getAutostartConfig();
});

// Automation Sequence with Brave Browser & VS Code
ipcMain.on('run-automation', (event, { type, params }) => {
  if (type === 'CREATE_GITHUB_REPO') {
    const bravePath = '"C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe"';
    const braveLocal = `"${process.env.LOCALAPPDATA}\\BraveSoftware\\Brave-Browser\\Application\\brave.exe"`;
    const url = 'https://github.com/new';

    exec(`${bravePath} ${url}`, (err) => {
      if (err) {
        exec(`${braveLocal} ${url}`, (err2) => {
          if (err2) {
            shell.openExternal(url);
          }
        });
      }
    });
  } else if (type === 'OPEN_VSCODE') {
    const project = params?.project || 'temperatura-mobile';
    exec(`code . --add "${project}"`, (err) => {
      if (err) {
        exec('code .');
      }
    });
  }
});

// ---------------------------------------------------------------------------
// J.A.R.V.I.S. — MOTOR DE VOZ (Whisper local + Voz Neural Microsoft Edge)
// ---------------------------------------------------------------------------

// ---- 1. Worker de reconhecimento de voz offline (Whisper) ----
let sttWorker = null;
let sttSeq = 0;
let sttState = 'off';
let sttRestarts = 0;
let sttRestartTimer = null;
let sttModel = 'base';
const sttPending = new Map();

function setSttState(state, extra = {}) {
  sttState = state;
  notifyRenderer('stt-status', { state, model: sttModel, ...extra });
}

function failPendingStt(reason) {
  for (const [, entry] of sttPending) {
    clearTimeout(entry.timer);
    entry.resolve({ error: reason });
  }
  sttPending.clear();
}

function startSttWorker() {
  if (sttWorker) return sttWorker;

  const modelsDir = path.join(app.getPath('userData'), 'models');
  try {
    fs.mkdirSync(modelsDir, { recursive: true });
  } catch { /* ignora */ }

  try {
    sttWorker = spawn(process.execPath, [path.join(__dirname, 'stt-worker.cjs'), modelsDir], {
      env: { ...process.env, ELECTRON_RUN_AS_NODE: '1' },
      stdio: ['pipe', 'pipe', 'pipe'],
      windowsHide: true,
      cwd: __dirname
    });
  } catch (err) {
    console.error('[J.A.R.V.I.S.] Falha ao iniciar worker de voz:', err);
    setSttState('error', { message: err.message });
    return null;
  }

  setSttState('starting');
  sttWorker.stdin.write(`${JSON.stringify({ type: 'warmup', model: sttModel })}\n`);

  let buffer = '';
  sttWorker.stdout.on('data', (chunk) => {
    buffer += chunk.toString();
    let idx;
    while ((idx = buffer.indexOf('\n')) >= 0) {
      const line = buffer.slice(0, idx).trim();
      buffer = buffer.slice(idx + 1);
      if (!line) continue;

      let msg;
      try { msg = JSON.parse(line); } catch { continue; }

      if (msg.id != null && sttPending.has(msg.id)) {
        const entry = sttPending.get(msg.id);
        sttPending.delete(msg.id);
        clearTimeout(entry.timer);
        entry.resolve(msg);
        continue;
      }
      if (msg.type === 'started') {
        setSttState('starting', { pid: msg.pid });
      } else if (msg.type === 'ready') {
        sttRestarts = 0;
        setSttState('ready', { warmupMs: msg.ms });
      } else if (msg.type === 'log') {
        console.log('[J.A.R.V.I.S. STT]', msg.msg);
      }
    }
  });

  sttWorker.stderr.on('data', (data) => {
    const text = data.toString().trim();
    if (text) console.log('[J.A.R.V.I.S. STT:err]', text);
  });

  sttWorker.on('exit', (code) => {
    console.warn(`[J.A.R.V.I.S.] Worker de voz encerrado (code=${code})`);
    sttWorker = null;
    failPendingStt('worker-encerrado');

    // Reinício automático (o modelo pode ter ficado sem memória, etc.)
    if (sttRestarts < 3) {
      sttRestarts += 1;
      setSttState('restarting', { attempt: sttRestarts });
      sttRestartTimer = setTimeout(() => startSttWorker(), 1500);
    } else {
      setSttState('error', { message: `worker encerrado (código ${code})` });
    }
  });

  return sttWorker;
}

function transcribeAudio({ samples, sampleRate = 16000, model }) {
  const worker = startSttWorker();
  if (!worker) return Promise.resolve({ error: 'motor-de-voz-indisponivel' });

  const id = `t${++sttSeq}`;
  return new Promise((resolve) => {
    const timer = setTimeout(() => {
      sttPending.delete(id);
      resolve({ id, error: 'timeout' });
    }, 120000);

    sttPending.set(id, { resolve, timer });

    try {
      worker.stdin.write(`${JSON.stringify({ id, samples, sampleRate, model: model || sttModel })}\n`);
    } catch (err) {
      sttPending.delete(id);
      clearTimeout(timer);
      resolve({ id, error: err.message });
    }
  });
}

ipcMain.handle('stt-transcribe', async (event, payload) => transcribeAudio(payload || {}));
ipcMain.on('stt-warmup', (event, model) => {
  if (model && model !== sttModel) {
    sttModel = model;
    sttRestarts = 0;
    if (sttWorker) {
      try { sttWorker.kill(); } catch { /* ignora */ }
      sttWorker = null;
      startSttWorker();
      return;
    }
  }
  startSttWorker();
});
ipcMain.handle('stt-status', () => ({ state: sttState, model: sttModel }));

// Captura a janela do J.A.R.V.I.S (usado pelo autoteste para verificar a UI)
ipcMain.handle('save-screenshot', async (event, name = 'ui') => {
  try {
    if (!mainWindow || mainWindow.isDestroyed()) return { ok: false, error: 'sem janela' };
    // Traz a janela para frente: janela oculta/oculta gera quadro desatualizado na captura
    mainWindow.show();
    mainWindow.focus();
    await new Promise((r) => setTimeout(r, 300));
    const image = await mainWindow.webContents.capturePage();
    const dir = path.join(os.tmpdir(), 'jarvis-smoke');
    fs.mkdirSync(dir, { recursive: true });
    const safe = String(name).replace(/[^a-z0-9_-]/gi, '') || 'ui';
    const file = path.join(dir, `${safe}-${Date.now()}.png`);
    fs.writeFileSync(file, image.toPNG());
    console.log('[J.A.R.V.I.S.] captura de tela salva em', file);
    return { ok: true, path: file };
  } catch (err) {
    return { ok: false, error: err.message };
  }
});

// ---- 2. Voz neural Microsoft Edge (TTS sem chave de API) ----
let edgeTtsInstance = null;
let edgeTtsVoice = null;
let neuralVoicesCache = null;

const NEURAL_OUTPUT_FORMAT = 'audio-24khz-48kbitrate-mono-mp3';

function getEdgeTts() {
  if (!edgeTtsInstance) {
    const { MsEdgeTTS } = require('msedge-tts');
    edgeTtsInstance = new MsEdgeTTS();
  }
  return edgeTtsInstance;
}

function prosodyPercent(value, fallback = 0) {
  const num = Number(value);
  if (!Number.isFinite(num)) return `+${fallback}%`;
  const pct = Math.round((num - 1) * 100);
  return `${pct >= 0 ? '+' : ''}${pct}%`;
}

async function synthesizeNeural({ text, voice, rate = 1, pitch = 1 }) {
  const tts = getEdgeTts();
  const voiceName = voice || 'pt-BR-AntonioNeural';
  if (edgeTtsVoice !== voiceName) {
    await tts.setMetadata(voiceName, NEURAL_OUTPUT_FORMAT);
    edgeTtsVoice = voiceName;
  }

  const { audioStream } = tts.toStream(String(text), {
    rate: prosodyPercent(rate),
    pitch: prosodyPercent(pitch),
    volume: '100'
  });

  return new Promise((resolve, reject) => {
    const chunks = [];
    const timeout = setTimeout(() => {
      try { audioStream.destroy(); } catch { /* ignora */ }
      reject(new Error('timeout na síntese neural'));
    }, 25000);

    audioStream.on('data', (chunk) => chunks.push(chunk));
    audioStream.once('error', (err) => {
      clearTimeout(timeout);
      reject(err);
    });
    audioStream.once('end', () => {
      clearTimeout(timeout);
      const buf = Buffer.concat(chunks);
      if (!buf.length) reject(new Error('áudio neural vazio'));
      else resolve(buf);
    });
  });
}

ipcMain.handle('tts-synthesize', async (event, opts = {}) => {
  try {
    const text = String(opts.text || '').trim();
    if (!text) return { ok: false, error: 'texto-vazio' };
    const buf = await synthesizeNeural(opts);
    return { ok: true, mime: 'audio/mpeg', data: buf.toString('base64'), voice: opts.voice };
  } catch (err) {
    console.warn('[J.A.R.V.I.S.] TTS neural falhou:', err.message);
    return { ok: false, error: err.message };
  }
});

ipcMain.handle('tts-voices', async () => {
  if (neuralVoicesCache) return neuralVoicesCache;
  try {
    const tts = getEdgeTts();
    const voices = await tts.getVoices();
    const filtered = (Array.isArray(voices) ? voices : [])
      .filter((v) => v && v.ShortName && (String(v.Locale || '').startsWith('pt') || String(v.Locale || '') === 'en-US' || String(v.Locale || '') === 'en-GB'))
      .map((v) => ({
        shortName: v.ShortName,
        friendlyName: v.FriendlyName || v.Name || v.ShortName,
        gender: v.Gender || '',
        locale: v.Locale || ''
      }))
      .sort((a, b) => {
        const aPt = a.locale.startsWith('pt') ? 0 : 1;
        const bPt = b.locale.startsWith('pt') ? 0 : 1;
        if (aPt !== bPt) return aPt - bPt;
        return a.friendlyName.localeCompare(b.friendlyName);
      });
    neuralVoicesCache = filtered;
    return filtered;
  } catch (err) {
    console.warn('[J.A.R.V.I.S.] Falha ao listar vozes neurais:', err.message);
    return [];
  }
});

// ---- 3. Controle de aplicativos, documentos e projetos do PC ----
ipcMain.handle('pc-control', async (event, { action, query } = {}) => {
  try {
    if (action === 'find') {
      const result = await systemTools.findResource(query);
      return { ...result, description: systemTools.describe(result) };
    }

    if (action === 'open') {
      const result = await systemTools.findResource(query);
      if (!result.found) return { ...result, opened: false };
      const opened = await systemTools.openCandidate(result);
      return { ...result, opened: opened.opened, openMode: opened.mode, description: systemTools.describe(result), detail: opened.detail };
    }

    if (action === 'close') {
      return await systemTools.closeResource(query);
    }

    return { error: 'ação desconhecida' };
  } catch (err) {
    console.error('[J.A.R.V.I.S.] Erro no controle de PC:', err);
    return { error: err.message };
  }
});

app.whenReady().then(() => {
  createWindow();
  // Pré-carrega o modelo de voz para que o primeiro comando seja instantâneo
  setTimeout(() => startSttWorker(), 2500);
  // Pré-indexa atalhos/apps do Windows em segundo plano (busca por voz instantânea)
  setTimeout(() => {
    systemTools.warmupIndexes().catch(() => {});
  }, 4000);
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

