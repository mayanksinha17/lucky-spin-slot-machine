const fs = require('fs');

async function run() {
    console.log("Connecting to Chrome CDP on port 9222 for gameplay interaction test...");
    
    const res = await fetch("http://127.0.0.1:9222/json");
    const targets = await res.json();
    const pageTarget = targets.find(t => t.type === "page") || targets[0];

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

        if (msg.method === "Runtime.consoleAPICalled") {
            const type = msg.params.type;
            const args = msg.params.args.map(a => a.value || JSON.stringify(a)).join(" ");
            console.log(`[GAMEPLAY CONSOLE ${type.toUpperCase()}]`, args);
        }
    };

    await new Promise((resolve) => { ws.onopen = resolve; });
    console.log("Connected!");

    await send("Page.enable");
    await send("Runtime.enable");
    await send("Input.setIgnoreInputEvents", { ignore: false });

    // Focus canvas
    await send("Runtime.evaluate", {
        expression: `
            const canvas = document.querySelector("#unity-canvas");
            if (canvas) {
                canvas.focus();
                canvas.click();
            }
        `
    });

    console.log("Canvas focused. Sending Spacebar KeyDown event to initiate Spin...");
    await send("Input.dispatchKeyEvent", {
        type: "rawKeyDown",
        windowsVirtualKeyCode: 32,
        code: "Space",
        key: " ",
        text: " "
    });
    await send("Input.dispatchKeyEvent", {
        type: "keyUp",
        windowsVirtualKeyCode: 32,
        code: "Space",
        key: " "
    });

    console.log("Waiting 3.5 seconds for reels deceleration, settling, and win evaluation...");
    await new Promise(r => setTimeout(r, 3500));

    // Capture post-spin screenshot
    console.log("Capturing post-spin screenshot...");
    const screenshot = await send("Page.captureScreenshot", { format: "png" });
    if (screenshot && screenshot.data) {
        fs.writeFileSync("webgl_post_spin_capture.png", Buffer.from(screenshot.data, "base64"));
        console.log("Screenshot saved to webgl_post_spin_capture.png!");
    }

    ws.close();
    console.log("Interaction test complete!");
}

run().catch(err => {
    console.error("Interaction test error:", err);
    process.exit(1);
});
