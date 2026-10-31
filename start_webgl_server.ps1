# PowerShell Launcher for LUCKY SPIN WebGL Local Server
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  LUCKY SPIN - Ultimate Slot Machine WebGL / HTML5 Server" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Serving Build/WebGL/ at http://localhost:8080/index.html ..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop the web server." -ForegroundColor Yellow
Write-Host ""

Start-Process "http://localhost:8080/index.html"
python -m http.server 8080 --directory "Build\WebGL"
