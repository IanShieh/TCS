# ERP.Auth.Common - Aspire 一鍵停止腳本
# 使用方式：pwsh -File stop-aspire.ps1
# 策略：找到 dcpctrl → 往上找 AppHost（父程序）→ 遞迴殺整棵程序樹
#       只停 Aspire 相關程序，不影響其他 dotnet 程序

$ErrorActionPreference = "SilentlyContinue"

function Stop-ProcessTree {
    param([int]$ParentPid)
    $children = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.ParentProcessId -eq $ParentPid }
    foreach ($child in $children) {
        Stop-ProcessTree -ParentPid ([int]$child.ProcessId)
    }
    Stop-Process -Id $ParentPid -Force -ErrorAction SilentlyContinue
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ERP Aspire - 停止中" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# dcpctrl 是 Aspire 的 orchestrator，它的父程序就是 AppHost dotnet.exe
$dcp = Get-Process -Name "dcpctrl" -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $dcp) {
    Write-Host "dcpctrl 不在執行中，嘗試尋找 ERP.AppHost dotnet 程序..." -ForegroundColor Yellow
    # 備用：直接找 CommandLine 含 ERP.AppHost 的 dotnet 程序
    $appHostProc = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq "dotnet.exe" -and $_.CommandLine -match "ERP\.AppHost" } |
        Select-Object -First 1
    if ($appHostProc) {
        Write-Host "找到 AppHost PID $($appHostProc.ProcessId)，停止程序樹..." -ForegroundColor Yellow
        Stop-ProcessTree -ParentPid ([int]$appHostProc.ProcessId)
    } else {
        Write-Host "未找到任何 Aspire 相關程序，無需停止。" -ForegroundColor Gray
    }
} else {
    # 取 dcpctrl 的父程序 PID（= AppHost dotnet.exe）
    $dcpCim   = Get-CimInstance Win32_Process -Filter "ProcessId = $($dcp.Id)" -ErrorAction SilentlyContinue
    $parentPid = if ($dcpCim) { [int]$dcpCim.ParentProcessId } else { 0 }

    if ($parentPid -gt 0) {
        Write-Host "找到 AppHost PID $parentPid（dcpctrl 父程序），停止整棵程序樹..." -ForegroundColor Yellow
        Stop-ProcessTree -ParentPid $parentPid
    } else {
        Write-Host "直接停止 dcpctrl PID $($dcp.Id)..." -ForegroundColor Yellow
        Stop-Process -Id $dcp.Id -Force -ErrorAction SilentlyContinue
    }
}

Start-Sleep -Seconds 2

# 確認清理結果
$remaining = Get-Process -Name "dcpctrl" -ErrorAction SilentlyContinue
Write-Host ""
if ($remaining) {
    Write-Host "⚠️  dcpctrl 仍存在，強制停止..." -ForegroundColor Red
    $remaining | Stop-Process -Force -ErrorAction SilentlyContinue
} else {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ✅ Aspire 已停止（其他 dotnet 程序未受影響）" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
