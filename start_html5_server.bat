@echo off
echo ============================================================
echo   LUCKY SPIN - Standalone HTML5 Playable Demonstration
echo ============================================================
echo.
echo Serving Build/HTML5_Standalone/ at http://localhost:8080 ...
echo Opening default web browser...
echo Press Ctrl+C at any time to terminate the server.
echo.
start http://localhost:8080/index.html
python -m http.server 8080 --directory Build/HTML5_Standalone
if %ERRORLEVEL% NEQ 0 (
    echo Python not found in PATH. Attempting with Node npx serve...
    npx -y serve Build/HTML5_Standalone -p 8080
)
pause

