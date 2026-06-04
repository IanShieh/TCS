# 受訓回訓週期與證照起算日（方案2 純推導）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 改用 roll-forward 回訓週期演算法推導 `NextReviewDate`／`accumulatedHours`／`remainingHours`，並以「單一 type 1 不變式」鎖定受訓類型，使回訓週期在累計達標時正確前進。

**Architecture:** 純推導（方案2），不改 DB schema。`MappingExtensions.ToDto(...)` 在讀取時跑 §3 演算法：anchor = 唯一一筆 type 1（取得證照），依日期累加 type 2（回訓）時數，每次累計達 `header.Hours` 即推進 anchor 並把超額時數滾入下一週期（規則 2-A）。Service 層強制「首筆必為 type 1、其餘必為 type 2、編輯不可改 type」的不變式；前端依「是否第一筆」自動決定並鎖定類型 radio。

**Tech Stack:** C# / .NET 8、xUnit + FluentAssertions + Moq、jQuery + Bootstrap 5 modal。

**依據規格：** `docs/superpowers/specs/2026-06-03-training-retrain-cycle-design.md`

---

## File Structure

| 檔案 | 責任 | 動作 |
|---|---|---|
| `src/TCS.Core/Common/TrainingType.cs` | 受訓類型 enum 與語意註解 | Modify（註解） |
| `src/TCS.Core/Mapping/MappingExtensions.cs` | Entity→DTO 投影，含 roll-forward 推導 | Modify（重寫 `TrainingHeader.ToDto` 計算段） |
| `src/TCS.Core/Services/TrainingService.cs` | 受訓單身的不變式守則 | Modify（`AddDetailAsync` 反向守則、`UpdateDetailAsync` 鎖 type） |
| `src/TCS.Web/wwwroot/js/training.js` | 前端 detail modal 類型自動推導與鎖定 | Modify |
| `src/TCS.Web/Views/Training/Index.cshtml` | detail modal radio 標記 | 不需改（沿用既有 radio，由 JS 控制 disabled） |
| `tests/TCS.Tests/Mapping/MappingExtensionsTests.cs` | 演算法單元測試 | Modify（刪除舊語意測試、新增 §7 情境測試） |
| `tests/TCS.Tests/Services/TrainingServiceTests.cs` | 不變式守則測試 | Modify（新增反向守則 / 鎖 type 測試） |

> **關鍵語意變更（務必理解後再實作）：** 新演算法**只累加 type 2（回訓）時數**；type 1（取得證照）那筆的 `Hours` **不計入**回訓累計（spec §7 所有情境的 type 1 時數皆為「—」）。現有 `MappingExtensionsTests` 有數個測試把 type 1 時數算進累計、或使用兩筆 type 1，這些測試編碼的是規格 §2 指出「對客戶模型已壞掉」的舊行為，必須在 Task 2 一併刪除並用 §7 情境測試取代。

---

## Task 1: 更新 TrainingType 語意註解

**Files:**
- Modify: `src/TCS.Core/Common/TrainingType.cs`

- [ ] **Step 1: 更新註解，明確「取得證照為唯一一筆 anchor」**

將檔案內容改為：

```csharp
namespace TCS.Core.Common;

/// <summary>受訓類型（對應 spec §4-4 / retrain-cycle §6 規則3）</summary>
public enum TrainingType : byte
{
    /// <summary>取得證照（初始，每張表頭恰好一筆，作為回訓週期 anchor；時數不計入回訓累計）</summary>
    取得證照 = 1,
    /// <summary>回訓（第二筆起一律此類；依日期累加時數推進週期）</summary>
    回訓 = 2
}
```

- [ ] **Step 2: 建置確認無誤**

