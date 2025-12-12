// State management
let state = {
  isTracking: false,
  isPaused: false,
  currentApp: 'Idle',
  usedApps: {},  // Track time spent on each app
  elapsedTime: 0,
  showSummary: false,
  showStats: false,
  manualMode: false,
  category: '',
  summary: '',
  sessions: [],
  timer: null,
};

const categories = [
  { value: 'gamedev', label: 'Game Dev (Unity)' },
  { value: 'audio', label: 'Audio & Sound Design' },
  { value: 'art', label: 'Art & Visuals' },
  { value: 'electronics', label: 'Electronics & Arduino' },
  { value: 'paper', label: 'Technical Paper' },
  { value: 'blog', label: 'Blog & Media' },
  { value: 'planning', label: 'Planning & Admin' },
];

// define rules for categorizing websites
function getCategoryFromUrl(url) {
  const u = url.toLowerCase();

  if (u.includes("chatgpt") || u.includes("claude") || u.includes("aistudio")) {
    return "LLM";
  }
  if (u.includes("github") || u.includes("gitingest") || u.includes("stackoverflow") || u.includes("unity")) {
    return "Programming";
  }
  if (u.includes("medium") || u.includes("dev.to") || u.includes("overleaf") || u.includes("docs.google")) {
    return "Blog";
  }
  return null;
}

// Initialize
async function init() {
  state.sessions = await window.electronAPI.getSessions();

  // 1. Handle NATIVE apps (from main.js active-win)
  window.electronAPI.onAppDetected((app) => {
    // Only update if it actually changed to avoid UI flickering
    if (state.currentApp !== app) {
      state.currentApp = app;
      if (state.isPaused) resumeTimer();
      render(); // Update UI
    }
  });

  // 2. Handle PAUSE (when focusing untracked windows)
  window.electronAPI.onAppUndetected(() => {
    if (state.isTracking && !state.manualMode && !state.isPaused) {
      pauseTimer();
    }
  });

  // 3. Handle WEBSITES (from Extension)
  window.electronAPI.onActiveURL((url) => {
    // If in manual mode, ignore website updates
    if (state.manualMode) return;

    const detectedCategory = getCategoryFromUrl(url);

    if (detectedCategory) {
      state.currentApp = detectedCategory;
      if (state.isPaused) resumeTimer();
      render();
    } else {
      pauseTimer();
    }
  });

  // === AUTO START ===
  render();
  startTracking();
}

// Utility functions
function formatTime(seconds) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = seconds % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

function startTracking() {
  state.isTracking = true;
  state.isPaused = false;
  state.elapsedTime = 0;
  state.usedApps = {};  // Reset app tracking
  state.currentApp = state.manualMode ? 'Manual Timer' : 'Detecting...';

  window.electronAPI.startTracking(state.manualMode);

  state.timer = setInterval(() => {
    if (!state.isPaused) {
      state.elapsedTime++;
      // Track time per app
      if (state.currentApp && state.currentApp !== 'Detecting...') {
        state.usedApps[state.currentApp] = (state.usedApps[state.currentApp] || 0) + 1;
      }
    }
    render();
  }, 1000);

  render();
}

function pauseTimer() {
  state.isPaused = true;
  if (state.elapsedTime > 0) {
    state.currentApp = 'WORK ON UR CAPSTONE!!';
  }
  render();
}

function resumeTimer() {
  state.isPaused = false;
  render();
}

function stopTracking() {
  state.isTracking = false;
  clearInterval(state.timer);
  window.electronAPI.stopTracking();
  state.showSummary = true;
  render();
}

async function saveSession() {
  console.log('[Renderer] saveSession called');
  console.log('[Renderer] category:', state.category, 'summary:', state.summary);

  if (!state.category || !state.summary) {
    console.log('[Renderer] Missing category or summary, returning early');
    return;
  }

  // Build apps breakdown string
  const appsBreakdown = Object.entries(state.usedApps)
    .sort((a, b) => b[1] - a[1])
    .map(([app, secs]) => `${app}: ${formatTimeCompact(secs)}`)
    .join(', ');

  const session = {
    category: state.category,
    summary: state.summary,
    totalDuration: formatTimeCompact(state.elapsedTime),
    appsBreakdown: appsBreakdown || 'Manual Timer',
    date: new Date().toLocaleDateString(),
    timestamp: new Date().toISOString(),
  };

  console.log('[Renderer] Saving session:', session);

  try {
    const saved = await window.electronAPI.saveSession(session);
    console.log('[Renderer] Session saved successfully:', saved);
    state.sessions.unshift(saved);
    resetSession();
  } catch (error) {
    console.error('[Renderer] Error saving session:', error);
  }
}

