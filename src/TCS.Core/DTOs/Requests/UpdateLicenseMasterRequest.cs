namespace TCS.Core.DTOs.Requests;

/// <summary>修改證照主檔請求（LicenseType 由 route 帶入，存入 DTO 以供驗證器判斷大類/小類）</summary>
public record UpdateLicenseMasterRequest(
    string LicenseType,
    string Description,
    string? Category,
    int? Hours,
    int? Years);