Run: `dotnet build src/TCS.Core/TCS.Core.csproj`
Expected: Build succeeded, 0 errors。

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Core/Common/TrainingType.cs
git commit -m "docs: clarify TrainingType semantics for retrain-cycle invariant"
```

---

## Task 2: 重寫 MappingExtensions roll-forward 推導演算法

**Files:**
- Modify: `src/TCS.Core/Mapping/MappingExtensions.cs:39-105`（`TrainingHeader.ToDto` 計算段）
- Test: `tests/TCS.Tests/Mapping/MappingExtensionsTests.cs`

> **演算法（最終定案，對應 spec §3 + §6 規則1-A、2-A）：**
> - `anchor` = 最早一筆 type 1 的受訓日（`latestAcquireDate` DTO 欄位 = 此初始取得日）。
> - 依日期排序、`date >= anchor` 的 type 2 紀錄逐筆累加 `Hours`。
> - 每當累計 `acc >= header.Hours`：該筆受訓日成為新的 `latestAnchor`，並 `acc -= header.Hours`（超額滾入下一週期）。
> - `NextReviewDate = latestAnchor + header.Years 年`（`latestAnchor` 預設為初始取得日；`Years` 為 null → null）。
> - `accumulatedHours = acc`（當前未完成週期累計）；`remainingHours = max(0, header.Hours - acc)`。
> - 無任何 type 1 → `latestAcquireDate`/`NextReviewDate` 皆 null、不累加、`remainingHours = header.Hours`。
> - `latestRetrainDate`（最後一筆 type 2 日）語意不變。

### 2A. 刪除編碼舊行為的測試

- [ ] **Step 1: 刪除下列 5 個舊語意測試方法**

在 `tests/TCS.Tests/Mapping/MappingExtensionsTests.cs` 中**整段刪除**這些方法（含 `[Fact]`）：

- `ToDto_AcquiredWithInsufficientHours_StatusIsNone`（把 type 1 的 4h 計入累計 — 舊行為）
- `ToDto_AcquiredWithEnoughHours_StatusIsComplete`（type 1+type 2 累計達標 — 舊行為）
- `ToDto_LatestAcquireDate_PicksMostRecent`（使用兩筆 type 1 — 違反單一 type 1 不變式）
- `ToDto_AccumulatedHoursOnlyCountsFromLatestAcquire`（兩筆 type 1 — 違反不變式）
- `ToDto_RemainingHoursIsZeroWhenAccumulatedExceedsRequired`（type 1 的 10h 計入累計 — 舊行為）

> 保留其餘測試：`ToDto_NoDetails_StatusIsNone`、`ToDto_OnlyRetrainWithoutAcquire_StatusIsNone`、`ToDto_NextReviewDate_IsLatestAcquireDatePlusYears`、`ToDto_LatestRetrainDate_PicksMostRecent`、`ToDto_NoRetrainRecord_LatestRetrainDateIsNull`、`ToDto_NoYearsOnHeader_NextReviewDateIsNull`、`ToDto_NoDetails_AccumulatedHoursIsZero`、以及所有 Employee / License / LicenseMaster 測試（皆仍符合新行為）。

### 2B. 新增 §7 情境測試（先寫失敗測試）

- [ ] **Step 2: 在 `MappingExtensionsTests.cs` 的「Accumulated & Remaining Hours」區段後，新增以下測試**

```csharp
    // ── Roll-forward 回訓週期（spec §7 情境）─────────────────────────────────

    // 情境2：有回訓但累計未達標 → 期限不前進，remaining = H - acc
    [Fact]
    public void ToDto_RetrainBelowThreshold_PeriodDoesNotAdvance()
    {
        var details = new[]
        {
            D(new DateTime(2020, 1, 1), 1, 0m),
            D(new DateTime(2021, 3, 1), 2, 3m),
            D(new DateTime(2022, 5, 1), 2, 3m)
        };
        var dto = MakeHeader(8, 3).ToDto(null, MakeLicense(8, 3), details, new DateOnly(2022, 6, 1));
        dto.LatestAcquireDate.Should().Be(new DateOnly(2020, 1, 1));
        dto.NextReviewDate.Should().Be(new DateOnly(2023, 1, 1)); // 初始 + 3，未前進
        dto.AccumulatedHours.Should().Be(6m);
        dto.RemainingHours.Should().Be(2m);
    }

    // 情境3：累計剛好達標 → anchor 前進到跨門檻那筆，滾入後 acc = 0
    [Fact]
    public void ToDto_RetrainMeetsThreshold_PeriodAdvances()
    {
        var details = new[]
        {
            D(new DateTime(2020, 1, 1), 1, 0m),
            D(new DateTime(2021, 3, 1), 2, 3m),
            D(new DateTime(2022, 5, 1), 2, 5m)   // 累計 8 → 達標
        };
        var dto = MakeHeader(8, 3).ToDto(null, MakeLicense(8, 3), details, new DateOnly(2023, 1, 1));
        dto.NextReviewDate.Should().Be(new DateOnly(2025, 5, 1)); // 2022-05-01 + 3
        dto.AccumulatedHours.Should().Be(0m);
        dto.RemainingHours.Should().Be(8m);
    }

    // 情境4：連續多週期達標 → anchor 逐次前進，取最後一個完成日 + N
    [Fact]
    public void ToDto_MultipleCyclesMet_AnchorAdvancesEachTime()
    {
        var details = new[]
        {
            D(new DateTime(2020, 1, 1), 1, 0m),
            D(new DateTime(2021, 3, 1), 2, 3m),
            D(new DateTime(2022, 5, 1), 2, 5m),  // C1 = 2022-05-01
            D(new DateTime(2023, 6, 1), 2, 8m)   // C2 = 2023-06-01
        };
        var dto = MakeHeader(8, 3).ToDto(null, MakeLicense(8, 3), details, new DateOnly(2024, 1, 1));
        dto.NextReviewDate.Should().Be(new DateOnly(2026, 6, 1)); // 2023-06-01 + 3
        dto.RemainingHours.Should().Be(8m);
    }

    // 情境5：達標後又有回訓但未達下一週期門檻 → 期限不變，remaining 反映下一週期累計
    [Fact]
    public void ToDto_MetThenPartial_NextReviewUnchanged()
    {
        var details = new[]
        {
            D(new DateTime(2020, 1, 1), 1, 0m),
            D(new DateTime(2022, 5, 1), 2, 8m),  // C1 = 2022-05-01
            D(new DateTime(2024, 1, 1), 2, 4m)   // 下一週期累積中
        };
        var dto = MakeHeader(8, 3).ToDto(null, MakeLicense(8, 3), details, new DateOnly(2024, 6, 1));
        dto.NextReviewDate.Should().Be(new DateOnly(2025, 5, 1)); // 2022-05-01 + 3
        dto.AccumulatedHours.Should().Be(4m);
        dto.RemainingHours.Should().Be(4m);
    }

    // 情境7：超額時數採滾入（規則2-A）→ 多出的時數計入下一週期
    [Fact]
    public void ToDto_OverflowHours_RollsIntoNextCycle()
    {
        var details = new[]
        {
            D(new DateTime(2020, 1, 1), 1, 0m),
            D(new DateTime(2021, 3, 1), 2, 6m),
            D(new DateTime(2022, 5, 1), 2, 5m)   // 累計 11 → 達標，超額 3
        };
        var dto = MakeHeader(8, 3).ToDto(null, MakeLicense(8, 3), details, new DateOnly(2023, 1, 1));
        dto.NextReviewDate.Should().Be(new DateOnly(2025, 5, 1)); // 2022-05-01 + 3
        dto.AccumulatedHours.Should().Be(3m);   // 11 - 8 滾入
        dto.RemainingHours.Should().Be(5m);     // 8 - 3
    }

    // 情境1：只有初始取得、無任何回訓 → NextReview = 取得 + N，remaining = H
    [Fact]
    public void ToDto_OnlyInitialAcquire_RemainingIsFullHours()
    {
        var details = new[] { D(new DateTime(2020, 1, 1), 1, 0m) };
        var dto = MakeHeader(8, 3).ToDto(null, MakeLicense(8, 3), details, new DateOnly(2021, 1, 1));
        dto.LatestAcquireDate.Should().Be(new DateOnly(2020, 1, 1));
        dto.NextReviewDate.Should().Be(new DateOnly(2023, 1, 1));
        dto.AccumulatedHours.Should().Be(0m);
        dto.RemainingHours.Should().Be(8m);
    }