function cancelSession() {
  resetSession();
}

function resetSession() {
  state.showSummary = false;
  state.category = '';
  state.summary = '';
  state.currentApp = 'Idle';
  state.usedApps = {};
  state.elapsedTime = 0;
  render();
}

// Render functions
function render() {
  const app = document.getElementById('app');

  if (state.showStats) {
    app.innerHTML = renderStats();
  } else if (state.showSummary) {
    app.innerHTML = renderSummary();
  } else {
    app.innerHTML = renderMain();
  }

  attachEventListeners();
}

function renderMain() {
  return `
    <div class="main-container">
      <div class="timer-card">
        <div class="timer-display ${state.isPaused && state.elapsedTime > 0 ? 'paused' : ''}">
          <div class="time-digits">${formatTime(state.elapsedTime)}</div>
          ${state.isTracking ? `
            <div class="status-indicator">
              <span class="pulse-dot"></span>
              <span class="app-name">${state.currentApp}</span>
            </div>
          ` : ''}
        </div>
        
        <div class="controls">
          <label class="checkbox-container">
            <input 
              type="checkbox" 
              id="manual" 
              ${state.manualMode ? 'checked' : ''}
            />
            <span class="checkbox-label">Manual mode (ignore app detection)</span>
          </label>
          
          <button id="toggleBtn" class="btn btn-primary">
            ${state.isTracking ? '⏹ STOP' : '▶ START'}
          </button>
          
          <button id="statsBtn" class="btn btn-secondary">
            STATS
          </button>
        </div>
      </div>
    </div>
  `;
}

function formatTimeCompact(seconds) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = seconds % 60;
  if (h > 0) return `${h}h ${m}m`;
  if (m > 0) return `${m}m ${s}s`;
  return `${s}s`;
}

function renderSummary() {
  // Build apps used summary
  const appsUsedList = Object.entries(state.usedApps)
    .sort((a, b) => b[1] - a[1])  // Sort by time desc
    .map(([app, secs]) => `${app}: ${formatTimeCompact(secs)}`)
    .join(' • ');

  return `
    <div class="main-container">
      <div class="summary-card">
        <div class="summary-header">
          <div class="session-total">Total: ${formatTimeCompact(state.elapsedTime)}</div>
          <div class="session-apps">${appsUsedList || 'No apps tracked'}</div>
        </div>
        
        <div class="form-group">
          <label class="form-label">CATEGORY</label>
          <select id="category" class="form-select">
            <option value="">Select...</option>
            ${categories.map(cat => `<option value="${cat.value}" ${state.category === cat.value ? 'selected' : ''}>${cat.label}</option>`).join('')}
          </select>
        </div>
        
        <div class="form-group">
          <label class="form-label">SUMMARY</label>
          <textarea 
            id="summary" 
            class="form-textarea"
            placeholder="What did you accomplish?"
            rows="4"
          >${state.summary}</textarea>
        </div>
        
        <div class="button-group">
          <button id="saveBtn" class="btn btn-primary flex-1">
            SAVE
          </button>
          <button id="cancelBtn" class="btn btn-icon">
            ✕
          </button>
        </div>
      </div>
    </div>
  `;
}

