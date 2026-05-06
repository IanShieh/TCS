---
description: 掃描 ops-docs/ 分析 ERP 作業文件，從 demo/ 複製 Sample 範例到 src/，初始化作業開發環境
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

本 agent 負責在 DingxinErpTemplate 模板根目錄中，將 `demo/` 的 Sample 範例複製到 `src/`，為 SDD 流程準備可 build 的開發環境。

### Step 1: 確認執行位置

執行以下命令確認目前是在 DingxinErpTemplate 模板根目錄：

```bash
git remote get-url origin
```

判斷規則：
- 若回傳 URL **不含** `DingxinErpTemplate` → 顯示警告：
  > 「此指令設計在 DingxinErpTemplate 模板根目錄執行。目前位於其他 repo，是否繼續？(yes/no)」
  >
  > 等待使用者確認。
- 若含 `DingxinErpTemplate` → 繼續

### Step 2: 檢查 src/ 狀態

檢查 `src/DingxinErp.Core/Entities/` 是否已有 `.cs` 檔案（排除 `.gitkeep`）：

- **有 Entity 檔案** → 顯示警告：
  > 「src/ 已包含 Entity 檔案，scaffold 會覆蓋現有內容。是否繼續？(yes/no)」
- **無 Entity 檔案**（空骨架）→ 繼續

### Step 3: 檢查 demo/ 存在

確認 `demo/src/` 目錄存在且包含 Sample 範例：

```bash
ls demo/src/DingxinErp.Core/Entities/
```

- 若不存在 → **停止**，顯示錯誤：
  > 「找不到 demo/src/ 範例目錄。請確認模板 repo 的 demo/ 目錄完整。」

### Step 4: 掃描 ops-docs/

列出 `ops-docs/` 下所有子資料夾（排除以 `_` 開頭的範例資料夾）：

```bash
ls -d ops-docs/*/  | grep -v '_example'
```

對每個子資料夾：
1. 讀取 `erp-doc.md`（若存在）
2. 讀取 `requirements.md`（若存在）
3. 從文件內容推斷：
   - 作業名稱（如「供應廠商資料建立」）
   - ERP 模組類別（如「採購」）
   - 主要表格名（如 PURMA）

顯示分析結果：

```
找到以下 ERP 作業文件：

  📁 ops-docs/[作業名稱]-[中文說明]/
     推斷作業名稱：[作業名稱]
     主要表格：[TABLE_NAME]（推測）

請輸入作業代號（例如：PURI01）：
```

等待使用者輸入代號。格式要求：英文字母 + 數字（如 PURI01、SALA02、INVA03）。

### Step 5: 確認執行動作

顯示即將執行的操作，等待使用者確認：

```
即將執行以下操作：

  1. 複製 demo/src/ 所有內容 → src/（覆蓋空骨架，取得 Sample 範例）
  2. 複製 demo/tests/ → tests/（取得範例測試）
  3. 執行 dotnet build 驗證

  ⚠️ src/ 現有檔案會被覆蓋（空骨架 → Sample 範例）

確認執行？(yes/no)
```

### Step 6: 執行複製

使用者確認後，執行以下 PowerShell 命令：

```powershell
# 複製 demo/src/ 內容到 src/（建立開發用專案）
Copy-Item -Path "demo/src/*" -Destination "src/" -Recurse -Force

# 複製 demo/tests/ 內容到 tests/（建立測試專案）
Copy-Item -Path "demo/tests/*" -Destination "tests/" -Recurse -Force

# 複製 demo/.sln 到根目錄（讓根目錄 .sln 指向 src/）
Copy-Item -Path "demo/DingxinErpTemplate.sln" -Destination "DingxinErpTemplate.sln" -Force
```

> **注意**：根目錄原本的 .sln 指向 `demo/src/`（範例），複製後改為指向 `src/`（開發中）。

### Step 6b: 移除 Sample 範例檔案

複製完成後，**自動移除** src/ 中的 Sample 範例作業檔案，只保留通用基礎架構。

```powershell
# 1. 移除 Sample 專用檔案（Entity/DTO/Validator/Service/Repository/Configuration/Controller/View）
Get-ChildItem -Path "src" -Recurse -Filter "*Sample*" | Remove-Item -Recurse -Force
Remove-Item -Path "src/DingxinErp.Core/DTOs/MappingExtensions.cs" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "src/DingxinErp.Web/Views/Sample" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "tests/DingxinErp.Core.Tests/UnitTest1.cs" -Force -ErrorAction SilentlyContinue
```

