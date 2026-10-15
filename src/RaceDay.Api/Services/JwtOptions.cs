namespace RaceDay.Api.Services;

/// <summary>Bound from the "Jwt" configuration section.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "RaceDay.Api";

    public string Audience { get; set; } = "RaceDay.Clients";

    /// <summary>HMAC-SHA256 signing key; must be at least 32 characters.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 7;
}
