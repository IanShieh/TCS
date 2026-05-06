# 受訓證件作業（TCS）— 設計規格書

| 項目 | 內容 |
|---|---|
| 作業代號 | TCS（Training Certificate System） |
| 撰寫日期 | 2026-05-06 |
| 版本 | 0.1（草案，待技術計畫產出後升 1.0） |
| 對應憲法 | 鼎新 ERP 作業轉換專案憲法 v1.2.0 |
| 模板來源 | DingxinErpTemplate（Clean Architecture / Razor Pages / .NET 8） |

---

## 1. 背景與目標

公司既有員工會持有多種證照（如急救、堆高機操作等），各證照依規定須在固定週期內回訓並累積一定時數，否則視為過期失效。本作業提供受訓紀錄的維護介面，並自動判定每張證照是否處於有效、待補訓或已過期狀態。

**目標**：

1. 建立證照規格主檔與廠別需求數量主檔
2. 維護員工受訓異動紀錄（單頭：員工×證照；單身：每次受訓事件）
3. 系統自動依累計時數與週期判定「已過期」狀態
4. 提供受訓資料 Excel 匯出
5. 結合公司既有 JWT 登入系統，依 `action` 欄位控制 CRUD 權限

---

## 2. 範圍

### 2-1. 包含

- 證照規格主檔 CRUD
- 證照廠別需求 CRUD
- 受訓異動單頭 CRUD
- 受訓異動單身 CRUD
- 員工資料查詢（唯讀）
- 過期狀態自動掃描（BackgroundService）
- 受訓資料 Excel 匯出
- JWT 整合與 action 權限檢查

### 2-2. 不包含（本版排除）

- 即將到期警示清單（F1）
- Excel 批次匯入（F2）
- 紙本列印（F4）
- 部門證照達成率統計頁（F5）
- Email 提醒（F6）
- 跨部門/跨廠別資料權限過濾（凡登入即可看全部）

---

## 3. 角色與權限

### 3-1. 登入控制（外部）

- 員工登入由公司既有 JWT 簽發系統處理；本作業**不負責**判斷使用者是否可登入。
- 凡持有有效 JWT 即可進入本作業頁面與所有查詢端點。

### 3-2. JWT Payload 欄位

| 欄位 | 說明 | 用途 |
|---|---|---|
| `employeeId` | 員工編號 | 顯示登入者 |
| `name` | 姓名 | 顯示登入者 |
| `action` | 動作權限（逗號分隔字串） | 本作業 CRUD 授權 |
| `department` | 部門別 | 暫不使用（資料無範圍限制） |
| `jti` | JWT ID | 防重放 |
| `iat` | 簽發時間 | JWT 標準 |
| `exp` | 到期時間 | JWT 標準 |

### 3-3. action 與功能對應

| action 值 | 對應功能 |
|---|---|
| `新增` | 所有 POST 端點 |
| `修改` | 所有 PUT 端點 |
| `刪除` | 所有 DELETE 端點 |
| `儲存` | 受訓資料 Excel 匯出 |
| `列印` / `排序` / `搜尋` | **本作業不檢查**（前端排序、搜尋皆開放；無列印功能） |

---

## 4. 資料模型

### 4-1. `LicenseMaster` — 證照規格主檔

**設計慣例 — 大類/小類以 `LicenseType` 數字格式區分**：

- `LicenseType` 純整數（如 `1`、`2`、`10`）= **大類列**，作為分類抬頭，本身不對應實際證照
- `LicenseType` 含小數點（如 `1.1`、`2.3.1`）= **小類列**，為實際可頒發的證照
- 「對應大類」下拉清單就是直接撈 `LicenseMaster` 中 `LicenseType` 為純整數的列

