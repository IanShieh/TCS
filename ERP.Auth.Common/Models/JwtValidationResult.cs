namespace ERP.Auth.Common.Models;

public class JwtValidationResult
{
    public bool IsValid { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
    public string? Error { get; init; }

    public static JwtValidationResult Success(
        string userId,
        string userName,
        IReadOnlyDictionary<string, string> claims) =>
        new() { IsValid = true, UserId = userId, UserName = userName, Claims = claims };

    public static JwtValidationResult Fail(string error) =>
        new() { IsValid = false, Error = error };
}
