using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("Venues", t =>
            t.HasCheckConstraint("CHK_Venues_Province", $"Province IN ({Province.SqlInList})"));
        builder.HasKey(v => v.VenueId).HasName("PK_Venues");
        builder.Property(v => v.VenueId).UseIdentityColumn();
        builder.Property(v => v.VenueName).HasMaxLength(150).IsRequired();
        builder.Property(v => v.AddressLine).HasMaxLength(200);
        builder.Property(v => v.City).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Province).HasMaxLength(20).IsRequired();
        builder.Property(v => v.PostalCode).HasMaxLength(10);
        builder.Property(v => v.Latitude).HasPrecision(9, 6);
        builder.Property(v => v.Longitude).HasPrecision(9, 6);
    }
}