| 欄位 | 型別 | PK | NULL | 說明 |
|---|---|:-:|:-:|---|
| `LicenseType` | `CHAR(10)` | ✓ | N | 證照類別代碼；純整數=大類，含小數點=小類 |
| `Description` | `NVARCHAR(70)` | | N | 證照/類別名稱描述 |
| `Category` | `CHAR(10)` | | **Y** | 對應大類的 `LicenseType`；**大類列強制為 NULL**，小類列必填且須對應實際存在的大類 |
| `Hours` | `INT` | | **Y** | 每週期應訓時數；**小類列必填**；大類列可填可空（不參與週期計算） |
| `Years` | `INT` | | **Y** | 週期年數；**小類列必填**；大類列可填可空（不參與週期計算） |
| `Creator` / `CreateDate` / `Modifier` / `ModiDate` / `Flag` | — | | | `IAuditableEntity` 標準欄位 |

> **欄位長度已由使用者調整**。後續欄位若需再修改，會同步更新本表。

### 4-2. `LicensePlantRequirement` — 證照廠別需求

| 欄位 | 型別 | PK | NULL | 說明 |
|---|---|:-:|:-:|---|
| `LicenseType` | `CHAR(10)` | ✓ | N | FK → LicenseMaster |
| `Plant` | `CHAR(6)` | ✓ | N | 廠別代碼 |
| `RequiredCount` | `INT` | | N | 該廠別需求證照數 |
| 審計欄位 | — | | | 同上 |

### 4-3. `TrainingHeader` — 受訓異動單頭

| 欄位 | 型別 | PK | NULL | 說明 |
|---|---|:-:|:-:|---|
| `EmployeeId` | `CHAR(10)` | ✓ | N | 邏輯對應 Employee.EmployeeId（Employee 為 view，無實體 FK；新增時 Service 層驗證存在） |
| `LicenseType` | `CHAR(10)` | ✓ | N | FK → LicenseMaster |
| `RequiredHours` | `INT` | | N | 應回訓時數（從 LicenseMaster.Hours 自動帶入，使用者不可改） |
| `Remark` | `NVARCHAR(70)` | | Y | 備註 |
| 審計欄位 | — | | | 同上 |

### 4-4. `TrainingDetail` — 受訓異動單身

| 欄位 | 型別 | PK | NULL | 說明 |
|---|---|:-:|:-:|---|
| `EmployeeId` | `CHAR(10)` | ✓ | N | |
| `LicenseType` | `CHAR(10)` | ✓ | N | 須為小類（含小數點），不可為大類 |
| `TrainingDate` | `DATE` | ✓ | N | 受訓日期 |
| `TrainingType` | `TINYINT` | | N | `1=取得證照`、`2=回訓`；使用者輸入；不被系統覆寫 |
| `IsExpired` | `BIT` | | N | 是否已過期；由 BackgroundService 維護 |
| `Hours` | `DECIMAL(5,1)` | | N | 該筆受訓時數 |
| 審計欄位 | — | | | 同上 |

> **TrainingType 編碼常數**：在 Core 層以 `enum TrainingType : byte { 取得證照 = 1, 回訓 = 2 }` 表示；DTO 與 API 同樣使用整數傳輸，前端顯示文字由 i18n 對應表處理。

### 4-5. `Employee` — 員工資料（唯讀映射既有表）

EF Core 設為 `ToView`，僅供查詢與下拉選擇，不參與 migration。

| EF 屬性 | 對應 ERP 欄位 | 型別 | 說明 |
|---|---|---|---|
| `EmployeeId` | `MV001` | `CHAR(10)` | 員工編號 |
| `Name` | `MV002` | `CHAR(10)` | 姓名 |
| `Department` | `MV004` | `CHAR(6)` | 部門別 |
| `HireDate` | `MV021` | `CHAR(8)` | 到職日 YYYYMMDD |

### 4-5-2. `Plant` — 廠別主檔（唯讀映射既有 `CMSMB` 表）

EF Core 設為 `ToView`，僅供查詢與下拉、JOIN 顯示廠別名稱用，不參與 migration。

