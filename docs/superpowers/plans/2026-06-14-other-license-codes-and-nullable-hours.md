# 「其他」證照代碼 + TA004 nullable 實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓 `TrainingHeader.Hours`(TA004)可為 null,並讓「其他」證照以每位員工各自的流水號代碼(`99.{n}` / `X.0.{n}`)寫入 TCSTA,避免 PK 衝突。

**Architecture:** 後端在 `CreateHeaderAsync` 偵測 `IsOther` 請求,用純函式 helper 依現有代碼算出下一個唯一代碼;產生碼只進 TCSTA、不進 TCSMA。Hours 全程改 `int?`,並在 roll-forward 計算與 Excel 匯出處理 null。FK 與欄位 nullable 由使用者在 DB 手動執行(`docs/db-changes-2026-06-14.md`)。

**Tech Stack:** .NET 8 / EF Core(DB-first)/ FluentValidation / xUnit + FluentAssertions + Moq / jQuery + Bootstrap。

**設計文件:** `docs/superpowers/specs/2026-06-14-other-license-codes-and-nullable-hours-design.md`

**通用指令:**(此 repo 無 TCS 解決方案檔;Web 專案會連帶建置 Infrastructure→Core)
- 建置:`dotnet build src/TCS.Web/TCS.Web.csproj`
- 測試:`dotnet test tests/TCS.Tests/TCS.Tests.csproj`
- 已在分支 `feat/other-license-codes-and-nullable-hours`。

---

## 檔案結構

| 檔案 | 責任 | 動作 |
|---|---|---|
| `src/TCS.Core/Entities/TrainingHeader.cs` | `Hours` 改 `int?` | Modify |
| `src/TCS.Core/DTOs/TrainingHeaderDto.cs` | `Hours` 改 `int?` | Modify |
| `src/TCS.Core/Mapping/MappingExtensions.cs` | roll-forward null 語意 | Modify |
| `src/TCS.Core/Services/TrainingService.cs` | `Hours` 帶入 + IsOther 分支 | Modify |
| `src/TCS.Infrastructure/Services/ExcelExportService.cs` | Hours null → 空白 | Modify |
| `src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs` | TA004 `IsRequired(false)` | Modify |
| `src/TCS.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | TCSTA Hours `int?` | Modify |
| `src/TCS.Infrastructure/Migrations/20260522041125_InitialCreate.Designer.cs` | TCSTA Hours `int?` | Modify |
| `schema.json` | TA004 `nullable: true` | Modify |
| `src/TCS.Core/Helpers/OtherLicenseCode.cs` | 代碼前綴與流水號純函式 | Create |
| `src/TCS.Core/DTOs/Requests/CreateTrainingHeaderRequest.cs` | 加 `IsOther`/`Hours`/`Years` | Modify |
| `src/TCS.Core/Validators/Training/CreateTrainingHeaderValidator.cs` | IsOther 分支 | Modify |
| `src/TCS.Core/Interfaces/ITrainingRepository.cs` | 加查詢方法 | Modify |
| `src/TCS.Infrastructure/Repositories/TrainingRepository.cs` | 實作查詢方法 | Modify |
| `src/TCS.Web/wwwroot/js/training.js` | 「其他」送出 + 欄位開放 | Modify |
| `src/TCS.Web/Views/Training/Index.cshtml` | Hours/Years 欄位說明 | Modify |
| `tests/TCS.Tests/Mapping/MappingExtensionsTests.cs` | null Hours 測試 | Modify |
| `tests/TCS.Tests/Helpers/OtherLicenseCodeTests.cs` | helper 單元測試 | Create |
| `tests/TCS.Tests/Validators/TrainingValidatorTests.cs` | IsOther 驗證測試 | Modify |
| `tests/TCS.Tests/Services/TrainingServiceTests.cs` | IsOther 服務測試 | Modify |

---

## Task 1: TrainingHeader.Hours 改為 nullable + ToDto null 語意

**Files:**
- Modify: `src/TCS.Core/Entities/TrainingHeader.cs:13`
- Modify: `src/TCS.Core/DTOs/TrainingHeaderDto.cs:13`
- Modify: `src/TCS.Core/Mapping/MappingExtensions.cs:64-90`
- Modify: `src/TCS.Core/Services/TrainingService.cs:96`
- Modify: `src/TCS.Infrastructure/Services/ExcelExportService.cs:32`
- Modify: `src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs:17`
- Modify: `src/TCS.Infrastructure/Migrations/AppDbContextModelSnapshot.cs:342-343`
- Modify: `src/TCS.Infrastructure/Migrations/20260522041125_InitialCreate.Designer.cs:345-346`
- Modify: `schema.json:58`
- Test: `tests/TCS.Tests/Mapping/MappingExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

