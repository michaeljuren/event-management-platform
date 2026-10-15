using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

/// <summary>Organizers defining age and distance categories; both roles viewing them.</summary>
public class CategoryTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private async Task<(HttpClient Organizer, int EventId)> NewEventAsync()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var evt = await (await organizer.PostAsJsonAsync("/api/events", TestData.EventBody())).ReadAsync<EventDetailResponse>();
        return (organizer, evt.EventId);
    }

    [Fact]
    public async Task Organizer_CanDefineAgeAndDistanceCategories()
    {
        var (organizer, eventId) = await NewEventAsync();

        var under20 = await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody("Under 20", 10m, minAge: 14));
        var senior = await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody("Senior", 21.1m, minAge: 20));
        var tenK = await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody("10km", 10m, maxParticipants: 500));

        Assert.Equal(HttpStatusCode.Created, under20.StatusCode);
        Assert.Equal(HttpStatusCode.Created, senior.StatusCode);
        Assert.Equal(HttpStatusCode.Created, tenK.StatusCode);
        var category = await tenK.ReadAsync<CategoryResponse>();
        Assert.Equal(eventId, category.EventId);
        Assert.Equal(500, category.SpotsRemaining);
        Assert.Equal(Gender.Open, category.GenderRestriction);
    }

    [Fact]
    public async Task BothRoles_CanViewCategories()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (participant, _) = await factory.NewUserClientAsync("Participant");
        var (eventId, categoryId) = await TestData.CreatePublishedEventAsync(organizer);

        foreach (var client in new[] { organizer, participant })
        {
            var list = await client.GetAsync($"/api/events/{eventId}/categories");
            var single = await client.GetAsync($"/api/categories/{categoryId}");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Single(await list.ReadAsync<List<CategoryResponse>>());
            Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        }
    }

    [Fact]
    public async Task DuplicateCategoryName_Returns409()
    {
        var (organizer, eventId) = await NewEventAsync();
        await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody("Senior"));

        var duplicate = await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody("Senior"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task InvalidCategory_Returns400()
    {
        var (organizer, eventId) = await NewEventAsync();

        var zeroDistance = await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody(distanceKm: 0m));
        var badGender = await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody(genderRestriction: "Mixed"));
        var routeAtOtherVenue = await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            routeId = 3, // seeded route at Zwartkops, event is at Green Point
            categoryName = "Wrong Route",
            distanceKm = 40m,
            entryFee = 0m
        });

        Assert.Equal(HttpStatusCode.BadRequest, zeroDistance.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badGender.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, routeAtOtherVenue.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanUpdateAndDeleteCategory_WithoutEntries()
    {
        var (organizer, eventId) = await NewEventAsync();
        var created = await (await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody("5km", 5m)))
            .ReadAsync<CategoryResponse>();

        var update = await organizer.PutAsJsonAsync($"/api/categories/{created.CategoryId}", TestData.CategoryBody("5km Fun Run", 5m, entryFee: 50m));
        var delete = await organizer.DeleteAsync($"/api/categories/{created.CategoryId}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("5km Fun Run", (await update.ReadAsync<CategoryResponse>()).CategoryName);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task DeletingCategoryWithEntries_Returns409()
    {
        // Seeded category 2 (event 1, Sarah) has an entry.
        var organizer = await factory.SeedUserClientAsync(ApiClient.SeedOrganizerEmail);

        var response = await organizer.DeleteAsync("/api/categories/2");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CategoryForMissingEvent_Returns404()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");

        var response = await organizer.PostAsJsonAsync("/api/events/99999/categories", TestData.CategoryBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
