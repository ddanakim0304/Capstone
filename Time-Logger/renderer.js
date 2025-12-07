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

// Initialize
async function init() {
  state.sessions = await window.electronAPI.getSessions();

  // Listen for app detection - resume if paused
  window.electronAPI.onAppDetected((app) => {
    state.currentApp = app;
    if (state.isPaused) {
      resumeTimer();
    }
    render();
  });

  // Listen for when user switches to untracked app - pause timer
  window.electronAPI.onAppUndetected(() => {
    if (state.isTracking && !state.manualMode && !state.isPaused) {
      pauseTimer();
    }
  });

  render();
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
  state.currentApp = 'You are not working on your Capstone!!';
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
  if (!state.category || !state.summary) return;

  const session = {
    app: state.currentApp,
    category: state.category,
    summary: state.summary,
    duration: formatTime(state.elapsedTime),
    date: new Date().toLocaleDateString(),
    timestamp: new Date().toISOString(),
  };

  const saved = await window.electronAPI.saveSession(session);
  state.sessions.unshift(saved);

  resetSession();
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

async function exportExcel() {
  const result = await window.electronAPI.exportExcel(state.sessions);
  if (result.success) {
    alert(`✓ Exported to: ${result.path}`);
  } else {
    alert('✗ Export failed');
  }
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
        <div class="timer-display">
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
              ${state.isTracking ? 'disabled' : ''}
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
  // Filter out "Detecting..." sessions from stats
  const validSessions = state.sessions.filter(s => s.app !== 'Detecting...');

  const totalTime = validSessions.reduce((acc, s) => {
    const parts = s.duration.split(':');
    return acc + parseInt(parts[0]) * 3600 + parseInt(parts[1]) * 60 + parseInt(parts[2]);
  }, 0);

  const categoryStats = {};
  validSessions.forEach(s => {
    const parts = s.duration.split(':');
    const seconds = parseInt(parts[0]) * 3600 + parseInt(parts[1]) * 60 + parseInt(parts[2]);
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
            <div class="stat-value">${validSessions.length}</div>
          </div>
          <div class="stat-box">
            <div class="stat-label">TOTAL TIME</div>
            <div class="stat-value stat-value-medium">${formatTime(totalTime)}</div>
          </div>
          <div class="stat-box">
            <div class="stat-label">AVG SESSION</div>
            <div class="stat-value stat-value-medium">${validSessions.length > 0 ? formatTime(Math.floor(totalTime / validSessions.length)) : '00:00:00'}</div>
          </div>
        </div>
        
        <button id="exportBtn" class="btn btn-primary btn-full">
          EXPORT TO EXCEL
        </button>
        
        <div class="sessions-section">
          <h3 class="section-title">RECENT SESSIONS</h3>
          <div class="sessions-list">
            ${state.sessions.length === 0 ? `
              <div class="empty-state">No sessions yet. Start tracking to see your data here!</div>
            ` : state.sessions.slice(0, 20).map(s => `
              <div class="session-item">
                <div class="session-item-header">
                  <span class="session-item-app">${s.app}</span>
                  <span class="session-item-duration">${s.duration}</span>
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
  const exportBtn = document.getElementById('exportBtn');
  const categorySelect = document.getElementById('category');
  const summaryTextarea = document.getElementById('summary');

  if (manualCheckbox) {
    manualCheckbox.addEventListener('change', (e) => {
      state.manualMode = e.target.checked;
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

  if (exportBtn) {
    exportBtn.addEventListener('click', exportExcel);
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