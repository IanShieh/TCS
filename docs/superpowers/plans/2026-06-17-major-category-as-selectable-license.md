# 無小類的大類可直接作為受訓單頭證照 — 實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓「純整數大類且當下底下無小類、非保留碼 99」的大類可直接被選為受訓單頭證照（`IsOther=false`），名稱/時數由主檔 join 帶出，零 schema 變更。

**Architecture:** 採方案 A —「建單當下有無小類」結構規則判定。把「是否真證照」的語意閘從無狀態的 validator 移到 `TrainingService.CreateHeaderAsync`(非其他路徑)，因該判定需查 DB(該大類底下有無小類)。Validator 僅保留格式/長度/安全檢查。前端下拉依「有無小類」三分呈現。既有「其他」(99 / 序號)機制完全不動。

**Tech Stack:** C# / .NET、FluentValidation、EF Core(SqlServer 正式 / SQLite in-memory 測試)、xUnit + FluentAssertions + Moq、jQuery 前端。

**設計來源:** `docs/superpowers/specs/2026-06-17-major-category-as-selectable-license-design.md`

---

## 檔案結構

| 檔案 | 角色 | 動作 |
|---|---|---|
| `src/TCS.Core/Interfaces/ILicenseRepository.cs` | License 倉儲介面 | 新增 `HasChildLicensesAsync` |
| `src/TCS.Infrastructure/Repositories/LicenseRepository.cs` | License 倉儲實作 | 實作 `HasChildLicensesAsync` |
| `src/TCS.Core/Services/TrainingService.cs` | 受訓服務 | `CreateHeaderAsync` 非其他路徑加入大類語意閘 |
| `src/TCS.Core/Validators/Training/CreateTrainingHeaderValidator.cs` | 單頭驗證 | 移除「非其他必須為小類」規則 |
| `src/TCS.Web/wwwroot/js/training.js` | 前端單頭 modal | optgroup 改三分建構 |
| `tests/TCS.Tests/Repositories/LicenseRepositoryTests.cs` | 倉儲測試 | **新建** |
| `tests/TCS.Tests/Services/TrainingServiceTests.cs` | 服務測試 | 新增大類語意閘案例 |
| `tests/TCS.Tests/Validators/TrainingValidatorTests.cs` | 驗證測試 | 改寫 2 筆既有整數大類斷言 |

**任務順序理由:** 介面與實作先行(Task 1)，否則 Service(Task 3)引用 `HasChildLicensesAsync` 無法編譯。Validator(Task 2)與 Service(Task 3)互不相依。前端(Task 4)最後。

---

## Task 1: Repository — `HasChildLicensesAsync`(查大類底下有無小類)

**Files:**
- Modify: `src/TCS.Core/Interfaces/ILicenseRepository.cs:13`
- Modify: `src/TCS.Infrastructure/Repositories/LicenseRepository.cs:44`
- Test: `tests/TCS.Tests/Repositories/LicenseRepositoryTests.cs`(新建)

- [ ] **Step 1: 寫失敗測試**

新建 `tests/TCS.Tests/Repositories/LicenseRepositoryTests.cs`。採 `TrainingRepositoryQueryTests` 的 SQLite in-memory 模式:

```csharp
using ERP.Auth.Common.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TCS.Core.Entities;
using TCS.Infrastructure.Data;
using TCS.Infrastructure.Repositories;
using Xunit;

namespace TCS.Tests.Repositories;

public class LicenseRepositoryTests
{
    private static (AppDbContext db, SqliteConnection conn) BuildContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = OFF;";
            pragma.ExecuteNonQuery();
        }
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opts, Mock.Of<ICurrentUserService>());
        db.Database.EnsureCreated();
        return (db, conn);
    }

    [Fact]
    public async Task HasChildLicensesAsync_ReturnsTrue_WhenCategoryHasChildren()
    {
        var (db, conn) = BuildContext();
        using var _c = conn;
        using var _d = db;

        db.LicenseMasters.Add(new LicenseMaster { LicenseType = "1", Description = "電氣大類", Category = null });
        db.LicenseMasters.Add(new LicenseMaster { LicenseType = "1.1", Description = "電氣小類", Category = "1" });
        await db.SaveChangesAsync();

        var repo = new LicenseRepository(db);
        (await repo.HasChildLicensesAsync("1")).Should().BeTrue();
    }

    [Fact]
    public async Task HasChildLicensesAsync_ReturnsFalse_WhenCategoryHasNoChildren()
    {
        var (db, conn) = BuildContext();
        using var _c = conn;
        using var _d = db;

        db.LicenseMasters.Add(new LicenseMaster { LicenseType = "5", Description = "獨立真證照", Category = null });
        await db.SaveChangesAsync();

        var repo = new LicenseRepository(db);
        (await repo.HasChildLicensesAsync("5")).Should().BeFalse();
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~LicenseRepositoryTests"`
Expected: 編譯失敗 —「ILicenseRepository 未包含 HasChildLicensesAsync 的定義」。

