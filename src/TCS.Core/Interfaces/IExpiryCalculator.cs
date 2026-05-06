using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

/// <summary>
/// 計算每筆 TrainingDetail 是否應標記為 IsExpired（§8-4）。
/// 純商業邏輯，不依賴 DB；由 ExpiryScanService 呼叫後批次更新 DB。
/// </summary>
public interface IExpiryCalculator
{
    /// <summary>
    /// 依週期演算法計算所有 detail 的 IsExpired 狀態。
    /// </summary>
    /// <param name="details">同一 (EmployeeId, LicenseType) 的所有 TrainingDetail</param>
    /// <param name="years">LicenseMaster.Years（週期年數）</param>
    /// <param name="requiredHours">LicenseMaster.Hours（應達時數）</param>
    /// <param name="today">今日日期（由 IClock 提供）</param>
    IReadOnlyList<ExpiryResult> Calculate(
        IReadOnlyList<TrainingDetail> details,
        int years,
        int requiredHours,
        DateOnly today);
}
