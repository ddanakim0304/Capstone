const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
    startTracking: (manualMode) => ipcRenderer.send('start-tracking', manualMode),
    stopTracking: () => ipcRenderer.send('stop-tracking'),
    onAppDetected: (callback) => ipcRenderer.on('app-detected', (event, app) => callback(app)),
    saveSession: (session) => ipcRenderer.invoke('save-session', session),
    getSessions: () => ipcRenderer.invoke('get-sessions'),
    exportExcel: (sessions) => ipcRenderer.invoke('export-excel', sessions),
});