| EF 屬性 | 對應 ERP 欄位 | 型別 | 說明 |
|---|---|---|---|
| `PlantCode` | `MB001` | `CHAR(6)` | 廠別代碼（與 `LicensePlantRequirement.Plant` 對應） |
| `PlantName` | `MB002` | `NVARCHAR(?)` | 廠別名稱（顯示用） |

### 4-6. 衍生計算欄位（不存資料庫；DTO 層計算）

**名詞定義（修正：允許多次取得證照）：**

- 同一 `(EmployeeId, LicenseType)` **可有多筆** `TrainingType=1 (取得證照)`，代表更新版本的證照取得；最早一筆必須是取得證照（見 §8-2）
- **「週期」**：每一筆 `取得證照` 紀錄起算一個週期，區間 `[TrainingDate, TrainingDate + Years 年)`
- **「當前週期」**：以**最後一筆**（最新 `TrainingDate`）的 `取得證照` 為起點所定義之週期；若該週期已結束且無更新的取得證照，則「當前週期」即為這最後一個週期（已結束狀態）
- **某筆 `TrainingDetail` 屬於哪個週期**：以「`TrainingDate ≤ 該 detail.TrainingDate` 的最近一筆 `取得證照`」為週期起點

**衍生欄位（顯示於 TrainingHeader DTO 與單頭表格）：**

| 欄位 | 計算邏輯 |
|---|---|
| `LatestAcquireDate` | 最後一筆 `取得證照` 的 `TrainingDate`；無則 NULL |
| `LatestRetrainDate` (最新回訓日期) | 最後一筆 `回訓` 的 `TrainingDate`；無則 NULL |
| `NextReviewDate` (下次回訓時間) | `LatestAcquireDate + LicenseMaster.Years` 年；`LatestAcquireDate` 為 NULL 時為 NULL |
| `AccumulatedHours` | 當前週期內所有 `TrainingDetail.Hours` 加總 |
| `RemainingHours` (未達受訓時數) | `MAX(0, RequiredHours - AccumulatedHours)` |
| `OverallStatus` | 四態：`未取得`（無取得證照紀錄）/ `通過`（累計 ≥ 應訓且當前週期無 IsExpired=1）/ `進行中`（累計 < 應訓且 NextReviewDate ≥ 今天）/ `已過期`（當前週期任一 detail 的 IsExpired=1） |

### 4-7. 關聯與 Cascade 規則

```
LicenseMaster (1) ── (N) LicensePlantRequirement   [Cascade Delete]
LicenseMaster (1) ── (N) TrainingHeader            [Restrict Delete：有引用即擋]
TrainingHeader (1) ── (N) TrainingDetail           [Cascade Delete]
Employee (1) ── (N) TrainingHeader                 [唯讀關聯，不 Cascade]
```

---

## 5. API 端點

### 5-1. 證照規格主檔（模組 A）

| Method | Path | 所需 action |
|---|---|---|
| GET | `/api/license` | — |
| GET | `/api/license/{licenseType}` | — |
| POST | `/api/license` | 新增 |
| PUT | `/api/license/{licenseType}` | 修改 |
| DELETE | `/api/license/{licenseType}` | 刪除 |

### 5-2. 證照廠別需求（模組 B）

| Method | Path | 所需 action |
|---|---|---|
| GET | `/api/license/{licenseType}/requirements` | — |
| POST | `/api/license/{licenseType}/requirements` | 新增 |
| PUT | `/api/license/{licenseType}/requirements/{plant}` | 修改 |
| DELETE | `/api/license/{licenseType}/requirements/{plant}` | 刪除 |

### 5-3. 受訓異動單頭（模組 C）

| Method | Path | 所需 action |
|---|---|---|
| GET | `/api/training` | — |
| GET | `/api/training/{empId}/{licType}` | — |
| POST | `/api/training` | 新增 |
| PUT | `/api/training/{empId}/{licType}` | 修改 |
| DELETE | `/api/training/{empId}/{licType}` | 刪除 |

### 5-4. 受訓異動單身（模組 D）

