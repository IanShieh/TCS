using ERP.Auth.Common.Models;

namespace ERP.Auth.Common.Services;

public interface IJwtService
{
    JwtValidationResult ValidateToken(string token);
}