function renderStats() {
  console.log('[Renderer] renderStats called, sessions:', state.sessions.length);

  // Helper to parse duration from various formats
  function parseDuration(s) {
    // Try new format: totalDuration (e.g., "2m 15s", "1h 30m", "45s")
    const dur = s.totalDuration || s.duration || '0s';
    let seconds = 0;

    const hourMatch = dur.match(/(\d+)h/);
    const minMatch = dur.match(/(\d+)m/);
    const secMatch = dur.match(/(\d+)s/);

    if (hourMatch) seconds += parseInt(hourMatch[1]) * 3600;
    if (minMatch) seconds += parseInt(minMatch[1]) * 60;
    if (secMatch) seconds += parseInt(secMatch[1]);

    // Fallback: try old format "HH:MM:SS"
    if (seconds === 0 && dur.includes(':')) {
      const parts = dur.split(':');
      if (parts.length === 3) {
        seconds = parseInt(parts[0]) * 3600 + parseInt(parts[1]) * 60 + parseInt(parts[2]);
      }
    }

    return seconds;
  }

  const totalTime = state.sessions.reduce((acc, s) => acc + parseDuration(s), 0);

  const categoryStats = {};
  state.sessions.forEach(s => {
    const seconds = parseDuration(s);
    categoryStats[s.category] = (categoryStats[s.category] || 0) + seconds;
  });

  return `
    <div class="stats-container">
      <div class="stats-card">
        <div class="stats-header">
          <h2 class="stats-title">Statistics</h2>
          <button id="backBtn" class="btn btn-primary">Back</button>
        </div>
        
        <div class="stats-grid">
          <div class="stat-box">
            <div class="stat-label">SESSIONS</div>
            <div class="stat-value">${state.sessions.length}</div>
          </div>
          <div class="stat-box">
            <div class="stat-label">TOTAL TIME</div>
            <div class="stat-value stat-value-medium">${formatTime(totalTime)}</div>
          </div>
          <div class="stat-box">
            <div class="stat-label">AVG SESSION</div>
            <div class="stat-value stat-value-medium">${state.sessions.length > 0 ? formatTime(Math.floor(totalTime / state.sessions.length)) : '00:00:00'}</div>
          </div>
        </div>
        
        <div class="sessions-section">
          <h3 class="section-title">RECENT SESSIONS</h3>
          <div class="sessions-list">
            ${state.sessions.length === 0 ? `
              <div class="empty-state">No sessions yet. Start tracking to see your data here!</div>
            ` : state.sessions.slice(0, 20).map(s => `
              <div class="session-item">
                <div class="session-item-header">
                  <span class="session-item-app">${s.totalDuration || s.duration || ''}</span>
                  <span class="session-item-duration">${s.appsBreakdown || s.app || ''}</span>
                </div>
                <div class="session-item-meta">
                  <span class="session-item-category">${categories.find(c => c.value === s.category)?.label || s.category}</span>
                  <span class="session-item-date">${s.date}</span>
                </div>
                <p class="session-item-summary">${s.summary}</p>
              </div>
            `).join('')}
          </div>
        </div>
      </div>
    </div>
  `;
}

function attachEventListeners() {
  const manualCheckbox = document.getElementById('manual');
  const toggleBtn = document.getElementById('toggleBtn');
  const statsBtn = document.getElementById('statsBtn');
  const saveBtn = document.getElementById('saveBtn');
  const cancelBtn = document.getElementById('cancelBtn');
  const backBtn = document.getElementById('backBtn');
  const categorySelect = document.getElementById('category');
  const summaryTextarea = document.getElementById('summary');

  if (manualCheckbox) {
    manualCheckbox.addEventListener('change', (e) => {
      state.manualMode = e.target.checked;

      // If we are currently tracking, restart the tracking process to switch modes immediately
      if (state.isTracking) {
        state.currentApp = state.manualMode ? 'Manual Timer' : 'Detecting...';
        window.electronAPI.startTracking(state.manualMode);

        // Resume timer if we were paused (e.g. from unregistered app) and switched to manual
        if (state.manualMode && state.isPaused) {
          resumeTimer();
        }

        render();
      }
    });
  }

  if (toggleBtn) {
    toggleBtn.addEventListener('click', () => {
      if (state.isTracking) {
        stopTracking();
      } else {
        startTracking();
      }
    });
  }

  if (statsBtn) {
    statsBtn.addEventListener('click', () => {
      state.showStats = true;
      render();
    });
  }

  if (saveBtn) {
    saveBtn.addEventListener('click', () => {
      const categoryEl = document.getElementById('category');
      const summaryEl = document.getElementById('summary');
      if (categoryEl) state.category = categoryEl.value;
      if (summaryEl) state.summary = summaryEl.value;

      if (!state.category || !state.summary) {
        alert('Please select a category and write a summary');
        return;
      }
      saveSession();
    });
  }

  if (cancelBtn) {
    cancelBtn.addEventListener('click', cancelSession);
  }

  if (backBtn) {
    backBtn.addEventListener('click', () => {
      state.showStats = false;
      render();
    });
  }

  if (categorySelect) {
    categorySelect.addEventListener('change', (e) => {
      state.category = e.target.value;
      render();
    });
  }

  if (summaryTextarea) {
    summaryTextarea.addEventListener('input', (e) => {
      state.summary = e.target.value;
      // Don't call render() here - it causes focus loss
    });
  }
}

// Start app
init();