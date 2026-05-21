using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ERP.Auth.Common.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub")
        ?? User?.FindFirstValue("employeeId")  // erp_plus SSO token 使用 employeeId 而非 sub
        ?? string.Empty;

    public string UserName =>
        User?.FindFirstValue(ClaimTypes.Name)
        ?? User?.FindFirstValue("name")
        ?? string.Empty;

    public IEnumerable<string> Roles =>
        // R1-12：同時查長名 ClaimTypes.Role 與短名 "role"，相容 MapInboundClaims=false 的情境
        // 預設 MapInboundClaims=true 時兩者都查會產生重複值，Distinct() 負責去重
        User is null
            ? Enumerable.Empty<string>()
            : User.FindAll(ClaimTypes.Role)
                  .Concat(User.FindAll("role"))
                  .Select(c => c.Value)
                  .Distinct(StringComparer.OrdinalIgnoreCase);

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public string UsrGroup =>
        User?.FindFirstValue("department") ?? string.Empty;
}
