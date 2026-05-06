namespace TCS.Core.Common;

/// <summary>審計欄位介面，沿襲鼎新 ERP 慣例</summary>
public interface IAuditableEntity
{
    string? Creator { get; set; }
    string? CreateDate { get; set; }
    string? Modifier { get; set; }
    string? ModiDate { get; set; }
    decimal? Flag { get; set; }
}
