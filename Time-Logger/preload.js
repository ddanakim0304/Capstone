const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("electronAPI", {
    startTracking: (manual) => ipcRenderer.send("start-tracking", manual),
    stopTracking: () => ipcRenderer.send("stop-tracking"),
    onAppDetected: (callback) => ipcRenderer.on("app-detected", (_, d) => callback(d)),
    onAppUndetected: (callback) => ipcRenderer.on("app-undetected", callback),

    onActiveURL: (callback) => ipcRenderer.on("active-url", (_, url) => callback(url)),

    saveSession: (session) => ipcRenderer.invoke("save-session", session),
    getSessions: () => ipcRenderer.invoke("get-sessions")
});
