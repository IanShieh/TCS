# 五項調整 Implementation Plan（2026-08-07）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 實作 spec `docs/superpowers/specs/2026-08-07-five-adjustments-design.md` 的五項調整：頁面改名、廠別需求匯出、大類搜尋、單身類型開放、已過期顯示。

**Architecture:** ASP.NET Core MVC（TCS.Web）+ Core Service 層 + jQuery 前端。衍生欄位於 `MappingExtensions.ToDto` 即時推導；進階搜尋於 `TrainingService.GetHeadersAsync` 記憶體過濾；Excel 用 ClosedXML。零資料庫 schema 變更。

**Tech Stack:** .NET 8、xUnit + Moq + FluentAssertions、ClosedXML、jQuery + Bootstrap 5

## Global Constraints

- 零資料庫 schema 變更
- JSON 序列化：PascalCase（`PropertyNamingPolicy = null`）、enum 序列化為**數字**（`OverallStatus`：0=回訓完成 1=待回訓 2=已過期 3=無）
- 讀取分層規則：跨 LicenseMaster 的查詢邏輯必走 Service，不放 Controller
- 測試指令：`dotnet test`（repo 根目錄執行；現有 139 條須全綠）
- 建置指令：`dotnet build`
- 工作分支：`feat/adjustments-2026-08-07`，基底 = `feat/major-category-as-selectable-license`（前案未併 main，本案語意依賴它）
- Commit 訊息格式沿用現有中文風格（`feat: ...` / `test: ...`），結尾加 `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`

---

### Task 0: 建立分支

**Files:** 無（git 操作）

- [ ] **Step 1: 從目前分支建立工作分支**

```bash
git checkout feat/major-category-as-selectable-license
git checkout -b feat/adjustments-2026-08-07
```

- [ ] **Step 2: 確認基準測試全綠**

Run: `dotnet test`
Expected: 139 passed, 0 failed

---

### Task 1: T1 —「證照管理」改名「證照類別」

**Files:**
- Modify: `src/TCS.Web/Views/Shared/_Layout.cshtml:14`
- Modify: `src/TCS.Web/Views/License/Index.cshtml:2,4`

**Interfaces:** 無（純文字）

- [ ] **Step 1: 修改導覽列**

`_Layout.cshtml` 第 14 行：

```html
<a class="nav-link" href="@(Context.Request.PathBase)/License">證照類別</a>
```

- [ ] **Step 2: 修改頁面標題**

`License/Index.cshtml` 第 1-4 行改為：

```html
@{
    ViewData["Title"] = "證照類別";
}
<h2>證照類別</h2>
```

