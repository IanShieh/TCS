# export-and-push.ps1
# 將已開發完成的專案複製到獨立資料夾，並推送至 GitHub
#
# 使用情境:
#   - 在模板目錄內完成 scaffold 後，需要把專案「移出」成獨立 repo
#   - 或把任何位置的已完成專案匯出並 push
#
# 使用方式:
#   .\export-and-push.ps1 `
#       -SourceDir   "C:\projects\DingxinErpTemplate" `
#       -ExportRoot  "C:\projects" `
#       -ProjectName "SupplierCreate" `
#       -GitHubUser  "chrisbln2014" `
#       -Description "PURI01 供應廠商資料建立"
#
# 前置需求:
#   - Git 已安裝
#   - GitHub CLI (gh) 已安裝且已登入 (gh auth login)

param(
    [Parameter(Mandatory=$true)]
    [string]$SourceDir,         # 來源專案路徑（模板根目錄）

    [Parameter(Mandatory=$true)]
    [string]$ExportRoot,        # 匯出的父資料夾（例如 C:\projects）

    [Parameter(Mandatory=$true)]
    [string]$ProjectName,       # 新資料夾名稱（同時作為 Repo 名稱）

    [Parameter(Mandatory=$true)]
    [string]$GitHubUser,        # GitHub 使用者名稱或組織

    [string]$Description = "",  # Repo 說明（選填）

    [switch]$Private            # 是否設為私有 Repo（預設: public）
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-NamespacePrefix {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $segments = @([regex]::Matches($Name, '[A-Za-z0-9]+') | ForEach-Object { $_.Value })
    if (-not $segments -or $segments.Count -eq 0) {
        throw "ProjectName must contain at least one English letter or digit to derive a C# namespace."
    }

    $namespacePrefix = ($segments | ForEach-Object {
        if ($_.Length -eq 1) {
            $_.ToUpperInvariant()
        } else {
            $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1)
        }
    }) -join ''

    if ($namespacePrefix -match '^[0-9]') {
        $namespacePrefix = "_$namespacePrefix"
    }

    return $namespacePrefix
}

$exportDir = Join-Path $ExportRoot $ProjectName
$projectNamespace = ConvertTo-NamespacePrefix -Name $ProjectName

Write-Host ""
Write-Host "=== Export: $ProjectName ===" -ForegroundColor Cyan
Write-Host "  Source : $SourceDir"
Write-Host "  Export : $exportDir"
Write-Host "  Repo   : $GitHubUser/$ProjectName"
Write-Host "  Namespace Prefix : $projectNamespace"
Write-Host ""

# -- Pre-checks --
if (-not (Test-Path $SourceDir)) {
    Write-Error "Source directory not found: $SourceDir"; exit 1
}
if (-not (Get-Command "git" -ErrorAction SilentlyContinue)) {
    Write-Error "Git not found. Install: https://git-scm.com/"; exit 1
}
if (-not (Get-Command "gh" -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) not found. Install: https://cli.github.com/"; exit 1
}
if (Test-Path $exportDir) {
    Write-Error "Export directory already exists: $exportDir"; exit 1
}

# -- Step 1/9: Copy --
Write-Host "[1/9] Copying project..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $exportDir -Force | Out-Null
Copy-Item -Recurse "$SourceDir\*" $exportDir
Write-Host "  Done: $exportDir"

# Bootstrap src/tests from demo when exporting the initial template state
$rootCoreProjectPath = Join-Path $exportDir "src\DingxinErp.Core\DingxinErp.Core.csproj"
$demoCoreProjectPath = Join-Path $exportDir "demo\src\DingxinErp.Core\DingxinErp.Core.csproj"
$demoTestsPath = Join-Path $exportDir "demo\tests"
if (-not (Test-Path $rootCoreProjectPath) -and (Test-Path $demoCoreProjectPath)) {
    Copy-Item -Path (Join-Path $exportDir "demo\src\*") -Destination (Join-Path $exportDir "src") -Recurse -Force
    if (Test-Path $demoTestsPath) {
        Copy-Item -Path (Join-Path $exportDir "demo\tests\*") -Destination (Join-Path $exportDir "tests") -Recurse -Force
    }
    Write-Host "  Bootstrapped src/tests from demo sample"
}

# Normalize the root solution to the scaffolded src/tests layout when exporting
$rootSolutionPath = Join-Path $exportDir "DingxinErpTemplate.sln"
$demoSolutionPath = Join-Path $exportDir "demo\DingxinErpTemplate.sln"
if ((Test-Path $rootSolutionPath) -and (Test-Path $demoSolutionPath)) {
    $rootSolutionContent = Get-Content $rootSolutionPath -Raw -Encoding UTF8
    if ($rootSolutionContent -match 'demo\\src\\|demo\\tests\\') {
        Copy-Item -Path $demoSolutionPath -Destination $rootSolutionPath -Force
        Write-Host "  Normalized root solution to src/tests layout"
    }
}

