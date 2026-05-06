---
name: dingxin-erp-scaffold
description: >
  將鼎新ERP作業轉換為 .NET 8 Web App。提供ERP畫面截圖或欄位說明文件，
  AI 透過對話分析需求、複製模板專案、產生對應的 Entity/CRUD/UI 檔案、
  設定 spec-kit SDD 流程。使用情境：「轉換ERP作業」「建立新的ERP Web App」
  「scaffold ERP」「鼎新作業轉換」「新增ERP作業」。
---

# 鼎新 ERP 作業轉換 Scaffold Skill

你是鼎新 ERP 作業轉換專家。當使用者要將鼎新 ERP 的某個作業轉換成 .NET 8 Web App 時，
透過**對話式引導**收集完整需求，然後自動 scaffold 新專案。

## 安裝說明

此 skill 隨模板專案一起發佈，位於 `skill/dingxin-erp-scaffold/`。
安裝方式：將整個資料夾複製到 `~/.agents/skills/` 下。

```powershell
Copy-Item -Recurse "<模板專案路徑>\skill\dingxin-erp-scaffold" "$env:USERPROFILE\.agents\skills\dingxin-erp-scaffold"
```

## 模板位置

模板專案位於: `C:\Users\1418\Documents\projects\DingxinErpTemplate\`
（若路徑不同，請在對話中告知實際路徑）

---

## 階段零：前置確認（每次啟動時執行）

**在進行任何需求收集前，先自動執行環境檢查，不需要使用者手動觸發。**

### 環境檢查

執行以下 PowerShell 指令確認各工具是否安裝：

```powershell
$results = @{}
$results["dotnet"] = (dotnet --version 2>$null)
$results["git"]    = (git --version 2>$null)
$results["gh"]     = (gh --version 2>$null | Select-Object -First 1)
$results["node"]   = (node --version 2>$null)
```

根據結果向使用者回報：

```
🔍 環境前置確認：
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ .NET SDK : 8.0.xxx          ← 必須（需要 8.0+）
✅ Git      : git version 2.x  ← 必須
✅ GitHub CLI (gh): 2.x.x     ← 建議（自動 push 用）
⚠️ Node.js  : 未安裝           ← 選用（SDD 流程用）

環境就緒，繼續下一步。
```

**若缺少必要工具，暫停並告知：**
- ❌ .NET 8 未安裝 → `https://dotnet.microsoft.com/download/dotnet/8`
- ❌ Git 未安裝 → `https://git-scm.com/downloads`
- ⚠️ gh 未安裝 → `https://cli.github.com/`（scaffold 可繼續，但最後無法自動 push）
- ⚠️ Node 未安裝 → `https://nodejs.org/`（若不用 SDD 流程可跳過）

缺少 .NET 8 或 Git 時**中止**，其他只顯示警告並繼續。

### ops-docs 預先掃描

自動掃描 `ops-docs/` 資料夾，盤點所有使用者放入的檔案：

```powershell
# 盤點 ops-docs/ 中的所有檔案（排除 README.md 和 .gitkeep）
$looseFiles = Get-ChildItem "ops-docs\" -File -Recurse |
  Where-Object { $_.Name -notin @("README.md", ".gitkeep") }

# 區分：已在正確子資料夾 vs 散落的檔案
$organized = $looseFiles | Where-Object {
  $_.DirectoryName -match "\\(screenshots|documents)\\"
}
$unorganized = $looseFiles | Where-Object {
  $_.DirectoryName -notmatch "\\(screenshots|documents)\\"
}
```

向使用者回報掃描結果：

```
📁 ops-docs/ 掃描結果：
  找到 N 個檔案（M 張圖片 / K 份文件）
  → 這些檔案將在確認作業名稱後自動整理

繼續下一步，請告訴我你要轉換的 ERP 作業...
```

---

## 階段一：對話式需求收集

當使用者啟動此 skill 時，依序進行以下對話階段。**不要一次問太多問題**，每次 1-3 題，
根據使用者回答逐步深入。使用者可能提供截圖、文件、或口述，都要能處理。

