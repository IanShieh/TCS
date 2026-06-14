# 設計:TA004 可為 null + 「其他」證照代碼避免 PK 衝突

日期:2026-06-14
分支:`feat/other-license-codes-and-nullable-hours`

## 背景與問題

兩個獨立但都牽涉受訓單頭(`TrainingHeader` / 資料表 `TCSTA`)的問題,合併於同一份設計與分支處理。

1. **TA004 應如同 MA004 為 nullable**
   `schema.json` 中 `MA004`(LicenseMaster.Hours)、`MA005`(Years)、`TA006`(TrainingHeader.Years)皆為 `nullable: true`,唯獨 `TA004`(TrainingHeader.Hours)為 `nullable: false`,是唯一的例外。`TrainingHeader.Hours` 為 `int`,`CreateHeaderAsync` 以 `Hours = license.Hours ?? 0` 把 null 壓成 0。

2. **「其他」定義模糊導致 PK 衝突**
   `TrainingHeader` PK 為 `(EmployeeId, LicenseType)`。目前選「其他」時,前端送出的 `LicenseType` 是母類代碼(`99`,或某大類 X),自定義名稱放 `Remark`。同一員工選兩次同一個「其他」→ PK 重複 → `CreateHeaderAsync` 丟 `InvalidOperationException`("already exists")。此外現有 `CreateTrainingHeaderValidator` 會擋純整數大類,所以「其他大類」(`99`)目前其實送不出去。

## 決策(已與使用者確認)

- **FK 處理**:`schema.json` 正確,實體 DB 存在 FK `TCSTA.TA002 → TCSMA.MA001`(restrict)。產生的代碼只寫進 `TCSTA`、不寫進 `TCSMA`,因此**需移除此 FK**。待執行 SQL 記於 `docs/db-changes-2026-06-14.md`,由使用者在 DB 執行。
- **代碼格式**:
  - 其他大類:base = `99` → `99.{n}`(一個點),例:`99.1`、`99.2`
  - 其他小類(母大類 X):base = `X` → `X.0.{n}`(兩個點,`0` 為保留的「其他桶」),例:`1.0.1`、`1.0.2`
- **流水號範圍**:**每位員工各自計算**(查該員工現有同 base 代碼的最大序號 +1)。不同員工可各自擁有 `99.1`,因 PK 含 EmployeeId 故不衝突。
- **Hours/Years 來源**:「其他」單頭的 Hours/Years **預設 null,但允許使用者在 modal 手動填入**。一般證照單頭維持「自動帶入主檔、唯讀」的既有行為。
- **TCSMA**:僅保留母類 `99`(及既有真實大類/小類),不為每筆「其他」occurrence 新增主檔列。

## 設計

### 第一部分:TA004 改為 nullable

| 層 | 變更 |
|---|---|
| DB(使用者執行) | `ALTER COLUMN TA004 int NULL`(見 SQL 文件) |
| `Core/Entities/TrainingHeader.cs` | `public int Hours` → `public int? Hours` |
| `Infrastructure/Configurations/TrainingHeaderConfiguration.cs` | TA004 加 `.IsRequired(false)` |
| `Core/Services/TrainingService.cs` `CreateHeaderAsync` | `Hours = license.Hours ?? 0` → `Hours = license.Hours` |
| `Core/DTOs/TrainingHeaderDto.cs` | `Hours` 欄位型別 → `int?` |
| `Infrastructure/Migrations/AppDbContextModelSnapshot.cs`、`20260522041125_InitialCreate.Designer.cs` | TCSTA `Hours`:`b.Property<int>` → `b.Property<int?>` |
| `schema.json` | TA004 `nullable: false` → `true` |
| `Core/Mapping/MappingExtensions.cs` `ToDto` | null Hours 的 roll-forward 語意(見下) |

**null Hours 的 roll-forward 語意**(`MappingExtensions.ToDto`,目前直接用 `header.Hours` 做算術):

- 週期累加區塊(`acc >= header.Hours` / `acc -= header.Hours`)僅在 `header.Hours` 有值且 `> 0` 時執行。Hours 為 null/0 時不前進 anchor(無回訓時數門檻)。
- `remainingHours = header.Hours.HasValue ? Math.Max(0m, header.Hours.Value - acc) : 0m`
- `OverallStatus`:`remainingHours == 0` 的判斷在 Hours 為 null 時自然成立(視為無時數要求)。`Years` 仍可獨立決定 `nextReviewDate` 與過期狀態。

### 第二部分:「其他」唯一代碼產生

**介面變更**

`Core/DTOs/Requests/CreateTrainingHeaderRequest.cs` 新增欄位:

```csharp
public record CreateTrainingHeaderRequest(
    string EmployeeId,
    string LicenseType,   // IsOther 時 = base 母類碼(99 或 X)
    string? Remark,       // IsOther 時 = 自定義證照名稱
    string? Plant,
    bool IsOther = false,
    int? Hours = null,    // 僅 IsOther 時採用(手動填,可空)
    int? Years = null);   // 僅 IsOther 時採用(手動填,可空)
```