> URL 中 `{date}` 一律使用 ISO 8601 格式 `yyyy-MM-dd`（例：`2025-06-20`）。Controller 使用 `[FromRoute] DateTime` 自動繫結；非法格式回 400。

| Method | Path | 所需 action |
|---|---|---|
| GET | `/api/training/{empId}/{licType}/details` | — |
| GET | `/api/training/{empId}/{licType}/details/{date}` | — |
| POST | `/api/training/{empId}/{licType}/details` | 新增 |
| PUT | `/api/training/{empId}/{licType}/details/{date}` | 修改 |
| DELETE | `/api/training/{empId}/{licType}/details/{date}` | 刪除 |

### 5-5. 員工查詢（模組 E）

| Method | Path | 所需 action |
|---|---|---|
| GET | `/api/employee?keyword=...` | — |

### 5-6. Excel 匯出（模組 F）

| Method | Path | 所需 action |
|---|---|---|
| GET | `/api/training/export?...filters` | 儲存 |

---

## 6. 授權實作

### 6-1. JWT 驗證

- 使用 ASP.NET Core 標準 `JwtBearer` middleware
- `Authority / Issuer / Audience` 由 `appsettings.json` 提供，不得硬編碼
- 驗證失敗：401，訊息 `身分驗證失敗，請重新登入`

### 6-2. 自訂屬性 `[RequireAction(...)]`

```csharp
[RequireAction("新增")]
public async Task<IActionResult> Create(...) { ... }
```

執行流程：

1. 從 `HttpContext.User` 取出 `action` claim
2. 解析逗號分隔字串為 `HashSet<string>`
3. 比對屬性所要求的 action 是否全部包含
4. 不足：403，訊息 `您沒有此操作權限：{action}`

### 6-3. 前端 UI 同步

頁面載入時解析 JWT 的 action 欄位，依規則 disable 對應按鈕：

| 按鈕 | disabled 條件 |
|---|---|
| 新增 | 不含 `新增` |
| 修改 | 不含 `修改` |
| 刪除 | 不含 `刪除` |
| 匯出 Excel | 不含 `儲存` |

---

## 7. UI / Razor Pages 設計

### 7-1. 共通慣例

- Bootstrap 5 + jQuery，皆走 CDN
- 配色與版型沿用 DingxinErpTemplate
- 非阻斷式 Toast 通知：成功綠底 3 秒消失；失敗紅底手動關閉
- **單選列高亮**：點擊單頭表格列時整列底色變藍 + 左側豎線；切換列自動清除前一筆；無 checkbox 欄
- 單頭單身連動：點選單頭列即 AJAX 載入單身

**搜尋設計 — 兩段式：**

1. **快速搜尋列（永遠顯示）**：頁面頂端一個輸入框 + 搜尋按鈕。輸入文字後對該頁所有可搜尋欄位做 OR 模糊比對（後端 `LIKE %kw%` 套用於每個 string/char 欄位的 UNION）
2. **完整欄位搜尋（預設折疊）**：點擊「進階搜尋 ▾」展開面板，每個欄位獨立輸入框/下拉，多條件以 AND 組合；含「重設」按鈕清空所有條件
3. 兩段式同時生效時，以**進階搜尋**為準（快速搜尋自動清空）

### 7-2. 頁面 1：`/License` 證照主檔

```
┌─ 快速搜尋 ──────────────────────────────────────┐
│ [____________] [搜尋]   [進階搜尋 ▾]            │
└─────────────────────────────────────────────────┘
┌─ 操作按鈕（單頭）───────────────────────────────┐
│ [新增] [修改] [刪除]                            │
└─────────────────────────────────────────────────┘
┌─ 證照主檔表格（單頭）───────────────────────────┐
│   類別   描述         大類   時數   年          │
│   1      安全類別     —     —     —            │
│   1.1    急救證       1      8     2           │
│ ▌ 2.3    堆高機操作   2     16     3           │
└─────────────────────────────────────────────────┘
┌─ 廠別需求表格（單身）───────────────────────────┐
│ [+] [✎] [✗]                                     │
│   廠別代碼  廠別名稱     需求數                 │
│   A01       台北廠       5                      │
│   A02       高雄廠       3                      │
└─────────────────────────────────────────────────┘
```

