namespace TCS.Core.Entities;

/// <summary>
/// 員工 view (CMSMV) — 唯讀，無 EF migration DDL
/// Open item §15 #4: 員工 view 名稱 CMSMV 待確認
/// </summary>
public class Employee
{
    public string EmployeeId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Department { get; set; }
    public string? HireDate { get; set; }   // char(8) YYYYMMDD
    public string? Plant { get; set; }
}
