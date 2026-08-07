# 五項調整設計（2026-08-07）

## 背景

使用者提出五項調整（todo 對話 2026-08-07）：

1. 「證照管理」頁面名稱改為「證照類別」
2. 「廠別需求」頁面加入「匯出 Excel」
3. 「受訓紀錄」進階搜尋「證照類別」下拉可選大類（含其下所有小類）
4. 受訓單身開放任意新增「取得證照」（首筆仍鎖定取得證照；第二筆起預設回訓但可自由選擇）
5. 下次回訓日小於今天須顯示「已過期」，且搜尋能找到已過期資料

零資料庫 schema 變更。

---

## T1 — 「證照管理」改名「證照類別」

純文字修改：

- `src/TCS.Web/Views/Shared/_Layout.cshtml:14` 導覽列連結文字
- `src/TCS.Web/Views/License/Index.cshtml` `ViewData["Title"]` 與 `<h2>`
- 後端使用者可見訊息中出現「證照管理」字樣者一併改為「證照類別」（如 `TrainingService` 錯誤訊息）；程式註解不強制

## T2 — 廠別需求頁「匯出 Excel」

匯出範圍：**目前選擇的廠別**（與畫面所見一致）。

- **Service**：`IExcelExportService` 新增
  `byte[] ExportPlantRequirements(IReadOnlyList<PlantRequirementOverviewDto> rows)`
  （ClosedXML；工作表「廠別需求」；欄位：證照類別／類別名稱／需求數；廠別由檔名承載，工作表內不另加廠別列）
- **API**：`ExportController` 新增 `GET /api/export/plant-requirements?plant={code}`，
  `[RequireAction("列印")]`；資料重用 `ILicenseService.GetRequirementsByPlantAsync`（與畫面同一查詢與排序）
- **前端**（`PlantRequirement/Index.cshtml` + `plantRequirement.js`）：
  按鈕列加「匯出 Excel」；未選廠別時停用；權限由 `TcsAuth`（列印 action）控制；
  檔名 `plant_requirements_{廠別}_{yyyyMMdd}.xlsx`；下載方式沿用 training.js 的 blob 模式

## T3 — 進階搜尋「證照類別」可選大類（含其下小類）

語意：選大類 = 找出「該大類本身掛單的紀錄 + 其下所有小類的紀錄 + 該大類衍生的其他證照（99.x / X.0.x）」。

- **前端**（`training.js` `populateAdvancedDropdowns`）：
  證照下拉改 optgroup 結構——每個大類一組，組內第一個選項為大類本身（值 = 大類碼，文字「{碼} {名稱}（全部）」），其後列該大類的小類；無小類的大類該組僅此一項。選小類行為不變。
- **後端**（`TrainingService.GetHeadersAsync`）：
  現況 `effLicenseType` 直接傳 Repo 做完全比對（`TrainingService.cs:34-36`）。改為：
  1. 以 `_licenseRepo` 查該碼主檔；若為大類（`IsCategory`，即碼不含小數點）→ Repo 端 licenseType 傳 null，改在記憶體過濾：
     `d.LicenseType == 碼 || 小類集合.Contains(d.LicenseType) || d.LicenseType.StartsWith(碼 + ".")`
     （小類集合 = 主檔 `Category == 碼` 者；StartsWith 涵蓋 99.x / X.0.x 其他證照）
  2. 非大類（小類、其他碼、主檔查無）→ 維持現行完全比對，行為不變
  - 分層符合讀取分層規則：跨 LicenseMaster 的邏輯放 Service

## T4 — 單身類型：首筆鎖定取得證照，第二筆起自由選擇

### Service

- **CreateDetail**（`TrainingService.cs` §6 規則3 區段）：
  - 保留「首筆必須為取得證照」語意閘
  - **移除**「已有紀錄後續只能回訓」語意閘 → 第二筆起兩種類型皆可（過期重考 = 再新增一筆取得證照）
  - 保留：日期不重複、append-only（新增日期必晚於最後一筆）、不可未來日、回訓時數必填 > 0（validator）
- **UpdateDetail**：開放一併更新 `TrainingType`（前端 PUT body 既有欄位，現被忽略），但**該筆為首筆（最早一筆）時類型不可改**，維持首筆不變式；時數依更新後類型驗證（回訓 → 必填 > 0）

### 衍生欄位（`MappingExtensions.ToDto`）

- 週期起點（anchor）從「最早一筆取得證照」改為「**最新一筆取得證照**」
- 累計時數只計 anchor 之後的回訓；下次回訓日 = anchor 日 + Years；roll-forward 超額滾入邏輯不變
- 首筆必為取得證照 → anchor 必存在（既有無單身情況維持 null 行為）
- `LatestRetrainDate`（最新回訓日）語意不變：全體最後一筆回訓

### 前端（`training.js`）

- 新增：首筆鎖定「取得證照」（現狀）；第二筆起預設「回訓」、radio 開放切換
- 修改：UI 僅允許改最後一筆——單頭僅一筆時（最後一筆即首筆）類型鎖定；兩筆以上時開放切換，預設為現值
- 回訓必填時數提示（`syncHoursRequired`）已綁 change 事件，radio 解鎖後自然生效

### 測試

- 改寫 `TrainingServiceTests` 中「第二筆非回訓應丟例外」相關案例（改為成功）
- 保留並確認「首筆非取得證照應丟例外」案例
- 新增：UpdateDetail 改類型成功案例、首筆改類型遭拒案例
- `MappingExtensionsTests` 新增：多筆取得證照 → 以最新一筆為 anchor 的衍生欄位案例（含過期重考後狀態脫離已過期）

## T5 — 「已過期」列表顯示 + 搜尋確認

現況：`OverallStatus` 為查詢時即時推導的衍生值（`MappingExtensions.cs:99-107`），非資料庫欄位；
「下次回訓日 < 今天」即為 `已過期`。進階搜尋 `ExpiredOnly` 已存在並對此狀態過濾（`TrainingService.cs:57-58`）。

- **列表加「狀態」欄**（`Training/Index.cshtml` + `training.js renderTable`）：
  置於最後一欄，與 Excel 匯出欄位一致；顯示 已過期／待回訓／回訓完成，`無` 顯示空白；「已過期」以紅色標示（如 `text-danger`）
- 表格 colspan（無資料列）同步 12 → 13
- **搜尋**：`ExpiredOnly` 邏輯不動；T4 anchor 改為最新取得證照後自動正確。補測試：過期重考情境下，重考前可被 ExpiredOnly 找到、重考後脫離已過期

---

## 驗收條件

1. 導覽列與頁面標題顯示「證照類別」
2. 廠別需求頁選定廠別後可下載該廠別需求 Excel，內容與畫面一致；未選廠別按鈕停用；無列印權限者按鈕停用
3. 進階搜尋選大類可同時找到大類本身掛單、其下小類、其 99.x／X.0.x 其他證照的受訓紀錄；選小類行為不變
4. 單身：首筆僅能取得證照；第二筆起預設回訓、可改取得證照；修改最後一筆（非首筆）可切換類型；衍生欄位以最新取得證照起算
5. 列表出現「狀態」欄，過期資料顯示紅色「已過期」；勾選「僅顯示已過期」可篩出
6. 既有測試全綠（現 139 條），新增案例涵蓋上述行為
