<!--
此為繁體中文參考版本。
Spec-kit 自動化流程使用的是英文版: constitution.md
手動修訂請先更新 constitution.md，再同步此檔案。
-->

# 鼎新 ERP 作業轉換專案 — 團隊憲法

## 核心原則

### I. 模組化架構原則

所有專案必須採用 **Clean Architecture** 三層架構，每一層皆為獨立、
自包含的模組，具備清楚的職責劃分。模組必須可獨立測試、有完整文件、
且可在不同作業間重複使用。嚴格的關注點分離確保可維護性，並降低業務
邏輯、驗證邏輯與資料持久化之間的耦合。

- **Web 層** (`*.Web`): Controllers, Views, Middleware, wwwroot
- **Core 層** (`*.Core`): Entities, DTOs, Interfaces, Services,
  Validators, Common
- **Infrastructure 層** (`*.Infrastructure`): DbContext,
  Configurations, Repositories

依賴方向: Web → Core ← Infrastructure
(Core 不依賴任何外部套件，僅 FluentValidation)

### II. 文件優先原則

每個功能、API 與資料結構在實作前或實作期間必須有完整文件。文件包含
目的、用法範例、驗證規則，以及任何業務邏輯限制。程式碼註解解釋
「為什麼」而非「做什麼」— 模糊的 ERP 需求必須在文件中釐清後才能
開始實作。

- 所有規格書、計畫書、使用者文件**必須使用繁體中文 (zh-TW)**
- 程式碼變數名稱、方法名稱使用**英文**
- 程式碼註解使用**繁體中文**
- XML Summary 使用**繁體中文**

### III. 設定驅動開發

行為必須盡可能透過設定控制，而非修改程式碼。

- 鼎新 ERP 的 `char()` 欄位必須使用
  `IsFixedLength()` + `IsUnicode(false)` 設定
- 所有 Entity Configuration 使用 `IEntityTypeConfiguration<T>` 實作
- 審計欄位 (CREATOR/CREATE_DATE/MODIFIER/MODI_DATE/FLAG)
  透過 `IAuditableEntity` 統一管理
- JSON 序列化使用 `PropertyNamingPolicy = null`
  (保持 PascalCase，ERP 欄位名不被轉換)
- 未設定連線字串時自動切換 InMemory DB 示範模式
- 預設設定必須向後相容 — 新功能預設為停用
- 設定變更不得需要重新編譯程式碼
- 所有設定選項必須在文件中有對應說明

原因：設定驅動的方式減少開發時間、最小化程式碼變更，並讓業務人員
可透過設定文件了解系統能力。

### IV. 服務導向架構與明確邊界

每個 Service 必須遵循單一職責原則，並具備明確定義的介面。

- **Controllers** 僅處理路由與 Request/Response
- **Services** 包含業務邏輯、驗證與資料協調 (Core 層實作)
- **Repositories** 負責資料存取 (Infrastructure 層實作)
- 統一回傳格式: `CrudResult<T>` (Success/Message/Data/Errors)
- 分頁格式: `PagedResult<T>`
  (Items/TotalItems/TotalPages/CurrentPage/PageSize)
- Services 必須依賴抽象 (介面)，不得依賴具體實作
- Service 註冊必須使用 DI (`Program.cs` 中的
  `AddScoped<IService, Service>()`)
- 驗證: FluentValidation，Validator 放在 Core 層
- 單頭/單身 CRUD **完全分離**: 各自獨立的 API 端點、Modal 與操作按鈕

原因：清楚的架構邊界使系統可測試、可維護，並透過降低耦合促進團隊
協作。

### V. 使用者體驗原則

使用者需求與來自 ERP 操作團隊的回饋驅動所有設計決策。功能需求必須
在實作前與最終使用者驗證。系統必須支援使用者實際操作 ERP 資料的方式，
而非強加任意工作流程。

- 前端 UI 採用**傳統 Web Form 式**
  (搜尋列 + 表格 + Modal 編輯)
