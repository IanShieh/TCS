# GEMINI.md — Gemini AI 協作指引

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

## 常用命令

```bash
# 範例專案（clone 後直接可用）
dotnet build                                    # 建置 demo 範例
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

SDD 流程直接在模板根目錄執行。

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

- 憲法位置: `.specify/memory/constitution.md` (英文) / `constitution-zh-TW.md` (繁中)
- `demo/` — Sample 範例（唯讀參考，scaffold 複製來源）
- `src/` — scaffold 後為開發中的作業專案
- 所有 SDD 產出皆遵循憲法原則

## 新增作業步驟

1. 執行 `/speckit.scaffold` 從 demo/ 複製範例到 src/
2. 在 `Core/Entities/` 建立 Entity (參考 demo/src 中的 SampleHeader/SampleDetail)
3. 在 `Infrastructure/Configurations/` 建立 Configuration
4. 在 `Core/DTOs/` 建立 DTO + MappingExtensions
5. 在 `Core/Interfaces/` 建立 IRepository + IService
6. 在 `Infrastructure/Repositories/` 實作 Repository
7. 在 `Core/Services/` 實作 Service
8. 在 `Core/Validators/` 建立 Validator
9. 在 `Web/Controllers/` 建立 Controller
10. 在 `Web/Views/` 建立 CRUD 頁面
11. 在 `Program.cs` 註冊 DI

## 重要注意事項

- **不使用 Mini API** — 僅使用 Controller-based API
- **不使用 AutoMapper** — 使用手動 MappingExtensions
- **char 欄位一定要設定 `IsFixedLength()` + `IsUnicode(false)`** — 否則 EF Core 會產生 `N''` 前綴導致查詢失敗
- **複合主鍵**: 大部分 ERP 表格使用 2-3 個欄位的複合主鍵
- **前端使用 PascalCase**: JavaScript 中存取 API 回傳資料用 `item.TA001`、`data.Items`、`result.Success`
- **前端框架**: Bootstrap 5 + jQuery (CDN)，傳統 Web Form 風格

## 偏好設定

- 回覆語言：繁體中文 (zh-TW)
- 程式碼註解：繁體中文
- Git Commit Messages：繁體中文
- 錯誤訊息：繁體中文
- 類別名/方法名/變數名：英文
