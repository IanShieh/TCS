<#
.SYNOPSIS
    安裝 gitleaks pre-commit hook，push 前自動偵測敏感資料。
.DESCRIPTION
    1. 檢查 gitleaks 是否已安裝（未安裝則提示下載方式）
    2. 建立 .git/hooks/pre-commit，在每次 commit 前執行 gitleaks
    3. 偵測到敏感資料 → 阻擋 commit 並列出問題檔案
.EXAMPLE
    powershell -File .specify/scripts/powershell/install-security-hook.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Security Hook Installer ===" -ForegroundColor Cyan
Write-Host ""

# --- 檢查 gitleaks ---
$gitleaks = Get-Command gitleaks -ErrorAction SilentlyContinue
if (-not $gitleaks) {
    Write-Host "`[!`] gitleaks 尚未安裝" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    Windows (winget):"
    Write-Host "      winget install gitleaks"
    Write-Host ""
    Write-Host "    Windows (scoop):"
    Write-Host "      scoop install gitleaks"
    Write-Host ""
    Write-Host "    macOS (brew):"
    Write-Host "      brew install gitleaks"
    Write-Host ""
    Write-Host "    或從 GitHub Releases 下載:"
    Write-Host "      https://github.com/gitleaks/gitleaks/releases"
    Write-Host ""
    Write-Host "    安裝後重新執行此腳本。" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

$version = & gitleaks version 2>&1
Write-Host "`[OK`] gitleaks 已安裝: $version" -ForegroundColor Green

# --- 找到 .git/hooks 目錄 ---
$repoRoot = git rev-parse --show-toplevel 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "`[!`] 目前不在 git repo 中" -ForegroundColor Red
    exit 1
}

$hooksDir = Join-Path $repoRoot ".git" "hooks"
if (-not (Test-Path $hooksDir)) {
    New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null
}

# --- 建立 pre-commit hook ---
$hookPath = Join-Path $hooksDir "pre-commit"

$hookContent = @'
#!/bin/sh
# gitleaks pre-commit hook — 偵測敏感資料
# 由 install-security-hook.ps1 自動產生

if command -v gitleaks >/dev/null 2>&1; then
    gitleaks protect --staged --verbose
    if [ $? -ne 0 ]; then
        echo ""
        echo "[BLOCKED] gitleaks detected sensitive data in staged files."
        echo "Fix the issues above before committing."
        echo "To skip this check (emergency only): git commit --no-verify"
        echo ""
        exit 1
    fi
else
    echo "[WARN] gitleaks not found, skipping pre-commit scan"
fi
'@

Set-Content -Path $hookPath -Value $hookContent -Encoding UTF8 -NoNewline

Write-Host "`[OK`] pre-commit hook 已安裝: $hookPath" -ForegroundColor Green
Write-Host ""
Write-Host "效果: 每次 git commit 前會自動掃描 staged 檔案" -ForegroundColor Cyan
Write-Host "  - 發現密碼/金鑰 -> 阻擋 commit" -ForegroundColor Cyan
Write-Host "  - 緊急跳過: git commit --no-verify" -ForegroundColor Yellow
Write-Host ""