```

- [ ] **Step 3: 執行新測試，確認失敗（紅燈）**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~MappingExtensionsTests"`
Expected: 新增的 6 個情境測試 FAIL（舊演算法會把 type 1 時數計入、或不前進 anchor）。

### 2C. 重寫 ToDto 計算段（轉綠燈）

- [ ] **Step 4: 用 roll-forward 演算法取代 `MappingExtensions.cs` 第 46-86 行的計算段**

將 `src/TCS.Core/Mapping/MappingExtensions.cs` 中 `ToDto(...)` 方法**自註解「最後一筆取得證照」起、到 `OverallStatus status;` 區塊結束**（現行 46-86 行）整段，替換為：

```csharp
        // anchor = 最早一筆 type 1（取得證照）；每張表頭恰好一筆（§6 規則3 不變式）
        var initialAcquire = details
            .Where(d => d.TrainingType == (int)TrainingType.取得證照)
            .OrderBy(d => d.TrainingDate)
            .FirstOrDefault();

        DateOnly? latestAcquireDate = initialAcquire is not null
            ? DateOnly.FromDateTime(initialAcquire.TrainingDate) : null;

        // 最後一筆回訓（語意不變）
        var lastRetrain = details
            .Where(d => d.TrainingType == (int)TrainingType.回訓)
            .OrderByDescending(d => d.TrainingDate)
            .FirstOrDefault();

        DateOnly? latestRetrainDate = lastRetrain is not null
            ? DateOnly.FromDateTime(lastRetrain.TrainingDate) : null;

        // roll-forward 週期推導（§3）：只累加 type 2 時數；達標即前進 anchor，超額滾入（§6 規則2-A）
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
                if (acc >= header.Hours)
                {
                    latestAnchor = DateOnly.FromDateTime(s.TrainingDate);
                    acc -= header.Hours;        // 超額滾入下一週期（§6 規則2-A）
                }
            }
        }

        // 下次回訓 = latestAnchor + Years（Years 為 null → null）
        DateOnly? nextReviewDate = latestAnchor.HasValue && header.Years.HasValue
            ? latestAnchor.Value.AddYears(header.Years.Value)
            : null;

        decimal accumulatedHours = acc;
        decimal remainingHours = Math.Max(0m, header.Hours - acc);

        OverallStatus status;
        if (nextReviewDate.HasValue && nextReviewDate.Value < today)
            status = OverallStatus.已過期;
        else if (remainingHours == 0)
            status = OverallStatus.回訓完成;
        else if (nextReviewDate.HasValue && nextReviewDate.Value <= today.AddYears(1))
            status = OverallStatus.待回訓;
        else
            status = OverallStatus.無;
```

