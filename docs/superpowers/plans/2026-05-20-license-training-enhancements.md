# License & Training Enhancements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Four changes: auto-suggest LicenseType code from Category selection (T1+T2), fix Excel export to respect search filters (T3), and add Plant field to TrainingHeader create/edit flow (T4).

**Architecture:** T1+T2 are pure frontend edits to `license.js`. T3 removes two dead parameters from `ExportController`. T4 is full-stack: backend entity/DTO/mapping/migration + frontend `training.js` + Razor view.

**Tech Stack:** ASP.NET Core 8 Web API, EF Core 8 (SQL Server), jQuery/Bootstrap 5, no test project (manual acceptance tests only).

---

## File Map

| File | Change |
|------|--------|
| `src/TCS.Web/wwwroot/js/license.js` | T1+T2: add `cachedAllLicensesFull`, new `suggestNextLicenseType()`, modify `ensureAllCategoriesLoaded()`, `populateCategoryOptions()`, `syncCategoryVisibility()`, `submitLicense`, `openLicenseModal` |
| `src/TCS.Web/Controllers/ExportController.cs` | T3: remove dead `employeeId`/`licenseType` params |
| `src/TCS.Core/Entities/TrainingHeader.cs` | T4: add `Plant` property |
| `src/TCS.Core/DTOs/Requests/CreateTrainingHeaderRequest.cs` | T4: add `Plant` |
| `src/TCS.Core/DTOs/Requests/UpdateTrainingHeaderRequest.cs` | T4: add `Plant` |
| `src/TCS.Core/DTOs/TrainingHeaderDto.cs` | T4: add `Plant` |
| `src/TCS.Core/Mapping/MappingExtensions.cs` | T4: map `Plant` in `TrainingHeader.ToDto()` |
| `src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs` | T4: configure `Plant` column |
| `src/TCS.Core/Services/TrainingService.cs` | T4: assign `Plant` in Create/Update |
| `src/TCS.Web/wwwroot/js/training.js` | T4: `loadPlantOptions()`, update `openHeaderModal()`, `submitHeader()` |
| `src/TCS.Web/Views/Training/Index.cshtml` | T4: add `#m-Plant` select field in modal |

---

## Task 1: T3 — Remove dead parameters from ExportController

**Files:**
- Modify: `src/TCS.Web/Controllers/ExportController.cs:23-34`

- [ ] **Step 1: Edit ExportController.cs**

Replace the entire `ExportHeaders` action with this (removes the two dead top-level params; the service already reads employeeId/licenseType from `query` when advanced search is active):

```csharp
[HttpGet("training-headers")]
public async Task<IActionResult> ExportHeaders(
    [FromQuery] TrainingSearchQuery? query = null,
    CancellationToken ct = default)
{
    var result = await _trainingSvc.GetHeadersAsync(null, null, 1, int.MaxValue, query, ct);
    var bytes = _excelSvc.ExportTrainingHeaders(result.Items);
    return File(bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"training_export_{DateTime.Today:yyyyMMdd}.xlsx");
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/TCS.Web/TCS.Web.csproj`
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Web/Controllers/ExportController.cs
git commit -m "fix: remove unused employeeId/licenseType params from ExportController"
```

---

## Task 2: T2 — Add "大類" sentinel to Category dropdown (license.js)

**Files:**
- Modify: `src/TCS.Web/wwwroot/js/license.js`

This task adds the `__MAJOR__` sentinel option to `#m-Category` and updates `syncCategoryVisibility()` and `submitLicense` to handle it.

- [ ] **Step 1: Replace `syncCategoryVisibility()` to handle `__MAJOR__`**

Find and replace the entire `syncCategoryVisibility` function (lines 211–224):

```js
function syncCategoryVisibility() {
    const v = $('#m-LicenseType').val();
    const categoryIsMajor = $('#m-Category').val() === '__MAJOR__';
    if (isLicenseTypeMajor(v) || categoryIsMajor) {
        $('#m-Category-wrap').hide();
        $('#m-Category').val('__MAJOR__');
        $('#m-Hours-required, #m-Years-required').addClass('d-none');
        $('#m-Hours, #m-Years').prop('required', false);
    } else {
        $('#m-Category-wrap').show();
        const minor = isLicenseTypeMinor(v);
        $('#m-Hours-required, #m-Years-required').toggleClass('d-none', !minor);
        $('#m-Hours, #m-Years').prop('required', minor);
    }
}
```