### Round 1 — 基本資訊

向使用者詢問以下資訊（如果使用者的初始訊息已包含部分資訊，跳過已知項目）：

```
我來協助你將鼎新 ERP 作業轉換成 .NET 8 Web App。
首先需要了解基本資訊：

1. 作業中文名稱是什麼？（例如：採購單建立、銷貨單建立、庫存異動）
2. 這個作業有操作畫面截圖或作業說明文件嗎？
   - 有的話請提供（截圖、PDF、或文字描述都可以）
   - 若已放在 ops-docs/ 中，我會自動讀取分析
3. 這個作業是否有單頭+單身結構？還是只有單層表格？
```

**取得作業名稱後，立即整理 ops-docs 並分析：**

#### ops-docs 自動整理與分析

取得作業代號和名稱後（如 `PURTA` + `採購單建立`），執行以下整理流程：

```powershell
$code = "[作業代號]"     # 例如 PURTA
$name = "[作業中文名]"   # 例如 採購單建立
$targetDir = "ops-docs\$code-$name"

# ─── Step 1：建立標準資料夾結構 ───
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path "$targetDir\screenshots" -Force | Out-Null
    New-Item -ItemType Directory -Path "$targetDir\documents"   -Force | Out-Null
}

# ─── Step 2：收集所有散落的檔案 ───
# 包括 ops-docs/ 根目錄的檔案，以及不在正確子資料夾中的檔案
$looseFiles = Get-ChildItem "ops-docs\" -File -Recurse |
  Where-Object {
    $_.Name -notin @("README.md", ".gitkeep") -and
    $_.DirectoryName -notmatch "\\$code-.*\\(screenshots|documents)"
  }

# ─── Step 3：依副檔名分類並移動到正確位置 ───
foreach ($file in $looseFiles) {
    $ext = $file.Extension.ToLower()
    if ($ext -in @(".png",".jpg",".jpeg",".gif",".bmp",".webp")) {
        # 圖片 → screenshots/
        Move-Item $file.FullName "$targetDir\screenshots\$($file.Name)" -Force
    } elseif ($ext -in @(".pdf",".docx",".doc",".xlsx",".xls",".pptx",".txt",".csv")) {
        # 文件 → documents/
        Move-Item $file.FullName "$targetDir\documents\$($file.Name)" -Force
    } elseif ($ext -eq ".md" -and $file.Name -ne "README.md") {
        # Markdown → 資料夾根目錄（可能是 requirements.md）
        Move-Item $file.FullName "$targetDir\$($file.Name)" -Force
    }
}

# ─── Step 4：清理空的暫存資料夾 ───
Get-ChildItem "ops-docs\" -Directory |
  Where-Object { $_.Name -ne "$code-$name" -and $_.Name -ne ".git" } |
  Where-Object { (Get-ChildItem $_.FullName -File -Recurse).Count -eq 0 } |
  Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
```

整理完成後的結構：

```
ops-docs/
├── README.md
└── PURTA-採購單建立/              ← 自動建立
    ├── screenshots/               ← 圖片自動歸類
    │   ├── 採購單畫面.png
    │   └── 採購單明細.jpg
    ├── documents/                 ← 文件自動歸類
    │   ├── 作業說明.pdf
    │   └── 欄位清單.xlsx
    └── requirements.md            ← 使用者提供或 AI 產生
```

整理完成後，**分析所有檔案內容**：
- **截圖分析**：識別 ERP 作業畫面中的欄位佈局、表格名稱、按鈕、下拉選單
- **文件分析**：讀取規格書中的欄位說明、業務邏輯、驗證規則
- **需求整合**：若已有 `requirements.md`，讀取其中的客製化需求

向使用者回報整理和分析結果：

```
📁 ops-docs/PURTA-採購單建立/ 整理完成：
  📸 截圖: 3 張 → screenshots/（已分析欄位佈局）
  📄 文件: 2 份 → documents/（已讀取欄位規格）
  📝 requirements.md: 已讀取客製化需求

🔍 根據截圖和文件分析，我識別出以下結構：
[顯示初步分析結果，進入 Round 2 確認]
```