- [ ] **Step 3: 建置確認**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Web/Views/Shared/_Layout.cshtml src/TCS.Web/Views/License/Index.cshtml
git commit -m "feat: 證照管理頁改名為證照類別 (T1)"
```

---

### Task 2: T4-a — 衍生欄位週期起點改為「最新一筆取得證照」

**Files:**
- Modify: `src/TCS.Core/Mapping/MappingExtensions.cs:50-57,71-79`
- Test: `tests/TCS.Tests/Mapping/MappingExtensionsTests.cs`

**Interfaces:**
- Produces: `ToDto` 語意變更——anchor = 最新 type 1；Task 9 的 ExpiredOnly 測試依賴此行為。簽名不變。

- [ ] **Step 1: 寫 3 條失敗測試**

加到 `MappingExtensionsTests.cs` 檔尾 class 內（沿用既有 `MakeHeader` / `MakeLicense` / `D` helper）：

```csharp
    // ── 多筆取得證照（2026-08-07 T4：過期重考） ─────────────────────────────

    [Fact]
    public void ToDto_MultipleAcquires_AnchorIsLatestAcquire()
    {
        var details = new[]
        {
            D(new DateTime(2020, 1, 1), 1, 0m),
            D(new DateTime(2021, 1, 1), 2, 8m),
            D(new DateTime(2024, 5, 1), 1, 0m)   // 過期重考
        };
        var dto = MakeHeader(8, 2).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.LatestAcquireDate.Should().Be(new DateOnly(2024, 5, 1));
        dto.NextReviewDate.Should().Be(new DateOnly(2026, 5, 1));
    }

    [Fact]
    public void ToDto_ReacquireAfterExpiry_LeavesExpiredStatus()
    {
        // 2020 取證 + Years=2 → 2022 到期即已過期；2024 重考後脫離已過期
        var expiredOnly = new[] { D(new DateTime(2020, 1, 1), 1, 0m) };
        MakeHeader(8, 2).ToDto(null, MakeLicense(8, 2), expiredOnly, new DateOnly(2024, 6, 1))
            .OverallStatus.Should().Be(OverallStatus.已過期);

        var reacquired = new[]
        {
            D(new DateTime(2020, 1, 1), 1, 0m),
            D(new DateTime(2024, 5, 1), 1, 0m)
        };
        MakeHeader(8, 2).ToDto(null, MakeLicense(8, 2), reacquired, new DateOnly(2024, 6, 1))
            .OverallStatus.Should().NotBe(OverallStatus.已過期);
    }

    [Fact]
    public void ToDto_RetrainsBeforeLatestAcquire_NotAccumulated()
    {
        var details = new[]
        {
            D(new DateTime(2020, 1, 1), 1, 0m),
            D(new DateTime(2021, 1, 1), 2, 6m),   // 舊週期回訓，不應累計
            D(new DateTime(2024, 5, 1), 1, 0m),
            D(new DateTime(2024, 5, 10), 2, 3m)   // 新週期回訓
        };
        var dto = MakeHeader(8, 2).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.AccumulatedHours.Should().Be(3m);
        dto.RemainingHours.Should().Be(5m);
    }
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test --filter "FullyQualifiedName~MappingExtensionsTests"`
Expected: 3 條新測試 FAIL（anchor 仍取最早一筆）

- [ ] **Step 3: 修改 `MappingExtensions.ToDto`**

第 50-57 行（`initialAcquire` 區段）改為：

```csharp
        // anchor = 最新一筆 type 1（取得證照）；過期重考會再新增取得證照，以最新者起算新週期（2026-08-07 T4）
        var latestAcquire = details
            .Where(d => d.TrainingType == (int)TrainingType.取得證照)
            .OrderByDescending(d => d.TrainingDate)
            .FirstOrDefault();

        DateOnly? latestAcquireDate = latestAcquire is not null
            ? DateOnly.FromDateTime(latestAcquire.TrainingDate) : null;
