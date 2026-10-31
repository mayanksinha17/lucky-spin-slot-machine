const fs = require('fs');

async function run() {
    console.log("Connecting to Chrome CDP on port 9222...");
    
    // Fetch list of targets from Chrome
    let targets;
    for (let i = 0; i < 10; i++) {
        try {
            const res = await fetch("http://127.0.0.1:9222/json");
            targets = await res.json();
            if (targets && targets.length > 0) break;
        } catch (e) {
            await new Promise(r => setTimeout(r, 500));
        }
    }

    if (!targets || targets.length === 0) {
        console.error("Could not find Chrome debug targets!");
        process.exit(1);
    }

    const pageTarget = targets.find(t => t.type === "page") || targets[0];
    console.log("Found target:", pageTarget.title, pageTarget.url, pageTarget.webSocketDebuggerUrl);

    const ws = new WebSocket(pageTarget.webSocketDebuggerUrl);
    let msgId = 1;
    const callbacks = new Map();

    function send(method, params = {}) {
        return new Promise((resolve, reject) => {
            const id = msgId++;
            callbacks.set(id, { resolve, reject });
            ws.send(JSON.stringify({ id, method, params }));
        });
    }

    ws.onmessage = (event) => {
        const msg = JSON.parse(event.data);
        if (msg.id && callbacks.has(msg.id)) {
            const { resolve } = callbacks.get(msg.id);
            callbacks.delete(msg.id);
            resolve(msg.result);
        }

        // Capture console messages
        if (msg.method === "Runtime.consoleAPICalled") {
            const type = msg.params.type;
            const args = msg.params.args.map(a => a.value || JSON.stringify(a)).join(" ");
            console.log(`[BROWSER CONSOLE ${type.toUpperCase()}]`, args);
        } else if (msg.method === "Log.entryAdded") {
            const entry = msg.params.entry;
            console.log(`[BROWSER LOG ${entry.level.toUpperCase()}]`, entry.text);
        } else if (msg.method === "Network.loadingFailed") {
            console.error(`[NETWORK FAILED]`, msg.params.errorText, msg.params.requestId);
        }
    };

    await new Promise((resolve) => { ws.onopen = resolve; });
    console.log("WebSocket connected to Chrome DevTools Protocol!");

    // Enable domains
    await send("Page.enable");
    await send("Runtime.enable");
    await send("Log.enable");
    await send("Network.enable");

    // Navigate to WebGL game
    console.log("Navigating to http://127.0.0.1:8085/index.html...");
    await send("Page.navigate", { url: "http://127.0.0.1:8085/index.html" });

    // Wait for page load and WebGL initialization
    console.log("Waiting for WebAssembly compilation and Unity initialization (15 seconds)...");
    for (let s = 1; s <= 15; s++) {
        await new Promise(r => setTimeout(r, 1000));
        if (s % 3 === 0) {
            console.log(`  ... Elapsed ${s}s`);
        }
    }

    // Inspect Unity canvas and loading bar status in DOM
    const evalRes = await send("Runtime.evaluate", {
        expression: `
            (() => {
                const canvas = document.querySelector("#unity-canvas");
                const loadingBar = document.querySelector("#unity-loading-bar");
                const warningBanner = document.querySelector("#unity-warning");
                return {
                    canvasExists: !!canvas,
                    canvasWidth: canvas ? canvas.width : 0,
                    canvasHeight: canvas ? canvas.height : 0,
                    loadingBarVisible: loadingBar ? loadingBar.style.display !== "none" : false,
                    warningText: warningBanner ? warningBanner.innerText : "",
                    title: document.title
                };
            })()
        `,
        returnByValue: true
    });

    console.log("DOM State:", JSON.stringify(evalRes.result.value, null, 2));

    // Capture screenshot
    console.log("Capturing screenshot of running WebGL build...");
    const screenshot = await send("Page.captureScreenshot", { format: "png" });
    if (screenshot && screenshot.data) {
        fs.writeFileSync("webgl_browser_capture.png", Buffer.from(screenshot.data, "base64"));
        console.log("Screenshot saved to webgl_browser_capture.png!");
    }

    ws.close();
    console.log("Verification run complete!");
}

run().catch(err => {
    console.error("Test execution error:", err);
    process.exit(1);
});
