
# Capstone Time Logger

*   **`Time-Logger/`**: The main Electron application
    *   `main.js`: The backend process handling window creation, native app detection (active-win), and the WebSocket server.
    *   `renderer.js`: The frontend logic for the timer, session categorization, and statistics visualization.
    *   `preload.js`: A secure bridge exposing specific IPC channels between the main process and the UI.
    *   `database.js`: Handles reading and writing session data to the local CSV file.
    *   `index.html`: The entry point for the application's GUI.
    *   **`extension/`**: A Chrome extension for tracking browser activity
        *   `background.js`: Connects to the desktop app via WebSocket to report the active browser URL.
    *   **`Stats/sessions.csv`**: The database file storing session info