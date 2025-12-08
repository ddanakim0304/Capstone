let ws = null;
let lastUrl = null;

// Connect to Electron's WebSocket server
function connectWebSocket() {
    ws = new WebSocket("ws://localhost:31337");

    ws.onopen = () => {
        console.log("[Extension] Connected to Electron");
    };

    ws.onclose = () => {
        console.log("[Extension] Lost connection. Reconnecting...");
        setTimeout(connectWebSocket, 1500);
    };

    ws.onerror = () => {
        // Silently retry
    };
}

connectWebSocket();

// Send URL if changed
function sendUrl(url) {
    if (ws && ws.readyState === WebSocket.OPEN) {
        if (url !== lastUrl) {
            ws.send(JSON.stringify({ url }));
            lastUrl = url;
        }
    }
}

// Get active tab URL on tab update
chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (tab.active && changeInfo.url) {
        sendUrl(changeInfo.url);
    }
});

// Get active tab URL when switching tabs
chrome.tabs.onActivated.addListener(activeInfo => {
    chrome.tabs.get(activeInfo.tabId, tab => {
        if (tab && tab.url) {
            sendUrl(tab.url);
        }
    });
});

// Also poll every 2 seconds (backup for corner cases)
setInterval(() => {
    chrome.tabs.query({ active: true, currentWindow: true }, tabs => {
        if (tabs.length > 0 && tabs[0].url) {
            sendUrl(tabs[0].url);
        }
    });
}, 2000);
