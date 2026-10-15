using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

/// <summary>Organizers creating, updating, publishing and deleting events; everyone viewing them.</summary>
public class EventManagementTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Theory]
    [InlineData("run", "Running")]
    [InlineData("walk", "Walking")]
    [InlineData("cycle", "Cycling")]
    public async Task Organizer_CanCreateEvent_WithAllRequiredDetails(string discipline, string storedDiscipline)
    {
        var (organizer, tokens) = await factory.NewUserClientAsync("Organizer");

        var response = await organizer.PostAsJsonAsync("/api/events", TestData.EventBody("Spring Classic", discipline));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var evt = await response.ReadAsync<EventDetailResponse>();
        Assert.Equal("Spring Classic", evt.EventName);
        Assert.Equal(storedDiscipline, evt.Discipline);
        Assert.Equal(EventStatus.Draft, evt.Status);
        Assert.Equal(tokens.User.UserId, evt.OrganizerId);
        Assert.Equal("Green Point Park", evt.Venue.VenueName); // location
        Assert.Equal(TestData.Today.AddDays(30), evt.EventDate);
    }

    [Fact]
    public async Task Organizer_CanUpdateOwnEvent()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var created = await (await organizer.PostAsJsonAsync("/api/events", TestData.EventBody())).ReadAsync<EventDetailResponse>();

        var response = await organizer.PutAsJsonAsync($"/api/events/{created.EventId}", TestData.EventBody("Renamed Event", "cycle", venueId: 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<EventDetailResponse>();
        Assert.Equal("Renamed Event", updated.EventName);
        Assert.Equal("Cycling", updated.Discipline);
        Assert.Equal(2, updated.Venue.VenueId);
    }

    [Fact]
    public async Task Organizer_CanPublishEvent_AfterAddingACategory()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var created = await (await organizer.PostAsJsonAsync("/api/events", TestData.EventBody())).ReadAsync<EventDetailResponse>();

        var tooEarly = await organizer.PostAsync($"/api/events/{created.EventId}/publish", null);
        await organizer.PostAsJsonAsync($"/api/events/{created.EventId}/categories", TestData.CategoryBody());
        var published = await organizer.PostAsync($"/api/events/{created.EventId}/publish", null);
        var again = await organizer.PostAsync($"/api/events/{created.EventId}/publish", null);

        Assert.Equal(HttpStatusCode.BadRequest, tooEarly.StatusCode);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        Assert.Equal(EventStatus.Published, (await published.ReadAsync<EventDetailResponse>()).Status);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanDeleteOwnEvent_WithoutEntries()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (eventId, _) = await TestData.CreatePublishedEventAsync(organizer);

        var response = await organizer.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organizer.GetAsync($"/api/events/{eventId}")).StatusCode);
        Assert.False(await factory.WithDbAsync(db => db.Categories.AnyAsync(c => c.EventId == eventId)));
    }

    [Fact]
    public async Task Deleting_EventWithEntries_CancelsItInstead()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (participant, _) = await factory.NewUserClientAsync("Participant");
        var (eventId, categoryId) = await TestData.CreatePublishedEventAsync(organizer);
        await participant.PostAsync($"/api/categories/{categoryId}/entries", null);

        var response = await organizer.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var evt = await (await organizer.GetAsync($"/api/events/{eventId}")).ReadAsync<EventDetailResponse>();
        Assert.Equal(EventStatus.Cancelled, evt.Status);
    }

    [Fact]
    public async Task Deleting_EventWithResults_Returns409()
    {
        // Seeded event 1 (Sarah) has recorded results.
        var organizer = await factory.SeedUserClientAsync(ApiClient.SeedOrganizerEmail);

        var response = await organizer.DeleteAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidData_Returns400()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");

        var badDiscipline = await organizer.PostAsJsonAsync("/api/events", TestData.EventBody(discipline: "swim"));
        var unknownVenue = await organizer.PostAsJsonAsync("/api/events", TestData.EventBody(venueId: 9999));
        var missingName = await organizer.PostAsJsonAsync("/api/events", new { venueId = 1, discipline = "run" });
        var regClosesBeforeOpening = await organizer.PostAsJsonAsync("/api/events", new
        {
            venueId = 1,
            eventName = "Backwards",
            discipline = "run",
            eventDate = TestData.Today.AddDays(30).ToString("yyyy-MM-dd"),
            startTime = "07:00:00",
            registrationOpenDate = DateTime.UtcNow.AddDays(10).ToString("O"),
            registrationCloseDate = DateTime.UtcNow.AddDays(5).ToString("O")
        });
        var pastEvent = await organizer.PostAsJsonAsync("/api/events", new
        {
            venueId = 1,
            eventName = "Yesterday",
            discipline = "run",
            eventDate = TestData.Today.AddDays(-1).ToString("yyyy-MM-dd"),
            startTime = "07:00:00",
            registrationOpenDate = DateTime.UtcNow.AddDays(-10).ToString("O"),
            registrationCloseDate = DateTime.UtcNow.AddDays(-2).ToString("O")
        });

        Assert.Equal(HttpStatusCode.BadRequest, badDiscipline.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownVenue.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, regClosesBeforeOpening.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, pastEvent.StatusCode);
    }

    [Fact]
    public async Task UpdateOrDeleteMissingEvent_Returns404()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");

        var update = await organizer.PutAsJsonAsync("/api/events/99999", TestData.EventBody());
        var delete = await organizer.DeleteAsync("/api/events/99999");
        var get = await organizer.GetAsync("/api/events/99999");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotCreateEvents_ButCanManageAnyEvent()
    {
        var admin = await factory.SeedUserClientAsync(ApiClient.SeedAdminEmail);
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var created = await (await organizer.PostAsJsonAsync("/api/events", TestData.EventBody())).ReadAsync<EventDetailResponse>();

        var create = await admin.PostAsJsonAsync("/api/events", TestData.EventBody());
        var update = await admin.PutAsJsonAsync($"/api/events/{created.EventId}", TestData.EventBody("Admin Edit"));

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanViewPublishedEvents_ButOnlyOwnerSeesDrafts()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (participant, _) = await factory.NewUserClientAsync("Participant");
        var (publishedId, _) = await TestData.CreatePublishedEventAsync(organizer);
        var draft = await (await organizer.PostAsJsonAsync("/api/events", TestData.EventBody())).ReadAsync<EventDetailResponse>();

        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync($"/api/events/{publishedId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await organizer.GetAsync($"/api/events/{publishedId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync($"/api/events/{draft.EventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await organizer.GetAsync($"/api/events/{draft.EventId}")).StatusCode);

        var list = await (await participant.GetAsync("/api/events?pageSize=100")).ReadAsync<PagedResult<EventSummaryResponse>>();
        Assert.Contains(list.Items, e => e.EventId == publishedId);
        Assert.DoesNotContain(list.Items, e => e.EventId == draft.EventId);
        Assert.All(list.Items, e => Assert.NotEqual(EventStatus.Draft, e.Status));
    }

    [Fact]
    public async Task EventList_CanBeFilteredByDiscipline()
    {
        var response = await factory.CreateClient().GetAsync("/api/events?discipline=cycle");

        var list = await response.ReadAsync<PagedResult<EventSummaryResponse>>();
        Assert.NotEmpty(list.Items);
        Assert.All(list.Items, e => Assert.Equal("Cycling", e.Discipline));
    }

    [Fact]
    public async Task EventDetail_IncludesCategoriesAndDistances()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/1");

        var evt = await response.ReadAsync<EventDetailResponse>();
        Assert.Equal(2, evt.Categories.Count);
        Assert.Equal(new[] { 10.00m, 21.10m }, evt.DistancesKm);
    }
}
