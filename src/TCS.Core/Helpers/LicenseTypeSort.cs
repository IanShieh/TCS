namespace TCS.Core.Helpers;

/// <summary>證照代碼自然排序（1.1 → 3.2 → 11.2，非字串排序）</summary>
public static class LicenseTypeSort
{
    /// <summary>將 "1.10" → "000001.000010" 使字串排序等同自然數排序；非數字段落原樣保留</summary>
    public static string NaturalKey(string licenseType) =>
        string.Join(".", licenseType.Split('.')
            .Select(seg => int.TryParse(seg, out var n) ? n.ToString("D6") : seg));
}
