namespace TCS.Core.DTOs.Requests;

/// <summary>修改受訓單身請求（EmployeeId+LicenseType+TrainingDate 由 route 帶入）</summary>
public record UpdateTrainingDetailRequest(
    string EmployeeId,
    string LicenseType,
    DateOnly TrainingDate,
    int TrainingType,
    decimal? Hours);