接著清理共用檔案中的 Sample 引用：

**AppDbContext.cs** — 移除 Sample DbSet，保留空白 DbSet 區域：

```powershell
$ctx = "src/DingxinErp.Infrastructure/Data/AppDbContext.cs"
(Get-Content $ctx) |
  Where-Object { $_ -notmatch 'DbSet<Sample' } |
  ForEach-Object { $_ -replace '// ===== 範例表格.*', '// ===== DbSet (依作業需求新增) =====' } |
  Set-Content $ctx
```

**Program.cs** — 移除 Sample DI 註冊和種子資料，保留結構框架：

```powershell
$prog = "src/DingxinErp.Web/Program.cs"
(Get-Content $prog) |
  Where-Object {
    $_ -notmatch 'ISampleRepository|ISampleService|SampleRepository|SampleService' -and
    $_ -notmatch 'CreateSampleHeaderValidator'
  } |
  Set-Content $prog

# 移除 SeedDemoData 方法中的 Sample 引用，替換為空的 seed 範本
$content = Get-Content $prog -Raw
$content = $content -replace '(?s)static void SeedDemoData\(AppDbContext db\)\s*\{.*\}', @'
static void SeedDemoData(AppDbContext db)
{
    // ★ 在此新增作業的種子資料（InMemory 模式使用）
    // 範例:
    // if (db.YourEntities.Any()) return;
    // db.YourEntities.Add(new YourEntity { ... });
    // db.SaveChanges();
}
'@
Set-Content $prog $content
```

**_Layout.cshtml** — 移除 Sample 導覽連結：

```powershell
$layout = "src/DingxinErp.Web/Views/Shared/_Layout.cshtml"
(Get-Content $layout) |
  Where-Object { $_ -notmatch 'SamplePage' -and $_ -notmatch '範例作業' } |
  Set-Content $layout
```

**Program.cs using** — 移除已不需要的 Sample 命名空間引用：

```powershell
$prog = "src/DingxinErp.Web/Program.cs"
(Get-Content $prog) |
  Where-Object { $_ -notmatch 'using DingxinErp\.Core\.Entities;' } |
  Set-Content $prog
```

清理完成後顯示：

```
🧹 已移除 Sample 範例檔案：
   - Entity/DTO/Validator/Service/Repository/Configuration（15 檔）
   - Controller/View（3 檔 + 1 目錄）
   - 清理 AppDbContext、Program.cs、_Layout.cshtml 中的 Sample 引用
   src/ 現在只剩通用基礎架構，可直接新增作業。
```

### Step 7: 驗證 build

```bash
dotnet build DingxinErpTemplate.sln
```

- **Build succeeded** → 繼續 Step 8
- **Build failed** → 顯示錯誤訊息，提示使用者檢查

### Step 8: 完成提示

```
✅ scaffold 完成！

  src/ 已包含通用基礎架構（Sample 範例已自動清理）
  作業代號：{使用者輸入的代號}（如 PURI01）

  保留的基礎架構：
    ├── Core/Common/          CrudResult, PagedResult, IAuditableEntity
    ├── Infrastructure/Data/  AppDbContext（空 DbSet）
    ├── Web/Controllers/      HomeController
    ├── Web/Views/            Home, Shared/_Layout
    ├── Web/Middleware/        ExceptionHandlingMiddleware
    └── Web/wwwroot/          css/site.css, js/crud-common.js

  下一步：執行 SDD 流程建立作業規格
  👉 /speckit.specify {作業名稱描述}

  範例：/speckit.specify [作業名稱] - 基於 [TABLE_NAME] 表格的單層 CRUD
```

## 注意事項

- 此 agent 負責複製並清理：複製 demo/ → src/，然後自動移除 Sample 範例檔案
- Sample 範例僅保留在 `demo/` 目錄供開發參考（唯讀）
- `demo/` 目錄在複製後仍保留（唯讀參考）
- 若使用者提供 `$ARGUMENTS` 且包含作業代號（如 `/speckit.scaffold PURI01`），跳過 Step 4 的詢問，直接使用該代號
