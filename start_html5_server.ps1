# PowerShell Launcher for LUCKY SPIN Standalone HTML5 Server
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  LUCKY SPIN - Standalone HTML5 Playable Demonstration" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Serving Build/HTML5_Standalone/ at http://localhost:8080/index.html ..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop the web server." -ForegroundColor Yellow
Write-Host ""

Start-Process "http://localhost:8080/index.html"
python -m http.server 8080 --directory "Build\HTML5_Standalone"