- [ ] **Step 3: 介面新增方法**

在 `src/TCS.Core/Interfaces/ILicenseRepository.cs` 的 `HasTrainingHeadersAsync`(第 13 行)下方新增:

```csharp
    Task<bool> HasChildLicensesAsync(string licenseType, CancellationToken ct = default);
```

- [ ] **Step 4: 實作方法**

在 `src/TCS.Infrastructure/Repositories/LicenseRepository.cs` 的 `HasTrainingHeadersAsync`(第 43-44 行)下方新增:

```csharp
    public Task<bool> HasChildLicensesAsync(string licenseType, CancellationToken ct = default) =>
        _db.LicenseMasters.AnyAsync(l => l.Category == licenseType, ct);
```

- [ ] **Step 5: 跑測試確認通過**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~LicenseRepositoryTests"`
Expected: PASS(2 passed)。

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/Interfaces/ILicenseRepository.cs src/TCS.Infrastructure/Repositories/LicenseRepository.cs tests/TCS.Tests/Repositories/LicenseRepositoryTests.cs
git commit -m "feat: 新增 HasChildLicensesAsync 查大類底下有無小類"
```

---

## Task 2: Validator — 移除「非其他必須為小類」規則

**Files:**
- Modify: `src/TCS.Core/Validators/Training/CreateTrainingHeaderValidator.cs:22-26`
- Test: `tests/TCS.Tests/Validators/TrainingValidatorTests.cs:30-36,111-116`

> 背景:整數大類於 validator 層改為**格式有效**(語意改由 Service 把關)。先改測試斷言新行為,確認失敗,再改實作。

- [ ] **Step 1: 改寫失敗測試(兩筆既有斷言反轉)**

在 `tests/TCS.Tests/Validators/TrainingValidatorTests.cs`:

把第 30-36 行的 `Header_LargeCategoryLicenseType_IsInvalid` 整段改為(更名 + 反轉斷言):

```csharp
    [Fact]
    public void Header_NonOther_IntegerCategory_IsNowValidFormat()
    {
        // 語意改由 Service 把關;validator 層整數大類現為格式有效
        var result = _header.Validate(new CreateTrainingHeaderRequest("E001", "1", null, null));
        result.IsValid.Should().BeTrue();
    }
```

把第 111-116 行的 `Header_NonOther_IntegerCategory_StillInvalid` 整段**刪除**(與上方新案例語意相反,已過時):

```csharp
    // （刪除原 Header_NonOther_IntegerCategory_StillInvalid，整數大類於 validator 不再被擋）
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~TrainingValidatorTests"`
Expected: `Header_NonOther_IntegerCategory_IsNowValidFormat` FAIL(`IsValid` 為 false,因規則仍在)。

- [ ] **Step 3: 移除規則**

在 `src/TCS.Core/Validators/Training/CreateTrainingHeaderValidator.cs` 刪除第 22-26 行整段:

```csharp
        // 非其他:單頭只能對應小類（§9）
        RuleFor(x => x.LicenseType)
            .Must(lt => !ValidatorHelpers.IsLicenseTypeCategory(lt))
                .WithMessage("受訓單頭的證照類別必須為小類（含小數點）")
            .When(x => !x.IsOther);
```

