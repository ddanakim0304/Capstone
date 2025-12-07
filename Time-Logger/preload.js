const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
    startTracking: (manualMode) => ipcRenderer.send('start-tracking', manualMode),
    stopTracking: () => ipcRenderer.send('stop-tracking'),
    onAppDetected: (callback) => ipcRenderer.on('app-detected', (event, app) => callback(app)),
    onAppUndetected: (callback) => ipcRenderer.on('app-undetected', () => callback()),
    saveSession: (session) => ipcRenderer.invoke('save-session', session),
    getSessions: () => ipcRenderer.invoke('get-sessions'),
});