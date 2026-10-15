namespace RaceDay.Api.Models;

/// <summary>Event type. The API also accepts the short forms walk, run and cycle.</summary>
public static class Discipline
{
    public const string Running = "Running";
    public const string Walking = "Walking";
    public const string Cycling = "Cycling";
    public const string Multisport = "Multisport";

    public static readonly string[] All = [Running, Walking, Cycling, Multisport];

    /// <summary>Normalises user input (e.g. "run", "Walk", "cycling") to the stored value, or null if unknown.</summary>
    public static string? Normalise(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "run" or "running" => Running,
        "walk" or "walking" => Walking,
        "cycle" or "cycling" => Cycling,
        "multisport" => Multisport,
        _ => null
    };
}
