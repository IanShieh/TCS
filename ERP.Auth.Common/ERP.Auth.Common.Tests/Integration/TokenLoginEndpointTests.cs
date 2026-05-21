using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using ERP.Auth.Common.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ERP.Auth.Common.Tests.Integration;

public class TokenLoginEndpointTests
{
    private const string SecretKey = "test-secret-key-must-be-at-least-32-chars!";
    private const string AllowedOrigin = "http://192.168.168.15:9922";

    [Fact]
    public async Task TokenLogin_WithAllowedOriginAndValidToken_ReturnsOk()
    {
        await using var app = await CreateAppAsync();
        var response = await PostTokenLoginAsync(app, AllowedOrigin, BuildValidToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(cookie => cookie.Contains("erp_auth=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TokenLogin_WithAllowedOriginAndInvalidToken_ReturnsUnauthorized()
    {
        await using var app = await CreateAppAsync();
        var response = await PostTokenLoginAsync(app, AllowedOrigin, "not.a.jwt");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenLogin_WithDisallowedOrigin_ReturnsForbiddenBeforeTokenValidation()
    {
        await using var app = await CreateAppAsync();
        var response = await PostTokenLoginAsync(app, "http://evil.example", BuildValidToken());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TokenLogin_WithoutOriginAndValidToken_ReturnsOk()
    {
        await using var app = await CreateAppAsync();
        var response = await PostTokenLoginAsync(app, origin: null, BuildValidToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void MapErpAuthEndpoints_WithoutAllowedOrigins_ThrowsStartupFailure()
    {
        using var app = CreateAppWithoutMapping(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = SecretKey
        });

        var action = () => app.MapErpAuthEndpoints();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:AllowedOrigins must be configured*");
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var app = CreateAppWithoutMapping(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = SecretKey,
            ["Jwt:AllowedOrigins:0"] = AllowedOrigin
        });

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapErpAuthEndpoints();
        await app.StartAsync();

        return app;
    }

    private static WebApplication CreateAppWithoutMapping(Dictionary<string, string?> configuration)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddErpAuth(builder.Configuration);

        return builder.Build();
    }

    private static async Task<HttpResponseMessage> PostTokenLoginAsync(
        WebApplication app,
        string? origin,
        string token)
    {
        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/token-login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Token"] = token
            })
        };

        if (!string.IsNullOrEmpty(origin))
            request.Headers.TryAddWithoutValidation("Origin", origin);

        return await client.SendAsync(request);
    }

    private static string BuildValidToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim("employeeId", "EMP001"),
                new Claim("name", "測試員工"),
                new Claim("action", "511"),
                new Claim("jti", Guid.NewGuid().ToString())
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