**進階搜尋欄位**：證照類別（前綴 LIKE）/ 描述（LIKE）/ 大類（下拉，源自 `LicenseType` 為純整數的列）/ 時數區間 / 年數區間

**Modal**：

- 證照主檔：`LicenseType`（新增可填、修改 readonly；驗證為純整數或合法小數點格式）/ `Description` / `Category`（下拉，僅可選大類；當 LicenseType 為純整數時自動隱藏並設 NULL） / `Hours`（小類必填，大類可選填） / `Years`（同左）
- 廠別需求：`Plant`（下拉，源自 `Plant` view 的 `PlantCode + PlantName`） / `RequiredCount`

### 7-3. 頁面 2：`/Training` 受訓異動

```
┌─ 快速搜尋 ──────────────────────────────────────┐
│ [____________] [搜尋]   [進階搜尋 ▾]            │
└─────────────────────────────────────────────────┘
┌─ 操作按鈕 ──────────────────────────────────────┐
│ [新增] [修改] [刪除] [匯出 Excel]               │
└─────────────────────────────────────────────────┘
┌─ 受訓單頭表格 ──────────────────────────────────────────────────────────────────┐
│   員編  姓名  部門  到職日    證照  描述   時數  最新回訓日  未達時數  下次回訓  備註   │
│   E001  張三  D01   20210101  1.1   急救    8    2025-06-20  0         2026-01-15 —    │
│ ▌ E002  李四  D01   20220801  1.1   急救    8    —           4         2024-12-30 補訓中│
│   E003  王五  D02   20180510  2.3   堆高機  16   —           16        —          —    │
└──────────────────────────────────────────────────────────────────────────────────┘
┌─ 受訓單身表格 ──────────────────────────────────┐
│ [+] [✎] [✗]                                     │
│   日期        類型      時數   是否過期         │
│   2024-01-15  取得證照  8.0    否              │
│   2025-06-20  回訓      4.0    否              │
└─────────────────────────────────────────────────┘
```

**單頭表格欄位說明**：

| 顯示欄位 | 來源 |
|---|---|
| 員編 / 姓名 / 部門 / 到職日 | `Employee` (MV001/MV002/MV004/MV021) |
| 證照 (LicenseType) / 描述 | `LicenseMaster` |
| 時數 (應回訓時數) | `TrainingHeader.RequiredHours` |
| 最新回訓日 | `LatestRetrainDate` 衍生欄位（無回訓紀錄顯示「—」） |
| 未達時數 | `RemainingHours` 衍生欄位 |
| 下次回訓 | `NextReviewDate` 衍生欄位 |
| 備註 | `TrainingHeader.Remark` |

> **註**：依使用者要求，**已移除**「應訓 / 累計 / 狀態」三欄；過期/通過資訊改由「未達時數」、「下次回訓」與單身表格的「是否過期」欄位推得。

**進階搜尋欄位**：員工編號、姓名（LIKE）、部門（下拉）、證照類別（下拉）、僅顯示已過期、未達時數 > 0、下次回訓區間

**Modal**：

- 受訓單頭新增：`EmployeeId`（自動補完）/ `LicenseType`（下拉，僅小類）/ `Remark`；`RequiredHours` 系統自動帶 readonly 顯示
- 受訓單頭修改：僅可改 `Remark`；其餘 readonly
- 受訓單身新增/修改：`TrainingDate` / `TrainingType`（單選按鈕：取得證照 / 回訓） / `Hours`

---

## 8. 業務規則

### 8-1. 應回訓時數帶入

