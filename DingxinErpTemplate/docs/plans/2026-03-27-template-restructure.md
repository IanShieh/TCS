# 模板結構重組 + SDD Scaffold 機制 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 將 DingxinErpTemplate 目錄結構重組：`demo/reference/` = Sample 範例，`src/` = 空骨架，新增 `speckit.scaffold` agent 自動從 demo/ 複製到 src/。

**Architecture:** 模板 repo 維持 src/ 空骨架可 build，demo/reference/ 保存完整 Sample 範例。scaffold agent 掃描 ops-docs → 詢問代號 → 複製 demo/ → src/，然後 SDD 在根目錄執行。

**Tech Stack:** .NET 8, spec-kit (specify-cli), PowerShell, GitHub Copilot Agents

---

### Task 1: 建立 demo/reference/ 目錄結構

**Files:**
- Create: `demo/reference/src/` (copy from `src/`)
- Create: `demo/reference/tests/` (copy from `tests/`)

**Step 1: 複製 src/ 到 demo/reference/src/**

Run:
```powershell
New-Item -ItemType Directory -Path "demo/reference" -Force
Copy-Item -Path "src/*" -Destination "demo/reference/src/" -Recurse -Exclude @("bin", "obj")
Copy-Item -Path "tests/*" -Destination "demo/reference/tests/" -Recurse -Exclude @("bin", "obj")
```

**Step 2: 驗證檔案結構完整**

Run:
```powershell
Get-ChildItem -Path "demo/reference/src" -Recurse -File | Where-Object { $_.Extension -match '\.(cs|csproj|json)$' } | Select-Object FullName
```
Expected: 應包含 SampleHeader.cs, SampleDetail.cs, SampleService.cs, SampleController.cs, Index.cshtml 等所有檔案

---

### Task 2: 建立 demo/reference/ 專屬 .sln

**Files:**
- Create: `demo/reference/DingxinErpTemplate.sln`

**Step 1: 建立 .sln 檔案**

Run:
```powershell
cd demo/reference
dotnet new sln --name DingxinErpTemplate
dotnet sln add src/DingxinErp.Core/DingxinErp.Core.csproj
dotnet sln add src/DingxinErp.Infrastructure/DingxinErp.Infrastructure.csproj
dotnet sln add src/DingxinErp.Web/DingxinErp.Web.csproj
dotnet sln add tests/DingxinErp.Core.Tests/DingxinErp.Core.Tests.csproj
cd ../..
```

**Step 2: 驗證 demo/reference/ 可獨立 build**

Run:
```powershell
dotnet build demo/reference/DingxinErpTemplate.sln
```
Expected: Build succeeded. 0 Error(s)

**Step 3: Commit**

```bash
git add demo/reference/
git commit -m "feat: add demo/reference/ with full Sample example"
```

---

### Task 3: 精簡 src/ 為空骨架 — Core 層

**Files:**
- Delete: `src/DingxinErp.Core/Entities/SampleHeader.cs`
- Delete: `src/DingxinErp.Core/Entities/SampleDetail.cs`
- Delete: `src/DingxinErp.Core/DTOs/SampleHeaderDto.cs`
- Delete: `src/DingxinErp.Core/DTOs/SampleDetailDto.cs`
- Delete: `src/DingxinErp.Core/DTOs/CreateSampleHeaderRequest.cs`
- Delete: `src/DingxinErp.Core/DTOs/UpdateSampleHeaderRequest.cs`
- Delete: `src/DingxinErp.Core/DTOs/MappingExtensions.cs`
- Delete: `src/DingxinErp.Core/Services/SampleService.cs`
- Delete: `src/DingxinErp.Core/Validators/CreateSampleHeaderValidator.cs`
- Delete: `src/DingxinErp.Core/Validators/UpdateSampleHeaderValidator.cs`
- Delete: `src/DingxinErp.Core/Interfaces/ISampleRepository.cs`
- Delete: `src/DingxinErp.Core/Interfaces/ISampleService.cs`
- Keep: `src/DingxinErp.Core/Common/CrudResult.cs`
- Keep: `src/DingxinErp.Core/Common/PagedResult.cs`
- Keep: `src/DingxinErp.Core/Common/IAuditableEntity.cs`

**Step 1: 刪除 Sample 相關檔案**

Run:
```powershell
Remove-Item "src/DingxinErp.Core/Entities/SampleHeader.cs"
Remove-Item "src/DingxinErp.Core/Entities/SampleDetail.cs"
Remove-Item "src/DingxinErp.Core/DTOs/*" -Recurse
Remove-Item "src/DingxinErp.Core/Services/SampleService.cs"
Remove-Item "src/DingxinErp.Core/Validators/*" -Recurse
Remove-Item "src/DingxinErp.Core/Interfaces/ISampleRepository.cs"
Remove-Item "src/DingxinErp.Core/Interfaces/ISampleService.cs"
```

**Step 2: 確認 Common/ 還在**

Run:
```powershell
Get-ChildItem "src/DingxinErp.Core/Common/" -File
```
Expected: CrudResult.cs, PagedResult.cs, IAuditableEntity.cs

---

### Task 4: 精簡 src/ 為空骨架 — Infrastructure 層

**Files:**
- Delete: `src/DingxinErp.Infrastructure/Configurations/SampleHeaderConfiguration.cs`
- Delete: `src/DingxinErp.Infrastructure/Configurations/SampleDetailConfiguration.cs`
- Delete: `src/DingxinErp.Infrastructure/Repositories/SampleRepository.cs`
- Modify: `src/DingxinErp.Infrastructure/Data/AppDbContext.cs` — 移除 Sample DbSet

**Step 1: 刪除 Sample 相關檔案**

Run:
```powershell
Remove-Item "src/DingxinErp.Infrastructure/Configurations/*" -Recurse
Remove-Item "src/DingxinErp.Infrastructure/Repositories/SampleRepository.cs"
```

**Step 2: 修改 AppDbContext.cs**

移除 `DbSet<SampleHeader>` 和 `DbSet<SampleDetail>` 宣告及 `OnModelCreating` 中的 Sample configuration。
保留 `AppDbContext` 類別空殼和 `SaveChangesAsync` 審計欄位邏輯。

**Step 3: 驗證 Infrastructure 編譯**

Run: `dotnet build src/DingxinErp.Infrastructure/DingxinErp.Infrastructure.csproj`
Expected: Build succeeded

---

### Task 5: 精簡 src/ 為空骨架 — Web 層

**Files:**
- Delete: `src/DingxinErp.Web/Controllers/SampleController.cs`
- Delete: `src/DingxinErp.Web/Controllers/SamplePageController.cs`
- Delete: `src/DingxinErp.Web/Views/Sample/` (entire folder)
- Modify: `src/DingxinErp.Web/Program.cs` — 移除 Sample DI 註冊
- Keep: `src/DingxinErp.Web/Controllers/HomeController.cs`
- Keep: `src/DingxinErp.Web/Middleware/ExceptionHandlingMiddleware.cs`
- Keep: `src/DingxinErp.Web/Views/Home/`
- Keep: `src/DingxinErp.Web/Views/Shared/`

**Step 1: 刪除 Sample Controller/View**

Run:
```powershell
Remove-Item "src/DingxinErp.Web/Controllers/SampleController.cs"
Remove-Item "src/DingxinErp.Web/Controllers/SamplePageController.cs"
Remove-Item "src/DingxinErp.Web/Views/Sample" -Recurse -Force
```

**Step 2: 修改 Program.cs**

移除 `ISampleService`、`ISampleRepository` 的 DI 註冊，保留其他基礎設施（DbContext、ExceptionHandling）。

**Step 3: 確認 wwwroot/ js 檔案是否需要保留**

檢查 `wwwroot/js/crud-common.js` 和 `wwwroot/js/master-detail.js`：
- 如果是通用工具 → 保留
- 如果只被 Sample 使用 → 保留（scaffold 複製後新作業也會用到）

---

### Task 6: 精簡 tests/

**Files:**
- Modify: `tests/DingxinErp.Core.Tests/UnitTest1.cs` — 移除 Sample 相關測試，保留空殼

**Step 1: 清空測試內容**

將 `UnitTest1.cs` 改為空的 placeholder。

---

### Task 7: 驗證空骨架 build

**Step 1: 全量 build**

Run:
```powershell
dotnet build DingxinErpTemplate.sln
```
Expected: Build succeeded. 0 Error(s)

**Step 2: Commit**

```bash
git add -A
git commit -m "refactor: strip src/ to empty skeleton, keep Sample in demo/reference/"
```

---

### Task 8: 建立 speckit.scaffold.agent.md

**Files:**
- Create: `.github/agents/speckit.scaffold.agent.md`

**Step 1: 建立 agent 檔案**

內容包含完整的 scaffold 流程：
1. 確認位置（git remote check）
2. 掃描 ops-docs/（讀取 erp-doc.md, requirements.md）
3. 顯示分析結果，詢問作業代號
4. 確認後複製 demo/reference/src/ → src/, demo/reference/tests/ → tests/
5. 驗證 dotnet build
6. 提示繼續 /speckit.specify

**Step 2: 驗證 agent 可被 VS Code 辨識**

確認 `.github/agents/speckit.scaffold.agent.md` 有正確的 YAML frontmatter。

**Step 3: Commit**

```bash
git add .github/agents/speckit.scaffold.agent.md
git commit -m "feat: add speckit.scaffold agent for ERP project initialization"
```

---

### Task 9: 更新 ERP 補充規則

**Files:**
- Modify: `.specify/customizations/erp-supplements/speckit.specify.md` — 前置確認邏輯
- Modify: `.specify/customizations/erp-supplements/speckit.implement.md` — 安全檢查邏輯

**Step 1: 更新 speckit.specify.md 前置確認**

新邏輯：
```
偵測到在模板根目錄 →
  檢查 src/ 是否有 Entities（非 Common/）→
    無 → 提示「src/ 尚未初始化，建議先執行 /speckit.scaffold」
    有 → 直接繼續 SDD
```

**Step 2: 更新 speckit.implement.md 安全檢查**

新邏輯：
```
偵測到在模板根目錄 →
  檢查 src/DingxinErp.Core/Entities/ 是否有 .cs 檔案 →
    無 → 停止，提示先執行 /speckit.scaffold
    有 → 允許繼續
```

**Step 3: Commit**

```bash
git add .specify/customizations/erp-supplements/
git commit -m "feat: update ERP supplements for new scaffold workflow"
```

---

### Task 10: 更新文件

**Files:**
- Modify: `AGENTS.md` — 目錄結構、SDD 流程說明
- Modify: `CLAUDE.md` — 同上
- Modify: `README.md` — 使用說明更新
- Modify: `.gitignore` — 確保 demo/ 可提交

**Step 1: 更新 AGENTS.md**

- 目錄結構章節：反映新結構
- SDD 流程章節：更新為 scaffold → specify → plan → tasks → implement
- 新增作業步驟：加入「先執行 /speckit.scaffold」

**Step 2: 更新 CLAUDE.md**

同步 AGENTS.md 的變更。

**Step 3: 更新 README.md**

- 快速開始流程
- demo/reference/ 說明

**Step 4: 更新 .gitignore**

確保 `demo/reference/` 不被忽略。

**Step 5: Commit**

```bash
git add AGENTS.md CLAUDE.md README.md .gitignore
git commit -m "docs: update documentation for new template structure"
```

---

### Task 11: 最終驗證

**Step 1: 模板根目錄 build**

Run: `dotnet build DingxinErpTemplate.sln`
Expected: Build succeeded. 0 Error(s)

**Step 2: demo/reference/ build**

Run: `dotnet build demo/reference/DingxinErpTemplate.sln`
Expected: Build succeeded. 0 Error(s)

**Step 3: 確認 src/ 是空骨架**

Run:
```powershell
Get-ChildItem "src/DingxinErp.Core/Entities/" -File -ErrorAction SilentlyContinue
```
Expected: 無檔案（空目錄或不存在）

**Step 4: 確認 demo/reference/ 有完整 Sample**

Run:
```powershell
Get-ChildItem "demo/reference/src/DingxinErp.Core/Entities/" -File
```
Expected: SampleHeader.cs, SampleDetail.cs

**Step 5: 確認 speckit.scaffold agent 存在**

Run:
```powershell
Test-Path ".github/agents/speckit.scaffold.agent.md"
```
Expected: True

**Step 6: Final Commit**

```bash
git add -A
git commit -m "chore: final verification - template restructure complete"
```
