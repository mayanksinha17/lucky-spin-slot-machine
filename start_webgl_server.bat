@echo off
echo ============================================================
echo   LUCKY SPIN - Ultimate Slot Machine WebGL / HTML5 Server
echo ============================================================
echo.
echo Starting local HTTP server for Build/WebGL/ at http://localhost:8080 ...
echo Opening default web browser...
echo Press Ctrl+C at any time to terminate the server.
echo.
start http://localhost:8080/index.html
python -m http.server 8080 --directory Build\WebGL
if %ERRORLEVEL% NEQ 0 (
    echo Python not found in PATH. Attempting with Node npx serve...
    npx -y serve Build\WebGL -p 8080
)
pause
