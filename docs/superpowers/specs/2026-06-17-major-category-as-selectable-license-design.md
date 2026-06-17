# 設計:無小類的大類可直接作為受訓單頭的證照

日期:2026-06-17
分支(建議):`feat/major-category-as-selectable-license`
決議來源:`docs/discuss-2026-06-17-major-category-as-selectable-license.md`(方案 A + 「無小類」結構規則)

## 背景與問題

現行 `CreateTrainingHeaderValidator` 在 `IsOther=false` 時一律擋下純整數大類,要求必須是小類(含小數點):

```csharp
RuleFor(x => x.LicenseType)
    .Must(lt => !ValidatorHelpers.IsLicenseTypeCategory(lt))
        .WithMessage("受訓單頭的證照類別必須為小類（含小數點）")
    .When(x => !x.IsOther);
```

實務上有些證照種類自成一格、不再往下展一層,**大類本身即真實證照**,應可直接被選為受訓對象;且其名稱、時數本就記在主檔,不該被迫走「其他」流程逐筆手填。

`LicenseType` 的整數/小數格式同時被超載表達三件事(階層位置、是否真證照、是否「其他」桶),其中「是否真證照」不該由格式單獨決定。

## 決策(已與使用者確認)

採 **方案 A**:可選大類 = 真證照,與「其他」並存;靠 **「建單當下有無小類」結構規則**區分,**不新增欄位、零 schema 變更**。

- **判定來源**:純整數大類 **+ 當下底下無小類(無任何 `Category = 該碼` 的列)+ 非保留碼 `99`** → 視為可直接選的真證照。即時判斷。
- **名稱**:一般大類/小類名稱一律由主檔 join 帶出,不需手填、不需 Remark;**僅「其他」**才記錄自填名稱。直接選的獨立大類行為等同一般小類(Hours/Years 帶主檔、唯讀)。
- **`99`**:永遠走「其他」(`IsOther=true`),選它帶出下一個 `99.{n}`,排除於「可直接選」之外。既有「其他」序號機制(`OtherLicenseCode`、`99.{n}` / `X.0.{n}`)**完全不動**。
- **判定落點**:「有無小類」需查資料;`CreateTrainingHeaderValidator` 為無狀態、同步,查不了 DB → 此語意閘移至 **Service 層**(`CreateHeaderAsync` 非其他路徑),validator 僅保留格式 / 長度 / 安全檢查。

### 下拉呈現(三分)

| 大類型態 | 呈現 | 送出 |
|---|---|---|
| 有小類(分類抬頭) | 小類清單 + 「其他（X類）」 | 小類 `IsOther=false`;其他 `IsOther=true`, base=X → `X.0.{n}` |
| 無小類且非 99(獨立真證照) | 直接一個可選 option | `IsOther=false`, `LicenseType=X`,名稱 join 帶出 |
| `99`(全域其他) | 「其他」 | `IsOther=true`, base=99 → `99.{n}` |

> 註:現行前端對**每個**大類都已放了「大類本身」option(`training.js:302-304`),本案是把它收斂成「僅無小類大類才放」,並對有小類大類改放小類 + 其他 sentinel。

## 設計

### 第一部分:Service 語意閘(`TrainingService.CreateHeaderAsync` 非其他路徑)

在取得 `license` 後、組 `header` 前,新增大類守門:

```csharp
var license = await _licenseRepo.GetByIdAsync(req.LicenseType, ct)
    ?? throw new KeyNotFoundException($"LicenseMaster '{req.LicenseType}' not found.");

// 純整數大類:僅「非 99 且底下無小類」可直接作為受訓對象
if (ValidatorHelpers.IsLicenseTypeCategory(req.LicenseType))
{
    if (req.LicenseType == "99")
        throw new InvalidOperationException("「其他」大類(99)不可直接選取,請改用其他證照流程。");
    if (await _licenseRepo.HasChildLicensesAsync(req.LicenseType, ct))
        throw new InvalidOperationException($"大類 {req.LicenseType} 底下尚有小類,不可直接作為受訓對象,請選擇小類。");
}
```

- `ValidatorHelpers.IsLicenseTypeCategory` 為同組件(TCS.Core)`internal`,Service 可直接使用;若不願跨命名空間引用,可在 Service 內以等價整數判斷取代。
- 小類(含小數點)走原路徑不受影響;通過守門的無小類大類,後續 `Hours = license.Hours`、`Years = license.Years`、名稱由 `ToDto` 帶 `license.Description`,與一般小類完全一致。

