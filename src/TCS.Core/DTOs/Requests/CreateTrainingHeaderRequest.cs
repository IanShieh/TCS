namespace TCS.Core.DTOs.Requests;

/// <summary>新增受訓單頭請求（Hours/Years 由 Service 層自 LicenseMaster 帶入，§8-1）</summary>
public record CreateTrainingHeaderRequest(
    string EmployeeId,
    string LicenseType,
    string? Remark,
    string? Plant);
