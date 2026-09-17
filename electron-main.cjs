// Electron Main Process for J.A.R.V.I.S. Desktop Application
const { app, BrowserWindow, ipcMain, shell } = require('electron');
const path = require('path');
const os = require('os');
const { exec } = require('child_process');

let mainWindow;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1024,
    minHeight: 700,
    backgroundColor: '#02060f',
    frame: true,
    title: 'J.A.R.V.I.S. // Painel Holográfico Tático v4.8',
    autoHideMenuBar: true,
    webPreferences: {
      preload: path.join(__dirname, 'electron-preload.cjs'),
      nodeIntegration: false,
      contextIsolation: true,
      webSecurity: false, // Permite carregar iframes e webviews de serviços externos como YouTube Music
      allowRunningInsecureContent: true
    }
  });

  // Interceptar requisições para remover restrições de X-Frame-Options do YouTube Music
  mainWindow.webContents.session.webRequest.onHeadersReceived({ urls: ['*://*/*'] }, (details, callback) => {
    const responseHeaders = { ...details.responseHeaders };
    delete responseHeaders['x-frame-options'];
    delete responseHeaders['X-Frame-Options'];
    delete responseHeaders['content-security-policy'];
    delete responseHeaders['Content-Security-Policy'];
    callback({ cancel: false, responseHeaders });
  });

  // Conceder permissão automática para microfone e gravação de áudio
  mainWindow.webContents.session.setPermissionRequestHandler((webContents, permission, callback) => {
    if (permission === 'media' || permission === 'microphone' || permission === 'audioCapture') {
      callback(true); // Autorizar microfone sempre
      return;
    }
    callback(true);
  });

  const indexPath = path.join(__dirname, 'dist', 'index.html');
  mainWindow.loadFile(indexPath);

  // Set Windows start-up registry integration
  try {
    app.setLoginItemSettings({
      openAtLogin: true,
      path: process.execPath
    });
  } catch (err) {
    console.warn('Login item settings error:', err);
  }
}

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

// Open Dedicated Embedded YouTube Music Window
let ytMusicWindow = null;
ipcMain.on('open-youtube-window', () => {
  if (ytMusicWindow && !ytMusicWindow.isDestroyed()) {
    ytMusicWindow.focus();
    return;
  }

  ytMusicWindow = new BrowserWindow({
    width: 1050,
    height: 720,
    title: 'J.A.R.V.I.S. // YouTube Music Terminal',
    backgroundColor: '#030712',
    autoHideMenuBar: true,
    parent: mainWindow,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true
    }
  });

  ytMusicWindow.loadURL('https://music.youtube.com');
  ytMusicWindow.on('closed', () => {
    ytMusicWindow = null;
  });
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
  try {
    app.setLoginItemSettings({
      openAtLogin: enable,
      path: process.execPath
    });
    return true;
  } catch (err) {
    console.error('Error setting login item:', err);
    return false;
  }
});

ipcMain.handle('get-autostart', () => {
  try {
    const settings = app.getLoginItemSettings();
    return settings.openAtLogin;
  } catch {
    return false;
  }
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

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

