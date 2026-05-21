using System.Text.Json.Serialization;

namespace ERP.Auth.Common.Models;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    // R1-16：防止 SecretKey 出現在 JSON 序列化日誌或 Exception Detail Page
    [JsonIgnore]
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // R2-B：Origin 白名單，防 Token Fixation CSRF
    // 設定後，/api/auth/token-login 只接受清單內 Origin 的跨站請求
    // 未設定（空陣列）時系統啟動失敗，避免 token-login 入口失去 CSRF 防線
    public string[] AllowedOrigins { get; set; } = [];

    // R1-16：防止 ToString() 或字串插值意外輸出 SecretKey（如錯誤訊息、debug 日誌）
    public override string ToString() =>
        $"JwtSettings {{ Issuer = {Issuer}, Audience = {Audience}, SecretKey = [REDACTED] }}";
}
