namespace TCS.Core.DTOs.Requests;

/// <summary>修改證照廠別需求請求（LicenseType + Plant 由 route 帶入）</summary>
public record UpdateLicensePlantRequirementRequest(
    string LicenseType,
    string Plant,
    int RequiredCount);
