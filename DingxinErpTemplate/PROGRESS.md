# 開發進度記錄 (PROGRESS.md)

> 最後更新: 2026-03-27

## 專案目標

建立一個標準化的 .NET 8 模板專案，讓團隊能快速將鼎新 ERP 的各種作業（單頭+單身）轉換為現代化 Web App。同時整合 spec-kit SDD 流程與 Copilot CLI Skill，實現 AI 驅動的開發工作流。

---

## 已完成項目

### Phase 1: 專案分析與規劃

- [x] 分析 4 個既有 ERP 轉換專案 (AssetManagement, BankAccountCreate, AccountOffsetEntry, AccountCodeCreate)
- [x] 萃取各專案的共通模式 (Clean Architecture, CrudResult, 審計欄位, char 欄位處理)
- [x] 制定模板架構規範與 API 端點格式
- [x] 建立完整開發計畫 (40+ 項任務)

### Phase 2: 核心基礎建設

- [x] 建立 Solution 結構 (4 個 .csproj: Web, Core, Infrastructure, Tests)
- [x] 實作共用元件: `CrudResult<T>`, `PagedResult<T>`, `IAuditableEntity`
- [x] 設定 .editorconfig, .gitignore

### Phase 3: 範例 Entity 與資料層

- [x] 建立 `SampleHeader` / `SampleDetail` 範例 Entity (含複合主鍵、FK 關聯)
- [x] 建立 EF Core Configuration (IsFixedLength + IsUnicode(false) 確保鼎新 ERP 相容)
- [x] 實作 `AppDbContext` (支援 SQL Server + InMemory 雙模式)
- [x] 實作 `SampleRepository` (分頁查詢、搜尋、CRUD)

### Phase 4: 業務邏輯層

- [x] 定義 `ISampleRepository` / `ISampleService` 介面
- [x] 實作 `SampleService` (含審計欄位自動填寫)
- [x] 建立 FluentValidation 驗證器 (CreateSampleHeaderValidator, UpdateSampleHeaderValidator)
- [x] 建立 DTO 與手動映射 (MappingExtensions — 不依賴 AutoMapper)

### Phase 5: 表現層

- [x] 建立 `Program.cs` (DI 註冊、Middleware 管線、Swagger、InMemory 示範模式)
- [x] 建立 `ExceptionHandlingMiddleware` (全域例外處理)
- [x] 建立 `SampleController` (API) + `SamplePageController` (MVC) + `HomeController`
- [x] 建立首頁 (Home/Index.cshtml) 含導覽卡片
- [x] 建立範例作業頁面 (Sample/Index.cshtml) 含完整 CRUD UI
- [x] 建立共用 Layout (_Layout.cshtml) 含 Bootstrap 5 + Bootstrap Icons

### Phase 6: 前端 JavaScript

- [x] `crud-common.js` — 單頭 CRUD 通用函式 (Toast、分頁、搜尋、Modal、儲存、快捷鍵)
- [x] `master-detail.js` — 單身連動 + 獨立單身 CRUD (選取、新增/編輯/刪除明細)
- [x] `site.css` — 自訂樣式 (selected-row、cursor-pointer)

### Phase 7: 單頭/單身 CRUD 分離 ⭐

- [x] **Backend**: 單頭 Create/Update 不再包含單身資料
- [x] **Backend**: 新增獨立單身 CRUD (GetDetailByKey, CreateDetail, UpdateDetail, DeleteDetail)
- [x] **Frontend**: 拆分為獨立的 `#headerModal` 和 `#detailModal`
- [x] **Frontend**: 單身區域擁有獨立的 新增明細/編輯明細/刪除明細 按鈕
- [x] **Frontend**: 單身表格支援 checkbox 選取、全選
- [x] **Frontend**: 點擊單頭行自動選取 (checkbox 勾選 + 啟用編輯/刪除按鈕)
- [x] **Frontend**: 切換單頭行時自動取消前一行選取（單選模式）
- [x] 移除舊的 replace-all 模式 (ReplaceDetailsAsync)

### Phase 8: Spec-kit SDD 整合

