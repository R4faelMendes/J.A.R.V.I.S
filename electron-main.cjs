// Electron Main Process for J.A.R.V.I.S. Desktop Application
const { app, BrowserWindow, ipcMain, shell } = require('electron');
const path = require('path');
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
    }
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

