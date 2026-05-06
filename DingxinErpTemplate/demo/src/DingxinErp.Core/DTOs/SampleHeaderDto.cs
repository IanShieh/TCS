namespace DingxinErp.Core.DTOs;

/// <summary>
/// 單頭查詢回傳 DTO
/// </summary>
public class SampleHeaderDto
{
    public string TA001 { get; set; } = string.Empty;
    public string TA002 { get; set; } = string.Empty;
    public string TA003 { get; set; } = string.Empty;
    public string TA004 { get; set; } = string.Empty;
    public string? TA005 { get; set; }
    public string? TA006 { get; set; }
    public string TA007 { get; set; } = "Y";
    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public int DetailCount { get; set; }
}
