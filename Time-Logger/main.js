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

// Start app detection
ipcMain.on('start-tracking', (event, manualMode) => {
    if (manualMode) {
        event.reply('app-detected', 'Manual Timer');
        return;
    }

    detectionInterval = setInterval(async () => {
        try {
            const window = await activeWin();
            if (!window) return;

            const appName = window.owner.name.toLowerCase();
            const title = window.title.toLowerCase();

            // DEBUG: Print app name and title to see how they appear
            console.log(`[ActiveWin] App: "${window.owner.name}" | Title: "${window.title}"`);

            let detectedApp = null;

            // Unity detection → Unity category
            if (appName.includes('unity')) {
                detectedApp = 'Unity';
            }
            // VS Code detection → Programming category
            else if (appName.includes('code') || appName.includes('visual studio code')) {
                detectedApp = 'Programming';
            }
            // Antigravity detection → Programming category
            else if (appName.includes('antigravity')) {
                detectedApp = 'Programming';
            }
            // NAVER Whale browser with specific websites (detect by tab name)
            else if (appName.includes('naver whale')) {
                // LLM category
                if (title.includes('claude')) {
                    detectedApp = 'LLM';
                } else if (title.includes('ai studio')) {
                    detectedApp = 'LLM';
                } else if (title.includes('chatgpt')) {
                    detectedApp = 'LLM';
                }
                // Programming category
                else if (title.includes('github')) {
                    detectedApp = 'Programming';
                } else if (title.includes('gitingest')) {
                    detectedApp = 'Programming';
                }
                // Blog category
                else if (title.includes('medium')) {
                    detectedApp = 'Blog';
                }
            }

            if (detectedApp) {
                event.reply('app-detected', detectedApp);
            } else {
                // Send signal that no tracked app is active (for auto-stop)
                event.reply('app-undetected');
            }
        } catch (error) {
            // Silently ignore detection errors
        }
    }, 2000); // Check every 2 seconds
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