namespace TCS.Core.DTOs.Requests;

/// <summary>
/// 新增受訓單頭請求。
/// 一般證照:Hours/Years 由 Service 層自 LicenseMaster 帶入(忽略請求中的 Hours/Years)。
/// 其他證照(IsOther=true):LicenseType 帶 base 母類碼(99 或 X),Remark 為自定義名稱,
/// Hours/Years 由使用者手動填(可空),Service 會產生唯一代碼。
/// </summary>
public record CreateTrainingHeaderRequest(
    string EmployeeId,
    string LicenseType,
    string? Remark,
    string? Plant,
    bool IsOther = false,
    int? Hours = null,
    int? Years = null);
