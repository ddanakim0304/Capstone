const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const activeWin = require('active-win');
const Database = require('./database');
const WebSocket = require("ws");


let mainWindow;
let db;
let detectionInterval;
let wss;

function startWebSocketServer() {
    wss = new WebSocket.Server({ port: 31337 });

    wss.on("connection", ws => {
        console.log("[Electron] Extension connected via WebSocket");

        ws.on("message", raw => {
            try {
                const { url } = JSON.parse(raw);

                // Forward URL to renderer
                if (mainWindow) {
                    mainWindow.webContents.send("active-url", url);
                }
            } catch (e) { }
        });
    });

    console.log("[Electron] WebSocket server listening on ws://localhost:31337");
}


function createWindow() {
    mainWindow = new BrowserWindow({
        width: 600,
        height: 350,
        alwaysOnTop: true,
        frame: true,
        resizable: true,
        webPreferences: {
            preload: path.join(__dirname, 'preload.js'),
            nodeIntegration: false,
            contextIsolation: true,
        },
    });

    mainWindow.loadFile('index.html');

    // Open DevTools in development
    // mainWindow.webContents.openDevTools();
}

app.whenReady().then(async () => {
    db = new Database();
    await db.init();

    startWebSocketServer();

    createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            createWindow();
        }
    });
});


app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});


// List of browser process names to IGNORE in main.js
const BROWSER_PROCESSES = [
    'chrome',
    'google chrome',
    'brave',
    'firefox',
    'msedge',
    'naver whale',
    'safari'
];

ipcMain.on('start-tracking', (event, manualMode) => {
    // Clear existing interval if any to allow restarts
    if (detectionInterval) {
        clearInterval(detectionInterval);
        detectionInterval = null;
    }

    if (manualMode) {
        event.reply('app-detected', 'Manual Timer');
        return;
    }

    // Define the check function
    const checkApp = async () => {
        try {
            const window = await activeWin();
            if (!window) return;

            const appName = window.owner.name.toLowerCase();

            // 1. If it is a browser, DO NOTHING here. 
            // Let the extension send the data via WebSocket -> Renderer.
            if (BROWSER_PROCESSES.some(browser => appName.includes(browser))) {
                return;
            }

            // 2. Handle NATIVE apps (Unity, VS Code, etc.)
            let detectedApp = null;

            // Unity
            if (appName.includes('unity')) {
                detectedApp = 'Unity';
            }
            // VS Code / Programming
            else if (appName.includes('code') || appName.includes('visual studio code') || appName.includes('cursor')) {
                detectedApp = 'Programming';
            }
            // Antigravity
            else if (appName.includes('antigravity')) {
                detectedApp = 'Programming';
            }

            // 3. Send result if found, otherwise signal undetected
            if (detectedApp) {
                event.reply('app-detected', detectedApp);
            } else {
                event.reply('app-undetected');
            }
        } catch (error) {
            // Silently ignore detection errors
        }
    };

    // Run IMMEDIATELY
    checkApp();

    // Then run every 2 seconds
    detectionInterval = setInterval(checkApp, 2000);
});

// Stop detection
ipcMain.on('stop-tracking', () => {
    if (detectionInterval) {
        clearInterval(detectionInterval);
        detectionInterval = null;
    }
});

// Database operations
ipcMain.handle('save-session', async (event, session) => {
    return db.saveSession(session);
});

ipcMain.handle('get-sessions', async () => {
    return db.getSessions();
});