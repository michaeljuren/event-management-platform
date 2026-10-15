using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Authorization;

/// <summary>
/// Runs after a JWT's signature and lifetime have been validated and rejects it unless the session it
/// was issued for still exists, has not been revoked or expired, and belongs to an active user.
/// This is what makes logout and account deactivation take effect immediately.
/// </summary>
public static class SessionValidation
{
    public static async Task ValidateSessionAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var sessionId = principal?.GetSessionId();
        if (principal is null || sessionId is null)
        {
            context.Fail("Token is not bound to a session.");
            return;
        }

        var services = context.HttpContext.RequestServices;
        var db = services.GetRequiredService<RaceDayDbContext>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        var session = await db.UserSessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId.Value)
            .Select(s => new { s.UserId, s.RevokedAt, s.ExpiresAt, s.User.IsActive })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now || !session.IsActive)
        {
            context.Fail("Session has ended. Please log in again.");
            return;
        }

        if (session.UserId != principal.GetUserId())
        {
            context.Fail("Token does not match its session.");
        }
    }
}
