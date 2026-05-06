
---

## 鼎新 ERP 作業轉換 — 專屬指引

> 本專案是鼎新 ERP 作業轉換模板，以下額外規則優先於上方通用指引。

### ⚠️ 前置確認（規格撰寫前需確認）

在開始撰寫規格前，確認以下事項並告知使用者：

1. 執行 `git remote get-url origin` 確認目前工作目錄
2. 若為 `DingxinErpTemplate` 模板根目錄（URL 含 `DingxinErpTemplate`）：
   - 檢查 `src/` 是否有專案檔案（`src/DingxinErp.Core/` 等目錄是否存在）：
     - **src/ 是空的** → 提示使用者：「src/ 尚未初始化。請先執行 `/speckit.scaffold` 從 demo/ 複製範例專案到 src/，再繼續 SDD 流程。」
     - **src/ 已有內容** → 直接繼續 SDD
   - `speckit.specify` / `speckit.plan` / `speckit.tasks` 僅產生文件，可在模板內執行
   - `speckit.implement` 會修改 src/ 中的程式碼，需確認 scaffold 已完成
3. 若已在其他作業目錄中 → 直接繼續，無需額外動作

### 語言規範

- **所有規格文件使用繁體中文**撰寫
- Entity 屬性名使用 ERP 原始欄位名（如 `TA001`、`TB003`）
- 類別名/方法名使用英文

### ERP 作業結構

- 每個 ERP 作業必須確認：
  - **單頭表格名**（如 PURTA）+ **單身表格名**（如 PURTB）
  - **複合主鍵結構**（通常 2-3 個欄位，如 TA001 + TA002）
  - **單頭/單身 FK 關係**
  - **欄位型態**：特別注意 `char` vs `nvarchar`、`decimal` 精度
- **char 欄位**必須標註為 `IsFixedLength()` + `IsUnicode(false)`

### 作業文件掃描

- 在 Round 1 時，**主動掃描 `ops-docs/` 目錄**尋找使用者放入的 ERP 截圖和文件
- 確認作業名稱後，自動建立 `ops-docs/{代號}-{名稱}/` 資料夾
- 圖片歸入 `screenshots/`、文件歸入 `documents/`
- 從截圖中辨識欄位名稱、型態、PK/FK、下拉選單

### 資料庫查詢

- 在 Round 2 確認表格結構時，**主動使用 `ops-docs/db-query.ps1`** 查詢實際資料庫
- 執行方式：`powershell -File ops-docs/db-query.ps1 -Query "YOUR_SQL_QUERY"`
- **查詢表格 Schema（推薦）**：使用鼎新系統 metadata 表 `DSCSYS.dbo.ADMMD`，一次取得欄位名、中文說明、型別、長度：
  ```sql
  SELECT MD001, MD002, MD003, MD004, MD005, MD006
  FROM DSCSYS.dbo.ADMMD WHERE MD001 = 'TABLE_NAME' ORDER BY MD002
  ```
  - MD001=表格名, MD002=序號, MD003=欄位名, MD004=中文說明, MD005=型態(C=char, N=numeric, V=varchar), MD006=長度
- 將 ADMMD 查詢結果與截圖分析交叉比對，產出精確的欄位規格
- 可搭配 `INFORMATION_SCHEMA.COLUMNS` 查詢 NULL 約束、PK/FK 等補充資訊

### 範例參考

- 參考現有的 `src/DingxinErp.Core/Entities/SampleHeader.cs` 和 `SampleDetail.cs` 了解 Entity 結構
- 參考 `AGENTS.md` 了解完整的架構規範和 API 端點格式
