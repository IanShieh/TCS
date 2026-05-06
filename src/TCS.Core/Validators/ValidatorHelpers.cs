using System.Text.RegularExpressions;

namespace TCS.Core.Validators;

/// <summary>驗證器共用靜態輔助</summary>
internal static partial class ValidatorHelpers
{
    [GeneratedRegex(@"^\d+(\.\d+)*$", RegexOptions.None)]
    private static partial Regex LicenseTypePatternRegex();

    [GeneratedRegex(@"^\d+$", RegexOptions.None)]
    private static partial Regex CategoryPatternRegex();

    [GeneratedRegex(@"--|;|/\*|\*/|EXEC|DROP|ALTER", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousCharsRegex();

    /// <summary>LicenseType 格式：純整數（大類）或含小數點數字（小類），如 1, 1.1, 2.3.1</summary>
    internal static bool IsValidLicenseTypeFormat(string? v) =>
        v is not null && LicenseTypePatternRegex().IsMatch(v);

    /// <summary>LicenseType 為大類（純整數）</summary>
    internal static bool IsLicenseTypeCategory(string? v) =>
        v is not null && CategoryPatternRegex().IsMatch(v);

    /// <summary>字串不含危險 SQL/XSS 字元（§11）</summary>
    internal static bool IsSafe(string? v) =>
        v is null || !DangerousCharsRegex().IsMatch(v);
}
