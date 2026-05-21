# aspire-watchdog.ps1 — 監控 Aspire DCP，崩潰時自動重啟
# 使用方式（Debug）  ：pwsh -File aspire-watchdog.ps1 -DashboardPassword "your-password"
# 使用方式（Release）：pwsh -File aspire-watchdog.ps1 -Configuration Release -DashboardPassword "your-password"
# 停止方式：在終端機按 Ctrl+C（會自動清理所有子程序）

param(
    [int]$CheckIntervalSeconds       = 10,   # 健康檢查間隔（秒）
    [int]$RestartDelaySeconds        = 5,    # 崩潰後等待再重啟的秒數
    [int]$StartupGraceSeconds        = 30,   # AppHost 啟動後等待 DCP (dcpctrl) 出現的秒數
    [int]$ServiceStartupGraceSeconds = 90,   # DCP 就緒後，等待所有子系統完成啟動的秒數
    [int]$HealthFailThreshold        = 3,    # Gateway 連續失敗幾次才觸發整體重啟
    [string]$DashboardPassword       = "",   # 覆蓋 ERP Gateway Dashboard 登入密碼（選填）
    [ValidateSet("Debug","Release")]
    [string]$Configuration           = "Debug",  # 建置模式：Debug（開發/Staging）或 Release（正式）
    [string]$LogFile = "$env:TEMP\aspire-watchdog.log"
)

# ── 說明：為何健康檢查用 127.0.0.2 ─────────────────────────────────────────────
# Aspire 的 IsExternal=true 會讓 DCP (dcpctrl) 同時佔用 127.0.0.1:8180（作為內部 proxy）
# 以及讓 Gateway.exe 直接綁定 0.0.0.0:8180（外部可達）
# 若用 localhost/127.0.0.1，連線會被 dcpctrl proxy 攔截且無回應 → timeout
# 改用 127.0.0.2：dcpctrl 只精確綁 127.0.0.1，不攔 127.0.0.2
# 連線因此直達 Gateway.exe 的 0.0.0.0:8180 ✅
$GatewayHealthUrl = "http://127.0.0.2:8180/health"

# ── 子系統對照表（用於顯示啟動進度）────────────────────────────────────────────
# key = 程序 CommandLine 或程序名稱比對關鍵字，value = 顯示名稱
# 注意：key 使用 .NET regex，點號需逸出為 \.
$ServiceMap = [ordered]@{
    "ERP\.Gateway"       = "gateway               (port 8180)"
    "BankCheckbook\.Web" = "bank-checkbook-create"
    "tcs\.Web"           = "tcs"
}

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$appHostPath = Join-Path $scriptDir "ERP.AppHost\ERP.AppHost.csproj"
# AppHost 的 stdout 輸出到此檔，用來抓 Aspire Dashboard 登入 URL + token
$appHostStdout = "$env:TEMP\aspire-apphost-stdout.log"

# ── 工具函式 ─────────────────────────────────────────────────────────────────

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $ts   = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts][$Level] $Message"
    Add-Content -Path $LogFile -Value $line -ErrorAction SilentlyContinue
    $color = switch ($Level) {
        "ERROR" { "Red" }; "WARN" { "Yellow" }; "OK" { "Green" }
        "DASH"  { "Cyan" }; default { "White" }
    }
    Write-Host $line -ForegroundColor $color
}

# 遞迴殺掉指定 PID 及其所有子程序（只清理本次 AppHost 的樹，不影響其他 dotnet 程序）
function Stop-ProcessTree {
    param([int]$ParentPid)
    try {
        $children = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
            Where-Object { $_.ParentProcessId -eq $ParentPid })
        foreach ($child in $children) {
            Stop-ProcessTree -ParentPid ([int]$child.ProcessId)
        }
        Stop-Process -Id $ParentPid -Force -ErrorAction SilentlyContinue
    } catch {
        Write-Log "Stop-ProcessTree($ParentPid) 發生例外: $_" "WARN"
    }
}

