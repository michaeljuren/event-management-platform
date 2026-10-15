using System.Security.Claims;

namespace RaceDay.Api.Services;

/// <summary>Claim types written into RaceDay access tokens (inbound claim mapping is disabled).</summary>
public static class AppClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string SessionId = "sid";

    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(UserId), out var id)
            ? id
            : throw new InvalidOperationException("The current principal has no user id claim.");

    public static Guid? GetSessionId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(SessionId), out var id) ? id : null;
}