新增 `TrainingHeader` 時，`RequiredHours` 由系統依 `LicenseMaster.Hours` 自動填入。修改時不可更動。前端欄位 readonly。

### 8-2. 受訓事件次序規則

同一 `(EmployeeId, LicenseType)` 之 `TrainingDetail` 須符合：

- **時間最早的紀錄必須為 `TrainingType=1 (取得證照)`** — 不可在還沒取得證照前就有回訓紀錄
- **`取得證照` 可有多筆**：當員工取得新版證照時可再新增一筆，每筆 `取得證照` 各自開啟新週期（覆寫「最近一次」）
- **新增/修改/刪除受訓紀錄後**，須由 Service 層重新驗證上述規則；違反則回 400 與相應訊息：
  - `首筆受訓紀錄必須為「取得證照」`
  - `刪除此筆後將造成首筆非取得證照，請先處理其他紀錄`

### 8-3. 達標判定

「整張證照通過」= 當前週期內所有 `TrainingDetail.Hours` 加總 ≥ `TrainingHeader.RequiredHours`。判定發生於 DTO 投影時，無需另寫排程。

### 8-4. 過期狀態自動掃描

`ExpiryScanService : BackgroundService`：

- **觸發時機**：**每日凌晨 00:00:00（Asia/Taipei, UTC+8）執行一次**；應用程式啟動時**不立即掃描**，等待到當日下一個 00:00 才執行
- **生命週期**：隨應用程式 process 啟動而啟動、隨 process 結束而結束；不依賴外部排程器（無 SQL Agent、無 Cron）
- **時區處理**：以 `TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei")` 顯式指定，避免容器或主機時區設定差異造成偏移
- **Clock 抽象**：透過注入的 `IClock` 取得當前時刻（測試時可注入 `FakeClock` 模擬時間推進）

