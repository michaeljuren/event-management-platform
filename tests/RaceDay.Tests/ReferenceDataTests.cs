using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

/// <summary>Clubs, venues, routes and admin role management.</summary>
public class ReferenceDataTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private static object VenueBody(string province = "Gauteng") => new
    {
        venueName = $"Venue {Guid.NewGuid():N}"[..20],
        addressLine = "1 Test Road",
        city = "Johannesburg",
        province,
        postalCode = "2000",
        latitude = -26.2m,
        longitude = 28.04m
    };

    [Fact]
    public async Task ClubList_CanBeFilteredByProvince()
    {
        var page = await (await factory.CreateClient().GetAsync("/api/clubs?province=Gauteng"))
            .ReadAsync<PagedResult<ClubResponse>>();

        var club = Assert.Single(page.Items);
        Assert.Equal("Randburg Harriers", club.ClubName);
    }

    [Fact]
    public async Task Admin_CanManageClubs_AndDuplicateNameReturns409()
    {
        var admin = await factory.SeedUserClientAsync(ApiClient.SeedAdminEmail);
        var body = new { clubName = $"Club {Guid.NewGuid():N}"[..20], province = "Free State" };

        var created = await admin.PostAsJsonAsync("/api/clubs", body);
        var duplicate = await admin.PostAsJsonAsync("/api/clubs", body);
        var club = await created.ReadAsync<ClubResponse>();
        var deleted = await admin.DeleteAsync($"/api/clubs/{club.ClubId}");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanCreateVenueAndRoute()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");

        var venueResponse = await organizer.PostAsJsonAsync("/api/venues", VenueBody());
        var venue = await venueResponse.ReadAsync<VenueResponse>();
        var routeResponse = await organizer.PostAsJsonAsync($"/api/venues/{venue.VenueId}/routes", new
        {
            routeName = "City Loop",
            distanceKm = 15m,
            elevationGainM = 120
        });

        Assert.Equal(HttpStatusCode.Created, venueResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, routeResponse.StatusCode);
        var routes = await (await organizer.GetAsync($"/api/venues/{venue.VenueId}/routes")).ReadAsync<List<RouteResponse>>();
        Assert.Single(routes);
    }

    [Fact]
    public async Task Venue_WithInvalidProvince_Returns400()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");

        var response = await organizer.PostAsJsonAsync("/api/venues", VenueBody("Atlantis"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeletingVenueUsedByEvents_Returns409()
    {
        var admin = await factory.SeedUserClientAsync(ApiClient.SeedAdminEmail);

        var response = await admin.DeleteAsync("/api/venues/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotEditRouteUsedByAnotherOrganizersEvent()
    {
        // Route 1 is used by event 1, which belongs to Sarah.
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");

        var response = await organizer.PutAsJsonAsync("/api/routes/1", new { routeName = "Hijacked", distanceKm = 1m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanGrantRole_AndUnknownRoleReturns400()
    {
        var admin = await factory.SeedUserClientAsync(ApiClient.SeedAdminEmail);
        var (_, tokens) = await factory.NewUserClientAsync("Participant");

        var granted = await admin.PostAsJsonAsync($"/api/users/{tokens.User.UserId}/roles", new { roleName = "Organizer" });
        var unknown = await admin.PostAsJsonAsync($"/api/users/{tokens.User.UserId}/roles", new { roleName = "Wizard" });
        var missingUser = await admin.PostAsJsonAsync("/api/users/99999/roles", new { roleName = "Organizer" });

        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        Assert.Equal(new[] { "Organizer", "Participant" }, await granted.ReadAsync<List<string>>());
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingUser.StatusCode);
    }
}
