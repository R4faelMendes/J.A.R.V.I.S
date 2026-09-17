const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  systemPower: (type) => ipcRenderer.send('system-power', type),
  runAutomation: (type, params) => ipcRenderer.send('run-automation', { type, params }),
  isElectron: true
});

