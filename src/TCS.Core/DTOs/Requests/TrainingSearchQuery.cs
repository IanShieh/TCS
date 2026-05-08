namespace TCS.Core.DTOs.Requests;

/// <summary>受訓單頭進階搜尋參數（spec §7-3 進階搜尋）</summary>
public class TrainingSearchQuery
{
    /// <summary>快速搜尋；對 EmployeeId / EmployeeName / LicenseType / Description 做 OR LIKE</summary>
    public string? Search { get; set; }
    public string? EmployeeId { get; set; }
    /// <summary>姓名 LIKE</summary>
    public string? NameContains { get; set; }
    public string? Department { get; set; }
    public string? LicenseType { get; set; }
    /// <summary>僅顯示已過期（OverallStatus == 已過期）</summary>
    public bool? ExpiredOnly { get; set; }
    /// <summary>未達時數 &gt; 0</summary>
    public bool? UnmetHoursOnly { get; set; }
    public DateOnly? NextReviewFrom { get; set; }
    public DateOnly? NextReviewTo { get; set; }

    public bool IsAdvancedActive =>
        !string.IsNullOrWhiteSpace(EmployeeId)
        || !string.IsNullOrWhiteSpace(NameContains)
        || !string.IsNullOrWhiteSpace(Department)
        || !string.IsNullOrWhiteSpace(LicenseType)
        || ExpiredOnly == true
        || UnmetHoursOnly == true
        || NextReviewFrom is not null
        || NextReviewTo is not null;
}
