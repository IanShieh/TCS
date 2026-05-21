using System.Security.Claims;
using ERP.Auth.Common.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace ERP.Auth.Common.Tests.Services;

public class CurrentUserServiceTests
{
    // ── UserId fallback 順序 ─────────────────────────────────────────────

    [Fact]
    public void UserId_WithSubClaim_ReturnsSubValue()
    {
        // 標準 JWT：sub claim 被 MapInboundClaims 對應到 NameIdentifier
        var sut = BuildService(new Claim(ClaimTypes.NameIdentifier, "user01"));

        sut.UserId.Should().Be("user01");
    }

    [Fact]
    public void UserId_WithRawSubClaim_ReturnsSubValue()
    {
        // MapInboundClaims=false 時 "sub" 不被轉換，仍需能解析
        var sut = BuildService(new Claim("sub", "user02"));

        sut.UserId.Should().Be("user02");
    }

    [Fact]
    public void UserId_WithEmployeeIdClaim_ReturnsEmployeeId()
    {
        // R2-H 迴歸：erp_plus SSO token 使用 "employeeId" 而非 "sub"
        // 修正前：UserId 永遠回傳 string.Empty → 所有 erp_plus SSO 用戶在 Bearer 請求中無法識別
        var sut = BuildService(new Claim("employeeId", "EMP001"));

        sut.UserId.Should().Be("EMP001",
            "employeeId claim must be supported as UserId fallback for erp_plus SSO tokens");
    }

    [Fact]
    public void UserId_WithNoClaim_ReturnsEmpty()
    {
        var sut = BuildService(); // 沒有任何 userId 相關 claim

        sut.UserId.Should().BeEmpty();
    }

    [Fact]
    public void UserId_PrioritizesNameIdentifierOverSub()
    {
        // NameIdentifier 優先於 sub
        var sut = BuildService(
            new Claim(ClaimTypes.NameIdentifier, "from-nameidentifier"),
            new Claim("sub", "from-sub"));

        sut.UserId.Should().Be("from-nameidentifier");
    }

    // ── UserName ─────────────────────────────────────────────────────────

    [Fact]
    public void UserName_WithNameClaim_ReturnsName()
    {
        var sut = BuildService(new Claim("name", "柏林員工"));

        sut.UserName.Should().Be("柏林員工");
    }

    // ── IsAuthenticated ──────────────────────────────────────────────────

    [Fact]
    public void IsAuthenticated_WhenAuthenticated_ReturnsTrue()
    {
        var identity = new ClaimsIdentity([new Claim("sub", "u1")], authenticationType: "jwt");
        var principal = new ClaimsPrincipal(identity);
        var sut = BuildServiceWithPrincipal(principal);

        sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenNotAuthenticated_ReturnsFalse()
    {
        // 沒有 authenticationType = 未認證的 Identity
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        var sut = BuildServiceWithPrincipal(principal);

        sut.IsAuthenticated.Should().BeFalse();
    }

    // ── 輔助方法 ─────────────────────────────────────────────────────────

    private static CurrentUserService BuildService(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        var principal = new ClaimsPrincipal(identity);
        return BuildServiceWithPrincipal(principal);
    }

    private static CurrentUserService BuildServiceWithPrincipal(ClaimsPrincipal principal)
    {
        var mockAccessor = new Mock<IHttpContextAccessor>();
        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(c => c.User).Returns(principal);
        mockAccessor.Setup(a => a.HttpContext).Returns(mockContext.Object);
        return new CurrentUserService(mockAccessor.Object);
    }
}