```

第 71-79 行（roll-forward 區段開頭）中 `initialAcquire` 全部改用 `latestAcquire`：

```csharp
        // roll-forward 週期推導（§3）：只累加最新取得證照之後的 type 2 時數；達標即前進 anchor，超額滾入（§6 規則2-A）
        DateOnly? latestAnchor = latestAcquireDate;
        decimal acc = 0m;
        if (latestAcquire is not null)
        {
            var sessions = details
                .Where(d => d.TrainingType == (int)TrainingType.回訓
                            && d.TrainingDate >= latestAcquire.TrainingDate)
                .OrderBy(d => d.TrainingDate);
```

（foreach 內容不變。）

- [ ] **Step 4: 跑測試確認全綠**

Run: `dotnet test`
Expected: 全部 passed（既有測試皆單一取得證照，不受影響）

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Mapping/MappingExtensions.cs tests/TCS.Tests/Mapping/MappingExtensionsTests.cs
git commit -m "feat: 衍生欄位週期起點改為最新一筆取得證照 (T4)"
```

---

### Task 3: T4-b — Service 放寬單身類型閘門、開放修改類型

**Files:**
- Modify: `src/TCS.Core/Services/TrainingService.cs`（`AddDetailAsync` 約 191-227 行、`UpdateDetailAsync` 約 230-239 行）
- Test: `tests/TCS.Tests/Services/TrainingServiceTests.cs`

**Interfaces:**
- Consumes: `ITrainingRepository.GetDetailsAsync(string employeeId, string licenseType, CancellationToken)`（既有）
- Produces: `AddDetailAsync` 接受第二筆起任意類型；`UpdateDetailAsync` 接受 `req.TrainingType` 變更（首筆除外）。簽名皆不變。

- [ ] **Step 1: 改寫既有測試 + 新增測試**

`TrainingServiceTests.cs`——**刪除** `AddDetail_SecondRecordType1_ThrowsInvalidOperation`（約 218-238 行）與 `UpdateDetail_DoesNotChangeTrainingType_OnlyHours`（約 353-373 行），原位置分別放入：

```csharp
    [Fact]
    public async Task AddDetail_SecondRecordType1_Reacquire_Succeeds()
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
        repoMock.Setup(r => r.AddDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        // 過期重考：已有紀錄仍可新增「取得證照」（2026-08-07 T4）
        var req = new CreateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(existing.AddMonths(1)), 1, null);
        var dto = await BuildSvc(repoMock.Object).AddDetailAsync(req);
        dto.TrainingType.Should().Be(1);
    }
```

```csharp
    [Fact]
    public async Task UpdateDetail_NonFirstRecord_ChangesTrainingType()
    {
        var first = DateTime.Today.AddMonths(-4);
        var date = DateTime.Today.AddMonths(-2);
        var detail = new TrainingDetail
        {
            EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date,
            TrainingType = 2, Hours = 4m
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetDetailAsync("E001", "1.1", date, default)).ReturnsAsync(detail);
        repoMock.Setup(r => r.GetDetailsAsync("E001", "1.1", default)).ReturnsAsync(new List<TrainingDetail>
        {
            new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = first, TrainingType = 1, Hours = 0m },
            detail
        });
        repoMock.Setup(r => r.UpdateDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        // 非首筆：類型 2 → 1（過期重考補登）
        var req = new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(date), 1, 6m);
        var dto = await BuildSvc(repoMock.Object).UpdateDetailAsync(req);

        dto.TrainingType.Should().Be(1);
        dto.Hours.Should().Be(6m);
    }

    [Fact]
    public async Task UpdateDetail_FirstRecord_TypeChangeThrows()
    {
        var date = DateTime.Today.AddMonths(-2);
        var detail = new TrainingDetail
        {
            EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date,
            TrainingType = 1, Hours = 0m
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetDetailAsync("E001", "1.1", date, default)).ReturnsAsync(detail);
        repoMock.Setup(r => r.GetDetailsAsync("E001", "1.1", default)).ReturnsAsync(new List<TrainingDetail> { detail });

        // 首筆改回訓 → 拒絕（首筆不變式）
        var req = new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(date), 2, 6m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => BuildSvc(repoMock.Object).UpdateDetailAsync(req));
    }

    [Fact]
    public async Task UpdateDetail_FirstRecord_KeepType1_UpdatesHours()
    {
        var date = DateTime.Today.AddMonths(-2);
        var detail = new TrainingDetail
        {
            EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date,
            TrainingType = 1, Hours = 0m
        };
        var repoMock = new Mock<ITrainingRepository>();
        repoMock.Setup(r => r.GetDetailAsync("E001", "1.1", date, default)).ReturnsAsync(detail);
        repoMock.Setup(r => r.GetDetailsAsync("E001", "1.1", default)).ReturnsAsync(new List<TrainingDetail> { detail });
        repoMock.Setup(r => r.UpdateDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);

        var req = new UpdateTrainingDetailRequest("E001", "1.1", DateOnly.FromDateTime(date), 1, 2m);
        var dto = await BuildSvc(repoMock.Object).UpdateDetailAsync(req);
        dto.Hours.Should().Be(2m);
    }
```

保留 `AddDetail_FirstRecord_MustBeType1`（首筆閘門不動）。

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test --filter "FullyQualifiedName~TrainingServiceTests"`
Expected: `AddDetail_SecondRecordType1_Reacquire_Succeeds`、`UpdateDetail_NonFirstRecord_ChangesTrainingType`、`UpdateDetail_FirstRecord_TypeChangeThrows` FAIL

- [ ] **Step 3: 修改 `TrainingService`**

`AddDetailAsync`：**刪除**「第二筆起必須是 type 2」閘門（約 199-201 行）：

```csharp
        // §6 規則3: 第二筆起必須是 type 2（回訓），維持單一 type 1 不變式
        if (header.Details.Any() && req.TrainingType == (int)TrainingType.取得證照)
            throw new InvalidOperationException("已有受訓記錄，後續只能新增「回訓」（TrainingType = 2）。");
```

並把上方首筆閘門的註解改為：

```csharp
        // 首筆必須是 type 1（取得證照）；第二筆起類型不限（過期重考可再取證，2026-08-07 T4）
```

`UpdateDetailAsync`（約 230-239 行）整段改為：

```csharp
    public async Task<TrainingDetailDto> UpdateDetailAsync(UpdateTrainingDetailRequest req, CancellationToken ct = default)
    {
        var trainingDateTime = req.TrainingDate.ToDateTime(TimeOnly.MinValue);
        var detail = await _repo.GetDetailAsync(req.EmployeeId, req.LicenseType, trainingDateTime, ct)
            ?? throw new KeyNotFoundException($"TrainingDetail ({req.EmployeeId},{req.LicenseType},{req.TrainingDate:yyyy-MM-dd}) not found.");

        // 首筆（最早一筆）鎖定「取得證照」；其餘筆類型可自由修改（2026-08-07 T4）
        var details = await _repo.GetDetailsAsync(req.EmployeeId, req.LicenseType, ct);
        var earliest = details.Min(d => d.TrainingDate);
        if (detail.TrainingDate == earliest && req.TrainingType != (int)TrainingType.取得證照)
            throw new InvalidOperationException("首筆受訓記錄必須維持「取得證照」（TrainingType = 1）。");

        detail.TrainingType = req.TrainingType;
        detail.Hours = req.Hours;
        await _repo.UpdateDetailAsync(detail, ct);
        return detail.ToDto();
    }
```

- [ ] **Step 4: 跑測試確認全綠**

Run: `dotnet test`
Expected: 全部 passed

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Services/TrainingService.cs tests/TCS.Tests/Services/TrainingServiceTests.cs
git commit -m "feat: 單身第二筆起類型不限、修改開放切換類型（首筆鎖定取得證照）(T4)"
```

---

### Task 4: T4-c — 前端單身 Modal 類型 radio 邏輯

**Files:**
- Modify: `src/TCS.Web/wwwroot/js/training.js:484-513`（`setTrainingTypeLocked` / `openDetailModal`）

**Interfaces:**
- Consumes: Task 3 的後端行為（第二筆起任意類型、首筆鎖定）
- Produces: `setTrainingType(type, locked)` 取代 `setTrainingTypeLocked(type)`

- [ ] **Step 1: 改寫 helper 與 `openDetailModal`**

`setTrainingTypeLocked`（training.js 約 484-487 行）改為：

```javascript
function setTrainingType(type, locked) {
    $(`input[name="m-TrainingType"][value="${type}"]`).prop('checked', true);
    $('input[name="m-TrainingType"]').prop('disabled', locked);
}
```

`openDetailModal` 內兩處呼叫改為：

create 分支（原 `setTrainingTypeLocked(currentDetailCount === 0 ? 1 : 2);`）：

```javascript
        // 首筆鎖定「取得證照」；第二筆起預設「回訓」但可自由切換（2026-08-07 T4）
        setTrainingType(currentDetailCount === 0 ? 1 : 2, currentDetailCount === 0);
```

edit 分支（原 `setTrainingTypeLocked(item.TrainingType);`）：

```javascript
        // 僅最後一筆可修改；單頭僅一筆時該筆即首筆 → 類型鎖定，其餘開放切換
        setTrainingType(item.TrainingType, currentDetailCount === 1);
```

- [ ] **Step 2: 確認無其他 `setTrainingTypeLocked` 引用**

Run: `grep -n "setTrainingTypeLocked" src/TCS.Web/wwwroot/js/training.js`
Expected: 無輸出

- [ ] **Step 3: 建置確認**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Web/wwwroot/js/training.js
git commit -m "feat: 前端單身類型 radio 首筆鎖定、第二筆起預設回訓可切換 (T4)"
```

---

### Task 5: T3-a — Service 大類展開搜尋

**Files:**
- Modify: `src/TCS.Core/Services/TrainingService.cs:28-45`（`GetHeadersAsync` 前段）
- Test: `tests/TCS.Tests/Services/TrainingServiceTests.cs`

**Interfaces:**
- Consumes: `ILicenseRepository.GetByIdAsync` / `GetAllAsync`（既有）、`MappingExtensions.IsLicenseTypeCategory(string)`（既有 public helper）
- Produces: 進階搜尋 `query.LicenseType` 為大類碼時回傳「大類本身 + Category 小類 + `碼.` 前綴其他證照」的紀錄

- [ ] **Step 1: 寫 2 條失敗測試**

加到 `TrainingServiceTests.cs` 的「GetHeaders：排序 / 進階搜尋」區段（沿用 `H` / `HeadersRepo` helper）：

```csharp
    [Fact]
    public async Task GetHeaders_AdvancedMajorLicenseType_ExpandsToMinorsAndOthers()
    {
        // 大類 3 → 含本身掛單、Category=3 小類、3.x 其他證照；不含他類 4.1
        var repo = HeadersRepo(H("E001", "3"), H("E001", "3.1"), H("E001", "3.0.1"), H("E001", "4.1"));
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("3", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "3", Description = "堆高機" });
        licenseRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<LicenseMaster>
        {
            new() { LicenseType = "3", Description = "堆高機" },
            new() { LicenseType = "3.1", Description = "小類A", Category = "3" },
            new() { LicenseType = "4.1", Description = "他類小類", Category = "4" }
        });

        var query = new TrainingSearchQuery { LicenseType = "3" };
        var result = await BuildSvc(repo.Object, licenseRepo.Object).GetHeadersAsync(null, null, 1, 10, query);

        result.Items.Select(i => i.LicenseType).Should().BeEquivalentTo("3", "3.1", "3.0.1");
        // 大類不得下推 Repo 做完全比對，須改記憶體展開
        repo.Verify(r => r.GetHeadersAsync(null, null, default), Times.Once);
    }

    [Fact]
    public async Task GetHeaders_AdvancedMinorLicenseType_ExactMatchUnchanged()
    {
        var repo = HeadersRepo(H("E001", "3.1"));
        var licenseRepo = new Mock<ILicenseRepository>();
        licenseRepo.Setup(r => r.GetByIdAsync("3.1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "3.1", Description = "小類A", Category = "3" });

        var query = new TrainingSearchQuery { LicenseType = "3.1" };
        var result = await BuildSvc(repo.Object, licenseRepo.Object).GetHeadersAsync(null, null, 1, 10, query);

        result.Items.Should().HaveCount(1);
        repo.Verify(r => r.GetHeadersAsync(null, "3.1", default), Times.Once);
    }
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test --filter "FullyQualifiedName~TrainingServiceTests"`
Expected: `GetHeaders_AdvancedMajorLicenseType_ExpandsToMinorsAndOthers` FAIL（4.1 也被回傳或 repo 收到 "3"）；minor 測試 PASS（現行行為）

- [ ] **Step 3: 修改 `GetHeadersAsync`**

`TrainingService.cs` 第 34-36 行（`effLicenseType` 計算之後、`GetHeadersAsync` 呼叫處）改為：

```csharp
        var effLicenseType = advancedActive ? query!.LicenseType : licenseType;

        // 大類展開（2026-08-07 T3）：選大類 → 含本身 + Category 小類 + 「碼.」前綴其他證照（99.x / X.0.x）
        // 跨 LicenseMaster 判定屬 Service 職責（讀取分層規則）；非大類維持 Repo 完全比對
        string? repoLicenseType = effLicenseType;
        HashSet<string>? majorExpansion = null;
        string? majorPrefix = null;
        if (!string.IsNullOrWhiteSpace(effLicenseType))
        {
            var lic = await _licenseRepo.GetByIdAsync(effLicenseType, ct);
            if (lic is not null && MappingExtensions.IsLicenseTypeCategory(lic.LicenseType))
            {
                repoLicenseType = null;
                majorPrefix = effLicenseType + ".";
                var allLicenses = await _licenseRepo.GetAllAsync(ct);
                majorExpansion = allLicenses
                    .Where(m => m.Category == effLicenseType)
                    .Select(m => m.LicenseType)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                majorExpansion.Add(effLicenseType);
            }
        }

        var headers = await _repo.GetHeadersAsync(effEmployeeId, repoLicenseType, ct);
        if (majorExpansion is not null)
            headers = headers
                .Where(h => majorExpansion.Contains(h.LicenseType)
                            || h.LicenseType.StartsWith(majorPrefix!, StringComparison.Ordinal))
                .ToList();
```

（過濾在單身/員工逐筆查詢**之前**，避免對被淘汰列多打 DB。其餘程式不動。）

- [ ] **Step 4: 跑測試確認全綠**

Run: `dotnet test`
Expected: 全部 passed

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Services/TrainingService.cs tests/TCS.Tests/Services/TrainingServiceTests.cs
git commit -m "feat: 進階搜尋大類展開為本身+小類+其他證照 (T3)"
```

---

### Task 6: T3-b — 前端進階搜尋證照下拉列出大類

**Files:**
- Modify: `src/TCS.Web/wwwroot/js/training.js:677-683`（`populateAdvancedDropdowns` 證照段）

**Interfaces:**
- Consumes: Task 5 的後端展開行為；`cachedAllLicenses` / `cachedMinorLicenses` / `INTEGER_REGEX`（既有）
- Produces: `#adv-LicenseType` 含大類選項（值 = 大類碼）

- [ ] **Step 1: 改寫證照下拉建構**

`populateAdvancedDropdowns` 內「證照（僅小類）」段（約 677-683 行）改為：

```javascript
    // 證照：大類 optgroup（大類本身可選 = 含其下全部），組內列小類（2026-08-07 T3）
    const $lic = $('#adv-LicenseType').empty();
    $('<option></option>').val('').text('（不限）').appendTo($lic);
    const cats = cachedAllLicenses.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));
    const covered = new Set();
    cats.forEach(cat => {
        const $grp = $('<optgroup>').attr('label', `${cat.LicenseType} ${cat.Description}`);
        $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}（全部）`).appendTo($grp);
        covered.add(cat.LicenseType);
        cachedAllLicenses.filter(x => x.Category === cat.LicenseType).forEach(x => {
            $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($grp);
            covered.add(x.LicenseType);
        });
        $grp.appendTo($lic);
    });
    // 未歸入任何大類的小類（防呆）：附掛於清單尾端
    (cachedMinorLicenses || []).filter(x => !covered.has(x.LicenseType)).forEach(x => {
        $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($lic);
    });
