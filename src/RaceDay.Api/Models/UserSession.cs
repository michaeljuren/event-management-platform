namespace RaceDay.Api.Models;

/// <summary>
/// A server-side login session. Not part of the Part 1 script: it is the one table added so that
/// sessions can be listed, refreshed and revoked. Every access token carries its session id and is
/// rejected as soon as the session is revoked or expires.
/// </summary>
public class UserSession
{
    public Guid SessionId { get; set; }

    public int UserId { get; set; }

    /// <summary>SHA-256 of the current refresh token. The raw token is only ever held by the client.</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastRefreshedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = null!;

    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
