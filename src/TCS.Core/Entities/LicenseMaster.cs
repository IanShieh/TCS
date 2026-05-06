using TCS.Core.Common;

namespace TCS.Core.Entities;

public class LicenseMaster : IAuditableEntity
{
    public string LicenseType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Category { get; set; }       // 大類代碼；null = 本身是大類
    public int? Hours { get; set; }
    public int? Years { get; set; }
    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    public ICollection<LicensePlantRequirement> PlantRequirements { get; set; } = new List<LicensePlantRequirement>();
    public ICollection<TrainingHeader> TrainingHeaders { get; set; } = new List<TrainingHeader>();
}
