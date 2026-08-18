"""
Desktop Native Application Launcher for Spoonbill Studio Universal.
Wraps the FastAPI server and renders a dedicated native Desktop Window (Edge WebView2 on Windows / WebKit on Mac).
No external browser tab required.
"""

import os
import sys
import time
import threading
import socket
import uvicorn
import webview

def find_free_port(default_port=8080):
    """Finds an available local port."""
    for port in range(default_port, default_port + 50):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            if s.connect_ex(("127.0.0.1", port)) != 0:
                return port
    return default_port

def start_server(port):
    """Starts FastAPI backend in background thread."""
    from app import app
    config = uvicorn.Config(app=app, host="127.0.0.1", port=port, log_level="warning")
    server = uvicorn.Server(config)
    server.run()

def main():
    port = find_free_port(8080)
    server_thread = threading.Thread(target=start_server, args=(port,), daemon=True)
    server_thread.start()

    # Wait briefly for server ready
    time.sleep(1.2)

    # Launch Native Desktop Window
    app_url = f"http://127.0.0.1:{port}"
    print(f"[Desktop App] Opening Native Window on {app_url}...")

    window = webview.create_window(
        title="Spoonbill AI Studio - Universal Edition",
        url=app_url,
        width=1340,
        height=880,
        min_size=(1080, 720),
        resizable=True,
        confirm_close=False,
        background_color="#0f141c"
    )

    webview.start(debug=False)
    sys.exit(0)

if __name__ == "__main__":
    main()