> 下方 `return new TrainingHeaderDto(...)` 區塊不需更動：它引用的 `latestAcquireDate`、`latestRetrainDate`、`nextReviewDate`、`accumulatedHours`、`remainingHours`、`status` 變數名稱與型別皆與上方一致。

- [ ] **Step 5: 執行 Mapping 測試全綠**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~MappingExtensionsTests"`
Expected: 全數 PASS（含新增 6 情境 + 保留的舊測試）。

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/Mapping/MappingExtensions.cs tests/TCS.Tests/Mapping/MappingExtensionsTests.cs
git commit -m "feat: roll-forward retrain-cycle derivation in TrainingHeader.ToDto"
```

---

## Task 3: AddDetailAsync 反向守則（第二筆起拒絕 type 1）

**Files:**
- Modify: `src/TCS.Core/Services/TrainingService.cs:132-134`
- Test: `tests/TCS.Tests/Services/TrainingServiceTests.cs`

- [ ] **Step 1: 新增失敗測試 — header 已有 detail 時，新增 type 1 被拒**

在 `tests/TCS.Tests/Services/TrainingServiceTests.cs` 的「AddDetail」區段內新增：

```csharp
    [Fact]
    public async Task AddDetail_SecondRecordType1_ThrowsInvalidOperation()
    {
        var existing = DateTime.Today.AddMonths(-3);
        var header = new TrainingHeader
        {
            EmployeeId = "E001", LicenseType = "1.1", Hours = 16,
            Details = new List<TrainingDetail>
            {
                new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = existing, TrainingType = 1, Hours = 0m }
            }
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", true, default)).ReturnsAsync(header);

        var req = new CreateTrainingDetailRequest(
            "E001", "1.1", DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), 1, 4m);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => BuildSvc(repoMock.Object).AddDetailAsync(req));
    }
