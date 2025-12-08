/**
 * Migration Script: Convert individual app names to categories
 * 
 * This script updates the existing sessions.csv file to use category names
 * instead of individual app names in the "Apps Breakdown" column.
 * 
 * Category Mapping:
 * - Programming: VS Code, Antigravity, GitHub, GitIngest
 * - LLM: Claude, AI Studio, ChatGPT
 * - Unity: Unity
 * - Blog: Medium
 * 
 * Usage: node migrate_to_categories.js
 */

const fs = require('fs');
const path = require('path');

// Category mapping from app names to categories
const APP_TO_CATEGORY = {
    'vs code': 'Programming',
    'vscode': 'Programming',
    'antigravity': 'Programming',
    'github': 'Programming',
    'gitingest': 'Programming',
    'claude': 'LLM',
    'ai studio': 'LLM',
    'chatgpt': 'LLM',
    'unity': 'Unity',
    'medium': 'Blog'
};

// Path to the CSV file
const statsDir = path.join(__dirname, 'Stats');
const csvPath = path.join(statsDir, 'sessions.csv');
const backupPath = path.join(statsDir, 'sessions_backup.csv');

/**
 * Parse a single CSV line (handles quoted values with commas)
 */
function parseCSVLine(line) {
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

/**
 * Escape value for CSV (wrap in quotes if contains comma)
 */
function escapeCSV(value) {
    const str = String(value || '');
    if (str.includes(',') || str.includes('"') || str.includes('\n')) {
        return `"${str.replace(/"/g, '""')}"`;
    }
    return str;
}

/**
 * Parse duration string like "15m 48s" to total seconds
 */
function parseDuration(durationStr) {
    let totalSeconds = 0;

    // Match hours
    const hourMatch = durationStr.match(/(\d+)h/);
    if (hourMatch) {
        totalSeconds += parseInt(hourMatch[1]) * 3600;
    }

    // Match minutes
    const minMatch = durationStr.match(/(\d+)m/);
    if (minMatch) {
        totalSeconds += parseInt(minMatch[1]) * 60;
    }

    // Match seconds
    const secMatch = durationStr.match(/(\d+)s/);
    if (secMatch) {
        totalSeconds += parseInt(secMatch[1]);
    }

    return totalSeconds;
}

/**
 * Format seconds back to duration string
 */
function formatDuration(totalSeconds) {
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    const parts = [];
    if (hours > 0) parts.push(`${hours}h`);
    if (minutes > 0) parts.push(`${minutes}m`);
    if (seconds > 0 || parts.length === 0) parts.push(`${seconds}s`);

    return parts.join(' ');
}

/**
 * Convert apps breakdown string to category-based breakdown
 * Input: "Unity: 15m 48s, VS Code: 40s, AI Studio: 58s"
 * Output: "Unity: 15m 48s, Programming: 40s, LLM: 58s"
 */
function convertToCategories(appsBreakdown) {
    if (!appsBreakdown || appsBreakdown.trim() === '') {
        return appsBreakdown;
    }

    // Parse app entries (format: "AppName: Xm Ys")
    const entries = appsBreakdown.split(',').map(e => e.trim());
    const categoryDurations = {};

    for (const entry of entries) {
        // Match "AppName: duration"
        const match = entry.match(/^(.+?):\s*(.+)$/);
        if (!match) continue;

        const appName = match[1].trim().toLowerCase();
        const duration = match[2].trim();
        const seconds = parseDuration(duration);

        // Find the category for this app
        let category = null;
        for (const [appKey, cat] of Object.entries(APP_TO_CATEGORY)) {
            if (appName.includes(appKey) || appKey.includes(appName)) {
                category = cat;
                break;
            }
        }

        // If no category found, use the original app name (capitalized)
        if (!category) {
            category = match[1].trim(); // Use original name with original casing
        }

        // Aggregate durations by category
        if (categoryDurations[category]) {
            categoryDurations[category] += seconds;
        } else {
            categoryDurations[category] = seconds;
        }
    }

    // Format back to string
    const result = Object.entries(categoryDurations)
        .map(([category, seconds]) => `${category}: ${formatDuration(seconds)}`)
        .join(', ');

    return result;
}

/**
 * Main migration function
 */
function migrate() {
    console.log('=== CSV Category Migration Script ===\n');

    // Check if CSV exists
    if (!fs.existsSync(csvPath)) {
        console.log('❌ No sessions.csv found. Nothing to migrate.');
        return;
    }

    // Read current content
    const content = fs.readFileSync(csvPath, 'utf8');
    const lines = content.trim().split('\n');

    if (lines.length <= 1) {
        console.log('ℹ️  CSV only contains headers. Nothing to migrate.');
        return;
    }

    console.log(`📁 Found ${lines.length - 1} session(s) to migrate.\n`);

    // Create backup
    fs.copyFileSync(csvPath, backupPath);
    console.log(`✅ Backup created: ${backupPath}\n`);

    // Process each line
    const newLines = [lines[0]]; // Keep header

    for (let i = 1; i < lines.length; i++) {
        const values = parseCSVLine(lines[i]);

        if (values.length >= 5) {
            const originalBreakdown = values[4];
            const newBreakdown = convertToCategories(originalBreakdown);

            console.log(`Session ${i}:`);
            console.log(`  Before: ${originalBreakdown}`);
            console.log(`  After:  ${newBreakdown}\n`);

            values[4] = newBreakdown;
        }

        // Rebuild the CSV line
        const newLine = values.map(v => escapeCSV(v)).join(',');
        newLines.push(newLine);
    }

    // Write updated content
    fs.writeFileSync(csvPath, newLines.join('\n') + '\n', 'utf8');
    console.log('✅ Migration complete! sessions.csv has been updated.');
    console.log(`📂 Original data backed up to: sessions_backup.csv`);
}

// Run migration
migrate();
