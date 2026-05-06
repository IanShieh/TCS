namespace TCS.Core.DTOs.Requests;

/// <summary>新增證照廠別需求請求</summary>
public record CreateLicensePlantRequirementRequest(
    string LicenseType,
    string Plant,
    int RequiredCount);