```

- [ ] **Step 2: 執行測試，確認失敗**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~AddDetail_SecondRecordType1"`
Expected: FAIL（目前無此守則，type 1 會被接受並嘗試 AddDetailAsync）。

- [ ] **Step 3: 在 `AddDetailAsync` 補反向守則**

在 `src/TCS.Core/Services/TrainingService.cs` 第 132-134 行的首筆守則**之後**，緊接著加入反向守則。將該段改為：

```csharp
        // §6 規則3: 首筆必須是 type 1（取得證照）
        if (!header.Details.Any() && req.TrainingType != (int)TrainingType.取得證照)
            throw new InvalidOperationException("第一筆受訓記錄必須為「取得證照」（TrainingType = 1）。");

        // §6 規則3: 第二筆起必須是 type 2（回訓），維持單一 type 1 不變式
        if (header.Details.Any() && req.TrainingType == (int)TrainingType.取得證照)
            throw new InvalidOperationException("已有受訓記錄，後續只能新增「回訓」（TrainingType = 2）。");
```

- [ ] **Step 4: 執行測試，確認綠燈（並確認既有首筆守則測試仍過）**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~AddDetail"`
Expected: 全數 PASS。

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Services/TrainingService.cs tests/TCS.Tests/Services/TrainingServiceTests.cs
git commit -m "feat: reject type-1 detail when header already has records"
```

---

## Task 4: UpdateDetailAsync 鎖定 type（不可改類型）

**Files:**
- Modify: `src/TCS.Core/Services/TrainingService.cs:153-162`
- Test: `tests/TCS.Tests/Services/TrainingServiceTests.cs`

- [ ] **Step 1: 新增失敗測試 — 更新時不得改變 TrainingType，只更新 Hours**

在 `TrainingServiceTests.cs` 的「AddDetail」區段之後新增「UpdateDetail」區段：

```csharp
    // ── UpdateDetail ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDetail_DoesNotChangeTrainingType_OnlyHours()
    {
        var date = DateTime.Today.AddMonths(-2);
        var detail = new TrainingDetail
        {
            EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date,
            TrainingType = 2, Hours = 4m
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetDetailAsync("E001", "1.1", date, default)).ReturnsAsync(detail);
        repoMock.Setup(r => r.UpdateDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        // 請求嘗試把 type 改成 1，且改時數為 6
        var req = new UpdateTrainingDetailRequest(
            "E001", "1.1", DateOnly.FromDateTime(date), 1, 6m);
        var dto = await BuildSvc(repoMock.Object).UpdateDetailAsync(req);

        dto.TrainingType.Should().Be(2);   // type 鎖定，忽略請求的 1
        dto.Hours.Should().Be(6m);         // hours 仍更新
    }
```

- [ ] **Step 2: 執行測試，確認失敗**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~UpdateDetail_DoesNotChangeTrainingType"`
Expected: FAIL（目前 `detail.TrainingType = req.TrainingType;` 會把 type 改成 1）。

- [ ] **Step 3: 移除 `UpdateDetailAsync` 覆寫 TrainingType 的行為**

在 `src/TCS.Core/Services/TrainingService.cs` 的 `UpdateDetailAsync` 中，刪除 `detail.TrainingType = req.TrainingType;` 這一行（現行第 158 行）。方法主體改為：

```csharp
    public async Task<TrainingDetailDto> UpdateDetailAsync(UpdateTrainingDetailRequest req, CancellationToken ct = default)
    {
        var trainingDateTime = req.TrainingDate.ToDateTime(TimeOnly.MinValue);
        var detail = await _repo.GetDetailAsync(req.EmployeeId, req.LicenseType, trainingDateTime, ct)
            ?? throw new KeyNotFoundException($"TrainingDetail ({req.EmployeeId},{req.LicenseType},{req.TrainingDate:yyyy-MM-dd}) not found.");
        // §6 規則3: TrainingType 鎖定不可改（首筆永遠 1、其餘永遠 2），僅更新 Hours
        detail.Hours = req.Hours;
        await _repo.UpdateDetailAsync(detail, ct);
        return detail.ToDto();
    }
```

- [ ] **Step 4: 執行測試，確認綠燈**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter "FullyQualifiedName~UpdateDetail"`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Services/TrainingService.cs tests/TCS.Tests/Services/TrainingServiceTests.cs
git commit -m "feat: lock TrainingType on detail update (only Hours mutable)"
```

---

## Task 5: 前端 detail modal 類型自動推導並鎖定

**Files:**
- Modify: `src/TCS.Web/wwwroot/js/training.js`

> 無單元測試框架涵蓋前端；以本機手動驗證（Task 6 列步驟）。`Index.cshtml` 的兩個 radio（`m-TrainingType-1`/`-2`）沿用不改，由 JS 控制 `checked` 與 `disabled`。

- [ ] **Step 1: 新增模組變數 `currentDetailCount`**

在 `training.js` 第 18-19 行（`let selectedHeader = null; let selectedDetail = null;`）之後新增一行：

```javascript
let currentDetailCount = 0;   // 當前選取表頭的單身筆數（決定新增時類型）
```

- [ ] **Step 2: 在 `loadDetails` 中維護 `currentDetailCount`**

在 `loadDetails` 函式中：

(a) 將 `!res.ok` 分支改為先歸零計數。把：

```javascript
    if (!res.ok) {
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="3" class="text-center text-danger"></td>').text('載入失敗')
        ));
        return;
    }
    const items = await res.json();
```

改為：

```javascript
    if (!res.ok) {
        currentDetailCount = 0;
        $tbody.append($('<tr></tr>').append(
            $('<td colspan="3" class="text-center text-danger"></td>').text('載入失敗')
        ));
        return;
    }
    const items = await res.json();
    currentDetailCount = items.length;
```

- [ ] **Step 3: 在 `clearHeaderSelection` 中歸零計數**

在 `clearHeaderSelection` 函式開頭，`selectedDetail = null;` 之後新增：

```javascript
    currentDetailCount = 0;
```

- [ ] **Step 4: 新增鎖定 helper 並改寫 `openDetailModal`**

在 `openDetailModal` 函式**之前**新增 helper：

```javascript
function setTrainingTypeLocked(type) {
    $(`input[name="m-TrainingType"][value="${type}"]`).prop('checked', true);
    $('input[name="m-TrainingType"]').prop('disabled', true);
}
```

接著將 `openDetailModal` 中的 create/edit 分支：

```javascript
    if (mode === 'create') {
        $('#m-TrainingDate').val('').prop('readonly', false);
        $('input[name="m-TrainingType"][value="1"]').prop('checked', true);
        $('#m-Hours').val('');
    } else {
        $('#m-TrainingDate').val(item.TrainingDate).prop('readonly', true);
        $(`input[name="m-TrainingType"][value="${item.TrainingType}"]`).prop('checked', true);
        $('#m-Hours').val(item.Hours ?? '');
    }
```

替換為（新增時依「是否第一筆」自動決定並鎖定；編輯時沿用既有 type 並鎖定）：

```javascript
    if (mode === 'create') {
        $('#m-TrainingDate').val('').prop('readonly', false);
        setTrainingTypeLocked(currentDetailCount === 0 ? 1 : 2);
        $('#m-Hours').val('');
    } else {
        $('#m-TrainingDate').val(item.TrainingDate).prop('readonly', true);
        setTrainingTypeLocked(item.TrainingType);
        $('#m-Hours').val(item.Hours ?? '');
    }
```

> `syncHoursRequired()` 仍會被既有 `openDetailModal` 尾端呼叫；`:checked` 對 disabled radio 仍有效，時數必填提示（type 2）邏輯不受影響。`submitDetail` 以 `$('input[name="m-TrainingType"]:checked').val()` 讀值，disabled radio 仍可讀取，提交不受影響。

- [ ] **Step 5: 建置整個方案，確認無誤**

Run: `dotnet build`
Expected: Build succeeded, 0 errors（前端為靜態檔，不參與編譯；此步確認後端未受牽連）。

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Web/wwwroot/js/training.js
git commit -m "feat: auto-derive and lock TrainingType radio in detail modal"
```

---

## Task 6: 全量驗證與手動確認

**Files:** 無（驗證任務）

- [ ] **Step 1: 跑完整單元測試**

Run: `dotnet test tests/TCS.Tests/TCS.Tests.csproj`
Expected: 全數 PASS，0 failed。

- [ ] **Step 2: 完整建置**

Run: `dotnet build`
Expected: Build succeeded, 0 errors, 0 warnings（或與既有 baseline 一致）。

- [ ] **Step 3: 手動驗證前端類型鎖定（依 §9 最後兩項驗收）**

啟動 Web 專案後，於「受訓管理」頁：
1. 選一個**尚無單身**的表頭 → 點「新增受訓紀錄」→ 類型應自動選「取得證照」且兩個 radio 皆 disabled。
2. 為該表頭新增首筆後，再點「新增受訓紀錄」→ 類型應自動選「回訓」且 disabled。
3. 選任一筆既有單身 → 點「修改」→ 類型 radio 顯示原值且 disabled、日期 readonly、可改時數。
4. 觀察主表：對一筆「初始取得 + 多筆回訓累計達標」的資料，`下次回訓日` 應為「最後完成日 + N 年」、`未達時數` 重置為 H。

- [ ] **Step 4: 對照 spec §9 驗收清單逐項確認**

逐項勾選 spec `docs/superpowers/specs/2026-06-03-training-retrain-cycle-design.md` §9 的驗收條件，全部滿足。

- [ ] **Step 5: 最終 commit（如手動驗證過程有微調）**

```bash
git add -A
git commit -m "test: verify retrain-cycle behavior end-to-end"
```

---

## Self-Review

**1. Spec 覆蓋：**
- §3 roll-forward 演算法 → Task 2（含超額滾入 2-A、提早達標 1-A 經 anchor+N 自然成立）。✓
- §6 規則3 首筆守則 → 既有程式已具備（保留）；反向守則 → Task 3；編輯鎖 type → Task 4；前端自動推導並鎖定 → Task 5。✓
- §7 情境1-8 → Task 2 測試（情境6 提早達標由「anchor+N」涵蓋；情境8 Years null 由保留測試 `ToDto_NoYearsOnHeader_NextReviewDateIsNull` 涵蓋）。✓
- §8 受影響範圍 → Task 1-5 對應全部檔案。✓
- 不需 DB Migration（方案2）→ 計畫未含 migration。✓

**2. Placeholder 掃描：** 無 TBD／TODO／「適當處理」；每個改碼步驟均附完整程式碼。✓

**3. 型別一致性：** `latestAcquireDate`/`latestRetrainDate`/`nextReviewDate`/`accumulatedHours`/`remainingHours`/`status` 變數名與既有 `TrainingHeaderDto` 建構式參數一致；`setTrainingTypeLocked(type)` 名稱前後一致；`currentDetailCount` 宣告與使用一致。✓

> 備註：`OverallStatus` 判定邏輯保持不變（spec §8 未要求調整）。新模型下「回訓完成（remaining==0）」成為達標瞬間的暫態，達標後 `acc` 滾為 0、`remainingHours` 回到 H，狀態多落在「無／待回訓」——此為方案2 的預期行為（剛回訓完，下次期限在數年後）。

---

## Execution Handoff

待使用者確認後，依 `subagent-driven-development`（建議）或 `executing-plans` 逐 Task 實作。
