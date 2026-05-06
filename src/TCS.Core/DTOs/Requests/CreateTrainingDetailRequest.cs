namespace TCS.Core.DTOs.Requests;

/// <summary>新增受訓單身請求</summary>
public record CreateTrainingDetailRequest(
    string EmployeeId,
    string LicenseType,
    DateOnly TrainingDate,
    int TrainingType,
    decimal Hours);