- 單頭單身連動: 點擊單頭行 → AJAX 載入單身表格
- 單頭行點擊自動選取: checkbox 勾選 + 啟用編輯/刪除按鈕
- 切換行自動取消前一行選取（單選模式）
- 鍵盤快捷鍵: Ctrl+N (新增), Ctrl+E (編輯), Delete (刪除),
  F5 (重新整理)
- Toast 通知: 非阻斷式訊息
- Bootstrap 5 + jQuery (CDN)

### VI. 強制語言規範 — 不可協商

所有規格書、計畫書與使用者面向的文件**必須使用繁體中文 (zh-TW)**。

- `/specs/` 中的功能規格書必須使用繁體中文
- 實作計畫 (plan.md) 必須使用繁體中文
- 使用者故事與驗收標準必須使用繁體中文
- 任務清單 (tasks.md) 必須使用繁體中文
- README 必須使用繁體中文
- 使用者面向的錯誤訊息必須使用繁體中文
- Entity/DTO 屬性名: 使用 ERP 原始欄位名 (如 TA001, TB003)
- 方法名/類別名: **英文** (如 `GetByKeyAsync`, `SampleService`)
- Git Commit Message: **英文** (Conventional Commits)

原因：語言一致性確保所有利害關係人（包括非英語母語者）都能完整
理解並參與專案文件與開發決策。

## 程式碼品質標準

### 命名規範

- **繁體中文註解必要**: 用於業務邏輯解說
- **英文必要**: 用於程式碼識別項 (classes, methods, variables)
- Controller 命名模式: `{Feature}Controller.cs`
- Service 命名模式: `I{Service}Service.cs` (介面),
  `{Service}Service.cs` (實作)
- Repository 命名模式: `I{Feature}Repository.cs` (介面),
  `{Feature}Repository.cs` (實作)
- API 端點必須遵循 REST 慣例:
  `/api/{resource}` (單頭 CRUD)
  `/api/{resource}/{pk1}/{pk2}/details` (單身 CRUD)
- 方法名必須具描述性且以動詞開頭:
  `GetByKeyAsync`, `CreateAsync`, `DeleteDetailAsync`

### 程式碼組織

- 使用 XML Summary (`/// <summary>`) 說明 API 方法用途
- Entity 欄位使用 XML Summary 標注中文欄位說明
- API 方法按 單頭CRUD → 單身CRUD 順序排列
- 最大方法長度: 50 行 (例外需附理由)
- 最大類別長度: 1000 行 (超過必須重構)

### 錯誤處理

- 所有 Service 方法必須回傳 `CrudResult<T>` 帶有成功/失敗狀態
- 例外必須被捕獲並包裝為
  `CrudResult.ErrorResult(message, errors)`
- 使用者面向的錯誤訊息必須使用繁體中文
- 內部錯誤細節必須使用 `ILogger` 記錄含堆疊追蹤
- 全域例外透過 `ExceptionHandlingMiddleware` 統一處理
- HTTP 狀態碼必須遵循標準:
  200 (成功), 201 (新增成功), 400 (驗證失敗),
  404 (找不到), 500 (伺服器錯誤)

## 安全性要求

ERP 資料需要嚴格的安全控管：

### 機密管理

- 原始碼中**嚴格禁止**硬編碼 API 金鑰、Token 或密碼
- 所有機密必須儲存在環境變數或機密管理器
  (如 Azure Key Vault)
- `.env` / `.env.local` 檔案必須列在 `.gitignore` 中，
  **絕對不得**提交至版本控制
- Git 歷史記錄必須不含機密；任何意外提交都需要
  立即進行金鑰輪換
- 正式環境機密必須在託管平台設定
  (如 Azure App Service 應用程式設定)
- 應用程式在啟動時若缺少必要機密，必須快速失敗：
  在處理任何請求前拋出描述性錯誤

