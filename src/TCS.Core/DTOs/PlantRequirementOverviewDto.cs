namespace TCS.Core.DTOs;

/// <summary>廠別需求總覽回應 DTO（廠別視角反查證照需求，todo-2026-07-09 T3）</summary>
public record PlantRequirementOverviewDto(
    string LicenseType,
    string? Description,
    int RequiredCount);
