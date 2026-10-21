/**
 * Spoonbill Electron Studio V4 - Main Process
 * Minimalist Apple / Cloudflare Edition
 */

const { app, BrowserWindow, ipcMain, shell } = require('electron');
const path = require('path');
const { spawn, execSync } = require('child_process');
const net = require('net');
const http = require('http');

// GPU & Sandbox flags to prevent Chromium crash loops alongside CUDA initialization
app.commandLine.appendSwitch('disable-gpu-process-crash-limit');
app.commandLine.appendSwitch('no-sandbox');

let mainWindow = null;
let pythonProcess = null;
let backendPort = 8765;

function findFreePort(startPort = 8765) {
  return new Promise((resolve) => {
    const server = net.createServer();
    server.listen(startPort, '127.0.0.1', () => {
      const port = server.address().port;
      server.close(() => resolve(port));
    });
    server.on('error', () => {
      resolve(findFreePort(startPort + 1));
    });
  });
}

function waitForBackendReady(port, timeoutMs = 90000) {
  const start = Date.now();
  return new Promise((resolve, reject) => {
    let resolved = false;

    const ping = () => {
      if (resolved) return;
      if (Date.now() - start > timeoutMs) {
        return reject(new Error('Timed out waiting for Python AI Backend to start (90s limit).'));
      }

      const req = http.get(`http://127.0.0.1:${port}/api/health`, (res) => {
        if (res.statusCode === 200) {
          resolved = true;
          resolve(true);
        } else {
          setTimeout(ping, 500);
        }
      });

      req.on('error', () => {
        if (!resolved) {
          setTimeout(ping, 500);
        }
      });

      req.setTimeout(1500, () => {
        req.destroy();
      });
    };

    ping();
  });
}

let backendStartupError = null;

function findPythonBinary() {
  const candidates = process.platform === 'win32'
    ? ['python', 'py', 'python3']
    : ['python3', 'python'];
  for (const cmd of candidates) {
    try {
      execSync(`${cmd} --version`, { stdio: 'ignore' });
      return cmd;
    } catch (e) {}
  }
  return null;
}

function startPythonBackend(port) {
  const scriptPath = path.join(__dirname, 'backend', 'server.py');
  const pythonCmd = findPythonBinary();

  if (!pythonCmd) {
    backendStartupError = 'Python 3 executable not found on system PATH. Please install Python 3.10+ with ONNX / PyTorch.';
    console.error(`[Electron] ${backendStartupError}`);
    return;
  }

  const env = Object.assign({}, process.env, {
    SPOONBILL_PORT: String(port),
    PYTHONIOENCODING: 'utf-8'
  });

  try {
    pythonProcess = spawn(pythonCmd, [scriptPath], {
      cwd: __dirname,
      env: env,
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true
    });
  } catch (err) {
    backendStartupError = err.message;
    console.error('[Electron] Failed to spawn Python backend:', err);
    return;
  }

  pythonProcess.stdout.on('data', (data) => {
    process.stdout.write(`[Python AI] ${data}`);
  });

  pythonProcess.stderr.on('data', (data) => {
    const str = String(data);
    process.stderr.write(`[Python AI] ${str}`);
    if (str.includes('Error') || str.includes('Traceback') || str.includes('ModuleNotFoundError')) {
      backendStartupError = str.slice(0, 250);
    }
  });

  pythonProcess.on('error', (err) => {
    backendStartupError = `Python process error: ${err.message}`;
    console.error('[Electron]', backendStartupError);
  });

  pythonProcess.on('exit', (code) => {
    console.log(`[Electron] Python backend exited with code ${code}`);
    if (code !== 0 && code !== null) {
      backendStartupError = `Python backend exited with code ${code}. Check dependencies (onnxruntime, fastapi, uvicorn, opencv-python).`;
    }
    pythonProcess = null;
  });
}

function stopPythonBackend() {
  if (!pythonProcess) return;
  try {
    if (process.platform === 'win32') {
      execSync(`taskkill /pid ${pythonProcess.pid} /T /F`, { stdio: 'ignore' });
    } else {
      pythonProcess.kill('SIGTERM');
    }
  } catch (err) {
    // Process already exited
  }
  pythonProcess = null;
}

