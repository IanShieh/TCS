using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ERP.Auth.Common.Models;
using ERP.Auth.Common.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Auth.Common.Extensions;

public static class ErpAuthExtensions
{
    /// <summary>
    /// 註冊 ERP 共用認證服務（在 builder.Services 階段呼叫）
    /// </summary>
    public static IServiceCollection AddErpAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddHttpContextAccessor();
        // R4-C：JwtService 相依只有 IOptions<JwtSettings>（singleton），可安全升為 Singleton
        // Scoped 時每個 request 建新實例，_validationParams 是新 reference
        // 導致 static JwtSecurityTokenHandler 的內部快取永遠 cold miss
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "Jwt settings are not configured. Please add a 'Jwt' section to appsettings.json.");

        if (string.IsNullOrEmpty(jwtSettings.SecretKey))
            throw new InvalidOperationException(
                "Jwt:SecretKey is required but was not configured.");

        // 問題3：HMAC-SHA256 需要至少 32 bytes（256 bits）
        if (System.Text.Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
            throw new InvalidOperationException(
                "Jwt:SecretKey must be at least 32 bytes (256 bits) for HMAC-SHA256.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "ERP_AUTH";
                options.DefaultAuthenticateScheme = "ERP_AUTH";
                options.DefaultChallengeScheme = "ERP_AUTH";
            })
            .AddPolicyScheme("ERP_AUTH", "ERP cookie or bearer auth", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authorization = context.Request.Headers.Authorization.ToString();
                    return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : CookieAuthenticationDefaults.AuthenticationScheme;
                };
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "erp_auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api") ||
                            context.Request.Path.StartsWithSegments("/dbnetlink") ||
                            context.Request.Path.StartsWithSegments("/dbnetsuitecore"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        // 使用 PathBase 確保跳轉到子系統根（如 /account-code/），
                        // 而非 Gateway 的根路徑 /（會再被轉到 /health，令人困惑）
                        var pathBase = context.Request.PathBase.HasValue
                            ? context.Request.PathBase.ToString().TrimEnd('/')
                            : "";
                        context.Response.Redirect(pathBase + "/session-expired");
                        return Task.CompletedTask;
                    }
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateIssuer = !string.IsNullOrEmpty(jwtSettings.Issuer),
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = !string.IsNullOrEmpty(jwtSettings.Audience),
                    ValidAudience = jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero,
                    ValidateLifetime = true,   // 明確設定，與 JwtService 一致（預設亦為 true）
                    // R4-A：明確只接受 HS256，與 JwtService._validationParams 一致
                    // 防止 HS384/HS512 token 透過 Bearer 路徑被接受
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
                };                // 問題11：白名單檢查在 middleware 層統一執行，涵蓋直接 Bearer 請求
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt)
                        {
                            var hasUnknown = jwt.Claims
                                .Any(c => !ERP.Auth.Common.Services.JwtClaimPolicy.AllowedClaims.Contains(c.Type));
                            if (hasUnknown)
                                context.Fail("Token contains unrecognized claims.");
                        }
                        return Task.CompletedTask;
                    }
                };            });

        services.AddAuthorization(options =>
        {
            // 所有端點預設需要認證，無需在每個 Controller/Action 加 [Authorize]
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    /// <summary>
    /// 掛載 ERP 認證端點（在 WebApplication 配置階段呼叫，必須在 UseAuthorization() 之後）
    /// </summary>
    public static WebApplication MapErpAuthEndpoints(this WebApplication app)
    {
        // R1-18：確保 AddErpAuth() 已被呼叫，提供明確的錯誤訊息（否則 DI 錯誤難以 debug）
        // R3-A：IJwtService 是 Singleton；用 CreateScope() 解析只是要確認 AddErpAuth() 已被呼叫
        using (var startupScope = app.Services.CreateScope())
        {
            if (startupScope.ServiceProvider.GetService<IJwtService>() is null)
                throw new InvalidOperationException(
                    "AddErpAuth() must be called before MapErpAuthEndpoints(). " +
                    "Add 'builder.Services.AddErpAuth(builder.Configuration)' in Program.cs.");
        }

        // R1-4：Issuer/Audience 未設定時記錄警告；AllowedOrigins 未設定時直接啟動失敗
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("ERP.Auth.Common");
        var startupSettings = app.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
        if (string.IsNullOrEmpty(startupSettings.Issuer))
            logger.LogWarning(
                "Jwt:Issuer is not configured. Issuer validation is DISABLED. " +
                "Tokens from any issuer will be accepted.");
        if (string.IsNullOrEmpty(startupSettings.Audience))
            logger.LogWarning(
                "Jwt:Audience is not configured. Audience validation is DISABLED. " +
                "Tokens for any audience will be accepted.");
        // R3-G：AllowedOrigins 是唯一 CSRF 防線（Antiforgery 因 erp_plus 相容性已停用）
        // 未設定 = 零 CSRF 防護，必須強制要求設定，不允許靜默上線
        if (startupSettings.AllowedOrigins.Length == 0)
            throw new InvalidOperationException(
                "Jwt:AllowedOrigins must be configured. " +
                "This endpoint has Antiforgery disabled for erp_plus compatibility, " +
                "making AllowedOrigins the only CSRF protection. " +
                "Add allowed origins to appsettings.json: " +
                "\"Jwt\": { \"AllowedOrigins\": [\"http://erp-plus-host:port\"] }");

        // POST /api/auth/token-login
        // 接收來自 erp_plus 自動提交的 POST form，驗證 token 後回傳主頁
        app.MapPost("/api/auth/token-login", async (
            [FromForm] TokenRequest request,
            IJwtService jwtService,
            HttpContext httpContext,
            IOptions<JwtSettings> jwtOptions) =>
        {
            // 問題C：security headers 必須在所有回應路徑（含 401）前設定
            httpContext.Response.Headers["X-Frame-Options"] = "DENY";
            httpContext.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
            // 問題D：含 JWT 的 HTML 不得被快取（防 reverse proxy 或 CDN 快取洩漏）
            httpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            httpContext.Response.Headers["Pragma"] = "no-cache";

            // R5：Minimal API 不自動執行 DataAnnotations，TokenRequest.[Required] 無效
            // 必須在此明確驗證，否則空 token 會進 JwtService 回 401 而非 400
            if (string.IsNullOrWhiteSpace(request.Token))
                return Results.Json(
                    new { error = "Invalid request." },
                    statusCode: StatusCodes.Status400BadRequest);

            // 問題E：防止超長 Token 字串進行 DoS 攻擊（HMAC 運算 + GroupBy 耗資源）
            if (request.Token.Length > 4096)
                return Results.Json(
                    new { error = "Invalid request." },
                    statusCode: StatusCodes.Status400BadRequest);

            // R2-B：Origin 白名單驗證，防 Token Fixation CSRF
            // 瀏覽器發出跨站 POST 時一定帶 Origin；Jwt:AllowedOrigins 在啟動時已強制要求不可為空
            var allowedOrigins = jwtOptions.Value.AllowedOrigins;
            var origin = httpContext.Request.Headers["Origin"].ToString();
            if (!string.IsNullOrEmpty(origin) &&
                !allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                return Results.Json(
                    new { error = "Forbidden." },
                    statusCode: StatusCodes.Status403Forbidden);

            var result = jwtService.ValidateToken(request.Token);

            if (!result.IsValid)
                return Results.Json(
                    new { error = result.Error },
                    statusCode: StatusCodes.Status401Unauthorized);

            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(result.UserId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, result.UserId));
                claims.Add(new Claim("employeeId", result.UserId));
            }

            if (!string.IsNullOrWhiteSpace(result.UserName))
            {
                claims.Add(new Claim(ClaimTypes.Name, result.UserName));
                claims.Add(new Claim("name", result.UserName));
            }

            foreach (var (claimType, claimValue) in result.Claims)
            {
                if (string.IsNullOrWhiteSpace(claimValue))
                    continue;

                if (claimType == "role")
                {
                    foreach (var role in claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        claims.Add(new Claim("role", role));
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    continue;
                }

                if (claims.Any(c => c.Type == claimType && c.Value == claimValue))
                    continue;

                claims.Add(new Claim(claimType, claimValue));
            }

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(8);
            if (result.Claims.TryGetValue("exp", out var expValue) &&
                long.TryParse(expValue, out var expUnixSeconds))
            {
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds);
            }

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = expiresAt,
                    AllowRefresh = false
                });

            // R4-B：中介頁直接寫 sessionStorage 再導向
            // 原本用 window.__erp_token + meta-refresh 的方式無效：meta-refresh 導向 / 時是全新 window
            // 導向後的 erp-auth.js 看不到中介頁的 window.__erp_token，token 永遠無法寫入 sessionStorage
            // 改用 location.replace()：不留瀏覽器歷史，防止按回退重缺取包含 token 的中介頁
            var token = System.Web.HttpUtility.JavaScriptStringEncode(request.Token);
            // 使用 PathBase 確保跳轉到子系統的根路徑（如 /account-code/），而非 Gateway 的根 /
            var pathBase = httpContext.Request.PathBase.HasValue
                ? httpContext.Request.PathBase.ToString().TrimEnd('/')
                : "";
            var redirectTo = pathBase + "/";
            var html = $$"""
                <!DOCTYPE html>
                <html>
                <head>
                  <meta charset="utf-8" />
                  <script>
                    (function() {
                      try {
                        sessionStorage.setItem('erp_auth_token', '{{token}}');
                      } catch(e) {}
                      window.location.replace('{{redirectTo}}');
                    })();
                  </script>
                </head>
                <body></body>
                </html>
                """;

            return Results.Content(html, "text/html; charset=utf-8");
        }).AllowAnonymous()
          .WithName("ErpTokenLogin")
          .DisableAntiforgery()    // form POST 來自 erp_plus，無 antiforgery token
          // R3-D：在 model binding 之前限制 request body 大小（Kestrel 預設 30MB）
          // 防止攻擊者送超大 body 在讀入記憶體後才被 Token.Length > 4096 拒絕
          .WithMetadata(new RequestSizeLimitAttribute(8192));

        // GET /session-expired
        // 未登入時的落地頁（取代直接跳到 Gateway 根路徑造成的 /health 困惑）
        app.MapGet("/session-expired", (HttpContext ctx) =>
        {
            ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            var pathBase = ctx.Request.PathBase.HasValue
                ? ctx.Request.PathBase.ToString().TrimEnd('/')
                : "";
            var html = $$"""
                <!DOCTYPE html>
                <html lang="zh-TW">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>請從 ERP 系統進入</title>
                  <style>
                    * { box-sizing: border-box; margin: 0; padding: 0; }
                    body { font-family: system-ui, -apple-system, sans-serif;
                           background: #f0f2f5; display: flex; align-items: center;
                           justify-content: center; min-height: 100vh; }
                    .card { background: white; border-radius: 12px; padding: 40px 48px;
                            max-width: 420px; text-align: center;
                            box-shadow: 0 4px 20px rgba(0,0,0,.1); }
                    .icon { font-size: 48px; margin-bottom: 16px; }
                    h1 { font-size: 20px; color: #1e3a5f; margin-bottom: 10px; }
                    p { color: #6b7280; font-size: 14px; line-height: 1.6; }
                  </style>
                </head>
                <body>
                  <div class="card">
                    <div class="icon">🔐</div>
                    <h1>請從 ERP 系統進入</h1>
                    <p>此作業需要透過 ERP 主系統連結開啟。<br>
                       請回到 ERP Plus 點選對應的作業連結。</p>
                  </div>
                </body>
                </html>
                """;
            return Results.Content(html, "text/html; charset=utf-8");
        }).AllowAnonymous();

        // GET /api/auth/verify
        // 前端可呼叫此端點確認 token 仍有效，並取得當前使用者資訊
        app.MapGet("/api/auth/verify", (ICurrentUserService currentUser, HttpContext ctx) =>
        {
            // R4-D：含使用者資訊，禁止快取（防共用電腦上 User A 登出後快取被 User B 看到）
            ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            ctx.Response.Headers["Pragma"] = "no-cache";
            return Results.Ok(new
            {
                userId = currentUser.UserId,
                userName = currentUser.UserName,
                roles = currentUser.Roles,
                isAuthenticated = currentUser.IsAuthenticated
            });
        })
            .WithName("ErpAuthVerify");

        return app;
    }
}
