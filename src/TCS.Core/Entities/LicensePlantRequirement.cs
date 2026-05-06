using TCS.Core.Common;

namespace TCS.Core.Entities;

public class LicensePlantRequirement : IAuditableEntity
{
    public string LicenseType { get; set; } = null!;
    public string Plant { get; set; } = null!;
    public int RequiredCount { get; set; }
    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    public LicenseMaster LicenseMasterNav { get; set; } = null!;
}
