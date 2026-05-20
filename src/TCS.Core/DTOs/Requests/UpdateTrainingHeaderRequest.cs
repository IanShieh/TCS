namespace TCS.Core.DTOs.Requests;

/// <summary>修改受訓單頭請求（僅允許修改 Remark；EmployeeId+LicenseType 由 route 帶入）</summary>
public record UpdateTrainingHeaderRequest(
    string EmployeeId,
    string LicenseType,
    string? Remark,
    string? Plant);
