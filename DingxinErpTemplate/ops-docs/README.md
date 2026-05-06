# ops-docs — ERP 作業原始文件存放區

此資料夾用於存放鼎新 ERP 各作業的原始畫面截圖、說明文件與客製化需求。
**使用 Copilot CLI Skill（方式 A）時，AI 會自動讀取此資料夾的內容，分析表格結構與欄位。**

## 📁 使用方式

你**不需要**事先建立特定命名的資料夾。只要把截圖和文件丟進 `ops-docs/` 即可：

```
ops-docs/
├── 畫面截圖1.png        ← 直接丟在根目錄也可以
├── 作業說明.pdf
├── 欄位清單.xlsx
└── 任意子資料夾/         ← 或放在自己建的資料夾裡
    └── 更多截圖.jpg
```

**AI 會自動完成以下工作：**
1. 掃描 `ops-docs/` 中所有檔案
2. 分析截圖內容，識別 ERP 作業名稱和代號
3. 自動建立正確命名的資料夾（如 `PURTA-採購單建立/`）
4. 將圖片移入 `screenshots/`，文件移入 `documents/`
5. 產生初版 `requirements.md`

整理後的結構：
```
ops-docs/
└── PURTA-採購單建立/              ← AI 自動命名
    ├── screenshots/               ← AI 自動分類
    │   ├── 畫面截圖1.png
    │   └── 更多截圖.jpg
    ├── documents/                 ← AI 自動分類
    │   ├── 作業說明.pdf
    │   └── 欄位清單.xlsx
    └── requirements.md           ← AI 根據分析結果產生

## 📁 進階用法：預先整理

若你想自行整理，也可以手動建立資料夾結構：

```
ops-docs/
└── [作業代號]-[作業中文名稱]/
    ├── screenshots/           # 放置 ERP 畫面截圖 (.png / .jpg)
    ├── documents/             # 放置說明文件 (.pdf / .docx / .xlsx)
    └── requirements.md        # 填寫客製化需求（複製下方模板）
```

**命名範例：**
- `PURTA-採購單建立/`
- `SATRA-銷貨單建立/`
- `STKA-庫存異動/`

---

## 📋 requirements.md 模板

若要自行填寫客製化需求，可複製以下模板。
（使用方式 A 時 AI 會自動產生，不需要手動建立。）

```markdown
# [作業名稱] 客製化需求說明

## 基本資訊

- 作業中文名稱：
- 鼎新 ERP 作業代號：
- 單頭表格名稱：
- 單身表格名稱（如有）：
- 預計開發者：
- 預計完成日期：

## 欄位補充說明

> 若截圖或文件中看不到的隱藏欄位或特殊說明，在此補充

| 欄位代號 | 說明 | 備註 |
|----------|------|------|
|          |      |      |

## 客製化需求

### 計算規則
- [ ] 無
- [ ] 有，說明：

### 下拉選單
- [ ] 無
- [ ] 有，說明（來源表格、顯示欄位）：

### 欄位連動
- [ ] 無
- [ ] 有，說明（觸發欄位 → 自動填入欄位）：

### 特殊驗證規則
- [ ] 無
- [ ] 有，說明：

### 其他需求
-
```

---

## � 查詢 ERP 資料庫表格結構

本目錄提供 `db-query.ps1` 腳本，讓 AI 或開發者直接查詢鼎新 ERP 測試資料庫的表格結構和資料。

### 首次設定

```powershell
# 從範本建立本機版本（只需做一次）
Copy-Item ops-docs/db-query.ps1.example ops-docs/db-query.ps1

# 編輯 db-query.ps1，填入實際的連線資訊（Server/User/Password/Database）
```

> ⚠️ `db-query.ps1` 包含機密連線資訊，已加入 `.gitignore`，**不會上傳到 GitHub**。

### 使用方式

```powershell
powershell -File ops-docs/db-query.ps1 -Query "YOUR_SQL_QUERY"
```

### AI 使用情境

在 SDD 流程中，AI 會在以下階段使用此工具：
- **/speckit.specify** Round 2：自動查詢表格欄位結構，產出精確的規格書
- **/speckit.implement**：驗證 Entity 欄位定義與資料庫一致
- **除錯**：查詢實際資料確認業務規則

---

## �📌 範例

`ops-docs/_example-PURTA-採購單建立/` 為示範用範例資料夾（以 `_example-` 前綴區別），展示 AI 自動整理後的最終結構。

**最簡單的使用流程：**

1. 把截圖和文件丟進 `ops-docs/`（不需分類、不需命名）
2. 在 Copilot CLI 說：
   ```
   > 我要轉換鼎新ERP的採購單作業
   ```
3. AI 會自動：
   - 掃描 `ops-docs/` 所有檔案
   - 分析截圖判斷作業名稱和代號
   - 建立 `PURTA-採購單建立/` 並分類檔案
   - 產生 `requirements.md`
   - 進入 Round 2 請你確認欄位結構
