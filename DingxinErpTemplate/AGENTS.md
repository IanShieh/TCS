# AGENTS.md — AI Coding Agent 協作指引

> 本檔案為跨工具 AI 指引標準，適用於 GitHub Copilot、OpenAI Codex、Cursor 等 AI 編碼助手。

## 專案概述

這是鼎新 ERP 作業轉換的模板專案，採用 Clean Architecture (.NET 8)。
將鼎新 ERP 的各種作業（單頭+單身 CRUD）快速轉換為現代化 Web App。

- 詳細開發進度見 [PROGRESS.md](PROGRESS.md)
- 完整使用說明見 [README.md](README.md)
- 團隊開發憲法見 [.specify/memory/constitution.md](.specify/memory/constitution.md)

## 架構規則

- **三層架構**: Web → Core ← Infrastructure
- **Core 層無外部依賴** (僅 FluentValidation)
- **統一回傳**: `CrudResult<T>` (Success/Message/Data/Errors)
- **分頁回傳**: `PagedResult<T>` (Items/TotalItems/TotalPages/CurrentPage/PageSize)
- **審計欄位**: 透過 `IAuditableEntity` 統一管理 (CREATOR/CREATE_DATE/MODIFIER/MODI_DATE/FLAG)
- **單頭/單身 CRUD 完全分離**: 各自獨立的 API 端點、Modal、操作按鈕
- **單頭行點擊自動選取**: 點擊行自動勾選 checkbox + 啟用編輯/刪除，切換行自動取消前一行
- **JSON 序列化**: `PropertyNamingPolicy = null` (保持 PascalCase)
- **InMemory 示範模式**: 未設定連線字串時自動使用 InMemory DB
- **USE_INMEMORY_DB 環境變數**: 透過 launchSettings.json 切換 InMemory / SQL Server 模式
- **SQL Server 2008 R2 相容**: TLS 1.0 安全協定 + ROW_NUMBER() 分頁 (不使用 OFFSET/FETCH)
- **雙路徑分頁**: Repository 自動偵測 InMemory → LINQ Skip/Take、SQL Server → ROW_NUMBER()
- **手動映射**: 使用 MappingExtensions (ToDto/ToEntity/UpdateFrom)，不使用 AutoMapper

## 程式碼規範

- Entity 屬性名使用 ERP 原始欄位名 (TA001, TB003 等)
- char 欄位必須設定 `IsFixedLength()` + `IsUnicode(false)`
- 類別名/方法名使用英文
- 註解/文件使用繁體中文
- 使用 `async/await` 非同步模式
- 驗證使用 FluentValidation，Validator 放在 Core 層
- Service 依賴介面 (ISampleService)，透過 DI 注入

## 目錄結構

```
DingxinErpTemplate.sln         # 根目錄 .sln（預設指向 demo/src/，scaffold 後指向 src/）
demo/                          # ★ 完整 Sample 範例（唯讀參考，可獨立 build/run）
├── DingxinErpTemplate.sln    #   demo 專用 .sln
├── src/
│   ├── DingxinErp.Web/       #   範例表現層
│   ├── DingxinErp.Core/      #   範例核心層（SampleHeader/SampleDetail）
│   └── DingxinErp.Infrastructure/  # 範例資料層
└── tests/
src/                           # ★ 開發區（scaffold 前為空，scaffold 後為作業專案）
├── DingxinErp.Web/            #   表現層 (MVC Controllers + Views + API)
├── DingxinErp.Core/           #   核心業務層 (Entities, DTOs, Services, Validators)
└── DingxinErp.Infrastructure/ #   基礎設施層 (DbContext, Repositories)
tests/                         # 單元測試（scaffold 後才有內容）
ops-docs/                      # ERP 作業原始文件（截圖/說明）
specs/                         # SDD 規格文件產出
skill/                         # Copilot CLI Skill
.specify/                      # spec-kit SDD 設定 + 團隊憲法
.github/agents/                # speckit agent 定義檔
```

## 資安防護

- **gitleaks pre-commit hook**: `dotnet build` 首次執行時自動啟用（透過 `Directory.Build.props` + `.githooks/`）
- **CI 掃描**: `.github/workflows/security-scan.yml` — push/PR 到 main 時自動偵測敏感資料
- **前置需求**: 團隊成員需安裝 gitleaks (`winget install gitleaks`)

