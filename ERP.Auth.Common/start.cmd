@echo off
cd /d "%~dp0"
where pwsh >nul 2>&1
if %ERRORLEVEL%==0 (
    pwsh -File aspire-watchdog.ps1 -DashboardPassword "erp@prod2025"
) else (
    echo [WARN] pwsh (PowerShell 7) 未安裝，改用 powershell 5.1 啟動
    powershell -ExecutionPolicy Bypass -File aspire-watchdog.ps1 -DashboardPassword "erp@prod2025"
)
