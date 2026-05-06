namespace TCS.Core.DTOs;

/// <summary>證照規格主檔回應 DTO</summary>
/// <param name="IsCategory">LicenseType 為純整數=大類，含小數點=小類</param>
public record LicenseMasterDto(
    string LicenseType,
    string Description,
    string? Category,
    int? Hours,
    int? Years,
    bool IsCategory);