- [x] 建立團隊憲法 (constitution.md — 6 大原則)
- [x] 建立 spec/plan/tasks/checklist 模板
- [x] 建立 PowerShell 腳本 (create-new-feature.ps1, setup-plan.ps1)
- [x] 建立 GitHub Prompts (9 個 prompt 檔)
- [x] 建立 GitHub Agents (8 個 agent 檔)
- [x] 建立 Claude Commands (8 個 command 檔)
- [x] **升級至官方 spec-kit v0.4.2** — 以官方 agent 為基礎 + ERP 補充段追加
- [x] 建立 9 個 ERP supplement 檔 (`.specify/customizations/erp-supplements/`)
- [x] 建立 `setup-speckit.ps1` 團隊初始化腳本（官方 init + ERP 客製化疊回 + 憲法還原）
- [x] 修復 PowerShell 5.1 編碼問題（移除 emoji + 加 UTF-8 BOM + `-Encoding UTF8`）
- [x] 驗證防重複追加機制（marker 偵測 + 冪等性測試通過）

### Phase 9: Copilot CLI Skill

- [x] 建立 SKILL.md (4 輪對話式引導 scaffold)
- [x] 建立參考文件: architecture.md, code-templates.md, ui-patterns.md, speckit-setup.md
- [x] 安裝到使用者層級 (~/.agents/skills/dingxin-erp-scaffold/)
- [x] 加入專案內複本供團隊分發 (skill/dingxin-erp-scaffold/)

### Phase 10: 驗證與文件

- [x] Build 通過 (0 errors, 0 warnings)
- [x] InMemory 示範模式運作正常 (4 筆單頭 + 6 筆單身種子資料)
- [x] Playwright MCP UI 驗證全部通過:
  - 首頁渲染正常
  - 單頭表格載入 & 分頁
  - 單頭新增 (Modal 僅單頭欄位 → 儲存 → 筆數更新) ✓
  - 單頭編輯 (載入現有資料 → 修改 → 儲存) ✓
  - 單頭刪除 (確認 Modal 含 Cascade 提示 → 刪除 → 筆數減少) ✓
  - 單頭行點擊自動選取 (checkbox 勾選 + 編輯/刪除啟用) ✓
  - 單頭行切換時自動取消前一行選取（單選模式）✓
  - 單身連動 (點擊單頭 → 載入單身) ✓
  - 單身獨立新增 (Modal + 儲存 + DetailCount 更新) ✓
  - 單身獨立編輯 (載入現有資料 → 修改數量 → 金額自動重算) ✓
  - 單身獨立刪除 (確認 Modal → 刪除 → DetailCount 歸零) ✓
  - 單身選取 (checkbox + 按鈕狀態連動) ✓
  - 搜尋 (關鍵字篩選 → 清除搜尋恢復全部) ✓
- [x] CLAUDE.md、README.md、PROGRESS.md 文件完成
- [x] Skill 參考文件同步更新

---

## 架構決策記錄

| 決策 | 選擇 | 理由 |
|------|------|------|
| ORM | EF Core (取代 DbNetSuiteCore) | 原生 .NET、活躍社群、LINQ 支援 |
| 映射 | 手動 MappingExtensions (不用 AutoMapper) | 減少依賴、ERP 欄位名直覺對應 |
| 驗證 | FluentValidation | 與 EF Core 分離、規則可測試 |
| JSON | PropertyNamingPolicy = null | 保持 PascalCase 與 DTO 屬性名一致 |
| 前端 | Bootstrap 5 + jQuery + 原生 JS | 傳統 Web Form 風格、學習曲線低 |
| 單身 CRUD | 獨立 API + 獨立 Modal | 關注點分離、單頭/單身各自管理 |
| 示範模式 | InMemory DB (自動偵測) | 免設定 SQL Server 即可 demo |
| char 欄位 | IsFixedLength() + IsUnicode(false) | 避免 EF Core 產生 N'' 前綴導致查詢失敗 |

---

## 專案統計

| 項目 | 數量 |
|------|------|
| C# 原始碼檔案 | 25 |
| Razor Views | 5 |
| JavaScript 檔案 | 2 |
| CSS 檔案 | 1 |
| Spec-kit 設定檔 | 34 |
| Skill 參考文件 | 5 |
| 文件檔 (MD) | 41 |
| ERP Supplement 檔 | 9 |
| 合計 | ~122 檔案 |

---

## 已知限制