function Start-Aspire {
    Write-Log "▶  啟動 ERP.AppHost（dotnet run）..." "OK"
    if (-not [string]::IsNullOrEmpty($DashboardPassword)) {
        $env:Dashboard__Password = $DashboardPassword
        Write-Log "   ERP Gateway Dashboard 密碼已套用"
    }

    # ── 步驟 A：完整重建（/t:Rebuild = MSBuild Clean + Build）──
    # /t:Rebuild 會先清除 bin/obj，再從零開始建置所有 ProjectReference（BankCheckbook.Web + Gateway + ERP.Auth.Common）
    # 確保靜態資源(.js/.css)、依賴 DLL、RCL 嵌入資源全部更新，不留任何舊版 artifact。
    Write-Log "   完整重建（/t:Rebuild -c $Configuration）..." "INFO"
    $buildOutput = & dotnet build $appHostPath /t:Rebuild -c $Configuration 2>&1
    $buildExit   = $LASTEXITCODE
    if ($buildExit -ne 0) {
        Write-Log "⚠️  建置失敗（exit $buildExit），嘗試繼續啟動..." "WARN"
        # 只輸出最後 10 行錯誤訊息
        $buildOutput | Select-Object -Last 10 | ForEach-Object { Write-Log "   $_" "WARN" }
    } else {
        Write-Log "   ✅ 建置成功" "OK"
    }

    # 清除舊的 stdout log，以便抓取本次 Aspire Dashboard 登入 token
    if (Test-Path $appHostStdout) { Remove-Item $appHostStdout -Force -ErrorAction SilentlyContinue }

    # ── 步驟 B：執行（--no-build 跳過重複建置，直接用剛才的成果）──
    $proc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--no-build", "--configuration", $Configuration, "--project", $appHostPath `
        -WorkingDirectory $scriptDir `
        -PassThru `
        -NoNewWindow `
        -RedirectStandardOutput $appHostStdout `
        -RedirectStandardError  "$env:TEMP\aspire-apphost-stderr.log"

    Write-Log "   AppHost PID: $($proc.Id)"
    return $proc
}

function Get-IsDcpRunning {
    $dcp = Get-Process -Name "dcpctrl" -ErrorAction SilentlyContinue
    return ($null -ne $dcp -and $dcp.Count -gt 0)
}

