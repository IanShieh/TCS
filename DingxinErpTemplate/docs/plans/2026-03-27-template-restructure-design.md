# 設計文件：模板結構重組 + SDD Scaffold 機制

**日期**: 2026-03-27
**狀態**: 已確認

---

## 1. 目標

將 DingxinErpTemplate 的目錄結構重組，讓團隊 clone 後可以直接在根目錄執行 SDD 流程開發新的 ERP 作業，不需要手動建立子專案或切換工作目錄。

## 2. 需求摘要（brainstorming 確認結果）

| 決策項目 | 確認結果 |
|----------|----------|
| 觸發方式 | speckit.specify 偵測到在模板根目錄 → 詢問確認後執行 scaffold |
| 架構變更 | `demo/` = Sample 範例（唯讀參考），`src/` = 團隊開發新作業 |
| 複製範圍 | 完整模板（可 build 的專案） |
| 作業代號來源 | agent 分析 ops-docs 後互動詢問使用者 |
| Sample 處理 | 保留在 src/ 供 SDD 參考，使用者完成後自行決定是否清理 |
| 實作位置 | 新增 `speckit.scaffold.agent.md` + 修改 ERP 補充規則 |
| 不動的檔案 | `speckit.specify.agent.md`（speckit init 會覆蓋） |
| SDD 執行位置 | 模板根目錄（spec-kit 所有檔案都在根目錄，直接跑） |

## 3. 目錄結構變更

### 3.1 現行結構

```
DingxinErpTemplate/
├── DingxinErpTemplate.sln    → references src/ projects
├── src/
│   ├── DingxinErp.Core/      ← Sample 範例 + 基礎設施
│   ├── DingxinErp.Infrastructure/
│   └── DingxinErp.Web/
├── tests/
├── ops-docs/
├── .specify/
└── .github/agents/
```

### 3.2 新結構（模板 repo on GitHub）

```
DingxinErpTemplate/
├── DingxinErpTemplate.sln    → references src/ projects（空骨架，可 build）
├── src/
│   ├── DingxinErp.Core/      ← 空骨架（Common/, Interfaces/ 保留；無 Sample Entity/DTO/Service）
│   ├── DingxinErp.Infrastructure/ ← 空骨架（Data/AppDbContext.cs 保留；無 Sample Config/Repo）
│   └── DingxinErp.Web/       ← 空骨架（Program.cs, Middleware/ 保留；無 Sample Controller/View）
├── tests/
│   └── DingxinErp.Core.Tests/ ← 空骨架
├── demo/
│   └── reference/             ← 完整 Sample 範例（可獨立 build）
│       ├── DingxinErpTemplate.sln
│       ├── src/
│       │   ├── DingxinErp.Core/       ← SampleHeader, SampleDetail, DTOs, Service, Validator
│       │   ├── DingxinErp.Infrastructure/ ← Configurations, Repository
│       │   └── DingxinErp.Web/        ← Controllers, Views, js
│       └── tests/
├── ops-docs/
├── specs/
├── .specify/
└── .github/agents/
    ├── speckit.*.agent.md     ← 原生 spec-kit agents（不修改）
    └── speckit.scaffold.agent.md ← 新增：ERP scaffold agent
```

### 3.3 scaffold 執行後的結構（使用者的工作狀態）

```
DingxinErpTemplate/（clone 後）
├── src/
│   ├── DingxinErp.Core/
│   │   ├── Common/            ← CrudResult, PagedResult, IAuditableEntity
│   │   ├── Entities/          ← SampleHeader + SampleDetail（從 demo/ 複製來，供參考）
│   │   ├── DTOs/              ← Sample DTOs（供參考）
│   │   ├── Services/          ← SampleService（供參考）
│   │   ├── Validators/        ← Sample Validators（供參考）
│   │   └── Interfaces/        ← ISampleRepository, ISampleService（供參考）
│   ├── DingxinErp.Infrastructure/
│   │   ├── Data/              ← AppDbContext（含 Sample DbSet）
│   │   ├── Configurations/    ← Sample Configurations（供參考）
│   │   └── Repositories/      ← SampleRepository（供參考）
│   └── DingxinErp.Web/
│       ├── Controllers/       ← Sample Controllers（供參考）
│       ├── Views/             ← Sample Views（供參考）
│       ├── Middleware/
│       └── Program.cs         ← 含 Sample DI 註冊
├── ops-docs/
│   └── {作業文件}/
└── specs/                     ← SDD 產出的規格文件
```