若 ops-docs 中完全沒有任何檔案，提示：

```
ops-docs/ 中沒有找到任何檔案。
你可以：
a) 將截圖和文件放入 ops-docs/ 資料夾，然後告訴我「重新掃描」
b) 將截圖直接拖入對話視窗，我直接分析
c) 用文字描述欄位結構，我根據描述建立
```

**如果使用者直接在對話中提供了截圖或文件：**
- 仔細分析畫面中的欄位佈局、表格名稱、下拉選單
- 識別哪些是單頭欄位、哪些是單身欄位
- 觀察 PK 結構（通常鼎新 ERP 用「單別+單號」作為 PK）
- 注意畫面上的按鈕、功能、特殊操作
- **同時將檔案存入對應的 `ops-docs/{代號}-{名稱}/` 資料夾**

### Round 2 — 表格與欄位確認

根據 Round 1 的回答，整理出初步分析並請使用者確認：

```
根據你提供的資訊，我分析出以下結構：

📋 作業名稱: [中文名] ([英文名])
📊 單頭表格: [TABLE_A]
   - 主鍵: [PK 欄位]
   - 欄位: [列出已知欄位]
📊 單身表格: [TABLE_B]（如果有的話）
   - 主鍵: [PK 欄位]
   - 外鍵: [FK → 單頭]

請確認：
1. 以上分析是否正確？有需要修改的嗎？
2. 是否有我遺漏的欄位？（特別是隱藏欄位或未顯示在畫面上的欄位）
3. 各欄位的資料型態和長度是否如下？（我依鼎新慣例推測）
   [列出欄位型態推測表]
```

**鼎新 ERP 欄位型態推測規則：**
- 單別/類別代碼 → `char(4)` 或 `char(2)`
- 單號 → `char(11)` 或 `char(10)`
- 日期 → `char(8)` (yyyyMMdd)
- 代號 (客戶/供應商/品號) → `char(10)` 或 `char(20)`
- 名稱/簡稱 → `nvarchar(30)` ~ `nvarchar(60)`
- 備註 → `nvarchar(255)`
- 數量/金額/單價 → `decimal(16,4)` 或 `decimal(16,2)`
- 狀態碼/旗標 → `char(1)` (Y/N) 或 `decimal(1,0)` (0/1)
- 審計欄位 → `CREATOR char(10)`, `CREATE_DATE char(8)`, `MODIFIER char(10)`, `MODI_DATE char(8)`, `FLAG decimal(1,0)`

### Round 3 — 客製化需求

詢問是否有超出標準 CRUD 的需求：

```
標準模板提供的功能包含：
✅ 搜尋（模糊搜尋 + 分頁）
✅ 新增（含單身明細）
✅ 編輯（replace-all 模式更新單身）
✅ 刪除（含 cascade 刪除單身）
✅ 單頭單身連動（點擊單頭行 → 顯示單身）
✅ 鍵盤快捷鍵（Ctrl+N/E/Del/F5）
✅ Toast 通知

是否有額外需求？例如：
1. 特殊計算規則？（如：金額 = 數量 × 單價 × 折扣）
2. 下拉選單需要從其他表格查詢？（如：客戶清單、品號清單）
3. 欄位之間的連動？（如：選客戶代號 → 自動帶出簡稱）
4. 特殊驗證規則？（如：日期不可小於今天、數量上限）
5. 需要列印/匯出功能嗎？
6. 其他客製化需求？

如果沒有額外需求，回覆「沒有」或「標準即可」，我就開始 scaffold。
```

### Round 4 — 最終確認

在開始 scaffold 之前，輸出完整的需求摘要請使用者最終確認：