- [ ] **Step 2: Update `submitLicense` to exclude sentinel from Category value**

Find in `submitLicense` (around line 240):
```js
        Category: isMajor ? null : ($('#m-Category').val().trim() || null),
```

Replace with:
```js
        const catVal = $('#m-Category').val();
        Category: (isMajor || catVal === '__MAJOR__') ? null : (catVal.trim() || null),
```

Wait — this is inside an object literal. Rewrite the full `body` object in `submitLicense`:

```js
    const catVal = $('#m-Category').val();
    const body = {
        LicenseType: lt,
        Description: $('#m-Description').val().trim(),
        Category: (isMajor || catVal === '__MAJOR__') ? null : (catVal.trim() || null),
        Hours: $('#m-Hours').val() !== '' ? parseInt($('#m-Hours').val(), 10) : null,
        Years: $('#m-Years').val() !== '' ? parseInt($('#m-Years').val(), 10) : null
    };
```

- [ ] **Step 3: Update `populateCategoryOptions()` to add the sentinel option**

The current function uses `cachedLicenses` (current page only). Change it to use `cachedAllCategories` so the full list is always available, and insert the `__MAJOR__` sentinel after the empty option:

```js
function populateCategoryOptions() {
    const $sel = $('#m-Category').empty();
    $('<option></option>').val('').text('-- 請選擇 --').appendTo($sel);
    $('<option></option>').val('__MAJOR__').text('大類').appendTo($sel);
    (cachedAllCategories || []).forEach(x => {
        $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($sel);
    });
}
```

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Web/wwwroot/js/license.js
git commit -m "feat(license): add __MAJOR__ sentinel option to Category dropdown"
```

---

## Task 3: T1 — Auto-suggest LicenseType when Category changes (license.js)

**Files:**
- Modify: `src/TCS.Web/wwwroot/js/license.js`

Depends on Task 2 (sentinel option already in place). Adds the full-list cache and the suggestion logic.

- [ ] **Step 1: Add `cachedAllLicensesFull` variable**

After the existing `let cachedAllCategories = null;` line (line 13), add:

```js
let cachedAllLicensesFull = null; // 全部證照（含小類），T1 推算序號用
```

- [ ] **Step 2: Modify `ensureAllCategoriesLoaded()` to also populate `cachedAllLicensesFull`**

Replace the entire `ensureAllCategoriesLoaded` function:

```js
async function ensureAllCategoriesLoaded() {
    if (cachedAllCategories !== null) return;
    const res = await fetch(`${API}?page=1&pageSize=9999`);
    if (!res.ok) { cachedAllCategories = []; cachedAllLicensesFull = []; return; }
    const data = await res.json();
    cachedAllLicensesFull = data.Items || [];
    cachedAllCategories = cachedAllLicensesFull.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));

    const $sel = $('#adv-Category').empty();
    $('<option></option>').val('').text('（不限）').appendTo($sel);
    cachedAllCategories.forEach(x => {
        $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($sel);
    });
}
```

- [ ] **Step 3: Add `suggestNextLicenseType()` function**

Insert this new function after `ensureAllCategoriesLoaded()`:

```js
function suggestNextLicenseType(selectedCategory) {
    if (selectedCategory === '__MAJOR__') {
        const majors = cachedAllLicensesFull
            .filter(x => INTEGER_REGEX.test(x.LicenseType))
            .map(x => parseInt(x.LicenseType, 10));
        return String(majors.length ? Math.max(...majors) + 1 : 1);
    }
    if (selectedCategory && INTEGER_REGEX.test(selectedCategory)) {
        const subs = cachedAllLicensesFull
            .filter(x => x.Category === selectedCategory)
            .map(x => {
                const parts = x.LicenseType.split('.');
                return parseInt(parts[parts.length - 1], 10);
            })
            .filter(n => !isNaN(n));
        const nextIdx = subs.length ? Math.max(...subs) + 1 : 1;
        return `${selectedCategory}.${nextIdx}`;
    }
    return '';
}
```

- [ ] **Step 4: Make `openLicenseModal` async and add the `#m-Category` change handler**

