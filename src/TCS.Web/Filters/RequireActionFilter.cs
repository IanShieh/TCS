using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace TCS.Web.Filters;

public class RequireActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attr = context.ActionDescriptor.EndpointMetadata
            .OfType<RequireActionAttribute>()
            .FirstOrDefault();

        if (attr != null)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new ObjectResult(new { message = "未授權" }) { StatusCode = 401 };
                return;
            }

            var actionClaim = user.FindFirstValue("action") ?? "";
            var allowed = actionClaim.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (!allowed.Contains(attr.Action))
            {
                context.Result = new ObjectResult(new { message = $"您沒有此操作權限：{attr.Action}" }) { StatusCode = 403 };
                return;
            }
        }

        await next();
    }
}
