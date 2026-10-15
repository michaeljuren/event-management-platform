using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Seed;

/// <summary>
/// The representative seed data from database/RaceDay-Schema.sql, applied through HasData so it
/// ships with the InitialCreate migration. Identity values match the script exactly.
/// </summary>
public static class SeedData
{
    /// <summary>Password for every seeded account. Development/demo use only.</summary>
    public const string DemoPassword = "RaceDay@2026";

    /// <summary>
    /// ASP.NET Core Identity V3 (PBKDF2-HMACSHA512, 100 000 iterations) hash of <see cref="DemoPassword"/>.
    /// Pre-computed because HasData needs deterministic values; it verifies with PasswordHasher&lt;User&gt;.
    /// </summary>
    public const string DemoPasswordHash = "AQAAAAIAAYagAAAAEBAREhMUFRYXGBkaGxwdHh/eGzfAOStRFGOJb1AbE2eG2+N/xQOIQw3yD1ES+LBLpg==";

    private static readonly DateTime SeededAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Organizer" },
            new Role { RoleId = 3, RoleName = "Participant" });

        modelBuilder.Entity<Club>().HasData(
            new Club { ClubId = 1, ClubName = "Bay Athletic Club", Province = "Eastern Cape", AffiliationNumber = "EP-0142", ContactEmail = "info@bayac.co.za" },
            new Club { ClubId = 2, ClubName = "Randburg Harriers", Province = "Gauteng", AffiliationNumber = "GA-0871", ContactEmail = "info@randburgharriers.co.za" },
            new Club { ClubId = 3, ClubName = "Stellenbosch Long Runners", Province = "Western Cape", AffiliationNumber = "WP-0356", ContactEmail = "info@stellenboschlr.co.za" });

        modelBuilder.Entity<User>().HasData(
            SeedUser(1, "Admin", "User", "admin@raceday.co.za", "0110000000", new DateOnly(1990, 1, 1), "Other", null, null, null),
            SeedUser(2, "Sarah", "van der Merwe", "sarah.vdm@raceday.co.za", "0721234567", new DateOnly(1985, 3, 14), "Female", null, "Pieter van der Merwe", "0721234500"),
            SeedUser(3, "Thabo", "Nkosi", "thabo.nkosi@raceday.co.za", "0731234567", new DateOnly(1988, 7, 22), "Male", null, "Nomsa Nkosi", "0731234500"),
            SeedUser(4, "Lindiwe", "Dlamini", "lindiwe.d@example.co.za", "0741234567", new DateOnly(1994, 11, 2), "Female", 2, "Sipho Dlamini", "0741234500"),
            SeedUser(5, "Johan", "Botha", "johan.botha@example.co.za", "0761234567", new DateOnly(1990, 5, 30), "Male", 3, "Marie Botha", "0761234500"),
            SeedUser(6, "Aisha", "Patel", "aisha.patel@example.co.za", "0781234567", new DateOnly(1992, 9, 18), "Female", 1, "Farida Patel", "0781234500"));

        modelBuilder.Entity<UserRole>().HasData(
            new UserRole { UserId = 1, RoleId = 1 },
            new UserRole { UserId = 2, RoleId = 2 },
            new UserRole { UserId = 3, RoleId = 2 },
            new UserRole { UserId = 4, RoleId = 3 },
            new UserRole { UserId = 5, RoleId = 3 },
            new UserRole { UserId = 6, RoleId = 2 },
            new UserRole { UserId = 6, RoleId = 3 });

        modelBuilder.Entity<Venue>().HasData(
            new Venue { VenueId = 1, VenueName = "Green Point Park", AddressLine = "Fritz Sonnenberg Rd", City = "Cape Town", Province = "Western Cape", PostalCode = "8005", Latitude = -33.902800m, Longitude = 18.406500m },
            new Venue { VenueId = 2, VenueName = "Zwartkops Raceway Grounds", AddressLine = "Rabie Ring Rd", City = "Centurion", Province = "Gauteng", PostalCode = "0157", Latitude = -25.845400m, Longitude = 28.187900m },
            new Venue { VenueId = 3, VenueName = "Durban Beachfront Promenade", AddressLine = "Snell Parade", City = "Durban", Province = "KwaZulu-Natal", PostalCode = "4001", Latitude = -29.837500m, Longitude = 31.043600m });

        modelBuilder.Entity<Route>().HasData(
            new Route { RouteId = 1, VenueId = 1, RouteName = "Green Point Loop 10km", DistanceKm = 10.00m, ElevationGainM = 45, MapUrl = "https://maps.example.com/routes/green-point-10k", StartPointDescription = "Green Point Park main gate", EndPointDescription = "Green Point Park main gate" },
            new Route { RouteId = 2, VenueId = 1, RouteName = "Green Point Double Loop 21.1km", DistanceKm = 21.10m, ElevationGainM = 90, MapUrl = "https://maps.example.com/routes/green-point-21k", StartPointDescription = "Green Point Park main gate", EndPointDescription = "Green Point Park main gate" },
            new Route { RouteId = 3, VenueId = 2, RouteName = "Centurion Cycle Circuit 40km", DistanceKm = 40.00m, ElevationGainM = 220, MapUrl = "https://maps.example.com/routes/centurion-40k", StartPointDescription = "Zwartkops Raceway pit lane", EndPointDescription = "Zwartkops Raceway pit lane" },
            new Route { RouteId = 4, VenueId = 3, RouteName = "Durban Beachfront 5km Route", DistanceKm = 5.00m, ElevationGainM = 10, MapUrl = "https://maps.example.com/routes/durban-beach-5k", StartPointDescription = "uShaka Marine World", EndPointDescription = "Suncoast Casino" });

        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                EventId = 1, OrganizerId = 2, VenueId = 1, EventName = "Cape Town Peninsula Marathon",
                Description = "An annual road running event through Cape Town's Atlantic seaboard.",
                Discipline = "Running", EventDate = new DateOnly(2026, 4, 19), StartTime = new TimeOnly(6, 0),
                RegistrationOpenDate = new DateTime(2026, 1, 5, 0, 0, 0), RegistrationCloseDate = new DateTime(2026, 4, 10, 23, 59, 59),
                Status = "Published", CreatedAt = SeededAt, UpdatedAt = SeededAt
            },
            new Event
            {
                EventId = 2, OrganizerId = 3, VenueId = 2, EventName = "Centurion Cycle Classic",
                Description = "A closed-circuit road cycling race at the Zwartkops Raceway grounds.",
                Discipline = "Cycling", EventDate = new DateOnly(2026, 5, 10), StartTime = new TimeOnly(7, 0),
                RegistrationOpenDate = new DateTime(2026, 1, 15, 0, 0, 0), RegistrationCloseDate = new DateTime(2026, 5, 1, 23, 59, 59),
                Status = "Published", CreatedAt = SeededAt, UpdatedAt = SeededAt
            },
            new Event
            {
                EventId = 3, OrganizerId = 6, VenueId = 3, EventName = "Durban Beachfront Fun Run",
                Description = "A family-friendly 5km fun run along the Durban promenade.",
                Discipline = "Running", EventDate = new DateOnly(2026, 3, 22), StartTime = new TimeOnly(6, 30),
                RegistrationOpenDate = new DateTime(2026, 1, 20, 0, 0, 0), RegistrationCloseDate = new DateTime(2026, 3, 15, 23, 59, 59),
                Status = "Draft", CreatedAt = SeededAt, UpdatedAt = SeededAt
            });

        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, EventId = 1, RouteId = 1, CategoryName = "10km Individual", DistanceKm = 10.00m, MinAge = 12, GenderRestriction = "Open", EntryFee = 150.00m, MaxParticipants = 3000 },
            new Category { CategoryId = 2, EventId = 1, RouteId = 2, CategoryName = "21.1km Half Marathon", DistanceKm = 21.10m, MinAge = 16, GenderRestriction = "Open", EntryFee = 250.00m, MaxParticipants = 2000 },
            new Category { CategoryId = 3, EventId = 2, RouteId = 3, CategoryName = "40km Road Race", DistanceKm = 40.00m, MinAge = 18, GenderRestriction = "Open", EntryFee = 300.00m, MaxParticipants = 500 },
            new Category { CategoryId = 4, EventId = 3, RouteId = 4, CategoryName = "5km Fun Run", DistanceKm = 5.00m, MinAge = null, GenderRestriction = "Open", EntryFee = 80.00m, MaxParticipants = 1000 });

        modelBuilder.Entity<Entry>().HasData(
            new Entry { EntryId = 1, CategoryId = 2, ParticipantId = 4, BibNumber = "1042", Status = "Confirmed", AmountPaid = 250.00m, PaymentStatus = "Paid", EntryDate = SeededAt },
            new Entry { EntryId = 2, CategoryId = 1, ParticipantId = 5, BibNumber = "2031", Status = "Confirmed", AmountPaid = 150.00m, PaymentStatus = "Paid", EntryDate = SeededAt },
            new Entry { EntryId = 3, CategoryId = 3, ParticipantId = 6, BibNumber = null, Status = "Pending", AmountPaid = 0.00m, PaymentStatus = "Unpaid", EntryDate = SeededAt },
            new Entry { EntryId = 4, CategoryId = 3, ParticipantId = 4, BibNumber = "305", Status = "Confirmed", AmountPaid = 300.00m, PaymentStatus = "Paid", EntryDate = SeededAt });

        modelBuilder.Entity<Result>().HasData(
            new Result { ResultId = 1, EntryId = 1, FinishTime = new TimeOnly(1, 52, 34), GunTime = new TimeOnly(1, 53, 10), ChipTime = new TimeOnly(1, 52, 34), OverallPosition = 15, CategoryPosition = 3, GenderPosition = 2, FinishStatus = "Finished", RecordedByUserId = 2, RecordedAt = SeededAt },
            new Result { ResultId = 2, EntryId = 2, FinishTime = new TimeOnly(0, 48, 10), GunTime = new TimeOnly(0, 48, 40), ChipTime = new TimeOnly(0, 48, 10), OverallPosition = 22, CategoryPosition = 8, GenderPosition = 6, FinishStatus = "Finished", RecordedByUserId = 2, RecordedAt = SeededAt });
    }

    private static User SeedUser(int id, string first, string last, string email, string phone, DateOnly dob,
        string gender, int? clubId, string? emergencyName, string? emergencyPhone) => new()
    {
        UserId = id,
        FirstName = first,
        LastName = last,
        Email = email,
        PasswordHash = DemoPasswordHash,
        PhoneNumber = phone,
        DateOfBirth = dob,
        Gender = gender,
        ClubId = clubId,
        EmergencyContactName = emergencyName,
        EmergencyContactPhone = emergencyPhone,
        CreatedAt = SeededAt,
        IsActive = true
    };
}
