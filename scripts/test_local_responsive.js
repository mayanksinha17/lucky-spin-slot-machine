const fs = require('fs');

async function testResolution(width, height, outputFilename) {
    console.log(`\n=== Testing local WebGL at ${width}x${height} ===`);
    
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
    };

    await new Promise((resolve) => { ws.onopen = resolve; });

    await send("Page.enable");
    await send("Runtime.enable");
    await send("Emulation.setDeviceMetricsOverride", {
        width: width,
        height: height,
        deviceScaleFactor: 1,
        mobile: false
    });

    const localUrl = "http://localhost:8080/";
    console.log(`Navigating to ${localUrl}...`);
    await send("Page.navigate", { url: localUrl });

    console.log("Waiting 6 seconds for local Unity initialization...");
    for (let i = 1; i <= 6; i++) {
        await new Promise(r => setTimeout(r, 1000));
        if (i % 2 === 0) console.log(`  ... Elapsed ${i}s`);
    }

    // Inspect layout and canvas dimensions
    const metrics = await send("Runtime.evaluate", {
        expression: `
            (() => {
                const canvas = document.querySelector("#unity-canvas");
                const container = document.querySelector("#unity-container");
                const footer = document.querySelector("#unity-footer");
                const rect = canvas ? canvas.getBoundingClientRect() : null;
                const footerRect = footer ? footer.getBoundingClientRect() : null;
                return {
                    windowInnerWidth: window.innerWidth,
                    windowInnerHeight: window.innerHeight,
                    canvasRect: rect ? { top: rect.top, left: rect.left, width: rect.width, height: rect.height, bottom: rect.bottom, right: rect.right } : null,
                    footerRect: footerRect ? { top: footerRect.top, left: footerRect.left, width: footerRect.width, height: footerRect.height, bottom: footerRect.bottom } : null,
                    canvasStyleWidth: canvas ? canvas.style.width : null,
                    canvasStyleHeight: canvas ? canvas.style.height : null,
                    scrollWidth: document.documentElement.scrollWidth,
                    scrollHeight: document.documentElement.scrollHeight,
                    hasHorizontalOverflow: document.documentElement.scrollWidth > window.innerWidth,
                    hasVerticalOverflow: document.documentElement.scrollHeight > window.innerHeight
                };
            })()
        `,
        returnByValue: true
    });

    console.log("Layout Metrics:", JSON.stringify(metrics.result.value, null, 2));

    const screenshot = await send("Page.captureScreenshot", { format: "png" });
    if (screenshot && screenshot.data) {
        fs.writeFileSync(outputFilename, Buffer.from(screenshot.data, "base64"));
        console.log(`Screenshot saved to ${outputFilename}`);
    }

    ws.close();
}

async function run() {
    await testResolution(1920, 1080, "local_responsive_1920x1080.png");
    await testResolution(1366, 768, "local_responsive_1366x768.png");
    console.log("\nLocal responsive tests completed!");
}

run().catch(err => {
    console.error("Test error:", err);
    process.exit(1);
});
