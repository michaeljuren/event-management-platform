namespace RaceDay.Api.Models;

/// <summary>A participant's enrolment in a category (and therefore in that category's event).</summary>
public class Entry
{
    public int EntryId { get; set; }

    public int CategoryId { get; set; }

    public int ParticipantId { get; set; }

    public string? BibNumber { get; set; }

    public DateTime EntryDate { get; set; } = DateTime.UtcNow;

    /// <summary>Pending, Confirmed, Cancelled or Withdrawn.</summary>
    public string Status { get; set; } = EntryStatus.Pending;

    public decimal AmountPaid { get; set; }

    /// <summary>Unpaid, Paid or Refunded.</summary>
    public string PaymentStatus { get; set; } = Models.PaymentStatus.Unpaid;

    public Category Category { get; set; } = null!;

    public User Participant { get; set; } = null!;

    public Result? Result { get; set; }
}
