using System.Security.Claims;

namespace ERP.Auth.Common.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
    string UsrGroup { get; }
}
