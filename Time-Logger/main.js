const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const activeWin = require('active-win');
const Database = require('./database');

let mainWindow;
let db;
let detectionInterval;

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
    await db.init();  // Initialize database async
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

            // Unity detection
            if (appName.includes('unity')) {
                detectedApp = 'Unity';
            }
            // VS Code detection
            else if (appName.includes('code') || appName.includes('visual studio code')) {
                detectedApp = 'VS Code';
            }
            // Antigravity detection
            else if (appName.includes('antigravity')) {
                detectedApp = 'Antigravity';
            }
            // NAVER Whale browser with specific websites (detect by tab name)
            else if (appName.includes('naver whale')) {
                if (title.includes('claude')) {
                    detectedApp = 'Claude';
                } else if (title.includes('ai studio')) {
                    detectedApp = 'AI Studio';
                } else if (title.includes('chatgpt')) {
                    detectedApp = 'ChatGPT';
                } else if (title.includes('github')) {
                    detectedApp = 'GitHub';
                } else if (title.includes('gitingest')) {
                    detectedApp = 'GitIngest';
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