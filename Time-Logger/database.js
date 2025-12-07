
const Database = require('better-sqlite3');
const path = require('path');
const { app } = require('electron');

class TimerDatabase {
    constructor() {
        const dbPath = path.join(app.getPath('userData'), 'timer.db');
        this.db = new Database(dbPath);
        this.init();
    }

    init() {
        this.db.exec(`
      CREATE TABLE IF NOT EXISTS sessions (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        app TEXT NOT NULL,
        category TEXT NOT NULL,
        summary TEXT NOT NULL,
        duration INTEGER NOT NULL,
        date TEXT NOT NULL,
        timestamp TEXT NOT NULL
      )
    `);
    }

    saveSession(session) {
        const stmt = this.db.prepare(`
      INSERT INTO sessions (app, category, summary, duration, date, timestamp)
      VALUES (?, ?, ?, ?, ?, ?)
    `);

        const result = stmt.run(
            session.app,
            session.category,
            session.summary,
            session.duration,
            session.date,
            session.timestamp
        );

        return { id: result.lastInsertRowid, ...session };
    }

    getSessions() {
        const stmt = this.db.prepare('SELECT * FROM sessions ORDER BY timestamp DESC');
        return stmt.all();
    }
}

module.exports = TimerDatabase;