# -- Step 2/9: Rename project files (DingxinErp → ProjectName) --
Write-Host "[2/9] Renaming project files (DingxinErp → $ProjectName)..." -ForegroundColor Yellow

# Rename DingxinErp.* directories (deepest first to avoid path conflicts)
Get-ChildItem -Path $exportDir -Recurse -Directory |
    Where-Object { $_.Name -match '^DingxinErp\.' } |
    Sort-Object { $_.FullName.Length } -Descending |
    ForEach-Object {
        $newName = $_.Name -replace '^DingxinErp\.', "$ProjectName."
        Rename-Item -Path $_.FullName -NewName $newName
        Write-Host "  Renamed dir: $($_.Name) → $newName"
    }

# Rename DingxinErp.*.csproj files
Get-ChildItem -Path $exportDir -Recurse -Filter "DingxinErp.*.csproj" | ForEach-Object {
    $newName = $_.Name -replace '^DingxinErp\.', "$ProjectName."
    Rename-Item -Path $_.FullName -NewName $newName
    Write-Host "  Renamed: $($_.Name) → $newName"
}

# Update .sln file content (project labels + file paths), then rename the file
Get-ChildItem -Path $exportDir -Filter "*.sln" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8
    $content = $content -replace 'DingxinErp\.', "$ProjectName."
    $content = $content -replace 'DingxinErpTemplate', $ProjectName
    Set-Content -Path $_.FullName -Value $content -Encoding UTF8
    # Rename the .sln file itself
    $newSlnName = $_.Name -replace 'DingxinErpTemplate', $ProjectName
    if ($newSlnName -ne $_.Name) {
        Rename-Item -Path $_.FullName -NewName $newSlnName
        Write-Host "  Renamed sln: $($_.Name) → $newSlnName"
    } else {
        Write-Host "  Updated sln: $($_.Name)"
    }
}

# Update ProjectReference paths inside csproj files
Get-ChildItem -Path $exportDir -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8
    if ($content -match 'DingxinErp') {
        $content = $content -replace 'DingxinErp\.', "$ProjectName."
        $content = $content -replace 'DingxinErpTemplate', $ProjectName
        Set-Content -Path $_.FullName -Value $content -Encoding UTF8
        Write-Host "  Updated: $($_.Name)"
    }
}

# Update namespace/using declarations in all .cs and .cshtml source files
Write-Host "  Updating namespaces in source files (.cs, .cshtml)..."
$sourceFiles = Get-ChildItem -Path $exportDir -Recurse -Include "*.cs", "*.cshtml" -ErrorAction SilentlyContinue
foreach ($file in $sourceFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    if ($content -match 'DingxinErp') {
        $content = $content -replace 'DingxinErp\.', "$projectNamespace."
        $content = $content -replace 'DingxinErpTemplate', $projectNamespace
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
    }
}
Write-Host "  Done"

# -- Step 3/9: Clean up template files --
Write-Host "[3/9] Cleaning template-specific files..." -ForegroundColor Yellow
$toRemove = @(
    ".git",              # Old git history
    "skill",             # Copilot CLI Skill (template only)
    ".vs",               # VS user settings
    ".claude",           # Claude Code local config
    ".playwright-mcp"    # Playwright MCP local config
)
foreach ($item in $toRemove) {
    $path = Join-Path $exportDir $item
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
        Write-Host "  Removed: $item"
    }
}
# Remove bin/obj recursively
Get-ChildItem $exportDir -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @("bin","obj") } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  Removed: bin/obj (recursive)"

# -- Step 4/9: Remove demo/ folder --
Write-Host "[4/9] Removing demo/ folder..." -ForegroundColor Yellow
$demoPath = Join-Path $exportDir "demo"
if (Test-Path $demoPath) {
    Remove-Item $demoPath -Recurse -Force
    Write-Host "  Removed: demo/"
} else {
    Write-Host "  Skipped: demo/ not found"
}

# -- Step 5/9: Remove _example ops-docs --
Write-Host "[5/9] Removing example ops-docs..." -ForegroundColor Yellow
$opsDocsPath = Join-Path $exportDir "ops-docs"
if (Test-Path $opsDocsPath) {
    Get-ChildItem $opsDocsPath -Directory |
        Where-Object { $_.Name -like "_example*" } |
        ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force
            Write-Host "  Removed: ops-docs/$($_.Name)"
        }
} else {
    Write-Host "  Skipped: ops-docs/ not found"
}

# -- Step 6/9: Generate project README --
Write-Host "[6/9] Generating project README..." -ForegroundColor Yellow
$readmePath = Join-Path $exportDir "README.md"
$specPath = Join-Path $exportDir "specs\spec.md"

# Try to extract project info from spec.md
$programId = $ProjectName
$programName = $Description
$mainTable = ""
$primaryKey = ""
$structureType = ""

