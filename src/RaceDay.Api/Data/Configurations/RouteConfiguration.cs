using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes", t =>
        {
            t.HasCheckConstraint("CHK_Routes_DistanceKm", "DistanceKm > 0");
            t.HasCheckConstraint("CHK_Routes_ElevationGainM", "ElevationGainM IS NULL OR ElevationGainM >= 0");
        });
        builder.HasKey(r => r.RouteId).HasName("PK_Routes");
        builder.Property(r => r.RouteId).UseIdentityColumn();
        builder.Property(r => r.RouteName).HasMaxLength(150).IsRequired();
        builder.Property(r => r.DistanceKm).HasPrecision(6, 2);
        builder.Property(r => r.MapUrl).HasMaxLength(500);
        builder.Property(r => r.StartPointDescription).HasMaxLength(255);
        builder.Property(r => r.EndPointDescription).HasMaxLength(255);

        builder.HasIndex(r => r.VenueId).HasDatabaseName("IX_Routes_VenueId");

        builder.HasOne(r => r.Venue)
            .WithMany(v => v.Routes)
            .HasForeignKey(r => r.VenueId)
            .HasConstraintName("FK_Routes_Venues")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
