import subprocess
import time
import os
import sys

def main():
    chrome_path = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
    print("Launching headless Chrome with remote debugging on port 9222...")
    chrome_cmd = [
        chrome_path,
        "--headless=new",
        "--remote-debugging-port=9222",
        "--disable-gpu",
        "--no-first-run",
        "--no-default-browser-check",
        "--user-data-dir=" + os.path.join(os.environ.get("TEMP", r"C:\Temp"), "chrome_local_test_profile"),
        "about:blank"
    ]

    chrome_proc = subprocess.Popen(chrome_cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    time.sleep(2)

    try:
        print("Running local responsive test...")
        node_res = subprocess.run(["node", "scripts/test_local_responsive.js"], capture_output=True, text=True)
        print("--- Node Script Output ---")
        print(node_res.stdout)
        if node_res.stderr:
            print("--- Node Script Errors ---")
            print(node_res.stderr)
    finally:
        print("Cleaning up Chrome...")
        chrome_proc.terminate()
        print("Test runner finished.")

if __name__ == "__main__":
    main()
