// State management
let state = {
    isTracking: false,
    currentApp: 'Idle',
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
    { value: 'work', label: 'Work' },
    { value: 'study', label: 'Study' },
    { value: 'coding', label: 'Coding' },
    { value: 'design', label: 'Design' },
    { value: 'meeting', label: 'Meeting' },
    { value: 'break', label: 'Break' },
];

// Initialize
async function init() {
    state.sessions = await window.electronAPI.getSessions();

    // Listen for app detection
    window.electronAPI.onAppDetected((app) => {
        state.currentApp = app;
        render();
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
    state.elapsedTime = 0;
    state.currentApp = state.manualMode ? 'Manual Timer' : 'Detecting...';

    window.electronAPI.startTracking(state.manualMode);

    state.timer = setInterval(() => {
        state.elapsedTime++;
        render();
    }, 1000);

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
    state.elapsedTime = 0;
    render();
}

async function exportExcel() {
    const result = await window.electronAPI.exportExcel(state.sessions);
    if (result.success) {
        alert(`Exported to: ${result.path}`);
    } else {
        alert('Export failed');
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
    <div style="min-height: 100vh; background: white; display: flex; align-items: center; justify-content: center; padding: 20px;">
      <div style="width: 100%; max-width: 400px;">
        <div style="border: 2px solid black; background: white;">
          <div style="padding: 40px; text-align: center; border-bottom: 2px solid black;">
            <div style="font-size: 48px; font-weight: bold; margin-bottom: 10px; font-family: 'Courier New', monospace;">
              ${formatTime(state.elapsedTime)}
            </div>
            ${state.isTracking ? `
              <div style="font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                <div style="width: 8px; height: 8px; background: black; border-radius: 50%; animation: pulse 1.5s infinite;"></div>
                ${state.currentApp}
              </div>
            ` : `
              <div style="font-size: 14px; color: #666;">Ready</div>
            `}
          </div>
          
          <div style="padding: 20px;">
            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 15px;">
              <input 
                type="checkbox" 
                id="manual" 
                ${state.manualMode ? 'checked' : ''}
                ${state.isTracking ? 'disabled' : ''}
                style="width: 16px; height: 16px; border: 2px solid black;"
              />
              <label for="manual" style="font-size: 12px; cursor: pointer;">
                Manual mode (ignore app detection)
              </label>
            </div>
            
            <button id="toggleBtn" style="width: 100%; background: black; color: white; padding: 16px; font-weight: bold; border: none; margin-bottom: 10px;">
              ${state.isTracking ? '⏹ STOP' : '▶ START'}
            </button>
            
            <button id="statsBtn" style="width: 100%; border: 2px solid black; background: white; padding: 12px; font-weight: bold;">
              📊 STATS
            </button>
          </div>
        </div>
        
        <div style="margin-top: 20px; text-align: center; font-size: 11px; color: #666;">
          Tracking: Unity • Chrome (AI Studio, ChatGPT) • VS Code
        </div>
      </div>
    </div>
    
    <style>
      @keyframes pulse {
        0%, 100% { opacity: 1; }
        50% { opacity: 0.3; }
      }
    </style>
  `;
}

function renderSummary() {
    return `
    <div style="min-height: 100vh; background: white; display: flex; align-items: center; justify-content: center; padding: 20px;">
      <div style="width: 100%; max-width: 500px; border: 2px solid black; padding: 30px; background: white;">
        <div style="text-align: center; margin-bottom: 30px;">
          <div style="display: inline-block; border: 2px solid black; padding: 12px 24px;">
            <span style="font-weight: bold;">${state.currentApp}</span>
            <span style="margin: 0 10px;">•</span>
            <span style="font-weight: bold;">${formatTime(state.elapsedTime)}</span>
          </div>
        </div>
        
        <div style="margin-bottom: 20px;">
          <label style="display: block; font-size: 11px; font-weight: bold; margin-bottom: 8px;">CATEGORY</label>
          <select id="category" style="width: 100%; padding: 12px; border: 2px solid black; font-size: 14px;">
            <option value="">Select...</option>
            ${categories.map(cat => `<option value="${cat.value}" ${state.category === cat.value ? 'selected' : ''}>${cat.label}</option>`).join('')}
          </select>
        </div>
        
        <div style="margin-bottom: 20px;">
          <label style="display: block; font-size: 11px; font-weight: bold; margin-bottom: 8px;">SUMMARY</label>
          <textarea 
            id="summary" 
            placeholder="What did you accomplish?"
            style="width: 100%; padding: 12px; border: 2px solid black; resize: none; font-size: 14px;"
            rows="4"
          >${state.summary}</textarea>
        </div>
        
        <div style="display: flex; gap: 10px;">
          <button id="saveBtn" style="flex: 1; background: black; color: white; padding: 16px; font-weight: bold; border: none;" ${!state.category || !state.summary ? 'disabled' : ''}>
            💾 SAVE
          </button>
          <button id="cancelBtn" style="padding: 16px 20px; border: 2px solid black; background: white;">
            ✕
          </button>
        </div>
      </div>
    </div>
  `;
}

function renderStats() {
    const totalTime = state.sessions.reduce((acc, s) => {
        const parts = s.duration.split(':');
        return acc + parseInt(parts[0]) * 3600 + parseInt(parts[1]) * 60 + parseInt(parts[2]);
    }, 0);

    const categoryStats = {};
    state.sessions.forEach(s => {
        const parts = s.duration.split(':');
        const seconds = parseInt(parts[0]) * 3600 + parseInt(parts[1]) * 60 + parseInt(parts[2]);
        categoryStats[s.category] = (categoryStats[s.category] || 0) + seconds;
    });

    return `
    <div style="min-height: 100vh; background: white; padding: 30px;">
      <div style="max-width: 1000px; margin: 0 auto; border: 2px solid black; padding: 30px;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 30px; padding-bottom: 20px; border-bottom: 2px solid black;">
          <h2 style="font-size: 24px; font-weight: bold;">Statistics</h2>
          <button id="backBtn" style="padding: 10px 20px; background: black; color: white; border: none; font-weight: bold;">
            Back
          </button>
        </div>
        
        <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 20px; margin-bottom: 30px;">
          <div style="border: 2px solid black; padding: 20px;">
            <div style="font-size: 11px; margin-bottom: 5px; font-weight: bold;">SESSIONS</div>
            <div style="font-size: 32px; font-weight: bold;">${state.sessions.length}</div>
          </div>
          <div style="border: 2px solid black; padding: 20px;">
            <div style="font-size: 11px; margin-bottom: 5px; font-weight: bold;">TOTAL TIME</div>
            <div style="font-size: 24px; font-weight: bold;">${formatTime(totalTime)}</div>
          </div>
          <div style="border: 2px solid black; padding: 20px;">
            <div style="font-size: 11px; margin-bottom: 5px; font-weight: bold;">AVG SESSION</div>
            <div style="font-size: 24px; font-weight: bold;">${state.sessions.length > 0 ? formatTime(Math.floor(totalTime / state.sessions.length)) : '00:00:00'}</div>
          </div>
        </div>
        
        <button id="exportBtn" style="width: 100%; background: black; color: white; padding: 16px; font-weight: bold; border: none; margin-bottom: 30px;">
          📥 EXPORT TO EXCEL
        </button>
        
        <h3 style="font-size: 13px; font-weight: bold; margin-bottom: 15px;">RECENT SESSIONS</h3>
        <div style="max-height: 400px; overflow-y: auto;">
          ${state.sessions.slice(0, 20).map(s => `
            <div style="border: 1px solid black; padding: 15px; margin-bottom: 10px;">
              <div style="display: flex; justify-content: space-between; margin-bottom: 5px;">
                <span style="font-weight: bold; font-size: 14px;">${s.app}</span>
                <span style="font-size: 12px;">${s.duration}</span>
              </div>
              <div style="margin-bottom: 5px;">
                <span style="font-size: 11px; padding: 4px 8px; background: black; color: white;">${categories.find(c => c.value === s.category)?.label}</span>
                <span style="font-size: 11px; color: #666; margin-left: 10px;">${s.date}</span>
              </div>
              <p style="font-size: 13px; color: #333;">${s.summary}</p>
            </div>
          `).join('')}
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
        saveBtn.addEventListener('click', saveSession);
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
            render();
        });
    }
}

// Start app
init();