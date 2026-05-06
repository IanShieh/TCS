using System.Net;
using System.Text.Json;

namespace DingxinErp.Web.Middleware;

/// <summary>
/// 全域例外處理中介軟體
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發生未預期的錯誤: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, detail) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.BadRequest, "操作無效", exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, "參數錯誤", exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "資源不存在", exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "未授權存取", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "伺服器錯誤", "系統發生錯誤，請稍後再試")
        };

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail,
            instance = context.Request.Path.ToString()
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// 中介軟體擴充方法
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
