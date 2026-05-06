namespace TCS.Core.DTOs;

/// <summary>受訓異動單身回應 DTO</summary>
public record TrainingDetailDto(
    string EmployeeId,
    string LicenseType,
    DateOnly TrainingDate,
    int TrainingType,
    decimal Hours,
    bool IsExpired);