刪除後緊接的「其他:base 須為整數大類」規則(原第 28-32 行)與 `Remark` 相關規則保持不變。

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~TrainingValidatorTests"`
Expected: PASS(全數通過;`Header_Other_*`、`Header_Valid_*`、`Header_LicenseTypeInvalidFormat_IsInvalid` 等不受影響)。

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Validators/Training/CreateTrainingHeaderValidator.cs tests/TCS.Tests/Validators/TrainingValidatorTests.cs
git commit -m "refactor: validator 移除非其他必須為小類規則,語意改由 Service 把關"
```

---

## Task 3: Service — `CreateHeaderAsync` 非其他路徑加入大類語意閘

**Files:**
- Modify: `src/TCS.Core/Services/TrainingService.cs:92-93`
- Test: `tests/TCS.Tests/Services/TrainingServiceTests.cs`

> 守門:純整數大類僅「非 99 且底下無小類」可直接作為受訓對象。

- [ ] **Step 1: 寫失敗測試(3 筆新案例)**

在 `tests/TCS.Tests/Services/TrainingServiceTests.cs` 的 `CreateHeader_LicenseNotFound_ThrowsKeyNotFound`(第 61 行)之後新增:

```csharp
    [Fact]
    public async Task CreateHeader_StandaloneCategory_NoChildren_Succeeds()
    {
        // 無小類大類（HasChildLicensesAsync 回 false）、IsOther=false → 成功，Hours/Years 帶主檔
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("5", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "5", Description = "獨立真證照", Hours = 8, Years = 1 });
        licenseRepo.Setup(r => r.HasChildLicensesAsync("5", default)).ReturnsAsync(false);

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "5", default)).ReturnsAsync(false);
        TrainingHeader? added = null;
        trainingRepo.Setup(r => r.AddHeaderAsync(It.IsAny<TrainingHeader>(), default))
            .Callback<TrainingHeader, CancellationToken>((h, _) => added = h)
            .Returns(Task.CompletedTask);

        var result = await BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(
            new CreateTrainingHeaderRequest("E001", "5", null, null));

        result.LicenseType.Should().Be("5");
        result.Hours.Should().Be(8);
        result.Years.Should().Be(1);
        added!.LicenseType.Should().Be("5");
    }

    [Fact]
    public async Task CreateHeader_CategoryWithChildren_ThrowsInvalidOperation()
    {
        // 有小類大類（HasChildLicensesAsync 回 true）→ 丟 InvalidOperationException（含「小類」）
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "1", Description = "電氣大類" });
        licenseRepo.Setup(r => r.HasChildLicensesAsync("1", default)).ReturnsAsync(true);

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "1", default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(
                new CreateTrainingHeaderRequest("E001", "1", null, null)));
        ex.Message.Should().Contain("小類");
    }

    [Fact]
    public async Task CreateHeader_ReservedCode99_NonOther_ThrowsInvalidOperation()
    {
        // LicenseType="99"、IsOther=false → 丟 InvalidOperationException（含「其他」）
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("99", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "99", Description = "其他" });

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "99", default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(
                new CreateTrainingHeaderRequest("E001", "99", null, null)));
        ex.Message.Should().Contain("其他");
    }
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~TrainingServiceTests"`
Expected: 3 筆新案例 FAIL — `CreateHeader_CategoryWithChildren_*` 與 `CreateHeader_ReservedCode99_*` 不會丟例外(目前無守門),`CreateHeader_StandaloneCategory_*` 可能因 `ValidatorHelpers` 引用前的行為而通過(仍須完成 Step 3 才確立)。

- [ ] **Step 3: 加入語意閘**

在 `src/TCS.Core/Services/TrainingService.cs` 的 `CreateHeaderAsync` 內,取得 `license` 之後(第 92-93 行)、組 `header` 之前(第 95 行 `var header = new TrainingHeader` 之前)插入:

```csharp
        // 純整數大類:僅「非 99 且底下無小類」可直接作為受訓對象（語意閘，validator 不查 DB）
        if (ValidatorHelpers.IsLicenseTypeCategory(req.LicenseType))
        {
            if (req.LicenseType == "99")
                throw new InvalidOperationException("「其他」大類(99)不可直接選取,請改用其他證照流程。");
            if (await _licenseRepo.HasChildLicensesAsync(req.LicenseType, ct))
                throw new InvalidOperationException($"大類 {req.LicenseType} 底下尚有小類,不可直接作為受訓對象,請選擇小類。");
        }
```

同時在檔案頂端 `using` 區補上(若尚未存在):

```csharp
using TCS.Core.Validators;
```

> `ValidatorHelpers` 為 `TCS.Core` 同組件 `internal`,Service 可直接呼叫 `IsLicenseTypeCategory`。

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~TrainingServiceTests"`
Expected: PASS(全數通過,含既有 `CreateHeader_Hours_FromLicense`(小類 `1.1` 路徑不受影響)與 `CreateHeader_Other_*`)。

- [ ] **Step 5: 跑整組測試確認無回歸**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj`
Expected: PASS(全數通過)。

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/Services/TrainingService.cs tests/TCS.Tests/Services/TrainingServiceTests.cs
git commit -m "feat: Service 加入大類語意閘,無小類大類可直接作為受訓對象"
```

---

## Task 4: 前端 — `training.js` 下拉三分建構

**Files:**
- Modify: `src/TCS.Web/wwwroot/js/training.js:298-313`

> 後端已是真值來源(Task 3),前端只是把選項收斂成符合規則的呈現:`99`→其他入口、無小類→可直接選、有小類→小類 + 其他 sentinel。

- [ ] **Step 1: 改寫 optgroup 建構區塊**

把 `src/TCS.Web/wwwroot/js/training.js` 第 298-313 行整段:

