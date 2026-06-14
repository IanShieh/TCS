namespace TCS.Core.Helpers;

/// <summary>
/// 「其他」證照代碼產生規則(避免 PK 衝突)。
/// 其他大類:base "99" → 代碼 99.{n}(一個點)。
/// 其他小類:base 母大類 X → 代碼 X.0.{n}(兩個點,0 為保留的其他桶)。
/// 流水號由呼叫端提供「同前綴既有代碼」後計算最大值 +1。
/// </summary>
public static class OtherLicenseCode
{
    /// <summary>由 base 母類碼推導代碼前綴。</summary>
    public static string Prefix(string baseCategory) =>
        baseCategory == "99" ? "99" : $"{baseCategory}.0";

    /// <summary>
    /// 依既有代碼算出下一個唯一代碼。只計入「前綴 + 一段純數字」者。
    /// </summary>
    public static string Next(string prefix, IEnumerable<string> existing)
    {
        var head = prefix + ".";
        var max = 0;
        foreach (var code in existing)
        {
            if (code is null || !code.StartsWith(head, StringComparison.Ordinal)) continue;
            var tail = code[head.Length..];
            if (int.TryParse(tail, out var n) && n > max) max = n;
        }
        return $"{prefix}.{max + 1}";
    }
}