1. **InMemory DB 不支援交易和關聯約束** — 僅供 demo，正式環境須接 SQL Server
2. **審計欄位使用硬編碼 "SYSTEM"** — 正式環境需整合身分驗證取得實際使用者
3. **Swagger 僅開發環境可用** — Production 環境自動關閉
4. **Bootstrap CDN 依賴** — 無網路環境需改為本地檔案
5. **單一 DbContext** — 若需多資料庫連線，需擴充架構

---

## Phase 11: 清理與測試準備

- [x] 移除 openspec 資料夾（已移至 `feature/openspec-superpowers-skills` 分支）
- [x] 移除 Playwright 測試截圖（test-*.png，非版控追蹤檔）
- [x] 確認 Build 通過 (0 errors, 0 warnings)
- [x] 確認測試通過 (1 test passed)
- [x] 確認 InMemory 示範模式正常啟動
- [x] 確認 API 端點回應正常 (HTTP 200)
- [x] 更新文件（PROGRESS.md、README.md）

---

## Phase 12: Spec-kit + SDD 流程端對端測試

- [x] 建立乾淨測試環境 (git worktree)
- [x] `dotnet build` — 0 錯誤, 0 警告
- [x] InMemory Demo 頁面正常 (localhost:5099 — 4 筆單頭 + 6 筆單身)
- [x] `setup-speckit.ps1 -DryRun` — 3 步驟全部成功
- [x] `setup-speckit.ps1 -SkipInit` — 9 個 supplement 追加 + 2 個憲法還原
- [x] 防重複追加驗證（二次執行全部 `[SKIP]`，marker 無重複）
- [x] 作業文件放入 `ops-docs/PURTA-採購單建立/` 結構正確
- [x] SDD 檔案結構完整驗證：
  - 9 agents (含 ERP 補充段) + 9 prompts (YAML `agent:` 指向正確)
  - templates (spec/plan/tasks/checklist/constitution) 就位
  - `.specify/init-options.json` (speckit_version: 0.4.2)
  - constitution (英文 + 繁中) 已還原
- [x] 測試環境完整清理（worktree 移除 + 分支刪除）

---

## 後續可擴展方向

- [ ] 整合身分驗證 (Cookie / JWT)
- [ ] 新增匯出功能 (Excel / PDF)
- [ ] 新增批次作業模式
- [ ] Docker 容器化部署
- [ ] CI/CD Pipeline (GitHub Actions)
- [ ] 多語系支援 (i18n)
- [ ] 前端升級至 Vue.js / React (可選)

---

## Phase 13: 模板目錄結構重構 ⭐

> 2026-03-27 — 將 Sample 範例從 src/ 分離到 demo/，讓 src/ 作為 scaffold 後的乾淨開發區。

### 設計決策

| 項目 | 決策 | 理由 |
|------|------|------|
| demo/ 角色 | 完整 Sample 範例（唯讀參考） | scaffold 複製來源，clone 後可直接 build/run |
| src/ 角色 | 空（scaffold 後才有內容） | 避免 Sample 和作業程式碼混在一起 |
| 根目錄 .sln | 預設指向 demo/src/ | clone 後 `dotnet build` 立即可用 |
| scaffold 行為 | 複製 demo/ → src/ + 覆蓋 .sln | 讓 .sln 改指向 src/，無縫切換開發區 |
| speckit.scaffold | 獨立 agent（不被 speckit init 覆蓋） | 放在 `.github/agents/speckit.scaffold.agent.md` |

### 完成項目

- [x] 複製 src/ → demo/（完整 Sample + demo 專用 .sln）
- [x] 清空 src/（scaffold 前完全為空）
- [x] 重建根目錄 .sln（指向 demo/src/*.csproj）
- [x] 建立 `speckit.scaffold.agent.md`（掃描 ops-docs → 詢問代號 → 複製 demo → src）
- [x] 更新 ERP 補充規則（speckit.specify.md + speckit.implement.md — scaffold 前置檢查）
- [x] 更新 AGENTS.md、CLAUDE.md（目錄結構、常用命令、SDD 流程）
- [x] 更新 GEMINI.md（同步新結構）
- [x] 更新 README.md（架構圖、快速開始、完整目錄、SDD 流程）
- [x] 更新 getting-started.md（Phase 2 目錄結構、Phase 3 demo 執行命令、Phase 4 SDD 加入 scaffold 步驟）
- [x] 驗證 demo/ 獨立 build 通過
- [x] 驗證根目錄 `dotnet build` 通過（指向 demo/src/）
- [x] .gitignore 確認合理