```

- [ ] **Step 2: 建置確認**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Web/wwwroot/js/training.js
git commit -m "feat: 進階搜尋證照下拉改 optgroup 並可選大類 (T3)"
```

---

### Task 7: T2-a — 廠別需求匯出後端

**Files:**
- Modify: `src/TCS.Core/Interfaces/IExcelExportService.cs`
- Modify: `src/TCS.Infrastructure/Services/ExcelExportService.cs`
- Modify: `src/TCS.Web/Controllers/ExportController.cs`

**Interfaces:**
- Consumes: `ILicenseService.GetRequirementsByPlantAsync(string plant, CancellationToken)` → `List<PlantRequirementOverviewDto>`（既有，含自然排序）
- Produces: `byte[] ExportPlantRequirements(IReadOnlyList<PlantRequirementOverviewDto> rows)`；`GET /api/export/plant-requirements?plant={code}`（Task 8 前端呼叫）

（本 codebase 對 Excel 服務與 Controller 無既有單元測試，維持一致；由 Task 10 瀏覽器驗證。）

- [ ] **Step 1: 介面加方法**

`IExcelExportService.cs` 改為：

```csharp
using TCS.Core.DTOs;

namespace TCS.Core.Interfaces;

public interface IExcelExportService
{
    byte[] ExportTrainingHeaders(IReadOnlyList<TrainingHeaderDto> rows);
    /// <summary>廠別需求匯出（單一廠別，欄位與廠別需求頁一致；廠別由檔名承載）</summary>
    byte[] ExportPlantRequirements(IReadOnlyList<PlantRequirementOverviewDto> rows);
}
```

