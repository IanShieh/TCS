namespace TCS.Core.DTOs.Requests;

/// <summary>新增證照主檔請求</summary>
public record CreateLicenseMasterRequest(
    string LicenseType,
    string Description,
    string? Category,
    int? Hours,
    int? Years);
