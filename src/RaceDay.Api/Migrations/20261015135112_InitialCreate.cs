using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    ClubId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AffiliationNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.ClubId);
                    table.CheckConstraint("CHK_Clubs_Province", "Province IN (N'Eastern Cape', N'Free State', N'Gauteng', N'KwaZulu-Natal', N'Limpopo', N'Mpumalanga', N'North West', N'Northern Cape', N'Western Cape')");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    VenueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.VenueId);
                    table.CheckConstraint("CHK_Venues_Province", "Province IN (N'Eastern Cape', N'Free State', N'Gauteng', N'KwaZulu-Natal', N'Limpopo', N'Mpumalanga', N'North West', N'Northern Cape', N'Western Cape')");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Users_CreatedAt"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Users_IsActive")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CHK_Users_Gender", "Gender IN (N'Male', N'Female', N'Other')");
                    table.ForeignKey(
                        name: "FK_Users_Clubs",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    RouteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    ElevationGainM = table.Column<int>(type: "int", nullable: true),
                    MapUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartPointDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EndPointDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.RouteId);
                    table.CheckConstraint("CHK_Routes_DistanceKm", "DistanceKm > 0");
                    table.CheckConstraint("CHK_Routes_ElevationGainM", "ElevationGainM IS NULL OR ElevationGainM >= 0");
                    table.ForeignKey(
                        name: "FK_Routes_Venues",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "VenueId");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizerId = table.Column<int>(type: "int", nullable: false),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Discipline = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    RegistrationOpenDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistrationCloseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft")
                        .Annotation("Relational:DefaultConstraintName", "DF_Events_Status"),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Events_CreatedAt"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Events_UpdatedAt")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.CheckConstraint("CHK_Events_Discipline", "Discipline IN (N'Running', N'Walking', N'Cycling', N'Multisport')");
                    table.CheckConstraint("CHK_Events_RegDates", "RegistrationOpenDate < RegistrationCloseDate");
                    table.CheckConstraint("CHK_Events_Status", "Status IN (N'Draft', N'Published', N'Cancelled', N'Completed')");
                    table.ForeignKey(
                        name: "FK_Events_Users_Organizer",
                        column: x => x.OrganizerId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Events_Venues",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "VenueId");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<int>(type: "int", nullable: true),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: true),
                    GenderRestriction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Open")
                        .Annotation("Relational:DefaultConstraintName", "DF_Categories_GenderRestriction"),
                    EntryFee = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m)
                        .Annotation("Relational:DefaultConstraintName", "DF_Categories_EntryFee"),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                    table.CheckConstraint("CHK_Categories_DistanceKm", "DistanceKm > 0");
                    table.CheckConstraint("CHK_Categories_EntryFee", "EntryFee >= 0");
                    table.CheckConstraint("CHK_Categories_GenderRestriction", "GenderRestriction IN (N'Open', N'Male', N'Female')");
                    table.CheckConstraint("CHK_Categories_MaxParticipants", "MaxParticipants IS NULL OR MaxParticipants > 0");
                    table.CheckConstraint("CHK_Categories_MinAge", "MinAge IS NULL OR MinAge >= 0");
                    table.ForeignKey(
                        name: "FK_Categories_Events",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Categories_Routes",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "RouteId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    EntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    BibNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Entries_EntryDate"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                        .Annotation("Relational:DefaultConstraintName", "DF_Entries_Status"),
                    AmountPaid = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m)
                        .Annotation("Relational:DefaultConstraintName", "DF_Entries_AmountPaid"),
                    PaymentStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Unpaid")
                        .Annotation("Relational:DefaultConstraintName", "DF_Entries_PaymentStatus")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.EntryId);
                    table.CheckConstraint("CHK_Entries_AmountPaid", "AmountPaid >= 0");
                    table.CheckConstraint("CHK_Entries_PaymentStatus", "PaymentStatus IN (N'Unpaid', N'Paid', N'Refunded')");
                    table.CheckConstraint("CHK_Entries_Status", "Status IN (N'Pending', N'Confirmed', N'Cancelled', N'Withdrawn')");
                    table.ForeignKey(
                        name: "FK_Entries_Categories",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entries_Users_Participant",
                        column: x => x.ParticipantId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryId = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeOnly>(type: "time(3)", nullable: true),
                    GunTime = table.Column<TimeOnly>(type: "time(3)", nullable: true),
                    ChipTime = table.Column<TimeOnly>(type: "time(3)", nullable: true),
                    OverallPosition = table.Column<int>(type: "int", nullable: true),
                    CategoryPosition = table.Column<int>(type: "int", nullable: true),
                    GenderPosition = table.Column<int>(type: "int", nullable: true),
                    FinishStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Finished")
                        .Annotation("Relational:DefaultConstraintName", "DF_Results_FinishStatus"),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Results_RecordedAt")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.CheckConstraint("CHK_Results_CategoryPosition", "CategoryPosition IS NULL OR CategoryPosition > 0");
                    table.CheckConstraint("CHK_Results_FinishStatus", "FinishStatus IN (N'Finished', N'DNF', N'DNS', N'DSQ')");
                    table.CheckConstraint("CHK_Results_GenderPosition", "GenderPosition IS NULL OR GenderPosition > 0");
                    table.CheckConstraint("CHK_Results_OverallPosition", "OverallPosition IS NULL OR OverallPosition > 0");
                    table.ForeignKey(
                        name: "FK_Results_Entries",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Results_Users_RecordedBy",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Clubs",
                columns: new[] { "ClubId", "AffiliationNumber", "ClubName", "ContactEmail", "Province" },
                values: new object[,]
                {
                    { 1, "EP-0142", "Bay Athletic Club", "info@bayac.co.za", "Eastern Cape" },
                    { 2, "GA-0871", "Randburg Harriers", "info@randburgharriers.co.za", "Gauteng" },
                    { 3, "WP-0356", "Stellenbosch Long Runners", "info@stellenboschlr.co.za", "Western Cape" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Organizer" },
                    { 3, "Participant" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "ClubId", "CreatedAt", "DateOfBirth", "Email", "EmergencyContactName", "EmergencyContactPhone", "FirstName", "Gender", "IsActive", "LastName", "PasswordHash", "PhoneNumber" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1990, 1, 1), "admin@raceday.co.za", null, null, "Admin", "Other", true, "User", "AQAAAAIAAYagAAAAEBAREhMUFRYXGBkaGxwdHh/eGzfAOStRFGOJb1AbE2eG2+N/xQOIQw3yD1ES+LBLpg==", "0110000000" },
                    { 2, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1985, 3, 14), "sarah.vdm@raceday.co.za", "Pieter van der Merwe", "0721234500", "Sarah", "Female", true, "van der Merwe", "AQAAAAIAAYagAAAAEBAREhMUFRYXGBkaGxwdHh/eGzfAOStRFGOJb1AbE2eG2+N/xQOIQw3yD1ES+LBLpg==", "0721234567" },
                    { 3, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1988, 7, 22), "thabo.nkosi@raceday.co.za", "Nomsa Nkosi", "0731234500", "Thabo", "Male", true, "Nkosi", "AQAAAAIAAYagAAAAEBAREhMUFRYXGBkaGxwdHh/eGzfAOStRFGOJb1AbE2eG2+N/xQOIQw3yD1ES+LBLpg==", "0731234567" }
                });

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "VenueId", "AddressLine", "City", "Latitude", "Longitude", "PostalCode", "Province", "VenueName" },
                values: new object[,]
                {
                    { 1, "Fritz Sonnenberg Rd", "Cape Town", -33.902800m, 18.406500m, "8005", "Western Cape", "Green Point Park" },
                    { 2, "Rabie Ring Rd", "Centurion", -25.845400m, 28.187900m, "0157", "Gauteng", "Zwartkops Raceway Grounds" },
                    { 3, "Snell Parade", "Durban", -29.837500m, 31.043600m, "4001", "KwaZulu-Natal", "Durban Beachfront Promenade" }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "EventId", "BannerImageUrl", "CreatedAt", "Description", "Discipline", "EventDate", "EventName", "OrganizerId", "RegistrationCloseDate", "RegistrationOpenDate", "StartTime", "Status", "UpdatedAt", "VenueId" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "An annual road running event through Cape Town's Atlantic seaboard.", "Running", new DateOnly(2026, 4, 19), "Cape Town Peninsula Marathon", 2, new DateTime(2026, 4, 10, 23, 59, 59, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeOnly(6, 0, 0), "Published", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A closed-circuit road cycling race at the Zwartkops Raceway grounds.", "Cycling", new DateOnly(2026, 5, 10), "Centurion Cycle Classic", 3, new DateTime(2026, 5, 1, 23, 59, 59, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeOnly(7, 0, 0), "Published", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2 }
                });

            migrationBuilder.InsertData(
                table: "Routes",
                columns: new[] { "RouteId", "DistanceKm", "ElevationGainM", "EndPointDescription", "MapUrl", "RouteName", "StartPointDescription", "VenueId" },
                values: new object[,]
                {
                    { 1, 10.00m, 45, "Green Point Park main gate", "https://maps.example.com/routes/green-point-10k", "Green Point Loop 10km", "Green Point Park main gate", 1 },
                    { 2, 21.10m, 90, "Green Point Park main gate", "https://maps.example.com/routes/green-point-21k", "Green Point Double Loop 21.1km", "Green Point Park main gate", 1 },
                    { 3, 40.00m, 220, "Zwartkops Raceway pit lane", "https://maps.example.com/routes/centurion-40k", "Centurion Cycle Circuit 40km", "Zwartkops Raceway pit lane", 2 },
                    { 4, 5.00m, 10, "Suncoast Casino", "https://maps.example.com/routes/durban-beach-5k", "Durban Beachfront 5km Route", "uShaka Marine World", 3 }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 2 },
                    { 2, 3 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "ClubId", "CreatedAt", "DateOfBirth", "Email", "EmergencyContactName", "EmergencyContactPhone", "FirstName", "Gender", "IsActive", "LastName", "PasswordHash", "PhoneNumber" },
                values: new object[,]
                {
                    { 4, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1994, 11, 2), "lindiwe.d@example.co.za", "Sipho Dlamini", "0741234500", "Lindiwe", "Female", true, "Dlamini", "AQAAAAIAAYagAAAAEBAREhMUFRYXGBkaGxwdHh/eGzfAOStRFGOJb1AbE2eG2+N/xQOIQw3yD1ES+LBLpg==", "0741234567" },
                    { 5, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1990, 5, 30), "johan.botha@example.co.za", "Marie Botha", "0761234500", "Johan", "Male", true, "Botha", "AQAAAAIAAYagAAAAEBAREhMUFRYXGBkaGxwdHh/eGzfAOStRFGOJb1AbE2eG2+N/xQOIQw3yD1ES+LBLpg==", "0761234567" },
                    { 6, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1992, 9, 18), "aisha.patel@example.co.za", "Farida Patel", "0781234500", "Aisha", "Female", true, "Patel", "AQAAAAIAAYagAAAAEBAREhMUFRYXGBkaGxwdHh/eGzfAOStRFGOJb1AbE2eG2+N/xQOIQw3yD1ES+LBLpg==", "0781234567" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "DistanceKm", "EntryFee", "EventId", "GenderRestriction", "MaxParticipants", "MinAge", "RouteId" },
                values: new object[,]
                {
                    { 1, "10km Individual", 10.00m, 150.00m, 1, "Open", 3000, 12, 1 },
                    { 2, "21.1km Half Marathon", 21.10m, 250.00m, 1, "Open", 2000, 16, 2 },
                    { 3, "40km Road Race", 40.00m, 300.00m, 2, "Open", 500, 18, 3 }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "EventId", "BannerImageUrl", "CreatedAt", "Description", "Discipline", "EventDate", "EventName", "OrganizerId", "RegistrationCloseDate", "RegistrationOpenDate", "StartTime", "Status", "UpdatedAt", "VenueId" },
                values: new object[] { 3, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A family-friendly 5km fun run along the Durban promenade.", "Running", new DateOnly(2026, 3, 22), "Durban Beachfront Fun Run", 6, new DateTime(2026, 3, 15, 23, 59, 59, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeOnly(6, 30, 0), "Draft", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3 });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { 3, 4 },
                    { 3, 5 },
                    { 2, 6 },
                    { 3, 6 }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "DistanceKm", "EntryFee", "EventId", "GenderRestriction", "MaxParticipants", "MinAge", "RouteId" },
                values: new object[] { 4, "5km Fun Run", 5.00m, 80.00m, 3, "Open", 1000, null, 4 });

            migrationBuilder.InsertData(
                table: "Entries",
                columns: new[] { "EntryId", "AmountPaid", "BibNumber", "CategoryId", "EntryDate", "ParticipantId", "PaymentStatus", "Status" },
                values: new object[,]
                {
                    { 1, 250.00m, "1042", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Paid", "Confirmed" },
                    { 2, 150.00m, "2031", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, "Paid", "Confirmed" }
                });

            migrationBuilder.InsertData(
                table: "Entries",
                columns: new[] { "EntryId", "BibNumber", "CategoryId", "EntryDate", "ParticipantId", "PaymentStatus", "Status" },
                values: new object[] { 3, null, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, "Unpaid", "Pending" });

            migrationBuilder.InsertData(
                table: "Entries",
                columns: new[] { "EntryId", "AmountPaid", "BibNumber", "CategoryId", "EntryDate", "ParticipantId", "PaymentStatus", "Status" },
                values: new object[] { 4, 300.00m, "305", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Paid", "Confirmed" });

            migrationBuilder.InsertData(
                table: "Results",
                columns: new[] { "ResultId", "CategoryPosition", "ChipTime", "EntryId", "FinishStatus", "FinishTime", "GenderPosition", "GunTime", "OverallPosition", "RecordedAt", "RecordedByUserId" },
                values: new object[,]
                {
                    { 1, 3, new TimeOnly(1, 52, 34), 1, "Finished", new TimeOnly(1, 52, 34), 2, new TimeOnly(1, 53, 10), 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { 2, 8, new TimeOnly(0, 48, 10), 2, "Finished", new TimeOnly(0, 48, 10), 6, new TimeOnly(0, 48, 40), 22, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_EventId",
                table: "Categories",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_RouteId",
                table: "Categories",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_Event_Name",
                table: "Categories",
                columns: new[] { "EventId", "CategoryName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Clubs_ClubName",
                table: "Clubs",
                column: "ClubName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entries_ParticipantId",
                table: "Entries",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "UQ_Entries_Category_Participant",
                table: "Entries",
                columns: new[] { "CategoryId", "ParticipantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganizerId",
                table: "Events",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_VenueId",
                table: "Events",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_RecordedByUserId",
                table: "Results",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Results_EntryId",
                table: "Results",
                column: "EntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_VenueId",
                table: "Routes",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClubId",
                table: "Users",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Entries");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Venues");

            migrationBuilder.DropTable(
                name: "Clubs");
        }
    }
}
