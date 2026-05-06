namespace DingxinErp.Core.DTOs;

/// <summary>
/// 更新單頭請求 DTO (僅單頭欄位)
/// </summary>
public class UpdateSampleHeaderRequest
{
    public string TA003 { get; set; } = string.Empty;
    public string TA004 { get; set; } = string.Empty;
    public string? TA005 { get; set; }
    public string? TA006 { get; set; }
    public string TA007 { get; set; } = "Y";
}

/// <summary>
/// 更新單身請求 DTO (獨立單筆更新)
/// </summary>
public class UpdateSampleDetailRequest
{
    public string TB003 { get; set; } = string.Empty;
    public string TB004 { get; set; } = string.Empty;
    public string? TB005 { get; set; }
    public decimal TB006 { get; set; }
    public decimal TB007 { get; set; }
    public decimal TB008 { get; set; }
    public string? TB009 { get; set; }
}
