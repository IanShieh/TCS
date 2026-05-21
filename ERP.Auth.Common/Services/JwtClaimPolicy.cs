using System.IdentityModel.Tokens.Jwt;

namespace ERP.Auth.Common.Services;

/// <summary>
/// erp_plus 允許的 JWT claim key 白名單。
/// JwtService 和 JwtBearer middleware 共用此定義。
/// </summary>
internal static class JwtClaimPolicy
{
    internal static readonly HashSet<string> AllowedClaims = new(StringComparer.OrdinalIgnoreCase)
    {
        "iss",          // Issuer（R3-B：設定 Jwt:Issuer 後 token payload 帶此欄位，必須允許）
        "aud",          // Audience（R3-B：設定 Jwt:Audience 後 token payload 帶此欄位，必須允許）
        "sub",          // Subject（使用者 ID）
        "name",         // 使用者名稱
        "role",         // 角色（可多個）
        "company",      // 公司代號
        "department",   // 部門
        "email",        // Email
        "jti",          // JWT ID
        "iat",          // Issued At
        "exp",          // Expiration
        "nbf",          // Not Before
        // erp_plus JwtTokenHelper 使用的 claim key
        "employeeId",       // 員工帳號（erp_plus 慣用，對應 sub）
        "action",           // 系統權限代碼（erp_plus ROLE_SYSTEM.ACTION_ID）
        "originalAction",   // 進入子系統前的原始 Session action（erp_plus DraftVoucher/index.aspx.cs 寫入）
    };
}
