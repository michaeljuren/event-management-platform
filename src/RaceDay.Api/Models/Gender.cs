namespace RaceDay.Api.Models;

/// <summary>Allowed values for Users.Gender and Categories.GenderRestriction.</summary>
public static class Gender
{
    public const string Male = "Male";
    public const string Female = "Female";
    public const string Other = "Other";
    public const string Open = "Open";

    public static readonly string[] UserValues = [Male, Female, Other];

    public static readonly string[] RestrictionValues = [Open, Male, Female];
}
