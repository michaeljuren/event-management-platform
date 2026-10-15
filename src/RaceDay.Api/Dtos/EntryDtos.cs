using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public record EntryResponse(
    int EntryId,
    int EventId,
    string EventName,
    DateOnly EventDate,
    int CategoryId,
    string CategoryName,
    decimal DistanceKm,
    int ParticipantId,
    string ParticipantName,
    string? BibNumber,
    DateTime EntryDate,
    string Status,
    decimal AmountPaid,
    string PaymentStatus,
    int? ResultId)
{
    /// <summary>Requires Category.Event, Participant and Result to be loaded.</summary>
    public static EntryResponse From(Entry e) => new(
        e.EntryId, e.Category.EventId, e.Category.Event.EventName, e.Category.Event.EventDate,
        e.CategoryId, e.Category.CategoryName, e.Category.DistanceKm,
        e.ParticipantId, $"{e.Participant.FirstName} {e.Participant.LastName}",
        e.BibNumber, e.EntryDate, e.Status, e.AmountPaid, e.PaymentStatus, e.Result?.ResultId);
}

/// <summary>A row in an organizer's roster for an event.</summary>
public record RosterEntryResponse(
    int EntryId,
    int CategoryId,
    string CategoryName,
    int ParticipantId,
    string FirstName,
    string LastName,
    string Email,
    string Gender,
    DateOnly DateOfBirth,
    string? ClubName,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BibNumber,
    DateTime EntryDate,
    string Status,
    string PaymentStatus);

public class AssignBibRequest
{
    [Required, StringLength(10, MinimumLength = 1)]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Bib numbers may only contain letters, digits and hyphens.")]
    public string BibNumber { get; set; } = string.Empty;
}