```js
    const cats = cachedAllLicenses.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));
    cats.forEach(cat => {
        const $grp = $('<optgroup>').attr('label', `${cat.LicenseType} ${cat.Description}`);
        // '99'（其他）大類本身視同「其他」，選取時需填自定義名稱
        const $catOpt = $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`);
        if (cat.LicenseType === '99') $catOpt.attr('data-is-other', 'true');
        $catOpt.appendTo($grp);
        cachedAllLicenses.filter(x => x.Category === cat.LicenseType).forEach(x => {
            $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($grp);
        });
        // '99' 本身即為"其他"大類，不需再加其他選項
        if (cat.LicenseType !== '99') {
            $('<option></option>').val(cat.LicenseType).text(`其他（${cat.Description}）`).attr('data-is-other', 'true').appendTo($grp);
        }
        $grp.appendTo($licSel);
    });
```

改為(依「有無小類」三分):

```js
    const cats = cachedAllLicenses.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));
    cats.forEach(cat => {
        const $grp = $('<optgroup>').attr('label', `${cat.LicenseType} ${cat.Description}`);
        const children = cachedAllLicenses.filter(x => x.Category === cat.LicenseType);

        if (cat.LicenseType === '99') {
            // 全域其他:大類本身即入口，選取需填自定義名稱
            $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`)
                .attr('data-is-other', 'true').appendTo($grp);
        } else if (children.length === 0) {
            // 無小類 → 獨立真證照:可直接選，IsOther=false，名稱/時數由主檔帶出
            $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`).appendTo($grp);
        } else {
            // 有小類 → 分類抬頭:不放大類本身，列小類 + 其他 sentinel
            children.forEach(x =>
                $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($grp));
            $('<option></option>').val(cat.LicenseType).text(`其他（${cat.Description}）`)
                .attr('data-is-other', 'true').appendTo($grp);
        }
        $grp.appendTo($licSel);
    });
```

> `updateCustomNameVisibility`(第 368 行)/`updateLicenseSnapshotOnChange`(第 360 行)**不需改**:無小類大類 `data-is-other` 為 false → 自動走「一般證照」分支(Hours/Years 由 `licenseMap` 帶入且唯讀、名稱 join、無自定義名稱欄)。

- [ ] **Step 2: 手動驗證(無 JS 自動測試框架)**

啟動網站,開「受訓管理」→「新增受訓單頭」,展開證照下拉,逐一確認:
1. 某「有小類」大類(如 `1 電氣`):群組內僅見小類(`1.1`…)與「其他（電氣）」,**不見**裸 `1`。
2. 某「無小類」大類(如獨立 `5`):群組內見可直接選的 `5 獨立真證照`,**無**「其他」項。選它 → Hours/Years 自動帶入且唯讀、無自定義名稱欄。
3. `99 其他`:群組內僅「99 其他」一項,選它 → 出現自定義名稱欄、Hours/Years 可手填。
4. 選「無小類」大類送出 → 成功建立,單頭 `LicenseType` 即該大類碼,名稱由主檔顯示。

- [ ] **Step 3:(選配)補提示文字**

於 `src/TCS.Web/Views/Training/Index.cshtml` `#m-LicenseType` 下方提示可補一句「單一類別之大類可直接選取」。非必須,若既有版面無對應提示區則略過。

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Web/wwwroot/js/training.js
git commit -m "feat: 受訓單頭下拉依有無小類三分,無小類大類可直接選取"
```

---

## 自我檢查(Self-Review)結果

**Spec 覆蓋:**
- §設計第一部分(Service 語意閘)→ Task 3 ✓
- §設計第二部分(Repository `HasChildLicensesAsync`)→ Task 1 ✓
- §設計第三部分(Validator 移除規則)→ Task 2 ✓
- §設計第四部分(前端三分)→ Task 4 ✓
- §測試(Validator 改寫 / Service 3 案例 / Repository true&false)→ Task 2 Step1、Task 3 Step1、Task 1 Step1 ✓

**型別一致性:** `HasChildLicensesAsync(string, CancellationToken)` 介面/實作/Service 呼叫/測試 mock 簽章一致;`IsLicenseTypeCategory` 沿用既有 `ValidatorHelpers` 既有方法。

**Known Issue(沿用 spec):** 判定發生在建單當下。某大類今天無小類(可選)、日後長出小類即變回分類抬頭;翻轉後不追溯作廢先前已建的裸碼單頭(建立時合法、主檔列仍在、名稱照樣 join),僅之後新單須改走小類。接受此行為。

---

## 執行交付

完成計畫並儲存於 `docs/superpowers/plans/2026-06-17-major-category-as-selectable-license.md`。兩種執行方式:

1. **Subagent-Driven(建議)** — 每個 Task 派新 subagent、任務間審查、快速迭代。
2. **Inline Execution** — 在本 session 以 executing-plans 批次執行並設檢查點。

請選擇執行方式。
