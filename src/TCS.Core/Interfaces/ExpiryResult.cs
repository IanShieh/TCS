namespace TCS.Core.Interfaces;

/// <summary>過期計算結果</summary>
public record ExpiryResult(
    string EmployeeId,
    string LicenseType,
    DateTime TrainingDate,
    bool IsExpired);