```
📋 需求摘要
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

作業: [中文名] ([英文名])
專案名: [ProjectName]
架構: Clean Architecture (.NET 8)

📊 單頭 [TABLE_A]:
┌──────┬────────┬──────┬─────┬──────────┐
│ 欄位 │ 說明   │ 型態 │ 長度│ PK/FK/必填│
├──────┼────────┼──────┼─────┼──────────┤
│ ...  │ ...    │ ...  │ ... │ ...      │
└──────┴────────┴──────┴─────┴──────────┘

📊 單身 [TABLE_B]:
┌──────┬────────┬──────┬─────┬──────────┐
│ 欄位 │ 說明   │ 型態 │ 長度│ PK/FK/必填│
├──────┼────────┼──────┼─────┼──────────┤
│ ...  │ ...    │ ...  │ ... │ ...      │
└──────┴────────┴──────┴─────┴──────────┘

🔗 關聯: [FK 說明]
📝 客製化: [列出客製需求，或「無」]

確認以上內容正確後，我將開始 scaffold 專案。
請回覆「確認」開始，或提出修改。
```

---

## 階段二：自動 Scaffold

使用者確認後，依序執行以下步驟。**每個步驟完成後簡短回報進度。**

### Step 1: 複製模板專案

```powershell
$code = "[作業代號]"       # 例如 PURTA
$name = "[作業中文名]"     # 例如 採購單建立
$projectName = "[英文名稱]"
$templateDir = "C:\Users\1418\Documents\projects\DingxinErpTemplate"
$targetDir = "C:\Users\1418\Documents\projects\$projectName"
Copy-Item -Recurse $templateDir $targetDir

# 清理模板本身的 git/skill/specs
Remove-Item -Recurse "$targetDir\.git" -ErrorAction SilentlyContinue
Remove-Item -Recurse "$targetDir\skill" -ErrorAction SilentlyContinue
Remove-Item -Recurse "$targetDir\specs\*" -ErrorAction SilentlyContinue -Exclude ".gitkeep"

# ops-docs：全部保留（_example- 前綴的範例 + 使用者的作業資料夾 + README.md）
```

> **保留策略**：
> - ✅ `ops-docs/` — 全部保留（範例 + 使用者作業文件 + README.md）
> - ✅ `.specify/memory/constitution.md` — 保留，供使用者閱讀和修改

### Step 1.5: 確認憲法

複製完成後，**向使用者展示憲法內容**並確認：

```
📜 團隊憲法已複製到你的專案：
   .specify/memory/constitution.md        ← 英文版（spec-kit 使用）
   .specify/memory/constitution-zh-TW.md  ← 繁體中文版（供閱讀）

憲法定義了 6 大核心原則：
I.   模組化架構 — Clean Architecture 三層分離
II.  文件優先 — 繁體中文文件 + 英文程式碼
III. 設定驅動 — char 欄位 IsFixedLength() + IsUnicode(false)
IV.  服務導向 — CrudResult<T> 統一回傳、單頭/單身 CRUD 分離
V.   使用者中心 — 傳統 Web Form 風格、Header-Detail 連動
VI.  語言規範 — 文件繁中、程式碼英文

以及程式碼品質標準、安全要求、效能標準等。

✅ 符合需求 → 回覆「確認」繼續
✏️ 需要修改 → 請直接編輯上述檔案後告訴我「繼續」
```

等使用者確認後再進入 Step 2。

### Step 2: 重命名 Namespace

對所有檔案執行全域搜尋取代：

| 搜尋 | 替換 |
|------|------|
| `DingxinErp` | `[Namespace前綴]` (如 `PurchaseOrder`) |
| `DingxinErpTemplate` | `[專案名]` (如 `PurchaseOrderCreate`) |
| `鼎新 ERP 作業管理` | `[中文名]管理` |
| `鼎新 ERP 作業 API` | `[中文名] API` |
| `範例作業` | `[中文名]` |

需要重命名的檔案/資料夾：
- `.sln`, `.csproj` 檔案名稱
- `src/DingxinErp.*/` 資料夾名稱
- `tests/DingxinErp.Core.Tests/` 資料夾名稱
- 所有 `.cs` 檔案中的 `namespace` / `using`
- `_Layout.cshtml` 中的標題和導覽
- `Program.cs` 中的 Swagger 描述
- `README.md`, `CLAUDE.md` 中的專案名