在 `tests/TCS.Tests/Mapping/MappingExtensionsTests.cs` 內(class `MappingExtensionsTests`)新增測試。`MakeHeader` 簽章為 `MakeHeader(int hours = 8, int? years = null)`;新增一個可傳 null 的多載與測試:

```csharp
    private static TrainingHeader MakeHeaderNullHours(int? years = 2) =>
        new() { EmployeeId = "E001", LicenseType = "99.1", Hours = null, Years = years };

    [Fact]
    public void ToDto_NullHours_RemainingZero_AnchorStaysAtAcquire()
    {
        var acquire = DateTime.Today.AddYears(-1);
        var details = new List<TrainingDetail>
        {
            D(acquire, 1, 0m),
            D(acquire.AddMonths(1), 2, 5m),   // 有回訓時數，但無門檻
        };
        var dto = MakeHeaderNullHours(years: 2).ToDto(null, null, details, DateOnly.FromDateTime(DateTime.Today));

        dto.Hours.Should().BeNull();
        dto.RemainingHours.Should().Be(0m);                                   // 無時數要求 → 不欠時數
        dto.LatestAcquireDate.Should().Be(DateOnly.FromDateTime(acquire));    // anchor 不前進
        dto.NextReviewDate.Should().Be(DateOnly.FromDateTime(acquire).AddYears(2)); // Years 仍生效
    }
```

- [ ] **Step 2: Run test to verify it fails (compile error 即算 fail)**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~ToDto_NullHours"`
Expected: 編譯失敗(`Hours = null` 無法指派給 `int`;`dto.Hours` 不是 nullable)。

- [ ] **Step 3: Make Hours nullable through the chain**

`src/TCS.Core/Entities/TrainingHeader.cs:13` —
```csharp
    public int? Hours { get; set; }                 // 建立時從 LicenseMaster.Hours 帶入，唯讀；其他類可手動填或留空
```

`src/TCS.Core/DTOs/TrainingHeaderDto.cs:13`(第 7 個位置參數)—
```csharp
    int? Hours,
```

`src/TCS.Core/Services/TrainingService.cs:96` —
```csharp
            Hours = license.Hours,
```

`src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs:17` —
```csharp
        builder.Property(e => e.Hours).HasColumnName("TA004").HasColumnType("int").IsRequired(false);
```

`src/TCS.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` 與
`src/TCS.Infrastructure/Migrations/20260522041125_InitialCreate.Designer.cs`,將 TCSTA 的 Hours 由
```csharp
                    b.Property<int>("Hours")
                        .HasColumnType("int")
                        .HasColumnName("TA004");
```
改為(只改 `<int>` → `<int?>`,兩個檔案各一處,注意是 `HasColumnName("TA004")` 那一個,不是 MA004)—
```csharp
                    b.Property<int?>("Hours")
                        .HasColumnType("int")
                        .HasColumnName("TA004");
```

`schema.json:58` 將 TA004 的 `"nullable": false` 改為 `"nullable": true`。

- [ ] **Step 4: Handle null in ToDto roll-forward**

`src/TCS.Core/Mapping/MappingExtensions.cs`,將週期累加區塊(原 64-82 行)改為僅在有時數門檻時前進 anchor:

