using TCS.Core.Common;
using TCS.Core.Entities;
using TCS.Core.Interfaces;

namespace TCS.Core.Services;

/// <inheritdoc cref="IExpiryCalculator"/>
public sealed class ExpiryCalculator : IExpiryCalculator
{
    /// <summary>
    /// 週期演算法（§8-4）：
    /// 每筆「取得證照」開啟一個週期 [acquireDate, MIN(acquireDate+years, nextAcquireDate))。
    /// 週期結束（end &lt; today）且累計時數 &lt; requiredHours → 週期內所有 detail IsExpired = true。
    /// 孤兒紀錄（早於首筆取得證照的回訓）保持 IsExpired = false。
    /// </summary>
    public IReadOnlyList<ExpiryResult> Calculate(
        IReadOnlyList<TrainingDetail> details,
        int years,
        int requiredHours,
        DateOnly today)
    {
        // 預設所有 detail 為 false
        var expiredMap = details.ToDictionary(
            d => (d.EmployeeId, d.LicenseType, d.TrainingDate),
            _ => false);

        var acquireRecords = details
            .Where(d => d.TrainingType == (int)TrainingType.取得證照)
            .OrderBy(d => d.TrainingDate)
            .ToList();

        // 無取得證照紀錄 → 所有 detail 均為孤兒，保持 false
        if (acquireRecords.Count == 0)
            return BuildResults(details, expiredMap);

        for (int i = 0; i < acquireRecords.Count; i++)
        {
            var periodStart = DateOnly.FromDateTime(acquireRecords[i].TrainingDate);

            // 下一個取得証照的日期（用於截斷當前週期）
            var nextAcquireStart = i + 1 < acquireRecords.Count
                ? DateOnly.FromDateTime(acquireRecords[i + 1].TrainingDate)
                : DateOnly.MaxValue;

            // 週期的實際結束日 = MIN(periodStart + years, nextAcquireStart)
            var periodEnd = periodStart.AddYears(years);
            if (nextAcquireStart < periodEnd)
                periodEnd = nextAcquireStart;

            // 屬於此週期的 detail：[periodStart, nextAcquireStart)
            var periodDetails = details
                .Where(d =>
                {
                    var date = DateOnly.FromDateTime(d.TrainingDate);
                    return date >= periodStart && date < nextAcquireStart;
                })
                .ToList();

            // 週期已結束且累計未達標 → 標記過期
            bool periodEnded = periodEnd < today;
            decimal sumHours = periodDetails.Sum(d => d.Hours);
            bool shouldExpire = periodEnded && sumHours < requiredHours;

            foreach (var d in periodDetails)
                expiredMap[(d.EmployeeId, d.LicenseType, d.TrainingDate)] = shouldExpire;
        }

        return BuildResults(details, expiredMap);
    }

    private static IReadOnlyList<ExpiryResult> BuildResults(
        IReadOnlyList<TrainingDetail> details,
        Dictionary<(string, string, DateTime), bool> expiredMap) =>
        details.Select(d => new ExpiryResult(
            d.EmployeeId, d.LicenseType, d.TrainingDate,
            expiredMap.TryGetValue((d.EmployeeId, d.LicenseType, d.TrainingDate), out var v) && v))
        .ToList();
}