### 第二部分:Repository(`ILicenseRepository` + 實作)

新增查「是否有子小類」:

```csharp
Task<bool> HasChildLicensesAsync(string licenseType, CancellationToken ct = default);
```

實作(對照既有 `HasTrainingHeadersAsync` 模式):

```csharp
public Task<bool> HasChildLicensesAsync(string licenseType, CancellationToken ct = default) =>
    _db.LicenseMasters.AnyAsync(l => l.Category == licenseType, ct);
```

### 第三部分:Validator(`CreateTrainingHeaderValidator`)

**移除** `IsOther=false` 時「必須為小類」那條規則(第 23–26 行)。`IsOther=false` 改為僅保留:
- `LicenseType` 非空、長度 ≤ 10、`IsValidLicenseTypeFormat`(整數或小數皆可)、安全字元。

其餘規則不變:
- `IsOther=true`:base 須為整數大類(`IsLicenseTypeCategory`)、`Remark` 必填。
- `Remark` 長度 / 安全檢查照舊。

### 第四部分:前端(`wwwroot/js/training.js` `openHeaderModal`)

把現行 optgroup 建構區塊(第 298–313 行)改為依「有無小類」三分:

```js
cats.forEach(cat => {
    const $grp = $('<optgroup>').attr('label', `${cat.LicenseType} ${cat.Description}`);
    const children = cachedAllLicenses.filter(x => x.Category === cat.LicenseType);

    if (cat.LicenseType === '99') {
        // 全域其他:大類本身即入口
        $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`)
            .attr('data-is-other', 'true').appendTo($grp);
    } else if (children.length === 0) {
        // 無小類 → 獨立真證照:可直接選,IsOther=false,不放「其他」
        $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`).appendTo($grp);
    } else {
        // 有小類 → 分類抬頭:不放大類本身,列小類 + 其他 sentinel
        children.forEach(x =>
            $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($grp));
        $('<option></option>').val(cat.LicenseType).text(`其他（${cat.Description}）`)
            .attr('data-is-other', 'true').appendTo($grp);
    }
    $grp.appendTo($licSel);
});
```

- 「有無小類」由前端已載入的 `cachedAllLicenses` 直接計算,**不需新 API**。
- `updateCustomNameVisibility` / `updateLicenseSnapshotOnChange` 不需改:無小類大類 `data-is-other` 為 false → 自動走「一般證照」分支(Hours/Years 帶 `licenseMap` 唯讀、名稱 join、無自定義名稱欄)。
- (選配)`Views/Training/Index.cshtml` `#m-LicenseType` 下方提示文字可補一句「單一類別之大類可直接選取」。

## 測試

- **Validator**(`TrainingValidatorTests`):
  - 既有 `Header_LargeCategoryLicenseType_IsInvalid`(整數 `"1"`、`IsOther=false`)行為改變 → **改寫**:整數大類於 validator 層**現為格式有效**(語意改由 Service 把關)。新增一筆正向斷言。
  - `IsOther=true` / `Remark` 等既有案例不變。
- **Service**(`TrainingServiceTests`,mock `_licenseRepo`):
  - 無小類大類(`HasChildLicensesAsync` 回 false)、`IsOther=false` → 成功,`header.LicenseType == "1"`,Hours/Years 帶主檔。
  - 有小類大類(`HasChildLicensesAsync` 回 true)→ 丟 `InvalidOperationException`(含「小類」)。
  - `LicenseType="99"`、`IsOther=false` → 丟 `InvalidOperationException`(含「其他」)。
  - 小類正常路徑回歸不變。
- **Repository**(SQLite,對照 `TrainingRepositoryQueryTests` 模式):`HasChildLicensesAsync` 對有 / 無子小類各回 true / false。

## 影響範圍 / 風險

- **零 schema 變更、零 DB 手動操作**;既有「其他」與序號機制完全不碰。
- **既有資料相容**:現有所有大類若底下有小類,行為與今日一致(仍不可直接選);無小類大類由「被擋」變為「可選」,屬功能新增,不影響舊單頭。
- **Known Issue — 大類動態翻轉**:判定發生在建單當下。某大類今天無小類(可選)、日後長出小類即變回分類抬頭;翻轉後**不追溯作廢**先前已建的裸碼單頭(建立時合法、主檔列仍在、名稱照樣 join),僅之後新單須改走小類。接受此行為並列為 known issue;若日後需「一旦用過即永久可選」,再回到顯式旗標方案。
- API 防護:語意閘在 Service 層,直接呼叫 API 繞過前端亦會被擋。
