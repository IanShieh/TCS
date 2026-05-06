# setup-speckit.ps1
# 團隊 spec-kit 初始化腳本
#
# 用途：
#   1. 執行官方 specify init 取得最新的 spec-kit 框架
#   2. 在官方 agents 末尾追加鼎新 ERP 專屬補充段
#   3. 還原團隊憲法（constitution）
#   4. 保留 spec-kit 新增的指令（不會被刪除）
#
# 使用方式：
#   .\.specify\scripts\powershell\setup-speckit.ps1
#
# 前置需求：
#   - Python 3.11+ 及 uv 已安裝
#   - specify-cli 已安裝 (uv tool install specify-cli --from git+https://github.com/github/spec-kit.git)

param(
    [string]$AiAgent = "copilot",       # AI agent 類型 (copilot/claude/gemini 等)
    [switch]$SkipInit,                   # 跳過 specify init（僅疊回客製化）
    [switch]$DryRun                      # 預覽模式（不實際複製）
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = (Get-Item "$PSScriptRoot\..\..\..").FullName
$customDir = Join-Path $projectRoot ".specify\customizations"
$supplementDir = Join-Path $customDir "erp-supplements"

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  鼎新 ERP 模板 — Spec-kit 初始化 + ERP 客製化" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  專案根目錄 : $projectRoot"
Write-Host "  AI Agent   : $AiAgent"
Write-Host "  ERP 補充段 : $supplementDir"
Write-Host ""

# ── 確認 ERP supplements 存在 ─────────────────────────────────
if (-not (Test-Path $supplementDir)) {
    Write-Error "[ERROR] 找不到 ERP 補充段目錄: $supplementDir"
    exit 1
}

$customMemory = Join-Path $customDir "memory"
if (-not (Test-Path $customMemory)) {
    Write-Error "[ERROR] 缺少憲法備份目錄: $customMemory"
    exit 1
}

# ── Step 1: 執行官方 specify init ──────────────────────────────
if (-not $SkipInit) {
    Write-Host "[1/3] Step 1: specify init --here --force --ai $AiAgent --script ps" -ForegroundColor Yellow

    if ($DryRun) {
        Write-Host "   [DryRun] 將執行: specify init --here --force --ai $AiAgent --script ps" -ForegroundColor DarkGray
    }
    else {
        Push-Location $projectRoot
        try {
            specify init --here --force --ai $AiAgent --script ps
            if ($LASTEXITCODE -ne 0) {
                Write-Error "[ERROR] specify init 執行失敗 (exit code: $LASTEXITCODE)"
                exit 1
            }
            Write-Host "   [OK] specify init 完成" -ForegroundColor Green
        }
        finally {
            Pop-Location
        }
    }
}
else {
    Write-Host "[SKIP] Step 1/3: 跳過 specify init (--SkipInit)" -ForegroundColor DarkGray
}

# ── Step 2: 在官方 agents 末尾追加 ERP 補充段 ─────────────────
Write-Host "[2/3] Step 2: 追加鼎新 ERP 補充段到官方 agents..." -ForegroundColor Yellow

$targetAgents = Join-Path $projectRoot ".github\agents"

if (-not (Test-Path $targetAgents)) {
    Write-Error "[ERROR] 找不到 .github\agents\ 目錄，請確認 specify init 已執行成功"
    exit 1
}

$supplementFiles = Get-ChildItem "$supplementDir\speckit.*.md" -ErrorAction SilentlyContinue
$appendCount = 0

foreach ($file in $supplementFiles) {
    # speckit.specify.md → speckit.specify.agent.md
    $agentName = $file.BaseName + ".agent.md"
    $target = Join-Path $targetAgents $agentName

    if (-not (Test-Path $target)) {
        if ($DryRun) {
            Write-Host "   [DryRun] 跳過 (無對應 agent): $agentName" -ForegroundColor DarkGray
        }
        continue
    }

    # 檢查是否已追加過（避免重複）
    $marker = "鼎新 ERP 作業轉換 — 專屬指引"
    $content = Get-Content $target -Raw -Encoding UTF8
    if ($content -match [regex]::Escape($marker)) {
        if ($DryRun) {
            Write-Host "   [DryRun] 已存在: $agentName (跳過)" -ForegroundColor DarkGray
        }
        else {
            Write-Host "   [SKIP] 已存在 ERP 補充段: $agentName" -ForegroundColor DarkGray
        }
        continue
    }

    if ($DryRun) {
        Write-Host "   [DryRun] 追加: $agentName" -ForegroundColor DarkGray
    }
    else {
        $supplement = Get-Content $file.FullName -Raw -Encoding UTF8
        Add-Content -Path $target -Value $supplement -NoNewline -Encoding UTF8
        Write-Host "   [OK] 追加: $agentName" -ForegroundColor Green
    }
    $appendCount++
}

Write-Host "   [OK] 已追加 $appendCount 個 ERP 補充段" -ForegroundColor Green

# ── Step 3: 還原 constitution ─────────────────────────────────
Write-Host "[3/3] Step 3: 還原團隊憲法 (constitution)..." -ForegroundColor Yellow

$targetMemory = Join-Path $projectRoot ".specify\memory"
$memoryFiles = Get-ChildItem "$customMemory\*.md" -ErrorAction SilentlyContinue
$memoryCount = 0

foreach ($file in $memoryFiles) {
    $target = Join-Path $targetMemory $file.Name

    if ($DryRun) {
        Write-Host "   [DryRun] 還原: .specify\memory\$($file.Name)" -ForegroundColor DarkGray
    }
    else {
        Copy-Item $file.FullName $target -Force
    }
    $memoryCount++
}

Write-Host "   [OK] 已還原 $memoryCount 個憲法檔案" -ForegroundColor Green

# ── 完成報告 ─────────────────────────────────────────────────
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "  [OK] [DryRun] 預覽完成，未實際修改任何檔案" -ForegroundColor Yellow
}
else {
    Write-Host "  [OK] 初始化完成!" -ForegroundColor Green
}

Write-Host ""
Write-Host "  你現在可以開始使用 SDD 流程：" -ForegroundColor White
Write-Host "    1. 把 ERP 截圖/文件放到 ops-docs/" -ForegroundColor White
Write-Host "    2. /speckit.specify  → 產出規格書" -ForegroundColor White
Write-Host "    3. /speckit.plan     → 產出技術計畫" -ForegroundColor White
Write-Host "    4. /speckit.tasks    → 產出任務清單" -ForegroundColor White
Write-Host "    5. /speckit.implement→ 逐步實作" -ForegroundColor White
Write-Host "    6. /speckit.checklist→ 驗收檢核" -ForegroundColor White
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