- [ ] **Step 2: 實作**

`ExcelExportService.cs` 在 `ExportTrainingHeaders` 方法後加：

```csharp
    public byte[] ExportPlantRequirements(IReadOnlyList<PlantRequirementOverviewDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("廠別需求");

        var headers = new[] { "證照類別", "類別名稱", "需求數" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.LicenseType;
            ws.Cell(row, 2).Value = r.Description ?? "";
            ws.Cell(row, 3).Value = r.RequiredCount;
            row++;
        }

        ws.ColumnsUsed().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
```

- [ ] **Step 3: Controller 加端點**

`ExportController.cs` 注入 `ILicenseService` 並加端點——欄位與建構子改為：

```csharp
    private readonly ITrainingService _trainingSvc;
    private readonly ILicenseService _licenseSvc;
    private readonly IExcelExportService _excelSvc;

    public ExportController(ITrainingService trainingSvc, ILicenseService licenseSvc, IExcelExportService excelSvc)
    {
        _trainingSvc = trainingSvc;
        _licenseSvc = licenseSvc;
        _excelSvc = excelSvc;
    }
```

`ExportHeaders` 之後加：

```csharp
    [HttpGet("plant-requirements")]
    [RequireAction("列印")]
    public async Task<IActionResult> ExportPlantRequirements([FromQuery] string plant, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plant)) return BadRequest(new { message = "plant 為必填。" });
        var rows = await _licenseSvc.GetRequirementsByPlantAsync(plant, ct);
        var bytes = _excelSvc.ExportPlantRequirements(rows);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"plant_requirements_{plant}_{DateTime.Today:yyyyMMdd}.xlsx");
    }
```

