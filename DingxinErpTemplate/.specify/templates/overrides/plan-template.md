# 技術計畫: [FEATURE_NAME]

**分支**: `[###-feature-name]` | **日期**: [DATE] | **規格**: [link to spec.md]
**輸入**: 功能規格 `specs/[###-feature-name]/spec.md`

## 摘要

**功能:** [來自 spec.md 的功能描述]
**技術方向:** Clean Architecture + EF Core + Bootstrap MVC

## 技術環境

| 項目 | 值 |
|------|------|
| 語言/版本 | C# / .NET 8.0 |
| 資料庫 | SQL Server |
| ORM | EF Core 8.0 |
| 前端 | Bootstrap 5 + jQuery (CDN) |
| 驗證 | FluentValidation |
| 測試 | xUnit + Moq |
| 回傳型別 | `CrudResult<T>` / `PagedResult<T>` |
| 審計欄位 | `IAuditableEntity`（CREATOR/CREATE_DATE/MODIFIER/MODI_DATE/FLAG） |
| 欄位映射 | 手動 `MappingExtensions`（不使用 AutoMapper） |

## 憲法檢核

*閘道：必須全部通過才進入 Phase 0。實作後重新檢查。*

- [ ] **I. Modular Architecture**: Clean Architecture 三層分離
- [ ] **II. Documentation-First**: 所有 Entity/API 完整文件化
- [ ] **III. Configuration-Driven**: char IsFixedLength + IsUnicode 正確設定
- [ ] **IV. Service-Oriented**: CrudResult<T> 統一回傳 / 介面隔離
- [ ] **V. User-Centric**: 傳統 Web Form 式 UI / Bootstrap 5
- [ ] **VI. Language Convention**: 繁中文件 + 英文程式碼

## 文件結構

```
specs/[###-feature]/
├── plan.md              # 本檔案 (/speckit.plan 產出)
├── spec.md             # 規格書 (/speckit.specify 產出)
├── research.md         # Phase 0 產出 — NEEDS CLARIFICATION 解析
├── data-model.md       # Phase 1 產出 — Entity 定義（單頭+單身）
├── contracts/          # Phase 1 產出 — API 表跟約（若適用）
├── checklists/         # 驗收清單
└── tasks.md            # Phase 2 產出 (/speckit.tasks 指令)
```

## 專案結構

```
src/
├── [Project].Web/          # Controllers + Views + wwwroot
├── [Project].Core/         # Entities + DTOs + Services + Validators
└── [Project].Infrastructure/ # DbContext + Repositories
tests/
└── [Project].Core.Tests/   # 單元測試
```

## 實作方向

### 後端
1. Entity 定義 (對應 ERP 表格)
2. DbContext + Configuration (char IsFixedLength)
3. Repository (CRUD + 分頁 + 搜尋)
4. Service (業務邏輯 + 審計欄位)
5. Validator (FluentValidation)
6. Controller (API endpoints)

### 前端
1. Index.cshtml (搜尋 + 表格 + Modal)
2. 單頭單身連動 JS
3. CRUD 通用 JS (Toast + 鍵盤快捷鍵)

## Complexity Tracking

> **僅在憲法檢核有違反且需要說明理由時填寫**

| 違反項目 | 為什麼必要 | 更簡單方案被拒絕的原因 |
|---------|----------|-----------------------------------|
| [例：第 4 個專案] | [目前需求] | [為何 3 個專案不夠用] |