**Service 變更**(`TrainingService.CreateHeaderAsync`)

當 `req.IsOther` 為 true:
1. 決定 prefix:`req.LicenseType == "99"` → prefix = `"99"`;否則 prefix = `$"{req.LicenseType}.0"`。
2. 查該員工現有 `{prefix}.{n}` 的最大 `n`,+1 得新序號,組成 `LicenseType = $"{prefix}.{n}"`。
3. 以 base 母類碼查主檔(`GetByIdAsync(req.LicenseType)`)確認母類存在(不存在 → KeyNotFound)。
4. `Hours = req.Hours`、`Years = req.Years`(手動填,預設 null),`Remark = req.Remark`。
5. 既有的 `HeaderExistsAsync` 檢查仍對「產生後的代碼」執行(理論上不會撞,雙保險)。

當 `req.IsOther` 為 false:維持現狀(以 `req.LicenseType` 查主檔、Hours/Years 帶主檔、忽略 `req.Hours`/`req.Years`)。

**Repository 變更**(`ITrainingRepository` + 實作)

新增方法查最大序號:

```csharp
Task<int> GetMaxOtherSequenceAsync(string employeeId, string prefix, CancellationToken ct = default);
```

實作:查 `TCSTA` 中該 `EmployeeId` 且 `TA002` 形如 `{prefix}.{數字}` 的列,解析最後一段取最大值;無則回 0。需注意只比對「prefix 後恰好再一段純數字」,避免把 `99.1.x` 之類誤判(其他大類僅一層、其他小類 prefix 已含 `.0`,故各自只多一段)。

**Validator 變更**(`CreateTrainingHeaderValidator`)

- `IsOther` 為 true 時:
  - `LicenseType` 允許純整數母類碼(放行 `IsLicenseTypeCategory`),仍須通過 `IsValidLicenseTypeFormat` 與長度/安全檢查。
  - `Remark`(自定義名稱)必填、長度與安全檢查照舊。
- `IsOther` 為 false 時:維持現狀(`LicenseType` 必須為小類)。

**前端變更**(`wwwroot/js/training.js` + `Views/License/Index.cshtml` 對應欄位)

- `openHeaderModal`:選項標記 `data-is-other` 的邏輯沿用既有(`99` 大類本身、`其他（X）`)。
- 選「其他」時:
  - 顯示自定義名稱欄位(既有 `#m-CustomName`),`#m-CustomName` 必填。
  - 開放 `#m-header-Hours`、`#m-header-Years` 供手動輸入,預設空白。
- `submitHeader`:當所選為「其他」→ `body = { EmployeeId, LicenseType: <base 母類碼>, IsOther: true, Remark: <自定義名稱>, Plant, Hours: <或 null>, Years: <或 null> }`。一般證照維持原 body(不送 Hours/Years/IsOther)。

### 第三部分:DB 變更文件

於 `docs/db-changes-2026-06-14.md` 記錄待執行 SQL(移除 FK + TA004 改 nullable),由使用者在 DB 執行。內容含「動態查出 FK 名稱後 DROP」與「ALTER COLUMN」兩段。

## 測試

- **Validator**:`IsOther=true` 且 base=`99`/`Remark` 有值 → 有效;`Remark` 空 → 無效;`IsOther=false` 送整數大類 → 仍無效。
- **Service**:同一員工連續新增兩筆其他大類 → 得 `99.1`、`99.2`;其他小類(X=1)→ `1.0.1`、`1.0.2`;不同員工各自從 1 起算;`Hours`/`Years` 手動值正確寫入,未填為 null。
- **Mapping(`ToDto`)**:`Hours = null` 時 `remainingHours == 0`、不前進 anchor;`Years` 仍能算 `nextReviewDate`;既有非 null 案例行為不變。
- 既有受影響測試(`LicenseMasterValidatorTests`、`TrainingValidatorTests`、`MappingExtensionsTests`、`TrainingServiceTests`)同步調整。

## 影響範圍 / 風險

- **DB 需手動變更**:移除 FK 與 ALTER 欄位皆由使用者執行;程式合併前若 DB 未變更,新增「其他」會在 INSERT 時因舊 FK 失敗、存 null Hours 也會失敗。
- `int → int?` 連鎖:已確認受影響位置為 `MappingExtensions.ToDto`(roll-forward 算術)與 `ExcelExportService.cs:32`(`ws.Cell(row,7).Value = r.Hours;`,`int?` 需處理 null,例如 `r.Hours ?? ""` 或寫入空白)。`TrainingHeaderDto.Hours` 型別改 `int?` 後,所有消費端皆隨之檢查。
- 代碼長度:`LicenseType` 上限 10 字元。`99.{n}` 與 `{X}.0.{n}` 在合理序號內不會超過(如 `99.999` = 6 字元、`12.0.999` = 8 字元)。
