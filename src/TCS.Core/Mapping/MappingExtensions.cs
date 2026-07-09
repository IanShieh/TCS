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

    /// <summary>廠別視角總覽投影；需求須已 Include LicenseMasterNav</summary>
    public static PlantRequirementOverviewDto ToOverviewDto(this LicensePlantRequirement r) =>
        new(r.LicenseType, r.LicenseMasterNav?.Description, r.RequiredCount);

    // ── TrainingDetail ─────────────────────────────────────────────────────

    public static TrainingDetailDto ToDto(this TrainingDetail d) =>
        new(d.EmployeeId, d.LicenseType,
            DateOnly.FromDateTime(d.TrainingDate),
            d.TrainingType, d.Hours);

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

        // 有效時數門檻：Hours 為 null 或 ≤ 0 時視為「無門檻」
        int? hoursThreshold = header.Hours is int h && h > 0 ? h : null;

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
                if (hoursThreshold is int reqHours && acc >= reqHours)
                {
                    latestAnchor = DateOnly.FromDateTime(s.TrainingDate);
                    acc -= reqHours;        // 超額滾入下一週期（§6 規則2-A）
                }
            }
        }

        // 下次回訓 = latestAnchor + Years（Years 為 null → null）
        DateOnly? nextReviewDate = latestAnchor.HasValue && header.Years.HasValue
            ? latestAnchor.Value.AddYears(header.Years.Value)
            : null;

        decimal accumulatedHours = acc;
        decimal remainingHours = hoursThreshold is int req ? Math.Max(0m, req - acc) : 0m;

        OverallStatus status;
        if (nextReviewDate.HasValue && nextReviewDate.Value < today)
            status = OverallStatus.已過期;
        else if (header.Hours.HasValue && remainingHours == 0)
            status = OverallStatus.回訓完成;
        else if (nextReviewDate.HasValue && nextReviewDate.Value <= today.AddYears(1))
            status = OverallStatus.待回訓;
        else
            status = OverallStatus.無;

        return new TrainingHeaderDto(
            header.EmployeeId,
            employee?.Name,
            employee?.Department?.Trim(),
            employee?.HireDate,
            header.LicenseType,
            licenseMaster?.Description,
            header.Hours,
            header.Years,
            header.Remark,
            header.Plant,
            latestAcquireDate,
            latestRetrainDate,
            nextReviewDate,
            accumulatedHours,
            remainingHours,
            status);
    }

    // ── Employee / Plant ───────────────────────────────────────────────────

    public static EmployeeDto ToDto(this Employee e) =>
        new(e.EmployeeId, e.Name, e.Department?.Trim(), e.HireDate);

    public static PlantDto ToDto(this Plant p) =>
        new(p.PlantCode, p.PlantName);

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>LicenseType 純整數 → 大類；含小數點 → 小類</summary>
    public static bool IsLicenseTypeCategory(string licenseType) =>
        licenseType.All(c => char.IsDigit(c));
}
