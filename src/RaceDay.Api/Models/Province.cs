namespace RaceDay.Api.Models;

/// <summary>The nine South African provinces accepted by the Clubs and Venues check constraints.</summary>
public static class Province
{
    public static readonly string[] All =
    [
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal",
        "Limpopo", "Mpumalanga", "North West", "Northern Cape", "Western Cape"
    ];

    /// <summary>SQL list used by the CHK_*_Province check constraints.</summary>
    public static string SqlInList => string.Join(", ", All.Select(p => $"N'{p}'"));

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
