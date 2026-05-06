# CLAUDE.md — AI 協作指引

## 專案概述

這是鼎新 ERP 作業轉換的模板專案，採用 Clean Architecture (.NET 8)。
詳細開發進度見 [PROGRESS.md](PROGRESS.md)。

## 架構規則

- **三層架構**: Web → Core ← Infrastructure
- **Core 層無外部依賴** (僅 FluentValidation)
- **統一回傳**: `CrudResult<T>` (Success/Message/Data/Errors)
- **審計欄位**: 透過 `IAuditableEntity` 統一管理
- **單頭/單身 CRUD 完全分離**: 各自獨立的 API、Modal、操作按鈕
- **單頭行點擊自動選取**: 點擊行自動勾選 checkbox + 啟用編輯/刪除，切換行自動取消前一行
- **JSON 序列化**: PropertyNamingPolicy = null (保持 PascalCase)
- **InMemory 示範模式**: 未設定連線字串時自動使用 InMemory DB
- **USE_INMEMORY_DB 環境變數**: 透過 launchSettings.json 切換 InMemory / SQL Server 模式
- **SQL Server 2008 R2 相容**: TLS 1.0 安全協定 + ROW_NUMBER() 分頁 (不使用 OFFSET/FETCH)
- **雙路徑分頁**: Repository 自動偵測 Provider → InMemory 用 LINQ、SQL Server 用 ROW_NUMBER()

## 程式碼規範

- Entity 屬性名使用 ERP 原始欄位名 (TA001, TB003 等)
- char 欄位必須設定 `IsFixedLength()` + `IsUnicode(false)`
- 類別名/方法名使用英文
- 註解/文件使用繁體中文
- 使用 `async/await` 非同步模式

## 作業文件存放

- `ops-docs/[作業代號]-[作業名稱]/` — 存放 ERP 畫面截圖和作業說明文件
- `ops-docs/[作業代號]-[作業名稱]/requirements.md` — 客製化需求說明
- 詳見 [ops-docs/README.md](ops-docs/README.md)

## 資安防護

- **gitleaks pre-commit hook**: `dotnet build` 時自動啟用（`Directory.Build.props` + `.githooks/`）
- **CI 掃描**: push/PR 到 main 時 GitHub Actions 自動偵測敏感資料
- 團隊成員需安裝 gitleaks: `winget install gitleaks`

## 常用命令

```bash
# 範例專案（clone 後直接可用）
dotnet build                                    # 建置 demo 範例（首次自動啟用 gitleaks hook）
dotnet run --project demo/src/DingxinErp.Web    # 執行範例 (http://localhost:5000)

# 開發中的作業專案（scaffold 後才可用）
dotnet run --project src/DingxinErp.Web          # 執行開發中專案
dotnet test                                      # 測試
```

## 查詢 ERP 資料庫

使用 `ops-docs/db-query.ps1` 查詢鼎新 ERP 測試資料庫（需先從 `.example` 建立本機版本）：

```powershell
powershell -File ops-docs/db-query.ps1 -Query "YOUR_SQL_QUERY"
```

> `db-query.ps1` 含機密連線資訊（gitignored），範本: `ops-docs/db-query.ps1.example`

### 查詢表格 Schema（推薦）

```sql
SELECT MD001, MD002, MD003, MD004, MD005, MD006
FROM DSCSYS.dbo.ADMMD WHERE MD001 = 'TABLE_NAME' ORDER BY MD002
```

- MD003=欄位名, MD004=中文說明, MD005=型態(C/N/V), MD006=長度

## API 端點格式

★ 單頭和單身 CRUD 完全分離

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

## SDD 流程

SDD 流程直接在模板根目錄執行。

```
Step 0  初始化開發環境
  /speckit.scaffold    → 掃描 ops-docs/ → 詢問作業代號 → 從 demo/ 複製到 src/

Step 1~5  SDD 開發流程
  /speckit.specify     → 產出 ERP 作業規格書
  /speckit.plan        → 產出技術計畫
  /speckit.tasks       → 產出任務清單
  /speckit.implement   → 逐步實作任務（在 src/ 中新增作業檔案）
  /speckit.checklist   → 驗收檢核
```

- 憲法位置: `.specify/memory/constitution.md` (英文) / `constitution-zh-TW.md` (繁中)
- `demo/` — Sample 範例（唯讀參考，scaffold 複製來源）
- `src/` — scaffold 後為開發中的作業專案

## 新增作業步驟

1. 執行 `/speckit.scaffold` 從 demo/ 複製範例到 src/
2. 在 Core/Entities/ 建立 Entity (參考 demo/src 中的 SampleHeader/SampleDetail)
3. 在 Infrastructure/Configurations/ 建立 Configuration
4. 在 Core/DTOs/ 建立 DTO + MappingExtensions
5. 在 Core/Interfaces/ 建立 IRepository + IService
6. 在 Infrastructure/Repositories/ 實作 Repository
7. 在 Core/Services/ 實作 Service
8. 在 Core/Validators/ 建立 Validator
9. 在 Web/Controllers/ 建立 Controller
10. 在 Web/Views/ 建立 CRUD 頁面
11. 在 Program.cs 註冊 DI

## 重要注意事項

- **SQL Server 2008 R2**: 不支援 OFFSET/FETCH，Repository 必須用 ROW_NUMBER() 分頁
- **分頁 UI**: 最多顯示 5 頁 + 省略號 + 首尾頁跳轉
- **char 欄位**: 必須設定 `IsFixedLength()` + `IsUnicode(false)`

## 團隊開發流程

```
1. 從 initial-template 分支切出 feature branch (本機)
2. 執行 SDD 流程開發
3. 開發完成 → 選擇匯出方式 (export-and-push 或 export-bundle)
4. 刪除本機 feature branch
```

### 專案匯出——兩種方式

兩種腳本都會自動：移除 demo/、skill/、_example、產生專案 README。

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

產出：專案資料夾 + `.bundle` 檔（可用 `git clone xxx.bundle` 還原）