if (Test-Path $specPath) {
    $specContent = Get-Content $specPath -Raw -Encoding UTF8
    # Extract from spec.md table format: | **field** | value |
    if ($specContent -match '\*\*program_code[^\*]*\*\*\s*\|\s*(.+?)\s*\|') { $programId = $Matches[1].Trim() }
    elseif ($specContent -match 'PURI\d+|PURP\d+|SALI\d+|MOCI\d+|INVI\d+') { $programId = $Matches[0] }
    if ($specContent -match '\*\*main_table[^\*]*\*\*\s*\|\s*(.+?)\s*\|') { $mainTable = $Matches[1].Trim() }
    elseif ($specContent -match 'PUR[A-Z]{2}|SAL[A-Z]{2}|MOC[A-Z]{2}|INV[A-Z]{2}') { $mainTable = $Matches[0] }
    if ($specContent -match '\*\*primary_key[^\*]*\*\*\s*\|\s*(.+?)\s*\|') { $primaryKey = $Matches[1].Trim() }
    if ($specContent -match '\*\*structure[^\*]*\*\*\s*\|\s*(.+?)\s*\|') { $structureType = $Matches[1].Trim() }
}

$readmeContent = @"
# $ProjectName

$Description

> Scaffolded from [DingxinErpTemplate](https://github.com/chrisbln2014/DingxinErpTemplate).

## Architecture

Clean Architecture (.NET 8):

``````
src/
+-- $ProjectName.Web/            # Presentation (MVC + API)
+-- $ProjectName.Core/           # Business Logic (Entities, DTOs, Services, Validators)
+-- $ProjectName.Infrastructure/ # Data Access (DbContext, Repositories)
tests/                           # Unit Tests
specs/                           # SDD Specification Documents
ops-docs/                        # Original ERP Documents
``````

## Quick Start

``````powershell
# 1. Clone
git clone https://github.com/$GitHubUser/$ProjectName.git
cd $ProjectName

# 2. Build
dotnet build

# 3. Run (auto InMemory DB, no database setup needed)
dotnet run --project src/$ProjectName.Web

# 4. Browse http://localhost:5000
``````

### Connect to SQL Server

Copy ``src/$ProjectName.Web/appsettings.Development.json.example`` to ``appsettings.Development.json`` and fill in your connection string.

> Supports SQL Server 2008 R2 (TLS 1.0 + ROW_NUMBER paging).

## Test

``````powershell
dotnet test
``````

## Tech Stack

| Item | Version |
|------|---------|
| .NET | 8.0 LTS |
| EF Core | 8.0 |
| FluentValidation | 11.x |
| Bootstrap | 5.3 (CDN) |
| jQuery | 3.7 (CDN) |
| xUnit + Moq | Unit Tests |
"@

Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8
Write-Host "  Generated: README.md"

# -- Step 7/9: .gitignore --
Write-Host "[7/9] Checking .gitignore..." -ForegroundColor Yellow
$gitignorePath = Join-Path $exportDir ".gitignore"
if (Test-Path $gitignorePath) {
    Write-Host "  OK: .gitignore exists"
} else {
    @"
## .NET
bin/
obj/
*.user
.vs/

## Secrets
appsettings.*.json
!appsettings.json
!appsettings.Development.json

## Test artifacts
*.png
*.jpeg
"@ | Set-Content $gitignorePath -Encoding UTF8
    Write-Host "  Created: .gitignore"
}

# -- Step 8/9: git init + commit --
Write-Host "[8/9] Git init + commit..." -ForegroundColor Yellow
Set-Location $exportDir
git init
git branch -M main
git add .
git commit -m "feat: init $ProjectName from DingxinErpTemplate"
Write-Host "  Committed"

# -- Step 9/9: GitHub repo + push --
Write-Host "[9/9] Create GitHub repo + push..." -ForegroundColor Yellow
$ghArgs = @("repo", "create", "$GitHubUser/$ProjectName")
$ghArgs += if ($Private) { "--private" } else { "--public" }
if ($Description) { $ghArgs += @("--description", $Description) }

try {
    & gh @ghArgs
    Write-Host "  Repo created: https://github.com/$GitHubUser/$ProjectName"
} catch {
    Write-Warning "  Repo may already exist, trying push..."
}

$remoteUrl = "https://github.com/$GitHubUser/$ProjectName.git"
git remote remove origin 2>$null
git remote add origin $remoteUrl
git push --force -u origin main

# -- Done --
Write-Host ""
Write-Host "=== Export Complete ===" -ForegroundColor Green
Write-Host "  GitHub : https://github.com/$GitHubUser/$ProjectName"
Write-Host "  Local  : $exportDir"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. cd $exportDir"
Write-Host "  2. Configure appsettings.Development.json"
Write-Host "  3. dotnet run --project src\$ProjectName.Web"
Write-Host "  4. Browse http://localhost:5000"