### Step 3: 產生 Entity 檔案

參考模板的 `SampleHeader.cs` 和 `SampleDetail.cs`，用 Round 4 確認的欄位清單產生新 Entity。

**Entity 命名規則：**
- 類別名 = ERP 表格名的 PascalCase (如 `Purta`, `Purtb`)
- 屬性名 = ERP 原始欄位名 (如 `TA001`, `TB003`)

**型態對應規則：**
- `char(n)` → `string` + Configuration 設定 `IsFixedLength().IsUnicode(false).HasMaxLength(n)`
- `nvarchar(n)` → `string` + Configuration 設定 `HasMaxLength(n)`
- `decimal(p,s)` → `decimal` + Configuration 設定 `HasColumnType("decimal(p,s)")`
- 審計欄位 → 實作 `IAuditableEntity`
- 單頭 → `virtual ICollection<[Detail]> Details { get; set; } = new List<[Detail]>();`
- 單身 → `virtual [Header]? Header { get; set; }`

### Step 4: 產生 Configuration

- `ToTable("[ERP表格名]")` — 使用實際表格名（大寫）
- `HasKey(new { 複合PK })` — 複合主鍵
- char 欄位: `.IsFixedLength().IsUnicode(false)`
- FK + Cascade Delete: `.HasMany().WithOne().HasForeignKey().OnDelete(DeleteBehavior.Cascade)`

### Step 5: 產生 DTO / Mapping / Validator

依欄位清單產生：
- `[Entity]Dto.cs` — 包含所有查詢需要的欄位
- `Create[Entity]Request.cs` — 新增用，含 `List<CreateDetailRequest> Details`
- `Update[Entity]Request.cs` — 更新用，含 `List<UpdateDetailRequest> Details`
- `MappingExtensions.cs` — `ToDto()` / `ToEntity()` / `UpdateFrom()`
- Validator — 依欄位長度和必填設定驗證規則

### Step 6: 產生 Repository / Service / Controller

- Repository: CRUD + 分頁搜尋 + 單身管理
- Service: 業務邏輯 + 審計欄位 + CrudResult 回傳
- Controller (API): 8 個 endpoints
- Controller (MVC): 提供 View 頁面

### Step 7: 產生前端 View

依確認的欄位清單更新 `Index.cshtml`：
- 單頭表格的 `<thead>` 欄位
- Modal 表單的輸入欄位（含 label/maxlength/required）
- 單身表格的 `<thead>` 欄位
- Modal 中的動態單身行欄位
- JS 中的 payload 組裝

### Step 8: 刪除範例檔案

移除所有 `Sample*` 相關檔案（Entity, DTO, Service, Repository, Controller, View, Configuration, Validator, Interface）。

### Step 9: 更新 DI 註冊 + 建置確認

更新 `Program.cs` 中的 DI 註冊，將 Sample 替換為新的 Entity。
執行 `dotnet build` 確認 0 error。

### Step 10: 設定 spec-kit + 引導 SDD

初始化規格資料夾，產出初版 `spec.md`（用 Round 4 的需求摘要填入）。

### Step 11: 匯出專案 + 推送至 GitHub

建置成功後，**逐一詢問使用者**以下資訊，不可預設路徑：

```
專案開發完成！接下來要將專案匯出成獨立 repo。

請依序回答以下問題：

1. 📁 來源專案路徑 — 開發完成的專案位在哪裡？
   （例如：C:\projects\DingxinErpTemplate\PurchaseOrderCreate）

2. 📂 匯出目標路徑 — 你想把獨立專案放到哪個資料夾？
   （例如：C:\projects 或 D:\work\erp-apps）

3. 📛 專案名稱 — 匯出後的資料夾名稱？（同時作為 GitHub Repo 名稱）
   （例如：PurchaseOrderCreate）

4. 👤 GitHub 帳號 — 要推送到哪個 GitHub 帳號或組織？
   （例如：chrisbln2014）

5. 🔒 公開或私有？（public / private）
```

