using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

/// <summary>Participants entering events by category, and organizers viewing their rosters.</summary>
public class EnrolmentTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Participant_CanEnterEvent_AndEntryLinksParticipantEventAndCategory()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (participant, tokens) = await factory.NewUserClientAsync("Participant");
        var (eventId, categoryId) = await TestData.CreatePublishedEventAsync(organizer);

        var response = await participant.PostAsync($"/api/categories/{categoryId}/entries", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var entry = await response.ReadAsync<EntryResponse>();
        Assert.Equal(tokens.User.UserId, entry.ParticipantId);
        Assert.Equal(categoryId, entry.CategoryId);
        Assert.Equal(eventId, entry.EventId);
        Assert.Equal(EntryStatus.Pending, entry.Status);

        // The link is recorded in the database, not just echoed back.
        var stored = await factory.WithDbAsync(db => db.Entries
            .Include(e => e.Category)
            .SingleAsync(e => e.EntryId == entry.EntryId));
        Assert.Equal(tokens.User.UserId, stored.ParticipantId);
        Assert.Equal(categoryId, stored.CategoryId);
        Assert.Equal(eventId, stored.Category.EventId);
    }

    [Fact]
    public async Task Participant_SeesNewEntryInOwnEntries()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (participant, _) = await factory.NewUserClientAsync("Participant");
        var (_, categoryId) = await TestData.CreatePublishedEventAsync(organizer);
        await participant.PostAsync($"/api/categories/{categoryId}/entries", null);

        var mine = await (await participant.GetAsync("/api/users/me/entries")).ReadAsync<List<EntryResponse>>();

        Assert.Contains(mine, e => e.CategoryId == categoryId);
    }

    [Fact]
    public async Task Organizer_CanViewAllEntriesForTheirEvent()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (first, firstTokens) = await factory.NewUserClientAsync("Participant");
        var (second, secondTokens) = await factory.NewUserClientAsync("Participant");
        var (eventId, categoryId) = await TestData.CreatePublishedEventAsync(organizer);
        await first.PostAsync($"/api/categories/{categoryId}/entries", null);
        await second.PostAsync($"/api/categories/{categoryId}/entries", null);

        var response = await organizer.GetAsync($"/api/events/{eventId}/entries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roster = await response.ReadAsync<PagedResult<RosterEntryResponse>>();
        Assert.Equal(2, roster.TotalCount);
        Assert.Contains(roster.Items, r => r.ParticipantId == firstTokens.User.UserId);
        Assert.Contains(roster.Items, r => r.ParticipantId == secondTokens.User.UserId);
    }

    [Fact]
    public async Task EnteringTheSameEventTwice_Returns409()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (participant, _) = await factory.NewUserClientAsync("Participant");
        var (eventId, categoryId) = await TestData.CreatePublishedEventAsync(organizer);
        var other = await (await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories", TestData.CategoryBody("21km", 21m)))
            .ReadAsync<CategoryResponse>();
        await participant.PostAsync($"/api/categories/{categoryId}/entries", null);

        var sameCategory = await participant.PostAsync($"/api/categories/{categoryId}/entries", null);
        var otherCategory = await participant.PostAsync($"/api/categories/{other.CategoryId}/entries", null);

        Assert.Equal(HttpStatusCode.Conflict, sameCategory.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, otherCategory.StatusCode);
    }

    [Fact]
    public async Task EnteringAFullCategory_Returns409()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (first, _) = await factory.NewUserClientAsync("Participant");
        var (second, _) = await factory.NewUserClientAsync("Participant");
        var (_, categoryId) = await TestData.CreatePublishedEventAsync(organizer, TestData.CategoryBody(maxParticipants: 1));

        var accepted = await first.PostAsync($"/api/categories/{categoryId}/entries", null);
        var rejected = await second.PostAsync($"/api/categories/{categoryId}/entries", null);

        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
    }

    [Fact]
    public async Task Participant_NotMeetingAgeOrGenderRules_Returns400()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (youngMale, _) = await factory.NewUserClientAsync("Participant", gender: "Male",
            dateOfBirth: TestData.Today.AddYears(-15).ToString("yyyy-MM-dd"));
        var (eventId, seniorsId) = await TestData.CreatePublishedEventAsync(organizer, TestData.CategoryBody("Senior", minAge: 20));
        var womens = await (await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories",
            TestData.CategoryBody("Women's 10km", genderRestriction: "Female"))).ReadAsync<CategoryResponse>();

        var tooYoung = await youngMale.PostAsync($"/api/categories/{seniorsId}/entries", null);
        var wrongGender = await youngMale.PostAsync($"/api/categories/{womens.CategoryId}/entries", null);

        Assert.Equal(HttpStatusCode.BadRequest, tooYoung.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrongGender.StatusCode);
    }

    [Fact]
    public async Task EnteringWhenRegistrationIsClosed_Returns400()
    {
        var (participant, tokens) = await factory.NewUserClientAsync("Participant");
        var organizerId = await factory.WithDbAsync(db => db.Users.Where(u => u.Email == ApiClient.SeedOrganizerEmail).Select(u => u.UserId).SingleAsync());
        var entryId = await TestData.SeedPastEventWithEntryAsync(factory, organizerId, participantId: 5);
        var categoryId = await factory.WithDbAsync(db => db.Entries.Where(e => e.EntryId == entryId).Select(e => e.CategoryId).SingleAsync());

        var response = await participant.PostAsync($"/api/categories/{categoryId}/entries", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await factory.WithDbAsync(db => db.Entries.AnyAsync(e => e.ParticipantId == tokens.User.UserId)));
    }

    [Fact]
    public async Task EnteringDraftOrMissingCategory_Returns404()
    {
        var (participant, _) = await factory.NewUserClientAsync("Participant");

        var draftCategory = await participant.PostAsync("/api/categories/4/entries", null); // seeded draft event 3
        var missing = await participant.PostAsync("/api/categories/99999/entries", null);

        Assert.Equal(HttpStatusCode.NotFound, draftCategory.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEntry_ButNotTwice()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (participant, _) = await factory.NewUserClientAsync("Participant");
        var (_, categoryId) = await TestData.CreatePublishedEventAsync(organizer);
        var entry = await (await participant.PostAsync($"/api/categories/{categoryId}/entries", null)).ReadAsync<EntryResponse>();

        var cancel = await participant.PutAsync($"/api/entries/{entry.EntryId}/cancel", null);
        var again = await participant.PutAsync($"/api/entries/{entry.EntryId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(EntryStatus.Withdrawn, (await cancel.ReadAsync<EntryResponse>()).Status);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEntry()
    {
        var (participant, _) = await factory.NewUserClientAsync("Participant");

        var response = await participant.PutAsync("/api/entries/1/cancel", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanAssignUniqueBibNumbers()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (first, _) = await factory.NewUserClientAsync("Participant");
        var (second, _) = await factory.NewUserClientAsync("Participant");
        var (_, categoryId) = await TestData.CreatePublishedEventAsync(organizer);
        var firstEntry = await (await first.PostAsync($"/api/categories/{categoryId}/entries", null)).ReadAsync<EntryResponse>();
        var secondEntry = await (await second.PostAsync($"/api/categories/{categoryId}/entries", null)).ReadAsync<EntryResponse>();

        var assigned = await organizer.PutAsJsonAsync($"/api/entries/{firstEntry.EntryId}/bib", new { bibNumber = "101" });
        var duplicate = await organizer.PutAsJsonAsync($"/api/entries/{secondEntry.EntryId}/bib", new { bibNumber = "101" });

        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        Assert.Equal("101", (await assigned.ReadAsync<EntryResponse>()).BibNumber);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }
}
