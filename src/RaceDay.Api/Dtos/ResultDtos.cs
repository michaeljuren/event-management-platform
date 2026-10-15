using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class ResultRequest
{
    /// <summary>Official finish time, e.g. "01:52:34" or "00:48:10.250". Required when FinishStatus is Finished.</summary>
    public TimeOnly? FinishTime { get; set; }

    public TimeOnly? GunTime { get; set; }

    public TimeOnly? ChipTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? OverallPosition { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryPosition { get; set; }

    [Range(1, int.MaxValue)]
    public int? GenderPosition { get; set; }

    /// <summary>Finished, DNF, DNS or DSQ. Defaults to Finished.</summary>
    public string? FinishStatus { get; set; }
}

public record ResultResponse(
    int ResultId,
    int EntryId,
    int EventId,
    string EventName,
    int CategoryId,
    string CategoryName,
    int ParticipantId,
    string ParticipantName,
    string? BibNumber,
    TimeOnly? FinishTime,
    TimeOnly? GunTime,
    TimeOnly? ChipTime,
    int? OverallPosition,
    int? CategoryPosition,
    int? GenderPosition,
    string FinishStatus,
    DateTime RecordedAt)
{
    /// <summary>Requires Entry.Category.Event and Entry.Participant to be loaded.</summary>
    public static ResultResponse From(Result r) => new(
        r.ResultId, r.EntryId, r.Entry.Category.EventId, r.Entry.Category.Event.EventName,
        r.Entry.CategoryId, r.Entry.Category.CategoryName,
        r.Entry.ParticipantId, $"{r.Entry.Participant.FirstName} {r.Entry.Participant.LastName}",
        r.Entry.BibNumber, r.FinishTime, r.GunTime, r.ChipTime,
        r.OverallPosition, r.CategoryPosition, r.GenderPosition, r.FinishStatus, r.RecordedAt);
}