Replace the entire `openLicenseModal` function:

```js
async function openLicenseModal(mode, item) {
    $('#license-modal-error').addClass('d-none').text('');
    $('#license-modal-title').text(mode === 'create' ? '新增證照' : '修改證照');

    await ensureAllCategoriesLoaded();
    populateCategoryOptions();

    if (mode === 'create') {
        $('#m-LicenseType').val('').prop('disabled', false);
        $('#m-Description').val('');
        $('#m-Category').val('');
        $('#m-Hours').val('');
        $('#m-Years').val('');
    } else {
        $('#m-LicenseType').val(item.LicenseType).prop('disabled', true);
        $('#m-Description').val(item.Description ?? '');
        $('#m-Category').val(item.Category ?? '');
        $('#m-Hours').val(item.Hours ?? '');
        $('#m-Years').val(item.Years ?? '');
    }
    $('#license-form').data('mode', mode);
    syncCategoryVisibility();
    licenseModal.show();
}
```

Note: the original code used `prop('readonly', false/true)` on `#m-LicenseType`. The spec requires `disabled` for auto-suggested values, so create mode starts with `disabled: false` and the change handler will set it.

- [ ] **Step 5: Add `#m-Category` change handler in the `$(function() {...})` init block**

In the `$(function() {...})` block (around line 405), add after `$('#m-LicenseType').on('input', syncCategoryVisibility);`:

```js
    $('#m-Category').on('change', function () {
        const val = $(this).val();
        const suggested = suggestNextLicenseType(val);
        if (suggested) {
            $('#m-LicenseType').val(suggested).prop('disabled', true);
        } else {
            $('#m-LicenseType').val('').prop('disabled', false);
        }
        syncCategoryVisibility();
    });
```

- [ ] **Step 6: Fix `submitLicense` to read disabled `#m-LicenseType`**

A `disabled` input's value is still accessible via `.val()` in jQuery — no change needed here. But verify the `lt` variable reads correctly:

```js
    const lt = $('#m-LicenseType').val().trim();
```

This reads correctly even when the field is disabled.

- [ ] **Step 7: Commit**

```bash
git add src/TCS.Web/wwwroot/js/license.js
git commit -m "feat(license): auto-suggest next LicenseType code on Category change"
```

---

## Task 4: T4 Backend — Add Plant to TrainingHeader

**Files:**
- Modify: `src/TCS.Core/Entities/TrainingHeader.cs`
- Modify: `src/TCS.Core/DTOs/Requests/CreateTrainingHeaderRequest.cs`
- Modify: `src/TCS.Core/DTOs/Requests/UpdateTrainingHeaderRequest.cs`
- Modify: `src/TCS.Core/DTOs/TrainingHeaderDto.cs`
- Modify: `src/TCS.Core/Mapping/MappingExtensions.cs`
- Modify: `src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs`
- Modify: `src/TCS.Core/Services/TrainingService.cs`

- [ ] **Step 1: Add `Plant` to `TrainingHeader` entity**

In `src/TCS.Core/Entities/TrainingHeader.cs`, add after the `Remark` property:

```csharp
    public string? Plant { get; set; }
```

Full entity file after change:
```csharp
using TCS.Core.Common;

namespace TCS.Core.Entities;

public class TrainingHeader : IAuditableEntity
{
    public string EmployeeId { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public int RequiredHours { get; set; }
    public string? Remark { get; set; }
    public string? Plant { get; set; }
    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    public LicenseMaster? LicenseMasterNav { get; set; }
    public ICollection<TrainingDetail> Details { get; set; } = new List<TrainingDetail>();
}
```

- [ ] **Step 2: Add `Plant` to `CreateTrainingHeaderRequest`**