## 常用命令

```bash
# 範例專案（clone 後直接可用）
dotnet build                                    # 建置 demo 範例（首次自動啟用 gitleaks hook）
dotnet run --project demo/src/DingxinErp.Web    # 執行範例 (http://localhost:5000)

# 開發中的作業專案（scaffold 後才可用）
dotnet build src/DingxinErp.Web/DingxinErp.Web.csproj  # 建置開發中專案
dotnet run --project src/DingxinErp.Web                # 執行開發中專案

dotnet test                            # 測試
```

## 查詢 ERP 資料庫

使用 `ops-docs/db-query.ps1` 查詢鼎新 ERP 測試資料庫的表格結構（需先從 `.example` 建立本機版本）：

```powershell
powershell -File ops-docs/db-query.ps1 -Query "YOUR_SQL_QUERY"
```

> **注意**: `db-query.ps1` 包含機密連線資訊，已加入 `.gitignore` 不會上傳。
> 範本檔: `ops-docs/db-query.ps1.example`，團隊成員需複製後填入自己的連線資訊。

### 查詢表格 Schema（推薦）

鼎新 ERP 系統將所有表格的欄位定義存放在 `DSCSYS.dbo.ADMMD`，可一次取得欄位名、中文說明、型別、長度：

```sql
SELECT MD001, MD002, MD003, MD004, MD005, MD006
FROM DSCSYS.dbo.ADMMD
WHERE MD001 = 'TABLE_NAME'
ORDER BY MD002
```

| 欄位 | 說明 |
|------|------|
| MD001 | 表格名稱（如 PURMA、PURTA） |
| MD002 | 欄位序號 |
| MD003 | 欄位名稱（如 MA001、TA001） |
| MD004 | 中文欄位說明 |
| MD005 | 資料型態（C=char, N=numeric, V=varchar） |
| MD006 | 長度（numeric 含小數位數，如 5.4 表示整數5位小數4位） |

## API 端點格式

單頭和單身 CRUD 完全分離：

```
# 單頭 CRUD (僅單頭欄位)
GET    /api/{entity}                              # 分頁查詢
GET    /api/{entity}/{pk1}/{pk2}                  # 依主鍵取得單頭
POST   /api/{entity}                              # 新增單頭 (不含單身)
PUT    /api/{entity}/{pk1}/{pk2}                  # 更新單頭 (不含單身)
DELETE /api/{entity}/{pk1}/{pk2}                  # 刪除單頭 (Cascade 刪除單身)

# 單身 CRUD (獨立逐筆操作)
GET    /api/{entity}/{pk1}/{pk2}/details          # 列出所有單身
GET    /api/{entity}/{pk1}/{pk2}/details/{seq}    # 取得單筆單身
POST   /api/{entity}/{pk1}/{pk2}/details          # 新增單筆單身
PUT    /api/{entity}/{pk1}/{pk2}/details/{seq}    # 更新單筆單身
DELETE /api/{entity}/{pk1}/{pk2}/details/{seq}    # 刪除單筆單身
```

## SDD 流程 (spec-kit)

SDD 流程直接在模板根目錄執行，不需要建立新的作業目錄。

```
Step 0  初始化開發環境
  /speckit.scaffold    → 掃描 ops-docs/ → 詢問作業代號 → 從 demo/ 複製到 src/

Step 1~5  SDD 開發流程
  /speckit.specify     → 產出 ERP 作業規格書 (表格/欄位/PK/FK/業務規則)
  /speckit.plan        → 產出技術計畫 (架構/結構/實作方向)
  /speckit.tasks       → 產出任務清單 (按 Phase 分組)
  /speckit.implement   → 逐步實作任務（在 src/ 中新增作業檔案）
  /speckit.checklist   → 驗收檢核
```

### 使用流程

```powershell
# 1. Clone 模板
git clone https://github.com/chrisbln2014/DingxinErpTemplate.git
cd DingxinErpTemplate

# 2. 放入 ERP 作業文件到 ops-docs/
#    （截圖、欄位說明等，AI 會自動分析）

# 3. 執行 scaffold（從 demo/ 複製 Sample 到 src/）
/speckit.scaffold

# 4. 執行 SDD 流程
/speckit.specify → /speckit.plan → /speckit.tasks → /speckit.implement → /speckit.checklist
```

