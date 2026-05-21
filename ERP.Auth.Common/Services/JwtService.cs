using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ERP.Auth.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Auth.Common.Services;

public class JwtService : IJwtService
{
    // 問題15：單例 handler，利用內建 token validation cache
    private static readonly JwtSecurityTokenHandler TokenHandler = new();

    // R3-F：TokenValidationParameters 在建構時建立一次，讓 TokenHandler 快取真正生效
    // 每次呼叫 ValidateToken() 重建 key 會導致每個 request 都 cache miss
    private readonly TokenValidationParameters _validationParams;

    public JwtService(IOptions<JwtSettings> options)
    {
        var settings = options.Value;
        _validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
            ValidateIssuer = !string.IsNullOrEmpty(settings.Issuer),
            ValidIssuer = settings.Issuer,
            ValidateAudience = !string.IsNullOrEmpty(settings.Audience),
            ValidAudience = settings.Audience,
            ClockSkew = TimeSpan.Zero,       // 嚴格過期驗證
            ValidateLifetime = true,
            // R3-E：明確只接受 HS256，防演算法混淆攻擊（防禦縱深）
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    }

    public JwtValidationResult ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return JwtValidationResult.Fail("Token is empty.");

        try
        {
            TokenHandler.ValidateToken(token, _validationParams, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // Claim 白名單檢查（問題10：不揭露未知 claim 名稱）
            var hasUnknownClaims = jwtToken.Claims
                .Any(c => !JwtClaimPolicy.AllowedClaims.Contains(c.Type));

            if (hasUnknownClaims)
                return JwtValidationResult.Fail("Token contains unrecognized claims.");

            // erp_plus 用 "employeeId" key，標準 JWT 用 "sub"；兩者都支援
            var userId = jwtToken.Claims
                .FirstOrDefault(c => c.Type is "sub" or "employeeId")?.Value
                ?? string.Empty;

            var userName = jwtToken.Claims
                .FirstOrDefault(c => c.Type is "name")?.Value
                ?? string.Empty;

            // 問題1：多個同名 claim（如多個 role）用逗號合併，避免 ToDictionary crash
            var claimsDict = jwtToken.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(",", g.Select(c => c.Value)));

            return JwtValidationResult.Success(userId, userName, claimsDict);
        }
        catch (SecurityTokenExpiredException)
        {
            return JwtValidationResult.Fail("Token has expired.");
        }
        catch (SecurityTokenException)
        {
            // 問題7：不洩漏 exception 細節給 client
            return JwtValidationResult.Fail("Token validation failed.");
        }
        catch (Exception)
        {
            // 問題7：不洩漏 exception 細節給 client
            return JwtValidationResult.Fail("Token validation failed.");
        }
    }
}
