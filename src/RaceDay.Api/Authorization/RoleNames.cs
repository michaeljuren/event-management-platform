namespace RaceDay.Api.Authorization;

/// <summary>Role names exactly as stored in the Roles table.</summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Participant = "Participant";

    public const string OrganizerOrAdmin = Organizer + "," + Admin;

    /// <summary>Roles a member of the public may choose when registering.</summary>
    public static readonly string[] SelfRegisterable = [Organizer, Participant];

    public static readonly string[] All = [Admin, Organizer, Participant];
}
