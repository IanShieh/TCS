using DingxinErp.Core.Common;

namespace DingxinErp.Core.Entities;

/// <summary>
/// 範例單身 Entity (對應 ERP 表格 SAMPLE_DETAIL)
/// ★ 實際使用時，將此類改名為對應的 ERP 表格名稱 (如 PURTB, COPTD 等)
/// </summary>
public class SampleDetail
{
    // ===== 主鍵欄位 (複合PK: TB001+TB002+TB003) =====
    /// <summary>單別 (PK1, FK→SampleHeader.TA001, char(4))</summary>
    public string TB001 { get; set; } = string.Empty;

    /// <summary>單號 (PK2, FK→SampleHeader.TA002, char(11))</summary>
    public string TB002 { get; set; } = string.Empty;

    /// <summary>序號 (PK3, char(4))</summary>
    public string TB003 { get; set; } = string.Empty;

    // ===== 業務欄位 =====
    /// <summary>品號 (char(20))</summary>
    public string TB004 { get; set; } = string.Empty;

    /// <summary>品名 (nvarchar(50))</summary>
    public string? TB005 { get; set; }

    /// <summary>數量 (decimal(16,4))</summary>
    public decimal TB006 { get; set; }

    /// <summary>單價 (decimal(16,4))</summary>
    public decimal TB007 { get; set; }

    /// <summary>金額 (decimal(16,4))</summary>
    public decimal TB008 { get; set; }

    /// <summary>備註 (nvarchar(255))</summary>
    public string? TB009 { get; set; }

    // ===== Navigation Property =====
    /// <summary>關聯單頭</summary>
    public virtual SampleHeader? Header { get; set; }
}
