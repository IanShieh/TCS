using FluentAssertions;
using TCS.Core.DTOs;
using TCS.Core.Entities;
using TCS.Core.Mapping;

namespace TCS.Tests.Mapping;

public class MappingExtensionsTests
{
    private static TrainingHeader MakeHeader(int hours = 8, int? years = null) =>
        new() { EmployeeId = "E001", LicenseType = "1.1", Hours = hours, Years = years };

    private static LicenseMaster MakeLicense(int hours = 8, int years = 2) =>
        new() { LicenseType = "1.1", Description = "Test License", Hours = hours, Years = years };

    private static TrainingDetail D(DateTime date, int type, decimal hours) =>
        new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date, TrainingType = type, Hours = hours };

    // ── OverallStatus ──────────────────────────────────────────────────────

    [Fact]
    public void ToDto_NoDetails_StatusIsNone()
    {
        var dto = MakeHeader().ToDto(null, MakeLicense(), [], new DateOnly(2025, 6, 1));
        dto.OverallStatus.Should().Be(OverallStatus.無);
        dto.LatestAcquireDate.Should().BeNull();
        dto.LatestRetrainDate.Should().BeNull();
        dto.NextReviewDate.Should().BeNull();
    }

    [Fact]
    public void ToDto_OnlyRetrainWithoutAcquire_StatusIsNone()
    {
        var details = new[] { D(new DateTime(2024, 1, 1), 2, 8m) };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(), details, new DateOnly(2025, 1, 1));
        dto.OverallStatus.Should().Be(OverallStatus.無);
        dto.LatestAcquireDate.Should().BeNull();
    }

    // ── Computed Dates ─────────────────────────────────────────────────────

    [Fact]
    public void ToDto_NextReviewDate_IsLatestAcquireDatePlusYears()
    {
        // header.Years=2 drives the calculation
        var details = new[] { D(new DateTime(2024, 3, 15), 1, 4m) };
        var dto = MakeHeader(8, 2).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.LatestAcquireDate.Should().Be(new DateOnly(2024, 3, 15));
        dto.NextReviewDate.Should().Be(new DateOnly(2026, 3, 15));
    }

    [Fact]
    public void ToDto_LatestRetrainDate_PicksMostRecent()
    {
        var details = new[]
        {
            D(new DateTime(2024, 1, 1), 1, 4m),
            D(new DateTime(2024, 3, 1), 2, 2m),
            D(new DateTime(2024, 6, 1), 2, 2m)   // most recent retrain
        };
        var dto = MakeHeader(8, 2).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 9, 1));
        dto.LatestRetrainDate.Should().Be(new DateOnly(2024, 6, 1));
    }

    [Fact]
    public void ToDto_NoRetrainRecord_LatestRetrainDateIsNull()
    {
        var details = new[] { D(new DateTime(2024, 1, 1), 1, 4m) };
        var dto = MakeHeader(8, 2).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.LatestRetrainDate.Should().BeNull();
    }

    [Fact]
    public void ToDto_NoYearsOnHeader_NextReviewDateIsNull()
    {
        // header.Years is null → nextReviewDate should be null regardless of license
        var details = new[] { D(new DateTime(2024, 1, 1), 1, 4m) };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.NextReviewDate.Should().BeNull();
    }

    // ── Accumulated & Remaining Hours ──────────────────────────────────────

    [Fact]
    public void ToDto_NoDetails_AccumulatedHoursIsZero()
    {
        var dto = MakeHeader(16, 3).ToDto(null, MakeLicense(16, 3), [], new DateOnly(2025, 1, 1));
        dto.AccumulatedHours.Should().Be(0m);
        dto.RemainingHours.Should().Be(16m);
    }

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

    // ── Employee / License Info ────────────────────────────────────────────

    [Fact]
    public void ToDto_WithEmployee_MapsEmployeeFields()
    {
        var emp = new Employee { EmployeeId = "E001", Name = "張三", Department = "生產部", HireDate = "2020-01-01" };
        var dto = MakeHeader().ToDto(emp, MakeLicense(), [], new DateOnly(2025, 1, 1));
        dto.EmployeeName.Should().Be("張三");
        dto.Department.Should().Be("生產部");
    }

    [Fact]
    public void ToDto_NullEmployee_EmployeeFieldsAreNull()
    {
        var dto = MakeHeader().ToDto(null, MakeLicense(), [], new DateOnly(2025, 1, 1));
        dto.EmployeeName.Should().BeNull();
        dto.Department.Should().BeNull();
    }

    [Fact]
    public void ToDto_NullLicense_DescriptionIsNull()
    {
        var dto = MakeHeader().ToDto(null, null, [], new DateOnly(2025, 1, 1));
        dto.Description.Should().BeNull();
    }

    // ── LicenseMaster.ToDto ────────────────────────────────────────────────

    [Fact]
    public void LicenseMasterToDto_LargeCategory_IsCategoryTrue()
    {
        var license = new LicenseMaster { LicenseType = "1", Description = "電氣類" };
        var dto = license.ToDto();
        dto.IsCategory.Should().BeTrue();
        dto.LicenseType.Should().Be("1");
    }

    [Fact]
    public void LicenseMasterToDto_SmallCategory_IsCategoryFalse()
    {
        var license = new LicenseMaster { LicenseType = "1.1", Description = "低壓電氣作業", Category = "1", Hours = 8, Years = 2 };
        var dto = license.ToDto();
        dto.IsCategory.Should().BeFalse();
        dto.Hours.Should().Be(8);
        dto.Years.Should().Be(2);
        dto.Category.Should().Be("1");
    }
}
