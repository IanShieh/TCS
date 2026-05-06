
---

## 鼎新 ERP 作業轉換 — 專屬指引

> 本專案是鼎新 ERP 作業轉換模板，以下額外規則優先於上方通用指引。

### ERP 專屬分析重點

在進行跨文件一致性分析時，額外檢查：

- **憲法雙版本一致性**：英文版 `constitution.md` 和繁中版 `constitution-zh-TW.md` 的原則是否一致
- **char 欄位 IsFixedLength**：spec 中標註為 `char` 的欄位，在 plan/tasks 中是否有對應的 `IsFixedLength()` 設定任務
- **CrudResult<T> 統一回傳**：plan 中的 Service 設計是否使用 `CrudResult<T>`
- **單頭/單身 CRUD 分離**：是否在 API 端點設計中正確分離
- **MappingExtensions**：是否使用手動映射而非 AutoMapper
- **審計欄位**：是否有 `IAuditableEntity` 的實作任務
