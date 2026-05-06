
---

## 鼎新 ERP 作業轉換 — 專屬指引

> 本專案是鼎新 ERP 作業轉換模板，以下額外規則優先於上方通用指引。

### ⚠️ 前置安全檢查（實作前必做）

**在執行任何實作任務之前**，必須先確認開發環境已就緒：

```bash
git remote get-url origin
```

判斷規則：
- 若回傳 URL 包含 `DingxinErpTemplate`（在模板根目錄）：
  - 檢查 `src/DingxinErp.Core/` 目錄是否存在：
    - **不存在** → **立刻停止**，提示：
      > 「src/ 是空的，尚未執行 scaffold。請先執行 `/speckit.scaffold` 從 demo/ 複製範例專案到 src/，再繼續。」
    - **存在** → scaffold 已完成，允許繼續
- 若回傳 URL 是其他 repo（獨立作業專案）→ 正常繼續

### 實作參考（必讀）

每個任務實作前，**必須參考 `demo/` 中的範例檔案**（永久唯讀參考）：

| 任務類型 | 參考檔案 |
|----------|---------|
| 新 Entity | `demo/src/DingxinErp.Core/Entities/SampleHeader.cs` / `SampleDetail.cs` |
| 新 Configuration | `demo/src/DingxinErp.Infrastructure/Configurations/SampleHeaderConfiguration.cs` |
| 新 DTO | `demo/src/DingxinErp.Core/DTOs/SampleHeaderDto.cs` + `MappingExtensions.cs` |
| 新 Repository | `demo/src/DingxinErp.Infrastructure/Repositories/SampleRepository.cs` |
| 新 Service | `demo/src/DingxinErp.Core/Services/SampleService.cs` |
| 新 Validator | `demo/src/DingxinErp.Core/Validators/CreateSampleHeaderValidator.cs` |
| 新 Controller | `demo/src/DingxinErp.Web/Controllers/SampleController.cs` (API) + `SamplePageController.cs` (MVC) |
| 新 View | `demo/src/DingxinErp.Web/Views/Sample/Index.cshtml` |
| 前端 JS | `demo/src/DingxinErp.Web/wwwroot/js/crud-common.js` (通用) + `master-detail.js` (單身) |

### 實作規範

- **char 欄位**：`IsFixedLength()` + `IsUnicode(false)` — 否則 EF Core 產生 `N''` 前綴導致查詢失敗
- **統一回傳**：所有 Service 方法回傳 `CrudResult<T>`
- **手動映射**：使用 `MappingExtensions`（`ToDto`/`ToEntity`/`UpdateFrom`），不使用 AutoMapper
- **審計欄位**：實作 `IAuditableEntity`（CREATOR/CREATE_DATE/MODIFIER/MODI_DATE/FLAG）
- **DI 註冊**：完成後更新 `src/DingxinErp.Web/Program.cs`
- **驗證命令**：每個任務完成後執行 `dotnet build` 確認編譯通過

### 資料庫查詢

- 實作 Entity 時，**主動使用 `ops-docs/db-query.ps1`** 查詢實際資料庫驗證欄位定義
- 執行方式：`powershell -File ops-docs/db-query.ps1 -Query "YOUR_SQL_QUERY"`
- **查詢表格 Schema（推薦）**：使用 `DSCSYS.dbo.ADMMD` 取得欄位中文名與型別：
  ```sql
  SELECT MD003, MD004, MD005, MD006 FROM DSCSYS.dbo.ADMMD WHERE MD001 = 'TABLE_NAME' ORDER BY MD002
  ```
  - MD003=欄位名, MD004=中文說明, MD005=型態(C/N/V), MD006=長度
- 用於確認欄位型態、長度、PK/FK 與規格書一致

### 前端規範

- JavaScript 中存取 API 回傳資料用 PascalCase：`item.TA001`、`data.Items`、`result.Success`
- 使用 `crud-common.js` 的通用函式（Toast、分頁、搜尋、Modal、儲存）
- 單身連動使用 `master-detail.js`
