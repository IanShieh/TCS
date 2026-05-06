
---

## 鼎新 ERP 作業轉換 — 專屬指引

> 本專案是鼎新 ERP 作業轉換模板，以下額外規則優先於上方通用指引。

### 技術架構（已確定）

- **Clean Architecture 三層**：Web → Core ← Infrastructure
- **.NET 8** + EF Core + FluentValidation
- **前端**：Bootstrap 5 + jQuery（CDN）、傳統 Web Form 風格
- **JSON 序列化**：`PropertyNamingPolicy = null`（保持 PascalCase）
- **不使用 AutoMapper**：使用手動 `MappingExtensions`（`ToDto`/`ToEntity`/`UpdateFrom`）
- **不使用 Mini API**：僅使用 Controller-based API

### ERP 專屬規範

- `char` 欄位必須設定 `IsFixedLength()` + `IsUnicode(false)`
- 統一回傳 `CrudResult<T>`（Success/Message/Data/Errors）
- 分頁回傳 `PagedResult<T>`
- 審計欄位透過 `IAuditableEntity` 統一管理
- 單頭/單身 CRUD **完全分離**：各自獨立的 API 端點

### 參考範例

- Entity: `src/DingxinErp.Core/Entities/SampleHeader.cs` / `SampleDetail.cs`
- Configuration: `src/DingxinErp.Infrastructure/Configurations/SampleHeaderConfiguration.cs`
- Service: `src/DingxinErp.Core/Services/SampleService.cs`
- Repository: `src/DingxinErp.Infrastructure/Repositories/SampleRepository.cs`
- Controller: `src/DingxinErp.Web/Controllers/SampleController.cs`
- View: `src/DingxinErp.Web/Views/Sample/Index.cshtml`

### 資料庫查詢

- 規劃時可查詢鼎新系統 metadata 確認表格結構：
  ```sql
  SELECT MD003, MD004, MD005, MD006 FROM DSCSYS.dbo.ADMMD WHERE MD001 = 'TABLE_NAME' ORDER BY MD002
  ```
  - MD003=欄位名, MD004=中文說明, MD005=型態(C/N/V), MD006=長度
- 執行方式：`powershell -File ops-docs/db-query.ps1 -Query "YOUR_SQL_QUERY"`

### 環境收集

- 執行 `.specify/scripts/powershell/setup-plan.ps1` 收集 .NET SDK 版本、NuGet 套件等技術環境
