namespace ERP.Auth.Common.Models;

public class TokenRequest
{
    // 注意：Minimal API 不自動執行 DataAnnotations 驗證。
    // 長度保護在端點內 Token.Length > 4096 執行；空值在端點內 IsNullOrWhiteSpace 攔截。
    public string Token { get; set; } = string.Empty;
}
