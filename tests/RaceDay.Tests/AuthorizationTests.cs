using System.Net;
using System.Net.Http.Json;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

/// <summary>
/// Authenticated vs unauthenticated access, and role-based access enforced across the API:
/// organizer functionality is closed to participants and vice versa, admin functionality is closed
/// to both, and organizers can only manage their own events.
/// </summary>
public class AuthorizationTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private readonly HttpClient _anonymous = factory.CreateClient();

    public static TheoryData<string, string> ProtectedEndpoints => new()
    {
        { "GET", "/api/users/me" },
        { "PUT", "/api/users/me" },
        { "GET", "/api/users/me/performance-history" },
        { "GET", "/api/users/me/entries" },
        { "GET", "/api/users/1" },
        { "POST", "/api/users/2/roles" },
        { "POST", "/api/events" },
        { "PUT", "/api/events/1" },
        { "DELETE", "/api/events/1" },
        { "POST", "/api/events/1/publish" },
        { "POST", "/api/events/1/categories" },
        { "PUT", "/api/categories/1" },
        { "DELETE", "/api/categories/1" },
        { "POST", "/api/categories/1/entries" },
        { "GET", "/api/events/1/entries" },
        { "GET", "/api/entries/1" },
        { "PUT", "/api/entries/1/cancel" },
        { "PUT", "/api/entries/1/bib" },
        { "POST", "/api/entries/1/result" },
        { "PUT", "/api/results/1" },
        { "DELETE", "/api/results/1" },
        { "POST", "/api/clubs" },
        { "DELETE", "/api/clubs/1" },
        { "POST", "/api/venues" },
        { "DELETE", "/api/venues/1" },
        { "POST", "/api/venues/1/routes" },
        { "DELETE", "/api/routes/1" },
        { "POST", "/api/auth/logout" }
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutToken_Returns401(string method, string url)
    {
        var response = await _anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedToken_Returns401()
    {
        var (client, tokens) = await factory.NewUserClientAsync("Participant");
        client.UseToken(tokens.AccessToken[..^4] + "abcd");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/events/1")]
    [InlineData("/api/events/1/categories")]
    [InlineData("/api/categories/1")]
    [InlineData("/api/events/1/results")]
    [InlineData("/api/categories/2/results")]
    [InlineData("/api/results/1")]
    [InlineData("/api/clubs")]
    [InlineData("/api/venues")]
    [InlineData("/api/venues/1/routes")]
    [InlineData("/api/routes/1")]
    public async Task PublicEndpoint_WithoutToken_Returns200(string url)
    {
        var response = await _anonymous.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DraftEvent_IsHiddenFromAnonymousUsers()
    {
        // Seeded event 3 is still a Draft.
        var response = await _anonymous.GetAsync("/api/events/3");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public static TheoryData<string, string> OrganizerOnlyEndpoints => new()
    {
        { "POST", "/api/events" },
        { "PUT", "/api/events/1" },
        { "DELETE", "/api/events/1" },
        { "POST", "/api/events/1/publish" },
        { "POST", "/api/events/1/categories" },
        { "PUT", "/api/categories/1" },
        { "DELETE", "/api/categories/1" },
        { "GET", "/api/events/1/entries" },
        { "PUT", "/api/entries/1/bib" },
        { "POST", "/api/entries/1/result" },
        { "PUT", "/api/results/1" },
        { "DELETE", "/api/results/1" },
        { "POST", "/api/venues" },
        { "POST", "/api/venues/1/routes" }
    };

    [Theory]
    [MemberData(nameof(OrganizerOnlyEndpoints))]
    public async Task OrganizerEndpoint_AsParticipant_Returns403(string method, string url)
    {
        var (participant, _) = await factory.NewUserClientAsync("Participant");

        var response = await participant.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/categories/1/entries")]
    [InlineData("GET", "/api/users/me/entries")]
    [InlineData("PUT", "/api/entries/1/cancel")]
    [InlineData("GET", "/api/users/me/performance-history")]
    public async Task ParticipantEndpoint_AsOrganizer_Returns403(string method, string url)
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");

        var response = await organizer.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Organizer", "GET", "/api/users/1")]
    [InlineData("Participant", "GET", "/api/users/1")]
    [InlineData("Organizer", "POST", "/api/users/4/roles")]
    [InlineData("Participant", "POST", "/api/users/4/roles")]
    [InlineData("Organizer", "POST", "/api/clubs")]
    [InlineData("Organizer", "DELETE", "/api/clubs/1")]
    [InlineData("Organizer", "DELETE", "/api/venues/1")]
    public async Task AdminEndpoint_AsNonAdmin_Returns403(string role, string method, string url)
    {
        var (client, _) = await factory.NewUserClientAsync(role);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { roleName = "Admin", clubName = "X", province = "Gauteng" }) : null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotGrantThemselvesAdmin()
    {
        var (participant, tokens) = await factory.NewUserClientAsync("Participant");

        var response = await participant.PostAsJsonAsync($"/api/users/{tokens.User.UserId}/roles", new { roleName = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotManageAnotherOrganizersEvent()
    {
        // Event 1 belongs to seeded organizer Sarah; Thabo is a different organizer.
        var otherOrganizer = await factory.SeedUserClientAsync(ApiClient.SeedOtherOrganizerEmail);

        var update = await otherOrganizer.PutAsJsonAsync("/api/events/1", TestData.EventBody());
        var delete = await otherOrganizer.DeleteAsync("/api/events/1");
        var addCategory = await otherOrganizer.PostAsJsonAsync("/api/events/1/categories", TestData.CategoryBody("Hijack"));
        var roster = await otherOrganizer.GetAsync("/api/events/1/entries");
        var result = await otherOrganizer.PostAsJsonAsync("/api/entries/2/result", new { finishTime = "00:40:00" });

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, addCategory.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, roster.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewSomeoneElsesEntry()
    {
        var (participant, _) = await factory.NewUserClientAsync("Participant");

        // Entry 1 belongs to seeded participant Lindiwe.
        var view = await participant.GetAsync("/api/entries/1");

        Assert.Equal(HttpStatusCode.Forbidden, view.StatusCode);
    }

    [Fact]
    public async Task Admin_CanAccessAdminEndpoints()
    {
        var admin = await factory.SeedUserClientAsync(ApiClient.SeedAdminEmail);

        var user = await admin.GetAsync("/api/users/4");
        var roster = await admin.GetAsync("/api/events/1/entries");

        Assert.Equal(HttpStatusCode.OK, user.StatusCode);
        Assert.Equal(HttpStatusCode.OK, roster.StatusCode);
    }
}
