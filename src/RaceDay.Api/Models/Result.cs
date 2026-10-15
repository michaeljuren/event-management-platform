namespace RaceDay.Api.Models;

/// <summary>The outcome recorded against a single entry.</summary>
public class Result
{
    public int ResultId { get; set; }

    public int EntryId { get; set; }

    public TimeOnly? FinishTime { get; set; }

    public TimeOnly? GunTime { get; set; }

    public TimeOnly? ChipTime { get; set; }

    public int? OverallPosition { get; set; }

    public int? CategoryPosition { get; set; }

    public int? GenderPosition { get; set; }

    /// <summary>Finished, DNF, DNS or DSQ.</summary>
    public string FinishStatus { get; set; } = Models.FinishStatus.Finished;

    public int? RecordedByUserId { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Entry Entry { get; set; } = null!;

    public User? RecordedBy { get; set; }
}

public static class FinishStatus
{
    public const string Finished = "Finished";
    public const string Dnf = "DNF";
    public const string Dns = "DNS";
    public const string Dsq = "DSQ";

    public static readonly string[] All = [Finished, Dnf, Dns, Dsq];
}
