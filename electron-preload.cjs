const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  systemPower: (type) => ipcRenderer.send('system-power', type),
  runAutomation: (type, params) => ipcRenderer.send('run-automation', { type, params }),
  mediaControl: (action) => ipcRenderer.send('media-control', action),

  // Detecção do Brave (apenas info/autoteste — o app NUNCA abre janela externa)
  braveStatus: () => ipcRenderer.invoke('brave-status'),

  // ---- Brave Shields emulado (bloqueio de anúncios/rastreadores) ----
  shieldsStatus: () => ipcRenderer.invoke('shields-status'),
  shieldsSetEnabled: (enabled) => ipcRenderer.send('shields-set-enabled', enabled),
  onShieldsBlocked: (callback) => {
    if (typeof callback !== 'function') return;
    ipcRenderer.on('shields-blocked', (event, total) => callback(total));
  },
  processControl: (action, target) => ipcRenderer.send('process-control', { action, target }),
  getRunningProcesses: () => ipcRenderer.invoke('get-running-processes'),
  getSystemMetrics: () => ipcRenderer.invoke('get-system-metrics'),
  setAutostart: (enable) => ipcRenderer.invoke('set-autostart', enable),
  getAutostart: () => ipcRenderer.invoke('get-autostart'),

  // ---- Motor de voz local (Whisper offline) ----
  sttTranscribe: (payload) => ipcRenderer.invoke('stt-transcribe', payload),
  sttWarmup: (model) => ipcRenderer.send('stt-warmup', model),
  getSttStatus: () => ipcRenderer.invoke('stt-status'),
  onSttStatus: (callback) => {
    if (typeof callback !== 'function') return;
    ipcRenderer.on('stt-status', (event, status) => callback(status));
  },

  // ---- Voz neural Microsoft Edge (TTS sem chave) ----
  synthesizeSpeech: (options) => ipcRenderer.invoke('tts-synthesize', options),
  listNeuralVoices: () => ipcRenderer.invoke('tts-voices'),

  // ---- Controle de aplicativos, documentos e projetos ----
  pcControl: (action, query) => ipcRenderer.invoke('pc-control', { action, query }),

  // ---- Autoteste: captura a janela para conferência visual ----
  saveScreenshot: (name) => ipcRenderer.invoke('save-screenshot', name),

  // ---- Memória persistente em disco (sobrevive a reinícios/limpeza do localStorage) ----
  memoryLoadSync: () => {
    try { return ipcRenderer.sendSync('memory-load-sync'); } catch { return null; }
  },
  memorySave: (bundle) => ipcRenderer.invoke('memory-save', bundle),
  memorySaveSync: (bundle) => {
    try { return ipcRenderer.sendSync('memory-save-sync', bundle); } catch { return { ok: false }; }
  },

  // ---- User-Agent padrão Chrome (login Google/YouTube recusa navegadores embutidos) ----
  chromeUserAgent: (() => {
    try {
      const ua = navigator.userAgent;
      const platform = (ua.match(/\(([^)]+)\)/) || [])[1] || 'Windows NT 10.0; Win64; x64';
      const webkit = (ua.match(/AppleWebKit\/[\d.]+/) || ['AppleWebKit/537.36'])[0];
      const chrome = (ua.match(/Chrome\/[\d.]+/) || ['Chrome/131.0.0.0'])[0];
      const safari = (ua.match(/Safari\/[\d.]+/) || ['Safari/537.36'])[0];
      return `Mozilla/5.0 (${platform}) ${webkit} (KHTML, like Gecko) ${chrome} ${safari}`;
    } catch { return ''; }
  })(),

  isElectron: true
});
