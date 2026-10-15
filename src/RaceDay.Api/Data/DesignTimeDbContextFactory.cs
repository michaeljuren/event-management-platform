using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaceDay.Api.Data;

/// <summary>
/// Lets <c>dotnet ef</c> / Package Manager Console create the context without starting the web host.
/// Reads the connection string from appsettings.json, overridable with the
/// <c>ConnectionStrings__RaceDayDb</c> environment variable.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RaceDayDbContext>
{
    public RaceDayDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("RaceDayDb")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new RaceDayDbContext(options);
    }
}
