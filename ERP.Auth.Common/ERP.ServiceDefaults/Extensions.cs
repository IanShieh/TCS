using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// ERP 子系統共用 Aspire 整合 — AddServiceDefaults / MapDefaultEndpoints
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// 在子系統的 Program.cs 最前面呼叫，一行啟用 OpenTelemetry + 健康檢查 + Service Discovery
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        return builder;
    }

    private static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        // 健康檢查授權政策：僅允許本機（loopback）呼叫，外部 IP 一律 403
        // 不使用 AllowAnonymous，以維持其他端點的安全性
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("HealthCheckLocalhostOnly", policy =>
                policy.RequireAssertion(ctx =>
                {
                    if (ctx.Resource is HttpContext httpCtx)
                    {
                        var ip = httpCtx.Connection.RemoteIpAddress;
                        return ip != null && IPAddress.IsLoopback(ip);
                    }
                    return false;
                }));

        return builder;
    }

    /// <summary>
    /// 在 app.Run() 之前呼叫，掛載健康檢查端點（供 Aspire Dashboard 使用）
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // /health  → 綜合健康狀態（僅限 localhost 呼叫，外部 IP 回傳 403）
        // /alive   → 存活探針
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        }).RequireAuthorization("HealthCheckLocalhostOnly");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        }).RequireAuthorization("HealthCheckLocalhostOnly");

        return app;
    }
}
