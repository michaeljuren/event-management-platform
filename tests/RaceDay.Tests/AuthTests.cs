using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

/// <summary>User registration, login, token refresh and logout.</summary>
public class AuthTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("Participant", "Participant")]
    [InlineData("Organizer", "Organizer")]
    [InlineData("organiser", "Organizer")]
    public async Task Register_WithValidDetails_Returns201AndAssignsChosenRole(string requestedRole, string expectedRole)
    {
        var email = ApiClient.UniqueEmail();

        var response = await _client.RegisterAsync(ApiClient.RegistrationBody(email, requestedRole));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.ReadAsync<UserSummaryResponse>();
        Assert.Equal(email, user.Email);
        Assert.Equal(expectedRole, Assert.Single(user.Roles));
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var email = ApiClient.UniqueEmail();
        await _client.RegisterAsync(ApiClient.RegistrationBody(email, "Participant"));

        var storedHash = await factory.WithDbAsync(db =>
            db.Users.Where(u => u.Email == email).Select(u => u.PasswordHash).SingleAsync());

        Assert.NotEqual(ApiClient.ValidPassword, storedHash);
        Assert.DoesNotContain(ApiClient.ValidPassword, storedHash);
        Assert.StartsWith("AQAAAA", storedHash); // ASP.NET Core Identity V3 PBKDF2 format marker
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var email = ApiClient.UniqueEmail();
        await _client.RegisterAsync(ApiClient.RegistrationBody(email, "Participant"));

        var response = await _client.RegisterAsync(ApiClient.RegistrationBody(email.ToUpperInvariant(), "Organizer"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Superuser")]
    [InlineData("")]
    public async Task Register_WithRoleOtherThanOrganizerOrParticipant_Returns400(string role)
    {
        var response = await _client.RegisterAsync(ApiClient.RegistrationBody(ApiClient.UniqueEmail(), role));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("short1A")]          // too short
    [InlineData("alllowercase1")]    // no upper-case letter
    [InlineData("NoDigitsHere")]     // no digit
    public async Task Register_WithWeakPassword_Returns400(string password)
    {
        var response = await _client.RegisterAsync(
            ApiClient.RegistrationBody(ApiClient.UniqueEmail(), "Participant", password: password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmailGenderOrMissingDateOfBirth_Returns400()
    {
        var badEmail = await _client.RegisterAsync(ApiClient.RegistrationBody("not-an-email", "Participant"));
        var badGender = await _client.RegisterAsync(ApiClient.RegistrationBody(ApiClient.UniqueEmail(), "Participant", gender: "Unknown"));
        var noDob = await _client.RegisterAsync(ApiClient.RegistrationBody(ApiClient.UniqueEmail(), "Participant", dateOfBirth: null));
        var futureDob = await _client.RegisterAsync(ApiClient.RegistrationBody(ApiClient.UniqueEmail(), "Participant",
            dateOfBirth: TestData.Today.AddDays(1).ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badGender.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noDob.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, futureDob.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndRoles()
    {
        var email = ApiClient.UniqueEmail();
        await _client.RegisterAsync(ApiClient.RegistrationBody(email, "Organizer"));

        var response = await _client.LoginAsync(email, ApiClient.ValidPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.ReadAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.True(tokens.ExpiresIn > 0);
        Assert.Equal("Bearer", tokens.TokenType);
        Assert.Contains("Organizer", tokens.User.Roles);
    }

    [Fact]
    public async Task Login_CreatesServerSideSession()
    {
        var email = ApiClient.UniqueEmail();
        await _client.RegisterAsync(ApiClient.RegistrationBody(email, "Participant"));

        await _client.LoginOrFailAsync(email, ApiClient.ValidPassword);

        var sessions = await factory.WithDbAsync(db =>
            db.UserSessions.CountAsync(s => s.User.Email == email && s.RevokedAt == null));
        Assert.Equal(1, sessions);
    }

    [Fact]
    public async Task Login_SeededAccount_WorksWithDocumentedDemoPassword()
    {
        var tokens = await _client.LoginOrFailAsync(ApiClient.SeedOrganizerEmail, RaceDay.Api.Data.Seed.SeedData.DemoPassword);

        Assert.Contains("Organizer", tokens.User.Roles);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = ApiClient.UniqueEmail();
        await _client.RegisterAsync(ApiClient.RegistrationBody(email, "Participant"));

        var response = await _client.LoginAsync(email, "WrongPassw0rd");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _client.LoginAsync(ApiClient.UniqueEmail("nobody"), ApiClient.ValidPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ForDeactivatedAccount_Returns401()
    {
        var email = ApiClient.UniqueEmail();
        await _client.RegisterAsync(ApiClient.RegistrationBody(email, "Participant"));
        await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Email == email);
            user.IsActive = false;
            await db.SaveChangesAsync();
        });

        var response = await _client.LoginAsync(email, ApiClient.ValidPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RotatesToken_AndOldRefreshTokenStopsWorking()
    {
        var (_, tokens) = await factory.NewUserClientAsync("Participant");

        var refreshed = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var newTokens = await refreshed.ReadAsync<TokenResponse>();
        Assert.NotEqual(tokens.RefreshToken, newTokens.RefreshToken);

        var reused = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithUnknownToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesSession_SoAccessAndRefreshTokensAreRejected()
    {
        var (client, tokens) = await factory.NewUserClientAsync("Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