- [ ] **Step 4: 建置 + 既有測試確認**

Run: `dotnet build && dotnet test`
Expected: Build succeeded、全部 passed

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Interfaces/IExcelExportService.cs src/TCS.Infrastructure/Services/ExcelExportService.cs src/TCS.Web/Controllers/ExportController.cs
git commit -m "feat: 廠別需求匯出 Excel API (T2)"
```

---

### Task 8: T2-b — 廠別需求頁匯出按鈕

**Files:**
- Modify: `src/TCS.Web/Views/PlantRequirement/Index.cshtml:11-15`
- Modify: `src/TCS.Web/wwwroot/js/plantRequirement.js`

**Interfaces:**
- Consumes: Task 7 的 `GET /api/export/plant-requirements?plant={code}`；`TcsAuth.applyButtonGuards` / `enableIfAllowed`、`readErrorMessage`、`Toast`（既有）

- [ ] **Step 1: View 加按鈕**

`PlantRequirement/Index.cshtml` 按鈕列（11-15 行）改為：

```html
    <div class="mb-2 d-flex gap-2">
        <button id="btn-add" class="btn btn-primary" disabled>新增</button>
        <button id="btn-edit" class="btn btn-warning" disabled>修改</button>
        <button id="btn-delete" class="btn btn-danger" disabled>刪除</button>
        <button id="btn-export" class="btn btn-success ms-auto" disabled>匯出 Excel</button>
    </div>
