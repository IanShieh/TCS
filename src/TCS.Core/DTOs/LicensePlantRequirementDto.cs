namespace TCS.Core.DTOs;

/// <summary>證照廠別需求回應 DTO</summary>
public record LicensePlantRequirementDto(
    string LicenseType,
    string Plant,
    int RequiredCount,
    string? PlantName);
