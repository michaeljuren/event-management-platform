using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Tests.Unit;

public class TokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "unit-test-signing-key-that-is-long-enough-0123456789",
        AccessTokenMinutes = 15
    };

    private readonly TokenService _service = new(Microsoft.Extensions.Options.Options.Create(Options), TimeProvider.System);

    [Fact]
    public async Task AccessToken_IsSignedAndCarriesUserRolesAndSession()
    {
        var user = new User { UserId = 42, Email = "runner@test.co.za", FirstName = "Ann", LastName = "Runner" };
        var sessionId = Guid.NewGuid();

        var token = _service.CreateAccessToken(user, ["Organizer", "Participant"], sessionId);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = TokenService.CreateSigningKey(Options.SigningKey),
            RoleClaimType = AppClaims.Role
        });

        Assert.True(result.IsValid);
        Assert.Equal(15 * 60, token.ExpiresInSeconds);
        Assert.Equal("42", result.ClaimsIdentity.FindFirst(AppClaims.UserId)?.Value);
        Assert.Equal(sessionId.ToString(), result.ClaimsIdentity.FindFirst(AppClaims.SessionId)?.Value);
        Assert.Equal(2, result.ClaimsIdentity.FindAll(AppClaims.Role).Count());
    }

    [Fact]
    public async Task AccessToken_SignedWithDifferentKey_IsRejected()
    {
        var token = _service.CreateAccessToken(new User { UserId = 1, Email = "a@b.co.za" }, ["Participant"], Guid.NewGuid());

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = TokenService.CreateSigningKey("a-completely-different-signing-key-0123456789")
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RefreshTokens_AreRandom_AndHashDeterministically()
    {
        var first = _service.CreateRefreshToken();
        var second = _service.CreateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(_service.HashRefreshToken(first), _service.HashRefreshToken(first));
        Assert.NotEqual(first, _service.HashRefreshToken(first));
        Assert.Equal(64, _service.HashRefreshToken(first).Length);
    }
}
