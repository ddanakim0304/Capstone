const fs = require('fs');
const path = require('path');

class TimerDatabase {
  constructor() {
    // Hardcoded path to save data in the Capstone project directory
    this.statsDir = '/Users/yeinkim/Desktop/Capstone/Time-Logger/Stats';
    this.csvPath = path.join(this.statsDir, 'sessions.csv');
    console.log('[DB] Using hardcoded path:', this.csvPath);
  }

  async init() {
    // Create Stats directory if it doesn't exist
    if (!fs.existsSync(this.statsDir)) {
      fs.mkdirSync(this.statsDir, { recursive: true });
    }

    // Check if file exists, if not create it with headers
    if (!fs.existsSync(this.csvPath)) {
      const headers = 'ID,Date,Category,Total Duration,Apps Breakdown,Summary,Timestamp\n';
      fs.writeFileSync(this.csvPath, headers, 'utf8');
      console.log('[DB] Created new CSV file:', this.csvPath);
    }
  }

  // Helper: Parse CSV content into session objects
  _parseCSV(content) {
    const lines = content.trim().split('\n');
    if (lines.length <= 1) return []; // Only header or empty

    const sessions = [];
    for (let i = 1; i < lines.length; i++) {
      const values = this._parseCSVLine(lines[i]);
      if (values.length >= 7) {
        sessions.push({
          id: parseInt(values[0]) || i,
          date: values[1],
          category: values[2],
          totalDuration: values[3],
          appsBreakdown: values[4],
          summary: values[5],
          timestamp: values[6]
        });
      }
    }
    return sessions;
  }

  // Helper: Parse a single CSV line (handles quoted values with commas)
  _parseCSVLine(line) {
    const values = [];
    let current = '';
    let inQuotes = false;

    for (let i = 0; i < line.length; i++) {
      const char = line[i];
      if (char === '"') {
        inQuotes = !inQuotes;
      } else if (char === ',' && !inQuotes) {
        values.push(current.trim());
        current = '';
      } else {
        current += char;
      }
    }
    values.push(current.trim());
    return values;
  }

  // Helper: Escape value for CSV (wrap in quotes if contains comma)
  _escapeCSV(value) {
    const str = String(value || '');
    if (str.includes(',') || str.includes('"') || str.includes('\n')) {
      return `"${str.replace(/"/g, '""')}"`;
    }
    return str;
  }

  async saveSession(session) {
    // Make sure file exists
    await this.init();

    // Read current content
    let content = fs.readFileSync(this.csvPath, 'utf8');

    // FIX: Ensure file ends with newline before appending
    if (content.length > 0 && !content.endsWith('\n')) {
      fs.appendFileSync(this.csvPath, '\n', 'utf8');
      content = fs.readFileSync(this.csvPath, 'utf8');
    }

    const sessions = this._parseCSV(content);
    const newId = sessions.length + 1;

    // Create new row
    const row = [
      newId,
      this._escapeCSV(session.date),
      this._escapeCSV(session.category),
      this._escapeCSV(session.totalDuration),
      this._escapeCSV(session.appsBreakdown),
      this._escapeCSV(session.summary),
      this._escapeCSV(session.timestamp)
    ].join(',');

    // Append to file
    fs.appendFileSync(this.csvPath, row + '\n', 'utf8');
    console.log('[DB] Session saved successfully, ID:', newId);

    return {
      id: newId,
      date: session.date,
      category: session.category,
      totalDuration: session.totalDuration,
      appsBreakdown: session.appsBreakdown,
      summary: session.summary,
      timestamp: session.timestamp
    };
  }

  async getSessions() {
    // Make sure file exists
    await this.init();

    // Always read fresh from file
    const content = fs.readFileSync(this.csvPath, 'utf8');
    const sessions = this._parseCSV(content);
    return [...sessions].reverse();
  }

  getExcelPath() {
    return this.csvPath;
  }
}

module.exports = TimerDatabase;