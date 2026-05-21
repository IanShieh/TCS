using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ERP.Auth.Common.Models;
using ERP.Auth.Common.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ERP.Auth.Common.Tests.Services;

public class JwtServiceTests
{
    private const string ValidSecretKey = "test-secret-key-must-be-at-least-32-chars!";
    private const string WrongSecretKey = "wrong-secret-key-must-be-at-least-32chars";

    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = ValidSecretKey,
            Issuer = string.Empty,
            Audience = string.Empty
        });
        _sut = new JwtService(settings);
    }

    // ── 有效 Token ────────────────────────────────────────────────────────

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsSuccess()
    {
        var token = BuildToken(new Dictionary<string, string>
        {
            ["sub"] = "user01",
            ["name"] = "測試使用者"
        });

        var result = _sut.ValidateToken(token);

        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be("user01");
        result.UserName.Should().Be("測試使用者");
        result.Error.Should().BeNull();
    }

    // ── Claim 白名單 ───────────────────────────────────────────────────────

    [Fact]
    public void ValidateToken_WithUnknownClaim_ReturnsFail()
    {
        var token = BuildToken(new Dictionary<string, string>
        {
            ["sub"] = "user01",
            ["name"] = "測試使用者",
            ["__proto__"] = "injected"   // 不在白名單
        });

        var result = _sut.ValidateToken(token);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("unrecognized claims");
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("name")]
    [InlineData("role")]
    [InlineData("company")]
    [InlineData("department")]
    [InlineData("email")]
    [InlineData("jti")]
    [InlineData("employeeId")]   // erp_plus 專用
    [InlineData("action")]       // erp_plus 專用
    public void ValidateToken_WithAllowedClaims_ReturnsSuccess(string claimKey)
    {
        var token = BuildToken(new Dictionary<string, string>
        {
            ["sub"] = "user01",
            [claimKey] = "value"
        });

        var result = _sut.ValidateToken(token);

        result.IsValid.Should().BeTrue();
    }

    // ── erp_plus token 格式（employeeId 取代 sub）─────────────────────────

    [Fact]
    public void ValidateToken_WithEmployeeIdClaim_ReturnsSuccessAndExtractsUserId()
    {
        // erp_plus JwtTokenHelper 產生的 token：使用 employeeId 而非 sub
        var token = BuildToken(new Dictionary<string, string>
        {
            ["employeeId"] = "EMP001",
            ["name"] = "柏林員工",
            ["action"] = "511",
            ["jti"] = Guid.NewGuid().ToString()
        });

        var result = _sut.ValidateToken(token);

        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be("EMP001", "employeeId 應作為 userId 的 fallback");
        result.UserName.Should().Be("柏林員工");
    }

    // ── 多個同名 claim（多角色）───────────────────────────────────────

    [Fact]
    public void ValidateToken_WithMultipleRoleClaims_ReturnsSuccessWithMergedRoles()
    {
        // 問題1：以前 ToDictionary 在這個案例會 crash
        var token = BuildTokenWithMultipleClaims(new Dictionary<string, string>
        {
            ["sub"] = "user01",
            ["name"] = "報備人員"
        }, multiClaims: new[] { ("role", "admin"), ("role", "user"), ("role", "viewer") });

        var result = _sut.ValidateToken(token);

        result.IsValid.Should().BeTrue();
        result.Claims["role"].Should().Contain("admin");
        result.Claims["role"].Should().Contain("user");
    }

    // ── 過期 Token ─────────────────────────────────────────────────────────

    [Fact]
    public void ValidateToken_WithExpiredToken_ReturnsFail()
    {
        var token = BuildToken(
            new Dictionary<string, string> { ["sub"] = "user01", ["name"] = "測試" },
            expiry: DateTime.UtcNow.AddHours(-1));

        var result = _sut.ValidateToken(token);

        result.IsValid.Should().BeFalse();
    }

    // ── 偽造簽名 ───────────────────────────────────────────────────────────

    [Fact]
    public void ValidateToken_WithWrongSignature_ReturnsFail()
    {
        var token = BuildTokenWithKey(
            new Dictionary<string, string> { ["sub"] = "user01", ["name"] = "測試" },
            secretKey: WrongSecretKey);

        var result = _sut.ValidateToken(token);

        result.IsValid.Should().BeFalse();
    }

    // ── 空白/格式錯誤 ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not.a.jwt")]
    public void ValidateToken_WithInvalidInput_ReturnsFail(string token)
    {
        var result = _sut.ValidateToken(token);

        result.IsValid.Should().BeFalse();
    }

    // ── R3-B 回歸：iss / aud claim 必須在白名單 ──────────────────────────

    [Fact]
    public void ValidateToken_WithIssuerAndAudienceClaims_ReturnsSuccess()
    {
        // 設定 Issuer + Audience 的 JwtService 實例
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = ValidSecretKey,
            Issuer = "erp_plus",
            Audience = "erp_apps"
        });
        var sut = new JwtService(settings);

        // Token 包含 iss + aud claim（設定 Issuer/Audience 時 erp_plus 產生的 JWT 都有這兩個欄位）
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidSecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: "erp_plus",
            audience: "erp_apps",
            claims: [new Claim("sub", "user01"), new Claim("name", "測試")],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(jwt);

        var result = sut.ValidateToken(tokenStr);

        // R3-B 修正前：iss/aud 不在白名單 → "Token contains unrecognized claims."
        // R3-B 修正後：iss/aud 在白名單 → 驗證成功
        result.IsValid.Should().BeTrue("iss/aud must be in whitelist when Issuer/Audience is configured");
        result.UserId.Should().Be("user01");
    }

    // ── 輔助方法 ───────────────────────────────────────────────────────────

    private string BuildToken(
        Dictionary<string, string> claims,
        DateTime? expiry = null,
        string? secretKey = null) =>
        BuildTokenWithKey(claims, expiry, secretKey ?? ValidSecretKey);

    private static string BuildTokenWithKey(
        Dictionary<string, string> claims,
        DateTime? expiry = null,
        string secretKey = ValidSecretKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimsList = claims
            .Select(kvp => new Claim(kvp.Key, kvp.Value))
            .ToList();

        var token = new JwtSecurityToken(
            claims: claimsList,
            expires: expiry ?? DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// 建立含多個同名 claim 的 token（用於測試多角色）
    private static string BuildTokenWithMultipleClaims(
        Dictionary<string, string> baseClaims,
        (string key, string value)[] multiClaims,
        string secretKey = ValidSecretKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimsList = baseClaims
            .Select(kvp => new Claim(kvp.Key, kvp.Value))
            .Concat(multiClaims.Select(t => new Claim(t.key, t.value)))
            .ToList();

        var token = new JwtSecurityToken(
            claims: claimsList,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
