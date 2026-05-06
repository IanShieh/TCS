namespace DingxinErp.Core.Common;

/// <summary>
/// 鼎新 ERP 審計欄位介面 (CREATOR/CREATE_DATE/MODIFIER/MODI_DATE/FLAG)
/// </summary>
public interface IAuditableEntity
{
    string? Creator { get; set; }
    string? CreateDate { get; set; }
    string? Modifier { get; set; }
    string? ModiDate { get; set; }
    decimal? Flag { get; set; }
}