收集完畢後，向使用者確認摘要：

```
📋 匯出確認：
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
來源: [使用者填寫的來源路徑]
匯出: [匯出路徑]\[專案名稱]\
方式: A. GitHub Push / B. Git Bundle

確認無誤請回覆「確認」，或修改任一項。
```

確認後依選擇方式執行：

**方式 A：GitHub Push**（需要 `gh` CLI）

```powershell
$templateDir = (Get-Location).Path

& "$templateDir\.specify\scripts\powershell\export-and-push.ps1" `
    -SourceDir   "[使用者填寫的來源路徑]" `
    -ExportRoot  "[使用者填寫的匯出路徑]" `
    -ProjectName "[使用者填寫的專案名稱]" `
    -GitHubUser  "[使用者填寫的 GitHub 帳號]" `
    -Description "[中文名] — 由 DingxinErpTemplate 產生"
    # 若使用者選 private，加上 -Private
```

**方式 B：Git Bundle**（僅需 Git，本機封存）

```powershell
$templateDir = (Get-Location).Path

& "$templateDir\.specify\scripts\powershell\export-bundle.ps1" `
    -SourceDir   "[使用者填寫的來源路徑]" `
    -ExportRoot  "[使用者填寫的匯出路徑]" `
    -ProjectName "[使用者填寫的專案名稱]" `
    -Description "[中文名] — 由 DingxinErpTemplate 產生"
```

兩種方式都會自動：移除 `demo/`、`skill/`、`_example*`、產生專案 README、git init + commit。
方式 A 額外建立 GitHub Repo + push；方式 B 額外產生 `[專案名稱].bundle` 封存檔。

最後向使用者回報：

```
✅ 專案匯出完成！

📁 本地專案 : [匯出路徑]\[專案名稱]\
🔗 GitHub  : https://github.com/[user]/[專案名稱]  （方式 A）
📦 Bundle  : [匯出路徑]\[專案名稱].bundle          （方式 B）
📊 已產生  : Entity / DTO / Service / Repository / Controller / View
📝 已設定  : spec-kit SDD 流程

⚡ 下一步：
1. cd [匯出路徑]\[專案名稱]
2. 設定 appsettings.Development.json 的連線字串
3. dotnet run --project src/[Name].Web 確認可啟動
4. 如有客製需求，使用 /speckit.plan → /speckit.tasks → /speckit.implement 逐步實作
5. 完成後使用 /speckit.checklist 驗收
```

---

## 階段三：客製化需求處理

如果 Round 3 有收集到客製化需求，在 scaffold 完成後進入此階段：

### 計算規則
在 Service 層的 `CreateAsync` / `UpdateAsync` 中加入計算邏輯。
在前端 JS 的 `$(document).on('change', ...)` 中加入即時計算。

### 下拉選單 (Lookup)
1. 在 Core/Entities/ 建立 Lookup Entity (如 `Copma` 客戶主檔)
2. 在 DbContext 加入 DbSet
3. 在 Controller 加入 `GET /api/[entity]/lookup/[type]` endpoint
4. 在前端用 `<select>` + AJAX 載入選項

### 欄位連動
在前端 JS 加入 `$(document).on('change', '#f_[來源欄位]', ...)` 事件，
AJAX 查詢關聯資料並自動填入目標欄位。

### 特殊驗證
在 FluentValidation Validator 中加入自訂規則。

---

## 重要注意事項

1. **char 欄位務必使用 `IsFixedLength()`** — 否則 EF Core 會加 `N''` 前綴導致查不到資料
2. **審計欄位由 Service 層自動填寫** — 不在 DTO 中暴露
3. **單身使用 replace-all 模式** — 更新時刪除所有舊單身再插入新的
4. **前端使用 CDN (Bootstrap 5 + jQuery)** — 不需要 npm/webpack
5. **所有文件使用繁體中文**，程式碼使用英文
6. **Entity 屬性名使用 ERP 原始欄位名** (TA001, TB003 等)
7. **每步驟完成後回報進度** — 讓使用者知道目前狀態