```csharp
        // roll-forward 週期推導（§3）：只累加 type 2 時數；達標即前進 anchor，超額滾入（§6 規則2-A）
        // Hours 為 null/0（其他類無時數門檻）時不前進 anchor，acc 僅作累計顯示。
        DateOnly? latestAnchor = latestAcquireDate;
        decimal acc = 0m;
        if (initialAcquire is not null)
        {
            var sessions = details
                .Where(d => d.TrainingType == (int)TrainingType.回訓
                            && d.TrainingDate >= initialAcquire.TrainingDate)
                .OrderBy(d => d.TrainingDate);
            foreach (var s in sessions)
            {
                acc += s.Hours ?? 0m;
                if (header.Hours is int reqHours && reqHours > 0 && acc >= reqHours)
                {
                    latestAnchor = DateOnly.FromDateTime(s.TrainingDate);
                    acc -= reqHours;        // 超額滾入下一週期（§6 規則2-A）
                }
            }
        }
```

並把 `remainingHours`(原第 90 行)改為:
```csharp
        decimal accumulatedHours = acc;
        decimal remainingHours = header.Hours is int req2 ? Math.Max(0m, req2 - acc) : 0m;
```

(第 109 行 `header.Hours` 傳入 DTO 不需改,`int?` 已相容。)

- [ ] **Step 5: Handle null in Excel export**

`src/TCS.Infrastructure/Services/ExcelExportService.cs:32`,將
```csharp
            ws.Cell(row, 7).Value = r.Hours;
```
改為(null → 空白儲存格)—
```csharp
            if (r.Hours.HasValue) ws.Cell(row, 7).Value = r.Hours.Value;
            else ws.Cell(row, 7).Value = "";
```

- [ ] **Step 6: Run the new + existing tests**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj`
Expected: 全數 PASS(新測試通過;既有 `MappingExtensionsTests`/`TrainingServiceTests` 用 `Hours = 16` 等 `int` 指派給 `int?` 仍相容)。

- [ ] **Step 7: Commit**

```bash
git add src/TCS.Core/Entities/TrainingHeader.cs src/TCS.Core/DTOs/TrainingHeaderDto.cs src/TCS.Core/Mapping/MappingExtensions.cs src/TCS.Core/Services/TrainingService.cs src/TCS.Infrastructure/Services/ExcelExportService.cs src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs src/TCS.Infrastructure/Migrations/AppDbContextModelSnapshot.cs src/TCS.Infrastructure/Migrations/20260522041125_InitialCreate.Designer.cs schema.json tests/TCS.Tests/Mapping/MappingExtensionsTests.cs
git commit -m "[Update] TrainingHeader.Hours(TA004) 改為 nullable 並處理 roll-forward/匯出 null"
```

---

## Task 2: OtherLicenseCode 純函式 helper(代碼前綴 + 流水號)

**Files:**
- Create: `src/TCS.Core/Helpers/OtherLicenseCode.cs`
- Test: `tests/TCS.Tests/Helpers/OtherLicenseCodeTests.cs`

- [ ] **Step 1: Write the failing tests**

建立 `tests/TCS.Tests/Helpers/OtherLicenseCodeTests.cs`:

```csharp
using FluentAssertions;
using TCS.Core.Helpers;
using Xunit;

namespace TCS.Tests.Helpers;

public class OtherLicenseCodeTests
{
    [Theory]
    [InlineData("99", "99")]      // 其他大類
    [InlineData("1", "1.0")]      // 其他小類（母大類 1）
    [InlineData("12", "12.0")]
    public void Prefix_DerivesCorrectly(string baseCategory, string expected) =>
        OtherLicenseCode.Prefix(baseCategory).Should().Be(expected);

    [Fact]
    public void Next_NoExisting_StartsAtOne() =>
        OtherLicenseCode.Next("99", new string[0]).Should().Be("99.1");

    [Fact]
    public void Next_SkipsToMaxPlusOne()
    {
        var existing = new[] { "99.1", "99.2", "99.5" };
        OtherLicenseCode.Next("99", existing).Should().Be("99.6");
    }

    [Fact]
    public void Next_MinorPrefix_TwoDots()
    {
        var existing = new[] { "1.0.1", "1.0.2" };
        OtherLicenseCode.Next("1.0", existing).Should().Be("1.0.3");
    }

