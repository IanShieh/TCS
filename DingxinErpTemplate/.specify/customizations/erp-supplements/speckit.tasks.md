
---

## 鼎新 ERP 作業轉換 — 專屬指引

> 本專案是鼎新 ERP 作業轉換模板，以下額外規則優先於上方通用指引。

### Phase 結構（固定）

此專案的任務固定按以下 Phase 分組：

- **Phase 1: 基礎設定** — Entity (單頭+單身)、Configuration (IsFixedLength)、DTOs + MappingExtensions
- **Phase 2: 業務邏輯** — IRepository + Repository、IService + Service、FluentValidation Validators
- **Phase 3: API + 前端** — API Controller (CRUD + 搜尋 + 單身)、MVC Controller、Index.cshtml、導覽列
- **Phase 4: 整合 + 測試** — DI 註冊 (Program.cs)、單元測試、整合測試

### 檔案路徑範例

```
src/[Proj].Core/Entities/{Header}.cs          # 單頭 Entity
src/[Proj].Core/Entities/{Detail}.cs          # 單身 Entity
src/[Proj].Infrastructure/Configurations/     # EF Core Configuration
src/[Proj].Core/DTOs/                         # DTO + MappingExtensions
src/[Proj].Core/Interfaces/                   # IRepository + IService
src/[Proj].Infrastructure/Repositories/       # Repository 實作
src/[Proj].Core/Services/                     # Service 實作
src/[Proj].Core/Validators/                   # FluentValidation
src/[Proj].Web/Controllers/                   # API + MVC Controller
src/[Proj].Web/Views/{作業名}/Index.cshtml     # CRUD 頁面
src/[Proj].Web/Program.cs                     # DI 註冊
```
