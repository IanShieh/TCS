namespace TCS.Core.Entities;

/// <summary>
/// 廠別 view (CMSMB) — 唯讀，無 EF migration DDL
/// Open item §15 #6: MB002 欄位長度未知
/// </summary>
public class Plant
{
    public string PlantCode { get; set; } = null!;
    public string? PlantName { get; set; }
}
