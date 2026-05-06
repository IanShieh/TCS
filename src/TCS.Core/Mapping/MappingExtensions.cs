using TCS.Core.Common;
using TCS.Core.DTOs;
using TCS.Core.Entities;

namespace TCS.Core.Mapping;

/// <summary>Entity → DTO 投影擴充方法（t11）</summary>
public static class MappingExtensions
{
    // ── LicenseMaster ──────────────────────────────────────────────────────

    public static LicenseMasterDto ToDto(this LicenseMaster m) =>
        new(m.LicenseType, m.Description, m.Category, m.Hours, m.Years,
            IsLicenseTypeCategory(m.LicenseType));

    // ── LicensePlantRequirement ────────────────────────────────────────────

    public static LicensePlantRequirementDto ToDto(
        this LicensePlantRequirement r, string? plantName = null) =>
        new(r.LicenseType, r.Plant, r.RequiredCount, plantName);

    // ── TrainingDetail ─────────────────────────────────────────────────────

    public static TrainingDetailDto ToDto(this TrainingDetail d) =>
        new(d.EmployeeId, d.LicenseType,
            DateOnly.FromDateTime(d.TrainingDate),
            d.TrainingType, d.Hours, d.IsExpired);

    // ── TrainingHeader ─────────────────────────────────────────────────────

    /// <summary>
    /// 計算 TrainingHeaderDto 含所有衍生欄位（§4-6）。
    /// </summary>
    /// <param name="header">單頭</param>
    /// <param name="employee">員工 view（可 null，欄位顯示空白）</param>
    /// <param name="licenseMaster">證照主檔（可 null，Description 顯示空白）</param>
    /// <param name="details">該單頭所有單身紀錄</param>
    /// <param name="today">今日日期（由 IClock 提供，利於測試）</param>
    public static TrainingHeaderDto ToDto(
        this TrainingHeader header,
        Employee? employee,
        LicenseMaster? licenseMaster,
        IReadOnlyList<TrainingDetail> details,
        DateOnly today)
    {
        // 最後一筆取得證照
        var lastAcquire = details
            .Where(d => d.TrainingType == (int)TrainingType.取得證照)
            .OrderByDescending(d => d.TrainingDate)
            .FirstOrDefault();

        DateOnly? latestAcquireDate = lastAcquire is not null
            ? DateOnly.FromDateTime(lastAcquire.TrainingDate) : null;

        // 最後一筆回訓
        var lastRetrain = details
            .Where(d => d.TrainingType == (int)TrainingType.回訓)
            .OrderByDescending(d => d.TrainingDate)
            .FirstOrDefault();

        DateOnly? latestRetrainDate = lastRetrain is not null
            ? DateOnly.FromDateTime(lastRetrain.TrainingDate) : null;

        // 下次回訓時間 = LatestAcquireDate + Years 年
        DateOnly? nextReviewDate = latestAcquireDate.HasValue && licenseMaster?.Years.HasValue == true
            ? latestAcquireDate.Value.AddYears(licenseMaster.Years!.Value)
            : null;

        // 當前週期 = 從 LatestAcquireDate 起的所有單身
        var currentPeriodDetails = latestAcquireDate.HasValue
            ? details.Where(d => DateOnly.FromDateTime(d.TrainingDate) >= latestAcquireDate.Value)
                     .ToList()
            : [];

        decimal accumulatedHours = currentPeriodDetails.Sum(d => d.Hours);
        decimal remainingHours = Math.Max(0m, header.RequiredHours - accumulatedHours);

        OverallStatus status;
        if (!latestAcquireDate.HasValue)
            status = OverallStatus.未取得;
        else if (currentPeriodDetails.Any(d => d.IsExpired))
            status = OverallStatus.已過期;
        else if (accumulatedHours >= header.RequiredHours)
            status = OverallStatus.通過;
        else
            status = OverallStatus.進行中;

        return new TrainingHeaderDto(
            header.EmployeeId,
            employee?.Name,
            employee?.Department,
            employee?.HireDate,
            header.LicenseType,
            licenseMaster?.Description,
            header.RequiredHours,
            header.Remark,
            latestAcquireDate,
            latestRetrainDate,
            nextReviewDate,
            accumulatedHours,
            remainingHours,
            status);
    }

    // ── Employee / Plant ───────────────────────────────────────────────────

    public static EmployeeDto ToDto(this Employee e) =>
        new(e.EmployeeId, e.Name, e.Department, e.HireDate, e.Plant);

    public static PlantDto ToDto(this Plant p) =>
        new(p.PlantCode, p.PlantName);

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>LicenseType 純整數 → 大類；含小數點 → 小類</summary>
    public static bool IsLicenseTypeCategory(string licenseType) =>
        licenseType.All(c => char.IsDigit(c));
}