```

- [ ] **Step 2: JS 加匯出函式與綁定**

`plantRequirement.js`——「共用」區段前加：

```javascript
// ---------- Excel 匯出 ----------
async function exportExcel() {
    if (!currentPlant) return;
    const res = await fetch(`${BASE}/api/export/plant-requirements?plant=${encodeURIComponent(currentPlant)}`);
    if (!res.ok) { Toast.error(await readErrorMessage(res, '匯出失敗')); return; }
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    const today = new Date().toISOString().slice(0, 10).replace(/-/g, '');
    a.download = `plant_requirements_${currentPlant}_${today}.xlsx`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
```

初始化區段修改：`applyButtonGuards` 物件加一列 `'#btn-export': '列印'`；`#plant-select` change handler 改為：

```javascript
    $('#plant-select').on('change', function () {
        currentPlant = $(this).val();
        if (currentPlant) {
            TcsAuth.enableIfAllowed('#btn-add');
            TcsAuth.enableIfAllowed('#btn-export');
        } else {
            $('#btn-add, #btn-export').prop('disabled', true);
        }
        loadRequirements();
    });
```

並在 `$('#btn-delete').on('click', ...)` 之後加：

```javascript
    $('#btn-export').on('click', exportExcel);
```

- [ ] **Step 3: 建置確認**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Web/Views/PlantRequirement/Index.cshtml src/TCS.Web/wwwroot/js/plantRequirement.js
git commit -m "feat: 廠別需求頁匯出 Excel 按鈕 (T2)"
```

---

### Task 9: T5 — 受訓列表「狀態」欄 + 已過期搜尋測試

**Files:**
- Modify: `src/TCS.Web/Views/Training/Index.cshtml:76-89`（thead）
- Modify: `src/TCS.Web/wwwroot/js/training.js:73-98`（`renderTable`）
- Test: `tests/TCS.Tests/Services/TrainingServiceTests.cs`

**Interfaces:**
- Consumes: `TrainingHeaderDto.OverallStatus`（JSON 數字：0=回訓完成 1=待回訓 2=已過期 3=無）；Task 2 的最新取得證照 anchor 行為

- [ ] **Step 1: 寫失敗測試（ExpiredOnly × 過期重考）**

加到 `TrainingServiceTests.cs` GetHeaders 區段：

```csharp
    [Fact]
    public async Task GetHeaders_ExpiredOnly_FindsExpired_ReacquiredEscapes()
    {
        // E001：2020 取證 + Years=1 → 已過期；E002：同樣過期後重考 → 脫離已過期
        var expiredHeader = new TrainingHeader { EmployeeId = "E001", LicenseType = "1.1", Hours = 8, Years = 1 };
        var reacquiredHeader = new TrainingHeader { EmployeeId = "E002", LicenseType = "1.1", Hours = 8, Years = 1 };
        var repo = new Mock<ITrainingRepository>();
        repo.Setup(r => r.GetHeadersAsync(It.IsAny<string?>(), It.IsAny<string?>(), default))
            .ReturnsAsync(new List<TrainingHeader> { expiredHeader, reacquiredHeader });
        repo.Setup(r => r.GetDetailsAsync("E001", "1.1", default)).ReturnsAsync(new List<TrainingDetail>
        {
            new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = new DateTime(2020, 1, 1), TrainingType = 1, Hours = 0m }
        });
        repo.Setup(r => r.GetDetailsAsync("E002", "1.1", default)).ReturnsAsync(new List<TrainingDetail>
        {
            new() { EmployeeId = "E002", LicenseType = "1.1", TrainingDate = new DateTime(2020, 1, 1), TrainingType = 1, Hours = 0m },
            new() { EmployeeId = "E002", LicenseType = "1.1", TrainingDate = DateTime.Today.AddMonths(-1), TrainingType = 1, Hours = 0m }
        });

        var query = new TrainingSearchQuery { ExpiredOnly = true };
        var result = await BuildSvc(repo.Object).GetHeadersAsync(null, null, 1, 10, query);

        result.Items.Should().ContainSingle(i => i.EmployeeId == "E001");
    }
