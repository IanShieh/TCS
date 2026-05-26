using TCS.Core.Common;

namespace TCS.Core.Entities;

/// <summary>
/// TrainingDetail PK: (EmployeeId, LicenseType, TrainingDate)
/// </summary>
public class TrainingDetail : IAuditableEntity
{
    public string EmployeeId { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public DateTime TrainingDate { get; set; }
    public int TrainingType { get; set; }   // 1=取得證照 2=回訓
    public decimal? Hours { get; set; }
    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }
    public string? Company { get; set; }
    public string? UsrGroup { get; set; }
}
