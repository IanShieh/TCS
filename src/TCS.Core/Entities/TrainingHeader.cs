using TCS.Core.Common;

namespace TCS.Core.Entities;

/// <summary>
/// TrainingHeader PK: (EmployeeId, LicenseType)
/// 沒有 DB FK 到 Employee (Employee 是 view)；Employee 存在性由 Service 層驗證
/// </summary>
public class TrainingHeader : IAuditableEntity
{
    public string EmployeeId { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public int RequiredHours { get; set; }          // §8-1: 系統帶入，建立後唯讀
    public string? Remark { get; set; }
    public string? Plant { get; set; }
    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    public LicenseMaster? LicenseMasterNav { get; set; }
    public ICollection<TrainingDetail> Details { get; set; } = new List<TrainingDetail>();
}