Replace content of `src/TCS.Core/DTOs/Requests/CreateTrainingHeaderRequest.cs`:

```csharp
namespace TCS.Core.DTOs.Requests;

/// <summary>新增受訓單頭請求（RequiredHours 由 Service 層自 LicenseMaster.Hours 帶入，§8-1）</summary>
public record CreateTrainingHeaderRequest(
    string EmployeeId,
    string LicenseType,
    string? Remark,
    string? Plant);
```

- [ ] **Step 3: Add `Plant` to `UpdateTrainingHeaderRequest`**

Replace content of `src/TCS.Core/DTOs/Requests/UpdateTrainingHeaderRequest.cs`:

```csharp
namespace TCS.Core.DTOs.Requests;

/// <summary>修改受訓單頭請求（僅允許修改 Remark/Plant；EmployeeId+LicenseType 由 route 帶入）</summary>
public record UpdateTrainingHeaderRequest(
    string EmployeeId,
    string LicenseType,
    string? Remark,
    string? Plant);
```

- [ ] **Step 4: Add `Plant` to `TrainingHeaderDto`**

Replace content of `src/TCS.Core/DTOs/TrainingHeaderDto.cs`:

```csharp
namespace TCS.Core.DTOs;

/// <summary>
/// 受訓異動單頭回應 DTO（含衍生欄位，§4-6）
/// </summary>
public record TrainingHeaderDto(
    string EmployeeId,
    string? EmployeeName,
    string? Department,
    string? HireDate,
    string LicenseType,
    string? Description,
    int RequiredHours,
    string? Remark,
    string? Plant,
    DateOnly? LatestAcquireDate,
    DateOnly? LatestRetrainDate,
    DateOnly? NextReviewDate,
    decimal AccumulatedHours,
    decimal RemainingHours,
    OverallStatus OverallStatus);
```

- [ ] **Step 5: Update `MappingExtensions.ToDto()` for TrainingHeader to include Plant**

In `src/TCS.Core/Mapping/MappingExtensions.cs`, find the `return new TrainingHeaderDto(` call (lines 88-103) and add `header.Plant` after `header.Remark`:

```csharp
        return new TrainingHeaderDto(
            header.EmployeeId,
            employee?.Name,
            employee?.Department,
            employee?.HireDate,
            header.LicenseType,
            licenseMaster?.Description,
            header.RequiredHours,
            header.Remark,
            header.Plant,
            latestAcquireDate,
            latestRetrainDate,
            nextReviewDate,
            accumulatedHours,
            remainingHours,
            status);
```

- [ ] **Step 6: Configure `Plant` column in `TrainingHeaderConfiguration`**

In `src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs`, add after the `builder.Property(e => e.Remark)` line:

```csharp
        builder.Property(e => e.Plant).HasMaxLength(6).IsFixedLength(true).IsUnicode(false).IsRequired(false);
```

- [ ] **Step 7: Assign `Plant` in `TrainingService.CreateHeaderAsync` and `UpdateHeaderAsync`**

In `src/TCS.Core/Services/TrainingService.cs`:

In `CreateHeaderAsync` (around line 95), change the `TrainingHeader` initializer:
```csharp
        var header = new TrainingHeader
        {
            EmployeeId = req.EmployeeId,
            LicenseType = req.LicenseType,
            RequiredHours = license.Hours ?? 0,
            Remark = req.Remark,
            Plant = req.Plant
        };
```

In `UpdateHeaderAsync` (around line 112), change the property assignments:
```csharp
        header.Remark = req.Remark;
        header.Plant = req.Plant;
```

- [ ] **Step 8: Build to verify**

Run: `dotnet build src/TCS.Web/TCS.Web.csproj`
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 9: Commit**

```bash
git add src/TCS.Core/Entities/TrainingHeader.cs \
        src/TCS.Core/DTOs/Requests/CreateTrainingHeaderRequest.cs \
        src/TCS.Core/DTOs/Requests/UpdateTrainingHeaderRequest.cs \
        src/TCS.Core/DTOs/TrainingHeaderDto.cs \
        src/TCS.Core/Mapping/MappingExtensions.cs \
        src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs \
        src/TCS.Core/Services/TrainingService.cs
git commit -m "feat(training): add Plant field to TrainingHeader entity and DTOs"
```