## 4. speckit.scaffold.agent.md 設計

### 4.1 觸發方式

- 使用者直接呼叫 `/speckit.scaffold`
- 或由 `speckit.specify.md` ERP 補充規則自動引導

### 4.2 執行步驟

```
1. 確認位置
   ├── git remote get-url origin
   ├── 不含 DingxinErpTemplate → 警告，詢問是否繼續
   └── 含 DingxinErpTemplate → 繼續

2. 掃描 ops-docs/
   ├── 列出所有子資料夾
   ├── 讀取 erp-doc.md / requirements.md
   └── 推斷作業名稱、模組類別、主要表格

3. 顯示分析結果，詢問作業代號
   └── 等待使用者輸入（例如 PURI01）

4. 確認執行動作
   ├── 顯示來源（demo/reference/src/）
   ├── 顯示目標（src/）
   └── 等待 yes/no

5. 執行複製
   ├── 複製 demo/reference/src/ 的所有內容 → src/
   ├── 複製 demo/reference/tests/ → tests/
   └── 驗證 dotnet build 通過

6. 完成提示
   └── 「src/ 已就緒，請繼續執行 /speckit.specify {作業描述}」
```

## 5. speckit.specify.md 補充規則變更

### 5.1 「前置確認」區塊修改

```
現有邏輯：
  偵測到在模板根目錄 → 提醒使用者 clone

新邏輯：
  偵測到在模板根目錄 →
    檢查 src/ 是否為空骨架（無 Sample Entity）→
      YES → 提示：「src/ 尚未初始化，建議先執行 /speckit.scaffold」
      NO  → src/ 已有內容，直接繼續 SDD
```

### 5.2 speckit.implement.md 安全檢查

維持現有邏輯：
- 偵測到在模板根目錄 + src/ 只有空骨架 → 停止
- 偵測到在模板根目錄 + src/ 已有 Sample 內容 → 允許繼續（scaffold 已執行）

## 6. 變動檔案清單

| 檔案 | 動作 | 說明 |
|------|------|------|
| `demo/reference/` | **新增** | 從 src/ 完整搬入 Sample 範例 |
| `demo/reference/DingxinErpTemplate.sln` | **新增** | demo 專屬 .sln，可獨立 build |
| `src/DingxinErp.Core/` | **修改** | 移除 Sample Entity/DTO/Service/Validator，保留 Common/ Interfaces/ |
| `src/DingxinErp.Infrastructure/` | **修改** | 移除 Sample Config/Repo，保留 Data/AppDbContext（空 DbSet） |
| `src/DingxinErp.Web/` | **修改** | 移除 Sample Controller/View，保留 Program.cs Middleware/ |
| `tests/DingxinErp.Core.Tests/` | **修改** | 移除 Sample 測試 |
| `.github/agents/speckit.scaffold.agent.md` | **新增** | scaffold agent |
| `.specify/customizations/erp-supplements/speckit.specify.md` | **修改** | 前置確認邏輯更新 |
| `.specify/customizations/erp-supplements/speckit.implement.md` | **修改** | 安全檢查邏輯更新 |
| `AGENTS.md` | **修改** | 目錄結構說明更新 |
| `CLAUDE.md` | **修改** | 同上 |
| `README.md` | **修改** | 使用說明更新 |
| `.gitignore` | **修改** | 確保 demo/ 可提交 |

## 7. 不變動的檔案

- `.github/agents/speckit.specify.agent.md` — speckit init 會覆蓋
- `.github/agents/speckit.implement.agent.md` — speckit init 會覆蓋
- `.specify/` 核心框架（templates, scripts, memory）

## 8. 驗證標準

1. 模板 repo（GitHub 上）`dotnet build` 通過（src/ 空骨架可編譯）
2. `demo/reference/` 內可獨立 `dotnet build` 通過
3. scaffold 執行後 `src/` 可 `dotnet build` 通過
4. `/speckit.specify` 在模板根目錄能正確偵測 src/ 狀態並引導
5. `/speckit.implement` 安全檢查正常運作
