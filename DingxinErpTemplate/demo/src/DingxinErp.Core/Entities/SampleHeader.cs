using DingxinErp.Core.Common;

namespace DingxinErp.Core.Entities;

/// <summary>
/// 範例單頭 Entity (對應 ERP 表格 SAMPLE_HEADER)
/// ★ 實際使用時，將此類改名為對應的 ERP 表格名稱 (如 PURTA, COPTC 等)
/// </summary>
public class SampleHeader : IAuditableEntity
{
    // ===== 主鍵欄位 =====
    /// <summary>單別 (PK1, char(4))</summary>
    public string TA001 { get; set; } = string.Empty;

    /// <summary>單號 (PK2, char(11))</summary>
    public string TA002 { get; set; } = string.Empty;

    // ===== 業務欄位 =====
    /// <summary>日期 (char(8), yyyyMMdd)</summary>
    public string TA003 { get; set; } = string.Empty;

    /// <summary>客戶/供應商代號 (char(10))</summary>
    public string TA004 { get; set; } = string.Empty;

    /// <summary>客戶/供應商簡稱 (nvarchar(30))</summary>
    public string? TA005 { get; set; }

    /// <summary>備註 (nvarchar(255))</summary>
    public string? TA006 { get; set; }

    /// <summary>狀態碼 (char(1)): Y=有效, N=作廢</summary>
    public string TA007 { get; set; } = "Y";

    // ===== 審計欄位 (IAuditableEntity) =====
    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    // ===== Navigation Property =====
    /// <summary>單身明細集合</summary>
    public virtual ICollection<SampleDetail> Details { get; set; } = new List<SampleDetail>();
}