# 在 ServiceStartupGraceSeconds 期間，每 10s 輪詢子系統啟動狀態
# 同時偵測 Aspire Dashboard 登入 URL 並顯示
function Watch-ServiceStartup {
    $svcCount = $ServiceMap.Count
    Write-Log "⏳  等待 $svcCount 個服務完成啟動（最多 ${ServiceStartupGraceSeconds}s）..."

    $reported    = @{}   # 已回報的服務 key
    $dashShown   = $false
    $elapsed     = 0
    $pollInterval = 10

    while ($elapsed -lt $ServiceStartupGraceSeconds) {
        Start-Sleep -Seconds $pollInterval
        $elapsed += $pollInterval

        # ── 嘗試從 AppHost stdout 取得 Aspire Dashboard 登入 URL ──
        if (-not $dashShown -and (Test-Path $appHostStdout)) {
            try {
                $lines = Get-Content $appHostStdout -Encoding UTF8 -ErrorAction SilentlyContinue
                # Aspire 8.x 典型格式："Login to the dashboard at http://...?t=TOKEN"
                $tokenLine = $lines | Where-Object { $_ -match "login\?t=" } | Select-Object -Last 1
                if (-not $tokenLine) {
                    # 備用：抓含 dashboard 的任何 http URL
                    $tokenLine = $lines | Where-Object {
                        $_ -match "dashboard" -and $_ -match "http://"
                    } | Select-Object -Last 1
                }
                if ($tokenLine) {
                    $dashShown = $true
                    $url = if ($tokenLine -match "(https?://\S+)") { $matches[1] } else { $tokenLine.Trim() }
                    Write-Log "" 
                    Write-Log "╔══════════════════════════════════════════════════════╗" "DASH"
                    Write-Log "║  🔑  Aspire Dashboard 登入網址                      ║" "DASH"
                    Write-Log "║  $($url.PadRight(52))║" "DASH"
                    Write-Log "╚══════════════════════════════════════════════════════╝" "DASH"
                    Write-Log ""
                }
            } catch { }
        }

        # ── 偵測各子系統程序 ──
        $allProcs   = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue
        $newlyReady = @()
        foreach ($key in $ServiceMap.Keys) {
            if (-not $reported[$key]) {
                # Aspire DCP 有兩種啟動方式：
                # 1. framework-dependent：dotnet exec <Name>.dll → CommandLine 含 dll 路徑，程序名為 dotnet.exe
                # 2. self-contained exe：直接執行 <Name>.exe → 程序名為 <Name>.exe
                $namePattern = $key -replace '\\\.', '.'   # "BankCheckbook\.Web" → "BankCheckbook.Web"
                $match = $allProcs | Where-Object {
                    ($_.CommandLine -match $key) -or
                    ($_.Name -match $namePattern -and $_.Name -match "\.exe$")
                } | Select-Object -First 1
                if ($match) {
                    $reported[$key] = $true
                    $newlyReady += "   ✅ $($ServiceMap[$key])  (PID $($match.ProcessId))"
                }
            }
        }
        if ($newlyReady.Count -gt 0) {
            foreach ($line in $newlyReady) { Write-Log $line "OK" }
        }

        $readyCount   = $reported.Count
        $totalCount   = $ServiceMap.Count
        $remaining    = $ServiceStartupGraceSeconds - $elapsed
        if ($remaining -gt 0) {
            Write-Log "   進度：$readyCount/$totalCount 個服務已就緒，剩餘緩衝 ${remaining}s"
        }
    }

    # ── 最終結果 ──
    $readyCount = $reported.Count
    $totalCount = $ServiceMap.Count
    if ($readyCount -eq $totalCount) {
        Write-Log "✅  所有 $totalCount 個服務已就緒，開始健康監控" "OK"
    } else {
        $missing = $ServiceMap.Keys | Where-Object { -not $reported[$_] }
        Write-Log "⚠️  $readyCount/$totalCount 個服務就緒（以下服務未偵測到，可能仍在啟動）" "WARN"
        foreach ($m in $missing) { Write-Log "   ✗ $($ServiceMap[$m])" "WARN" }
        Write-Log "   繼續監控，若服務在監控期內恢復將自動偵測" "WARN"
    }
}

# ── 主迴圈 ────────────────────────────────────────────────────────────────────

Write-Log "══════════════════════════════════════════════" "OK"
Write-Log "  ERP Aspire Watchdog 啟動" "OK"
Write-Log "══════════════════════════════════════════════" "OK"
Write-Log "  AppHost  ：$appHostPath"
Write-Log "  建置模式 ：$Configuration"
Write-Log "  健康檢查 ：$GatewayHealthUrl"
Write-Log "  設定     ：DCP等待=${StartupGraceSeconds}s | 服務等待=${ServiceStartupGraceSeconds}s"
Write-Log "           ：健康檢查間隔=${CheckIntervalSeconds}s | 失敗門檻=${HealthFailThreshold}次 | 重啟延遲=${RestartDelaySeconds}s"
Write-Log "  記錄檔   ：$LogFile"
Write-Log "  停止方式 ：Ctrl+C（自動清理所有子程序）"
Write-Log "══════════════════════════════════════════════" "OK"

$restartCount = 0
$aspireProc   = $null