function getSplashHtml(port) {
  return `data:text/html;charset=utf-8,` + encodeURIComponent(`
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="UTF-8">
      <title>Loading Spoonbill Studio...</title>
      <style>
        * { margin: 0; padding: 0; box-sizing: border-box; font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', sans-serif; }
        body {
          background: #121214;
          color: #E4E4E7;
          height: 100vh;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          user-select: none;
          overflow: hidden;
          -webkit-font-smoothing: antialiased;
        }
        .card {
          background: #18181B;
          border: 1px solid rgba(255,255,255,0.08);
          border-radius: 14px;
          padding: 36px 44px;
          display: flex;
          flex-direction: column;
          align-items: center;
          box-shadow: 0 20px 48px rgba(0,0,0,0.6);
          max-width: 480px;
          text-align: center;
        }
        .mark {
          width: 44px; height: 44px;
          border-radius: 10px;
          background: linear-gradient(135deg, #27272A, #18181B);
          border: 1px solid rgba(255,255,255,0.12);
          color: #F6821F;
          display: flex; align-items: center; justify-content: center;
          margin-bottom: 16px;
        }
        h1 { font-size: 18px; font-weight: 600; letter-spacing: -0.01em; margin-bottom: 4px; color: #FFFFFF; }
        .sub { font-size: 12px; color: #71717A; margin-bottom: 22px; font-weight: 500; }
        .track {
          width: 220px; height: 3px;
          background: #27272A;
          border-radius: 99px;
          overflow: hidden;
          margin-bottom: 12px;
        }
        .bar {
          width: 35%; height: 100%;
          background: #F6821F;
          border-radius: 99px;
          animation: move 1.2s infinite ease-in-out;
        }
        @keyframes move {
          0% { transform: translateX(-100%); }
          100% { transform: translateX(290%); }
        }
        .status { font-size: 11px; color: #A1A1AA; line-height: 1.5; }
        .error-box {
          margin-top: 14px;
          padding: 10px 14px;
          background: rgba(239, 68, 68, 0.1);
          border: 1px solid rgba(239, 68, 68, 0.25);
          border-radius: 8px;
          font-size: 11px;
          color: #FCA5A5;
          text-align: left;
          max-height: 100px;
          overflow-y: auto;
          display: none;
        }
      </style>
    </head>
    <body>
      <div class="card">
        <div class="mark">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 2L2 7l10 5 10-5-10-5z"/>
            <path d="M2 17l10 5 10-5"/>
            <path d="M2 12l10 5 10-5"/>
          </svg>
        </div>
        <h1>Spoonbill Studio</h1>
        <span class="sub">Platalea minor AI Census &amp; Research</span>
        <div class="track"><div class="bar"></div></div>
        <p class="status" id="status-text">Starting AI Engine (CPU Safe Mode)...</p>
        <div class="error-box" id="error-box"></div>
      </div>

      <script>
        (function autoConnect() {
          const port = ${port};
          const targetUrl = 'http://127.0.0.1:' + port + '/';
          let tries = 0;
          function check() {
            tries++;
            fetch('http://127.0.0.1:' + port + '/api/health')
              .then(res => {
                if (res.ok) {
                  const statusEl = document.getElementById('status-text');
                  if (statusEl) statusEl.textContent = 'Engine online. Opening studio...';
                  setTimeout(() => {
                    window.location.href = targetUrl;
                  }, 250);
                } else {
                  setTimeout(check, 600);
                }
              })
              .catch(() => {
                const statusEl = document.getElementById('status-text');
                if (statusEl && tries > 12) {
                  statusEl.textContent = 'Connecting to backend engine (' + tries + 's)...';
                }
                if (statusEl && tries > 30) {
                  statusEl.innerHTML = 'Still initializing backend...<br><span style="font-size:10px;color:#71717A">Verifying Python &amp; model dependencies</span>';
                }
                setTimeout(check, 600);
              });
          }
          setTimeout(check, 600);
        })();
      </script>
    </body>
    </html>
  `);
}

async function createMainWindow() {
  backendPort = await findFreePort(8765);
  startPythonBackend(backendPort);

  mainWindow = new BrowserWindow({
    width: 1400,
    height: 880,
    minWidth: 1040,
    minHeight: 680,
    backgroundColor: '#121214',
    title: 'Spoonbill Studio — Photoshop Warehouse Edition',
    autoHideMenuBar: true,
    titleBarStyle: 'hidden',
    titleBarOverlay: {
      color: '#18181B',
      symbolColor: '#E4E4E7',
      height: 42
    },
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  mainWindow.loadURL(getSplashHtml(backendPort));

  try {
    await waitForBackendReady(backendPort);
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.loadURL(`http://127.0.0.1:${backendPort}/`);
    }
  } catch (err) {
    console.warn('[Electron Startup]', err.message);
    // Splash page client-side poller will seamlessly redirect the moment FastAPI responds
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

ipcMain.handle('electron:open-export-folder', async (_event, folderPath) => {
  const target = folderPath || path.join(__dirname, 'exports');
  await shell.openPath(target);
  return { opened: target };
});

ipcMain.handle('electron:get-app-info', async () => {
  return {
    electronVersion: process.versions.electron,
    nodeVersion: process.versions.node,
    chromeVersion: process.versions.chrome,
    platform: process.platform,
    arch: process.arch,
    backendPort: backendPort
  };
});

ipcMain.handle('electron:set-titlebar-colors', async (_event, { bgColor, symbolColor }) => {
  if (mainWindow && !mainWindow.isDestroyed() && typeof mainWindow.setTitleBarOverlay === 'function') {
    mainWindow.setTitleBarOverlay({
      color: bgColor,
      symbolColor: symbolColor,
      height: 52
    });
  }
});

app.whenReady().then(createMainWindow);

app.on('window-all-closed', () => {
  stopPythonBackend();
  app.quit();
});

app.on('before-quit', () => {
  stopPythonBackend();
});
