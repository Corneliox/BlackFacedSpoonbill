"""
Desktop Native Application Launcher for Spoonbill AI Studio (macOS / Windows / Linux).
Wraps the FastAPI V4.1 Unified Studio server and renders a native Desktop Window
(WKWebView on macOS Apple Silicon & Intel / Edge WebView2 on Windows).
Includes:
- Safe stdout/stderr redirection for PyInstaller --windowed macOS .app bundles
- Read-only App Translocation & DMG volume resilience
- Instant inline HTML Splash Screen with live health-check polling (eliminates blank screen on M1/M2/M3/M4)
"""

import os
import sys
import io
import time
import socket
import threading
import traceback
import tempfile

# 1. Redirect stdout/stderr when running in PyInstaller --windowed (console=False) mode on macOS/Windows
def _setup_safe_stdio() -> str:
    try:
        if sys.platform == "darwin":
            log_dir = os.path.expanduser("~/Library/Application Support/SpoonbillStudio")
        elif sys.platform == "win32":
            log_dir = os.path.join(os.environ.get("LOCALAPPDATA", os.path.expanduser("~")), "SpoonbillStudio")
        else:
            log_dir = os.path.expanduser("~/.spoonbill_studio")
        os.makedirs(log_dir, exist_ok=True)
        log_path = os.path.join(log_dir, "studio_runtime.log")
    except Exception:
        log_path = os.path.join(tempfile.gettempdir(), "spoonbill_studio_runtime.log")

    if sys.stdout is None or sys.stderr is None:
        try:
            log_file = open(log_path, "a", encoding="utf-8", buffering=1)
            if sys.stdout is None:
                sys.stdout = log_file
            if sys.stderr is None:
                sys.stderr = log_file
        except Exception:
            if sys.stdout is None:
                sys.stdout = io.StringIO()
            if sys.stderr is None:
                sys.stderr = io.StringIO()
    return log_path


LOG_FILE_PATH = _setup_safe_stdio()

# 2. Explicit top-level imports so PyInstaller bundles all required dependencies for V4.1 Studio
import csv
import uuid
import base64
import zipfile
import math
import builtins
import platform
import subprocess
import shutil
import urllib.request
import cv2
import numpy as np
import onnxruntime as ort
from PIL import Image
import fastapi
from fastapi.responses import HTMLResponse, JSONResponse, FileResponse
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware
import starlette.staticfiles
import starlette.responses
import uvicorn
import webview
try:
    import multipart
    import python_multipart
except ImportError:
    pass

SERVER_STARTUP_ERROR = None


def get_bundle_root() -> str:
    if getattr(sys, "frozen", False):
        return getattr(sys, "_MEIPASS", os.path.dirname(sys.executable))
    return os.path.dirname(os.path.abspath(__file__))


def find_free_port(default_port: int = 8765) -> int:
    for port in range(default_port, default_port + 60):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            if s.connect_ex(("127.0.0.1", port)) != 0:
                return port
    return default_port


def load_asgi_app():
    """Loads V4.1 Unified Studio (backend/server.py) if bundled, otherwise falls back to App/app.py."""
    root = get_bundle_root()
    backend_dir = os.path.join(root, "backend")
    if os.path.exists(os.path.join(backend_dir, "server.py")):
        if backend_dir not in sys.path:
            sys.path.insert(0, backend_dir)
        if root not in sys.path:
            sys.path.insert(0, root)
        import server as v4_server
        return v4_server.app

    # Check sibling Spoonbill_Electron_Studio when running from source
    sibling_studio = os.path.join(os.path.dirname(root), "Spoonbill_Electron_Studio", "backend")
    if os.path.exists(os.path.join(sibling_studio, "server.py")):
        if sibling_studio not in sys.path:
            sys.path.insert(0, sibling_studio)
        import server as v4_server
        return v4_server.app

    if root not in sys.path:
        sys.path.insert(0, root)
    from app import app as fallback_app
    return fallback_app


def start_server(port: int):
    """Starts FastAPI backend in background thread with log_config=None for --windowed safety."""
    global SERVER_STARTUP_ERROR
    try:
        asgi_app = load_asgi_app()
        config = uvicorn.Config(
            app=asgi_app,
            host="127.0.0.1",
            port=port,
            log_level="warning",
            log_config=None
        )
        server = uvicorn.Server(config)
        server.run()
    except Exception as exc:
        err_trace = traceback.format_exc()
        SERVER_STARTUP_ERROR = f"{exc}\n\n{err_trace}"
        print(f"[Desktop App Error] Backend failed to start:\n{SERVER_STARTUP_ERROR}", flush=True)


class StudioBridgeApi:
    def __init__(self, port: int):
        self.port = port

    def get_startup_status(self):
        return {
            "port": self.port,
            "error": SERVER_STARTUP_ERROR,
            "log_path": LOG_FILE_PATH
        }