try {
while ($true) {

    # ── 步驟 1：啟動 AppHost ───────────────────────────────────────────────────
    $aspireProc = Start-Aspire

    # ── 步驟 2：等待 DCP 控制器出現 ───────────────────────────────────────────
    Write-Log "⏳  等待 DCP 控制器 (dcpctrl) 啟動（最多 ${StartupGraceSeconds}s）..."
    Start-Sleep -Seconds $StartupGraceSeconds

    if (-not (Get-IsDcpRunning)) {
        Write-Log "❌  DCP 在 ${StartupGraceSeconds}s 內未出現" "WARN"
        Write-Log "    可能原因：AppHost 編譯失敗、port 被佔用、或上次程序未完全清理" "WARN"
        Write-Log "    請手動確認：dotnet run --project ERP.AppHost\ERP.AppHost.csproj" "WARN"
        if ($null -ne $aspireProc -and -not $aspireProc.HasExited) {
            Write-Log "    清理 AppHost PID $($aspireProc.Id)..." "WARN"
            Stop-ProcessTree -ParentPid $aspireProc.Id
        }
        $restartCount++
        Write-Log "    等待 ${RestartDelaySeconds}s 後第 $restartCount 次重試..." "WARN"
        Start-Sleep -Seconds $RestartDelaySeconds
        continue
    }

    Write-Log "✅  DCP (dcpctrl) 已就緒，開始監控子系統啟動" "OK"

    # ── 步驟 3：等待子系統啟動（邊等邊顯示進度）──────────────────────────────
    Watch-ServiceStartup

    $healthFailCount = 0

    # ── 步驟 4：持續健康監控迴圈 ──────────────────────────────────────────────
    while ($true) {
        Start-Sleep -Seconds $CheckIntervalSeconds

        # 4a. DCP 存活檢查（dcpctrl 消失 = Aspire 崩潰）
        if (-not (Get-IsDcpRunning)) {
            $restartCount++
            Write-Log "❌  dcpctrl 程序消失，Aspire 已崩潰（觸發第 $restartCount 次重啟）" "ERROR"
            Write-Log "    清理 AppHost PID $($aspireProc.Id) 的完整程序樹..." "WARN"
            if ($null -ne $aspireProc) { Stop-ProcessTree -ParentPid $aspireProc.Id }
            Write-Log "    等待 ${RestartDelaySeconds}s 後重啟..." "WARN"
            Start-Sleep -Seconds $RestartDelaySeconds
            break
        }

        # 4b. Gateway /health 連續失敗檢查
        try {
            $resp = Invoke-WebRequest -Uri $GatewayHealthUrl `
                -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
            if ($resp.StatusCode -eq 200) {
                $healthFailCount = 0  # 成功，重置計數器
            } else {
                $healthFailCount++
                Write-Log "⚠️  Gateway /health 回傳 HTTP $($resp.StatusCode)（連續第 $healthFailCount/$HealthFailThreshold 次）" "WARN"
            }
        } catch {
            $healthFailCount++
            $errMsg = $_.Exception.Message.Substring(0, [math]::Min(80, $_.Exception.Message.Length))
            Write-Log "⚠️  Gateway /health 無回應（連續第 $healthFailCount/$HealthFailThreshold 次）：$errMsg" "WARN"
        }

        if ($healthFailCount -ge $HealthFailThreshold) {
            $restartCount++
            Write-Log "❌  Gateway 連續 $HealthFailThreshold 次健康檢查失敗，觸發第 $restartCount 次重啟" "ERROR"
            Write-Log "    清理 AppHost PID $($aspireProc.Id) 的完整程序樹..." "WARN"
            if ($null -ne $aspireProc) { Stop-ProcessTree -ParentPid $aspireProc.Id }
            Start-Sleep -Seconds $RestartDelaySeconds
            break
        }
    }
}

} finally {
    # Ctrl+C 或任何中斷都會進入 finally，確保清理所有子程序
    Write-Log ""
    Write-Log "══════════════════════════════════════════════" "WARN"
    Write-Log "  Watchdog 收到停止訊號，清理 Aspire 程序樹" "WARN"
    Write-Log "══════════════════════════════════════════════" "WARN"
    if ($null -ne $aspireProc) {
        Write-Log "  清理 AppHost PID $($aspireProc.Id) 及所有子程序（dcpctrl + $($ServiceMap.Count) 服務）..." "WARN"
        Stop-ProcessTree -ParentPid $aspireProc.Id
        Start-Sleep -Seconds 3
        $dcpCheck = Get-Process dcpctrl -ErrorAction SilentlyContinue
        if ($dcpCheck) {
            Write-Log "  ⚠️  dcpctrl 仍存在，嘗試直接強制停止..." "ERROR"
            foreach ($d in $dcpCheck) { Stop-Process -Id $d.Id -Force -ErrorAction SilentlyContinue }
            Write-Log "  Aspire 已停止 ✅" "OK"
        } else {
            Write-Log "  ✅ Aspire 程序樹已完全清理" "OK"
        }
    } else {
        Write-Log "  無需清理（無追蹤中的 Aspire 程序）" "OK"
    }
}
