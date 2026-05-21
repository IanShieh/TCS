using FluentAssertions;
using TCS.Core.DTOs;
using TCS.Core.Entities;
using TCS.Core.Mapping;

namespace TCS.Tests.Mapping;

public class MappingExtensionsTests
{
    private static TrainingHeader MakeHeader(int requiredHours = 8) =>
        new() { EmployeeId = "E001", LicenseType = "1.1", RequiredHours = requiredHours };

    private static LicenseMaster MakeLicense(int hours = 8, int years = 2) =>
        new() { LicenseType = "1.1", Description = "Test License", Hours = hours, Years = years };

    private static TrainingDetail D(DateTime date, int type, decimal hours) =>
        new() { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = date, TrainingType = type, Hours = hours };

    // ── OverallStatus ──────────────────────────────────────────────────────

    [Fact]
    public void ToDto_NoDetails_StatusIsNotAcquired()
    {
        var dto = MakeHeader().ToDto(null, MakeLicense(), [], new DateOnly(2025, 6, 1));
        dto.OverallStatus.Should().Be(OverallStatus.未取得);
        dto.LatestAcquireDate.Should().BeNull();
        dto.LatestRetrainDate.Should().BeNull();
        dto.NextReviewDate.Should().BeNull();
    }

    [Fact]
    public void ToDto_OnlyRetrainWithoutAcquire_StatusIsNotAcquired()
    {
        var details = new[] { D(new DateTime(2024, 1, 1), 2, 8m) };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(), details, new DateOnly(2025, 1, 1));
        dto.OverallStatus.Should().Be(OverallStatus.未取得);
        dto.LatestAcquireDate.Should().BeNull();
    }

    [Fact]
    public void ToDto_AcquiredWithInsufficientHours_StatusIsInProgress()
    {
        var details = new[] { D(new DateTime(2024, 1, 1), 1, 4m) };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.OverallStatus.Should().Be(OverallStatus.進行中);
        dto.AccumulatedHours.Should().Be(4m);
        dto.RemainingHours.Should().Be(4m);
    }

    [Fact]
    public void ToDto_AcquiredWithEnoughHours_StatusIsPassed()
    {
        var details = new[]
        {
            D(new DateTime(2024, 1, 1), 1, 4m),
            D(new DateTime(2024, 6, 1), 2, 4m)
        };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 9, 1));
        dto.OverallStatus.Should().Be(OverallStatus.通過);
        dto.AccumulatedHours.Should().Be(8m);
        dto.RemainingHours.Should().Be(0m);
    }

    // ── Computed Dates ─────────────────────────────────────────────────────

    [Fact]
    public void ToDto_NextReviewDate_IsLatestAcquireDatePlusYears()
    {
        var details = new[] { D(new DateTime(2024, 3, 15), 1, 4m) };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.LatestAcquireDate.Should().Be(new DateOnly(2024, 3, 15));
        dto.NextReviewDate.Should().Be(new DateOnly(2026, 3, 15));
    }

    [Fact]
    public void ToDto_LatestAcquireDate_PicksMostRecent()
    {
        var details = new[]
        {
            D(new DateTime(2022, 1, 1), 1, 4m),   // older acquire
            D(new DateTime(2024, 1, 1), 1, 4m)    // newer acquire
        };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.LatestAcquireDate.Should().Be(new DateOnly(2024, 1, 1));
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
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 9, 1));
        dto.LatestRetrainDate.Should().Be(new DateOnly(2024, 6, 1));
    }

    [Fact]
    public void ToDto_NoRetrainRecord_LatestRetrainDateIsNull()
    {
        var details = new[] { D(new DateTime(2024, 1, 1), 1, 4m) };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.LatestRetrainDate.Should().BeNull();
    }

    [Fact]
    public void ToDto_NullLicense_NextReviewDateIsNull()
    {
        var details = new[] { D(new DateTime(2024, 1, 1), 1, 4m) };
        var dto = MakeHeader(8).ToDto(null, null, details, new DateOnly(2024, 6, 1));
        dto.NextReviewDate.Should().BeNull();
    }

    // ── Accumulated & Remaining Hours ──────────────────────────────────────

    [Fact]
    public void ToDto_AccumulatedHoursOnlyCountsFromLatestAcquire()
    {
        // Two acquire periods: hours from first period must NOT be counted in current accumulation
        var details = new[]
        {
            D(new DateTime(2022, 1, 1), 1, 4m),  // first acquire
            D(new DateTime(2022, 6, 1), 2, 4m),  // first period retrain
            D(new DateTime(2024, 1, 1), 1, 6m),  // second (latest) acquire
            D(new DateTime(2024, 6, 1), 2, 2m)   // second period retrain
        };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2025, 1, 1));
        dto.LatestAcquireDate.Should().Be(new DateOnly(2024, 1, 1));
        dto.AccumulatedHours.Should().Be(8m);  // 6 + 2 from latest period only
    }

    [Fact]
    public void ToDto_RemainingHoursIsZeroWhenAccumulatedExceedsRequired()
    {
        var details = new[] { D(new DateTime(2024, 1, 1), 1, 10m) };
        var dto = MakeHeader(8).ToDto(null, MakeLicense(8, 2), details, new DateOnly(2024, 6, 1));
        dto.AccumulatedHours.Should().Be(10m);
        dto.RemainingHours.Should().Be(0m);  // MAX(0, 8 - 10) = 0
    }

    [Fact]
    public void ToDto_NoDetails_AccumulatedHoursIsZero()
    {
        var dto = MakeHeader(16).ToDto(null, MakeLicense(16, 3), [], new DateOnly(2025, 1, 1));
        dto.AccumulatedHours.Should().Be(0m);
        dto.RemainingHours.Should().Be(16m);
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