def get_splash_html(port: int) -> str:
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <title>Spoonbill AI Studio</title>
  <style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', sans-serif; }}
    body {{
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
    }}
    .card {{
      background: #18181B;
      border: 1px solid rgba(255,255,255,0.09);
      border-radius: 14px;
      padding: 36px 44px;
      display: flex;
      flex-direction: column;
      align-items: center;
      box-shadow: 0 20px 48px rgba(0,0,0,0.65);
      max-width: 500px;
      width: 90%;
      text-align: center;
    }}
    .mark {{
      width: 48px; height: 48px;
      border-radius: 12px;
      background: linear-gradient(135deg, #27272A, #18181B);
      border: 1px solid rgba(246,130,31,0.35);
      color: #F6821F;
      display: flex; align-items: center; justify-content: center;
      margin-bottom: 16px;
    }}
    h1 {{ font-size: 18px; font-weight: 600; letter-spacing: -0.01em; margin-bottom: 4px; color: #FFFFFF; }}
    .sub {{ font-size: 12px; color: #A1A1AA; margin-bottom: 22px; font-weight: 500; }}
    .track {{
      width: 240px; height: 4px;
      background: #27272A;
      border-radius: 99px;
      overflow: hidden;
      margin-bottom: 14px;
    }}
    .bar {{
      width: 38%; height: 100%;
      background: #F6821F;
      border-radius: 99px;
      animation: move 1.1s infinite ease-in-out;
    }}
    @keyframes move {{
      0% {{ transform: translateX(-100%); }}
      100% {{ transform: translateX(280%); }}
    }}
    .status {{ font-size: 12px; color: #D4D4D8; line-height: 1.5; }}
    .err-box {{
      display: none;
      margin-top: 16px;
      padding: 12px;
      background: rgba(239, 68, 68, 0.12);
      border: 1px solid rgba(239, 68, 68, 0.35);
      border-radius: 8px;
      color: #FCA5A5;
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
      font-size: 11px;
      text-align: left;
      max-height: 180px;
      width: 100%;
      overflow-y: auto;
      white-space: pre-wrap;
      word-break: break-word;
    }}
  </style>
</head>
<body>
  <div class="card">
    <div class="mark">
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M12 2L2 7l10 5 10-5-10-5z"/>
        <path d="M2 17l10 5 10-5"/>
        <path d="M2 12l10 5 10-5"/>
      </svg>
    </div>
    <h1>Spoonbill AI Studio</h1>
    <span class="sub">Platalea minor Instance Segmentation &amp; Census Workstation</span>
    <div class="track" id="track"><div class="bar"></div></div>
    <p class="status" id="status">Initializing Local ONNX Runtime Engine...</p>
    <div class="err-box" id="err-box"></div>
  </div>
  <script>
    (function pollReady() {{
      const port = {port};
      const targetUrl = 'http://127.0.0.1:' + port + '/';
      const statusEl = document.getElementById('status');
      const errBox = document.getElementById('err-box');
      const track = document.getElementById('track');
      let attempts = 0;

      async function checkPythonBridgeError() {{
        try {{
          if (window.pywebview && window.pywebview.api && window.pywebview.api.get_startup_status) {{
            const st = await window.pywebview.api.get_startup_status();
            if (st && st.error) {{
              track.style.display = 'none';
              statusEl.textContent = 'Engine startup error encountered:';
              errBox.style.display = 'block';
              errBox.textContent = st.error + '\\nLog: ' + (st.log_path || '');
              return true;
            }}
          }}
        }} catch (e) {{}}
        return false;
      }}

      async function ping() {{
        attempts++;
        const hasErr = await checkPythonBridgeError();
        if (hasErr) return;

        fetch('http://127.0.0.1:' + port + '/api/health', {{ cache: 'no-store' }})
          .then(res => {{
            if (res.ok) {{
              statusEl.textContent = 'Engine ready. Launching Studio UI...';
              window.location.replace(targetUrl);
            }} else {{
              setTimeout(ping, 350);
            }}
          }})
          .catch(() => {{
            if (attempts > 6) {{
              statusEl.textContent = 'Starting AI Engine & Loading ONNX Graph (' + Math.round(attempts * 0.35) + 's)...';
            }}
            setTimeout(ping, 350);
          }});
      }}

      setTimeout(ping, 250);
    }})();
  </script>
</body>
</html>"""


def main():
    port = find_free_port(8765)
    server_thread = threading.Thread(target=start_server, args=(port,), daemon=True)
    server_thread.start()

    bridge = StudioBridgeApi(port)
    window = webview.create_window(
        title="Spoonbill AI Studio v4.1 — Universal Workstation",
        html=get_splash_html(port),
        js_api=bridge,
        width=1380,
        height=880,
        min_size=(1040, 680),
        resizable=True,
        confirm_close=False,
        background_color="#121214"
    )

    webview.start(debug=False)
    sys.exit(0)


if __name__ == "__main__":
    main()
