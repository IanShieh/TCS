using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(); // Aspire: OpenTelemetry + Service Discovery（讓 YARP 能解析 http://account-code/ 等 service name）

// ─── JWT 驗證（集中在 Gateway 驗證，子系統信任 Gateway 轉發的 Header）─────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey 未設定");
var issuer    = jwtSection["Issuer"]    ?? "";
var audience  = jwtSection["Audience"]  ?? "";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            // 與 ERP.Auth.Common 一致：Issuer/Audience 空字串 = 不驗證
            ValidateIssuer           = !string.IsNullOrEmpty(issuer),
            ValidIssuer              = issuer,
            ValidateAudience         = !string.IsNullOrEmpty(audience),
            ValidAudience            = audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
            ValidAlgorithms          = ["HS256"],
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // 允許從 query string 讀取 token（靜態資源等）
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token)) ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    })
    .AddCookie("Dashboard", opts =>
    {
        opts.LoginPath              = "/dashboard/login";
        opts.Cookie.Name            = "erp_dashboard";
        opts.Cookie.HttpOnly        = true;
        opts.Cookie.SameSite        = SameSiteMode.Lax;
        opts.Cookie.SecurePolicy    = CookieSecurePolicy.SameAsRequest;
        opts.ExpireTimeSpan         = TimeSpan.FromHours(8);
        opts.SlidingExpiration      = true;
    });

builder.Services.AddAuthorization(options =>
{
    // Dashboard Cookie 驗證（管理頁面）
    options.AddPolicy("DashboardPolicy", policy =>
        policy.AddAuthenticationSchemes("Dashboard").RequireAuthenticatedUser());

    // JWT Bearer 驗證（所有 ERP 子系統 API 路由）
    options.AddPolicy("JwtPolicy", policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
              .RequireAuthenticatedUser());
});

// IHttpClientFactory 供 Dashboard 健康檢查使用
builder.Services.AddHttpClient();

// ─── YARP 反向代理 ──────────────────────────────────────────────────────────
// AddServiceDiscoveryDestinationResolver：讓 YARP Cluster 用 Aspire 動態 port（service name 路由）
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint（Aspire 用）
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

// Gateway 首頁：顯示路由總覽
app.MapGet("/", () => Results.Redirect("/health")).AllowAnonymous();

// ─── Dashboard 登入 ─────────────────────────────────────────────────────────
app.MapGet("/dashboard/login", (string? returnUrl) =>
    Results.Content(DashboardHtml.LoginPage(returnUrl), "text/html"))
   .AllowAnonymous();

app.MapPost("/dashboard/login", async (HttpContext ctx, IConfiguration cfg) =>
{
    var form      = await ctx.Request.ReadFormAsync();
    var username  = form["username"].ToString().Trim();
    var password  = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var expectedUser = cfg["Dashboard:Username"] ?? "admin";
    var expectedPass = cfg["Dashboard:Password"] ?? "admin";

    if (username == expectedUser && password == expectedPass)
    {
        var claims    = new[] { new Claim(ClaimTypes.Name, username) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Dashboard"));
        await ctx.SignInAsync("Dashboard", principal);
        // 只接受相對路徑，防止 Open Redirect 攻擊（排除 // 協議相對路徑）
        var redirect = !string.IsNullOrEmpty(returnUrl)
            && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//")
            ? returnUrl : "/dashboard";
        return Results.Redirect(redirect);
    }
    return Results.Content(DashboardHtml.LoginPage(returnUrl, error: true), "text/html");
}).AllowAnonymous();

// POST 登出（防止 CSRF via link）
app.MapPost("/dashboard/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync("Dashboard");
    return Results.Redirect("/dashboard/login");
}).AllowAnonymous();

// ─── Dashboard 狀態頁（需 Dashboard Cookie 驗證）────────────────────────────
app.MapGet("/dashboard", async (IHttpClientFactory hcf, IConfiguration cfg, HttpContext ctx) =>
{
    var client = hcf.CreateClient();

    // 從 Routes config 建立 cluster → PathBase 前綴的對照表
    // 只取 "pages" 路由（不含 /api/）來萃取頂層前綴，例如 /account-code
    var clusterToPrefix = cfg.GetSection("ReverseProxy:Routes").GetChildren()
        .Select(r => new
        {
            ClusterId = r.GetSection("ClusterId").Value ?? "",
            Path      = r.GetSection("Match:Path").Value ?? ""
        })
        .Where(r => !string.IsNullOrEmpty(r.ClusterId)
                    && r.Path.EndsWith("/{**catch-all}")
                    && !r.Path.Contains("/api/"))
        .GroupBy(r => r.ClusterId)
        .ToDictionary(g => g.Key, g =>
            g.First().Path.Replace("/{**catch-all}", ""));

    var gatewayBase = $"{ctx.Request.Scheme}://{ctx.Request.Host}";

    // 從 YARP Cluster 設定讀取各服務位址，避免重複設定 port
    var healthTasks = cfg.GetSection("ReverseProxy:Clusters").GetChildren()
        .Select(c => new
        {
            Name         = c.Key,
            InternalAddr = (c.GetSection("Destinations:primary:Address").Value ?? "").TrimEnd('/'),
            DisplayUrl   = clusterToPrefix.TryGetValue(c.Key, out var prefix)
                           ? $"{gatewayBase}{prefix}/"
                           : (c.GetSection("Destinations:primary:Address").Value ?? "").TrimEnd('/')
        })
        .Where(c => !string.IsNullOrEmpty(c.InternalAddr))
        .Select(async c =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                var resp = await client.GetAsync($"{c.InternalAddr}/health", cts.Token);
                return new ServiceStatus(c.Name, c.DisplayUrl,
                    resp.IsSuccessStatusCode, $"HTTP {(int)resp.StatusCode}");
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                return new ServiceStatus(c.Name, c.DisplayUrl, false, "連線逾時 (3s)");
            }
            catch (Exception ex)
            {
                return new ServiceStatus(c.Name, c.DisplayUrl, false,
                    ex.Message[..Math.Min(80, ex.Message.Length)]);
            }
        });

    var statuses = await Task.WhenAll(healthTasks);
    var username = ctx.User.FindFirst(ClaimTypes.Name)?.Value ?? "admin";
    return Results.Content(DashboardHtml.StatusPage(statuses, username), "text/html");
}).RequireAuthorization("DashboardPolicy");

// JWT 授權改為 per-route（在 appsettings.json 各路由的 AuthorizationPolicy 設定）
// /*/api/auth/** → Anonymous（token-login 入口）
// /*/api/**     → JwtPolicy（API 資料端點）
// /**           → Anonymous（HTML 頁面 / 靜態資源）
app.MapReverseProxy();

app.Run();

