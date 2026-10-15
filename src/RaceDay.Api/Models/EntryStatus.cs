namespace RaceDay.Api.Models;

public static class EntryStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Withdrawn = "Withdrawn";

    public static readonly string[] All = [Pending, Confirmed, Cancelled, Withdrawn];

    /// <summary>Statuses that still hold a place in a category.</summary>
    public static readonly string[] Active = [Pending, Confirmed];
}

public static class PaymentStatus
{
    public const string Unpaid = "Unpaid";
    public const string Paid = "Paid";
    public const string Refunded = "Refunded";

    public static readonly string[] All = [Unpaid, Paid, Refunded];
}
