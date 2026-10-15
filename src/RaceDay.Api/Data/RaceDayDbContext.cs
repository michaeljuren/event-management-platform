using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using RaceDay.Api.Data.Seed;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Code-First model of RaceDayDB. The entity configurations in <c>Data/Configurations</c>
/// reproduce database/RaceDay-Schema.sql: column types, defaults, constraint names,
/// check constraints, delete behaviours and indexes.
/// </summary>
public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // The SQL script declares every index explicitly (and deliberately skips FK columns that a
        // primary key or unique constraint already covers), so EF's automatic FK indexes are turned off
        // and each configuration declares the script's indexes itself.
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RaceDayDbContext).Assembly);
        SeedData.Apply(modelBuilder);
    }
}