**排程演算法**：

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    while (!stoppingToken.IsCancellationRequested)
    {
        // 計算「下一個 Asia/Taipei 凌晨 00:00」與當前 UTC 的時間差
        var nowLocal = TimeZoneInfo.ConvertTime(_clock.UtcNow, tz);
        var nextMidnightLocal = nowLocal.Date.AddDays(1);          // 隔日 00:00
        var nextMidnightUtc = TimeZoneInfo.ConvertTimeToUtc(nextMidnightLocal, tz);
        var delay = nextMidnightUtc - _clock.UtcNow;

        try { await Task.Delay(delay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        if (!stoppingToken.IsCancellationRequested)
            await ScanOnceAsync(stoppingToken);
    }
}
```

**掃描演算法**（依 4-6 定義的週期概念）：

```
對每組 (EmployeeId, LicenseType)：
    1. 找出所有週期（每個取得證照記錄起算一個週期）
    2. 對每個週期 P：
         IF 週期 P.End < 今天                                 // 週期已結束
            AND 週期 P 內 TrainingDetail.Hours 加總 < RequiredHours    // 未達標
              → 將週期 P 內所有 TrainingDetail.IsExpired = 1
         ELSE
              → 將週期 P 內所有 TrainingDetail.IsExpired = 0  // 達標或仍進行中
    3. 若當前週期內補訓使累計達標 → IsExpired 自動回到 0
```

**邊界情況**：

- 員工尚無 `取得證照` 紀錄：理論上由 §8-2 阻止；若資料異常存在，所有 `回訓` 的 detail 視為孤兒紀錄，`IsExpired = 0`，前端 `OverallStatus` 顯示「未取得」
- 多筆 `取得證照` 之間相隔時間短於 `Years`：較早的週期會被較晚的取得日「截斷」（即每筆取得日所屬週期的真實有效範圍 = `[該取得日, MIN(該取得日+Years年, 下一筆取得日)]`），避免週期相互重疊

### 8-5. 刪除連動

| 刪除對象 | 行為 |
|---|---|
| `LicenseMaster` 且有 `TrainingHeader` 引用 | 擋下；回 409 與訊息 `尚有受訓資料引用此證照，無法刪除` |
| `LicenseMaster` 僅有 `LicensePlantRequirement` 引用 | Cascade 刪除 requirement |
| `TrainingHeader` | Cascade 刪除 `TrainingDetail` |

---

## 9. 驗證規則（FluentValidation）

> 下表配合 §4 已調整之欄位型別。

| Validator | 欄位 | 規則 |
|---|---|---|
| Create/Update LicenseMaster | LicenseType | 必填、≤ 10；格式須為**純整數**（大類）或**含小數點之合法數字**（小類，如 `1.1`、`2.3.1`）；新增時不可重複 |
| | Description | 必填、≤ 70 |
| | Category | 大類列：必須為 NULL；小類列：必填、≤ 10、必須對應已存在的大類 LicenseType |
| | Hours | 小類列：必填、> 0、INT 範圍；大類列：可選（NULL 或正整數皆允許） |
| | Years | 小類列：必填、≥ 1、INT 範圍；大類列：可選（NULL 或正整數皆允許） |
| Create/Update LicensePlantRequirement | Plant | 必填、≤ 6、(LicenseType, Plant) 不可重複；對應 LicenseType 必須是小類 |
| | RequiredCount | ≥ 0 |
| Create/Update TrainingHeader | EmployeeId | 必填、必須存在於 Employee |
| | LicenseType | 必填、必須存在於 LicenseMaster 且為小類 |
| | Remark | ≤ 70 |
| | RequiredHours | 使用者送出時不可包含此欄位（系統覆寫） |
| Create/Update TrainingDetail | TrainingDate | 必填、不可未來日；新增時 (Emp, Lic, Date) 不可重複 |
| | TrainingType | 必填、值 ∈ { 1, 2 } |
| | Hours | 必填、> 0、≤ 9999.9 |

業務規則類驗證（不在 Validator，在 Service 層）：§8-2 受訓事件次序、§8-5 刪除連動、§4-1 大類列 Hours/Years/Category 一致性。

---

## 10. 錯誤處理與回傳

- 沿用 `CrudResult<T>`：`{ Success, Message, Data, Errors }`
- 全域 `ExceptionHandlingMiddleware` 攔截未捕例外 → 500，記錄 stack trace，回傳通用繁中訊息
- HTTP 狀態：200 / 201 / 400（驗證失敗）/ 401（未登入）/ 403（無 action）/ 404（不存在）/ 409（業務衝突，例如刪除被引用）/ 500
- 使用者訊息全部繁中；技術細節僅進 `ILogger`
- HTTP 回應移除 `Server` / `X-Powered-By` 標頭

---

## 11. 安全性

- **SQL Injection**：EF Core 參數化（憲法強制）；驗證層攔截危險字串 `--, ;, /*, */, EXEC, DROP, ALTER`
- **XSS**：jQuery 一律用 `.text()` 顯示使用者資料；禁用 `@Html.Raw()`
- **CSRF**：對狀態變更端點啟用防偽 Token
- **CORS**：僅允許白名單來源
- **機密**：JWT 金鑰、連線字串放 `appsettings.{env}.json` 或環境變數；嚴禁硬編碼；`.env*` 入 `.gitignore`
- **DB Schema 不可由 DevTools 推得**：API 回傳 DTO（不直接回 Entity），且欄位採英文語意名稱（如 `EmployeeId`），無 ERP 編號（如 MV001）外洩
- **員工表禁止寫入**：EF Core 設為 `ToView`，DbContext 不開放 `Add/Update/Remove`

---

## 12. 環境與技術

- .NET 8 / Razor Pages / Clean Architecture（Web ← Core → Infrastructure）
- **部署平台：.NET Aspire**（AppHost 編排，本作業以單一服務身分加入 Aspire 解決方案）
  - **副本數固定為 1**（`WithReplicas(1)`）— `ExpiryScanService` 為定時任務，多副本會導致重複掃描；若未來需要水平擴展，須改採分散式鎖或 leader election
  - Service Discovery 使用 Aspire 內建（連 SQL Server 等資源透過 connection string reference）
  - Observability 透過 Aspire Dashboard 取得 OpenTelemetry trace / log / metric
- SQL Server 版本 `10.0.1600.22` = SQL Server 2008 RTM；分頁採 `ROW_NUMBER()` 不用 `OFFSET/FETCH`；連線啟用 TLS 1.0
- `char` 欄位均加 `IsFixedLength()` + `IsUnicode(false)`
- JSON `PropertyNamingPolicy = null`（PascalCase）
- 未設定連線字串時自動切 InMemory DB 並 seed 範例資料（3 張證照、5 名員工、若干受訓紀錄）
- `USE_INMEMORY_DB` 環境變數可由 `launchSettings.json` 切換

---

## 13. 測試

- Service 層整合測試：覆蓋每個 CRUD 與業務規則（8-2、8-3、8-5）
- BackgroundService 單元測試：注入 `IClock` 模擬時間，驗證 `IsExpired` 雙向轉換
- Validator 測試：每個規則正例與反例
- `dotnet build` 零錯誤零警告才可提交

---

## 14. 偏離憲法事項（Complexity Tracking）

| 偏離項 | 憲法條文 | 偏離理由 | 補償措施 |
|---|---|---|---|
| 單頭列以底色 highlight 取代 checkbox | 第 V 條「單頭行點擊自動選取：checkbox 勾選 + 啟用編輯/刪除按鈕」 | 使用者明確要求；畫面欄位緊湊優先 | 行為仍維持單選；切換列自動清除前一列；按鈕啟用邏輯不變 |

---

## 15. 待決事項（Open Items）

| # | 項目 | 處理時機 |
|---|---|---|
| 1 | （已解決）第 4 章資料表欄位型別/長度 | 使用者已於 spec 中調整 |
| 2 | （已解決）證照「對應大類」清單來源 | 由 LicenseMaster 中 LicenseType 為純整數的列推得 |
| 3 | （已解決）廠別主檔來源 | 接 `CMSMB`：MB001=PlantCode、MB002=PlantName |
| 4 | 員工表 schema 名稱與欄位驗證 | 確認既有員工表名稱、欄位是否就是 MV001/MV002/MV004/MV021 |
| 5 | JWT 簽發系統的 `Authority/Issuer/Audience` 設定值 | 需向登入系統管理者取得 |
| 6 | `Plant.PlantName (MB002)` 的實際長度 | 待 ERP 端 schema 提供 |
| 7 | 視窗參考圖 (`img/`) | 使用者未提供；現以 DingxinErpTemplate 風格替代；後續若提供圖片再對齊 |

---

## 附錄 A：JWT Payload 範例

```json
{
  "employeeId": "E001234",
  "name": "張三",
  "action": "新增,修改,刪除,儲存",
  "department": "D01",
  "jti": "abc-123-def-456",
  "iat": 1715000000,
  "exp": 1715003600
}
```

## 附錄 B：示意 ER 圖

```
┌─────────────────┐         ┌──────────────────────────┐         ┌─────────────┐
│  LicenseMaster  │ 1───N → │ LicensePlantRequirement  │ N───1 → │   Plant     │
│  PK LicenseType │         │  PK LicenseType + Plant  │         │  (CMSMB)    │
│  (大類/小類)    │         │                          │         │  唯讀 view  │
└────────┬────────┘         └──────────────────────────┘         └─────────────┘
         │ 1
         ↓ N
┌─────────────────┐         ┌──────────────────────────┐
│ TrainingHeader  │ 1───N → │      TrainingDetail      │
│ PK Emp + Lic    │         │  PK Emp + Lic + Date     │
└────────┬────────┘         └──────────────────────────┘
         │ N
         ↓ 1
┌─────────────────┐
│   Employee      │  (唯讀 view，映射既有 ERP 員工表)
│  PK EmployeeId  │
└─────────────────┘
```
