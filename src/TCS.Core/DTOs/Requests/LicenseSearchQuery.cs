namespace TCS.Core.DTOs.Requests;

/// <summary>證照主檔進階搜尋參數（spec §7-2 進階搜尋）</summary>
public class LicenseSearchQuery
{
    /// <summary>證照類別前綴 LIKE</summary>
    public string? LicenseTypePrefix { get; set; }
    /// <summary>描述 LIKE</summary>
    public string? DescriptionContains { get; set; }
    /// <summary>對應大類（LicenseType 純整數）</summary>
    public string? Category { get; set; }
    public int? HoursMin { get; set; }
    public int? HoursMax { get; set; }
    public int? YearsMin { get; set; }
    public int? YearsMax { get; set; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(LicenseTypePrefix)
        && string.IsNullOrWhiteSpace(DescriptionContains)
        && string.IsNullOrWhiteSpace(Category)
        && HoursMin is null && HoursMax is null
        && YearsMin is null && YearsMax is null;
}