    [Fact]
    public void Next_IgnoresNonMatchingPrefixAndNonNumericTail()
    {
        // 1.1（真實小類，無 .0.）、1.0.x（非數字）、10.0.1（不同 prefix）皆不計入
        var existing = new[] { "1.1", "1.0.x", "10.0.1", "1.0.2" };
        OtherLicenseCode.Next("1.0", existing).Should().Be("1.0.3");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~OtherLicenseCode"`
Expected: 編譯失敗(`OtherLicenseCode` 不存在)。

- [ ] **Step 3: Implement the helper**

建立 `src/TCS.Core/Helpers/OtherLicenseCode.cs`:

```csharp
namespace TCS.Core.Helpers;

/// <summary>
/// 「其他」證照代碼產生規則(避免 PK 衝突)。
/// 其他大類:base "99" → 代碼 99.{n}(一個點)。
/// 其他小類:base 母大類 X → 代碼 X.0.{n}(兩個點,0 為保留的其他桶)。
/// 流水號由呼叫端提供「同前綴既有代碼」後計算最大值 +1。
/// </summary>
public static class OtherLicenseCode
{
    /// <summary>由 base 母類碼推導代碼前綴。</summary>
    public static string Prefix(string baseCategory) =>
        baseCategory == "99" ? "99" : $"{baseCategory}.0";

    /// <summary>
    /// 依既有代碼算出下一個唯一代碼。只計入「前綴 + 一段純數字」者。
    /// </summary>
    public static string Next(string prefix, IEnumerable<string> existing)
    {
        var head = prefix + ".";
        var max = 0;
        foreach (var code in existing)
        {
            if (code is null || !code.StartsWith(head, StringComparison.Ordinal)) continue;
            var tail = code[head.Length..];
            if (int.TryParse(tail, out var n) && n > max) max = n;
        }
        return $"{prefix}.{max + 1}";
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~OtherLicenseCode"`
Expected: PASS(全部)。

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Helpers/OtherLicenseCode.cs tests/TCS.Tests/Helpers/OtherLicenseCodeTests.cs
git commit -m "[Add] OtherLicenseCode helper:其他證照唯一代碼產生規則"
```

---

## Task 3: Request DTO + Validator 支援 IsOther

**Files:**
- Modify: `src/TCS.Core/DTOs/Requests/CreateTrainingHeaderRequest.cs`
- Modify: `src/TCS.Core/Validators/Training/CreateTrainingHeaderValidator.cs`
- Test: `tests/TCS.Tests/Validators/TrainingValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

在 `tests/TCS.Tests/Validators/TrainingValidatorTests.cs`(class 內、`_header` 為既有的 `CreateTrainingHeaderValidator` 實例)新增:

```csharp
    [Fact]
    public void Header_Other_MajorCategoryWithCustomName_IsValid()
    {
        // IsOther 時允許整數母類碼(99),自定義名稱放 Remark
        var req = new CreateTrainingHeaderRequest("E001", "99", "自定義證照", null, IsOther: true);
        _header.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Header_Other_MinorBaseCategory_IsValid()
    {
        var req = new CreateTrainingHeaderRequest("E001", "1", "自定義證照", null, IsOther: true);
        _header.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Header_Other_MissingCustomName_IsInvalid()
    {
        var req = new CreateTrainingHeaderRequest("E001", "99", null, null, IsOther: true);
        var result = _header.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Remark");
    }

    [Fact]
    public void Header_NonOther_IntegerCategory_StillInvalid()
    {
        // 非其他時,整數大類仍須被擋(維持原規則)
        var req = new CreateTrainingHeaderRequest("E001", "1", null, null);
        _header.Validate(req).IsValid.Should().BeFalse();
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~TrainingValidatorTests"`
Expected: 編譯失敗(`CreateTrainingHeaderRequest` 無 `IsOther` 具名參數)。

- [ ] **Step 3: Add fields to the request record**

`src/TCS.Core/DTOs/Requests/CreateTrainingHeaderRequest.cs` 改為:

```csharp
namespace TCS.Core.DTOs.Requests;

/// <summary>
/// 新增受訓單頭請求。
/// 一般證照:Hours/Years 由 Service 層自 LicenseMaster 帶入(忽略請求中的 Hours/Years)。
/// 其他證照(IsOther=true):LicenseType 帶 base 母類碼(99 或 X),Remark 為自定義名稱,
/// Hours/Years 由使用者手動填(可空),Service 會產生唯一代碼。
/// </summary>
public record CreateTrainingHeaderRequest(
    string EmployeeId,
    string LicenseType,
    string? Remark,
    string? Plant,
    bool IsOther = false,
    int? Hours = null,
    int? Years = null);
```

- [ ] **Step 4: Branch the validator on IsOther**

`src/TCS.Core/Validators/Training/CreateTrainingHeaderValidator.cs` 改為:

```csharp
using FluentValidation;
using TCS.Core.DTOs.Requests;

namespace TCS.Core.Validators.Training;

/// <summary>新增受訓單頭驗證（§9）</summary>
public class CreateTrainingHeaderValidator : AbstractValidator<CreateTrainingHeaderRequest>
{
    public CreateTrainingHeaderValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("員工編號為必填")
            .MaximumLength(10).WithMessage("員工編號不可超過 10 字元")
            .Must(ValidatorHelpers.IsSafe).WithMessage("員工編號含有不允許的字元");

        RuleFor(x => x.LicenseType)
            .NotEmpty().WithMessage("證照類別代碼為必填")
            .MaximumLength(10).WithMessage("證照類別代碼不可超過 10 字元")
            .Must(ValidatorHelpers.IsValidLicenseTypeFormat)
                .WithMessage("證照類別代碼格式不正確");

        // 非其他:單頭只能對應小類（§9）
        RuleFor(x => x.LicenseType)
            .Must(lt => !ValidatorHelpers.IsLicenseTypeCategory(lt))
                .WithMessage("受訓單頭的證照類別必須為小類（含小數點）")
            .When(x => !x.IsOther);

        // 其他:自定義名稱(存 Remark)必填
        RuleFor(x => x.Remark)
            .NotEmpty().WithMessage("其他證照需填寫自定義名稱")
            .When(x => x.IsOther);

        RuleFor(x => x.Remark)
            .MaximumLength(70).WithMessage("備註不可超過 70 字元")
            .Must(ValidatorHelpers.IsSafe).WithMessage("備註含有不允許的字元")
            .When(x => x.Remark is not null);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~TrainingValidatorTests"`
Expected: PASS(全部,含既有測試)。

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/DTOs/Requests/CreateTrainingHeaderRequest.cs src/TCS.Core/Validators/Training/CreateTrainingHeaderValidator.cs tests/TCS.Tests/Validators/TrainingValidatorTests.cs
git commit -m "[Update] CreateTrainingHeaderRequest 加 IsOther/Hours/Years 與對應驗證"
```

---

## Task 4: Repository 查詢同前綴既有代碼

**Files:**
- Modify: `src/TCS.Core/Interfaces/ITrainingRepository.cs`
- Modify: `src/TCS.Infrastructure/Repositories/TrainingRepository.cs`

- [ ] **Step 1: Add the interface method**

`src/TCS.Core/Interfaces/ITrainingRepository.cs`,在 `HeaderExistsAsync` 之後加一行:

```csharp
    Task<List<string>> GetHeaderLicenseTypesByPrefixAsync(string employeeId, string prefix, CancellationToken ct = default);
```

- [ ] **Step 2: Implement it**

`src/TCS.Infrastructure/Repositories/TrainingRepository.cs`,在 `HeaderExistsAsync` 方法之後加入:

```csharp
    public Task<List<string>> GetHeaderLicenseTypesByPrefixAsync(string employeeId, string prefix, CancellationToken ct = default) =>
        _db.TrainingHeaders
            .Where(h => h.EmployeeId == employeeId && h.LicenseType.StartsWith(prefix + "."))
            .Select(h => h.LicenseType)
            .ToListAsync(ct);
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build src/TCS.Web/TCS.Web.csproj`
Expected: 建置成功(尚無新測試;此方法下一個 Task 由 Service 使用並以 mock 驗證)。

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Core/Interfaces/ITrainingRepository.cs src/TCS.Infrastructure/Repositories/TrainingRepository.cs
git commit -m "[Add] ITrainingRepository.GetHeaderLicenseTypesByPrefixAsync"
```

---

## Task 5: Service CreateHeaderAsync 的 IsOther 分支

**Files:**
- Modify: `src/TCS.Core/Services/TrainingService.cs:84-105`
- Test: `tests/TCS.Tests/Services/TrainingServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

在 `tests/TCS.Tests/Services/TrainingServiceTests.cs` 的 `// ── CreateHeader ──` 區塊新增:

```csharp
    [Fact]
    public async Task CreateHeader_Other_Major_GeneratesNextSequence()
    {
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("99", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "99", Description = "其他", Hours = null, Years = null });

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.GetHeaderLicenseTypesByPrefixAsync("E001", "99", default))
            .ReturnsAsync(new List<string> { "99.1", "99.2" });
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "99.3", default)).ReturnsAsync(false);
        TrainingHeader? added = null;
        trainingRepo.Setup(r => r.AddHeaderAsync(It.IsAny<TrainingHeader>(), default))
            .Callback<TrainingHeader, CancellationToken>((h, _) => added = h)
            .Returns(Task.CompletedTask);

        var req = new CreateTrainingHeaderRequest("E001", "99", "我的自定義證照", null, IsOther: true, Hours: 10, Years: 3);
        var result = await BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(req);

        result.LicenseType.Should().Be("99.3");
        result.Hours.Should().Be(10);           // 手動填入
        result.Years.Should().Be(3);
        result.Remark.Should().Be("我的自定義證照");
        added!.LicenseType.Should().Be("99.3");
    }

    [Fact]
    public async Task CreateHeader_Other_Minor_GeneratesTwoDotCode_DefaultsNullHours()
    {
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "1", Description = "電氣", Hours = null, Years = null });

        var trainingRepo = new Mock<ITrainingRepository>();
        trainingRepo.Setup(r => r.GetHeaderLicenseTypesByPrefixAsync("E001", "1.0", default))
            .ReturnsAsync(new List<string>());
        trainingRepo.Setup(r => r.HeaderExistsAsync("E001", "1.0.1", default)).ReturnsAsync(false);
        trainingRepo.Setup(r => r.AddHeaderAsync(It.IsAny<TrainingHeader>(), default)).Returns(Task.CompletedTask);

        var req = new CreateTrainingHeaderRequest("E001", "1", "現場自訂", null, IsOther: true);
        var result = await BuildSvc(trainingRepo.Object, licenseRepo.Object).CreateHeaderAsync(req);

        result.LicenseType.Should().Be("1.0.1");
        result.Hours.Should().BeNull();         // 未填 → null
        result.Years.Should().BeNull();
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~CreateHeader_Other"`
Expected: FAIL(目前 `CreateHeaderAsync` 未處理 IsOther,會以 base 碼 "99" 當 LicenseType,斷言不符)。

- [ ] **Step 3: Implement the IsOther branch**

`src/TCS.Core/Services/TrainingService.cs`,先在檔案頂端確認已有 `using TCS.Core.Helpers;`(已存在)。將 `CreateHeaderAsync`(84-105 行)整段改為:

```csharp
    public async Task<TrainingHeaderDto> CreateHeaderAsync(CreateTrainingHeaderRequest req, CancellationToken ct = default)
    {
        if (req.IsOther)
            return await CreateOtherHeaderAsync(req, ct);

        if (await _repo.HeaderExistsAsync(req.EmployeeId, req.LicenseType, ct))
            throw new InvalidOperationException($"TrainingHeader ({req.EmployeeId},{req.LicenseType}) already exists.");

        var license = await _licenseRepo.GetByIdAsync(req.LicenseType, ct)
            ?? throw new KeyNotFoundException($"LicenseMaster '{req.LicenseType}' not found.");

        var header = new TrainingHeader
        {
            EmployeeId = req.EmployeeId,
            LicenseType = req.LicenseType,
            Hours = license.Hours,
            Years = license.Years,
            Remark = req.Remark,
            Plant = req.Plant
        };
        await _repo.AddHeaderAsync(header, ct);

        var emp = await _empRepo.GetByIdAsync(req.EmployeeId, ct);
        return header.ToDto(emp, license, new List<TrainingDetail>(), DateOnly.FromDateTime(DateTime.Today));
    }

    // 其他證照:以 base 母類碼產生每位員工各自的唯一代碼(99.{n} / X.0.{n}),
    // 產生碼只寫入 TCSTA;Hours/Years 由使用者手動填(可空),自定義名稱存 Remark。
    private async Task<TrainingHeaderDto> CreateOtherHeaderAsync(CreateTrainingHeaderRequest req, CancellationToken ct)
    {
        var baseLicense = await _licenseRepo.GetByIdAsync(req.LicenseType, ct)
            ?? throw new KeyNotFoundException($"LicenseMaster '{req.LicenseType}' not found.");

        var prefix = OtherLicenseCode.Prefix(req.LicenseType);
        var existing = await _repo.GetHeaderLicenseTypesByPrefixAsync(req.EmployeeId, prefix, ct);
        var newCode = OtherLicenseCode.Next(prefix, existing);

        if (await _repo.HeaderExistsAsync(req.EmployeeId, newCode, ct))
            throw new InvalidOperationException($"TrainingHeader ({req.EmployeeId},{newCode}) already exists.");

        var header = new TrainingHeader
        {
            EmployeeId = req.EmployeeId,
            LicenseType = newCode,
            Hours = req.Hours,
            Years = req.Years,
            Remark = req.Remark,
            Plant = req.Plant
        };
        await _repo.AddHeaderAsync(header, ct);

        var emp = await _empRepo.GetByIdAsync(req.EmployeeId, ct);
        // 產生碼無對應主檔,Description 留空(自定義名稱在 Remark 欄顯示)
        return header.ToDto(emp, null, new List<TrainingDetail>(), DateOnly.FromDateTime(DateTime.Today));
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~CreateHeader"`
Expected: PASS(新測試 + 既有 `CreateHeader_Hours_FromLicense`/`CreateHeader_AlreadyExists`/`CreateHeader_LicenseNotFound`)。

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Services/TrainingService.cs tests/TCS.Tests/Services/TrainingServiceTests.cs
git commit -m "[Update] CreateHeaderAsync 支援其他證照唯一代碼產生"
```

---

## Task 6: 前端「其他」送出與欄位開放

**Files:**
- Modify: `src/TCS.Web/wwwroot/js/training.js`(`updateCustomNameVisibility` 約 330-341、`submitHeader` 約 343-389)
- Modify: `src/TCS.Web/Views/Training/Index.cshtml:148-156`

說明:此層無自動化測試,以手動驗收(見步驟 4)。

- [ ] **Step 1: 選「其他」時開放 Hours/Years 輸入**

`src/TCS.Web/wwwroot/js/training.js` 的 `updateCustomNameVisibility` 改為同時切換 Hours/Years 唯讀狀態:

```javascript
function updateCustomNameVisibility() {
    const isOther = $('#m-LicenseType option:selected').data('isOther') === true;
    if (isOther) {
        $('#m-CustomName-group').removeClass('d-none');
        $('#m-Remark-group').addClass('d-none');
        $('#m-CustomName').prop('required', true);
        // 其他:Hours/Years 可手動填,預設清空
        $('#m-header-Hours, #m-header-Years').prop('readonly', false).val('');
    } else {
        $('#m-CustomName-group').addClass('d-none');
        $('#m-Remark-group').removeClass('d-none');
        $('#m-CustomName').prop('required', false);
        // 一般證照:自動帶入、唯讀
        $('#m-header-Hours, #m-header-Years').prop('readonly', true);
    }
}
```

- [ ] **Step 2: submitHeader 送出 IsOther + base 母類碼 + 手動 Hours/Years**

`src/TCS.Web/wwwroot/js/training.js` 的 `submitHeader`,將既有的「其他」判斷與 body 組裝(約 359-369 行)改為:

```javascript
        const isOther = $('#m-LicenseType option:selected').data('isOther') === true;
        let body;
        if (isOther) {
            const customName = $('#m-CustomName').val().trim();
            if (!customName) {
                showModalError('#header-modal-error', '請輸入自定義證照名稱');
                return;
            }
            const hoursRaw = $('#m-header-Hours').val();
            const yearsRaw = $('#m-header-Years').val();
            body = {
                EmployeeId: employeeId,
                LicenseType: licenseType,          // = base 母類碼(99 或 X)
                IsOther: true,
                Remark: customName,
                Plant: $('#m-Plant').val() || null,
                Hours: hoursRaw !== '' ? parseInt(hoursRaw, 10) : null,
                Years: yearsRaw !== '' ? parseInt(yearsRaw, 10) : null
            };
        } else {
            body = {
                EmployeeId: employeeId,
                LicenseType: licenseType,
                Remark: remark,
                Plant: $('#m-Plant').val() || null
            };
        }
```

注意:刪除原本 `if ($('#m-LicenseType option:selected').data('isOther') === true) { ... remark = customName; }` 區塊與其下方原本的 `const body = {...}` 單一組裝,改用上面的條件式 body。其餘(`url`/`method`/`fetch`/回選)維持不變。

- [ ] **Step 3: 更新欄位說明文字**

`src/TCS.Web/Views/Training/Index.cshtml`,將應回訓時數(149-150)與回訓年限(154-155)的 `form-text` 由「系統自動帶入,不可修改」改為「一般證照自動帶入;其他證照可自填,可留空」。`<input>` 的 `readonly` 屬性保留(由 JS 動態切換)。

- [ ] **Step 4: 手動驗收**

啟動站台(依專案慣例,如 `dotnet run --project src/TCS.Web`),於受訓管理頁:
1. 新增 → 選某員工 → 證照類別選「其他（其他）」(99 大類)或「其他（X 描述）」。
2. 確認自定義名稱欄出現、Hours/Years 變為可輸入。
3. 同一員工連續新增兩筆其他大類 → DB 中 TA002 應為 `99.1`、`99.2`(可用 SSMS 或 `docs/db-changes-2026-06-14.md` 的查詢確認;此步驟需 DB 已套用變更)。
Expected: 兩筆皆成功、無 PK 衝突、Hours/Years 依輸入或留空。

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Web/wwwroot/js/training.js src/TCS.Web/Views/Training/Index.cshtml
git commit -m "[Update] 前端其他證照:送出 IsOther 與可手動填 Hours/Years"
```

---

## Task 7: 全量驗證

- [ ] **Step 1: Build + full test run**

Run: `dotnet build src/TCS.Web/TCS.Web.csproj && dotnet test tests/TCS.Tests/TCS.Tests.csproj`
Expected: 建置成功、所有測試 PASS。

- [ ] **Step 2: 確認 DB 文件就緒**

確認 `docs/db-changes-2026-06-14.md` 存在且內容為「動態 DROP FK + ALTER TA004 NULL + 驗證查詢」。提醒使用者:**程式合併/部署前須在目標 DB 執行該文件的指令**,否則新增其他證照(INSERT 非主檔代碼)與 null Hours 都會失敗。

- [ ] **Step 3: (可選)合併**

依 `superpowers:finishing-a-development-branch` 決定合併 / PR。

---

## 已知行為與備註

- **其他證照的 Description 顯示**:產生碼無對應 TCSMA 列,清單的「類別名稱」(Description)欄會空白;自定義名稱顯示於「備註」欄。此為刻意設計(TCSMA 只保留母類 99)。
- **FK 移除後的 Include**:`GetHeadersAsync` 仍 `Include(h => h.LicenseMasterNav)`;對 `99.1` 等代碼 LEFT JOIN 無對應 → 導覽屬性為 null,不報錯。
- **代碼長度**:`LicenseType` 上限 10 字元;`99.{n}`、`{X}.0.{n}` 在合理序號內不會超過。
- **流水號競態**:序號以「查現有 +1」計算,理論上高併發同員工同類別可能撞號;`HeaderExistsAsync` 雙保險會擋下並丟 `InvalidOperationException`(回 409)。單一使用者操作情境下可接受。
