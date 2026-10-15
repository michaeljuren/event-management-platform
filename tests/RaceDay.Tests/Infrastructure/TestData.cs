using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests.Infrastructure;

/// <summary>Request bodies and arranged data shared across test classes.</summary>
public static class TestData
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>A valid event 30 days out at seeded venue 1, with registration open now.</summary>
    public static object EventBody(string? name = null, string discipline = "run", int venueId = 1) => new
    {
        venueId,
        eventName = name ?? $"Test Event {Guid.NewGuid():N}"[..30],
        description = "Integration test event",
        discipline,
        eventDate = Today.AddDays(30).ToString("yyyy-MM-dd"),
        startTime = "06:30:00",
        registrationOpenDate = DateTime.UtcNow.AddDays(-1).ToString("O"),
        registrationCloseDate = DateTime.UtcNow.AddDays(20).ToString("O"),
        bannerImageUrl = (string?)null
    };

    public static object CategoryBody(string name = "10km", decimal distanceKm = 10m, int? minAge = null,
        string genderRestriction = "Open", int? maxParticipants = null, decimal entryFee = 100m) => new
    {
        routeId = (int?)null,
        categoryName = name,
        distanceKm,
        minAge,
        genderRestriction,
        entryFee,
        maxParticipants
    };

    /// <summary>Creates, adds a category to and publishes an event as the given organizer. Returns (eventId, categoryId).</summary>
    public static async Task<(int EventId, int CategoryId)> CreatePublishedEventAsync(
        HttpClient organizer, object? category = null)
    {
        var created = await organizer.PostAsJsonAsync("/api/events", EventBody());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var evt = await created.ReadAsync<EventDetailResponse>();

        var cat = await organizer.PostAsJsonAsync($"/api/events/{evt.EventId}/categories", category ?? CategoryBody());
        Assert.Equal(HttpStatusCode.Created, cat.StatusCode);
        var categoryResponse = await cat.ReadAsync<CategoryResponse>();

        var publish = await organizer.PostAsync($"/api/events/{evt.EventId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        return (evt.EventId, categoryResponse.CategoryId);
    }

    /// <summary>
    /// Inserts a published event that took place yesterday, owned by <paramref name="organizerId"/>,
    /// with one category and an entry for <paramref name="participantId"/>. Returns the entry id.
    /// </summary>
    public static Task<int> SeedPastEventWithEntryAsync(RaceDayApiFactory factory, int organizerId, int participantId) =>
        factory.WithDbAsync(async db =>
        {
            var evt = new Event
            {
                OrganizerId = organizerId,
                VenueId = 1,
                EventName = $"Past Event {Guid.NewGuid():N}"[..30],
                Discipline = Discipline.Running,
                EventDate = Today.AddDays(-1),
                StartTime = new TimeOnly(6, 0),
                RegistrationOpenDate = DateTime.UtcNow.AddDays(-30),
                RegistrationCloseDate = DateTime.UtcNow.AddDays(-3),
                Status = EventStatus.Published
            };
            var category = new Category { Event = evt, CategoryName = "10km", DistanceKm = 10m };
            var entry = new Entry { Category = category, ParticipantId = participantId, Status = EntryStatus.Confirmed };
            db.Entries.Add(entry);
            await db.SaveChangesAsync();
            return entry.EntryId;
        });
}
