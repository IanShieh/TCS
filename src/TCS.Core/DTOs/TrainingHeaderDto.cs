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
    DateOnly? LatestAcquireDate,
    DateOnly? LatestRetrainDate,
    DateOnly? NextReviewDate,
    decimal AccumulatedHours,
    decimal RemainingHours,
    OverallStatus OverallStatus);
