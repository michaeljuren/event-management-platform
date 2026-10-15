using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

/// <summary>Organizers capturing results after an event; participants viewing their own results.</summary>
public class ResultsTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private async Task<(HttpClient Organizer, HttpClient Participant, int ParticipantId, int EntryId)> PastEntryAsync()
    {
        var (organizer, organizerTokens) = await factory.NewUserClientAsync("Organizer");
        var (participant, participantTokens) = await factory.NewUserClientAsync("Participant");
        var entryId = await TestData.SeedPastEventWithEntryAsync(factory, organizerTokens.User.UserId, participantTokens.User.UserId);
        return (organizer, participant, participantTokens.User.UserId, entryId);
    }

    private static object ResultBody(string finishTime = "00:45:12", int overall = 3, string finishStatus = "Finished") => new
    {
        finishTime,
        overallPosition = overall,
        categoryPosition = 1,
        genderPosition = 1,
        finishStatus
    };

    [Fact]
    public async Task Organizer_CanCaptureFinishTimeAndPositions()
    {
        var (organizer, _, _, entryId) = await PastEntryAsync();

        var response = await organizer.PostAsJsonAsync($"/api/entries/{entryId}/result", ResultBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.ReadAsync<ResultResponse>();
        Assert.Equal(new TimeOnly(0, 45, 12), result.FinishTime);
        Assert.Equal(3, result.OverallPosition);
        Assert.Equal(FinishStatus.Finished, result.FinishStatus);
        Assert.True(await factory.WithDbAsync(db => db.Results.AnyAsync(r => r.EntryId == entryId)));
    }

    [Fact]
    public async Task Participant_CanViewOwnResults()
    {
        var (organizer, participant, _, entryId) = await PastEntryAsync();
        await organizer.PostAsJsonAsync($"/api/entries/{entryId}/result", ResultBody("01:02:03", 7));

        var history = await participant.GetAsync("/api/users/me/performance-history");
        var entries = await (await participant.GetAsync("/api/users/me/entries")).ReadAsync<List<EntryResponse>>();

        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        var item = Assert.Single(await history.ReadAsync<List<PerformanceHistoryItem>>());
        Assert.Equal(new TimeOnly(1, 2, 3), item.FinishTime);
        Assert.Equal(7, item.OverallPosition);
        Assert.NotNull(Assert.Single(entries).ResultId);
    }

    [Fact]
    public async Task CapturingASecondResult_Returns409()
    {
        var (organizer, _, _, entryId) = await PastEntryAsync();
        await organizer.PostAsJsonAsync($"/api/entries/{entryId}/result", ResultBody());

        var duplicate = await organizer.PostAsJsonAsync($"/api/entries/{entryId}/result", ResultBody());

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task InvalidResult_Returns400()
    {
        var (organizer, _, _, entryId) = await PastEntryAsync();

        var noTime = await organizer.PostAsJsonAsync($"/api/entries/{entryId}/result", new { finishStatus = "Finished" });
        var badStatus = await organizer.PostAsJsonAsync($"/api/entries/{entryId}/result", ResultBody(finishStatus: "Lost"));
        var zeroPosition = await organizer.PostAsJsonAsync($"/api/entries/{entryId}/result", ResultBody(overall: 0));

        Assert.Equal(HttpStatusCode.BadRequest, noTime.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badStatus.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zeroPosition.StatusCode);
    }

    [Fact]
    public async Task CapturingResultBeforeTheEvent_Returns400()
    {
        var (organizer, _) = await factory.NewUserClientAsync("Organizer");
        var (participant, _) = await factory.NewUserClientAsync("Participant");
        var (_, categoryId) = await TestData.CreatePublishedEventAsync(organizer);
        var entry = await (await participant.PostAsync($"/api/categories/{categoryId}/entries", null)).ReadAsync<EntryResponse>();

        var response = await organizer.PostAsJsonAsync($"/api/entries/{entry.EntryId}/result", ResultBody());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanCorrectAndRemoveResult()
    {
        var (organizer, _, _, entryId) = await PastEntryAsync();
        var created = await (await organizer.PostAsJsonAsync($"/api/entries/{entryId}/result", ResultBody())).ReadAsync<ResultResponse>();

        var corrected = await organizer.PutAsJsonAsync($"/api/results/{created.ResultId}", ResultBody("00:44:59", 2));
        var removed = await organizer.DeleteAsync($"/api/results/{created.ResultId}");

        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        Assert.Equal(2, (await corrected.ReadAsync<ResultResponse>()).OverallPosition);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResults()
    {
        var (_, participant, _, entryId) = await PastEntryAsync();

        var response = await participant.PostAsJsonAsync($"/api/entries/{entryId}/result", ResultBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EventLeaderboard_IsPublicAndOrderedByPosition()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/1/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.ReadAsync<PagedResult<ResultResponse>>();
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(new[] { 15, 22 }, page.Items.Select(r => r.OverallPosition!.Value));
    }
}