---

## Task 5: T4 Migration — Add Plant column to TRNF01 table

**Files:**
- Create: EF migration (auto-generated)

- [ ] **Step 1: Run EF migration add**

```bash
dotnet ef migrations add AddPlantToTrainingHeader --project src/TCS.Infrastructure --startup-project src/TCS.Web
```

Expected output: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 2: Verify migration content**

Open the generated migration file under `src/TCS.Infrastructure/Migrations/`. Confirm it contains:
```csharp
migrationBuilder.AddColumn<string>(
    name: "Plant",
    table: "TRNF01",
    type: "char(6)",
    fixedLength: true,
    unicode: false,
    maxLength: 6,
    nullable: true);
```

- [ ] **Step 3: Apply migration**

```bash
dotnet ef database update --project src/TCS.Infrastructure --startup-project src/TCS.Web
```

Expected: `Done.`

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Infrastructure/Migrations/
git commit -m "feat(db): add Plant CHAR(6) NULL column to TRNF01 (TrainingHeader)"
```

---

## Task 6: T4 Frontend — Plant dropdown in Training modal

**Files:**
- Modify: `src/TCS.Web/Views/Training/Index.cshtml`
- Modify: `src/TCS.Web/wwwroot/js/training.js`

- [ ] **Step 1: Add `#m-Plant` select field to Training modal in Index.cshtml**

In `src/TCS.Web/Views/Training/Index.cshtml`, find the `<div id="m-Remark-group" class="mb-3">` block (line 154). Insert the plant field immediately before it:

```html
                    <div class="mb-3" id="m-Plant-group">
                        <label class="form-label">廠別</label>
                        <select id="m-Plant" class="form-select"></select>
                        <div class="form-text">依所選證照類別列出廠別需求；可不選</div>
                    </div>
```

- [ ] **Step 2: Add `LICENSE_PLANT_API` constant in training.js**

After the existing `const LICENSE_API = BASE + '/api/licenses';` line (line 5), add:

```js
const LICENSE_PLANT_API = (lt) => `${BASE}/api/licenses/${encodeURIComponent(lt)}/plants`;
```

- [ ] **Step 3: Add `loadPlantOptions()` function in training.js**

Insert this new function after `ensureAllLicensesLoaded()` (after line 194):

```js
async function loadPlantOptions(licenseType) {
    const $sel = $('#m-Plant').empty();
    $('<option></option>').val('').text('（不選廠別）').appendTo($sel);
    if (!licenseType) return;
    const res = await fetch(LICENSE_PLANT_API(licenseType));
    if (!res.ok) return;
    const items = await res.json();
    items.forEach(p => {
        const label = p.PlantName ? `${p.Plant} ${p.PlantName}` : p.Plant;
        $('<option></option>').val(p.Plant).text(label).appendTo($sel);
    });
}
```

- [ ] **Step 4: Update `openHeaderModal` to load plant options**

In `openHeaderModal`, in the `create` branch (around line 216), add:

```js
        await loadPlantOptions('');
```

And in the `else` (edit) branch (around line 222), add after setting `#m-Remark`:

```js
        await loadPlantOptions(item.LicenseType);
        $('#m-Plant').val(item.Plant ?? '');
```

Full updated `openHeaderModal` function:

