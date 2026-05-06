
---

## 鼎新 ERP 作業轉換 — 專屬指引

> 本專案是鼎新 ERP 作業轉換模板，以下額外規則優先於上方通用指引。

### 憲法雙版本管理

本專案的 constitution 有**英文版**和**繁體中文版**：
- `.specify/memory/constitution.md` — 英文版（供 spec-kit 自動化流程使用）
- `.specify/memory/constitution-zh-TW.md` — 繁體中文版（供團隊閱讀參考）

### 修訂規則

- 修訂時**先更新英文版**，再**同步更新中文版**
- 兩個版本的原則內容必須保持一致
- 版本號和修訂日期同步更新

### 核心原則（6 大原則）

本專案的憲法包含以下核心原則，請在修訂時確認不被意外刪除：

1. **模組化架構** — Clean Architecture 三層分離
2. **文件優先** — 繁體中文文件 + 英文程式碼
3. **設定驅動** — char 欄位 `IsFixedLength()` + `IsUnicode(false)`
4. **服務導向** — `CrudResult<T>` 統一回傳、單頭/單身 CRUD 分離
5. **使用者中心** — 傳統 Web Form 風格、Header-Detail 連動
6. **語言規範** — 文件繁中、程式碼英文