### 目錄角色

- `demo/` — Sample 範例（唯讀參考，scaffold 複製來源）
- `src/` — scaffold 後為開發中的作業專案（SDD implement 在這裡新增檔案）
- `ops-docs/` — ERP 作業文件（截圖/說明/requirements.md）
- `specs/` — SDD 產出的規格文件（spec.md / plan.md / tasks.md）
- 憲法位置: `.specify/memory/constitution.md` (英文) / `constitution-zh-TW.md` (繁中)

## 新增作業步驟

1. 在 `Core/Entities/` 建立 Entity (參考 SampleHeader/SampleDetail)
2. 在 `Infrastructure/Configurations/` 建立 Configuration
3. 在 `Core/DTOs/` 建立 DTO + MappingExtensions
4. 在 `Core/Interfaces/` 建立 IRepository + IService
5. 在 `Infrastructure/Repositories/` 實作 Repository
6. 在 `Core/Services/` 實作 Service
7. 在 `Core/Validators/` 建立 Validator
8. 在 `Web/Controllers/` 建立 Controller
9. 在 `Web/Views/` 建立 CRUD 頁面
10. 在 `Program.cs` 註冊 DI

## 重要注意事項

- **不使用 Mini API** — 僅使用 Controller-based API
- **不使用 AutoMapper** — 使用手動 MappingExtensions
- **char 欄位一定要設定 `IsFixedLength()` + `IsUnicode(false)`** — 否則 EF Core 會產生 `N''` 前綴導致查詢失敗
- **複合主鍵**: 大部分 ERP 表格使用 2-3 個欄位的複合主鍵
- **前端使用 PascalCase**: JavaScript 中存取 API 回傳資料用 `item.TA001`、`data.Items`、`result.Success`
- **前端框架**: Bootstrap 5 + jQuery (CDN)，傳統 Web Form 風格
- **SQL Server 2008 R2**: 不支援 OFFSET/FETCH，Repository 必須用 ROW_NUMBER() 分頁
- **分頁 UI**: 最多顯示 5 頁 + 省略號 + 首尾頁跳轉

## 團隊開發流程

```
1. 從 initial-template 分支切出 feature branch (本機)
2. 執行 SDD 流程開發 (scaffold → specify → plan → tasks → implement)
3. 開發完成 → 選擇匯出方式 (export-and-push 或 export-bundle)
4. 刪除本機 feature branch
```

### 專案匯出——兩種方式

兩種匯出腳本都會自動：移除 `demo/`、`skill/`、`.claude/`、`.playwright-mcp/`、`bin/obj`、`ops-docs/_example*`，並產生專案專屬 README.md。

| 方式 | 腳本 | 適用情境 | 前置需求 |
|------|------|---------|----------|
| **A. GitHub Push** | `export-and-push.ps1` | 建立獨立 GitHub Repo 發布 | Git + GitHub CLI (`gh`) |
| **B. Git Bundle** | `export-bundle.ps1` | 本機封存歸檔，保留完整歷史 | 僅需 Git |

#### 方式 A: export-and-push.ps1 (GitHub Push)

```powershell
powershell -File .specify/scripts/powershell/export-and-push.ps1 `
    -SourceDir  "C:\projects\DingxinErpTemplate" `
    -ExportRoot "C:\projects" `
    -ProjectName "SupplierCreate" `
    -GitHubUser "chrisbln2014" `
    -Description "PURI01 供應廠商資料建立" `
    -Private
```

#### 方式 B: export-bundle.ps1 (Git Bundle)

```powershell
powershell -File .specify/scripts/powershell/export-bundle.ps1 `
    -SourceDir  "C:\projects\DingxinErpTemplate" `
    -ExportRoot "C:\projects" `
    -ProjectName "SupplierCreate" `
    -Description "PURI01 供應廠商資料建立"
```

產出：`C:\projects\SupplierCreate\`（專案資料夾）+ `C:\projects\SupplierCreate.bundle`（可用 `git clone` 還原）

**遠端分支規則**: 只保留 `main` 和 `initial-template`，feature branch 不推送遠端
