const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  systemPower: (type) => ipcRenderer.send('system-power', type),
  runAutomation: (type, params) => ipcRenderer.send('run-automation', { type, params }),
  mediaControl: (action) => ipcRenderer.send('media-control', action),
  openYouTubeWindow: () => ipcRenderer.send('open-youtube-window'),
  processControl: (action, target) => ipcRenderer.send('process-control', { action, target }),
  getRunningProcesses: () => ipcRenderer.invoke('get-running-processes'),
  getSystemMetrics: () => ipcRenderer.invoke('get-system-metrics'),
  setAutostart: (enable) => ipcRenderer.invoke('set-autostart', enable),
  getAutostart: () => ipcRenderer.invoke('get-autostart'),
  isElectron: true
});