原因：洩漏的憑證是資料外洩最常見的原因。機密必須被視為基礎設施
問題，而非程式碼問題。

### 輸入驗證與清理

- 所有使用者輸入必須透過 FluentValidation 驗證
- SQL Injection 防護: 使用 EF Core 參數化查詢 (強制)
- XSS 防護: 使用者提供的文字必須進行 HTML 清理
- 危險 SQL 模式必須被拒絕:
  `--, ;, /*, */, EXEC, DROP, ALTER`
- 檔案上傳必須驗證檔案類型與大小

### API 安全性

- 正式環境的 API 端點必須設定適當的授權保護
- CSRF 防護: 對狀態變更操作啟用防偽 Token
- CORS 設定: 僅允許明確白名單的來源網域
- 請求速率限制: 防止暴力攻擊與 DDoS

### 敏感資料外洩防護

- 密碼、Token、API 金鑰與個資**絕對不得**出現在
  應用程式日誌中
- 日誌項目必須遮蔽敏感欄位；使用識別碼
  (如 `userId`, `last4`) 取代原始值
- 使用者面向的錯誤回應僅可回傳通用訊息；
  堆疊追蹤與內部錯誤細節**不得**暴露
- 詳細錯誤資訊僅可記錄在伺服器端日誌中，
  並設定適當的存取控制
- HTTP 回應不得包含有助指紋辨識的伺服器版本標頭
  (`Server`, `X-Powered-By`)

原因：日誌或錯誤回應中的敏感資料意外暴露，是憑證竊取與隱私侵害
的主要原因。

## 效能與最佳化標準

- 資料庫查詢使用 `Select` 投影特定欄位，避免載入
  整個 Entity 的所有欄位
- 唯讀查詢使用 `AsNoTracking()` 提升效能
- 實作分頁: 超過 100 筆的表格必須分頁顯示
- 批次資料庫操作: 處理多筆記錄時使用批次操作
- 所有 I/O 操作使用 `async/await` 非同步模式
- Include 關聯載入: 僅在確實需要時才 Include 單身
- 前端表格: 自動分頁 + 搜尋避免載入過多資料

## 開發流程

- **SDD 流程**: 使用 spec-kit 驅動開發
  (`/speckit.specify → /speckit.plan → /speckit.tasks
  → /speckit.implement → /speckit.checklist`)
- **Code Reviews**: 所有 ERP 業務邏輯的變更需經過同儕審查
  才能合併至 main
- **測試需求**: 所有 CRUD 操作與驗證規則需要整合測試
- **文件規範**: 文件更新必須與程式碼變更同步
- **發布流程**: 語意化版本控制；對 Entity 結構的破壞性變更
  需要主版號遞增
- **建置驗證**: `dotnet build` 零錯誤零警告才可提交

## 治理

此憲法取代本專案中所有其他開發慣例。所有 Pull Request 必須驗證
是否符合這些原則。當這些原則與其他指引發生衝突時，以憲法為準。

開發決策應以這些核心原則為依據進行論述。憲法僅在專案的基礎方針
根本性改變時才可修訂；修訂需記錄理由，並更新相依文件
(plan.md, spec.md, tasks.md) 的計畫。

### 活文件

- 憲法是活文件 — 持續改善是必要的
- 實作經驗的回饋應納入修訂考量
- 建議每年審查一次，確保原則仍然適用
- 使用 `.specify/memory/constitution.md` 作為憲法的唯一來源
- 所有範本檔案 (plan, spec, tasks) 必須與憲法原則對齊

### 合規性驗證

- 所有 Code Review 必須驗證是否符合此憲法
- 違反原則的 Pull Request 必須附上憲法引用後退件
- 原則的例外必須記錄在實作計畫的複雜度追蹤區段
- 複雜度必須合理化: 「需要的原因」與
  「為何拒絕更簡單的替代方案」

**版本**: 1.2.0 | **制定日期**: 2026-03-15 | **最後修訂**: 2026-03-16
