
---

## 鼎新 ERP 作業轉換 — 專屬指引

> 本專案是鼎新 ERP 作業轉換模板，以下額外規則優先於上方通用指引。

### ERP 專屬驗收項目

除了上方的需求品質檢核外，還需要包含以下 ERP 專屬分類：

**ERP 資料層品質**：
- char 欄位是否都有 `IsFixedLength()` + `IsUnicode(false)` 的需求定義？
- 複合主鍵結構是否明確指定？
- 單頭/單身 FK 關係是否完整定義？

**ERP 架構合規**：
- 是否遵循 Clean Architecture 三層分離？
- 是否使用 `CrudResult<T>` 統一回傳格式？
- 單頭/單身 CRUD 是否完全分離？
- 是否使用手動 MappingExtensions（非 AutoMapper）？

**ERP 前端品質**：
- 單頭行點擊自動選取是否有需求定義？
- 單身連動是否有需求定義？
- 搜尋 + 分頁是否有需求定義？

### 驗證命令

在驗收結束時，執行以下命令確認品質：
- `dotnet build` — 編譯通過（0 errors, 0 warnings）
- `dotnet test` — 單元測試全部通過