```js
async function openHeaderModal(mode, item) {
    $('#header-modal-error').addClass('d-none').text('');
    $('#header-modal-title').text(mode === 'create' ? '新增受訓單頭' : '修改受訓單頭');

    await Promise.all([ensureEmployeesLoaded(), ensureAllLicensesLoaded()]);

    const $licSel = $('#m-LicenseType').empty();
    $('<option></option>').val('').text('-- 請選擇 --').appendTo($licSel);
    const cats = cachedAllLicenses.filter(x => x.IsCategory || INTEGER_REGEX.test(x.LicenseType));
    cats.forEach(cat => {
        const $grp = $('<optgroup>').attr('label', `${cat.LicenseType} ${cat.Description}`);
        $('<option></option>').val(cat.LicenseType).text(`${cat.LicenseType} ${cat.Description}`).appendTo($grp);
        cachedAllLicenses.filter(x => x.Category === cat.LicenseType).forEach(x => {
            $('<option></option>').val(x.LicenseType).text(`${x.LicenseType} ${x.Description}`).appendTo($grp);
        });
        $('<option></option>').val(cat.LicenseType).text(`其他（${cat.Description}）`).attr('data-is-other', 'true').appendTo($grp);
        $grp.appendTo($licSel);
    });

    if (mode === 'create') {
        $('#m-EmployeeId').val('').prop('readonly', false);
        $('#m-LicenseType').val('').prop('disabled', false);
        $('#m-RequiredHours').val('');
        $('#m-Remark').val('');
        await loadPlantOptions('');
        updateEmployeeHint();
    } else {
        $('#m-EmployeeId').val(item.EmployeeId).prop('readonly', true);
        $('#m-LicenseType').val(item.LicenseType).prop('disabled', true);
        $('#m-RequiredHours').val(item.RequiredHours ?? '');
        $('#m-Remark').val(item.Remark ?? '');
        await loadPlantOptions(item.LicenseType);
        $('#m-Plant').val(item.Plant ?? '');
        updateEmployeeHint();
    }
    $('#header-form').data('mode', mode);
    updateCustomNameVisibility();
    headerModal.show();
}
```

- [ ] **Step 5: Add `loadPlantOptions` call to `#m-LicenseType` change handler**

In the `$(function() {...})` init block, find the `$('#m-LicenseType').on('change', ...)` handler (line 512):

```js
    $('#m-LicenseType').on('change', () => {
        updateCustomNameVisibility();
        updateRequiredHoursOnLicenseChange();
    });
```

Change it to:

```js
    $('#m-LicenseType').on('change', async function () {
        updateCustomNameVisibility();
        updateRequiredHoursOnLicenseChange();
        await loadPlantOptions($(this).val());
    });
```

- [ ] **Step 6: Add `Plant` to `submitHeader` body**

In `submitHeader`, find the `const body = {...}` object (line 285):

```js
    const body = { EmployeeId: employeeId, LicenseType: licenseType, Remark: remark };
```

Change to:

```js
    const body = { EmployeeId: employeeId, LicenseType: licenseType, Remark: remark, Plant: $('#m-Plant').val() || null };
```

- [ ] **Step 7: Build to verify**

Run: `dotnet build src/TCS.Web/TCS.Web.csproj`
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 8: Commit**

```bash
git add src/TCS.Web/Views/Training/Index.cshtml \
        src/TCS.Web/wwwroot/js/training.js
git commit -m "feat(training): add Plant field to training header modal and submit"
```

---

## Final Acceptance Checklist

- [ ] `dotnet build` — 0 errors
- [ ] `dotnet ef database update` — succeeded, `TRNF01` has `Plant` column
- [ ] **T1+T2 manual acceptance:**
  - [ ] Open 新增證照 modal → `#m-Category` has "大類" option at top
  - [ ] Select "大類" → `#m-LicenseType` auto-fills next integer (e.g., `3` if 1 and 2 exist) and becomes disabled
  - [ ] Select an existing category (e.g., "2 安全類") → `#m-LicenseType` auto-fills `2.X` and becomes disabled
  - [ ] Select "-- 請選擇 --" → `#m-LicenseType` clears and becomes editable
  - [ ] Save a "大類" record → DB `Category = NULL`
- [ ] **T3 manual acceptance:**
  - [ ] Set advanced search Department filter → click 匯出 Excel → Excel rows match page count across all pages
  - [ ] Quick search then export → Excel matches screen
- [ ] **T4 manual acceptance:**
  - [ ] Create training header → select LicenseType → Plant dropdown loads the correct plants from LicensePlantRequirements
  - [ ] Select "（不選廠別）" → saves with `Plant = NULL` in DB
  - [ ] Select a specific plant → saves correctly in DB
  - [ ] Edit mode: Plant dropdown pre-selects existing value