```

- [ ] **Step 2: 跑測試**

Run: `dotnet test --filter "FullyQualifiedName~GetHeaders_ExpiredOnly"`
Expected: PASS（Task 2/5 已完成的情況下此為驗證性測試；若 FAIL 即發現整合問題，須修正後再繼續）

- [ ] **Step 3: thead 加「狀態」欄**

`Training/Index.cshtml` thead（88 行 `<th>備註</th>` 之後）加：

```html
                            <th>狀態</th>
```

- [ ] **Step 4: `renderTable` 加狀態 cell、colspan 12→13**

`training.js` `renderTable`：無資料列 colspan 改 13：

```javascript
            $('<td colspan="13" class="text-center text-muted"></td>').text('（無資料）')
```

`$('<td></td>').text(r.Remark ?? '').appendTo($tr);` 之後加：

```javascript
        // OverallStatus 數字→標籤（0=回訓完成 1=待回訓 2=已過期 3=無→空白）
        const statusLabel = { 0: '回訓完成', 1: '待回訓', 2: '已過期' }[r.OverallStatus] ?? '';
        $('<td></td>').text(statusLabel)
            .toggleClass('text-danger fw-bold', r.OverallStatus === 2)
            .appendTo($tr);
```

- [ ] **Step 5: 建置 + 全測試**

Run: `dotnet build && dotnet test`
Expected: Build succeeded、全部 passed

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Web/Views/Training/Index.cshtml src/TCS.Web/wwwroot/js/training.js tests/TCS.Tests/Services/TrainingServiceTests.cs
git commit -m "feat: 受訓列表加狀態欄(已過期紅字)並補已過期搜尋測試 (T5)"
```

---

### Task 10: 總驗證（測試 + 瀏覽器實測）

**Files:** 無新增修改（驗證發現問題才修）

- [ ] **Step 1: 全套測試**

Run: `dotnet test`
Expected: 147 條全部 passed，0 failed（139 − 2 改寫刪除 + 10 新增）

- [ ] **Step 2: 啟動網站瀏覽器實測（spec 驗收條件 1-5）**

啟動：`dotnet run --project src/TCS.Web`（或依專案慣用方式），逐項確認：

1. 導覽列顯示「證照類別」，License 頁標題「證照類別」
2. 廠別需求頁：未選廠別「匯出 Excel」停用；選廠別後可下載，開檔內容 = 畫面清單（證照類別/類別名稱/需求數）
3. 受訓紀錄進階搜尋：證照下拉出現大類（全部）選項；選大類可同時找到大類本身、小類、99.x/X.0.x 其他證照的紀錄；選小類行為不變
4. 單身：無紀錄時新增鎖定「取得證照」；已有紀錄時預設「回訓」可切「取得證照」；修改最後一筆（單頭≥2筆）可切換類型；新增第二筆「取得證照」後，單頭「下次回訓」以新取證日起算
5. 列表尾欄「狀態」：過期資料紅字「已過期」；勾「僅顯示已過期」只列已過期
6. （權限）無「列印」action 帳號：兩個匯出按鈕皆停用

- [ ] **Step 3: 推送分支**

```bash
git push -u origin feat/adjustments-2026-08-07
```
