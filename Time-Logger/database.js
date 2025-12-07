const ExcelJS = require('exceljs');
const path = require('path');
const fs = require('fs');

class TimerDatabase {
  constructor() {
    // Store Excel file in Stats folder relative to app directory
    this.statsDir = path.join(__dirname, 'Stats');
    this.excelPath = path.join(this.statsDir, 'sessions.xlsx');
    this.workbook = new ExcelJS.Workbook();
    this.sessions = [];
    this.initialized = false;
  }

  async init() {
    // Create Stats directory if it doesn't exist
    if (!fs.existsSync(this.statsDir)) {
      fs.mkdirSync(this.statsDir, { recursive: true });
    }

    try {
      // Try to load existing Excel file
      await this.workbook.xlsx.readFile(this.excelPath);
      const sheet = this.workbook.getWorksheet('Sessions');

      if (sheet) {
        // Load existing sessions from Excel
        this.sessions = [];
        sheet.eachRow((row, rowNumber) => {
          if (rowNumber === 1) return; // Skip header row

          this.sessions.push({
            id: row.getCell(1).value,
            date: row.getCell(2).value,
            category: row.getCell(3).value,
            totalDuration: row.getCell(4).value,
            appsBreakdown: row.getCell(5).value,
            summary: row.getCell(6).value,
            timestamp: row.getCell(7).value
          });
        });
      }
    } catch (error) {
      // File doesn't exist, create new workbook with headers
      const sheet = this.workbook.addWorksheet('Sessions');
      sheet.columns = [
        { header: 'ID', key: 'id', width: 8 },
        { header: 'Date', key: 'date', width: 12 },
        { header: 'Category', key: 'category', width: 20 },
        { header: 'Total Duration', key: 'totalDuration', width: 15 },
        { header: 'Apps Breakdown', key: 'appsBreakdown', width: 40 },
        { header: 'Summary', key: 'summary', width: 50 },
        { header: 'Timestamp', key: 'timestamp', width: 25 }
      ];

      // Style header row
      sheet.getRow(1).font = { bold: true };

      await this.workbook.xlsx.writeFile(this.excelPath);
    }

    this.initialized = true;
  }

  async saveSession(session) {
    if (!this.initialized) {
      await this.init();
    }

    const sheet = this.workbook.getWorksheet('Sessions');
    const newId = this.sessions.length + 1;

    const newSession = {
      id: newId,
      date: session.date,
      category: session.category,
      totalDuration: session.totalDuration,
      appsBreakdown: session.appsBreakdown,
      summary: session.summary,
      timestamp: session.timestamp
    };

    // Add row to Excel
    sheet.addRow({
      id: newId,
      date: session.date,
      category: session.category,
      totalDuration: session.totalDuration,
      appsBreakdown: session.appsBreakdown,
      summary: session.summary,
      timestamp: session.timestamp
    });

    // Auto-save to file
    try {
      await this.workbook.xlsx.writeFile(this.excelPath);
    } catch (error) {
      console.error('[DB] Error saving Excel file:', error);
    }

    // Add to in-memory cache
    this.sessions.push(newSession);

    return newSession;
  }

  async getSessions() {
    if (!this.initialized) {
      await this.init();
    }

    return [...this.sessions].reverse();
  }

  getExcelPath() {
    return this.excelPath;
  }
}

module.exports = TimerDatabase;