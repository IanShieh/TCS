using TCS.Core.Common;
using TCS.Core.Entities;
using TCS.Core.Services;
using Xunit;
using FluentAssertions;

namespace TCS.Tests.Services;

public class ExpiryCalculatorTests
{
    private static readonly ExpiryCalculator Sut = new();
    private const string EmpId = "E001";
    private const string LicType = "1.1";

    private static TrainingDetail MakeDetail(DateTime date, int type, decimal hours, bool isExpired = false) =>
        new() { EmployeeId = EmpId, LicenseType = LicType, TrainingDate = date, TrainingType = type, Hours = hours, IsExpired = isExpired };

    // ── 基本案例 ──────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_EmptyDetails_ReturnsEmpty()
    {
        var result = Sut.Calculate([], years: 2, requiredHours: 8, today: new DateOnly(2025, 1, 1));
        result.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_NoAcquireRecord_AllFalse()
    {
        // 孤兒回訓紀錄（無取得証照）→ 全部 false
        var details = new[] { MakeDetail(new DateTime(2024, 1, 1), (int)TrainingType.回訓, 4) };
        var result = Sut.Calculate(details, 2, 8, new DateOnly(2025, 6, 1));
        result.Should().AllSatisfy(r => r.IsExpired.Should().BeFalse());
    }

    // ── 單一週期 ──────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_SinglePeriod_ActiveNotExpired()
    {
        // 週期 [2024-01-01, 2026-01-01)，today = 2025-06-01（週期進行中）
        var details = new[]
        {
            MakeDetail(new DateTime(2024, 1, 1), (int)TrainingType.取得證照, 4),
            MakeDetail(new DateTime(2024, 6, 1), (int)TrainingType.回訓, 4)
        };
        var result = Sut.Calculate(details, years: 2, requiredHours: 8, today: new DateOnly(2025, 6, 1));
        result.Should().AllSatisfy(r => r.IsExpired.Should().BeFalse());
    }

    [Fact]
    public void Calculate_SinglePeriod_EndedAndNotPassed_AllExpired()
    {
        // 週期 [2022-01-01, 2024-01-01)，today = 2025-01-01（已結束），累計 4+2=6 < 8 → 全部過期
        var details = new[]
        {
            MakeDetail(new DateTime(2022, 1, 1), (int)TrainingType.取得證照, 4),
            MakeDetail(new DateTime(2022, 6, 1), (int)TrainingType.回訓, 2)
        };
        var result = Sut.Calculate(details, years: 2, requiredHours: 8, today: new DateOnly(2025, 1, 1));
        result.Should().AllSatisfy(r => r.IsExpired.Should().BeTrue());
    }

    [Fact]
    public void Calculate_SinglePeriod_EndedAndPassed_AllFalse()
    {
        // 週期結束但累計 8 >= 8 → 達標，不標記過期
        var details = new[]
        {
            MakeDetail(new DateTime(2022, 1, 1), (int)TrainingType.取得證照, 4),
            MakeDetail(new DateTime(2022, 6, 1), (int)TrainingType.回訓, 4)
        };
        var result = Sut.Calculate(details, years: 2, requiredHours: 8, today: new DateOnly(2025, 1, 1));
        result.Should().AllSatisfy(r => r.IsExpired.Should().BeFalse());
    }

    // ── 多週期 ────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_TwoPeriods_FirstExpiredSecondActive()
    {
        // 週期一 [2021-01-01, 2023-01-01)，累計 4 < 8 → 過期
        // 週期二 [2023-01-01, 2025-01-01)，today = 2024-06-01（進行中）→ false
        var d1Acquire = new DateTime(2021, 1, 1);
        var d1Retrain = new DateTime(2021, 6, 1);
        var d2Acquire = new DateTime(2023, 1, 1);
        var d2Retrain = new DateTime(2023, 6, 1);

        // 週期一累計 4+2=6 < 8；週期二仍進行中
        var details = new[]
        {
            MakeDetail(d1Acquire, (int)TrainingType.取得證照, 4),
            MakeDetail(d1Retrain, (int)TrainingType.回訓, 2),
            MakeDetail(d2Acquire, (int)TrainingType.取得證照, 4),
            MakeDetail(d2Retrain, (int)TrainingType.回訓, 4)
        };

        var result = Sut.Calculate(details, years: 2, requiredHours: 8, today: new DateOnly(2024, 6, 1))
            .ToDictionary(r => r.TrainingDate);

        result[d1Acquire].IsExpired.Should().BeTrue("週期一累計不足");
        result[d1Retrain].IsExpired.Should().BeTrue("週期一累計不足");
        result[d2Acquire].IsExpired.Should().BeFalse("週期二仍進行中");
        result[d2Retrain].IsExpired.Should().BeFalse("週期二仍進行中");
    }

    [Fact]
    public void Calculate_MultiplePeriods_TruncatesOlderPeriod()
    {
        // 週期一 [2022-01-01, MIN(2024-01-01, 2022-06-01)) = [2022-01-01, 2022-06-01)（被截斷）
        // 累計 = 4 (取得證照那筆) → 4 < 8，但截斷的週期 end = 2022-06-01 < today=2025-01-01 → 過期
        var d1 = new DateTime(2022, 1, 1);
        var d2 = new DateTime(2022, 6, 1); // 第二筆取得證照
        var d3 = new DateTime(2022, 8, 1);

        var details = new[]
        {
            MakeDetail(d1, (int)TrainingType.取得證照, 4),
            MakeDetail(d2, (int)TrainingType.取得證照, 8), // 新週期，本身達標
            MakeDetail(d3, (int)TrainingType.回訓, 4)
        };

        var result = Sut.Calculate(details, years: 2, requiredHours: 8, today: new DateOnly(2025, 1, 1))
            .ToDictionary(r => r.TrainingDate);

        result[d1].IsExpired.Should().BeTrue("週期一（被截斷）累計 4 < 8");
        result[d2].IsExpired.Should().BeFalse("週期二取得時數已達標");
        result[d3].IsExpired.Should().BeFalse("週期二未結束或達標");
    }
}
