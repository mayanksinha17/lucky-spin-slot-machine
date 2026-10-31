import http.server
import socketserver
import threading
import subprocess
import time
import os
import sys

PORT = 8085
webgl_dir = r"C:\Users\ASUS\Desktop\Underpin service new\Build\WebGL"

class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=webgl_dir, **kwargs)

    extensions_map = {
        '': 'application/octet-stream',
        '.html': 'text/html',
        '.css': 'text/css',
        '.js': 'application/javascript',
        '.wasm': 'application/wasm',
        '.data': 'application/octet-stream',
        '.ico': 'image/x-icon',
        '.png': 'image/png',
    }
    def log_message(self, format, *args):
        pass

def main():
    print(f"Starting WebGL HTTP server on port {PORT} serving {webgl_dir}...")
    server = socketserver.TCPServer(('127.0.0.1', PORT), Handler)
    t = threading.Thread(target=server.serve_forever)
    t.daemon = True
    t.start()

    chrome_path = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
    print(f"Launching headless Chrome with remote debugging on port 9222...")
    chrome_cmd = [
        chrome_path,
        "--headless=new",
        "--remote-debugging-port=9222",
        "--disable-gpu",
        "--no-first-run",
        "--no-default-browser-check",
        "--user-data-dir=" + os.path.join(os.environ.get("TEMP", r"C:\Temp"), "chrome_webgl_test_profile"),
        "about:blank"
    ]

    chrome_proc = subprocess.Popen(chrome_cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    time.sleep(2)

    try:
        print("\n--- PHASE 1: Loading & WASM Instantiation Test ---")
        node_res1 = subprocess.run(["node", "scripts/test_webgl_browser.js"], capture_output=True, text=True)
        print(node_res1.stdout)
        if node_res1.stderr:
            print("Errors:", node_res1.stderr)

        print("\n--- PHASE 2: Gameplay Input & Spin Cycle Test ---")
        node_res2 = subprocess.run(["node", "scripts/test_webgl_interaction.js"], capture_output=True, text=True)
        print(node_res2.stdout)
        if node_res2.stderr:
            print("Errors:", node_res2.stderr)
    finally:
        print("Cleaning up Chrome and HTTP server...")
        chrome_proc.terminate()
        server.shutdown()
        print("Test runner finished.")

if __name__ == "__main__":
    main()
